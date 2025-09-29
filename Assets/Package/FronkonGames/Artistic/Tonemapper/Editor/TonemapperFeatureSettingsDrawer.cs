////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Martin Bustos @FronkonGames <fronkongames@gmail.com>. All rights reserved.
//
// THIS FILE CAN NOT BE HOSTED IN PUBLIC REPOSITORIES.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
using UnityEngine;
using UnityEditor;
using static FronkonGames.Artistic.Tonemapper.Inspector;

namespace FronkonGames.Artistic.Tonemapper.Editor
{
  /// <summary> Artistic Tonemapper inspector. </summary>
  [CustomPropertyDrawer(typeof(Tonemapper.Settings))]
  public class TonemapperFeatureSettingsDrawer : Drawer
  {
    private Tonemapper.Settings settings;

    protected override void ResetValues() => settings?.ResetDefaultValues();

    protected override void InspectorGUI()
    {
      settings ??= GetSettings<Tonemapper.Settings>();

      /////////////////////////////////////////////////
      // Common.
      /////////////////////////////////////////////////
      settings.intensity = Slider("Intensity", "Controls the intensity of the effect [0, 1]. Default 0.", settings.intensity, 0.0f, 1.0f, 1.0f);

      /////////////////////////////////////////////////
      // Tonemapper.
      /////////////////////////////////////////////////
      Separator();

      settings.tonemapperOperator = (Tonemapper.Operators)EnumPopup("Operator", "Tonemapper operator", settings.tonemapperOperator, Tonemapper.Operators.Linear);
      IndentLevel++;
      switch (settings.tonemapperOperator)
      {
        case Tonemapper.Operators.Linear: break;
        case Tonemapper.Operators.Logarithmic:
        case Tonemapper.Operators.WhiteLumaReinhard:
        case Tonemapper.Operators.Hejl2015:
        case Tonemapper.Operators.Clamping:
          settings.whiteLevel = Slider("White level", "White level exposure [0 - 5]. Default 1.", settings.whiteLevel, 0.0f, 5.0f, 1.0f);
          break;
        case Tonemapper.Operators.FilmicAldridge:
          settings.cutOff = Slider("Cutoff", "Cutoff to black [0 - 0.5]. Default 0.025.", settings.cutOff, 0.0f, 0.5f, 0.025f);
          break;
        case Tonemapper.Operators.WatchDogs:
          settings.linearWhite = Slider("Linear white", "Linear white. Used in WatchDogs [0.5 - 2]. Default 1.5.", settings.linearWhite, 0.5f, 2.0f, 1.5f);
          settings.linearColor = Slider("Linear color", "Linear color. Used in WatchDogs [0.5 - 2]. Default 1.5.", settings.linearColor, 0.5f, 2.0f, 1.5f);
          break;
      }
      IndentLevel--;

      Separator();

      if (Foldout("Color Filter") == true)
      {
        settings.colorFilter = ColorField("Color filter", "Color tint. Default White.", settings.colorFilter, Color.white);
        IndentLevel++;
        settings.exposure = Slider("Exposure", "Exposure, affects the overal brightness [0, 10]. Default 0.", settings.exposure, 0.0f, 10.0f, 0.0f);
        settings.temperature = Slider("Temperature", "White balance temperature [-1, 1]. Negative values are cooler (blue), positive values are warmer (yellow). Default 0.", settings.temperature, -1.0f, 1.0f, 0.0f);
        settings.tint = Slider("Tint", "White balance tint [-1, 1]. Negative values are more green, positive values are more magenta. Default 0.", settings.tint, -1.0f, 1.0f, 0.0f);
        settings.vibrance = Slider("Vibrance", "Color vibrance [-1, 1]. Default 0.", settings.vibrance, -1.0f, 1.0f, 0.0f);
        IndentLevel++;
        settings.vibranceBalance = Vector3Field("Balance", "Color vibrance balance. Default (1, 1, 1).", settings.vibranceBalance, Vector3.one);
        IndentLevel--;
        settings.contrast = Slider("Contrast", "Contrast [0.0, 10.0]. Default 1.", settings.contrast, 0.0f, 10.0f, 1.0f);
        IndentLevel++;
        settings.contrastMidpoint = Slider("Midpoint", "Log of linear constrast midpoint. Default 0.18.", settings.contrastMidpoint, -5.0f, 5.0f, 0.18f);
        IndentLevel--;
        IndentLevel--;
      }

      Separator();

      if (Foldout("Lift, Midtones and Gain") == true)
      {
        settings.lift = ColorField("Lift", "Adjust shadows for RGB. Default White.", settings.lift, Color.white);
        IndentLevel++;
        settings.liftBright = Slider("Bright", "Lift bright [0, 2]. Default 1.", settings.liftBright, 0.0f, 2.0f, 1.0f);
        IndentLevel--;

        settings.midtones = ColorField("Midtones", "Adjust midtones for RGB. Default White.", settings.midtones, Color.white);
        IndentLevel++;
        settings.midtonesBright = Slider("Bright", "Midtones bright [0, 2]. Default 1.", settings.midtonesBright, 0.0f, 2.0f, 1.0f);
        IndentLevel--;

        settings.gain = ColorField("Gain", "Adjust highlights for RGB. Default White.", settings.gain, Color.white);
        IndentLevel++;
        settings.gainBright = Slider("Bright", "Gain bright [0, 2]. Default 1.", settings.gainBright, 0.0f, 2.0f, 1.0f);
        IndentLevel--;
      }

      /////////////////////////////////////////////////
      // Color.
      /////////////////////////////////////////////////
      Separator();

      if (Foldout("Tone Curve") == true)
      {
        IndentLevel++;
        settings.blackPoint = Slider("Black Point", "Tone curve black point [0.0, 0.5]. Lifts the blacks for a faded film look. Default 0.", settings.blackPoint, 0.0f, 0.5f, 0.0f);
        settings.whitePoint = Slider("White Point", "Tone curve white point [0.5, 1.0]. Lowers the whites for a vintage look. Default 1.", settings.whitePoint, 0.5f, 1.0f, 1.0f);
        settings.toeStrength = Slider("Toe Strength", "Tone curve toe strength [0.0, 1.0]. Controls shadow rolloff smoothness. Default 0.", settings.toeStrength, 0.0f, 1.0f, 0.0f);
        settings.shoulderStrength = Slider("Shoulder Strength", "Tone curve shoulder strength [0.0, 1.0]. Controls highlight rolloff smoothness. Default 0.", settings.shoulderStrength, 0.0f, 1.0f, 0.0f);
        IndentLevel--;
      }

      Separator();

      if (Foldout("Channel Mixer") == true)
      {
        IndentLevel++;
        settings.redChannelMixer = Vector3Field("Red Output", "Channel mixer red output [-2, 2]. Controls how much R, G, B input influences red output. Default (1, 0, 0).", settings.redChannelMixer, new Vector3(1, 0, 0));
        settings.greenChannelMixer = Vector3Field("Green Output", "Channel mixer green output [-2, 2]. Controls how much R, G, B input influences green output. Default (0, 1, 0).", settings.greenChannelMixer, new Vector3(0, 1, 0));
        settings.blueChannelMixer = Vector3Field("Blue Output", "Channel mixer blue output [-2, 2]. Controls how much R, G, B input influences blue output. Default (0, 0, 1).", settings.blueChannelMixer, new Vector3(0, 0, 1));
        IndentLevel--;
      }

      Separator();

      if (Foldout("Split Toning") == true)
      {
        IndentLevel++;
        settings.highlightTint = ColorField("Highlight Tint", "Split toning highlight color. Color applied to highlights. Default White.", settings.highlightTint, Color.white);
        settings.shadowTint = ColorField("Shadow Tint", "Split toning shadow color. Color applied to shadows. Default White.", settings.shadowTint, Color.white);
        settings.splitBalance = Slider("Balance", "Split toning balance [-1, 1]. Controls the balance between shadow and highlight tinting. Negative favors shadows, positive favors highlights. Default 0.", settings.splitBalance, -1.0f, 1.0f, 0.0f);
        IndentLevel--;
      }

      Separator();

      if (Foldout("Selective Color") == true)
      {
        settings.redsAdjustment = Vector4Field("Reds", "Cyan, Magenta, Yellow, Black adjustments for red color range", settings.redsAdjustment, Vector4.zero);
        settings.yellowsAdjustment = Vector4Field("Yellows", "Cyan, Magenta, Yellow, Black adjustments for yellow color range", settings.yellowsAdjustment, Vector4.zero);
        settings.greensAdjustment = Vector4Field("Greens", "Cyan, Magenta, Yellow, Black adjustments for green color range", settings.greensAdjustment, Vector4.zero);
        settings.cyansAdjustment = Vector4Field("Cyans", "Cyan, Magenta, Yellow, Black adjustments for cyan color range", settings.cyansAdjustment, Vector4.zero);
        settings.bluesAdjustment = Vector4Field("Blues", "Cyan, Magenta, Yellow, Black adjustments for blue color range", settings.bluesAdjustment, Vector4.zero);
        settings.magentasAdjustment = Vector4Field("Magentas", "Cyan, Magenta, Yellow, Black adjustments for magenta color range", settings.magentasAdjustment, Vector4.zero);
        settings.whitesAdjustment = Vector4Field("Whites", "Cyan, Magenta, Yellow, Black adjustments for white areas", settings.whitesAdjustment, Vector4.zero);
        settings.neutralsAdjustment = Vector4Field("Neutrals", "Cyan, Magenta, Yellow, Black adjustments for neutral gray areas", settings.neutralsAdjustment, Vector4.zero);
        settings.blacksAdjustment = Vector4Field("Blacks", "Cyan, Magenta, Yellow, Black adjustments for black areas", settings.blacksAdjustment, Vector4.zero);
      }

      // Advanced Vibrance
      if (Foldout("Advanced Vibrance") == true)
      {
        IndentLevel++;
        settings.advancedVibrance = Slider("Vibrance", "Advanced vibrance adjustment [-1, 1]. Increases saturation of unsaturated colors while protecting already saturated ones. Default 0.", settings.advancedVibrance, -1.0f, 1.0f, 0.0f);
        settings.vibranceSaturation = Slider("Saturation Control", "Controls the strength of vibrance effect [0, 2]. Default 1.", settings.vibranceSaturation, 0.0f, 2.0f, 1.0f);
        settings.vibranceProtect = Slider("Protection", "Protects already saturated colors from vibrance [0, 1]. Higher values reduce vibrance on saturated colors. Default 0.", settings.vibranceProtect, 0.0f, 1.0f, 0.0f);
        settings.vibranceColorBalance = Vector3Field("Color Balance", "Per-channel vibrance balance for RGB. Default (1, 1, 1).", settings.vibranceColorBalance, Vector3.one);

        Separator();

        settings.vibranceSkinTone = Slider("Skin Tone", "Vibrance adjustment specifically for skin tones [-1, 1]. Default 0.", settings.vibranceSkinTone, -1.0f, 1.0f, 0.0f);
        settings.vibranceSky = Slider("Sky", "Vibrance adjustment specifically for sky/blue tones [-1, 1]. Default 0.", settings.vibranceSky, -1.0f, 1.0f, 0.0f);
        settings.vibranceFoliage = Slider("Foliage", "Vibrance adjustment specifically for green foliage [-1, 1]. Default 0.", settings.vibranceFoliage, -1.0f, 1.0f, 0.0f);
        settings.vibranceWarmth = Slider("Warmth", "Vibrance adjustment for warm colors (red/orange/yellow) [-1, 1]. Default 0.", settings.vibranceWarmth, -1.0f, 1.0f, 0.0f);
        settings.vibranceCoolness = Slider("Coolness", "Vibrance adjustment for cool colors (blue/cyan) [-1, 1]. Default 0.", settings.vibranceCoolness, -1.0f, 1.0f, 0.0f);
        IndentLevel--;
      }

      Separator();

      if (Foldout("Color") == true)
      {
        IndentLevel++;

        settings.brightness = Slider("Brightness", "Brightness [-1.0, 1.0]. Default 0.", settings.brightness, -1.0f, 1.0f, 0.0f);
        settings.gamma = Slider("Gamma", "Gamma [0.1, 10.0]. Default 1.", settings.gamma, 0.01f, 10.0f, 1.0f);
        settings.hue = Slider("Hue", "The color wheel [0.0, 1.0]. Default 0.", settings.hue, 0.0f, 1.0f, 0.0f);
        settings.saturation = Slider("Saturation", "Intensity of a colors [0.0, 2.0]. Default 1.", settings.saturation, 0.0f, 2.0f, 1.0f);

        IndentLevel--;
      }

      /////////////////////////////////////////////////
      // Advanced.
      /////////////////////////////////////////////////
      Separator();

      if (Foldout("Advanced") == true)
      {
        IndentLevel++;

#if !UNITY_6000_0_OR_NEWER
        settings.filterMode = (FilterMode)EnumPopup("Filter mode", "Filter mode. Default Bilinear.", settings.filterMode, FilterMode.Bilinear);
#endif
        settings.affectSceneView = Toggle("Affect the Scene View?", "Does it affect the Scene View?", settings.affectSceneView);
        settings.whenToInsert = (UnityEngine.Rendering.Universal.RenderPassEvent)EnumPopup("RenderPass event",
          "Render pass injection. Default BeforeRenderingTransparents.",
          settings.whenToInsert,
          UnityEngine.Rendering.Universal.RenderPassEvent.BeforeRenderingTransparents);
#if !UNITY_6000_0_OR_NEWER
        settings.enableProfiling = Toggle("Enable profiling", "Enable render pass profiling", settings.enableProfiling);
#endif

        IndentLevel--;
      }
    }
  }
}
