Shader "ProjectCook/Cooking/TwoSidedCooking"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map (Raw Albedo)", 2D) = "white" {}
        _CookedMap("Cooked Map (Cooked Albedo)", 2D) = "white" {}
        _BurntColor("Burnt Color", Color) = (0.35, 0.3, 0.3, 1)
        _SideAProgress("Side A (Bottom) Progress", Range(0, 2)) = 0.0
        _SideBProgress("Side B (Top) Progress", Range(0, 2)) = 0.0
        _TintIntensity("Tint Intensity", Range(0, 1)) = 0.5
        _Smoothness("Smoothness (Oil Glossiness)", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 positionWS   : TEXCOORD1;
                float3 normalWS     : NORMAL;
                float3 normalOS     : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_CookedMap);
            SAMPLER(sampler_CookedMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _CookedMap_ST;
                float4 _BurntColor;
                float _SideAProgress;
                float _SideBProgress;
                float _TintIntensity;
                float _Smoothness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, float4(0,0,0,0));

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.normalOS = input.normalOS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 rawTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half4 cookedTex = SAMPLE_TEXTURE2D(_CookedMap, sampler_CookedMap, input.uv);

                // 1. Calculate Top (+Y) vs Bottom (-Y) face mask based on local normal
                float sideMask = saturate(input.normalOS.y * 2.0 + 0.5);

                // 2. Interpolate cook progress between Side A (Bottom) and Side B (Top)
                float progress = lerp(_SideAProgress, _SideBProgress, sideMask);

                // 3. Pure 100% texture transition from Raw Texture to Cooked Texture per side
                half4 baseAlbedoTex = lerp(rawTex, cookedTex, saturate(progress));

                // 4. Compute Tint Color: Pure crisp texture during cooking phase, tint only when burnt (progress > 1.0)
                half3 burntTint = lerp(half3(1.0, 1.0, 1.0), _BurntColor.rgb, saturate(progress - 1.0));

                // 5. Apply burnt tint according to _TintIntensity
                half3 finalAlbedo = lerp(baseAlbedoTex.rgb, baseAlbedoTex.rgb * burntTint, _TintIntensity);

                // 6. Lighting Calculation
                float3 normalWS = normalize(input.normalWS);
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                // Shadow and Light Attenuation
                half atten = mainLight.distanceAttenuation * mainLight.shadowAttenuation;
                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 directDiffuse = finalAlbedo * mainLight.color * (NdotL * atten);

                // Ambient Light / Light Probes (Matches dark kitchen scene environment)
                half3 ambient = finalAlbedo * SampleSH(normalWS);

                // Specular Oil Glossiness
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float3 halfDir = SafeNormalize(mainLight.direction + viewDirWS);
                half NdotH = saturate(dot(normalWS, halfDir));
                half specPower = lerp(10.0, 128.0, _Smoothness);
                half specTerm = pow(NdotH, specPower) * _Smoothness;
                half3 directSpecular = mainLight.color * (specTerm * NdotL * atten);

                half3 finalColor = directDiffuse + ambient + directSpecular;
                return half4(finalColor, baseAlbedoTex.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, float4(0,0,0,0));

                float3 positionWS = vertexInput.positionWS;
                float3 normalWS = normalInput.normalWS;
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

                #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                output.positionCS = positionCS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
