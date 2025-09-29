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
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
#endif

namespace FronkonGames.Artistic.Tonemapper
{
  ///------------------------------------------------------------------------------------------------------------------
  /// <summary> Render Pass. </summary>
  /// <remarks> Only available for Universal Render Pipeline. </remarks>
  ///------------------------------------------------------------------------------------------------------------------
  public sealed partial class Tonemapper
  {
    [DisallowMultipleRendererFeature]
    private sealed class RenderPass : ScriptableRenderPass
    {
      // Internal use only.
      internal Material material { get; set; }

      private readonly Settings settings;

#if UNITY_6000_0_OR_NEWER
#else
      private RenderTargetIdentifier colorBuffer;
      private RenderTextureDescriptor renderTextureDescriptor;

      private readonly int renderTextureHandle0 = Shader.PropertyToID($"{Constants.Asset.AssemblyName}.RTH0");

      private const string CommandBufferName = Constants.Asset.AssemblyName;

      private ProfilingScope profilingScope;
      private readonly ProfilingSampler profilingSamples = new(Constants.Asset.AssemblyName);
#endif

      private static class ShaderIDs
      {
        internal static readonly int Intensity = Shader.PropertyToID("_Intensity");

        internal static readonly int Exposure = Shader.PropertyToID("_Exposure");
        internal static readonly int ColorFilter = Shader.PropertyToID("_ColorFilter");
        internal static readonly int Temperature = Shader.PropertyToID("_Temperature");
        internal static readonly int Tint = Shader.PropertyToID("_Tint");
        internal static readonly int Vibrance = Shader.PropertyToID("_Vibrance");
        internal static readonly int VibranceBalance = Shader.PropertyToID("_VibranceBalance");
        internal static readonly int ContrastMidpoint = Shader.PropertyToID("_ContrastMidpoint");
        internal static readonly int WhiteLevel = Shader.PropertyToID("_WhiteLevel");
        internal static readonly int LinearWhite = Shader.PropertyToID("_LinearWhite");
        internal static readonly int LinearColor = Shader.PropertyToID("_LinearColor");
        internal static readonly int Lift = Shader.PropertyToID("_Lift");
        internal static readonly int LiftBright = Shader.PropertyToID("_LiftBright");
        internal static readonly int Midtones = Shader.PropertyToID("_Midtones");
        internal static readonly int MidtonesBright = Shader.PropertyToID("_MidtonesBright");
        internal static readonly int Gain = Shader.PropertyToID("_Gain");
        internal static readonly int GainBright = Shader.PropertyToID("_GainBright");
        internal static readonly int CutOff = Shader.PropertyToID("_Cutoff");

        internal static readonly int Brightness = Shader.PropertyToID("_Brightness");
        internal static readonly int Contrast = Shader.PropertyToID("_Contrast");
        internal static readonly int Gamma = Shader.PropertyToID("_Gamma");
        internal static readonly int Hue = Shader.PropertyToID("_Hue");
        internal static readonly int Saturation = Shader.PropertyToID("_Saturation");

        internal static readonly int BlackPoint = Shader.PropertyToID("_BlackPoint");
        internal static readonly int WhitePoint = Shader.PropertyToID("_WhitePoint");
        internal static readonly int ToeStrength = Shader.PropertyToID("_ToeStrength");
        internal static readonly int ShoulderStrength = Shader.PropertyToID("_ShoulderStrength");

        internal static readonly int RedChannelMixer = Shader.PropertyToID("_RedChannelMixer");
        internal static readonly int GreenChannelMixer = Shader.PropertyToID("_GreenChannelMixer");
        internal static readonly int BlueChannelMixer = Shader.PropertyToID("_BlueChannelMixer");

        internal static readonly int HighlightTint = Shader.PropertyToID("_HighlightTint");
        internal static readonly int ShadowTint = Shader.PropertyToID("_ShadowTint");
        internal static readonly int SplitBalance = Shader.PropertyToID("_SplitBalance");

        // Selective Color Adjustments
        internal static readonly int RedsAdjustment = Shader.PropertyToID("_RedsAdjustment");
        internal static readonly int YellowsAdjustment = Shader.PropertyToID("_YellowsAdjustment");
        internal static readonly int GreensAdjustment = Shader.PropertyToID("_GreensAdjustment");
        internal static readonly int CyansAdjustment = Shader.PropertyToID("_CyansAdjustment");
        internal static readonly int BluesAdjustment = Shader.PropertyToID("_BluesAdjustment");
        internal static readonly int MagentasAdjustment = Shader.PropertyToID("_MagentasAdjustment");
        internal static readonly int WhitesAdjustment = Shader.PropertyToID("_WhitesAdjustment");
        internal static readonly int NeutralsAdjustment = Shader.PropertyToID("_NeutralsAdjustment");
        internal static readonly int BlacksAdjustment = Shader.PropertyToID("_BlacksAdjustment");

