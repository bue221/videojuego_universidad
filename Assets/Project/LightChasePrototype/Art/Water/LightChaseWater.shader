Shader "LightChase/Water"
{
    // Stylized night-water shader for Light Chase prototype.
    // Designed for URP 17.x (Unity 6). Keeps the surface readable in dark scenes
    // and survives missing depth texture (gracefully degrades to flat translucent).
    Properties
    {
        [Header(Color)]
        _ShallowColor      ("Shallow Color", Color) = (0.28, 0.62, 0.72, 0.55)
        _DeepColor         ("Deep Color",    Color) = (0.02, 0.07, 0.16, 0.95)
        _DepthFadeDistance ("Depth Fade Distance", Range(0.05, 10)) = 2.5

        [Header(Waves)]
        _WaveAmplitude     ("Wave Amplitude", Range(0, 0.6)) = 0.18
        _WaveFrequency     ("Wave Frequency", Range(0.05, 3)) = 0.55
        _WaveSpeed         ("Wave Speed",     Range(0, 3))   = 0.9

        [Header(Foam)]
        _FoamColor         ("Foam Color", Color) = (0.92, 0.96, 1.0, 1.0)
        _FoamDistance      ("Shoreline Foam Distance", Range(0, 3)) = 0.55
        _FoamSharpness     ("Foam Sharpness", Range(0.1, 8)) = 2.4

        [Header(Highlights)]
        _SpecularColor     ("Specular Color", Color) = (1.0, 0.95, 0.78, 1.0)
        _SpecularPower     ("Specular Power", Range(1, 256)) = 96
        _FresnelPower      ("Fresnel Power", Range(0.1, 8)) = 3.0
        _FresnelStrength   ("Fresnel Strength", Range(0, 2)) = 0.55
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "Queue"           = "Transparent"
            "IgnoreProjector" = "True"
        }

        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float  _DepthFadeDistance;

                float  _WaveAmplitude;
                float  _WaveFrequency;
                float  _WaveSpeed;

                float4 _FoamColor;
                float  _FoamDistance;
                float  _FoamSharpness;

                float4 _SpecularColor;
                float  _SpecularPower;
                float  _FresnelPower;
                float  _FresnelStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float4 screenPos   : TEXCOORD2;
            };

            // Sum-of-sines surface: two crossing wave directions, cheap and stable.
            float WaveHeight(float2 worldXZ, float time)
            {
                float t = time * _WaveSpeed;
                float w1 = sin(dot(worldXZ, float2( 0.85,  0.52)) * _WaveFrequency + t * 1.0);
                float w2 = sin(dot(worldXZ, float2(-0.45,  0.89)) * _WaveFrequency * 1.4 + t * 1.3);
                float w3 = sin(dot(worldXZ, float2( 0.30, -0.95)) * _WaveFrequency * 0.7 + t * 0.7);
                return (w1 + w2 * 0.6 + w3 * 0.4) * _WaveAmplitude;
            }

            float3 WaveNormal(float2 worldXZ, float time)
            {
                float e = 0.25;
                float hL = WaveHeight(worldXZ + float2(-e, 0), time);
                float hR = WaveHeight(worldXZ + float2( e, 0), time);
                float hD = WaveHeight(worldXZ + float2(0, -e), time);
                float hU = WaveHeight(worldXZ + float2(0,  e), time);
                float3 n = normalize(float3(hL - hR, 2.0 * e, hD - hU));
                return n;
            }

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float h = WaveHeight(positionWS.xz, _Time.y);
                positionWS.y += h;

                OUT.positionWS  = positionWS;
                OUT.positionHCS = TransformWorldToHClip(positionWS);
                OUT.normalWS    = WaveNormal(positionWS.xz, _Time.y);
                OUT.screenPos   = ComputeScreenPos(OUT.positionHCS);
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float2 screenUV    = IN.screenPos.xy / max(IN.screenPos.w, 1e-4);
                float  rawDepth    = SampleSceneDepth(screenUV);
                float  sceneEyeZ   = LinearEyeDepth(rawDepth, _ZBufferParams);
                float  surfaceEyeZ = IN.screenPos.w;

                // If depth texture is disabled, _CameraDepthTexture returns 0 and
                // SceneEyeZ ends up massive. We clamp the diff so the shader still
                // looks correct (we just lose depth fade and shoreline foam).
                float  waterDepth  = clamp(sceneEyeZ - surfaceEyeZ, 0.0, 50.0);

                float depthFade  = saturate(waterDepth / max(_DepthFadeDistance, 0.001));
                float shoreMask  = saturate(1.0 - waterDepth / max(_FoamDistance, 0.001));
                float foam       = pow(shoreMask, _FoamSharpness);

                float3 normalWS  = normalize(IN.normalWS);
                Light mainLight  = GetMainLight();
                float3 viewDir   = SafeNormalize(_WorldSpaceCameraPos.xyz - IN.positionWS);
                float3 halfDir   = SafeNormalize(mainLight.direction + viewDir);

                float NdotL      = saturate(dot(normalWS, mainLight.direction));
                float NdotH      = saturate(dot(normalWS, halfDir));
                float specular   = pow(NdotH, _SpecularPower);
                float fresnel    = pow(saturate(1.0 - dot(normalWS, viewDir)), _FresnelPower) * _FresnelStrength;

                float3 baseColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, depthFade);
                float  baseAlpha = lerp(_ShallowColor.a,   _DeepColor.a,   depthFade);

                float3 lit = baseColor * (mainLight.color.rgb * NdotL + 0.35);
                lit += _SpecularColor.rgb * specular;
                lit += _ShallowColor.rgb  * fresnel;
                lit  = lerp(lit, _FoamColor.rgb, foam);

                float alpha = saturate(baseAlpha + foam * 0.85);
                return half4(lit, alpha);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Unlit"
}
