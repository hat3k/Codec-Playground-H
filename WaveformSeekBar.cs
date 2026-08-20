using System.ComponentModel;

namespace Codec_Playground_H
{
    /// <summary>
    /// Custom waveform seek bar control.
    /// Displays audio waveform and allows seeking by clicking or dragging.
    /// </summary>
    public class WaveformSeekBar : Control
    {
        // === Configuration constants ===
        private const int MinBarWidth = 1;
        private const int MaxBarWidth = 6;
        private const int BarGap = 1;
        private const int MinPeakCount = 20;
        private const int MaxPeakCount = 2000;

        // === Audio data ===
        private float[]? _samples;
        private int _channels;
        private float[]? _peaks;
        private int _peakCount;
        private CancellationTokenSource? _peakCalcCts;

        // === Playback state ===
        private float _position;
        private bool _isDragging;
        private bool _isHovering;
        private int _hoverX;

        // === Appearance ===
        private readonly Color _hoverLineColor = Color.FromArgb(180, 255, 255, 255);

        // === Events ===
        /// <summary>Raised when the user starts a seek operation (mouse down).</summary>
        public event EventHandler? SeekStarted;

        /// <summary>Raised continuously during seeking (mouse down or drag).</summary>
        public event EventHandler? SeekRequested;

        /// <summary>Raised when the seek operation completes (mouse up).</summary>
        public event EventHandler? SeekCompleted;

        // === Properties ===
        /// <summary>Current playback position from 0.0 (start) to 1.0 (end).</summary>
        [Category("Waveform")]
        [Description("Current playback position from 0.0 (start) to 1.0 (end).")]
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float Position
        {
            get => _position;
            set
            {
                float clamped = Math.Clamp(value, 0f, 1f);
                if (Math.Abs(_position - clamped) < 0.0001f)
                {
                    return;
                }

                _position = clamped;
                Invalidate();
            }
        }

        /// <summary>Color of the played portion of the waveform.</summary>
        [Category("Waveform")]
        [Description("Color of the played portion of the waveform.")]
        [DefaultValue(typeof(Color), "255, 85, 0")]
        public Color PlayedColor
        {
            get;
            set { field = value; Invalidate(); }
        } = Color.FromArgb(255, 85, 0);

        /// <summary>Color of the unplayed portion of the waveform.</summary>
        [Category("Waveform")]
        [Description("Color of the unplayed portion of the waveform.")]
        [DefaultValue(typeof(Color), "70, 70, 70")]
        public Color UnplayedColor
        {
            get;
            set { field = value; Invalidate(); }
        } = Color.FromArgb(70, 70, 70);

        /// <summary>Color of the vertical playback position line.</summary>
        [Category("Waveform")]
        [Description("Color of the vertical playback position line.")]
        [DefaultValue(typeof(Color), "White")]
        public Color PositionLineColor
        {
            get;
            set { field = value; Invalidate(); }
        } = Color.White;

        /// <summary>Whether to draw a border around the waveform.</summary>
        [Category("Waveform")]
        [Description("Whether to draw a border around the waveform.")]
        [DefaultValue(true)]
        public bool ShowBorder
        {
            get;
            set
            {
                if (field == value)
                {
                    return;
                }

                field = value;
                Invalidate();
            }
        } = true;

        /// <summary>The color of the waveform border.</summary>
        [Category("Waveform")]
        [Description("The color of the waveform border.")]
        [DefaultValue(typeof(Color), "60, 60, 60")]
        public Color BorderColor
        {
            get;
            set
            {
                if (field == value)
                {
                    return;
                }

                field = value;
                Invalidate();
            }
        } = Color.FromArgb(60, 60, 60);

        /// <summary>Whether the waveform is currently being calculated.</summary>
        [Category("Waveform")]
        [Description("Whether the waveform is currently being calculated.")]
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsCalculating { get; private set; }

