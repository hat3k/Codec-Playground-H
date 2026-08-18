using MathNet.Numerics.IntegralTransforms;
using MediaInfoLib;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Diagnostics;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Codec_Playground_H
{
    public partial class Form1 : Form
    {
        private readonly string _settingsFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Codec Playground-H Settings.json"
        );
        private AppSettings _settings = new();
        private bool _isLoadingSettings = false;
        private readonly string _tempFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Temp");
        private const string APP_VERSION = "2026.08.10";

        private static readonly int[] CbrBitrates = [8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320];
        private static readonly int[] AbrBitrates = [8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320];

        public enum PlayMode
        {
            Original,
            Encoded,
            Mix,
            Difference,
            PhaseTest
        }

        private PlayMode _currentPlayMode = PlayMode.Original;

        private enum PlayerState
        {
            Playing,
            Paused,
            Stopped
        }

        private PlayerState _currentPlayerState = PlayerState.Stopped;
        private long _currentPlaybackPosition = 0;
        private string? _originalFilePath = null;
        private string? _encodedFilePath = null;

        private WasapiOut? _waveOut;
        private CodecPlaygroundMixer? _playgroundMixer;
        private MemorySampleSource? _originalMemorySource;
        private WaveFormat? _originalFormat;
        private int _originalBytesPerFrame;

        private bool _isDraggingTrackBarSeek = false;
        private bool _loopPlayback = true;

        private enum EncodingStatus
        {
            Idle,
            Queued,
            Running,
            Completed,
            Canceled,
            Error
        }

        private System.Windows.Forms.Timer? _settingsDebounceTimer;
        private CancellationTokenSource? _seamlessCts;
        private bool _isSeamlessReencode = false;
        private string? _activeEncodedCacheKey = null;

        private EncodingStatus _encodingStatus = EncodingStatus.Idle;
        private CancellationTokenSource? _encodingCts;
        private Task? _currentEncodingTask;
        private string? _selectedEncoderPath;
        private readonly Dictionary<string, string> _encodedCache = [];
        private readonly Dictionary<string, float[]> _decodedCache = [];
        private readonly Dictionary<string, string> _encoderSettingsReturnedByMICache = [];
        private readonly Dictionary<string, int> _delayCache = [];
        private readonly Lock _cacheLock = new();
        private string? _currentCacheKey = null;

        private bool _needsReencoding = false;
        private bool _pendingPlayAfterEncode = false;
        private EncodingStatus _lastLoggedStatus = EncodingStatus.Idle;
        private bool _lastSeamlessState = false;

        public class GitHubRelease
        {
            [JsonPropertyName("tag_name")]
            public string TagName { get; set; } = "";

            [JsonPropertyName("name")]
            public string Name { get; set; } = "";

            [JsonPropertyName("published_at")]
            public DateTime PublishedAt { get; set; }

            [JsonPropertyName("html_url")]
            public string HtmlUrl { get; set; } = "";

            [JsonPropertyName("assets")]
            public List<GitHubAsset> Assets { get; set; } = [];

            [JsonPropertyName("body")]
            public string Body { get; set; } = "";

            public bool IsNewerThan(string currentVersion)
            {
                var parts1 = TagName.Split('.');
                var parts2 = currentVersion.Split('.');

                int maxLen = Math.Max(parts1.Length, parts2.Length);

                for (int i = 0; i < maxLen; i++)
                {
                    int p1 = i < parts1.Length ? int.Parse(parts1[i]) : 0;
                    int p2 = i < parts2.Length ? int.Parse(parts2[i]) : 0;

                    if (p1 != p2)
                        return p1 > p2;
                }

                return false;
            }
        }

        public class GitHubAsset
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = "";

            [JsonPropertyName("browser_download_url")]
            public string DownloadUrl { get; set; } = "";

            [JsonPropertyName("size")]
            public long Size { get; set; }
        }

        private System.Windows.Forms.Timer? _notificationTimer;

        public Form1()
        {
            Log("🏗️ Form1 constructor called");
            InitializeComponent();
            Log("✅ Form1 initialized");
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            Log($"📋 Form1_Load started, version: {APP_VERSION}");
            Text = $"Codec Playground-H [{APP_VERSION}]";
            labelEncoderSettingsReturnedByMI.Text = string.Empty;
            Log("📂 Loading settings...");
            LoadSettings();
            Log("📁 Ensuring temp folder exists...");
            EnsureTempFolderExists();
            EnsureMediaInfoDllExists();
            Log("✅ Form1_Load completed");
        }

        private static void Log(string message)
        {
            string logMsg = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
            Debug.WriteLine(logMsg);
        }

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static readonly JsonSerializerOptions _jsonOptionsCaseInsensitive = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private void SaveSettings()
        {
            Log($"💾 Saving settings to {_settingsFilePath}");
            try
            {
                Log($"📊 Saving {listViewEncoders.Items.Count} encoders");
                _settings.EncoderPaths = [.. listViewEncoders.Items
            .Cast<ListViewItem>()
            .Select(item => item.Tag?.ToString() ?? string.Empty)
            .Where(path => !string.IsNullOrEmpty(path))];

                Log($"📊 Saving {listViewAudioFiles.Items.Count} audio files");
                _settings.AudioFilePaths = [.. listViewAudioFiles.Items
            .Cast<ListViewItem>()
            .Select(item => item.Tag?.ToString() ?? string.Empty)
            .Where(path => !string.IsNullOrEmpty(path))];

                _settings.SelectedEncoderPath = _selectedEncoderPath;
                _settings.SelectedAudioFilePath = _originalFilePath;
                _settings.LoopPlayback = _loopPlayback;
                _settings.CurrentPlayMode = _currentPlayMode;
                _settings.EncoderSettings.MixBalanceValue = trackBarMixBalance.Value;
                _settings.CheckForUpdates = checkBoxCheckForUpdates.Checked;

                _settings.EncoderSettings.ModeCBR_MP3 = radioButtonModeCBR_MP3.Checked;
                _settings.EncoderSettings.ModeABR_MP3 = radioButtonModeABR_MP3.Checked;
                _settings.EncoderSettings.ModeVBR_MP3 = radioButtonModeVBR_MP3.Checked;

                _settings.EncoderSettings.CBRValue_MP3 = trackBarCBR_MP3.Value;
                _settings.EncoderSettings.ABRValue_MP3 = trackBarABR_MP3.Value;
                _settings.EncoderSettings.VBRValue_MP3 = trackBarVBR_MP3.Value;
                _settings.EncoderSettings.QualityValue_MP3 = trackBarParameter_q_MP3.Value;

                _settings.EncoderSettings.UseQuality_MP3 = checkBoxParameter_q_MP3.Checked;
                _settings.EncoderSettings.UseChannelModes_MP3 = checkBoxChannelsModes_MP3.Checked;

                _settings.EncoderSettings.ChannelJointStereo_MP3 = radioButtonJointStereo_MP3.Checked;
                _settings.EncoderSettings.ChannelStereo_MP3 = radioButtonStereo_MP3.Checked;
                _settings.EncoderSettings.ChannelMono_MP3 = radioButtonMono_MP3.Checked;

                _settings.EncoderSettings.LabelCBR_MP3 = labelCBRValue_MP3.Text;
                _settings.EncoderSettings.LabelABR_MP3 = labelABRValue_MP3.Text;
                _settings.EncoderSettings.LabelVBR_MP3 = labelVBRValue_MP3.Text;
                _settings.EncoderSettings.LabelQuality_MP3 = labelParameter_qValue_MP3.Text;

                _settings.UserPresets.UserPreset1 = radioButtonUserPreset1.Checked;
                _settings.UserPresets.UserPreset1Name = radioButtonUserPreset1.Text;
                _settings.UserPresets.UserPreset1CommandLineArgs = textBoxUserPreset1.Text;

                _settings.UserPresets.UserPreset2 = radioButtonUserPreset2.Checked;
                _settings.UserPresets.UserPreset2Name = radioButtonUserPreset2.Text;
                _settings.UserPresets.UserPreset2CommandLineArgs = textBoxUserPreset2.Text;

                _settings.UserPresets.UserPreset3 = radioButtonUserPreset3.Checked;
                _settings.UserPresets.UserPreset3Name = radioButtonUserPreset3.Text;
                _settings.UserPresets.UserPreset3CommandLineArgs = textBoxUserPreset3.Text;

                _settings.UserPresets.UserPreset4 = radioButtonUserPreset4.Checked;
                _settings.UserPresets.UserPreset4Name = radioButtonUserPreset4.Text;
                _settings.UserPresets.UserPreset4CommandLineArgs = textBoxUserPreset4.Text;

                _settings.UserPresets.UserPreset5 = radioButtonUserPreset5.Checked;
                _settings.UserPresets.UserPreset5Name = radioButtonUserPreset5.Text;
                _settings.UserPresets.UserPreset5CommandLineArgs = textBoxUserPreset5.Text;

                _settings.UserPresets.UserPreset6 = radioButtonUserPreset6.Checked;
                _settings.UserPresets.UserPreset6Name = radioButtonUserPreset6.Text;
                _settings.UserPresets.UserPreset6CommandLineArgs = textBoxUserPreset6.Text;

                Log($"📊 Saved user presets: 1={_settings.UserPresets.UserPreset1}, 2={_settings.UserPresets.UserPreset2}, 3={_settings.UserPresets.UserPreset3}, 4={_settings.UserPresets.UserPreset4}, 5={_settings.UserPresets.UserPreset5}, 6={_settings.UserPresets.UserPreset6}");

                _settings.HiddenModeMP3Off = radioButton_Hidden_Mode_OFF_MP3.Checked;
                _settings.HiddenUserPresetOff = radioButton_Hidden_UserPreset_OFF.Checked;

                Log($"📊 Saved hidden radio states: ModeMP3Off={_settings.HiddenModeMP3Off}, UserPresetOff={_settings.HiddenUserPresetOff}");

                _settings.EncoderListView.ColumnWidths = [];
                foreach (ColumnHeader col in listViewEncoders.Columns)
                {
                    _settings.EncoderListView.ColumnWidths.Add(col.Width);
                }
                Log($"📊 Saved {_settings.EncoderListView.ColumnWidths.Count} encoder column widths");

                _settings.AudioListView.ColumnWidths = [];
                foreach (ColumnHeader col in listViewAudioFiles.Columns)
                {
                    _settings.AudioListView.ColumnWidths.Add(col.Width);
                }
                Log($"📊 Saved {_settings.AudioListView.ColumnWidths.Count} audio column widths");

                if (WindowState != FormWindowState.Maximized)
                {
                    _settings.Window.Width = Width;
                    _settings.Window.Height = Height;
                    _settings.Window.X = Location.X;
                    _settings.Window.Y = Location.Y;
                    Log($"📊 Window position: ({Location.X}, {Location.Y}) size: {Width}x{Height}");
                }
                else
                {
                    var bounds = RestoreBounds;
                    _settings.Window.Width = bounds.Width;
                    _settings.Window.Height = bounds.Height;
                    _settings.Window.X = bounds.X;
                    _settings.Window.Y = bounds.Y;
                    Log($"📊 Window maximized, restoring to: ({bounds.X}, {bounds.Y}) size: {bounds.Width}x{bounds.Height}");
                }
                _settings.Window.Maximized = WindowState == FormWindowState.Maximized;
                Log($"📊 Window maximized: {_settings.Window.Maximized}");

                string json = JsonSerializer.Serialize(_settings, _jsonOptions);
                File.WriteAllText(_settingsFilePath, json);
                Log($"✅ Settings saved successfully ({json.Length} bytes)");
            }
            catch (Exception ex)
            {
                Log($"❌ Failed to save settings: {ex.Message}");
                Log($"❌ StackTrace: {ex.StackTrace}");
            }
        }
        private void LoadSettings()
        {
            Log($"📂 Loading settings from {_settingsFilePath}");
            try
            {
                if (!File.Exists(_settingsFilePath))
                {
                    Log($"ℹ️ Settings file not found, using defaults");
                    MinimumSize = new Size(874, 515);
                    return;
                }

                _isLoadingSettings = true;
                string json = File.ReadAllText(_settingsFilePath);
                Log($"📄 Settings file size: {json.Length} bytes");
                _settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptionsCaseInsensitive) ?? new AppSettings();
                Log($"✅ Settings deserialized successfully");

                Log($"📊 Loading {_settings.EncoderPaths.Count} encoders");
                listViewEncoders.Items.Clear();
                foreach (string encoderPath in _settings.EncoderPaths)
                {
                    if (!File.Exists(encoderPath))
                    {
                        Log($"⚠️ Encoder file not found: {encoderPath}");
                        continue;
                    }
                    (string? name, string? version) = GetEncoderInfo(encoderPath);
                    ListViewItem item = new(name) { Tag = encoderPath, Checked = false };
                    _ = item.SubItems.Add(version);
                    _ = item.SubItems.Add(Path.GetDirectoryName(encoderPath) ?? string.Empty);
                    _ = listViewEncoders.Items.Add(item);
                    Log($"✅ Added encoder: {name} version {version}");
                }

                if (_settings.EncoderListView.ColumnWidths.Count == listViewEncoders.Columns.Count)
                {
                    for (int i = 0; i < listViewEncoders.Columns.Count; i++)
                    {
                        listViewEncoders.Columns[i].Width = _settings.EncoderListView.ColumnWidths[i];
                    }
                    Log($"✅ Restored {_settings.EncoderListView.ColumnWidths.Count} encoder column widths");
                }
                else
                {
                    Log($"⚠️ Encoder column count mismatch: saved {_settings.EncoderListView.ColumnWidths.Count}, actual {listViewEncoders.Columns.Count}");
                }

                Log($"📊 Loading {_settings.AudioFilePaths.Count} audio files");
                listViewAudioFiles.Items.Clear();
                foreach (string audioPath in _settings.AudioFilePaths)
                {
                    if (!File.Exists(audioPath))
                    {
                        Log($"⚠️ Audio file not found: {audioPath}");
                        continue;
                    }
                    ListViewItem item = new(Path.GetFileName(audioPath)) { Tag = audioPath, Checked = false };

                    try
                    {
                        int channels = 0, bitsPerSample = 0, sampleRate = 0;
                        double durationSec = 0;
                        string ext = Path.GetExtension(audioPath);

                        if (ext.Equals(".wav", StringComparison.OrdinalIgnoreCase))
                        {
                            using var wavReader = new WaveFileReader(audioPath);
                            channels = wavReader.WaveFormat.Channels;
                            bitsPerSample = wavReader.WaveFormat.BitsPerSample;
                            sampleRate = wavReader.WaveFormat.SampleRate;
                            durationSec = wavReader.TotalTime.TotalSeconds;
                            Log($"📊 WAV info: {channels}ch, {bitsPerSample}bit, {sampleRate}Hz, {durationSec:F1}s");
                        }
                        else if (ext.Equals(".flac", StringComparison.OrdinalIgnoreCase))
                        {
                            var flacInfo = ReadFlacStreamInfo(audioPath);
                            channels = flacInfo.Channels;
                            bitsPerSample = flacInfo.BitsPerSample;
                            sampleRate = flacInfo.SampleRate;
                            durationSec = flacInfo.TotalSamples > 0 && flacInfo.SampleRate > 0
                                ? (double)flacInfo.TotalSamples / flacInfo.SampleRate
                                : 0;
                            Log($"📊 FLAC info: {channels}ch, {bitsPerSample}bit, {sampleRate}Hz, {durationSec:F1}s");
                        }

                        _ = item.SubItems.Add(channels.ToString());
                        _ = item.SubItems.Add(bitsPerSample.ToString());
                        _ = item.SubItems.Add($"{sampleRate / 1000.0:0.0} kHz");
                        _ = item.SubItems.Add($"{durationSec:F1}s");
                    }
                    catch (Exception ex)
                    {
                        Log($"⚠️ Failed to read audio info for {audioPath}: {ex.Message}");
                        _ = item.SubItems.Add("?");
                        _ = item.SubItems.Add("?");
                        _ = item.SubItems.Add("?");
                        _ = item.SubItems.Add("?");
                    }
                    _ = item.SubItems.Add(Path.GetDirectoryName(audioPath) ?? string.Empty);
                    _ = listViewAudioFiles.Items.Add(item);
                    Log($"✅ Added audio file: {Path.GetFileName(audioPath)}");
                }

                if (_settings.AudioListView.ColumnWidths.Count == listViewAudioFiles.Columns.Count)
                {
                    for (int i = 0; i < listViewAudioFiles.Columns.Count; i++)
                    {
                        listViewAudioFiles.Columns[i].Width = _settings.AudioListView.ColumnWidths[i];
                    }
                    Log($"✅ Restored {_settings.AudioListView.ColumnWidths.Count} audio column widths");
                }
                else
                {
                    Log($"⚠️ Audio column count mismatch: saved {_settings.AudioListView.ColumnWidths.Count}, actual {listViewAudioFiles.Columns.Count}");
                }

                textBoxUserPreset1.Text = _settings.UserPresets.UserPreset1CommandLineArgs ?? "";
                radioButtonUserPreset1.Checked = _settings.UserPresets.UserPreset1;

                textBoxUserPreset2.Text = _settings.UserPresets.UserPreset2CommandLineArgs ?? "";
                radioButtonUserPreset2.Checked = _settings.UserPresets.UserPreset2;

                textBoxUserPreset3.Text = _settings.UserPresets.UserPreset3CommandLineArgs ?? "";
                radioButtonUserPreset3.Checked = _settings.UserPresets.UserPreset3;

                textBoxUserPreset4.Text = _settings.UserPresets.UserPreset4CommandLineArgs ?? "";
                radioButtonUserPreset4.Checked = _settings.UserPresets.UserPreset4;

                textBoxUserPreset5.Text = _settings.UserPresets.UserPreset5CommandLineArgs ?? "";
                radioButtonUserPreset5.Checked = _settings.UserPresets.UserPreset5;

                textBoxUserPreset6.Text = _settings.UserPresets.UserPreset6CommandLineArgs ?? "";
                radioButtonUserPreset6.Checked = _settings.UserPresets.UserPreset6;

                Log($"📊 Restored user presets: 1={_settings.UserPresets.UserPreset1}, 2={_settings.UserPresets.UserPreset2}, 3={_settings.UserPresets.UserPreset3}, 4={_settings.UserPresets.UserPreset4}, 5={_settings.UserPresets.UserPreset5}, 6={_settings.UserPresets.UserPreset6}");

                radioButton_Hidden_Mode_OFF_MP3.Checked = _settings.HiddenModeMP3Off;
                radioButton_Hidden_UserPreset_OFF.Checked = _settings.HiddenUserPresetOff;

                Log($"📊 Restored hidden radio states: ModeMP3Off={_settings.HiddenModeMP3Off}, UserPresetOff={_settings.HiddenUserPresetOff}");

                if (!string.IsNullOrEmpty(_settings.SelectedEncoderPath))
                {
                    Log($"🔍 Looking for selected encoder: {_settings.SelectedEncoderPath}");
                    foreach (ListViewItem item in listViewEncoders.Items)
                    {
                        if (item.Tag?.ToString() == _settings.SelectedEncoderPath)
                        {
                            item.Checked = true;
                            ListViewEncoders_ItemChecked(this, new ItemCheckedEventArgs(item));
                            Log($"✅ Selected encoder found and checked");
                            break;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(_settings.SelectedAudioFilePath))
                {
                    Log($"🔍 Looking for selected audio file: {_settings.SelectedAudioFilePath}");
                    foreach (ListViewItem item in listViewAudioFiles.Items)
                    {
                        if (item.Tag?.ToString() == _settings.SelectedAudioFilePath)
                        {
                            item.Checked = true;
                            ListViewAudioFiles_ItemChecked(this, new ItemCheckedEventArgs(item));
                            Log($"✅ Selected audio file found and checked");
                            break;
                        }
                    }
                }

                _loopPlayback = _settings.LoopPlayback;
                buttonLoopPlayback.Text = _loopPlayback ? "Loop: ON" : "Loop: OFF";
                Log($"🔁 Loop playback: {_loopPlayback}");

                _currentPlayMode = _settings.CurrentPlayMode;
                radioButtonPlayOriginal.Checked = _currentPlayMode == PlayMode.Original;
                radioButtonPlayEncoded.Checked = _currentPlayMode == PlayMode.Encoded;
                radioButtonPlayMix.Checked = _currentPlayMode == PlayMode.Mix;
                radioButtonPlayDifference.Checked = _currentPlayMode == PlayMode.Difference;
                Log($"🎵 Current play mode: {_currentPlayMode}");

                trackBarMixBalance.Value = Math.Clamp(_settings.EncoderSettings.MixBalanceValue, 0, 100);
                bool showBalance = _currentPlayMode == PlayMode.Mix;
                labelMixBalance.Visible = showBalance;
                trackBarMixBalance.Visible = showBalance;
                if (showBalance)
                {
                    int origPct = (int)((1f - trackBarMixBalance.Value / 100f) * 100);
                    int encPct = (int)((trackBarMixBalance.Value / 100f) * 100);
                    labelMixBalance.Text = $"{origPct} / {encPct}";
                }

                radioButtonModeCBR_MP3.Checked = _settings.EncoderSettings.ModeCBR_MP3;
                radioButtonModeABR_MP3.Checked = _settings.EncoderSettings.ModeABR_MP3;
                radioButtonModeVBR_MP3.Checked = _settings.EncoderSettings.ModeVBR_MP3;

                trackBarCBR_MP3.Value = _settings.EncoderSettings.CBRValue_MP3;
                trackBarABR_MP3.Value = _settings.EncoderSettings.ABRValue_MP3;
                trackBarVBR_MP3.Value = _settings.EncoderSettings.VBRValue_MP3;
                trackBarParameter_q_MP3.Value = _settings.EncoderSettings.QualityValue_MP3;

                checkBoxParameter_q_MP3.Checked = _settings.EncoderSettings.UseQuality_MP3;
                checkBoxChannelsModes_MP3.Checked = _settings.EncoderSettings.UseChannelModes_MP3;

                radioButtonJointStereo_MP3.Checked = _settings.EncoderSettings.ChannelJointStereo_MP3;
                radioButtonStereo_MP3.Checked = _settings.EncoderSettings.ChannelStereo_MP3;
                radioButtonMono_MP3.Checked = _settings.EncoderSettings.ChannelMono_MP3;

                labelCBRValue_MP3.Text = _settings.EncoderSettings.LabelCBR_MP3;
                labelABRValue_MP3.Text = _settings.EncoderSettings.LabelABR_MP3;
                labelVBRValue_MP3.Text = _settings.EncoderSettings.LabelVBR_MP3;
                labelParameter_qValue_MP3.Text = _settings.EncoderSettings.LabelQuality_MP3;

                MinimumSize = new Size(874, 515);

                if (_settings.Window.Maximized)
                {
                    Log($"📊 Setting window to maximized");
                    WindowState = FormWindowState.Maximized;
                }
                else
                {
                    int width = Math.Max(_settings.Window.Width, MinimumSize.Width);
                    int height = Math.Max(_settings.Window.Height, MinimumSize.Height);
                    var screen = Screen.FromPoint(new Point(_settings.Window.X, _settings.Window.Y));
                    if (!screen.Bounds.Contains(_settings.Window.X, _settings.Window.Y))
                    {
                        Width = width;
                        Height = height;
                        CenterToScreen();
                        Log($"📊 Window centered on screen: ({Location.X}, {Location.Y}) size: {Width}x{Height}");
                    }
                    else
                    {
                        Width = width;
                        Height = height;
                        Location = new Point(_settings.Window.X, _settings.Window.Y);
                        Log($"📊 Window position: ({Location.X}, {Location.Y}) size: {Width}x{Height}");
                    }
                }

                checkBoxCheckForUpdates.Checked = _settings.CheckForUpdates;
            }
            catch (Exception ex)
            {
                Log($"❌ Failed to load settings: {ex.Message}");
                Log($"❌ StackTrace: {ex.StackTrace}");
            }
            finally
            {
                _isLoadingSettings = false;
                Log($"✅ Settings loading completed");
            }
        }
        private void EnsureTempFolderExists()
        {
            Log($"🔍 Checking temp folder: {_tempFolder}");
            try
            {
                if (!Directory.Exists(_tempFolder))
                {
                    Log($"📁 Creating temp folder: {_tempFolder}");
                    Directory.CreateDirectory(_tempFolder);
                    Log($"✅ Temp folder created: {_tempFolder}");
                }
                else
                {
                    Log($"✅ Temp folder already exists: {_tempFolder}");
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Failed to create temp folder: {ex.Message}");
                Log($"❌ StackTrace: {ex.StackTrace}");
            }
        }

        public class CodecPlaygroundMixer : ISampleProvider
        {
            private readonly MemorySampleSource _sourceOriginal;
            private MemorySampleSource _sourceEncoded;
            private readonly int _channels;
            private readonly int _sampleRate;
            private int _sampleOffset;
            private readonly int _bytesPerFrameOriginal;
            private volatile float _mixBalance = 0.5f;

            private readonly float[] _delayBufferOrig;
            private int _origWriteIdx;
            private int _origReadIdx;
            private int _bufferedSamplesOrig;

            private readonly Lock _seekLock = new();

            public PlayMode CurrentMode { get; set; } = PlayMode.Original;
            public WaveFormat WaveFormat { get; }

            public CodecPlaygroundMixer(MemorySampleSource original, MemorySampleSource encoded,
                                        WaveFormat originalFormat, int sampleOffset = 0)
            {
                Log($"🔧 Mixer ctor: ch={originalFormat.Channels}, rate={originalFormat.SampleRate}, offset={sampleOffset}");
                _sourceOriginal = original;
                _sourceEncoded = encoded;
                _channels = originalFormat.Channels;
                _sampleRate = originalFormat.SampleRate;
                WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(_sampleRate, _channels);
                _sampleOffset = sampleOffset;
                _bytesPerFrameOriginal = originalFormat.Channels * (originalFormat.BitsPerSample / 8);
                _delayBufferOrig = new float[192000];
                ResetSync();
            }

            public void ResetSync()
            {
                lock (_seekLock) { ResetSyncInternal(); }
            }

            private void ResetSyncInternal()
            {
                _origWriteIdx = 0;
                _origReadIdx = 0;
                _bufferedSamplesOrig = 0;
                Array.Clear(_delayBufferOrig, 0, _delayBufferOrig.Length);
                if (_sampleOffset > 0 && _sampleOffset < _delayBufferOrig.Length)
                {
                    _origReadIdx = (_origWriteIdx - _sampleOffset + _delayBufferOrig.Length) % _delayBufferOrig.Length;
                    _bufferedSamplesOrig = _sampleOffset;
                }
            }

            public void SeekToBytes(long originalBytesPosition)
            {
                lock (_seekLock)
                {
                    long frame = originalBytesPosition / _bytesPerFrameOriginal;
                    long samplePos = frame * _channels;

                    _sourceEncoded.PositionSamples = samplePos;

                    _origWriteIdx = 0;
                    _origReadIdx = 0;
                    _bufferedSamplesOrig = 0;
                    Array.Clear(_delayBufferOrig, 0, _delayBufferOrig.Length);

                    if (_sampleOffset > 0 && _sampleOffset < _delayBufferOrig.Length)
                    {
                        int D = _sampleOffset;
                        long preStart = samplePos - D;

                        if (preStart >= 0)
                        {
                            _sourceOriginal.PositionSamples = preStart;
                            float[] preBuf = new float[D];
                            int preRead = _sourceOriginal.Read(preBuf, 0, D);
                            for (int i = 0; i < preRead; i++)
                            {
                                _delayBufferOrig[i] = preBuf[i];
                            }
                            _origWriteIdx = preRead % _delayBufferOrig.Length;
                            _origReadIdx = 0;
                            _bufferedSamplesOrig = preRead;
                            Log($"🔄 Seek: pre-filled {preRead} delay samples from pos {preStart}");
                        }
                        else
                        {
                            _sourceOriginal.PositionSamples = 0;
                            _origReadIdx = (_origWriteIdx - D + _delayBufferOrig.Length) % _delayBufferOrig.Length;
                            _bufferedSamplesOrig = D;
                            Log($"🔄 Seek: start of file, using {D} zero delay samples");
                        }
                    }
                    else
                    {
                        _sourceOriginal.PositionSamples = samplePos;
                    }

                    Log($"🔄 Mixer SeekToBytes: {originalBytesPosition} B → frame {frame}");
                }
            }

            public long GetPositionBytes()
            {
                lock (_seekLock)
                {
                    long frame = _sourceOriginal.PositionSamples / _channels;
                    return frame * _bytesPerFrameOriginal;
                }
            }

            public long GetTotalBytes()
            {
                long frame = _sourceOriginal.LengthSamples / _channels;
                return frame * _bytesPerFrameOriginal;
            }

            public int Read(float[] buffer, int offset, int count)
            {
                lock (_seekLock)
                {
                    float[] rawOrig = new float[count];
                    float[] rawEnc = new float[count];
                    int readOrig = _sourceOriginal.Read(rawOrig, 0, count);
                    int readEnc = _sourceEncoded.Read(rawEnc, 0, count);
                    if (readOrig == 0 && readEnc == 0) return 0;
                    int maxRead = Math.Max(readOrig, readEnc);

                    for (int i = 0; i < maxRead; i++)
                    {
                        float sOrig = (i < readOrig) ? rawOrig[i] : 0f;
                        _delayBufferOrig[_origWriteIdx] = sOrig;
                        _origWriteIdx = (_origWriteIdx + 1) % _delayBufferOrig.Length;
                        if (_bufferedSamplesOrig < _delayBufferOrig.Length) _bufferedSamplesOrig++;
                    }

                    for (int i = 0; i < maxRead; i++)
                    {
                        float sOrig = PopOrig();
                        float sEnc = (i < readEnc) ? rawEnc[i] : 0f;
                        switch (CurrentMode)
                        {
                            case PlayMode.Original: buffer[offset + i] = sOrig; break;
                            case PlayMode.Encoded: buffer[offset + i] = sEnc; break;
                            case PlayMode.Mix:
                                float b = _mixBalance;
                                buffer[offset + i] = sOrig * (1f - b) + sEnc * b;
                                break;
                            case PlayMode.Difference: buffer[offset + i] = (sOrig - sEnc) * 0.5f; break;
                            case PlayMode.PhaseTest: buffer[offset + i] = sOrig - sOrig; break;
                        }
                    }
                    return maxRead;
                }
            }

            private float PopOrig()
            {
                if (_bufferedSamplesOrig == 0) return 0f;
                float s = _delayBufferOrig[_origReadIdx];
                _origReadIdx = (_origReadIdx + 1) % _delayBufferOrig.Length;
                _bufferedSamplesOrig--;
                return s;
            }

            public void SwapEncoded(MemorySampleSource newEncoded, int newSampleOffset)
            {
                lock (_seekLock)
                {
                    long currentReadPos = _sourceOriginal.PositionSamples;

                    long newEncPos = Math.Clamp(currentReadPos, 0, newEncoded.LengthSamples);
                    newEncoded.PositionSamples = newEncPos;

                    _origWriteIdx = 0;
                    _origReadIdx = 0;
                    _bufferedSamplesOrig = 0;
                    Array.Clear(_delayBufferOrig, 0, _delayBufferOrig.Length);

                    if (newSampleOffset > 0 && newSampleOffset < _delayBufferOrig.Length)
                    {
                        int D = newSampleOffset;
                        long preStart = currentReadPos - D;

                        if (preStart >= 0)
                        {
                            _sourceOriginal.PositionSamples = preStart;
                            float[] preBuf = new float[D];
                            int preRead = _sourceOriginal.Read(preBuf, 0, D);
                            for (int i = 0; i < preRead; i++)
                            {
                                _delayBufferOrig[i] = preBuf[i];
                            }
                            _origWriteIdx = preRead % _delayBufferOrig.Length;
                            _origReadIdx = 0;
                            _bufferedSamplesOrig = preRead;
                            Log($"🔀 SwapEncoded: pre-filled {preRead} samples from pos {preStart}");
                        }
                        else
                        {
                            _sourceOriginal.PositionSamples = 0;
                            _origReadIdx = (_origWriteIdx - D + _delayBufferOrig.Length) % _delayBufferOrig.Length;
                            _bufferedSamplesOrig = D;
                            Log($"🔀 SwapEncoded: near start, using {D} zero delay samples");
                        }
                    }
                    else
                    {
                        _sourceOriginal.PositionSamples = currentReadPos;
                    }

                    _sourceEncoded = newEncoded;
                    _sampleOffset = newSampleOffset;

                    Log($"🔀 SwapEncoded: readPos={currentReadPos}, newDelay={newSampleOffset}, newEncPos={newEncPos}, bufferedNow={_bufferedSamplesOrig}");
                }
            }

            public void SetMixBalance(float value)
            {
                _mixBalance = Math.Clamp(value, 0f, 1f);
            }
        }

        public class MemorySampleSource(float[] data, WaveFormat format) : ISampleProvider
        {
            private readonly float[] _data = data;
            private long _position;

            public WaveFormat WaveFormat { get; } = format;
            public long LengthSamples => _data.Length;

            public long PositionSamples
            {
                get => _position;
                set => _position = Math.Clamp(value, 0, _data.Length);
            }

            public int Read(float[] buffer, int offset, int count)
            {
                long available = _data.Length - _position;
                if (available <= 0) return 0;
                int toCopy = (int)Math.Min(count, available);
                Array.Copy(_data, _position, buffer, offset, toCopy);
                _position += toCopy;
                return toCopy;
            }
        }

        private static int FindSampleOffsetFromArrays(
            float[] origSamples,
            float[] encSamples,
            int channels,
            int sampleRate,
            int bitrate)
        {
            Log($"🔍 FindSampleOffsetFromArrays: channels={channels}, sampleRate={sampleRate}, bitrate={bitrate}");
            Log($"📊 Original samples: {origSamples.Length}, Encoded samples: {encSamples.Length}");

            int totalFramesOrig = origSamples.Length / channels;
            int totalFramesEnc = encSamples.Length / channels;
            Log($"📊 Original frames: {totalFramesOrig}, Encoded frames: {totalFramesEnc}");

            float[] monoOrig = new float[totalFramesOrig];
            float[] monoEnc = new float[totalFramesEnc];
            for (int f = 0; f < totalFramesOrig; f++)
            {
                float sum = 0;
                for (int c = 0; c < channels; c++)
                    sum += origSamples[f * channels + c];
                monoOrig[f] = sum / channels;
            }
            for (int f = 0; f < totalFramesEnc; f++)
            {
                float sum = 0;
                for (int c = 0; c < channels; c++)
                    sum += encSamples[f * channels + c];
                monoEnc[f] = sum / channels;
            }
            Log($"✅ Converted to mono: {monoOrig.Length} samples");

            int filterSize = 4;
            if (bitrate is > 0 and <= 56) filterSize = 16;
            else if (bitrate is > 0 and <= 64) filterSize = 4;
            Log($"🔧 Initial filter size: {filterSize}");

            float highFreqRatio = 0;
            float totalEnergy = 0, highFreqEnergy = 0;
            for (int i = 1; i < monoOrig.Length; i++)
            {
                float diff = monoOrig[i] - monoOrig[i - 1];
                highFreqEnergy += diff * diff;
                totalEnergy += monoOrig[i] * monoOrig[i];
            }
            highFreqRatio = totalEnergy > 0 ? highFreqEnergy / totalEnergy : 0;
            Log($"📊 High frequency ratio: {highFreqRatio:F4}");
            if (highFreqRatio < 0.005)
            {
                filterSize = Math.Max(filterSize, 16);
                Log($"🔧 Adjusted filter size to {filterSize} (low HF ratio)");
            }

            // Multi-window voting: analyze multiple sections of the file
            int windowDurationSec = 2;
            int windowSizeFrames = windowDurationSec * sampleRate;
            int minWindowSizeFrames = sampleRate; // Minimum 1 second

            // CRITICAL FIX: Adapt window size to available data
            if (monoOrig.Length < windowSizeFrames * 2)
            {
                // File too short for multi-window with 2s windows
                // Reduce window size to fit at least 3 windows
                windowSizeFrames = monoOrig.Length / 3;
                Log($"⚠️ File too short for 2s windows, adapted to {windowSizeFrames} frames ({windowSizeFrames / (double)sampleRate:F2}s)");
            }

            if (windowSizeFrames < minWindowSizeFrames)
            {
                // Still too short, use single window
                windowSizeFrames = monoOrig.Length;
                Log($"⚠️ Using single window of {windowSizeFrames} frames ({windowSizeFrames / (double)sampleRate:F2}s)");
            }

            // Determine window positions (0%, 25%, 50%, 75%, 100% of file)
            var windowPositions = new List<int>();
            int numWindows = 5;

            if (windowSizeFrames >= monoOrig.Length)
            {
                // Only one window possible
                windowPositions.Add(0);
                numWindows = 1;
            }
            else
            {
                for (int i = 0; i < numWindows; i++)
                {
                    int position = (int)(monoOrig.Length * i / (double)(numWindows - 1));
                    int maxPosition = monoOrig.Length - windowSizeFrames;
                    position = Math.Clamp(position - windowSizeFrames / 2, 0, maxPosition);
                    if (position >= 0 && position + windowSizeFrames <= monoOrig.Length)
                    {
                        windowPositions.Add(position);
                    }
                }
            }

            Log($"🪟 Multi-window voting: {windowPositions.Count} windows, size={windowSizeFrames} frames ({windowSizeFrames / (double)sampleRate:F2}s)");

            var allCandidates = new Dictionary<int, (int votes, double totalCorr)>();
            int searchWindow = 12000;

            foreach (int windowStart in windowPositions)
            {
                int windowEnd = Math.Min(windowStart + windowSizeFrames, monoOrig.Length);
                int actualWindowSize = windowEnd - windowStart;

                float[] windowOrig = new float[actualWindowSize];
                float[] windowEnc = new float[actualWindowSize];
                Array.Copy(monoOrig, windowStart, windowOrig, 0, actualWindowSize);
                Array.Copy(monoEnc, windowStart, windowEnc, 0, actualWindowSize);

                if (filterSize > 1)
                {
                    float[] filteredWindowOrig = new float[actualWindowSize];
                    float[] filteredWindowEnc = new float[actualWindowSize];
                    for (int i = 0; i < actualWindowSize; i++)
                    {
                        float sumO = 0, sumE = 0;
                        int count = 0;
                        for (int j = Math.Max(0, i - filterSize / 2); j < Math.Min(actualWindowSize, i + filterSize / 2); j++)
                        {
                            sumO += windowOrig[j];
                            sumE += windowEnc[j];
                            count++;
                        }
                        filteredWindowOrig[i] = sumO / count;
                        filteredWindowEnc[i] = sumE / count;
                    }
                    windowOrig = filteredWindowOrig;
                    windowEnc = filteredWindowEnc;
                }

                float meanOrig = windowOrig.Average();
                float meanEnc = windowEnc.Average();
                for (int i = 0; i < actualWindowSize; i++)
                {
                    windowOrig[i] -= meanOrig;
                    windowEnc[i] -= meanEnc;
                }

                double energyOrig = 0, energyEnc = 0;
                for (int i = 0; i < actualWindowSize; i++)
                {
                    energyOrig += windowOrig[i] * windowOrig[i];
                    energyEnc += windowEnc[i] * windowEnc[i];
                }
                double normFactor = Math.Sqrt(energyOrig * energyEnc);

                if (normFactor < 1e-10)
                {
                    Log($"⚠️ Window at {windowStart} has too low energy, skipping");
                    continue;
                }

                int fftSize = 1;
                while (fftSize < actualWindowSize * 2) fftSize <<= 1;

                Complex[] fftOrig = new Complex[fftSize];
                Complex[] fftEncBuf = new Complex[fftSize];
                for (int i = 0; i < actualWindowSize; i++)
                {
                    fftOrig[i] = new Complex(windowOrig[i], 0);
                    fftEncBuf[i] = new Complex(windowEnc[i], 0);
                }

                Fourier.Forward(fftOrig);
                Fourier.Forward(fftEncBuf);

                for (int i = 0; i < fftSize; i++)
                    fftOrig[i] = Complex.Conjugate(fftOrig[i]) * fftEncBuf[i];

                Fourier.Inverse(fftOrig);

                var windowPeaks = new List<(int offset, double corr)>();
                for (int offset = -searchWindow; offset < searchWindow; offset++)
                {
                    int idx = offset >= 0 ? offset : fftSize + offset;
                    int idxPrev = offset > -searchWindow ? (offset - 1 >= 0 ? offset - 1 : fftSize + offset - 1) : -1;
                    int idxNext = offset < searchWindow - 1 ? (offset + 1 >= 0 ? offset + 1 : fftSize + offset + 1) : -1;

                    if (idxPrev < 0 || idxPrev >= fftSize || idxNext < 0 || idxNext >= fftSize) continue;

                    double corr = fftOrig[idx].Real / normFactor;
                    double corrPrev = fftOrig[idxPrev].Real / normFactor;
                    double corrNext = fftOrig[idxNext].Real / normFactor;

                    if (corr > corrPrev && corr > corrNext && corr > 0.001)
                    {
                        windowPeaks.Add((offset, corr));
                    }
                }

                windowPeaks.Sort((a, b) => b.corr.CompareTo(a.corr));
                var topPeaks = windowPeaks.Take(3).ToList();

                Log($"🪟 Window at {windowStart / (double)sampleRate:F2}s: {topPeaks.Count} peaks");
                foreach (var (offset, corr) in topPeaks)
                {
                    Log($"  📌 offset={offset} ({offset / (double)sampleRate * 1000:F2} ms), corr={corr:F6}");

                    if (!allCandidates.ContainsKey(offset))
                        allCandidates[offset] = (0, 0);

                    var current = allCandidates[offset];
                    allCandidates[offset] = (current.votes + 1, current.totalCorr + corr);
                }
            }

            if (allCandidates.Count == 0)
            {
                Log($"⚠️ No candidates found in any window, returning 0");
                return 0;
            }

            var sortedCandidates = allCandidates
                .OrderByDescending(kv => kv.Value.votes)
                .ThenByDescending(kv => kv.Value.totalCorr / kv.Value.votes)
                .ToList();

            Log($"🗳️ Voting results:");
            foreach (var (offset, (votes, totalCorr)) in sortedCandidates.Take(10))
            {
                double avgCorr = totalCorr / votes;
                Log($"  📊 offset={offset} ({offset / (double)sampleRate * 1000:F2} ms): {votes} votes, avgCorr={avgCorr:F6}");
            }

            var winner = sortedCandidates[0];
            int bestFrameOffset = winner.Key;
            double avgCorrelation = winner.Value.totalCorr / winner.Value.votes;

            double consensusThreshold = windowPositions.Count * 0.5;
            if (winner.Value.votes < consensusThreshold)
            {
                Log($"⚠️ Weak consensus: winner has {winner.Value.votes}/{windowPositions.Count} votes (< {consensusThreshold:F1})");
            }

            int sampleOffset = bestFrameOffset * channels;
            Log($"✅ OFFLINE DELAY (multi-window): {sampleOffset} samples, {sampleOffset / (double)channels / sampleRate * 1000:F2} ms, votes={winner.Value.votes}, avgCorr={avgCorrelation:F4}");
            return sampleOffset;
        }

        private int CalculateCodecDelay(string originalPath, string encodedPath, int bitrate)
        {
            Log($"🔍 CalculateCodecDelay: original={Path.GetFileName(originalPath)}, encoded={Path.GetFileName(encodedPath)}, bitrate={bitrate}");
            ISampleProvider? origProvider = null;
            WaveFormat? origFormat = null;
            IDisposable? origDisposable = null;
            try
            {
                string ext = Path.GetExtension(originalPath).ToLower();
                Log($"📄 Original file extension: {ext}");
                if (ext == ".wav")
                {
                    var wav = new WaveFileReader(originalPath);
                    origDisposable = wav;
                    origFormat = wav.WaveFormat;
                    origProvider = (origFormat.BitsPerSample == 24)
                        ? new Wave24ToFloatProvider(wav)
                        : wav.ToSampleProvider();
                }
                else if (ext == ".flac")
                {
                    var audio = new AudioFileReader(originalPath);
                    origDisposable = audio;
                    origFormat = audio.WaveFormat;
                    origProvider = audio;
                }
                else
                {
                    Log($"⚠️ Unsupported format: {ext}, returning 0");
                    return 0;
                }
                if (origFormat == null || origProvider == null)
                {
                    Log($"⚠️ Failed to get format or provider for original file");
                    return 0;
                }
                Log($"📌 Opening encoded MP3 file: {encodedPath}");
                using var mpegReader = new MediaFoundationReader(encodedPath);
                var mp3Provider = mpegReader.ToSampleProvider();
                var mp3Format = mpegReader.WaveFormat;
                ISampleProvider finalMp3 = mp3Provider;
                if (mp3Format.Channels == 1 && origFormat.Channels == 2)
                    finalMp3 = new MonoToStereoSampleProvider(finalMp3);
                if (mp3Format.SampleRate != origFormat.SampleRate)
                    finalMp3 = new WdlResamplingSampleProvider(finalMp3, origFormat.SampleRate);
                int sampleRate = origFormat.SampleRate;
                int channels = origFormat.Channels;

                // Read up to 10 seconds of data for multi-window voting
                int searchDurationSec = 10;
                int searchSamples = searchDurationSec * sampleRate * channels;
                float[] origBuffer = new float[searchSamples];
                float[] encBuffer = new float[searchSamples];
                int readOrig = origProvider.Read(origBuffer, 0, searchSamples);
                int readEnc = finalMp3.Read(encBuffer, 0, searchSamples);
                int maxRead = Math.Min(readOrig, readEnc);

                if (maxRead < 1000)
                {
                    Log($"⚠️ Too few samples read ({maxRead}), returning 0");
                    return 0;
                }

                Log($"📊 Read {maxRead} samples ({maxRead / (double)channels / sampleRate:F2}s) for delay calculation");

                // Pass all read data to FindSampleOffsetFromArrays for multi-window voting
                float[] origWindow = new float[maxRead];
                float[] encWindow = new float[maxRead];
                Array.Copy(origBuffer, origWindow, maxRead);
                Array.Copy(encBuffer, encWindow, maxRead);

                int delay = FindSampleOffsetFromArrays(origWindow, encWindow, channels, sampleRate, bitrate);

                if (delay < 0)
                {
                    Log($"⚠️ Negative delay {delay}, searching only positive delays...");
                    int monoSize = maxRead / channels;
                    float[] monoOrig = new float[monoSize];
                    float[] monoEnc = new float[monoSize];
                    for (int f = 0; f < monoSize; f++)
                    {
                        float sumOrig = 0, sumEnc = 0;
                        int baseIdx = f * channels;
                        for (int c = 0; c < channels; c++)
                        {
                            sumOrig += origWindow[baseIdx + c];
                            sumEnc += encWindow[baseIdx + c];
                        }
                        monoOrig[f] = sumOrig / channels;
                        monoEnc[f] = sumEnc / channels;
                    }
                    int maxDelay = Math.Min(12000, monoSize / 2);
                    int bestDelay = 0;
                    double bestCorr = -1;
                    for (int d = 0; d < maxDelay; d += 5)
                    {
                        double corr = 0;
                        double normOrig = 0, normEnc = 0;
                        int len = monoSize - d;
                        for (int i = 0; i < len; i++)
                        {
                            corr += monoOrig[i] * monoEnc[i + d];
                            normOrig += monoOrig[i] * monoOrig[i];
                            normEnc += monoEnc[i + d] * monoEnc[i + d];
                        }
                        if (normOrig > 0 && normEnc > 0)
                        {
                            corr /= Math.Sqrt(normOrig * normEnc);
                            if (corr > bestCorr)
                            {
                                bestCorr = corr;
                                bestDelay = d;
                            }
                        }
                    }
                    delay = bestDelay * channels;
                    Log($"✅ Found positive delay: {delay} samples (corr={bestCorr:F4})");
                }
                Log($"✅ Final delay: {delay} samples ({delay / (double)channels / sampleRate * 1000:F2} ms)");
                return delay;
            }
            catch (Exception ex)
            {
                Log($"❌ Error in CalculateCodecDelay: {ex.Message}");
                return 0;
            }
            finally
            {
                origDisposable?.Dispose();
            }
        }
        private double ConvertWavBytesToMilliseconds(long wavBytes)
        {
            WaveFormat? format = _originalFormat;
            if (format == null)
            {
                Log($"⚠️ ConvertWavBytesToMilliseconds: no format available");
                return 0;
            }
            int bytesPerSecond = format.AverageBytesPerSecond;
            if (bytesPerSecond <= 0)
            {
                Log($"⚠️ ConvertWavBytesToMilliseconds: invalid bytesPerSecond={bytesPerSecond}");
                return 0;
            }
            double ms = (wavBytes / (double)bytesPerSecond) * 1000;
            Log($"📊 ConvertWavBytesToMilliseconds: {wavBytes} bytes = {ms:F2} ms");
            return ms;
        }

        // Encoders
        private void ListViewEncoders_DragEnter(object? sender, DragEventArgs e)
        {
            Log($"🖱️ DragEnter on encoders list");
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) != true)
            {
                e.Effect = DragDropEffects.None;
                Log($"⚠️ No file drop data");
                return;
            }
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            {
                bool allValid = files.All(f =>
                    Directory.Exists(f) ||
                    Path.GetExtension(f).Equals(".exe", StringComparison.OrdinalIgnoreCase));
                e.Effect = allValid ? DragDropEffects.Copy : DragDropEffects.None;
                Log($"📊 {files.Length} items, all valid: {allValid}");
            }
            else
            {
                e.Effect = DragDropEffects.None;
                Log($"⚠️ Invalid file list");
            }
        }
        private void ListViewEncoders_DragDrop(object? sender, DragEventArgs e)
        {
            Log($"🖱️ DragDrop on encoders list");
            if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files)
            {
                Log($"⚠️ No files in drag drop");
                return;
            }
            Log($"📊 Processing {files.Length} items");
            foreach (string path in files)
            {
                Log($"📄 Processing: {path}");
                if (Directory.Exists(path))
                {
                    try
                    {
                        var exeFiles = Directory.GetFiles(path, "*.exe", SearchOption.AllDirectories);
                        Log($"📁 Found {exeFiles.Length} .exe files in folder: {path}");
                        foreach (string exePath in exeFiles)
                        {
                            AddEncoderToList(exePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"⚠️ Failed to scan folder {path}: {ex.Message}");
                    }
                }
                else if (File.Exists(path))
                {
                    AddEncoderToList(path);
                }
                else
                {
                    Log($"⚠️ Path does not exist: {path}");
                }
            }
        }
        private void AddEncoderToList(string encoderPath)
        {
            Log($"📌 AddEncoderToList: {encoderPath}");
            if (!Path.GetExtension(encoderPath).Equals(".exe", StringComparison.OrdinalIgnoreCase))
            {
                Log($"⚠️ Not an .exe file, skipping");
                return;
            }
            bool exists = listViewEncoders.Items.Cast<ListViewItem>().Any(item => string.Equals(item.Tag as string, encoderPath, StringComparison.OrdinalIgnoreCase));
            if (exists)
            {
                Log($"ℹ️ Encoder already in list, skipping");
                return;
            }

            (string? name, string? version) = GetEncoderInfo(encoderPath);
            Log($"📊 Encoder info: name={name}, version={version}");
            ListViewItem item = new(name) { Tag = encoderPath, Checked = false };
            _ = item.SubItems.Add(version);
            _ = item.SubItems.Add(Path.GetDirectoryName(encoderPath) ?? string.Empty);
            _ = listViewEncoders.Items.Add(item);
            Log($"✅ Encoder added to list");

            if (listViewEncoders.Items.Count == 1)
            {
                item.Checked = true;
                ListViewEncoders_ItemChecked(this, new ItemCheckedEventArgs(item));
                Log($"✅ First encoder auto-selected (list was empty)");
            }
        }
        private void ListViewEncoders_ItemChecked(object? sender, ItemCheckedEventArgs e)
        {
            Log($"📌 Encoder item checked: {e.Item.Text}, checked={e.Item.Checked}");

            if (!e.Item.Checked)
            {
                bool hasOtherChecked = listViewEncoders.Items.Cast<ListViewItem>().Any(item => item != e.Item && item.Checked);

                if (!hasOtherChecked)
                {
                    e.Item.Checked = true;
                    Log($"⚠️ Preventing uncheck: only one encoder, keeping checked");
                    return;
                }

                Log($"ℹ️ Skipping uncheck event for {e.Item.Text} (programmatic)");
                return;
            }

            string? newPath = e.Item.Tag as string;
            if (_selectedEncoderPath == newPath)
            {
                Log($"ℹ️ Already selected: {newPath}, ignoring");
                return;
            }

            foreach (ListViewItem item in listViewEncoders.Items)
                if (item != e.Item) item.Checked = false;

            _selectedEncoderPath = newPath;
            _needsReencoding = true;
            _encodedFilePath = null;
            _currentCacheKey = null;
            Log($"✅ Encoder selected: {_selectedEncoderPath}");
            UpdateEncodingUI();

            if (_waveOut != null && _playgroundMixer != null &&
                _waveOut.PlaybackState != PlaybackState.Stopped)
            {
                ScheduleSeamlessSwap();
            }
        }
        private (string Name, string Version) GetEncoderInfo(string encoderPath)
        {
            Log($"🔍 GetEncoderInfo for: {encoderPath}");
            try
            {
                string name = Path.GetFileName(encoderPath);
                string version = "Unknown";

                try
                {
                    Log($"🔄 Running --version: {encoderPath}");
                    using Process process = new()
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = encoderPath,
                            Arguments = "--version",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        },
                        EnableRaisingEvents = true
                    };
                    _ = process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    if (!process.WaitForExit(2000))
                    {
                        Log($"⚠️ Process timeout, killing");
                        process.Kill(true);
                        process.WaitForExit();
                    }
                    string[] lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length > 0)
                    {
                        version = lines[0].Trim();
                        if (version.Contains("LAME", StringComparison.OrdinalIgnoreCase))
                        {
                            Log($"✅ Detected LAME MP3 encoder: {version}");
                        }
                        else if (version.Contains("Ogg", StringComparison.OrdinalIgnoreCase))
                        {
                            Log($"✅ Detected Ogg encoder: {version}");
                        }
                        else
                        {
                            Log($"ℹ️ Unknown encoder family: {version}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"⚠️ --version failed: {ex.Message}, trying FileVersionInfo");
                    try
                    {
                        FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(encoderPath);
                        version = versionInfo.FileVersion ?? "Unknown";
                        if (!string.IsNullOrEmpty(versionInfo.FileDescription) && versionInfo.FileDescription.Contains("LAME", StringComparison.OrdinalIgnoreCase))
                        {
                            Log($"✅ Detected LAME MP3 encoder via FileVersionInfo");
                        }
                    }
                    catch (Exception ex2)
                    {
                        Log($"⚠️ FileVersionInfo failed: {ex2.Message}");
                    }
                }
                Log($"📊 Result: name={name}, version={version}");
                return (name, version);
            }
            catch (Exception ex)
            {
                Log($"❌ GetEncoderInfo failed: {ex.Message}");
                return (Path.GetFileName(encoderPath), "Unknown");
            }
        }
        private void ListViewEncoders_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && listViewEncoders.SelectedItems.Count > 0)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                List<ListViewItem> itemsToDelete = [.. listViewEncoders.SelectedItems.Cast<ListViewItem>()];
                bool deletingActiveEncoder = itemsToDelete.Any(item => item.Tag?.ToString() == _selectedEncoderPath);

                if (deletingActiveEncoder)
                {
                    StopDualPlayback();
                    _selectedEncoderPath = null;
                    _encodedFilePath = null;
                    _needsReencoding = true;
                }

                foreach (ListViewItem item in itemsToDelete)
                {
                    listViewEncoders.Items.Remove(item);
                }

                if (listViewEncoders.Items.Count > 0)
                {
                    if (deletingActiveEncoder || _selectedEncoderPath == null)
                    {
                        listViewEncoders.Items[0].Checked = true;
                        ListViewEncoders_ItemChecked(this, new ItemCheckedEventArgs(listViewEncoders.Items[0]));
                    }
                }
                else
                {
                    _selectedEncoderPath = null;
                    _encodedFilePath = null;
                    _needsReencoding = true;
                    UpdateEncodingUI();
                }

                ClearCache();
                Log($"🗑️ Deleted {itemsToDelete.Count} encoders" + (deletingActiveEncoder ? " (active)" : ""));
            }
        }

        // AudioFiles
        private void ListViewAudioFiles_DragEnter(object? sender, DragEventArgs e)
        {
            Log($"🖱️ DragEnter on audio files list");
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) != true)
            {
                e.Effect = DragDropEffects.None;
                Log($"⚠️ No file drop data");
                return;
            }
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            {
                bool allValid = files.All(f =>
                    Directory.Exists(f) ||
                    Path.GetExtension(f).Equals(".wav", StringComparison.OrdinalIgnoreCase) ||
                    Path.GetExtension(f).Equals(".flac", StringComparison.OrdinalIgnoreCase));
                e.Effect = allValid ? DragDropEffects.Copy : DragDropEffects.None;
                Log($"📊 {files.Length} items, all valid: {allValid}");
            }
            else
            {
                e.Effect = DragDropEffects.None;
                Log($"⚠️ Invalid file list");
            }
        }
        private void ListViewAudioFiles_DragDrop(object? sender, DragEventArgs e)
        {
            Log($"🖱️ DragDrop on audio files list");
            if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files)
            {
                Log($"⚠️ No files in drag drop");
                return;
            }
            Log($"📊 Processing {files.Length} items");
            foreach (string path in files)
            {
                Log($"📄 Processing: {path}");
                if (Directory.Exists(path))
                {
                    try
                    {
                        var audioFiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                            .Where(f =>
                                Path.GetExtension(f).Equals(".wav", StringComparison.OrdinalIgnoreCase) ||
                                Path.GetExtension(f).Equals(".flac", StringComparison.OrdinalIgnoreCase))
                            .ToArray();
                        Log($"📁 Found {audioFiles.Length} audio files in folder: {path}");
                        foreach (string audioPath in audioFiles)
                        {
                            AddAudioFileFileToList(audioPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"⚠️ Failed to scan folder {path}: {ex.Message}");
                    }
                }
                else if (File.Exists(path))
                {
                    string ext = Path.GetExtension(path);
                    if (ext.Equals(".wav", StringComparison.OrdinalIgnoreCase) ||
                        ext.Equals(".flac", StringComparison.OrdinalIgnoreCase))
                    {
                        AddAudioFileFileToList(path);
                    }
                    else
                    {
                        Log($"⚠️ Unsupported file type: {path}");
                    }
                }
                else
                {
                    Log($"⚠️ Path does not exist: {path}");
                }
            }
        }
        private void AddAudioFileFileToList(string audioFileInputPath)
        {
            Log($"📌 AddAudioFileToList: {audioFileInputPath}");
            string ext = Path.GetExtension(audioFileInputPath);
            if (!ext.Equals(".wav", StringComparison.OrdinalIgnoreCase) &&
                !ext.Equals(".flac", StringComparison.OrdinalIgnoreCase))
            {
                Log($"⚠️ Unsupported extension: {ext}, skipping");
                return;
            }

            bool exists = listViewAudioFiles.Items.Cast<ListViewItem>()
                .Any(item => string.Equals(item.Tag as string, audioFileInputPath, StringComparison.OrdinalIgnoreCase));
            if (exists)
            {
                Log($"ℹ️ Audio file already in list, skipping");
                return;
            }

            ListViewItem item = new(Path.GetFileName(audioFileInputPath)) { Tag = audioFileInputPath, Checked = false };

            try
            {
                int channels = 0, bitsPerSample = 0, sampleRate = 0;
                double durationSec = 0;

                if (ext.Equals(".wav", StringComparison.OrdinalIgnoreCase))
                {
                    using var wavReader = new WaveFileReader(audioFileInputPath);
                    channels = wavReader.WaveFormat.Channels;
                    bitsPerSample = wavReader.WaveFormat.BitsPerSample;
                    sampleRate = wavReader.WaveFormat.SampleRate;
                    durationSec = wavReader.TotalTime.TotalSeconds;
                    Log($"📊 WAV: {channels}ch, {bitsPerSample}bit, {sampleRate}Hz, {durationSec:F1}s");
                }
                else if (ext.Equals(".flac", StringComparison.OrdinalIgnoreCase))
                {
                    var flacInfo = ReadFlacStreamInfo(audioFileInputPath);
                    channels = flacInfo.Channels;
                    bitsPerSample = flacInfo.BitsPerSample;
                    sampleRate = flacInfo.SampleRate;
                    durationSec = flacInfo.TotalSamples > 0 && flacInfo.SampleRate > 0
                        ? (double)flacInfo.TotalSamples / flacInfo.SampleRate
                        : 0;
                    Log($"📊 FLAC: {channels}ch, {bitsPerSample}bit, {sampleRate}Hz, {durationSec:F1}s");
                }

                _ = item.SubItems.Add(channels.ToString());
                _ = item.SubItems.Add(bitsPerSample.ToString());
                _ = item.SubItems.Add($"{sampleRate / 1000.0:0.0} kHz");
                _ = item.SubItems.Add($"{durationSec:F1}s");
            }
            catch (Exception ex)
            {
                Log($"⚠️ Failed to read audio info for {audioFileInputPath}: {ex.Message}");
                _ = item.SubItems.Add("?"); _ = item.SubItems.Add("?");
                _ = item.SubItems.Add("?"); _ = item.SubItems.Add("?");
            }

            _ = item.SubItems.Add(Path.GetDirectoryName(audioFileInputPath) ?? string.Empty);
            _ = listViewAudioFiles.Items.Add(item);
            Log($"✅ Audio file added to list");

            if (listViewAudioFiles.Items.Count == 1)
            {
                item.Checked = true;
                ListViewAudioFiles_ItemChecked(this, new ItemCheckedEventArgs(item));
                Log($"✅ First audio file auto-selected (list was empty)");
            }
        }
        private static (int Channels, int BitsPerSample, int SampleRate, long TotalSamples) ReadFlacStreamInfo(string filePath)
        {
            Log($"🔍 Reading FLAC STREAMINFO from: {filePath}");
            using var fs = File.OpenRead(filePath);
            using var br = new BinaryReader(fs);

            byte[] signature = br.ReadBytes(4);
            if (signature.Length < 4 || signature[0] != 'f' || signature[1] != 'L' || signature[2] != 'a' || signature[3] != 'C')
            {
                Log($"❌ Invalid FLAC signature");
                throw new InvalidDataException("Not a valid FLAC file");
            }
            Log($"✅ Valid FLAC signature");

            while (true)
            {
                byte headerByte = br.ReadByte();
                bool isLast = (headerByte & 0x80) != 0;
                int blockType = headerByte & 0x7F;
                int blockSize = (br.ReadByte() << 16) | (br.ReadByte() << 8) | br.ReadByte();
                Log($"📊 Block type: {blockType}, size: {blockSize}, last: {isLast}");

                if (blockType == 0)
                {
                    _ = br.ReadBytes(10);
                    byte[] infoBytes = br.ReadBytes(8);

                    int sr = (infoBytes[0] << 12) | (infoBytes[1] << 4) | ((infoBytes[2] >> 4) & 0x0F);
                    int ch = ((infoBytes[2] >> 1) & 0x07) + 1;
                    int bps = (((infoBytes[2] & 0x01) << 4) | ((infoBytes[3] >> 4) & 0x0F)) + 1;
                    long totalSamples = ((long)(infoBytes[3] & 0x0F) << 32) |
                                        ((long)infoBytes[4] << 24) |
                                        ((long)infoBytes[5] << 16) |
                                        ((long)infoBytes[6] << 8) |
                                        infoBytes[7];

                    Log($"✅ FLAC info: channels={ch}, bits={bps}, sampleRate={sr}, totalSamples={totalSamples}");
                    return (ch, bps, sr, totalSamples);
                }

                fs.Seek(blockSize, SeekOrigin.Current);
                if (isLast) break;
            }

            Log($"❌ STREAMINFO block not found");
            throw new InvalidDataException("STREAMINFO block not found in FLAC file");
        }
        private void ListViewAudioFiles_ItemChecked(object? sender, ItemCheckedEventArgs e)
        {
            Log($"📌 Audio file item checked: {e.Item.Text}, checked={e.Item.Checked}");

            if (!e.Item.Checked)
            {
                bool hasOtherChecked = listViewAudioFiles.Items.Cast<ListViewItem>().Any(item => item != e.Item && item.Checked);

                if (!hasOtherChecked)
                {
                    e.Item.Checked = true;
                    Log($"⚠️ Preventing uncheck: only one audio file, keeping checked");
                    return;
                }

                Log($"ℹ️ Skipping uncheck event for {e.Item.Text} (programmatic)");
                return;
            }

            string? newPath = e.Item.Tag as string;
            if (_originalFilePath == newPath)
            {
                Log($"ℹ️ Already selected: {newPath}, ignoring");
                return;
            }

            foreach (ListViewItem item in listViewAudioFiles.Items)
                if (item != e.Item) item.Checked = false;

            if (string.IsNullOrEmpty(newPath)) return;

            _originalFilePath = newPath;
            _currentPlaybackPosition = 0;
            trackBarSeek.Value = 0;
            _encodedFilePath = null;
            _needsReencoding = true;
            _encodingStatus = EncodingStatus.Idle;
            Log($"✅ Audio file selected: {_originalFilePath}");
            UpdateEncodingUI();

            bool wasPlaying = _waveOut?.PlaybackState == PlaybackState.Playing;
            bool wasPaused = _waveOut?.PlaybackState == PlaybackState.Paused;

            StopDualPlayback();

            if (wasPlaying || wasPaused)
            {
                Log($"▶️ Auto-starting playback for new file (was {(wasPlaying ? "playing" : "paused")})");
                if (string.IsNullOrEmpty(_selectedEncoderPath))
                {
                    Log($"⚠️ No encoder selected, cannot auto-start");
                    return;
                }
                StartEncodingForPlay(_originalFilePath);
            }
        }
        private void ListViewAudioFiles_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && listViewAudioFiles.SelectedItems.Count > 0)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                List<ListViewItem> itemsToDelete = [.. listViewAudioFiles.SelectedItems.Cast<ListViewItem>()];
                bool deletingActiveFile = itemsToDelete.Any(item => item.Tag?.ToString() == _originalFilePath);

                if (deletingActiveFile)
                {
                    StopDualPlayback();
                    _originalFilePath = null;
                    _encodedFilePath = null;
                    _needsReencoding = true;
                }

                foreach (ListViewItem item in itemsToDelete)
                {
                    listViewAudioFiles.Items.Remove(item);
                }

                if (listViewAudioFiles.Items.Count > 0)
                {
                    if (deletingActiveFile || _originalFilePath == null)
                    {
                        listViewAudioFiles.Items[0].Checked = true;
                        ListViewAudioFiles_ItemChecked(this, new ItemCheckedEventArgs(listViewAudioFiles.Items[0]));
                    }
                }
                else
                {
                    _originalFilePath = null;
                    _encodedFilePath = null;
                    _needsReencoding = true;
                    UpdateEncodingUI();
                }

                ClearCache();
                Log($"🗑️ Deleted {itemsToDelete.Count} audio files" + (deletingActiveFile ? " (active)" : ""));
            }
        }

        private void ButtonClear_Click(object? sender, EventArgs e)
        {
            if (sender is not Button clickedButton) return;

            Log($"🗑️ Clear button clicked: {clickedButton.Name}");
            StopDualPlayback();

            if (clickedButton.Name == "buttonClearAudioFiles")
            {
                Log($"🗑️ Clearing audio files");
                listViewAudioFiles.Items.Clear();
                _originalFilePath = null;
                _encodedFilePath = null;
                ClearCache();
                Log("🗑️ Audio files and cache cleared");
            }
            else if (clickedButton.Name == "buttonClearEncoders")
            {
                Log($"🗑️ Clearing encoders");
                listViewEncoders.Items.Clear();
                _selectedEncoderPath = null;
                _encodedFilePath = null;
                _encodingStatus = EncodingStatus.Idle;
                ClearCache();
                UpdateEncodingUI();
                Log("🗑️ Encoders and cache cleared");
            }
        }

        private void StartEncodingForPlay(string originalFilePath)
        {
            Log($"▶️ StartEncodingForPlay: {Path.GetFileName(originalFilePath)}");
            if (string.IsNullOrEmpty(_selectedEncoderPath))
            {
                Log($"⚠️ No encoder selected");
                MessageBox.Show("Please select an encoder first!", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string cacheKey = GenerateCacheFileNameAndCacheKey(originalFilePath, _selectedEncoderPath);
            _currentCacheKey = cacheKey;
            Log($"🔑 Cache key: {cacheKey}");

            if (TryGetFromCache(cacheKey, out string? cachedFile, out int cachedDelay))
            {
                Log($"📦 Using cached MP3: {Path.GetFileName(cachedFile)} (delay: {cachedDelay} samples)");
                _encodedFilePath = cachedFile;
                _needsReencoding = false;
                _encodingStatus = EncodingStatus.Completed;
                UpdateEncodingUI();

                Log($"▶️ Starting playback from cache");
                InitializePlayback();
                PlayDual();
                UpdateEncoderSettingsReturnedByMILabel();
                return;
            }

            Log($"🔄 Encoding new variant: {cacheKey[..Math.Min(12, cacheKey.Length)]}...");
            _encodingCts?.Cancel();
            _encodingCts?.Dispose();
            _encodingCts = new CancellationTokenSource();
            CancellationToken token = _encodingCts.Token;

            _encodingStatus = EncodingStatus.Queued;
            _pendingPlayAfterEncode = true;
            Log($"📊 Encoding queued, pending play: {_pendingPlayAfterEncode}");
            UpdateEncodingUI();

            _currentEncodingTask = Task.Run(async () =>
            {
                Log($"🚀 Encoding task started");
                try
                {
                    await EncodeFileAsync(originalFilePath, cacheKey, token);
                }
                catch (OperationCanceledException)
                {
                    Log($"⏹️ Encoding canceled");
                    Invoke(() =>
                    {
                        _encodingStatus = EncodingStatus.Canceled;
                        _pendingPlayAfterEncode = false;
                        UpdateEncodingUI();
                    });
                }
                catch (Exception ex)
                {
                    Log($"❌ Encoding failed: {ex.Message}");
                    Log($"❌ StackTrace: {ex.StackTrace}");
                    Invoke(() =>
                    {
                        _encodingStatus = EncodingStatus.Error;
                        _pendingPlayAfterEncode = false;
                        UpdateEncodingUI();
                        _ = MessageBox.Show($"Encoding failed: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                }
                finally
                {
                    Log($"🔚 Encoding task finished");
                }
            });
        }

        private async Task EncodeFileAsync(string inputPath, string cacheKey, CancellationToken ct)
        {
            Log($"📝 EncodeFileAsync started: input={Path.GetFileName(inputPath)}, cacheKey={cacheKey}");
            if (string.IsNullOrEmpty(_selectedEncoderPath))
                throw new InvalidOperationException("Encoder is not selected.");

            string encoderPath = _selectedEncoderPath;
            Log($"📌 Encoder path: {encoderPath}");

            string tempEncodedFile = Path.Combine(_tempFolder, $"{cacheKey}.mp3");
            Log($"📁 Output file: {tempEncodedFile}");

            if (File.Exists(tempEncodedFile))
            {
                Log($"ℹ️ Output file already exists: {tempEncodedFile}");
                lock (_cacheLock)
                {
                    if (_encodedCache.ContainsKey(cacheKey))
                    {
                        Log($"📦 File already exists and cached: {Path.GetFileName(tempEncodedFile)}");
                        _encodedFilePath = tempEncodedFile;

                        if (_delayCache.TryGetValue(cacheKey, out int cachedDelay))
                        {
                            Log($"📦 Using cached delay: {cachedDelay} samples");
                        }
                        else
                        {
                            try
                            {
                                Log($"🔄 Calculating delay for existing file");
                                int bitrate = 0;
                                using (var mpegReader = new MediaFoundationReader(tempEncodedFile))
                                {
                                    double duration = mpegReader.TotalTime.TotalSeconds;
                                    if (duration > 0)
                                        bitrate = (int)((new FileInfo(tempEncodedFile).Length * 8) / duration / 1000);
                                    Log($"📊 MP3 duration: {duration:F2}s, bitrate: {bitrate} kbps");
                                }
                                int delay = CalculateCodecDelay(inputPath, tempEncodedFile, bitrate);
                                _delayCache[cacheKey] = delay;
                                Log($"🔧 Calculated and cached delay: {delay} samples");
                            }
                            catch (Exception ex)
                            {
                                Log($"⚠️ Failed to calculate delay: {ex.Message}");
                            }
                        }

                        Invoke(() =>
                        {
                            _encodedFilePath = tempEncodedFile;
                            _encodingStatus = EncodingStatus.Completed;
                            _needsReencoding = false;
                            Log($"✅ Encoding completed (existing file)");
                            UpdateEncodingUI();

                            if (_pendingPlayAfterEncode)
                            {
                                _pendingPlayAfterEncode = false;
                                Log($"▶️ Starting playback after encoding");
                                InitializePlayback();
                                PlayDual();
                                UpdateEncoderSettingsReturnedByMILabel();
                            }
                        });
                        return;
                    }
                    else
                    {
                        Log($"⚠️ File exists but not in cache, will be added");
                    }
                }
            }

            string? tempPreEncodeWav = null;
            string lameInputPath = inputPath;

            try
            {
                string ext = Path.GetExtension(inputPath);
                bool needsConversion = false;
                int targetBits = 16;
                Log($"📄 Input extension: {ext}");

                if (ext.Equals(".wav", StringComparison.OrdinalIgnoreCase))
                {
                    using var checkReader = new WaveFileReader(inputPath);
                    int bps = checkReader.WaveFormat.BitsPerSample;
                    var enc = checkReader.WaveFormat.Encoding;
                    Log($"📊 WAV: bits={bps}, encoding={enc}");

                    if (bps == 16 && enc == WaveFormatEncoding.Pcm)
                    {
                        Log("📌 16-bit PCM WAV → LAME directly");
                    }
                    else if (bps == 24 && enc == WaveFormatEncoding.Pcm)
                    {
                        Log("📌 24-bit PCM WAV → LAME directly");
                    }
                    else
                    {
                        needsConversion = true;
                        targetBits = 24;
                        Log($"📌 {bps}-bit {enc} WAV → converting to 24-bit PCM WAV");
                    }
                }
                else if (ext.Equals(".flac", StringComparison.OrdinalIgnoreCase))
                {
                    needsConversion = true;
                    try
                    {
                        var info = ReadFlacStreamInfo(inputPath);
                        targetBits = info.BitsPerSample;
                        Log($"📌 {targetBits}-bit FLAC → converting to {targetBits}-bit PCM WAV");
                    }
                    catch
                    {
                        targetBits = 16;
                        Log("⚠️ Could not read FLAC header, falling back to 16-bit");
                    }
                }
                else
                {
                    needsConversion = true;
                    targetBits = 16;
                    Log($"📌 {ext} → converting to 16-bit PCM WAV");
                }

                if (needsConversion)
                {
                    tempPreEncodeWav = Path.Combine(_tempFolder, $"preencode_{Guid.NewGuid()}.wav");
                    Log($"📁 Pre-encode temp file: {tempPreEncodeWav}");

                    ISampleProvider sourceProvider;
                    int sampleRate, channels;
                    IDisposable? disposableSource = null;

                    if (ext.Equals(".wav", StringComparison.OrdinalIgnoreCase))
                    {
                        Log($"📌 Reading WAV for conversion");
                        var wavReader = new WaveFileReader(inputPath);
                        disposableSource = wavReader;
                        sampleRate = wavReader.WaveFormat.SampleRate;
                        channels = wavReader.WaveFormat.Channels;
                        sourceProvider = wavReader.WaveFormat.BitsPerSample == 24
                            ? new Wave24ToFloatProvider(wavReader)
                            : wavReader.ToSampleProvider();
                        Log($"📊 WAV source: {sampleRate}Hz, {channels}ch");
                    }
                    else
                    {
                        Log($"📌 Reading {ext} for conversion");
                        var audioReader = new AudioFileReader(inputPath);
                        disposableSource = audioReader;
                        sampleRate = audioReader.WaveFormat.SampleRate;
                        channels = audioReader.WaveFormat.Channels;
                        sourceProvider = audioReader;
                        Log($"📊 Source: {sampleRate}Hz, {channels}ch");
                    }

                    IWaveProvider waveWriter;
                    WaveFormat targetFormat;

                    if (targetBits == 24)
                    {
                        targetFormat = new WaveFormat(sampleRate, 24, channels);
                        waveWriter = new SampleToWaveProvider24(sourceProvider);
                        Log($"📌 Target format: 24-bit PCM");
                    }
                    else
                    {
                        targetFormat = new WaveFormat(sampleRate, 16, channels);
                        waveWriter = new SampleToWaveProvider16(sourceProvider);
                        Log($"📌 Target format: 16-bit PCM");
                    }

                    Log($"🔄 Converting to {targetBits}-bit PCM...");
                    using (var writer = new WaveFileWriter(tempPreEncodeWav, targetFormat))
                    {
                        byte[] buf = new byte[targetFormat.AverageBytesPerSecond];
                        int read;
                        long totalBytes = 0;
                        while ((read = waveWriter.Read(buf, 0, buf.Length)) > 0)
                        {
                            ct.ThrowIfCancellationRequested();
                            writer.Write(buf, 0, read);
                            totalBytes += read;
                            if (totalBytes % (targetFormat.AverageBytesPerSecond * 10) < buf.Length)
                            {
                                Log($"📊 Converted {totalBytes / 1024} KB");
                            }
                        }
                        Log($"✅ Pre-encode complete: {totalBytes / 1024} KB written");
                    }

                    disposableSource?.Dispose();
                    lameInputPath = tempPreEncodeWav;
                    Log($"✅ Pre-encode: {new FileInfo(tempPreEncodeWav).Length} bytes ({targetBits}-bit)");
                }

                Log($"📁 Encoding: {lameInputPath} → {Path.GetFileName(tempEncodedFile)}");
                string args = BuildLameArguments(lameInputPath, tempEncodedFile);
                Log($"🔧 LAME arguments: {args}");

                using Process process = new()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = encoderPath,
                        Arguments = args,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true
                    },
                    EnableRaisingEvents = true
                };

                process.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        int progress = ParseProgress(e.Data);
                        if (progress is >= 0 and <= 100)
                        {
                            _ = BeginInvoke(() =>
                            {
                                if (_encodingStatus == EncodingStatus.Queued)
                                {
                                    _encodingStatus = EncodingStatus.Running;
                                    Log($"🔄 Encoding started");
                                }
                                progressBarEncodingProcess.Value = progress;
                                if (progress % 10 == 0 || progress == 100)
                                {
                                    Log($"📊 Encoding progress: {progress}%");
                                }
                                UpdateEncodingUI();
                            });
                        }
                    }
                };

                Invoke(() => { _encodingStatus = EncodingStatus.Running; UpdateEncodingUI(); });
                Log($"🚀 Starting LAME process...");
                _ = process.Start();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync(ct);
                Log($"✅ LAME process exited with code: {process.ExitCode}");

                if (ct.IsCancellationRequested)
                {
                    Log($"⏹️ Cancellation requested, deleting output file");
                    try { File.Delete(tempEncodedFile); } catch { }
                    throw new OperationCanceledException();
                }

                if (process.ExitCode != 0)
                {
                    Log($"❌ LAME failed with code {process.ExitCode}");
                    try { File.Delete(tempEncodedFile); } catch { }
                    throw new Exception($"LAME failed with code {process.ExitCode}");
                }

                if (!File.Exists(tempEncodedFile) || new FileInfo(tempEncodedFile).Length == 0)
                {
                    Log($"❌ Encoded file is empty or missing");
                    throw new Exception("Encoded file is empty");
                }

                long fileSize = new FileInfo(tempEncodedFile).Length;
                Log($"✅ MP3 created: {fileSize} bytes ({fileSize / 1024} KB)");

                int bitrate = 0;
                using (var mpegReader = new MediaFoundationReader(tempEncodedFile))
                {
                    double duration = mpegReader.TotalTime.TotalSeconds;
                    if (duration > 0)
                        bitrate = (int)((new FileInfo(tempEncodedFile).Length * 8) / duration / 1000);
                    Log($"📊 MP3 duration: {duration:F2}s, bitrate: {bitrate} kbps");
                }

                Log($"🔄 Calculating codec delay...");
                int delay = CalculateCodecDelay(inputPath, tempEncodedFile, bitrate);

                AddToCache(cacheKey, tempEncodedFile, delay);

                string encoderInfo = GetEncoderSettingsFromFile(tempEncodedFile);
                Log($"📊 MediaInfo: {encoderInfo}");

                Invoke(() =>
                {
                    _encodedFilePath = tempEncodedFile;
                    _encodingStatus = EncodingStatus.Completed;
                    _needsReencoding = false;
                    Log($"✅ Encoding completed successfully");
                    UpdateEncodingUI();

                    if (_pendingPlayAfterEncode)
                    {
                        _pendingPlayAfterEncode = false;
                        Log($"▶️ Starting playback after encoding");
                        InitializePlayback();
                        PlayDual();
                        UpdateEncoderSettingsReturnedByMILabel();
                    }
                });
            }
            catch (OperationCanceledException)
            {
                Log($"⏹️ Encoding was canceled");
                throw;
            }
            catch (Exception ex)
            {
                Log($"❌ EncodeFileAsync error: {ex.Message}");
                Log($"❌ StackTrace: {ex.StackTrace}");
                throw;
            }
            finally
            {
                if (tempPreEncodeWav != null && File.Exists(tempPreEncodeWav))
                {
                    try
                    {
                        File.Delete(tempPreEncodeWav);
                        Log($"🗑️ Temp pre-encode file deleted: {tempPreEncodeWav}");
                    }
                    catch (Exception ex)
                    {
                        Log($"⚠️ Failed to delete temp file: {ex.Message}");
                    }
                }
                Log($"🔚 EncodeFileAsync finished");
            }
        }
        private string GetCurrentCommandLineArgs()
        {
            string mode = "";
            int bitrate = 0;
            int vbrValue = 0;
            int qValue = 0;
            bool useQ = false;
            bool useChannels = false;
            string channelMode = "";

            if (radioButtonModeCBR_MP3.Checked)
            {
                mode = "CBR";
                bitrate = CbrBitrates[trackBarCBR_MP3.Value];
            }
            else if (radioButtonModeABR_MP3.Checked)
            {
                mode = "ABR";
                bitrate = AbrBitrates[trackBarABR_MP3.Value];
            }
            else if (radioButtonModeVBR_MP3.Checked)
            {
                mode = "VBR";
                vbrValue = Math.Abs(trackBarVBR_MP3.Value);
            }

            useQ = checkBoxParameter_q_MP3.Checked;
            if (useQ) qValue = Math.Abs(trackBarParameter_q_MP3.Value);

            useChannels = checkBoxChannelsModes_MP3.Checked;
            if (useChannels)
            {
                if (radioButtonJointStereo_MP3.Checked) channelMode = "j";
                else if (radioButtonStereo_MP3.Checked) channelMode = "s";
                else if (radioButtonMono_MP3.Checked) channelMode = "m";
            }

            StringBuilder args = new();

            if (!string.IsNullOrEmpty(mode))
            {
                switch (mode)
                {
                    case "CBR": _ = args.Append($"-b {bitrate} "); break;
                    case "ABR": _ = args.Append($"--abr {bitrate} "); break;
                    case "VBR": _ = args.Append($"-V {vbrValue} "); break;
                }
            }

            if (useQ) _ = args.Append($"-q {qValue} ");
            if (useChannels && !string.IsNullOrEmpty(channelMode)) _ = args.Append($"-m {channelMode} ");

            return args.ToString().TrimEnd();
        }

        private string BuildLameArguments(string inputPath, string outputPath)
        {
            string presetArgs = "";
            bool isPresetSelected = false;
            string uiArgs = "";

            Invoke(() =>
            {
                if (radioButtonUserPreset1.Checked) { presetArgs = textBoxUserPreset1.Text; isPresetSelected = true; }
                else if (radioButtonUserPreset2.Checked) { presetArgs = textBoxUserPreset2.Text; isPresetSelected = true; }
                else if (radioButtonUserPreset3.Checked) { presetArgs = textBoxUserPreset3.Text; isPresetSelected = true; }
                else if (radioButtonUserPreset4.Checked) { presetArgs = textBoxUserPreset4.Text; isPresetSelected = true; }
                else if (radioButtonUserPreset5.Checked) { presetArgs = textBoxUserPreset5.Text; isPresetSelected = true; }
                else if (radioButtonUserPreset6.Checked) { presetArgs = textBoxUserPreset6.Text; isPresetSelected = true; }

                if (!isPresetSelected)
                {
                    uiArgs = GetCurrentCommandLineArgs();
                }
            });

            StringBuilder args = new();

            if (isPresetSelected)
            {
                if (!string.IsNullOrEmpty(presetArgs))
                {
                    _ = args.Append($"{presetArgs} \"{inputPath}\" \"{outputPath}\"");
                    Log($"🔧 BuildLameArguments: using preset '{presetArgs}'");
                }
                else
                {
                    _ = args.Append($"\"{inputPath}\" \"{outputPath}\"");
                    Log($"🔧 BuildLameArguments: preset selected but empty - NO parameters");
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(uiArgs))
                {
                    _ = args.Append($"{uiArgs} \"{inputPath}\" \"{outputPath}\"");
                }
                else
                {
                    _ = args.Append($"\"{inputPath}\" \"{outputPath}\"");
                }
                Log($"🔧 BuildLameArguments: {uiArgs}");
            }

            return args.ToString();
        }

        private int ParseProgress(string line)
        {
            Match match = Regex.Match(line, @"\((\d+)%\)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int progress))
            {
                return progress;
            }
            return -1;
        }

        private void UpdateEncodingUI()
        {
            if (InvokeRequired) { Invoke(UpdateEncodingUI); return; }

            if (_encodingStatus != _lastLoggedStatus || _isSeamlessReencode != _lastSeamlessState)
            {
                Log($"🔄 UpdateEncodingUI: status={_encodingStatus}, seamless={_isSeamlessReencode}");
                _lastLoggedStatus = _encodingStatus;
                _lastSeamlessState = _isSeamlessReencode;
            }

            switch (_encodingStatus)
            {
                case EncodingStatus.Idle:
                    progressBarEncodingProcess.Visible = false;
                    progressBarEncodingProcess.Style = ProgressBarStyle.Blocks;
                    buttonPlayPause.Enabled = true;
                    buttonStop.Enabled = true;
                    break;
                case EncodingStatus.Queued:
                case EncodingStatus.Running:
                    progressBarEncodingProcess.Visible = true;
                    progressBarEncodingProcess.Style = _encodingStatus == EncodingStatus.Queued ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous;
                    if (_isSeamlessReencode)
                    {
                        buttonPlayPause.Enabled = true;
                        buttonStop.Enabled = true;
                    }
                    else
                    {
                        buttonPlayPause.Enabled = false;
                        buttonStop.Enabled = false;
                    }
                    break;
                case EncodingStatus.Completed:
                case EncodingStatus.Canceled:
                case EncodingStatus.Error:
                    progressBarEncodingProcess.Visible = false;
                    _isSeamlessReencode = false;
                    buttonPlayPause.Enabled = true;
                    buttonStop.Enabled = true;
                    break;
            }
        }

        private void OnSettingsChanged()
        {
            if (_isLoadingSettings)
            {
                Log($"ℹ️ Settings changed during load, ignoring");
                return;
            }
            _needsReencoding = true;
            _encodedFilePath = null;
            _currentCacheKey = null;
            Log("⚙️ Settings changed, will use cached version if available");

            if (_waveOut != null && _playgroundMixer != null &&
                _waveOut.PlaybackState != PlaybackState.Stopped)
            {
                ScheduleSeamlessSwap();
            }
        }

        private void ScheduleSeamlessSwap()
        {
            if (_settingsDebounceTimer == null)
            {
                _settingsDebounceTimer = new System.Windows.Forms.Timer { Interval = 500 };
                _settingsDebounceTimer.Tick += OnSeamlessDebounceTick;
            }
            _settingsDebounceTimer.Stop();
            _settingsDebounceTimer.Start();
            Log("⏱️ Seamless swap scheduled (debounce)");
        }

        private void OnSeamlessDebounceTick(object? sender, EventArgs e)
        {
            _settingsDebounceTimer?.Stop();
            StartSeamlessSwap();
        }

        private void StartSeamlessSwap()
        {
            if (_waveOut == null || _playgroundMixer == null || _waveOut.PlaybackState == PlaybackState.Stopped)
            {
                Log("⚠️ Seamless swap skipped: playback not active");
                return;
            }
            if (string.IsNullOrEmpty(_originalFilePath) || string.IsNullOrEmpty(_selectedEncoderPath))
            {
                Log("⚠️ Seamless swap skipped: no original file or encoder");
                return;
            }

            string cacheKey = GenerateCacheFileNameAndCacheKey(_originalFilePath, _selectedEncoderPath);
            _currentCacheKey = cacheKey;
            Log($"🔁 Seamless swap target key: {cacheKey}");

            _seamlessCts?.Cancel();
            _seamlessCts?.Dispose();
            _seamlessCts = new CancellationTokenSource();
            CancellationToken token = _seamlessCts.Token;

            string originalPath = _originalFilePath;

            _isSeamlessReencode = true;
            _encodingStatus = EncodingStatus.Queued;
            UpdateEncodingUI();

            _ = Task.Run(async () =>
            {
                try
                {
                    string encodedPath;
                    int delay;

                    if (TryGetFromCache(cacheKey, out string? cachedFile, out int cachedDelay))
                    {
                        encodedPath = cachedFile!;
                        delay = cachedDelay;
                        Log($"📦 Seamless: using cached file {Path.GetFileName(encodedPath)} (delay {delay})");
                    }
                    else
                    {
                        encodedPath = await EncodeToTempFileAsync(originalPath, cacheKey, token);
                        if (token.IsCancellationRequested) return;
                        Log("🔄 Seamless: calculating codec delay...");
                        delay = ComputeDelayForFile(originalPath, encodedPath);
                        AddToCache(cacheKey, encodedPath, delay);
                        Log($"🔧 Seamless: delay = {delay} samples");
                    }

                    if (token.IsCancellationRequested) return;

                    DecodeAndSwap(encodedPath, delay, cacheKey);

                    if (token.IsCancellationRequested) return;

                    Invoke(() =>
                    {
                        if (_currentCacheKey != cacheKey)
                        {
                            Log("⚠️ Seamless swap superseded by a newer request");
                            _isSeamlessReencode = false;
                            UpdateEncodingUI();
                            return;
                        }
                        _encodedFilePath = encodedPath;
                        _needsReencoding = false;
                        _isSeamlessReencode = false;
                        _encodingStatus = EncodingStatus.Completed;
                        UpdateEncodingUI();
                        UpdateEncoderSettingsReturnedByMILabel();
                        Log("✅ Seamless swap applied");
                    });
                }
                catch (OperationCanceledException)
                {
                    Log("⚠️ Seamless encode canceled");
                }
                catch (Exception ex)
                {
                    Log($"❌ Seamless swap failed: {ex.Message}");
                    try
                    {
                        Invoke(() =>
                        {
                            _isSeamlessReencode = false;
                            _encodingStatus = EncodingStatus.Error;
                            UpdateEncodingUI();
                        });
                    }
                    catch { }
                }
            });
        }

        private void DecodeAndSwap(string encodedPath, int delaySamples, string cacheKey)
        {
            var mixer = _playgroundMixer;
            var format = _originalFormat;
            if (mixer == null || format == null)
            {
                Log("⚠️ DecodeAndSwap skipped: mixer/format unavailable");
                return;
            }

            float[] encData;
            lock (_cacheLock)
            {
                if (_decodedCache.TryGetValue(cacheKey, out float[]? cached))
                {
                    encData = cached;
                    Log($"📦 Decoded cache hit for {cacheKey}: {encData.Length} samples");
                }
                else
                {
                    Log($"🔄 Decoding MP3 to memory: {Path.GetFileName(encodedPath)}");
                    encData = LoadEncodedToMemory(format, encodedPath);
                    _decodedCache[cacheKey] = encData;
                    Log($"💾 Decoded cache stored: {encData.Length} samples");
                }
            }

            var floatFormat = WaveFormat.CreateIeeeFloatWaveFormat(format.SampleRate, format.Channels);
            var newEncSource = new MemorySampleSource(encData, floatFormat);
            mixer.SwapEncoded(newEncSource, delaySamples);
            Log($"✅ Decoded & swapped: {encData.Length} samples, delay={delaySamples}");
        }

        private int ComputeDelayForFile(string originalPath, string encodedPath)
        {
            int bitrate = 0;
            try
            {
                using var mpegReader = new MediaFoundationReader(encodedPath);
                double duration = mpegReader.TotalTime.TotalSeconds;
                if (duration > 0)
                    bitrate = (int)((new FileInfo(encodedPath).Length * 8) / duration / 1000);
            }
            catch (Exception ex)
            {
                Log($"⚠️ ComputeDelayForFile: failed to estimate bitrate: {ex.Message}");
            }
            return CalculateCodecDelay(originalPath, encodedPath, bitrate);
        }

        private (string lameInputPath, string? tempPreEncodeWav) PrepareLameInput(string inputPath, CancellationToken ct)
        {
            string ext = Path.GetExtension(inputPath);
            bool needsConversion = false;
            int targetBits = 16;

            if (ext.Equals(".wav", StringComparison.OrdinalIgnoreCase))
            {
                using var checkReader = new WaveFileReader(inputPath);
                int bps = checkReader.WaveFormat.BitsPerSample;
                var enc = checkReader.WaveFormat.Encoding;
                if (bps == 16 && enc == WaveFormatEncoding.Pcm) { }
                else if (bps == 24 && enc == WaveFormatEncoding.Pcm) { }
                else { needsConversion = true; targetBits = 24; }
            }
            else if (ext.Equals(".flac", StringComparison.OrdinalIgnoreCase))
            {
                needsConversion = true;
                try { targetBits = ReadFlacStreamInfo(inputPath).BitsPerSample; }
                catch { targetBits = 16; }
            }
            else
            {
                needsConversion = true;
                targetBits = 16;
            }

            if (!needsConversion) return (inputPath, null);

            string tempPreEncodeWav = Path.Combine(_tempFolder, $"preencode_{Guid.NewGuid()}.wav");
            ISampleProvider sourceProvider;
            int sampleRate, channels;
            IDisposable? disposableSource = null;

            if (ext.Equals(".wav", StringComparison.OrdinalIgnoreCase))
            {
                var wavReader = new WaveFileReader(inputPath);
                disposableSource = wavReader;
                sampleRate = wavReader.WaveFormat.SampleRate;
                channels = wavReader.WaveFormat.Channels;
                sourceProvider = wavReader.WaveFormat.BitsPerSample == 24
                    ? new Wave24ToFloatProvider(wavReader)
                    : wavReader.ToSampleProvider();
            }
            else
            {
                var audioReader = new AudioFileReader(inputPath);
                disposableSource = audioReader;
                sampleRate = audioReader.WaveFormat.SampleRate;
                channels = audioReader.WaveFormat.Channels;
                sourceProvider = audioReader;
            }

            WaveFormat targetFormat;
            IWaveProvider waveWriter;
            if (targetBits == 24)
            {
                targetFormat = new WaveFormat(sampleRate, 24, channels);
                waveWriter = new SampleToWaveProvider24(sourceProvider);
            }
            else
            {
                targetFormat = new WaveFormat(sampleRate, 16, channels);
                waveWriter = new SampleToWaveProvider16(sourceProvider);
            }

            using (var writer = new WaveFileWriter(tempPreEncodeWav, targetFormat))
            {
                byte[] buf = new byte[targetFormat.AverageBytesPerSecond];
                int read;
                while ((read = waveWriter.Read(buf, 0, buf.Length)) > 0)
                {
                    ct.ThrowIfCancellationRequested();
                    writer.Write(buf, 0, read);
                }
            }
            disposableSource?.Dispose();
            Log($"✅ Seamless pre-encode ready: {new FileInfo(tempPreEncodeWav).Length} bytes ({targetBits}-bit)");
            return (tempPreEncodeWav, tempPreEncodeWav);
        }

        private async Task<string> EncodeToTempFileAsync(string inputPath, string cacheKey, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(_selectedEncoderPath))
                throw new InvalidOperationException("Encoder is not selected.");
            string encoderPath = _selectedEncoderPath;
            string tempEncodedFile = Path.Combine(_tempFolder, $"{cacheKey}.mp3");

            if (File.Exists(tempEncodedFile) && new FileInfo(tempEncodedFile).Length > 0)
            {
                Log($"ℹ️ Seamless: encoded file already exists: {Path.GetFileName(tempEncodedFile)}");
                return tempEncodedFile;
            }

            string? tempPreEncodeWav = null;
            string lameInputPath = inputPath;
            try
            {
                (lameInputPath, tempPreEncodeWav) = PrepareLameInput(inputPath, ct);

                string args = BuildLameArguments(lameInputPath, tempEncodedFile);
                Log($"🔧 Seamless LAME args: {args}");
                using Process process = new()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = encoderPath,
                        Arguments = args,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true
                    },
                    EnableRaisingEvents = true
                };
                process.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        int progress = ParseProgress(e.Data);
                        if (progress is >= 0 and <= 100)
                        {
                            try
                            {
                                BeginInvoke(() =>
                                {
                                    if (_encodingStatus == EncodingStatus.Queued) _encodingStatus = EncodingStatus.Running;
                                    progressBarEncodingProcess.Value = progress;
                                    UpdateEncodingUI();
                                });
                            }
                            catch { }
                        }
                    }
                };
                try { Invoke(() => { _encodingStatus = EncodingStatus.Running; UpdateEncodingUI(); }); } catch { }

                _ = process.Start();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync(ct);

                if (ct.IsCancellationRequested)
                {
                    try { File.Delete(tempEncodedFile); } catch { }
                    throw new OperationCanceledException();
                }
                if (process.ExitCode != 0)
                {
                    try { File.Delete(tempEncodedFile); } catch { }
                    throw new Exception($"LAME failed with code {process.ExitCode}");
                }
                if (!File.Exists(tempEncodedFile) || new FileInfo(tempEncodedFile).Length == 0)
                    throw new Exception("Encoded file is empty");

                Log($"✅ Seamless MP3 created: {new FileInfo(tempEncodedFile).Length} bytes");
                return tempEncodedFile;
            }
            finally
            {
                if (tempPreEncodeWav != null && File.Exists(tempPreEncodeWav))
                {
                    try { File.Delete(tempPreEncodeWav); } catch { }
                }
            }
        }

        private float[] ReadAllSamples(ISampleProvider provider)
        {
            using var ms = new MemoryStream();
            float[] buf = new float[65536];
            byte[] bytes = new byte[65536 * 4];
            int read;
            while ((read = provider.Read(buf, 0, buf.Length)) > 0)
            {
                Buffer.BlockCopy(buf, 0, bytes, 0, read * 4);
                ms.Write(bytes, 0, read * 4);
            }
            byte[] all = ms.ToArray();
            float[] result = new float[all.Length / 4];
            Buffer.BlockCopy(all, 0, result, 0, all.Length);
            Log($"📦 ReadAllSamples: {result.Length} samples");
            return result;
        }

        private float[] LoadOriginalToMemory(out WaveFormat originalFormat)
        {
            string ext = Path.GetExtension(_originalFilePath!).ToLower();
            ISampleProvider provider;
            IDisposable? disposable = null;
            if (ext == ".wav")
            {
                var wav = new WaveFileReader(_originalFilePath);
                originalFormat = wav.WaveFormat;
                provider = (originalFormat.BitsPerSample == 24)
                    ? new Wave24ToFloatProvider(wav)
                    : wav.ToSampleProvider();
                disposable = wav;
            }
            else
            {
                var audio = new AudioFileReader(_originalFilePath);
                originalFormat = audio.WaveFormat;
                provider = audio;
                disposable = audio;
            }
            try
            {
                return ReadAllSamples(provider);
            }
            finally
            {
                disposable?.Dispose();
            }
        }

        private float[] LoadEncodedToMemory(WaveFormat originalFormat)
        {
            if (string.IsNullOrEmpty(_encodedFilePath))
                throw new InvalidOperationException("No encoded file path specified for LoadEncodedToMemory");
            return LoadEncodedToMemory(originalFormat, _encodedFilePath);
        }

        private float[] LoadEncodedToMemory(WaveFormat originalFormat, string encodedPath)
        {
            using var mpegReader = new MediaFoundationReader(encodedPath);
            ISampleProvider provider = mpegReader.ToSampleProvider();
            if (mpegReader.WaveFormat.Channels == 1 && originalFormat.Channels == 2)
                provider = new MonoToStereoSampleProvider(provider);
            if (mpegReader.WaveFormat.SampleRate != originalFormat.SampleRate)
                provider = new WdlResamplingSampleProvider(provider, originalFormat.SampleRate);
            return ReadAllSamples(provider);
        }

        private long GetPlaybackPositionBytes()
        {
            if (_playgroundMixer != null) return _playgroundMixer.GetPositionBytes();
            if (_originalMemorySource != null)
            {
                long frame = _originalMemorySource.PositionSamples / _originalMemorySource.WaveFormat.Channels;
                return frame * _originalBytesPerFrame;
            }
            return 0;
        }

        private long GetPlaybackTotalBytes()
        {
            if (_playgroundMixer != null) return _playgroundMixer.GetTotalBytes();
            if (_originalMemorySource != null)
            {
                long frame = _originalMemorySource.LengthSamples / _originalMemorySource.WaveFormat.Channels;
                return frame * _originalBytesPerFrame;
            }
            return 0;
        }

        private void SeekPlaybackToBytes(long bytes)
        {
            if (_playgroundMixer != null)
            {
                _playgroundMixer.SeekToBytes(bytes);
            }
            else if (_originalMemorySource != null)
            {
                long frame = bytes / _originalBytesPerFrame;
                _originalMemorySource.PositionSamples = frame * _originalMemorySource.WaveFormat.Channels;
            }
            _currentPlaybackPosition = bytes;
        }

        private void InitializePlayback()
        {
            Log($"🎵 InitializePlayback started");
            StopDualPlayback();
            if (string.IsNullOrEmpty(_originalFilePath) || !File.Exists(_originalFilePath))
            {
                Log($"⚠️ No valid original file: {_originalFilePath}");
                return;
            }
            try
            {
                float[] origData = LoadOriginalToMemory(out WaveFormat originalFormat);
                _originalFormat = originalFormat;
                _originalBytesPerFrame = originalFormat.Channels * (originalFormat.BitsPerSample / 8);
                var floatFormat = WaveFormat.CreateIeeeFloatWaveFormat(originalFormat.SampleRate, originalFormat.Channels);
                _originalMemorySource = new MemorySampleSource(origData, floatFormat);
                Log($"✅ Original loaded to memory: {origData.Length} samples, format={originalFormat}");

                if (!string.IsNullOrEmpty(_encodedFilePath) && File.Exists(_encodedFilePath))
                {
                    int delaySamples = 0;
                    if (_currentCacheKey != null)
                    {
                        lock (_cacheLock) { _delayCache.TryGetValue(_currentCacheKey, out delaySamples); }
                    }
                    Log($"📌 Using delay: {delaySamples} samples");

                    float[] encData = LoadEncodedToMemory(originalFormat);

                    if (_currentCacheKey != null)
                    {
                        lock (_cacheLock)
                        {
                            _decodedCache[_currentCacheKey] = encData;
                        }
                        Log($"💾 Decoded cache stored at startup: {encData.Length} samples for {_currentCacheKey}");
                    }

                    var encSource = new MemorySampleSource(encData, floatFormat);
                    Log($"✅ Encoded loaded to memory: {encData.Length} samples");

                    _playgroundMixer = new CodecPlaygroundMixer(_originalMemorySource, encSource, originalFormat, delaySamples)
                    {
                        CurrentMode = _currentPlayMode
                    };

                    float savedBalance = trackBarMixBalance.Value / 100f;
                    _playgroundMixer.SetMixBalance(savedBalance);

                    _activeEncodedCacheKey = _currentCacheKey;
                    Log($"✅ Mixer created, mode={_currentPlayMode}, activeKey={_activeEncodedCacheKey}");
                }

                _waveOut = new WasapiOut();
                _waveOut.PlaybackStopped += WaveOut_PlaybackStopped!;
                _waveOut.Init(_playgroundMixer != null ? _playgroundMixer : _originalMemorySource);
                Log("📌 WasapiOut initialized");

                if (_currentPlaybackPosition > 0)
                {
                    long totalBytes = GetPlaybackTotalBytes();
                    long safePosition = Math.Min(_currentPlaybackPosition, totalBytes > 0 ? totalBytes : 0);

                    if (_originalBytesPerFrame > 0)
                        safePosition -= safePosition % _originalBytesPerFrame;

                    Log($"📌 Restoring position: {safePosition} bytes (requested: {_currentPlaybackPosition}, max: {totalBytes})");
                    SeekPlaybackToBytes(safePosition);
                }
                Log($"✅ InitializePlayback completed");
            }
            catch (Exception ex)
            {
                Log($"❌ ERROR in InitializePlayback: {ex.Message}");
                Log($"❌ StackTrace: {ex.StackTrace}");
                MessageBox.Show($"Error initializing playback: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PlayDual()
        {
            Log($"▶️ PlayDual called, waveOut={_waveOut != null}");
            if (_waveOut == null) return;
            try
            {
                _waveOut.Play();
                _currentPlayerState = PlayerState.Playing;
                buttonPlayPause.Text = "❚❚";
                timerTrackBarSeek.Start();
                Log($"▶️ Playback started");
            }
            catch (Exception ex)
            {
                Log($"❌ Play error: {ex.Message}");
                Debug.WriteLine($"Play error: {ex.Message}");
            }
        }

        private void PauseDual()
        {
            Log($"⏸️ PauseDual called");
            if (_waveOut != null)
            {
                _currentPlaybackPosition = GetPlaybackPositionBytes();
                _waveOut.Pause();
                Log($"⏸️ Playback paused at position {_currentPlaybackPosition}");
            }
            _currentPlayerState = PlayerState.Paused;
            buttonPlayPause.Text = "▶";
            timerTrackBarSeek.Stop();
        }

        private void StopDualPlayback()
        {
            Log($"⏹️ StopDualPlayback called");
            _pendingPlayAfterEncode = false;

            _settingsDebounceTimer?.Stop();
            _seamlessCts?.Cancel();
            if (_isSeamlessReencode)
            {
                _isSeamlessReencode = false;
                _encodingStatus = EncodingStatus.Idle;
                UpdateEncodingUI();
            }

            if (_waveOut != null)
            {
                Log($"⏹️ Stopping and disposing waveOut");
                _waveOut.PlaybackStopped -= WaveOut_PlaybackStopped!;
                _waveOut.Stop();
                _waveOut.Dispose();
                _waveOut = null;
            }
            _playgroundMixer = null;
            _originalMemorySource = null;
            timerTrackBarSeek.Stop();
            trackBarSeek.Value = 0;
            _currentPlaybackPosition = 0;
            _currentPlayerState = PlayerState.Stopped;
            labelEncoderSettingsReturnedByMI.Text = string.Empty;
            buttonPlayPause.Text = "▶";
            Log($"⏹️ Playback stopped completely");
        }

        public class Wave24ToFloatProvider : ISampleProvider
        {
            private readonly WaveFileReader _reader;
            private readonly byte[] _buffer;
            private readonly int _bytesPerSample;
            private readonly int _channels;
            private readonly float _scale;
            public WaveFormat WaveFormat { get; }

            public Wave24ToFloatProvider(WaveFileReader reader)
            {
                Log($"🔧 Wave24ToFloatProvider created");
                _reader = reader; _channels = reader.WaveFormat.Channels; _bytesPerSample = reader.WaveFormat.BitsPerSample / 8;
                WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(reader.WaveFormat.SampleRate, _channels);
                _buffer = new byte[_bytesPerSample * _channels]; _scale = 1.0f / 8388608.0f;
                Log($"📊 24-bit provider: channels={_channels}, bytesPerSample={_bytesPerSample}");
            }

            public int Read(float[] buffer, int offset, int count)
            {
                int samplesRead = 0; int framesNeeded = count / _channels; int framesRead = 0;
                while (framesRead < framesNeeded)
                {
                    int bytesRead = _reader.Read(_buffer, 0, _buffer.Length); if (bytesRead == 0) break;
                    for (int c = 0; c < _channels && samplesRead < count; c++)
                    {
                        int sample24 = 0; int byteOffset = c * _bytesPerSample;
                        sample24 |= _buffer[byteOffset + 0] << 0; sample24 |= _buffer[byteOffset + 1] << 8; sample24 |= _buffer[byteOffset + 2] << 16;
                        if ((sample24 & 0x800000) != 0) sample24 |= unchecked((int)0xFF000000);
                        buffer[offset + samplesRead] = sample24 * _scale; samplesRead++;
                    }
                    framesRead++;
                }
                return samplesRead;
            }
        }

        public class SampleToWaveProvider24 : IWaveProvider
        {
            private readonly ISampleProvider _source;

            public WaveFormat WaveFormat { get; }

            public SampleToWaveProvider24(ISampleProvider source)
            {
                Log($"🔧 SampleToWaveProvider24 created");
                _source = source;
                WaveFormat = new WaveFormat(
                    source.WaveFormat.SampleRate,
                    24,
                    source.WaveFormat.Channels);
                Log($"📊 24-bit target: {WaveFormat}");
            }

            public int Read(byte[] buffer, int offset, int count)
            {
                int samplesNeeded = count / 3;
                float[] floatBuffer = new float[samplesNeeded];
                int samplesRead = _source.Read(floatBuffer, 0, samplesNeeded);

                for (int i = 0; i < samplesRead; i++)
                {
                    float sample = Math.Clamp(floatBuffer[i], -1.0f, 1.0f);
                    int intSample = (int)(sample * 8388607.0f);

                    int pos = offset + i * 3;
                    buffer[pos + 0] = (byte)(intSample & 0xFF);
                    buffer[pos + 1] = (byte)((intSample >> 8) & 0xFF);
                    buffer[pos + 2] = (byte)((intSample >> 16) & 0xFF);
                }

                return samplesRead * 3;
            }
        }

        private void TimerTrackBarSeek_Tick(object? sender, EventArgs e)
        {
            if (_isDraggingTrackBarSeek || _waveOut == null) return;
            if (_waveOut.PlaybackState is PlaybackState.Playing or PlaybackState.Paused)
            {
                try
                {
                    long wavBytes = GetPlaybackPositionBytes();
                    _currentPlaybackPosition = wavBytes;
                    long totalLength = GetPlaybackTotalBytes();
                    if (totalLength > 0)
                    {
                        double progress = Math.Clamp((double)wavBytes / totalLength, 0, 1);
                        trackBarSeek.Value = Math.Clamp((int)(progress * 1000), 0, 1000);
                        if (trackBarSeek.Value >= 1000)
                        {
                            trackBarSeek.Value = 1000;
                            if (!_loopPlayback)
                            {
                                Log($"⏹️ Playback reached end, stopping");
                                StopDualPlayback();
                            }
                            else
                            {
                                Log($"🔁 Loop: restarting playback");
                                SeekPlaybackToBytes(0);
                                if (_waveOut != null && _currentPlayerState == PlayerState.Playing) _waveOut.Play();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"❌ Timer error: {ex.Message}");
                    Debug.WriteLine($"Timer error: {ex.Message}");
                }
            }
        }

        private void TrackBarSeek_Scroll(object sender, EventArgs e)
        {
            _isDraggingTrackBarSeek = true;
            Log($"🖱️ Seek scroll: {trackBarSeek.Value}");
        }

        private void TrackBarSeek_MouseDown(object? sender, MouseEventArgs e)
        {
            Log($"🖱️ Seek mouse down at position: {e.X}");

            if (sender is not TrackBar trackBar) return;

            double thumbPosition = (double)(trackBar.Value - trackBar.Minimum) / (trackBar.Maximum - trackBar.Minimum);
            int thumbX = (int)(thumbPosition * (trackBar.Width - 4));

            int thumbHalfWidth = 8;

            bool isThumbClick = Math.Abs(e.X - thumbX) <= thumbHalfWidth;

            if (isThumbClick)
            {
                _isDraggingTrackBarSeek = true;
                Log($"🖱️ Seek drag started (thumb click)");
                return;
            }

            Log($"📍 Seek click on track bar (not thumb)");

            double value = (double)e.X / (trackBar.Width - 4) * (trackBar.Maximum - trackBar.Minimum);
            int newValue = (int)Math.Round(value);
            newValue = Math.Clamp(newValue, trackBar.Minimum, trackBar.Maximum);

            trackBar.Value = newValue;

            long totalLength = GetPlaybackTotalBytes();
            if (totalLength <= 0) return;

            long newWavPosition = (long)(totalLength * Math.Clamp(trackBar.Value / 1000.0, 0, 1));

            bool wasPlaying = _waveOut?.PlaybackState == PlaybackState.Playing;
            if (wasPlaying)
            {
                _waveOut?.Pause();
                Log($"⏸️ Paused for seek");
            }

            try
            {
                SeekPlaybackToBytes(newWavPosition);
                Log($"📍 Seek to position: {newWavPosition} bytes ({newWavPosition / (double)totalLength * 100:F1}%)");
            }
            finally
            {
                if (wasPlaying && _waveOut != null)
                {
                    _waveOut.Play();
                    Log($"▶️ Resumed after seek");
                }
            }
        }

        private void TrackBarSeek_MouseUp(object sender, MouseEventArgs e)
        {
            if (_isDraggingTrackBarSeek)
            {
                Log($"🖱️ Seek mouse up: {trackBarSeek.Value}");
                _isDraggingTrackBarSeek = false;

                long totalLength = GetPlaybackTotalBytes();
                if (totalLength <= 0) return;

                long newWavPosition = (long)(totalLength * Math.Clamp(trackBarSeek.Value / 1000.0, 0, 1));

                bool wasPlaying = _waveOut?.PlaybackState == PlaybackState.Playing;
                if (wasPlaying)
                {
                    _waveOut?.Pause();
                    Log($"⏸️ Paused for seek");
                }

                try
                {
                    SeekPlaybackToBytes(newWavPosition);
                    Log($"📍 Seek to position: {newWavPosition} bytes ({newWavPosition / (double)totalLength * 100:F1}%)");
                }
                finally
                {
                    if (wasPlaying && _waveOut != null)
                    {
                        _waveOut.Play();
                        Log($"▶️ Resumed after seek");
                    }
                }
            }
        }

        private void WaveOut_PlaybackStopped(object? sender, StoppedEventArgs e)
        {
            Log($"⏹️ WaveOut_PlaybackStopped event, loop={_loopPlayback}, state={_currentPlayerState}");
            if (_loopPlayback && _currentPlayerState != PlayerState.Paused)
            {
                Log($"🔁 Loop: restarting from beginning");
                SeekPlaybackToBytes(0);
                _waveOut?.Play();
            }
            else
            {
                Invoke(() =>
                {
                    _currentPlayerState = PlayerState.Stopped;
                    buttonPlayPause.Text = "▶";
                    trackBarSeek.Value = 0;
                    _currentPlaybackPosition = 0;
                    Log($"⏹️ Playback stopped");
                });
            }
        }

        private void RadioPlaySource_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is RadioButton rb && rb.Checked)
            {
                PlayMode newMode = rb == radioButtonPlayOriginal ? PlayMode.Original :
                                   rb == radioButtonPlayEncoded ? PlayMode.Encoded :
                                   rb == radioButtonPlayMix ? PlayMode.Mix :
                                   rb == radioButtonPlayDifference ? PlayMode.Difference : PlayMode.Original;

                if (_currentPlayMode == newMode) return;
                Log($"=== SWITCH: {_currentPlayMode} -> {newMode} ===");
                _currentPlayMode = newMode;
                if (_playgroundMixer != null)
                {
                    _playgroundMixer.CurrentMode = _currentPlayMode;
                    Log($"✅ Mixer mode updated to {_currentPlayMode}");
                }

                bool showBalance = _currentPlayMode == PlayMode.Mix;
                trackBarMixBalance.Visible = showBalance;
                labelMixBalance.Visible = showBalance;
                if (showBalance)
                {
                    TrackBarMixBalance_Scroll(trackBarMixBalance, EventArgs.Empty);
                }
            }
        }

        private void RadioButtonMode_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is RadioButton rb && (rb == radioButton_Hidden_Mode_OFF_MP3 || rb == radioButton_Hidden_UserPreset_OFF))
                return;

            if (sender is not RadioButton radio || !radio.Checked)
                return;

            bool isMP3SettingsModeButton = radio == radioButtonModeCBR_MP3 || radio == radioButtonModeABR_MP3 ||
                                           radio == radioButtonModeVBR_MP3;

            bool isUserPresetButton = radio == radioButtonUserPreset1 || radio == radioButtonUserPreset2 ||
                                      radio == radioButtonUserPreset3 || radio == radioButtonUserPreset4 ||
                                      radio == radioButtonUserPreset5 || radio == radioButtonUserPreset6;

            if (isMP3SettingsModeButton)
            {
                radioButtonUserPreset1.Checked = false;
                radioButtonUserPreset2.Checked = false;
                radioButtonUserPreset3.Checked = false;
                radioButtonUserPreset4.Checked = false;
                radioButtonUserPreset5.Checked = false;
                radioButtonUserPreset6.Checked = false;
                radioButton_Hidden_UserPreset_OFF.Checked = true;
                radioButton_Hidden_Mode_OFF_MP3.Checked = false;
            }
            else if (isUserPresetButton)
            {
                radioButtonModeCBR_MP3.Checked = false;
                radioButtonModeABR_MP3.Checked = false;
                radioButtonModeVBR_MP3.Checked = false;
                radioButton_Hidden_Mode_OFF_MP3.Checked = true;
                radioButton_Hidden_UserPreset_OFF.Checked = false;
            }
            else
            {
                return;
            }

            bool isMP3SettingsSelected = radioButtonModeCBR_MP3.Checked || radioButtonModeABR_MP3.Checked ||
                                         radioButtonModeVBR_MP3.Checked;

            bool isUserPresetSelected = radioButtonUserPreset1.Checked || radioButtonUserPreset2.Checked ||
                                        radioButtonUserPreset3.Checked || radioButtonUserPreset4.Checked ||
                                        radioButtonUserPreset5.Checked || radioButtonUserPreset6.Checked;

            if (isMP3SettingsSelected)
            {
                trackBarCBR_MP3.Enabled = radioButtonModeCBR_MP3.Checked;
                labelCBRValue_MP3.Enabled = radioButtonModeCBR_MP3.Checked;
                trackBarABR_MP3.Enabled = radioButtonModeABR_MP3.Checked;
                labelABRValue_MP3.Enabled = radioButtonModeABR_MP3.Checked;
                trackBarVBR_MP3.Enabled = radioButtonModeVBR_MP3.Checked;
                labelVBRValue_MP3.Enabled = radioButtonModeVBR_MP3.Checked;

                checkBoxParameter_q_MP3.Enabled = true;
                trackBarParameter_q_MP3.Enabled = checkBoxParameter_q_MP3.Checked;
                labelParameter_qValue_MP3.Enabled = checkBoxParameter_q_MP3.Checked;

                checkBoxChannelsModes_MP3.Enabled = true;
                radioButtonJointStereo_MP3.Enabled = checkBoxChannelsModes_MP3.Checked;
                radioButtonStereo_MP3.Enabled = checkBoxChannelsModes_MP3.Checked;
                radioButtonMono_MP3.Enabled = checkBoxChannelsModes_MP3.Checked;

                buttonSaveUserPreset1.Enabled = true;
                buttonSaveUserPreset2.Enabled = true;
                buttonSaveUserPreset3.Enabled = true;
                buttonSaveUserPreset4.Enabled = true;
                buttonSaveUserPreset5.Enabled = true;
                buttonSaveUserPreset6.Enabled = true;

                Log($"📌 MP3 Encoder settings selected - encoder settings and save buttons enabled");
            }
            else if (isUserPresetSelected)
            {
                trackBarCBR_MP3.Enabled = false;
                labelCBRValue_MP3.Enabled = false;
                trackBarABR_MP3.Enabled = false;
                labelABRValue_MP3.Enabled = false;
                trackBarVBR_MP3.Enabled = false;
                labelVBRValue_MP3.Enabled = false;

                checkBoxParameter_q_MP3.Enabled = false;
                trackBarParameter_q_MP3.Enabled = false;
                labelParameter_qValue_MP3.Enabled = false;

                checkBoxChannelsModes_MP3.Enabled = false;
                radioButtonJointStereo_MP3.Enabled = false;
                radioButtonStereo_MP3.Enabled = false;
                radioButtonMono_MP3.Enabled = false;

                buttonSaveUserPreset1.Enabled = false;
                buttonSaveUserPreset2.Enabled = false;
                buttonSaveUserPreset3.Enabled = false;
                buttonSaveUserPreset4.Enabled = false;
                buttonSaveUserPreset5.Enabled = false;
                buttonSaveUserPreset6.Enabled = false;

                Log($"📌 Preset selected - all MP3 encoder settings and save buttons disabled");
            }

            Log($"📌 RadioButton changed: {radio.Text}");
            OnSettingsChanged();
        }

        private void TrackBarCBR_Scroll(object? sender, EventArgs e)
        {
            labelCBRValue_MP3.Text = CbrBitrates[trackBarCBR_MP3.Value].ToString();
            Log($"📊 CBR trackbar: {labelCBRValue_MP3.Text}");
            OnSettingsChanged();
        }
        private void TrackBarVBR_Scroll(object? sender, EventArgs e)
        {
            labelVBRValue_MP3.Text = $"V{Math.Abs(trackBarVBR_MP3.Value)}";
            Log($"📊 VBR trackbar: {labelVBRValue_MP3.Text}");
            OnSettingsChanged();
        }
        private void TrackBarABR_Scroll(object? sender, EventArgs e)
        {
            labelABRValue_MP3.Text = AbrBitrates[trackBarABR_MP3.Value].ToString();
            Log($"📊 ABR trackbar: {labelABRValue_MP3.Text}");
            OnSettingsChanged();
        }
        private void CheckBoxQ_CheckedChanged(object? sender, EventArgs e)
        {
            bool en = checkBoxParameter_q_MP3.Checked && !radioButton_Hidden_Mode_OFF_MP3.Checked;
            trackBarParameter_q_MP3.Enabled = en;
            labelParameter_qValue_MP3.Enabled = en;
            Log($"📊 Quality checkbox: {en}");
            OnSettingsChanged();
        }
        private void TrackBarQ_Scroll(object? sender, EventArgs e)
        {
            labelParameter_qValue_MP3.Text = $"q{Math.Abs(trackBarParameter_q_MP3.Value)}";
            Log($"📊 Quality trackbar: {labelParameter_qValue_MP3.Text}");
            OnSettingsChanged();
        }
        private void CheckBoxChannelsMix_CheckedChanged(object? sender, EventArgs e)
        {
            bool en = checkBoxChannelsModes_MP3.Checked && !radioButton_Hidden_Mode_OFF_MP3.Checked;
            radioButtonStereo_MP3.Enabled = en;
            radioButtonJointStereo_MP3.Enabled = en;
            radioButtonMono_MP3.Enabled = en;
            Log($"📊 Channel modes checkbox: {en}");
            OnSettingsChanged();
        }
        private void RadioButtonStereoMode_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is RadioButton rb && rb.Checked && checkBoxChannelsModes_MP3.Checked)
            {
                Log($"📌 Stereo mode changed: {rb.Text}");
                OnSettingsChanged();
            }
        }

        private void TrackBarMixBalance_Scroll(object? sender, EventArgs e)
        {
            float balance = trackBarMixBalance.Value / 100f;
            _playgroundMixer?.SetMixBalance(balance);

            int origPct = (int)((1f - balance) * 100);
            int encPct = (int)(balance * 100);
            labelMixBalance.Text = $"{origPct} / {encPct}";
            Log($"🎚️ Mix balance: {origPct}% orig / {encPct}% enc");
        }
        private void TrackBarMixBalance_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                trackBarMixBalance.Value = 50;
                TrackBarMixBalance_Scroll(trackBarMixBalance, EventArgs.Empty);
                Log("🎚️ Mix balance reset to default (50/50)");
            }
        }

        private void ButtonPlayPause_Click(object? sender, EventArgs e)
        {
            Log($"🎵 Play/Pause clicked, state={_currentPlayerState}");

            if (_currentPlayerState == PlayerState.Playing)
            {
                PauseDual();
                return;
            }

            if (_currentPlayerState == PlayerState.Paused)
            {
                PlayDual();
                return;
            }

            if (string.IsNullOrEmpty(_originalFilePath) || listViewAudioFiles.Items.Count == 0)
            {
                Log($"⚠️ No audio file selected or list is empty");
                _ = MessageBox.Show(
                    this,
                    "Please add an audio file (WAV or FLAC) first!\n\n" +
                    "Drag and drop files or folders onto the 'Audio Files' list.",
                    "No Audio File",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_selectedEncoderPath) || listViewEncoders.Items.Count == 0)
            {
                Log($"⚠️ No encoder selected or list is empty");
                _ = MessageBox.Show(
                    this,
                    "Please add a LAME encoder (lame.exe) first!\n\n" +
                    "Drag and drop lame.exe or a folder containing it onto the 'Encoders' list.",
                    "No Encoder",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (!File.Exists(_originalFilePath))
            {
                Log($"⚠️ Audio file not found: {_originalFilePath}");
                _ = MessageBox.Show(
                    this,
                    $"Audio file not found:\n{_originalFilePath}\n\n" +
                    "Please add the file again.",
                    "File Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (!File.Exists(_selectedEncoderPath))
            {
                Log($"⚠️ Encoder not found: {_selectedEncoderPath}");
                _ = MessageBox.Show(
                    this,
                    $"Encoder not found:\n{_selectedEncoderPath}\n\n" +
                    "Please add the encoder again.",
                    "File Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            bool hasEncodedFile = !string.IsNullOrEmpty(_encodedFilePath);
            bool needEncode = _needsReencoding || !hasEncodedFile || !File.Exists(_encodedFilePath);
            Log($"📊 Need encode: {needEncode}, needsReencoding={_needsReencoding}, hasEncoded={hasEncodedFile}, fileExists={hasEncodedFile || File.Exists(_encodedFilePath)}");

            if (needEncode)
            {
                StartEncodingForPlay(_originalFilePath);
            }
            else
            {
                Log($"▶️ Starting playback from existing encoded file");
                InitializePlayback();
                PlayDual();
                UpdateEncoderSettingsReturnedByMILabel();
            }
        }
        private void ButtonStop_Click(object? sender, EventArgs e)
        {
            Log($"⏹️ Stop clicked");
            if (_encodingStatus is EncodingStatus.Running or EncodingStatus.Queued)
            {
                Log($"⏹️ Canceling encoding");
                _encodingCts?.Cancel();
            }
            StopDualPlayback();
        }

        private void ButtonLoopPlayback_Click(object? sender, EventArgs e)
        {
            _loopPlayback = !_loopPlayback;
            buttonLoopPlayback.Text = _loopPlayback ? "Loop: ON" : "Loop: OFF";
            Log($"🔁 Loop playback: {_loopPlayback}");
        }

        private void ButtonSaveUserPreset_Click(object? sender, EventArgs e)
        {
            if (sender is not Button button) return;

            int presetNumber = 0;
            if (button == buttonSaveUserPreset1) presetNumber = 1;
            else if (button == buttonSaveUserPreset2) presetNumber = 2;
            else if (button == buttonSaveUserPreset3) presetNumber = 3;
            else if (button == buttonSaveUserPreset4) presetNumber = 4;
            else if (button == buttonSaveUserPreset5) presetNumber = 5;
            else if (button == buttonSaveUserPreset6) presetNumber = 6;
            else return;

            Log($"💾 Saving user preset {presetNumber}");

            string args = GetCurrentCommandLineArgs();

            switch (presetNumber)
            {
                case 1: textBoxUserPreset1.Text = args; break;
                case 2: textBoxUserPreset2.Text = args; break;
                case 3: textBoxUserPreset3.Text = args; break;
                case 4: textBoxUserPreset4.Text = args; break;
                case 5: textBoxUserPreset5.Text = args; break;
                case 6: textBoxUserPreset6.Text = args; break;
            }

            Log($"✅ User preset {presetNumber} saved: '{args}'");
            OnSettingsChanged();
        }
        private void ButtonUserPresetClear_Click(object? sender, EventArgs e)
        {
            if (sender is not Button button) return;

            int presetNumber = 0;
            if (button == buttonUserPreset1Clear) presetNumber = 1;
            else if (button == buttonUserPreset2Clear) presetNumber = 2;
            else if (button == buttonUserPreset3Clear) presetNumber = 3;
            else if (button == buttonUserPreset4Clear) presetNumber = 4;
            else if (button == buttonUserPreset5Clear) presetNumber = 5;
            else if (button == buttonUserPreset6Clear) presetNumber = 6;
            else return;

            Log($"🗑️ Clearing user preset {presetNumber}");

            switch (presetNumber)
            {
                case 1:
                    textBoxUserPreset1.Text = "";
                    break;
                case 2:
                    textBoxUserPreset2.Text = "";
                    break;
                case 3:
                    textBoxUserPreset3.Text = "";
                    break;
                case 4:
                    textBoxUserPreset4.Text = "";
                    break;
                case 5:
                    textBoxUserPreset5.Text = "";
                    break;
                case 6:
                    textBoxUserPreset6.Text = "";
                    break;
            }

            Log($"✅ User preset {presetNumber} cleared");
            OnSettingsChanged();
        }

        private void Form1_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && !e.Shift)
            {
                switch (e.KeyCode)
                {
                    case Keys.D1:
                        radioButtonUserPreset1.Checked = true;
                        Log($"⌨️ Shortcut: selected preset 1");
                        e.Handled = true;
                        break;
                    case Keys.D2:
                        radioButtonUserPreset2.Checked = true;
                        Log($"⌨️ Shortcut: selected preset 2");
                        e.Handled = true;
                        break;
                    case Keys.D3:
                        radioButtonUserPreset3.Checked = true;
                        Log($"⌨️ Shortcut: selected preset 3");
                        e.Handled = true;
                        break;
                    case Keys.D4:
                        radioButtonUserPreset4.Checked = true;
                        Log($"⌨️ Shortcut: selected preset 4");
                        e.Handled = true;
                        break;
                    case Keys.D5:
                        radioButtonUserPreset5.Checked = true;
                        Log($"⌨️ Shortcut: selected preset 5");
                        e.Handled = true;
                        break;
                    case Keys.D6:
                        radioButtonUserPreset6.Checked = true;
                        Log($"⌨️ Shortcut: selected preset 6");
                        e.Handled = true;
                        break;
                    case Keys.R:  // Random AudioFile
                        SelectRandomAudioFile();
                        e.Handled = true;
                        break;
                }
            }

            if (e.Control && e.Shift)
            {
                switch (e.KeyCode)
                {
                    case Keys.D2:
                        ABTestPresets(2);
                        e.Handled = true;
                        break;
                    case Keys.D3:
                        ABTestPresets(3);
                        e.Handled = true;
                        break;
                    case Keys.D4:
                        ABTestPresets(4);
                        e.Handled = true;
                        break;
                    case Keys.D5:
                        ABTestPresets(5);
                        e.Handled = true;
                        break;
                    case Keys.D6:
                        ABTestPresets(6);
                        e.Handled = true;
                        break;
                }
            }
        }
        private void SelectRandomAudioFile()
        {
            if (listViewAudioFiles.Items.Count == 0)
            {
                Log($"⚠️ No audio files in list to select");
                ShowNotification("⚠️ No audio files in list", false, 3000);
                return;
            }

            if (listViewAudioFiles.Items.Count == 1)
            {
                Log($"ℹ️ Only one audio file, selecting it");
                var item = listViewAudioFiles.Items[0];
                item.Checked = true;
                ShowNotification($"🎵 Selected: {item.Text}", true, 3000);
                return;
            }

            var random = new Random();
            int selectedIndex = random.Next(listViewAudioFiles.Items.Count);
            var selectedItem = listViewAudioFiles.Items[selectedIndex];
            selectedItem.Checked = true;

            Log($"🎲 Random audio file selected: {selectedItem.Text} (index {selectedIndex})");
            ShowNotification($"🎲 Random: {selectedItem.Text}", true, 3000);
        }
        private void ABTestPresets(int count)
        {
            var presetRadios = new List<RadioButton>();
            var presetNames = new List<string>();
            int currentIndex = -1;

            for (int i = 1; i <= count; i++)
            {
                RadioButton? radio = i switch
                {
                    1 => radioButtonUserPreset1,
                    2 => radioButtonUserPreset2,
                    3 => radioButtonUserPreset3,
                    4 => radioButtonUserPreset4,
                    5 => radioButtonUserPreset5,
                    6 => radioButtonUserPreset6,
                    _ => null
                };

                string args = i switch
                {
                    1 => textBoxUserPreset1.Text,
                    2 => textBoxUserPreset2.Text,
                    3 => textBoxUserPreset3.Text,
                    4 => textBoxUserPreset4.Text,
                    5 => textBoxUserPreset5.Text,
                    6 => textBoxUserPreset6.Text,
                    _ => ""
                };

                if (radio != null && !string.IsNullOrEmpty(args))
                {
                    presetRadios.Add(radio);
                    presetNames.Add(radio.Text);
                    if (radio.Checked) currentIndex = presetRadios.Count - 1;
                }
            }

            if (presetRadios.Count == 0)
            {
                Log($"⚠️ A/B test: No presets 1-{count} have content");
                ShowNotification($"⚠️ No presets 1-{count} configured", false, 3000);
                return;
            }

            var random = new Random();
            int newIndex = random.Next(presetRadios.Count);
            var selectedRadio = presetRadios[newIndex];
            string selection = presetNames[newIndex];

            if (newIndex == currentIndex && currentIndex != -1)
            {
                ShowNotification($"🔀 A/B test: Preset {selection} (again)", true, 3000);
                Log($"🔀 A/B test {count}: Preset {selection} (again)");
            }
            else
            {
                selectedRadio.Checked = true;
                ShowNotification($"🔀 A/B test: Preset {selection} selected", true, 3000);
                Log($"🔀 A/B test {count}: Preset {selection} selected");
            }
        }

        // Cache
        private string GenerateCacheFileNameAndCacheKey(string audioPath, string encoderPath)
        {
            Log($"🔑 Generating cache key for {Path.GetFileName(audioPath)}");

            string presetArgs = "";
            bool isPresetSelected = false;
            Invoke(() =>
            {
                if (radioButtonUserPreset1.Checked) { presetArgs = textBoxUserPreset1.Text; isPresetSelected = true; }
                else if (radioButtonUserPreset2.Checked) { presetArgs = textBoxUserPreset2.Text; isPresetSelected = true; }
                else if (radioButtonUserPreset3.Checked) { presetArgs = textBoxUserPreset3.Text; isPresetSelected = true; }
                else if (radioButtonUserPreset4.Checked) { presetArgs = textBoxUserPreset4.Text; isPresetSelected = true; }
                else if (radioButtonUserPreset5.Checked) { presetArgs = textBoxUserPreset5.Text; isPresetSelected = true; }
                else if (radioButtonUserPreset6.Checked) { presetArgs = textBoxUserPreset6.Text; isPresetSelected = true; }
            });

            if (isPresetSelected)
            {
                string fileNameWithExt = Path.GetFileName(audioPath);
                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    fileNameWithExt = fileNameWithExt.Replace(c, '_');
                }
                if (string.IsNullOrEmpty(fileNameWithExt)) fileNameWithExt = "audio.wav";

                string encoderName = Path.GetFileNameWithoutExtension(encoderPath);
                if (string.IsNullOrEmpty(encoderName)) encoderName = "encoder";

                string encoderVersion = "unknown";
                try
                {
                    (string Name, string Version) info = GetEncoderInfo(encoderPath);
                    if (!string.IsNullOrEmpty(info.Version) && info.Version != "Unknown")
                    {
                        encoderVersion = info.Version;
                        foreach (char c in Path.GetInvalidFileNameChars())
                        {
                            encoderVersion = encoderVersion.Replace(c, '_');
                        }
                        encoderVersion = encoderVersion.Replace(' ', '_').Replace('.', '_');
                        if (encoderVersion.Length > 70) encoderVersion = encoderVersion[..70];
                    }
                }
                catch { }

                string encoderFullName = $"{encoderName}_{encoderVersion}";

                string cleanPresetArgs = presetArgs;
                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    cleanPresetArgs = cleanPresetArgs.Replace(c, '_');
                }
                cleanPresetArgs = cleanPresetArgs.Replace(' ', '_').Replace('"', '_');
                if (cleanPresetArgs.Length > 50) cleanPresetArgs = cleanPresetArgs[..50];

                if (string.IsNullOrEmpty(cleanPresetArgs)) cleanPresetArgs = "empty";

                string readableName = $"{fileNameWithExt}_{encoderFullName}_PRESET_{cleanPresetArgs}";
                if (readableName.Length > 130) readableName = readableName[..130];

                StringBuilder settings = new();
                _ = settings.Append($"{audioPath}|{encoderPath}|{encoderVersion}|PRESET|{presetArgs}");

                byte[] bytes = Encoding.UTF8.GetBytes(settings.ToString());
                byte[] hash = SHA256.HashData(bytes);
                string hashString = Convert.ToBase64String(hash)
                    .Replace("/", "_")
                    .Replace("+", "-")[..4];

                string result = $"{readableName}____{hashString}";
                Log($"🔑 Generated cache key from preset: {result}");
                return result;
            }

            string fileNameWithExt2 = Path.GetFileName(audioPath);
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileNameWithExt2 = fileNameWithExt2.Replace(c, '_');
            }
            if (string.IsNullOrEmpty(fileNameWithExt2)) fileNameWithExt2 = "audio.wav";

            string encoderName2 = Path.GetFileNameWithoutExtension(encoderPath);
            if (string.IsNullOrEmpty(encoderName2)) encoderName2 = "encoder";

            string encoderVersion2 = "unknown";
            try
            {
                (string Name, string Version) info = GetEncoderInfo(encoderPath);
                if (!string.IsNullOrEmpty(info.Version) && info.Version != "Unknown")
                {
                    encoderVersion2 = info.Version;
                    foreach (char c in Path.GetInvalidFileNameChars())
                    {
                        encoderVersion2 = encoderVersion2.Replace(c, '_');
                    }
                    encoderVersion2 = encoderVersion2.Replace(' ', '_').Replace('.', '_');
                    if (encoderVersion2.Length > 70) encoderVersion2 = encoderVersion2[..70];
                }
            }
            catch { }

            string encoderFullName2 = $"{encoderName2}_{encoderVersion2}";

            string modeAndBitrate = "CBR_128";
            if (radioButtonModeCBR_MP3.Checked)
            {
                modeAndBitrate = $"CBR_{CbrBitrates[trackBarCBR_MP3.Value]}";
            }
            else if (radioButtonModeABR_MP3.Checked)
            {
                modeAndBitrate = $"ABR_{AbrBitrates[trackBarABR_MP3.Value]}";
            }
            else if (radioButtonModeVBR_MP3.Checked)
            {
                modeAndBitrate = $"VBR_V{Math.Abs(trackBarVBR_MP3.Value)}";
            }

            string quality = "";
            if (checkBoxParameter_q_MP3.Checked)
            {
                quality = $"_q{Math.Abs(trackBarParameter_q_MP3.Value)}";
            }

            string channelMode = "";
            if (checkBoxChannelsModes_MP3.Checked)
            {
                if (radioButtonJointStereo_MP3.Checked) channelMode = "_j";
                else if (radioButtonStereo_MP3.Checked) channelMode = "_s";
                else if (radioButtonMono_MP3.Checked) channelMode = "_m";
            }

            string readableName2 = $"{fileNameWithExt2}_{encoderFullName2}_{modeAndBitrate}{quality}{channelMode}";

            if (readableName2.Length > 130)
            {
                readableName2 = readableName2[..130];
            }

            StringBuilder settings2 = new();
            _ = settings2.Append($"{audioPath}|{encoderPath}|{encoderVersion2}|");

            if (radioButtonModeCBR_MP3.Checked)
                _ = settings2.Append($"CBR_{CbrBitrates[trackBarCBR_MP3.Value]}|");
            else if (radioButtonModeABR_MP3.Checked)
                _ = settings2.Append($"ABR_{AbrBitrates[trackBarABR_MP3.Value]}|");
            else if (radioButtonModeVBR_MP3.Checked)
                _ = settings2.Append($"VBR_{Math.Abs(trackBarVBR_MP3.Value)}|");

            if (checkBoxParameter_q_MP3.Checked)
                _ = settings2.Append($"q{Math.Abs(trackBarParameter_q_MP3.Value)}|");

            if (checkBoxChannelsModes_MP3.Checked)
            {
                if (radioButtonJointStereo_MP3.Checked) _ = settings2.Append("j|");
                else if (radioButtonStereo_MP3.Checked) _ = settings2.Append("s|");
                else if (radioButtonMono_MP3.Checked) _ = settings2.Append("m|");
            }

            byte[] bytes2 = Encoding.UTF8.GetBytes(settings2.ToString());
            byte[] hash2 = SHA256.HashData(bytes2);
            string hashString2 = Convert.ToBase64String(hash2)
                .Replace("/", "_")
                .Replace("+", "-")[..4];

            string result2 = $"{readableName2}____{hashString2}";
            Log($"🔑 Generated cache key: {result2}");
            return result2;
        }
        private void AddToCache(string key, string filePath, int delay)
        {
            Log($"💾 AddToCache: key={key}, file={Path.GetFileName(filePath)}, delay={delay}");
            lock (_cacheLock)
            {
                _encodedCache[key] = filePath;
                _delayCache[key] = delay;
                Log($"📦 Cached: {Path.GetFileName(filePath)} (delay: {delay} samples)");
                Log($"📊 Cache size: {_encodedCache.Count} entries");
            }
        }
        private bool TryGetFromCache(string key, out string? filePath, out int delay)
        {
            Log($"🔍 TryGetFromCache: key={key}");
            lock (_cacheLock)
            {
                filePath = null;
                delay = 0;

                if (_encodedCache.TryGetValue(key, out string? cachedFile) &&
                    !string.IsNullOrEmpty(cachedFile) &&
                    File.Exists(cachedFile))
                {
                    filePath = cachedFile;
                    _delayCache.TryGetValue(key, out delay);
                    Log($"✅ Cache hit: {Path.GetFileName(cachedFile)}, delay={delay}");
                    return true;
                }
                Log($"❌ Cache miss");
                return false;
            }
        }
        private void ClearCache()
        {
            Log($"🗑️ ClearCache called");
            lock (_cacheLock)
            {
                int count = _encodedCache.Count;
                Log($"📊 Cache contains {count} entries");
                foreach (var file in _encodedCache.Values)
                {
                    try
                    {
                        if (File.Exists(file))
                        {
                            File.Delete(file);
                            Log($"🗑️ Deleted cached file: {file}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"⚠️ Failed to delete {file}: {ex.Message}");
                    }
                }
                _encodedCache.Clear();
                _decodedCache.Clear();
                _encoderSettingsReturnedByMICache.Clear();
                _delayCache.Clear();
                _currentCacheKey = null;
                Log("🗑️ Cache cleared");
            }
        }

        // MediaInfo
        private void EnsureMediaInfoDllExists()
        {
            string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MediaInfo.dll");

            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("Codec_Playground_H.MediaInfo.dll");
            if (stream == null)
            {
                Log("⚠️ MediaInfo.dll resource not found in assembly.");
                return;
            }

            using var md5 = MD5.Create();
            byte[] resourceHash = md5.ComputeHash(stream);
            stream.Seek(0, SeekOrigin.Begin);

            if (File.Exists(dllPath))
            {
                using var fileStream = File.OpenRead(dllPath);
                byte[] fileHash = md5.ComputeHash(fileStream);

                if (resourceHash.SequenceEqual(fileHash))
                {
                    Log($"✅ MediaInfo.dll already exists and matches embedded version.");
                    return;
                }

                Log($"🔄 MediaInfo.dll version mismatch. Updating...");
                try { File.Delete(dllPath); } catch { }
            }

            try
            {
                using var fileStream = File.Create(dllPath);
                stream.CopyTo(fileStream);
                Log($"✅ MediaInfo.dll extracted to {dllPath}");
            }
            catch (Exception ex)
            {
                Log($"❌ Failed to extract MediaInfo.dll: {ex.Message}");
                MessageBox.Show("Failed to extract MediaInfo.dll. Some features may not work.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private string GetEncoderSettingsFromFile(string filePath)
        {
            lock (_cacheLock)
            {
                if (_encoderSettingsReturnedByMICache.TryGetValue(filePath, out string? cachedSettings))
                {
                    return cachedSettings;
                }
            }

            string fileToOpen = filePath;
            bool isTempFile = false;

            try
            {
                if (filePath.Length > 259)
                {
                    fileToOpen = Path.Combine(_tempFolder, $"mi_{Guid.NewGuid():N}.mp3");
                    File.Copy(filePath, fileToOpen, true);
                    isTempFile = true;
                    Log($"📋 MediaInfo: using temp file due to long path ({filePath.Length} chars)");
                }

                var mediaInfo = new MediaInfo();

                if (mediaInfo.Open(fileToOpen) == 0)
                {
                    mediaInfo.Close();
                    return "MediaInfo failed to open file";
                }

                string encodingSettings = mediaInfo.Get(StreamKind.Audio, 0, "Encoded_Library_Settings");
                mediaInfo.Close();

                string result = !string.IsNullOrEmpty(encodingSettings) && encodingSettings != "N/A"
                    ? encodingSettings
                    : "Settings not stored in file";

                lock (_cacheLock)
                {
                    _encoderSettingsReturnedByMICache[filePath] = result;
                }

                return result;
            }
            catch (Exception ex)
            {
                Log($"⚠️ MediaInfo error: {ex.Message}");
                return "MediaInfo failed to open file";
            }
            finally
            {
                if (isTempFile && File.Exists(fileToOpen))
                {
                    try { File.Delete(fileToOpen); } catch { }
                }
            }
        }
        private void UpdateEncoderSettingsReturnedByMILabel()
        {
            if (!string.IsNullOrEmpty(_encodedFilePath) && File.Exists(_encodedFilePath))
            {
                string info = GetEncoderSettingsFromFile(_encodedFilePath);
                labelEncoderSettingsReturnedByMI.Text = info;

                bool isRealSettings = !string.IsNullOrEmpty(info) &&
                                      info != "MediaInfo failed to open file" &&
                                      info != "Settings not stored in file";

                labelEncoderSettingsReturnedByMI.ForeColor = isRealSettings ? Color.Green : Color.Gray;
                toolTip1.SetToolTip(labelEncoderSettingsReturnedByMI, info);
            }
            else
            {
                labelEncoderSettingsReturnedByMI.Text = "";
                labelEncoderSettingsReturnedByMI.ForeColor = Color.Gray;
            }
        }

        // Form closing and cleanup
        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            Log($"🔚 Form closing");
            try
            {
                SaveSettings();
                _encodingCts?.Cancel();
                _encodingCts?.Dispose();
                if (_currentEncodingTask != null && !_currentEncodingTask.IsCompleted)
                {
                    Log($"⏳ Waiting for encoding task to complete...");
                    try { _ = _currentEncodingTask.Wait(100); } catch { }
                }

                StopDualPlayback();

                for (int i = 0; i < 20; i++)
                {
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(50);
                    if (_waveOut == null) break;
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();

                CleanupTempFiles();
            }
            catch (Exception ex)
            {
                Log($"❌ Cleanup error: {ex.Message}");
                Debug.WriteLine($"Cleanup error: {ex.Message}");
            }
            Log($"🔚 Form closed");
        }
        private void CleanupTempFiles()
        {
            Log($"🗑️ CleanupTempFiles started");
            List<string> filesToDelete = [];
            try
            {
                lock (_cacheLock)
                {
                    filesToDelete.AddRange(_encodedCache.Values);
                    _encodedCache.Clear();
                    _decodedCache.Clear();
                    _delayCache.Clear();
                }

                if (Directory.Exists(_tempFolder))
                {
                    var tempFiles = Directory.GetFiles(_tempFolder, "*.mp3");
                    Log($"📊 Found {tempFiles.Length} MP3 files in temp");
                    foreach (var file in tempFiles)
                        if (!filesToDelete.Contains(file))
                            filesToDelete.Add(file);

                    var preEncodeFiles = Directory.GetFiles(_tempFolder, "preencode_*.wav");
                    Log($"📊 Found {preEncodeFiles.Length} pre-encode WAV files in temp");
                    foreach (var file in preEncodeFiles)
                        if (!filesToDelete.Contains(file))
                            filesToDelete.Add(file);
                }

                Log($"📊 Total files to delete: {filesToDelete.Count}");

                foreach (string? file in filesToDelete.Distinct())
                {
                    if (string.IsNullOrEmpty(file) || !File.Exists(file)) continue;

                    bool deleted = false;
                    for (int attempt = 0; attempt < 5 && !deleted; attempt++)
                    {
                        try
                        {
                            File.Delete(file);
                            deleted = true;
                            Log($"🗑️ Temp file deleted: {Path.GetFileName(file)}");
                        }
                        catch (IOException)
                        {
                            if (attempt < 4)
                            {
                                Log($"⏳ File busy, retry {attempt + 1}/5: {Path.GetFileName(file)}");
                                System.Threading.Thread.Sleep(200);
                                Application.DoEvents();
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"❌ Failed to delete {file}: {ex.Message}");
                            break;
                        }
                    }

                    if (!deleted && File.Exists(file))
                    {
                        Log($"⚠️ Could not delete after 5 attempts: {Path.GetFileName(file)}");
                        try
                        {
                            string deleteMarker = file + ".delete";
                            if (!File.Exists(deleteMarker))
                                File.Create(deleteMarker).Close();
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Cleanup error: {ex.Message}");
            }
            Log($"🗑️ CleanupTempFiles completed");
        }

        // Update check
        private bool _isUpdateInProgress = false;
        private const string REPO_OWNER = "hat3k";
        private const string REPO_NAME = "Codec-Playground-H";
        private const string RELEASES_URL = $"https://github.com/{REPO_OWNER}/{REPO_NAME}/releases";
        private const string API_URL = $"https://api.github.com/repos/{REPO_OWNER}/{REPO_NAME}/releases/latest";

        private async Task CheckForUpdatesAsync()
        {
            if (_isUpdateInProgress) return;

            try
            {
                _isUpdateInProgress = true;
                ShowNotification("⏳ Checking for updates...", true, 0);
                Log($"🔍 Checking for updates... Current: {APP_VERSION}");

                var release = await GetLatestReleaseAsync();
                if (release == null)
                {
                    Log("ℹ️ No updates available or API error");
                    ShowNotification("❌ Update check failed", false);
                    return;
                }

                if (!release.IsNewerThan(APP_VERSION))
                {
                    Log($"ℹ️ Latest version {release.TagName} is not newer than current {APP_VERSION}");
                    ShowNotification("✅ No updates available", true);
                    return;
                }

                Log($"📢 New version {release.TagName} available!");

                Invoke(() => labelNoUpdates.Visible = false);

                DialogResult result = MessageBox.Show(
                    $"New version {release.TagName} is available!\n" +
                    $"Current: {APP_VERSION}\n\n" +
                    "Do you want to open the releases page to download it?",
                    "Update Available",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = RELEASES_URL,
                        UseShellExecute = true
                    });
                    Log($"🌐 Opened releases page: {RELEASES_URL}");
                }
                else
                {
                    ShowNotification($"ℹ️ Update {release.TagName} available", true, 10000);
                    Log($"ℹ️ User postponed update to {release.TagName}");
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Update check failed: {ex.Message}");
                ShowNotification($"❌ Update check failed", false);
            }
            finally
            {
                _isUpdateInProgress = false;
            }
        }
        private async Task<GitHubRelease?> GetLatestReleaseAsync()
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("Codec-Playground-H/1.0");
                httpClient.Timeout = TimeSpan.FromSeconds(10);

                var response = await httpClient.GetAsync(API_URL);
                if (!response.IsSuccessStatusCode)
                {
                    Log($"⚠️ GitHub API error: {response.StatusCode}");
                    return null;
                }

                string json = await response.Content.ReadAsStringAsync();
                var release = JsonSerializer.Deserialize<GitHubRelease>(json);

                if (release == null || string.IsNullOrEmpty(release.TagName))
                {
                    Log("⚠️ Failed to parse GitHub response");
                    return null;
                }

                Log($"📊 Latest release: {release.TagName} (published: {release.PublishedAt:yyyy-MM-dd})");
                return release;
            }
            catch (HttpRequestException ex)
            {
                Log($"⚠️ Network error: {ex.Message}");
                return null;
            }
            catch (TaskCanceledException)
            {
                Log("⚠️ Request timeout");
                return null;
            }
            catch (Exception ex)
            {
                Log($"⚠️ Failed to get latest release: {ex.Message}");
                return null;
            }
        }
        private void ShowNotification(string message, bool isSuccess = true, int durationMs = 3000)
        {
            if (InvokeRequired)
            {
                Invoke(() => ShowNotification(message, isSuccess, durationMs));
                return;
            }

            labelNoUpdates.Text = message;
            labelNoUpdates.ForeColor = isSuccess ? Color.Green : Color.Red;
            labelNoUpdates.Visible = true;

            if (durationMs == 0)
            {
                _notificationTimer?.Stop();
                _notificationTimer?.Dispose();
                _notificationTimer = null;
                return;
            }

            _notificationTimer?.Stop();
            _notificationTimer?.Dispose();

            _notificationTimer = new System.Windows.Forms.Timer
            {
                Interval = durationMs
            };
            _notificationTimer.Tick += (s, e) =>
            {
                labelNoUpdates.Visible = false;
                _notificationTimer?.Stop();
                _notificationTimer?.Dispose();
                _notificationTimer = null;
            };
            _notificationTimer.Start();
        }
        private void CheckBoxCheckForUpdates_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxCheckForUpdates.Checked)
            {
                _ = Task.Delay(1000).ContinueWith(_ => CheckForUpdatesAsync());
            }
        }

        public class AppSettings
        {
            public List<string> EncoderPaths { get; set; } = [];
            public List<string> AudioFilePaths { get; set; } = [];
            public string? SelectedEncoderPath { get; set; }
            public string? SelectedAudioFilePath { get; set; }
            public bool LoopPlayback { get; set; } = false;
            public Form1.PlayMode CurrentPlayMode { get; set; } = Form1.PlayMode.Original;
            public EncoderSettings EncoderSettings { get; set; } = new();
            public WindowSettings Window { get; set; } = new();
            public ListViewSettings EncoderListView { get; set; } = new();
            public ListViewSettings AudioListView { get; set; } = new();
            public UserPreset UserPresets { get; set; } = new();
            public bool HiddenModeMP3Off { get; set; } = false;
            public bool HiddenUserPresetOff { get; set; } = true;
            public bool CheckForUpdates { get; set; } = false;
        }

        public class ListViewSettings
        {
            public List<int> ColumnWidths { get; set; } = [];
        }

        public class EncoderSettings
        {
            // MP3 settings
            public bool ModeCBR_MP3 { get; set; } = true;
            public bool ModeABR_MP3 { get; set; } = false;
            public bool ModeVBR_MP3 { get; set; } = false;
            public int CBRValue_MP3 { get; set; } = 16;
            public int ABRValue_MP3 { get; set; } = 16;
            public int VBRValue_MP3 { get; set; } = 0;
            public bool UseQuality_MP3 { get; set; } = false;
            public int QualityValue_MP3 { get; set; } = 0;
            public bool UseChannelModes_MP3 { get; set; } = false;
            public bool ChannelJointStereo_MP3 { get; set; } = true;
            public bool ChannelStereo_MP3 { get; set; } = false;
            public bool ChannelMono_MP3 { get; set; } = false;
            public string LabelCBR_MP3 { get; set; } = "320";
            public string LabelABR_MP3 { get; set; } = "320";
            public string LabelVBR_MP3 { get; set; } = "V0";
            public string LabelQuality_MP3 { get; set; } = "q0";

            // Balance of Mix mode
            public int MixBalanceValue { get; set; } = 50;
        }

        public class UserPreset
        {
            public bool UserPreset1 { get; set; } = false;
            public string UserPreset1Name { get; set; } = "";
            public string UserPreset1CommandLineArgs { get; set; } = "";
            public bool UserPreset2 { get; set; } = false;
            public string UserPreset2Name { get; set; } = "";
            public string UserPreset2CommandLineArgs { get; set; } = "";
            public bool UserPreset3 { get; set; } = false;
            public string UserPreset3Name { get; set; } = "";
            public string UserPreset3CommandLineArgs { get; set; } = "";
            public bool UserPreset4 { get; set; } = false;
            public string UserPreset4Name { get; set; } = "";
            public string UserPreset4CommandLineArgs { get; set; } = "";
            public bool UserPreset5 { get; set; } = false;
            public string UserPreset5Name { get; set; } = "";
            public string UserPreset5CommandLineArgs { get; set; } = "";
            public bool UserPreset6 { get; set; } = false;
            public string UserPreset6Name { get; set; } = "";
            public string UserPreset6CommandLineArgs { get; set; } = "";
        }

        public class WindowSettings
        {
            public int Width { get; set; } = 1008;
            public int Height { get; set; } = 753;
            public int X { get; set; } = 100;
            public int Y { get; set; } = 100;
            public bool Maximized { get; set; } = false;
        }
    }
}