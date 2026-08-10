# Codec Playground-H

A tool for real-time testing of encoder settings.

Change encoder parameters on the fly and hear the difference immediately - no interruptions.

<img width="860" height="679" alt="Untitled-2 (3)" src="https://github.com/user-attachments/assets/6746abe7-805a-41da-b7df-a058ea7d6133" />

---
![GitHub all releases](https://img.shields.io/github/downloads/hat3k/Codec-Playground-H/total)

## What it does

- Load WAV or FLAC files

- Encode them using LAME with your chosen settings
- **Seamless re-encode** - tweak parameters while playback continues
- Instantly hear how each setting affects the sound
- Compare original vs encoded in real-time

> **Currently supports MP3 encoding only** (via LAME). Other codecs may be added in the future.

---

## First Steps

1. **Get LAME encoder** - download `lame.exe` from [https://lame.sourceforge.io/](https://lame.sourceforge.io/)
2. **Add encoder** - drag & drop `lame.exe` into the Encoders list
3. **Add audio files** - drag & drop WAV or FLAC files into the Audio Files list
4. **Press Play** and start tweaking settings

---

## Recommended Workflow

Here's a simple way to use this tool effectively:

1. **Start with Encoded mode** - pick a setting (e.g., CBR 320) and just listen. Does it sound good? Does it sound different from Original? Switch back and forth between Original and Encoded. Your ears will tell you if something is off.

2. **Try different settings** - switch to VBR V0, CBR 128, VBR V6. Listen to the same section of the track. Which one sounds best to you? Which one feels like it's "missing something"?

3. **Use presets for quick A/B** - save your favorite settings as presets (1-6). Then use `Ctrl+Shift+2-6` to randomly switch between them. Can you tell which is which? This is the real test.

4. **Optionally, explore Difference mode** - this mode is a diagnostic tool. It plays only what the encoder removed. If you want to understand *why* one setting sounds better than another, Difference mode can reveal the specific artifacts and losses. But remember: it's a tool for understanding, not the final judge.

5. **Trust your ears in Encoded mode** - Difference mode shows you what's missing, but the only thing that matters is how the Encoded mode sounds to you. If it sounds good, it is good - regardless of what any measurement or difference signal says.

---

## Listening Modes

| Mode | What you hear | When to use |
|------|---------------|-------------|
| **Original** | Unmodified source audio | Reference for comparison |
| **Encoded** | MP3-encoded version with current settings | Main listening mode - this is what matters |
| **Mix** | Blend of original and encoded with adjustable balance | Hear the transition between original and encoded |
| **Difference** | Only what was lost during encoding (artifacts) | Diagnostic tool - understand what the encoder removes |

---

### 🎧 Difference Mode - What It Is and When to Use It

**Technically:** Original signal minus encoded signal, played at half volume.

**What you hear:**
- **The "loss"** - everything the encoder removed or distorted
- **High-frequency content** - cymbals, hi-hats, sibilance, air
- **Transients** - attack of drums, plucks, percussive sounds
- **Spatial information** - stereo width, reverb tails, panning

**How to interpret what you hear:**

| If you hear... | It means... |
|----------------|-------------|
| **Almost nothing** (very quiet) | The encoder is doing a great job. The original and encoded signals are nearly identical. |
| **Clear music with vocals** | The encoder is losing a lot. Try higher bitrate or better VBR setting. |
| **"Swirling" or "watery" sounds** | The encoder is struggling with complex high-frequency content (common at low bitrates). |
| **Metallic ringing or warbling** | The encoder is introducing audible artifacts. Consider a different setting. |
| **Loss of stereo width** | The encoder is collapsing the stereo image. Check joint stereo settings. |
| **Only high-frequency "shimmer"** | The encoder is preserving transients but compressing the rest. Usually a good sign! |

**Remember:** Difference mode is a diagnostic tool, not the final verdict. Always make your decision by listening to the **Encoded** mode. If it sounds good to you, it's the right setting - regardless of what Difference mode shows.

---

## The Only Rule That Matters

**Trust your ears. Nothing else.**

No spectrogram, no bitrate chart, no "scientific" measurement can tell you what sounds good to *you*.

- A spectrogram might show loss of high frequencies - but if you can't hear it, does it matter?
- A bitrate meter might say 320 kbps is "better" than V0 - but your ears might disagree.
- Someone else's "golden ears" might prefer settings that sound terrible to you.

**Your ears are the only thing that matters.**

This tool exists to help you *hear* the difference, not to prove anything with numbers. Use it to discover what sounds right to you.

---

## Encoder Settings

- **CBR** (8-320 kbps) - constant bitrate
- **ABR** (8-320 kbps) - average bitrate
- **VBR** (V0-V9) - variable bitrate
- **Quality (q0-q9)** - algorithm quality selection
- **Channel Modes** - Joint Stereo / Stereo / Mono

---

## User Presets

Save up to 6 presets with your favorite settings:

1. Tweak encoder settings in the UI
2. Click the 💾 button next to any preset number
3. The current settings are saved as a command line string
4. Click the preset radio button or use `Ctrl+1-6` to apply

### A/B Testing Shortcuts

- `Ctrl+Shift+2` - randomly pick between presets 1-2
- `Ctrl+Shift+3` - randomly pick between presets 1-3
- ... up to `Ctrl+Shift+6`

Perfect for blind testing - you hear the difference without knowing which preset is active.

---

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `Ctrl+1-6` | Select preset 1-6 |
| `Ctrl+Shift+2-6` | Random pick from presets 1-N |
| `Ctrl+R` | Random audio file |

---

## Why this exists

After years of silence, the LAME development team resumed work on the encoder. New versions brought fresh optimizations and improvements. I wanted to explore these changes and truly *hear* what they do - not just read changelogs.

This tool was built to make that exploration immediate and interactive.

- **Hear the difference instantly** - change `-V 0` to `-V 6` while the music plays
- **Discover what each parameter actually does**
- **Find the optimal settings** for your ears and your files

---

## Credits

- [LAME](https://lame.sourceforge.io/) - the legendary MP3 encoder. Thank you for keeping the fire alive.
- [NAudio](https://github.com/naudio/NAudio) - audio library for .NET
- [MathNet.Numerics](https://www.mathdotnet.com/) - numerical computing

---

## License

MIT

---

**Hear the difference. Instantly.**
**Trust your ears. Always.**
