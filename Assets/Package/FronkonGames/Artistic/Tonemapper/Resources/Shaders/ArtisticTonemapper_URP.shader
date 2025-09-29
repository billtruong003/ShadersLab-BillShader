// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
Shader "Hidden/Fronkon Games/Artistic/Tonemapper URP"
{
  Properties
  {
    _MainTex("Main Texture", 2D) = "white" {}
  }

  HLSLINCLUDE
  float _Exposure;
  float3 _ColorFilter;
  float _Temperature;
  float _Tint;
  float _Vibrance;
  float3 _VibranceBalance;
  float _ContrastMidpoint;
  float4 _Lift;
  float4 _Midtones;
  float4 _Gain;
  float _LiftBright;
  float _MidtonesBright;
  float _GainBright;

  float _WhiteLevel;
  float _LinearWhite;
  float _LinearColor;

  float _Cutoff;

  float _Saturation;
  float _Contrast;

  float _BlackPoint;
  float _WhitePoint;
  float _ToeStrength;
  float _ShoulderStrength;

  float3 _RedChannelMixer;
  float3 _GreenChannelMixer;
  float3 _BlueChannelMixer;

  float4 _HighlightTint;
  float4 _ShadowTint;
  float _SplitBalance;

  float4 _RedsAdjustment;
  float4 _YellowsAdjustment;
  float4 _GreensAdjustment;
  float4 _CyansAdjustment;
  float4 _BluesAdjustment;
  float4 _MagentasAdjustment;
  float4 _WhitesAdjustment;
  float4 _NeutralsAdjustment;
  float4 _BlacksAdjustment;

  float _AdvancedVibrance;
  float _VibranceSaturation;
  float _VibranceProtect;
  float3 _VibranceColorBalance;
  float _VibranceSkinTone;
  float _VibranceSky;
  float _VibranceFoliage;
  float _VibranceWarmth;
  float _VibranceCoolness;

  float _LuminanceMean;

  static const float FloatEpsilon = 1.0e-10;
 
  inline float SafePositivePowFloat(float base, float power)
  {
    return pow(max(abs(base), FloatEpsilon), power);
  }

  inline float3 Rgb2Hsv(float3 rgb)
  {
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(rgb.bg, K.wz), float4(rgb.gb, K.xy), step(rgb.b, rgb.g));
    float4 q = lerp(float4(p.xyw, rgb.r), float4(rgb.r, p.yzx), step(p.x, rgb.r));

    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
  }

  inline float3 Hsv2Rgb(float3 hsv)
  {
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(frac(hsv.xxx + K.xyz) * 6.0 - K.www);
    return hsv.z * lerp(K.xxx, saturate(p - K.xxx), hsv.y);
  }

  inline float CalcLuminance(half3 pixel)
  {
    const float3 LumCeoff = float3(0.299, 0.587, 0.114);

    return (log(1.0 + dot(pixel, LumCeoff))).r;
  }

  inline half3 ExposureAndColorFilter(half3 pixel)
  {
    return exp2(_Exposure) * pixel * _ColorFilter;
  }

  inline half3 WhiteBalance(half3 pixel)
  {
    float temp = lerp(2000.0, 12000.0, _Temperature * 0.5 + 0.5);
    float tempScale = temp / 6500.0;
    
    float3 tempRGB;
    if (tempScale < 1.0)
    {
      tempRGB.r = 0.5 + 0.5 * tempScale;
      tempRGB.g = 0.7 + 0.3 * tempScale;
      tempRGB.b = 1.0;
    }
    else
    {
      tempRGB.r = 1.0;
      tempRGB.g = 1.0 - 0.2 * (tempScale - 1.0);
      tempRGB.b = 1.0 - 0.4 * (tempScale - 1.0);
    }
    
    pixel *= tempRGB;
    
    float3 tintRGB = float3(1.0, 1.0, 1.0);
    if (_Tint < 0.0)
    {
      tintRGB.r = 1.0 + _Tint * 0.3;
      tintRGB.b = 1.0 + _Tint * 0.1;
    }
    else
      tintRGB.g = 1.0 - _Tint * 0.3;
    
    pixel *= tintRGB;
    
    return pixel;
  }

  inline half3 SaturationAndVibrance(half3 pixel)
  {
    float luma = CalcLuminance(pixel);
    
    pixel = lerp(luma.xxx, pixel, _Saturation);

    float maxColor = max(pixel.r, max(pixel.g, pixel.b));
    float minColor = min(pixel.r, min(pixel.g, pixel.b));

    float saturationColor = maxColor - minColor;
	  float3 coeffVibrance = float3(_VibranceBalance * _Vibrance);

    return lerp(luma.xxx, pixel, 1.0 + (coeffVibrance * (1.0 - (sign(coeffVibrance) * saturationColor))));
  }

  inline half3 LiftGammaGain(half3 pixel)
  {
    _Lift *= _LiftBright;
    _Midtones *= _MidtonesBright;
    _Gain *= _GainBright;
    
    pixel.r = SafePositivePowFloat(_Gain.r * (pixel.r + (_Lift.r - 1.0) * (1.0 - pixel.r)), 1.0 / _Midtones.r);
    pixel.g = SafePositivePowFloat(_Gain.g * (pixel.g + (_Lift.g - 1.0) * (1.0 - pixel.g)), 1.0 / _Midtones.g);
    pixel.b = SafePositivePowFloat(_Gain.b * (pixel.b + (_Lift.b - 1.0) * (1.0 - pixel.b)), 1.0 / _Midtones.b);
    
    return pixel;    
  }
  
  inline half3 LogContrast(half3 pixel)
  {
    const float eps = 0.00001;

    float3 adjX = _ContrastMidpoint + (log2(pixel + eps) - _ContrastMidpoint) * _Contrast;
    
    return max((half3)0.0, exp2(adjX) - eps);
  }

  inline half3 ToneCurve(half3 pixel)
  {
    pixel = lerp(pixel, (half3)1.0, _BlackPoint);
    pixel = max(pixel, (half3)_BlackPoint);
    
    pixel = min(pixel, (half3)_WhitePoint);
    pixel = pixel / _WhitePoint;
    
    float3 toe = pixel;
    if (_ToeStrength > 0.0)
    {
      float3 toeBlend = smoothstep(0.0, 0.5, pixel);
      float3 toeAmount = _ToeStrength * (1.0 - toeBlend);
      toe = lerp(pixel, sqrt(pixel), toeAmount);
    }
    
    float3 shoulder = toe;
    if (_ShoulderStrength > 0.0)
    {
      float3 shoulderBlend = smoothstep(0.5, 1.0, toe);
      float3 shoulderAmount = _ShoulderStrength * shoulderBlend;
      
      float3 compressed = 1.0 - exp(-toe * 2.0);
      shoulder = lerp(toe, compressed, shoulderAmount);
    }
    
    return shoulder * _WhitePoint;
  }

  inline half3 ChannelMixer(half3 pixel)
  {
    float3 input = pixel;
    
    float3 result;
    result.r = dot(input, _RedChannelMixer);
    result.g = dot(input, _GreenChannelMixer);
    result.b = dot(input, _BlueChannelMixer);
    
    return max((half3)0.0, result);
  }

  inline half3 SplitToning(half3 pixel)
  {
    float luma = CalcLuminance(pixel);
    
    float shadowMask = 1.0 - smoothstep(0.0, 0.5, luma);
    float highlightMask = smoothstep(0.5, 1.0, luma);
    
    float balanceAdjust = _SplitBalance * 0.5 + 0.5;
    shadowMask = shadowMask * (1.0 - balanceAdjust);
    highlightMask = highlightMask * balanceAdjust;
    
    float3 shadowResult = pixel;
    float3 highlightResult = pixel;
    
    if (shadowMask > 0.0)
    {
      float3 shadowColor = _ShadowTint.rgb;
      shadowResult = lerp(pixel, pixel * shadowColor * 2.0, shadowMask * _ShadowTint.a);
    }
    
    if (highlightMask > 0.0)
    {
      float3 highlightColor = _HighlightTint.rgb;  
      float3 screenBlend = 1.0 - (1.0 - pixel) * (1.0 - highlightColor);
      highlightResult = lerp(pixel, screenBlend, highlightMask * _HighlightTint.a);
    }
    
    float3 result = lerp(shadowResult, highlightResult, highlightMask);
    
    return max((half3)0.0, result);
  }

  inline half3 SelectiveColor(half3 pixel)
  {
    float3 hsv = Rgb2Hsv(pixel);
    float hue = hsv.x;
    float sat = hsv.y;
    float val = hsv.z;
    
    float redMask = 0.0;
    float yellowMask = 0.0;
    float greenMask = 0.0;
    float cyanMask = 0.0;
    float blueMask = 0.0;
    float magentaMask = 0.0;
    float whiteMask = 0.0;
    float neutralMask = 0.0;
    float blackMask = 0.0;
    
    if (hue < 0.0833 || hue > 0.9167)
      redMask = smoothstep(0.0, 0.0833, min(hue, 1.0 - hue));
    
    // Yellow range: 45-75 degrees
    if (hue >= 0.125 && hue <= 0.2083)
      yellowMask = smoothstep(0.125, 0.1667, hue) * smoothstep(0.2083, 0.1667, hue);
    
    // Green range: 90-150 degrees
    if (hue >= 0.25 && hue <= 0.4167)
      greenMask = smoothstep(0.25, 0.3333, hue) * smoothstep(0.4167, 0.3333, hue);
    
    // Cyan range: 165-195 degrees
    if (hue >= 0.4583 && hue <= 0.5417)
      cyanMask = smoothstep(0.4583, 0.5, hue) * smoothstep(0.5417, 0.5, hue);
    
    // Blue range: 210-270 degrees
    if (hue >= 0.5833 && hue <= 0.75)
      blueMask = smoothstep(0.5833, 0.6667, hue) * smoothstep(0.75, 0.6667, hue);
    
    // Magenta range: 285-315 degrees
    if (hue >= 0.7917 && hue <= 0.875)
      magentaMask = smoothstep(0.7917, 0.8333, hue) * smoothstep(0.875, 0.8333, hue);
    
    whiteMask = smoothstep(0.8, 1.0, val) * smoothstep(1.0, 0.8, sat);
    neutralMask = smoothstep(0.1, 0.3, sat);
    blackMask = smoothstep(0.0, 0.2, val);
    
    float3 result = pixel;
    
    if (redMask > 0.0)
    {
      float4 adj = _RedsAdjustment * redMask;
      result.r -= adj.x;
      result.g -= adj.y;
      result.b -= adj.z;
      result *= (1.0 - adj.w);
    }
    
    if (yellowMask > 0.0)
    {
      float4 adj = _YellowsAdjustment * yellowMask;
      result.r -= adj.x;
      result.g -= adj.y;
      result.b -= adj.z;
      result *= (1.0 - adj.w);
    }
    
    if (greenMask > 0.0)
    {
      float4 adj = _GreensAdjustment * greenMask;
      result.r -= adj.x;
      result.g -= adj.y;
      result.b -= adj.z;
      result *= (1.0 - adj.w);
    }
    
    if (cyanMask > 0.0)
    {
      float4 adj = _CyansAdjustment * cyanMask;
      result.r -= adj.x;
      result.g -= adj.y;
      result.b -= adj.z;
      result *= (1.0 - adj.w);
    }
    
    if (blueMask > 0.0)
    {
      float4 adj = _BluesAdjustment * blueMask;
      result.r -= adj.x;
      result.g -= adj.y;
      result.b -= adj.z;
      result *= (1.0 - adj.w);
    }
    
    if (magentaMask > 0.0)
    {
      float4 adj = _MagentasAdjustment * magentaMask;
      result.r -= adj.x;
      result.g -= adj.y;
      result.b -= adj.z;
      result *= (1.0 - adj.w);
    }
    
    if (whiteMask > 0.0)
    {
      float4 adj = _WhitesAdjustment * whiteMask;
      result.r -= adj.x;
      result.g -= adj.y;
      result.b -= adj.z;
      result *= (1.0 - adj.w);
    }
    
    if (neutralMask > 0.0)
    {
      float4 adj = _NeutralsAdjustment * neutralMask;
      result.r -= adj.x;
      result.g -= adj.y;
      result.b -= adj.z;
      result *= (1.0 - adj.w);
    }
    
    if (blackMask > 0.0)
    {
      float4 adj = _BlacksAdjustment * blackMask;
      result.r -= adj.x;
      result.g -= adj.y;
      result.b -= adj.z;
      result *= (1.0 - adj.w);
    }
    
    return max((half3)0.0, result);
  }

  inline half3 AdvancedVibrance(half3 pixel)
  {
    float3 hsv = Rgb2Hsv(pixel);
    float hue = hsv.x;
    float sat = hsv.y;
    float val = hsv.z;
    
    float luma = CalcLuminance(pixel);
    
    float skinToneMask = 0.0;
    float skyMask = 0.0;
    float foliageMask = 0.0;
    float warmthMask = 0.0;
    float coolnessMask = 0.0;
    
    if (hue >= 0.05 && hue <= 0.15)
      skinToneMask = smoothstep(0.05, 0.1, hue) * smoothstep(0.15, 0.1, hue);
    
    if (hue >= 0.55 && hue <= 0.7)
      skyMask = smoothstep(0.55, 0.625, hue) * smoothstep(0.7, 0.625, hue);
    
    if (hue >= 0.25 && hue <= 0.45)
      foliageMask = smoothstep(0.25, 0.35, hue) * smoothstep(0.45, 0.35, hue);
    
    if (hue < 0.2 || hue > 0.9)
      warmthMask = smoothstep(0.0, 0.1, hue) + smoothstep(0.9, 1.0, hue);
    
    if (hue >= 0.5 && hue <= 0.75)
      coolnessMask = smoothstep(0.5, 0.625, hue) * smoothstep(0.75, 0.625, hue);
    
    float vibranceAmount = _AdvancedVibrance;
    
    vibranceAmount += _VibranceSkinTone * skinToneMask;
    vibranceAmount += _VibranceSky * skyMask;
    vibranceAmount += _VibranceFoliage * foliageMask;
    vibranceAmount += _VibranceWarmth * warmthMask;
    vibranceAmount += _VibranceCoolness * coolnessMask;
    
    float protection = _VibranceProtect;
    float saturationProtection = sat * protection;
    vibranceAmount *= (1.0 - saturationProtection);
    
    float3 colorBalance = _VibranceColorBalance;
    float3 balancedVibrance = vibranceAmount * colorBalance;
    
    float3 saturationChange = balancedVibrance * (1.0 - sat);
    
    float3 result = pixel;
    if (vibranceAmount > 0.0)
    {
      float3 saturated = pixel + saturationChange * pixel;
      result = lerp(pixel, saturated, _VibranceSaturation);
    }
    else if (vibranceAmount < 0.0)
    {
      float3 desaturated = lerp(luma.xxx, pixel, 1.0 + vibranceAmount);
      result = lerp(pixel, desaturated, _VibranceSaturation);
    }
    
    return max((half3)0.0, result);
  }
  ENDHLSL

  SubShader
  {
    Tags
    {
      "RenderType" = "Opaque"
      "RenderPipeline" = "UniversalPipeline"
    }
    LOD 100
    ZTest Always ZWrite Off Cull Off

    Pass
    {
      Name "Fronkon Games Artistic Tonemapper Linear Pass"

      HLSLPROGRAM
      #pragma vertex ArtisticVert
      #pragma fragment ArtisticFrag
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma exclude_renderers d3d9 d3d11_9x ps3 flash

      #include "Artistic.hlsl"
      #include "Operators.hlsl"

      half4 ArtisticFrag(ArtisticVaryings input) : SV_Target
      {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        const float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord).xy;
        const half4 color = SAMPLE_MAIN(uv);
        half4 pixel = color;

        pixel.rgb = PreOperator(pixel.rgb);
        pixel.rgb = saturate(LinearOperator(pixel.rgb));
        pixel.rgb = PostOperator(pixel.rgb);
#if 0
        pixel.rgb = PixelDemo(color.rgb, pixel.rgb, uv);
#endif
        return lerp(color, pixel, _Intensity);
      }
      ENDHLSL
    }

    Pass
    {
      Name "Fronkon Games Artistic Tonemapper Logarithmic Pass"

      HLSLPROGRAM
      #pragma vertex ArtisticVert
      #pragma fragment ArtisticFrag
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma exclude_renderers d3d9 d3d11_9x ps3 flash

      #include "Artistic.hlsl"
      #include "Operators.hlsl"

      half4 ArtisticFrag(ArtisticVaryings input) : SV_Target
      {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        const float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord).xy;
        const half4 color = SAMPLE_MAIN(uv);
        half4 pixel = color;

        pixel.rgb = PreOperator(pixel.rgb);
        pixel.rgb = saturate(LogarithmicOperator(pixel.rgb));
        pixel.rgb = PostOperator(pixel.rgb);
#if 0
        pixel.rgb = PixelDemo(color.rgb, pixel.rgb, uv);
#endif
        return lerp(color, pixel, _Intensity);
      }
      ENDHLSL
    }

    Pass
    {
      Name "Fronkon Games Artistic Tonemapper Exponential Pass"

      HLSLPROGRAM
      #pragma vertex ArtisticVert
      #pragma fragment ArtisticFrag
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma exclude_renderers d3d9 d3d11_9x ps3 flash

      #include "Artistic.hlsl"
      #include "Operators.hlsl"

      half4 ArtisticFrag(ArtisticVaryings input) : SV_Target
      {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        const float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord).xy;
        const half4 color = SAMPLE_MAIN(uv);
        half4 pixel = color;

        pixel.rgb = PreOperator(pixel.rgb);
        pixel.rgb = saturate(ExponentialOperator(pixel.rgb));
        pixel.rgb = PostOperator(pixel.rgb);
#if 0
        pixel.rgb = PixelDemo(color.rgb, pixel.rgb, uv);
#endif
        return lerp(color, pixel, _Intensity);
      }
      ENDHLSL
    }

    Pass
    {
      Name "Fronkon Games Artistic Tonemapper Simple Reinhard Pass"

      HLSLPROGRAM
      #pragma vertex ArtisticVert
      #pragma fragment ArtisticFrag
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma exclude_renderers d3d9 d3d11_9x ps3 flash

      #include "Artistic.hlsl"
      #include "Operators.hlsl"

      half4 ArtisticFrag(ArtisticVaryings input) : SV_Target
      {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        const float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord).xy;
        const half4 color = SAMPLE_MAIN(uv);
        half4 pixel = color;

        pixel.rgb = PreOperator(pixel.rgb);
        pixel.rgb = saturate(SimpleReinhardOperator(pixel.rgb));
        pixel.rgb = PostOperator(pixel.rgb);
#if 0
        pixel.rgb = PixelDemo(color.rgb, pixel.rgb, uv);
#endif
        return lerp(color, pixel, _Intensity);
      }
      ENDHLSL
    }

    Pass
    {
      Name "Fronkon Games Artistic Tonemapper Luma Reinhard Pass"

      HLSLPROGRAM
      #pragma vertex ArtisticVert
      #pragma fragment ArtisticFrag
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma exclude_renderers d3d9 d3d11_9x ps3 flash

      #include "Artistic.hlsl"
      #include "Operators.hlsl"

      half4 ArtisticFrag(ArtisticVaryings input) : SV_Target
      {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        const float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord).xy;
        const half4 color = SAMPLE_MAIN(uv);
        half4 pixel = color;

        pixel.rgb = PreOperator(pixel.rgb);
        pixel.rgb = saturate(LumaReinhardOperator(pixel.rgb));
        pixel.rgb = PostOperator(pixel.rgb);
#if 0
        pixel.rgb = PixelDemo(color.rgb, pixel.rgb, uv);
#endif
        return lerp(color, pixel, _Intensity);
      }
      ENDHLSL
    }

    Pass
    {
      Name "Fronkon Games Artistic Tonemapper Luma Inverted Reinhard Pass"

      HLSLPROGRAM
      #pragma vertex ArtisticVert
      #pragma fragment ArtisticFrag
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma exclude_renderers d3d9 d3d11_9x ps3 flash

      #include "Artistic.hlsl"
      #include "Operators.hlsl"

      half4 ArtisticFrag(ArtisticVaryings input) : SV_Target
      {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        const float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord).xy;
        const half4 color = SAMPLE_MAIN(uv);
        half4 pixel = color;

        pixel.rgb = PreOperator(pixel.rgb);
        pixel.rgb = saturate(LumaInvertedReinhardOperator(pixel.rgb));
        pixel.rgb = PostOperator(pixel.rgb);
#if 0
        pixel.rgb = PixelDemo(color.rgb, pixel.rgb, uv);
#endif
        return lerp(color, pixel, _Intensity);
      }
      ENDHLSL
    }

    Pass
    {
      Name "Fronkon Games Artistic Tonemapper White Luma Reinhard Pass"

      HLSLPROGRAM
      #pragma vertex ArtisticVert
      #pragma fragment ArtisticFrag
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma exclude_renderers d3d9 d3d11_9x ps3 flash

      #include "Artistic.hlsl"
      #include "Operators.hlsl"

      half4 ArtisticFrag(ArtisticVaryings input) : SV_Target
      {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        const float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord).xy;
        const half4 color = SAMPLE_MAIN(uv);
        half4 pixel = color;

        pixel.rgb = PreOperator(pixel.rgb);
        pixel.rgb = saturate(WhiteLumaReinhardOperator(pixel.rgb));
        pixel.rgb = PostOperator(pixel.rgb);
#if 0
        pixel.rgb = PixelDemo(color.rgb, pixel.rgb, uv);
#endif
        return lerp(color, pixel, _Intensity);
      }
      ENDHLSL
    }

    Pass
    {
      Name "Fronkon Games Artistic Tonemapper Hejl 2015 Pass"

      HLSLPROGRAM
      #pragma vertex ArtisticVert
      #pragma fragment ArtisticFrag
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma exclude_renderers d3d9 d3d11_9x ps3 flash

      #include "Artistic.hlsl"
      #include "Operators.hlsl"

      half4 ArtisticFrag(ArtisticVaryings input) : SV_Target
      {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        const float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord).xy;
        const half4 color = SAMPLE_MAIN(uv);
        half4 pixel = color;

        pixel.rgb = PreOperator(pixel.rgb);
        pixel.rgb = saturate(Hejl2015Operator(pixel.rgb));
        pixel.rgb = PostOperator(pixel.rgb);
#if 0
        pixel.rgb = PixelDemo(color.rgb, pixel.rgb, uv);
#endif
        return lerp(color, pixel, _Intensity);
      }
      ENDHLSL
    }

    Pass
    {
      Name "Fronkon Games Artistic Tonemapper Filmic Pass"

      HLSLPROGRAM
      #pragma vertex ArtisticVert
      #pragma fragment ArtisticFrag
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma exclude_renderers d3d9 d3d11_9x ps3 flash

      #include "Artistic.hlsl"
      #include "Operators.hlsl"

      half4 ArtisticFrag(ArtisticVaryings input) : SV_Target
      {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        const float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord).xy;
        const half4 color = SAMPLE_MAIN(uv);
        half4 pixel = color;

        pixel.rgb = PreOperator(pixel.rgb);
        pixel.rgb = saturate(FilmicOperator(pixel.rgb));
        pixel.rgb = PostOperator(pixel.rgb);
#if 0
        pixel.rgb = PixelDemo(color.rgb, pixel.rgb, uv);
#endif
        return lerp(color, pixel, _Intensity);
      }
      ENDHLSL
    }

    Pass
    {
      Name "Fronkon Games Artistic Tonemapper Filmic Aldridge Pass"

      HLSLPROGRAM
      #pragma vertex ArtisticVert
      #pragma fragment ArtisticFrag
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma exclude_renderers d3d9 d3d11_9x ps3 flash

      #include "Artistic.hlsl"
      #include "Operators.hlsl"

      half4 ArtisticFrag(ArtisticVaryings input) : SV_Target
      {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        const float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord).xy;
        const half4 color = SAMPLE_MAIN(uv);
        half4 pixel = color;

        pixel.rgb = PreOperator(pixel.rgb);
        pixel.rgb = saturate(FilmicAldridgeOperator(pixel.rgb));
        pixel.rgb = PostOperator(pixel.rgb);
#if 0
        pixel.rgb = PixelDemo(color.rgb, pixel.rgb, uv);
#endif
        return lerp(color, pixel, _Intensity);
      }
      ENDHLSL
    }

    Pass
    {
      Name "Fronkon Games Artistic Tonemapper ACES Pass"

      HLSLPROGRAM
      #pragma vertex ArtisticVert
      #pragma fragment ArtisticFrag
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma exclude_renderers d3d9 d3d11_9x ps3 flash

      #include "Artistic.hlsl"
      #include "Operators.hlsl"

      half4 ArtisticFrag(ArtisticVaryings input) : SV_Target
      {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        const float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord).xy;
        const half4 color = SAMPLE_MAIN(uv);
        half4 pixel = color;

        pixel.rgb = PreOperator(pixel.rgb);
        pixel.rgb = saturate(ACESOperator(pixel.rgb));
        pixel.rgb = PostOperator(pixel.rgb);
#if 0
        pixel.rgb = PixelDemo(color.rgb, pixel.rgb, uv);
#endif
        return lerp(color, pixel, _Intensity);
      }
      ENDHLSL
    }

    Pass
    {
      Name "Fronkon Games Artistic Tonemapper ACES Oscars Pass"

      HLSLPROGRAM
      #pragma vertex ArtisticVert
      #pragma fragment ArtisticFrag
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma exclude_renderers d3d9 d3d11_9x ps3 flash

      #include "Artistic.hlsl"
      #include "Operators.hlsl"

      half4 ArtisticFrag(ArtisticVaryings input) : SV_Target
      {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        const float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord).xy;
        const half4 color = SAMPLE_MAIN(uv);
        half4 pixel = color;

        pixel.rgb = PreOperator(pixel.rgb);
        pixel.rgb = saturate(ACESOscarsOperator(pixel.rgb));
        pixel.rgb = PostOperator(pixel.rgb);
#if 0
        pixel.rgb = PixelDemo(color.rgb, pixel.rgb, uv);
#endif
        return lerp(color, pixel, _Intensity);
      }
      ENDHLSL
    }

    Pass
    {
      Name "Fronkon Games Artistic Tonemapper ACES Hill Pass"

      HLSLPROGRAM
      #pragma vertex ArtisticVert
      #pragma fragment ArtisticFrag
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma exclude_renderers d3d9 d3d11_9x ps3 flash

      #include "Artistic.hlsl"
      #include "Operators.hlsl"

      half4 ArtisticFrag(ArtisticVaryings input) : SV_Target
      {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        const float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord).xy;
        const half4 color = SAMPLE_MAIN(uv);
        half4 pixel = color;

        pixel.rgb = PreOperator(pixel.rgb);
        pixel.rgb = saturate(ACESHillOperator(pixel.rgb));
        pixel.rgb = PostOperator(pixel.rgb);
#if 0
        pixel.rgb = PixelDemo(color.rgb, pixel.rgb, uv);
#endif
        return lerp(color, pixel, _Intensity);
      }
      ENDHLSL
    }

    Pass
    {
      Name "Fronkon Games Artistic Tonemapper ACES Narkowicz Pass"

      HLSLPROGRAM
      #pragma vertex ArtisticVert
      #pragma fragment ArtisticFrag
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma exclude_renderers d3d9 d3d11_9x ps3 flash

      #include "Artistic.hlsl"
      #include "Operators.hlsl"

      half4 ArtisticFrag(ArtisticVaryings input) : SV_Target
      {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        const float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord).xy;
        const half4 color = SAMPLE_MAIN(uv);
        half4 pixel = color;

        pixel.rgb = PreOperator(pixel.rgb);
        pixel.rgb = saturate(ACESNarkowiczOperator(pixel.rgb));
        pixel.rgb = PostOperator(pixel.rgb);
#if 0
        pixel.rgb = PixelDemo(color.rgb, pixel.rgb, uv);
#endif
        return lerp(color, pixel, _Intensity);
      }
      ENDHLSL
    }

    Pass
    {
      Name "Fronkon Games Artistic Tonemapper Lottes Pass"

      HLSLPROGRAM
      #pragma vertex ArtisticVert
      #pragma fragment ArtisticFrag
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma exclude_renderers d3d9 d3d11_9x ps3 flash

      #include "Artistic.hlsl"
      #include "Operators.hlsl"

      half4 ArtisticFrag(ArtisticVaryings input) : SV_Target
      {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        const float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord).xy;
        const half4 color = SAMPLE_MAIN(uv);
        half4 pixel = color;

        pixel.rgb = PreOperator(pixel.rgb);
        pixel.rgb = saturate(LottesOperator(pixel.rgb));
        pixel.rgb = PostOperator(pixel.rgb);
#if 0
        pixel.rgb = PixelDemo(color.rgb, pixel.rgb, uv);
#endif
        return lerp(color, pixel, _Intensity);
      }
      ENDHLSL
    }

    Pass
    {
      Name "Fronkon Games Artistic Tonemapper Uchimura Pass"

      HLSLPROGRAM
      #pragma vertex ArtisticVert
      #pragma fragment ArtisticFrag
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma exclude_renderers d3d9 d3d11_9x ps3 flash

      #include "Artistic.hlsl"
      #include "Operators.hlsl"

      half4 ArtisticFrag(ArtisticVaryings input) : SV_Target
      {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        const float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord).xy;
        const half4 color = SAMPLE_MAIN(uv);
        half4 pixel = color;

        pixel.rgb = PreOperator(pixel.rgb);
        pixel.rgb = saturate(UchimuraOperator(pixel.rgb));
        pixel.rgb = PostOperator(pixel.rgb);
#if 0
        pixel.rgb = PixelDemo(color.rgb, pixel.rgb, uv);
#endif
        return lerp(color, pixel, _Intensity);
      }
      ENDHLSL
    }

    Pass
    {
      Name "Fronkon Games Artistic Tonemapper Unreal 3 Pass"

      HLSLPROGRAM
      #pragma vertex ArtisticVert
      #pragma fragment ArtisticFrag
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma exclude_renderers d3d9 d3d11_9x ps3 flash

      #include "Artistic.hlsl"
      #include "Operators.hlsl"

      half4 ArtisticFrag(ArtisticVaryings input) : SV_Target
      {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        const float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord).xy;
        const half4 color = SAMPLE_MAIN(uv);
        half4 pixel = color;

        pixel.rgb = PreOperator(pixel.rgb);
        pixel.rgb = saturate(Unreal3Operator(pixel.rgb));
        pixel.rgb = PostOperator(pixel.rgb);
#if 0
        pixel.rgb = PixelDemo(color.rgb, pixel.rgb, uv);
#endif
        return lerp(color, pixel, _Intensity);
      }
      ENDHLSL
    }

    Pass
    {
      Name "Fronkon Games Artistic Tonemapper Uncharted 2 Pass"

      HLSLPROGRAM
      #pragma vertex ArtisticVert
      #pragma fragment ArtisticFrag
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma exclude_renderers d3d9 d3d11_9x ps3 flash

      #include "Artistic.hlsl"
      #include "Operators.hlsl"

      half4 ArtisticFrag(ArtisticVaryings input) : SV_Target
      {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        const float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord).xy;
        const half4 color = SAMPLE_MAIN(uv);
        half4 pixel = color;

        pixel.rgb = PreOperator(pixel.rgb);
        pixel.rgb = saturate(Uncharted2Operator(pixel.rgb));
        pixel.rgb = PostOperator(pixel.rgb);
#if 0
        pixel.rgb = PixelDemo(color.rgb, pixel.rgb, uv);
#endif
        return lerp(color, pixel, _Intensity);
      }
      ENDHLSL
    }

    Pass
    {
      Name "Fronkon Games Artistic Tonemapper WatchDogs Pass"

      HLSLPROGRAM
      #pragma vertex ArtisticVert
      #pragma fragment ArtisticFrag
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma exclude_renderers d3d9 d3d11_9x ps3 flash

      #include "Artistic.hlsl"
      #include "Operators.hlsl"

      half4 ArtisticFrag(ArtisticVaryings input) : SV_Target
      {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        const float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord).xy;
        const half4 color = SAMPLE_MAIN(uv);
        half4 pixel = color;

        pixel.rgb = PreOperator(pixel.rgb);
        pixel.rgb = saturate(WatchDogsOperator(pixel.rgb));
        pixel.rgb = PostOperator(pixel.rgb);
#if 0
        pixel.rgb = PixelDemo(color.rgb, pixel.rgb, uv);
#endif
        return lerp(color, pixel, _Intensity);
      }
      ENDHLSL
    }

    Pass
    {
      Name "Fronkon Games Artistic Tonemapper Piece-Wise Pass"

      HLSLPROGRAM
      #pragma vertex ArtisticVert
      #pragma fragment ArtisticFrag
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma exclude_renderers d3d9 d3d11_9x ps3 flash

      #include "Artistic.hlsl"
      #include "Operators.hlsl"

      half4 ArtisticFrag(ArtisticVaryings input) : SV_Target
      {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        const float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord).xy;
        const half4 color = SAMPLE_MAIN(uv);
        half4 pixel = color;

        pixel.rgb = PreOperator(pixel.rgb);
        pixel.rgb = saturate(PieceWiseOperator(pixel.rgb));
        pixel.rgb = PostOperator(pixel.rgb);
#if 0
        pixel.rgb = PixelDemo(color.rgb, pixel.rgb, uv);
#endif
        return lerp(color, pixel, _Intensity);
      }
      ENDHLSL
    }

    Pass
    {
      Name "Fronkon Games Artistic Tonemapper RomBinDaHouse Pass"

      HLSLPROGRAM
      #pragma vertex ArtisticVert
      #pragma fragment ArtisticFrag
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma exclude_renderers d3d9 d3d11_9x ps3 flash

      #include "Artistic.hlsl"
      #include "Operators.hlsl"

      half4 ArtisticFrag(ArtisticVaryings input) : SV_Target
      {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        const float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord).xy;
        const half4 color = SAMPLE_MAIN(uv);
        half4 pixel = color;

        pixel.rgb = PreOperator(pixel.rgb);
        pixel.rgb = saturate(RomBinDaHouseOperator(pixel.rgb));
        pixel.rgb = PostOperator(pixel.rgb);
#if 0
        pixel.rgb = PixelDemo(color.rgb, pixel.rgb, uv);
#endif
        return lerp(color, pixel, _Intensity);
      }
      ENDHLSL
    }

    Pass
    {
      Name "Fronkon Games Artistic Tonemapper Oklab Pass"

      HLSLPROGRAM
      #pragma vertex ArtisticVert
      #pragma fragment ArtisticFrag
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma exclude_renderers d3d9 d3d11_9x ps3 flash

      #include "Artistic.hlsl"
      #include "Operators.hlsl"

      half4 ArtisticFrag(ArtisticVaryings input) : SV_Target
      {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        const float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord).xy;
        const half4 color = SAMPLE_MAIN(uv);
        half4 pixel = color;

        pixel.rgb = PreOperator(pixel.rgb);
        pixel.rgb = saturate(OklabOperator(pixel.rgb));
        pixel.rgb = PostOperator(pixel.rgb);
#if 0
        pixel.rgb = PixelDemo(color.rgb, pixel.rgb, uv);
#endif
        return lerp(color, pixel, _Intensity);
      }
      ENDHLSL
    }

    Pass
    {
      Name "Fronkon Games Artistic Tonemapper Clamping Pass"

      HLSLPROGRAM
      #pragma vertex ArtisticVert
      #pragma fragment ArtisticFrag
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma exclude_renderers d3d9 d3d11_9x ps3 flash

      #include "Artistic.hlsl"
      #include "Operators.hlsl"

      half4 ArtisticFrag(ArtisticVaryings input) : SV_Target
      {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        const float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord).xy;
        const half4 color = SAMPLE_MAIN(uv);
        half4 pixel = color;

        pixel.rgb = PreOperator(pixel.rgb);
        pixel.rgb = saturate(ClampingOperator(pixel.rgb));
        pixel.rgb = PostOperator(pixel.rgb);
#if 0
        pixel.rgb = PixelDemo(color.rgb, pixel.rgb, uv);
#endif
        return lerp(color, pixel, _Intensity);
      }
      ENDHLSL
    }

    Pass
    {
      Name "Fronkon Games Artistic Tonemapper Max3 Pass"

      HLSLPROGRAM
      #pragma vertex ArtisticVert
      #pragma fragment ArtisticFrag
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma exclude_renderers d3d9 d3d11_9x ps3 flash

      #include "Artistic.hlsl"
      #include "Operators.hlsl"

      half4 ArtisticFrag(ArtisticVaryings input) : SV_Target
      {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        const float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord).xy;
        const half4 color = SAMPLE_MAIN(uv);
        half4 pixel = color;

        pixel.rgb = PreOperator(pixel.rgb);
        pixel.rgb = saturate(Max3Operator(pixel.rgb));
        pixel.rgb = PostOperator(pixel.rgb);
#if 0
        pixel.rgb = PixelDemo(color.rgb, pixel.rgb, uv);
#endif
        return lerp(color, pixel, _Intensity);
      }
      ENDHLSL
    }

    Pass
    {
      Name "Fronkon Games Artistic Tonemapper Ma3 Inverted Pass"

      HLSLPROGRAM
      #pragma vertex ArtisticVert
      #pragma fragment ArtisticFrag
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma exclude_renderers d3d9 d3d11_9x ps3 flash

      #include "Artistic.hlsl"
      #include "Operators.hlsl"

      half4 ArtisticFrag(ArtisticVaryings input) : SV_Target
      {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        const float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord).xy;
        const half4 color = SAMPLE_MAIN(uv);
        half4 pixel = color;

        pixel.rgb = PreOperator(pixel.rgb);
        pixel.rgb = saturate(Max3InvertedOperator(pixel.rgb));
        pixel.rgb = PostOperator(pixel.rgb);
#if 0
        pixel.rgb = PixelDemo(color.rgb, pixel.rgb, uv);
#endif
        return lerp(color, pixel, _Intensity);
      }
      ENDHLSL
    }

    Pass
    {
      Name "Fronkon Games Artistic Tonemapper PBR Neutral Pass"

      HLSLPROGRAM
      #pragma vertex ArtisticVert
      #pragma fragment ArtisticFrag
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma exclude_renderers d3d9 d3d11_9x ps3 flash

      #include "Artistic.hlsl"
      #include "Operators.hlsl"

      half4 ArtisticFrag(ArtisticVaryings input) : SV_Target
      {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        const float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord).xy;
        const half4 color = SAMPLE_MAIN(uv);
        half4 pixel = color;

        pixel.rgb = PreOperator(pixel.rgb);
        pixel.rgb = saturate(PBRNeutralOperator(pixel.rgb));
        pixel.rgb = PostOperator(pixel.rgb);
#if 0
        pixel.rgb = PixelDemo(color.rgb, pixel.rgb, uv);
#endif
        return lerp(color, pixel, _Intensity);
      }
      ENDHLSL
    }

    Pass
    {
      Name "Fronkon Games Artistic Tonemapper Schlick Pass"

      HLSLPROGRAM
      #pragma vertex ArtisticVert
      #pragma fragment ArtisticFrag
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma exclude_renderers d3d9 d3d11_9x ps3 flash

      #include "Artistic.hlsl"
      #include "Operators.hlsl"

      half4 ArtisticFrag(ArtisticVaryings input) : SV_Target
      {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        const float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord).xy;
        const half4 color = SAMPLE_MAIN(uv);
        half4 pixel = color;

        pixel.rgb = PreOperator(pixel.rgb);
        pixel.rgb = saturate(SchlickOperator(pixel.rgb));
        pixel.rgb = PostOperator(pixel.rgb);
#if 0
        pixel.rgb = PixelDemo(color.rgb, pixel.rgb, uv);
#endif
        return lerp(color, pixel, _Intensity);
      }
      ENDHLSL
    }

    Pass
    {
      Name "Fronkon Games Artistic Tonemapper Drago Pass"

      HLSLPROGRAM
      #pragma vertex ArtisticVert
      #pragma fragment ArtisticFrag
      #pragma fragmentoption ARB_precision_hint_fastest
      #pragma exclude_renderers d3d9 d3d11_9x ps3 flash

      #include "Artistic.hlsl"
      #include "Operators.hlsl"

      half4 ArtisticFrag(ArtisticVaryings input) : SV_Target
      {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        const float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord).xy;
        const half4 color = SAMPLE_MAIN(uv);
        half4 pixel = color;

        pixel.rgb = PreOperator(pixel.rgb);
        pixel.rgb = saturate(DragoOperator(pixel.rgb));
        pixel.rgb = PostOperator(pixel.rgb);
#if 0
        pixel.rgb = PixelDemo(color.rgb, pixel.rgb, uv);
#endif
        return lerp(color, pixel, _Intensity);
      }
      ENDHLSL
    }    
  }
  
  FallBack "Diffuse"
}
