Shader "Hidden/CannoliCat/Space/BlackHole"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "BlackHole"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "BlackHoleCommon.hlsl"

            TEXTURE2D_X_FLOAT(_SceneDepth);

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                
                float rawDepth = SAMPLE_TEXTURE2D_X_LOD(_SceneDepth, sampler_PointClamp, uv, 0).r;
                #if UNITY_REVERSED_Z
                bool hasGeometry = rawDepth > 0.0;
                #else
                bool hasGeometry = rawDepth < 1.0;
                #endif
                if (hasGeometry)
                    return float4(SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, uv, 0).rgb, 1.0);

                float2 ndc = uv * 2.0 - 1.0;
                float3 rd = BH_RayDir(ndc);
                return float4(BH_Render(_CamPos, rd), 1.0);
            }
            ENDHLSL
        }
    }
}
