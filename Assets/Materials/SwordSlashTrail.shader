Shader "Zelda/SwordSlashTrail"
{
    Properties
    {
        [MainTexture] _MainTex ("Slash Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (0.25, 0.95, 0.45, 1.0)
        [HDR] _EmissionColor ("Emission Color", Color) = (0.4, 2.8, 1.0, 1.0)
        _TailFalloff ("Tail Falloff", Range(0.5, 5.0)) = 1.8
        _EdgeSharpness ("Cutting Edge Sharpness", Range(1.0, 10.0)) = 3.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent+50" 
            "RenderPipeline"="UniversalPipeline" 
            "IgnoreProjector"="True"
        }

        Blend SrcAlpha One // Additive blend for radiant glowing slash energy
        ZWrite Off
        Cull Off

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EmissionColor;
                float _TailFalloff;
                float _EdgeSharpness;
                float4 _MainTex_ST;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // Sample slash texture
                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // Longitudinal trail falloff: uv.x is 0 at blade, 1 at tail
                float tailFade = pow(saturate(1.0 - input.uv.x), _TailFalloff);

                // Cross-blade profile: uv.y is 0 at inner radius, 1 at outer tip
                float edgeGlow = pow(saturate(input.uv.y), _EdgeSharpness);
                float innerFade = smoothstep(0.0, 0.25, input.uv.y);

                // Combine alpha
                float alpha = tailFade * innerFade * texColor.a * input.color.a * _BaseColor.a;

                // Leading blade tip has an intense white-hot core
                float whiteCore = pow(saturate(1.0 - input.uv.x), 5.0) * edgeGlow * 1.5;
                float3 finalColor = (_BaseColor.rgb + _EmissionColor.rgb + whiteCore) * alpha;

                return float4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
