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
using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace FronkonGames.Artistic.Tonemapper
{
  ///------------------------------------------------------------------------------------------------------------------
  /// <summary> Settings. </summary>
  /// <remarks> Only available for Universal Render Pipeline. </remarks>
  ///------------------------------------------------------------------------------------------------------------------
  public sealed partial class Tonemapper
  {
    /// <summary> Settings. </summary>
    [Serializable]
    public sealed class Settings
    {
      public Settings() => ResetDefaultValues();

      /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
      #region Common settings.

      /// <summary> Controls the intensity of the effect [0, 1]. Default 1. </summary>
      /// <remarks> An effect with Intensity equal to 0 will not be executed. </remarks>
      public float intensity = 1.0f;
      #endregion
      /////////////////////////////////////////////////////////////////////////////////////////////////////////////////

      /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
      #region Tonemapping settings.

      /// <summary> Tonemapping operator. </summary>
      public Operators tonemapperOperator = Operators.Linear;

      /// <summary> Exposure, affects the overal brightness [0, 10]. Default 0. </summary>
      public float exposure = 0.0f;

      /// <summary> Color tint. Default White. </summary>
      public Color colorFilter = Color.white;

      /// <summary> White balance temperature [-1, 1]. Default 0. Negative values are cooler (more blue), positive values are warmer (more yellow). </summary>
      public float temperature = 0.0f;

      /// <summary> White balance tint [-1, 1]. Default 0. Negative values are more green, positive values are more magenta. </summary>
      public float tint = 0.0f;

      /// <summary> Color vibrance [-1, 1]. Default 0. </summary>
      public float vibrance = 0.0f;

      /// <summary> Color vibrance color channels balance. Default (1, 1, 1). </summary>
      public Vector3 vibranceBalance = Vector3.one;

      /// <summary> Log of linear constrast midpoint. Default 0.18. </summary>
      public float contrastMidpoint = 0.18f;

      /// <summary> Adjust shadows for RGB. Default White. </summary>
      public Color lift = Color.white;

      /// <summary> Lift bright [0, 2]. Default 1. </summary>
      public float liftBright = 1.0f;

      /// <summary> Adjust midtones for RGB. Default White. </summary>
      public Color midtones = Color.white;

      /// <summary> Midtones bright [0, 2]. Default 1. </summary>
      public float midtonesBright = 1.0f;

      /// <summary> Adjust highlights for RGB. Default White. </summary>
      public Color gain = Color.white;

      /// <summary> Gain bright [0, 2]. Default 1. </summary>
      public float gainBright = 1.0f;

      /// <summary>
      /// White level exposure [0 - 5]. Default 1.
      /// Used in Linear, Logarithmic, WhiteLumaReinhard, Hejl2015 and Clamping.
      /// </summary>
      public float whiteLevel = 1.0f;

      /// <summary>
      /// Linear white [0.5 - 2]. Default 1.5.
      /// Used in WatchDogs.
      /// </summary>
      public float linearWhite = 1.5f;

      /// <summary>
      /// Linear color [0.5 - 2]. Default 1.5.
      /// Used in WatchDogs.
      /// </summary>
      public float linearColor = 1.5f;

      /// <summary>
      /// Curoff color [0 - 0.5]. Default 0.025.
      /// Used in Filmic Aldridge.
      /// </summary>
      public float cutOff = 0.025f;

      #endregion
      /////////////////////////////////////////////////////////////////////////////////////////////////////////////////

      /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
      #region Color settings.

      /// <summary> Brightness [-1.0, 1.0]. Default 0. </summary>
      public float brightness = 0.0f;

      /// <summary> Contrast [0.0, 10.0]. Default 1. </summary>
      public float contrast = 1.0f;

      /// <summary> Gamma [0.1, 10.0]. Default 1. </summary>
      public float gamma = 1.0f;

      /// <summary> The color wheel [0.0, 1.0]. Default 0. </summary>
      public float hue = 0.0f;

      /// <summary> Intensity of a colors [0.0, 2.0]. Default 1. </summary>
      public float saturation = 1.0f;

      /// <summary> Tone curve black point [0.0, 0.5]. Default 0. Lifts the blacks for a faded film look. </summary>
      public float blackPoint = 0.0f;

      /// <summary> Tone curve white point [0.5, 1.0]. Default 1. Lowers the whites for a vintage look. </summary>
      public float whitePoint = 1.0f;

      /// <summary> Tone curve toe strength [0.0, 1.0]. Default 0. Controls shadow rolloff smoothness. </summary>
      public float toeStrength = 0.0f;

      /// <summary> Tone curve shoulder strength [0.0, 1.0]. Default 0. Controls highlight rolloff smoothness. </summary>
      public float shoulderStrength = 0.0f;

      /// <summary> Channel mixer red output [-2, 2]. Default (1, 0, 0). Controls how much R, G, B input influences red output. </summary>
      public Vector3 redChannelMixer = new Vector3(1, 0, 0);

      /// <summary> Channel mixer green output [-2, 2]. Default (0, 1, 0). Controls how much R, G, B input influences green output. </summary>
      public Vector3 greenChannelMixer = new Vector3(0, 1, 0);

      /// <summary> Channel mixer blue output [-2, 2]. Default (0, 0, 1). Controls how much R, G, B input influences blue output. </summary>
      public Vector3 blueChannelMixer = new Vector3(0, 0, 1);

      /// <summary> Split toning highlight color. Default White. Color applied to highlights. </summary>
      public Color highlightTint = Color.white;

      /// <summary> Split toning shadow color. Default White. Color applied to shadows. </summary>
      public Color shadowTint = Color.white;

      /// <summary> Split toning balance [-1, 1]. Default 0. Controls the balance between shadow and highlight tinting. Negative favors shadows, positive favors highlights. </summary>
      public float splitBalance = 0.0f;

      // Selective Color Adjustments
      // Each color range has CMYB (Cyan, Magenta, Yellow, Black) adjustments
      public Vector4 redsAdjustment = Vector4.zero;
      public Vector4 yellowsAdjustment = Vector4.zero;
      public Vector4 greensAdjustment = Vector4.zero;
      public Vector4 cyansAdjustment = Vector4.zero;
      public Vector4 bluesAdjustment = Vector4.zero;
      public Vector4 magentasAdjustment = Vector4.zero;
      public Vector4 whitesAdjustment = Vector4.zero;
      public Vector4 neutralsAdjustment = Vector4.zero;
      public Vector4 blacksAdjustment = Vector4.zero;

      // Advanced Vibrance Controls
      public float advancedVibrance = 0.0f;
      public float vibranceSaturation = 1.0f;
      public float vibranceProtect = 0.0f;
      public Vector3 vibranceColorBalance = Vector3.one;
      public float vibranceSkinTone = 0.0f;
      public float vibranceSky = 0.0f;
      public float vibranceFoliage = 0.0f;
      public float vibranceWarmth = 0.0f;
      public float vibranceCoolness = 0.0f;

      #endregion
      /////////////////////////////////////////////////////////////////////////////////////////////////////////////////

      /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
      #region Advanced settings.
      /// <summary> Does it affect the Scene View? </summary>
      public bool affectSceneView = false;

#if !UNITY_6000_0_OR_NEWER
      /// <summary> Enable render pass profiling. </summary>
      public bool enableProfiling = false;

      /// <summary> Filter mode. Default Bilinear. </summary>
      public FilterMode filterMode = FilterMode.Bilinear;
#endif
      /// <summary> Render pass injection. Default BeforeRenderingPostProcessing. </summary>
      public RenderPassEvent whenToInsert = RenderPassEvent.BeforeRenderingPostProcessing;

      #endregion
      /////////////////////////////////////////////////////////////////////////////////////////////////////////////////

      /// <summary> Reset to default values. </summary>
      public void ResetDefaultValues()
      {
        intensity = 1.0f;

        tonemapperOperator = Operators.Linear;
        exposure = 0.0f;
        temperature = 0.0f;
        tint = 0.0f;
        vibrance = 0.0f;
        vibranceBalance = Vector3.one;
        contrastMidpoint = 0.18f;
        lift = Color.white;
        liftBright = 1.0f;
        midtones = Color.white;
        midtonesBright = 1.0f;
        gain = Color.white;
        gainBright = 1.0f;
        whiteLevel = 1.0f;
        linearWhite = 1.5f;
        linearColor = 1.5f;
        cutOff = 0.025f;

        brightness = 0.0f;
        contrast = 1.0f;
        gamma = 1.0f;
        hue = 0.0f;
        saturation = 1.0f;

        blackPoint = 0.0f;
        whitePoint = 1.0f;
        toeStrength = 0.0f;
        shoulderStrength = 0.0f;

        redChannelMixer = new Vector3(1, 0, 0);
        greenChannelMixer = new Vector3(0, 1, 0);
        blueChannelMixer = new Vector3(0, 0, 1);

        highlightTint = Color.white;
        shadowTint = Color.white;
        splitBalance = 0.0f;

        redsAdjustment = Vector4.zero;
        yellowsAdjustment = Vector4.zero;
        greensAdjustment = Vector4.zero;
        cyansAdjustment = Vector4.zero;
        bluesAdjustment = Vector4.zero;
        magentasAdjustment = Vector4.zero;
        whitesAdjustment = Vector4.zero;
        neutralsAdjustment = Vector4.zero;
        blacksAdjustment = Vector4.zero;

        advancedVibrance = 0.0f;
        vibranceSaturation = 1.0f;
        vibranceProtect = 0.0f;
        vibranceColorBalance = Vector3.one;
        vibranceSkinTone = 0.0f;
        vibranceSky = 0.0f;
        vibranceFoliage = 0.0f;
        vibranceWarmth = 0.0f;
        vibranceCoolness = 0.0f;

        affectSceneView = false;
#if !UNITY_6000_0_OR_NEWER
        enableProfiling = false;
        filterMode = FilterMode.Bilinear;
#endif
        whenToInsert = RenderPassEvent.BeforeRenderingPostProcessing;
      }
    }
  }
}