        // Advanced Vibrance Controls
        internal static readonly int AdvancedVibrance = Shader.PropertyToID("_AdvancedVibrance");
        internal static readonly int VibranceSaturation = Shader.PropertyToID("_VibranceSaturation");
        internal static readonly int VibranceProtect = Shader.PropertyToID("_VibranceProtect");
        internal static readonly int VibranceColorBalance = Shader.PropertyToID("_VibranceColorBalance");
        internal static readonly int VibranceSkinTone = Shader.PropertyToID("_VibranceSkinTone");
        internal static readonly int VibranceSky = Shader.PropertyToID("_VibranceSky");
        internal static readonly int VibranceFoliage = Shader.PropertyToID("_VibranceFoliage");
        internal static readonly int VibranceWarmth = Shader.PropertyToID("_VibranceWarmth");
        internal static readonly int VibranceCoolness = Shader.PropertyToID("_VibranceCoolness");
      }

      /// <summary> Render pass constructor. </summary>
      public RenderPass(Settings settings) : base()
      {
        this.settings = settings;
#if UNITY_6000_0_OR_NEWER
        profilingSampler = new ProfilingSampler(Constants.Asset.AssemblyName);
#endif
      }

      /// <summary> Destroy the render pass. </summary>
      ~RenderPass() => material = null;