        // === Constructor ===
        public WaveformSeekBar()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);
            BackColor = Color.Transparent;
            Height = 27;
        }

        // === Public methods ===
        /// <summary>
        /// Sets the audio data and starts peak calculation in background.
        /// </summary>
        /// <param name="samples">Interleaved audio samples.</param>
        /// <param name="channels">Number of audio channels.</param>
        public void SetAudioData(float[]? samples, int channels)
        {
            _peakCalcCts?.Cancel();
            _peakCalcCts?.Dispose();
            _peakCalcCts = new CancellationTokenSource();

            _samples = samples;
            _channels = Math.Max(1, channels);
            _peaks = null;
            _peakCount = 0;
            _position = 0f;

            if (samples == null || samples.Length == 0)
            {
                IsCalculating = false;
                Invalidate();
                return;
            }

            IsCalculating = true;
            CancellationToken token = _peakCalcCts.Token;
            _ = Task.Run(() => CalculatePeaks(token), token);
            Invalidate();
        }

        /// <summary>Clears the waveform data.</summary>
        public void ClearWaveform()
        {
            _peakCalcCts?.Cancel();
            _samples = null;
            _peaks = null;
            _peakCount = 0;
            IsCalculating = false;
            _position = 0f;
            Invalidate();
        }

        // === Peak calculation ===
        private void CalculatePeaks(CancellationToken token)
        {
            try
            {
                if (_samples == null || _samples.Length == 0)
                {
                    IsCalculating = false;
                    return;
                }

                int totalFrames = _samples.Length / _channels;
                if (totalFrames <= 0)
                {
                    IsCalculating = false;
                    return;
                }

                // Determine peak count based on control width
                int barUnit = MinBarWidth + BarGap;
                int targetPeakCount = Width > 0 ? Width / barUnit : 500;
                targetPeakCount = Math.Clamp(targetPeakCount, MinPeakCount, MaxPeakCount);

                int samplesPerPeak = totalFrames / targetPeakCount;
                if (samplesPerPeak < 1)
                {
                    samplesPerPeak = 1;
                }

                float[] peaks = new float[targetPeakCount];

                for (int i = 0; i < targetPeakCount; i++)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    int startFrame = i * samplesPerPeak;
                    int endFrame = Math.Min(startFrame + samplesPerPeak, totalFrames);

                    float maxVal = 0f;
                    for (int frame = startFrame; frame < endFrame; frame++)
                    {
                        int baseIdx = frame * _channels;
                        for (int ch = 0; ch < _channels; ch++)
                        {
                            int idx = baseIdx + ch;
                            if (idx < _samples.Length)
                            {
                                float abs = Math.Abs(_samples[idx]);
                                if (abs > maxVal)
                                {
                                    maxVal = abs;
                                }
                            }
                        }
                    }
                    peaks[i] = maxVal;
                }

                // Normalize peaks to 0..1 range
                float maxPeak = 0f;
                for (int i = 0; i < peaks.Length; i++)
                {
                    if (peaks[i] > maxPeak)
                    {
                        maxPeak = peaks[i];
                    }
                }

                if (maxPeak > 0f)
                {
                    for (int i = 0; i < peaks.Length; i++)
                    {
                        peaks[i] /= maxPeak;
                    }
                }

                if (!token.IsCancellationRequested)
                {
                    _peaks = peaks;
                    _peakCount = targetPeakCount;
                    IsCalculating = false;

                    if (IsHandleCreated && !IsDisposed)
                    {
                        _ = BeginInvoke(Invalidate);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelled
            }
            catch (Exception)
            {
                IsCalculating = false;
            }
        }

        // === Rendering ===
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            if (IsCalculating)
            {
                DrawLoadingState(g);
                return;
            }

            if (_peaks == null || _peakCount == 0)
            {
                DrawEmptyState(g);
                return;
            }

            DrawWaveform(g);
            DrawHoverLine(g);
            DrawPositionLine(g);
            DrawBorder(g);
        }

        private void DrawWaveform(Graphics g)
        {
            if (_peaks == null || _peakCount == 0)
            {
                return;
            }

            int w = ClientRectangle.Width;
            int h = ClientRectangle.Height;
            int centerY = h / 2;
            float positionX = _position * w;

            // Calculate bar dimensions
            int totalBars = _peakCount;
            float barWidthFloat = (float)w / totalBars;

            for (int i = 0; i < totalBars; i++)
            {
                // Calculate exact start/end coordinates using rounding
                // This guarantees bars are adjacent with no gaps
                int xStart = (int)Math.Round(i * barWidthFloat);
                int xEnd = (int)Math.Round((i + 1) * barWidthFloat);
                int barWidth = Math.Max(xEnd - xStart, 1);

                // Skip bars outside visible area
                if (xEnd < 0 || xStart > w)
                {
                    continue;
                }

                float peak = _peaks[i];
                int barHeight = Math.Max((int)(peak * (centerY - 2)), 1);

                bool isPlayed = xStart + (barWidth / 2) < positionX;
                Color barColor = isPlayed ? PlayedColor : UnplayedColor;

                using SolidBrush brush = new(barColor);
                g.FillRectangle(brush, xStart, centerY - barHeight, barWidth, barHeight * 2);
            }
        }

        private void DrawPositionLine(Graphics g)
        {
            if (_position <= 0f)
            {
                return;
            }

            int w = ClientRectangle.Width;
            int h = ClientRectangle.Height;
            float x = _position * w;

            using Pen pen = new(PositionLineColor, 2f);
            g.DrawLine(pen, x, 0, x, h);
        }

        private void DrawHoverLine(Graphics g)
        {
            if (!_isHovering || _isDragging)
            {
                return;
            }

            int h = ClientRectangle.Height;
            using Pen pen = new(_hoverLineColor, 1f);
            g.DrawLine(pen, _hoverX, 0, _hoverX, h);
        }

        private void DrawLoadingState(Graphics g)
        {
            Rectangle rect = ClientRectangle;
            using SolidBrush brush = new(Color.FromArgb(100, 100, 100));
            using Font font = new("Segoe UI", 9f);
            using StringFormat sf = new()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString("Calculating waveform...", font, brush, rect, sf);
        }

        private void DrawEmptyState(Graphics g)
        {
            Rectangle rect = ClientRectangle;
            using SolidBrush brush = new(Color.FromArgb(80, 80, 80));
            using Font font = new("Segoe UI", 9f);
            using StringFormat sf = new()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString("", font, brush, rect, sf);
        }

        private void DrawBorder(Graphics g)
        {
            // Skip drawing border if disabled in designer
            if (!ShowBorder)
            {
                return;
            }

            using Pen pen = new(BorderColor, 1f);
            g.DrawRectangle(pen, 0, 0, ClientRectangle.Width - 1, ClientRectangle.Height - 1);
        }

        // === Mouse handling ===
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            _isDragging = true;
            UpdatePositionFromMouse(e.X);
            SeekStarted?.Invoke(this, EventArgs.Empty);
            SeekRequested?.Invoke(this, EventArgs.Empty);
            Capture = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            _hoverX = e.X;

            if (_isDragging)
            {
                UpdatePositionFromMouse(e.X);
                SeekRequested?.Invoke(this, EventArgs.Empty);
            }

            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            if (_isDragging)
            {
                _isDragging = false;
                UpdatePositionFromMouse(e.X);
                SeekCompleted?.Invoke(this, EventArgs.Empty);
                Capture = false;
                Invalidate();
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovering = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovering = false;
            Invalidate();
        }

        private void UpdatePositionFromMouse(int mouseX)
        {
            int w = ClientRectangle.Width;
            if (w <= 0)
            {
                return;
            }

            _position = Math.Clamp((float)mouseX / w, 0f, 1f);
            Invalidate();
        }

        // === Resize handling ===
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            // Recalculate peaks if we have data
            if (_samples != null && _samples.Length > 0 && !IsCalculating)
            {
                _peakCalcCts?.Cancel();
                _peakCalcCts?.Dispose();
                _peakCalcCts = new CancellationTokenSource();
                CancellationToken token = _peakCalcCts.Token;
                IsCalculating = true;
                _ = Task.Run(() => CalculatePeaks(token), token);
            }
            Invalidate();
        }

        // === Dispose ===
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _peakCalcCts?.Cancel();
                _peakCalcCts?.Dispose();
                _samples = null;
                _peaks = null;
            }
            base.Dispose(disposing);
        }
    }
}