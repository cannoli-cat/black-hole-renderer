Shader "Hidden/CannoliCat/Space/BlackHole"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        TEXTURE2D_X_FLOAT(_SceneDepth);

        bool HasGeometry(float2 uv)
        {
            float rawDepth = SAMPLE_TEXTURE2D_X_LOD(_SceneDepth, sampler_PointClamp, uv, 0).r;
            #if UNITY_REVERSED_Z
            return rawDepth > 0.0;
            #else
            return rawDepth < 1.0;
            #endif
        }
        ENDHLSL

        Pass
        {
            Name "BlackHole"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_local _ BH_KERR

            #include "BlackHoleCommon.hlsl"

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                if (HasGeometry(uv))
                    return float4(SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, uv, 0).rgb, 1.0);

                float2 ndc = uv * 2.0 - 1.0;
                float3 rd = BH_RayDir(ndc);
                return float4(BH_Render(_CamPos, rd), 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "BlackHole Low Res"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_local _ BH_KERR

            #include "BlackHoleCommon.hlsl"
            
            float4 Frag(Varyings input) : SV_Target
            {
                float2 ndc = input.texcoord * 2.0 - 1.0;
                float3 rd = BH_RayDir(ndc);
                return float4(BH_Render(_CamPos, rd), 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "BlackHole Composite"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            
            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                if (HasGeometry(uv)) discard;
                return float4(SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, 0).rgb, 1.0);
            }
            ENDHLSL
        }
    }
}
