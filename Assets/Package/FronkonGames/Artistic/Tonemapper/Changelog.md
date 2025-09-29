# Changelog
All notable changes to this package will be documented in this file.

## [2.2.0] - 02-09-2025

# Added
- PBR Neutral tone mapper by Khronos Group - Designed for PBR workflows to maintain material accuracy.
- Schlick tone mapper - Simple rational function, very fast and efficient.
- Drago adaptive logarithmic mapping - Bias parameter for local adaptation and excellent dynamic range compression.
- Split Toning controls - Separate color grading for highlights and shadows with balance control for cinematic color separation.
- Selective Color Adjustments - Fine-tune specific color ranges (Reds, Yellows, Greens, Cyans, Blues, Magentas, Whites, Neutrals, Blacks) with CMYB adjustments for professional color correction.
- Advanced Vibrance Controls - Sophisticated vibrance adjustments with color range detection, protection mechanisms, and specialized controls for skin tones, sky, foliage, warmth, and coolness.
- Channel Mixer controls - Advanced per-channel color mixing for creative color manipulation and correction.
- Tone Curve controls - Black Point, White Point, Toe Strength, and Shoulder Strength for cinematic S-curve adjustments.
- White Balance controls - Temperature and Tint adjustments for professional color correction.

# Fix
- Improved VR support for Unity 6.


## [2.1.1] - 22-04-2025

# Fix
- macOS Metal support (thanks to Sergey Akopkokhyants).

## [2.1.0] - 08-03-2025

# Added
- Support for effects in multiples Renderers.
- 'Affect the Scene View' added to Unity 6.

# Removed
- Removed Instance.settings, use .Instance.settings

# Fix
- Camera 'Post Processing' checkbox fixed.
- Errors when domain reload is disabled.
- Memory leak in Unity 2022.3.
- Documentation URL fixed.

## [2.0.0] - 11-11-2024

# Added
- Support for Unity 6000.0.

# Changed
- Performance improvements.
- Removed support for Unity 2021.3.

# Fix
- Small fixes.

## [1.1.0] - 15-07-2024

# Changed
- Removed the AddRenderFeature() and RemoveRenderFeature() from the effect that damaged the configuration file.
- Performance improvements.

# Fix
- Small fixes.

## [1.0.2] - 02-06-2024

# Changed
- New website and documentation.

## [1.0.1] - 10-02-2024

# Fix
- 'Remap' function renamed to avoid collisions.

## [1.0.0] - 20-09-2023

- Initial release.