      private void UpdateMaterial()
      {
        material.shaderKeywords = null;
        material.SetFloat(ShaderIDs.Intensity, settings.intensity);

        material.SetFloat(ShaderIDs.Exposure, settings.exposure);
        material.SetColor(ShaderIDs.ColorFilter, settings.colorFilter);
        material.SetFloat(ShaderIDs.Temperature, settings.temperature);
        material.SetFloat(ShaderIDs.Tint, settings.tint);
        material.SetFloat(ShaderIDs.Vibrance, settings.vibrance);
        material.SetVector(ShaderIDs.VibranceBalance, settings.vibranceBalance);
        material.SetFloat(ShaderIDs.ContrastMidpoint, settings.contrastMidpoint);

        switch (settings.tonemapperOperator)
        {
          case Operators.Linear: break;
          case Operators.Logarithmic:
          case Operators.WhiteLumaReinhard:
          case Operators.Hejl2015:
          case Operators.Clamping:
          case Operators.Schlick:
          case Operators.Drago:
            material.SetFloat(ShaderIDs.WhiteLevel, Mathf.Max(settings.whiteLevel, 0.0001f));
            break;
          case Operators.FilmicAldridge:
            material.SetFloat(ShaderIDs.CutOff, Mathf.Max(settings.cutOff, 0.0001f));
            break;
          case Operators.WatchDogs:
            material.SetFloat(ShaderIDs.LinearWhite, settings.linearWhite);
            material.SetFloat(ShaderIDs.LinearColor, settings.linearColor);
            break;
        }

        material.SetColor(ShaderIDs.Lift, settings.lift);
        material.SetFloat(ShaderIDs.LiftBright, settings.liftBright);
        material.SetColor(ShaderIDs.Midtones, settings.midtones);
        material.SetFloat(ShaderIDs.MidtonesBright, settings.midtonesBright);
        material.SetColor(ShaderIDs.Gain, settings.gain);
        material.SetFloat(ShaderIDs.GainBright, settings.gainBright);

        material.SetFloat(ShaderIDs.Brightness, settings.brightness);
        material.SetFloat(ShaderIDs.Contrast, settings.contrast);
        material.SetFloat(ShaderIDs.Gamma, 1.0f / settings.gamma);
        material.SetFloat(ShaderIDs.Hue, settings.hue);
        material.SetFloat(ShaderIDs.Saturation, settings.saturation);

        material.SetFloat(ShaderIDs.BlackPoint, settings.blackPoint);
        material.SetFloat(ShaderIDs.WhitePoint, settings.whitePoint);
        material.SetFloat(ShaderIDs.ToeStrength, settings.toeStrength);
        material.SetFloat(ShaderIDs.ShoulderStrength, settings.shoulderStrength);

        material.SetVector(ShaderIDs.RedChannelMixer, settings.redChannelMixer);
        material.SetVector(ShaderIDs.GreenChannelMixer, settings.greenChannelMixer);
        material.SetVector(ShaderIDs.BlueChannelMixer, settings.blueChannelMixer);

        material.SetColor(ShaderIDs.HighlightTint, settings.highlightTint);
        material.SetColor(ShaderIDs.ShadowTint, settings.shadowTint);
        material.SetFloat(ShaderIDs.SplitBalance, settings.splitBalance);

        // Selective Color Adjustments
        material.SetVector(ShaderIDs.RedsAdjustment, settings.redsAdjustment);
        material.SetVector(ShaderIDs.YellowsAdjustment, settings.yellowsAdjustment);
        material.SetVector(ShaderIDs.GreensAdjustment, settings.greensAdjustment);
        material.SetVector(ShaderIDs.CyansAdjustment, settings.cyansAdjustment);
        material.SetVector(ShaderIDs.BluesAdjustment, settings.bluesAdjustment);
        material.SetVector(ShaderIDs.MagentasAdjustment, settings.magentasAdjustment);
        material.SetVector(ShaderIDs.WhitesAdjustment, settings.whitesAdjustment);
        material.SetVector(ShaderIDs.NeutralsAdjustment, settings.neutralsAdjustment);
        material.SetVector(ShaderIDs.BlacksAdjustment, settings.blacksAdjustment);

        // Advanced Vibrance Controls
        material.SetFloat(ShaderIDs.AdvancedVibrance, settings.advancedVibrance);
        material.SetFloat(ShaderIDs.VibranceSaturation, settings.vibranceSaturation);
        material.SetFloat(ShaderIDs.VibranceProtect, settings.vibranceProtect);
        material.SetVector(ShaderIDs.VibranceColorBalance, settings.vibranceColorBalance);
        material.SetFloat(ShaderIDs.VibranceSkinTone, settings.vibranceSkinTone);
        material.SetFloat(ShaderIDs.VibranceSky, settings.vibranceSky);
        material.SetFloat(ShaderIDs.VibranceFoliage, settings.vibranceFoliage);
        material.SetFloat(ShaderIDs.VibranceWarmth, settings.vibranceWarmth);
        material.SetFloat(ShaderIDs.VibranceCoolness, settings.vibranceCoolness);
      }

#if UNITY_6000_0_OR_NEWER
      /// <inheritdoc/>
      public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
      {
        if (material == null || settings.intensity == 0.0f)
          return;

        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        if (resourceData.isActiveTargetBackBuffer == true)
          return;

        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        if (cameraData.camera.cameraType == CameraType.SceneView && settings.affectSceneView == false || cameraData.postProcessEnabled == false)
          return;

        TextureHandle source = resourceData.activeColorTexture;
        TextureHandle destination = renderGraph.CreateTexture(source.GetDescriptor(renderGraph));

        UpdateMaterial();

        RenderGraphUtils.BlitMaterialParameters pass = new(source, destination, material, (int)settings.tonemapperOperator);
        renderGraph.AddBlitPass(pass, $"{Constants.Asset.AssemblyName}.Pass");

        resourceData.cameraColor = destination;
      }
#elif UNITY_2022_3_OR_NEWER
      /// <inheritdoc/>
      public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
      {
        renderTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        renderTextureDescriptor.depthBufferBits = 0;

        colorBuffer = renderingData.cameraData.renderer.cameraColorTargetHandle;
        cmd.GetTemporaryRT(renderTextureHandle0, renderTextureDescriptor, settings.filterMode);
      }

      /// <inheritdoc/>
      public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
      {
        if (material == null ||
            renderingData.postProcessingEnabled == false ||
            settings.intensity == 0.0f ||
            settings.affectSceneView == false && renderingData.cameraData.isSceneViewCamera == true)
          return;

        CommandBuffer cmd = CommandBufferPool.Get(CommandBufferName);

        if (settings.enableProfiling == true)
          profilingScope = new ProfilingScope(cmd, profilingSamples);

        UpdateMaterial();

        cmd.Blit(colorBuffer, renderTextureHandle0, material, (int)settings.tonemapperOperator);
        cmd.Blit(renderTextureHandle0, colorBuffer);

        cmd.ReleaseTemporaryRT(renderTextureHandle0);

        if (settings.enableProfiling == true)
          profilingScope.Dispose();

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
      }

      public override void OnCameraCleanup(CommandBuffer cmd) => cmd.ReleaseTemporaryRT(renderTextureHandle0);
#else
      #error Unsupported Unity version. Please update to a newer version of Unity.
#endif
    }
  }
}
