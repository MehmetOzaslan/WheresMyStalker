Shader "Hidden/HeatSplat"
{
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        ZWrite Off
        ZTest Always
        Cull Off
        Blend One One 
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            struct PointData {
                float2 uv;        // 0..1 in the target texture
                float  intensity; // how much heat to add
                float  radiusPx;  // blob radius in pixels
            };

            StructuredBuffer<PointData> _Points;
            float2 _TexSize;

            struct Attributes {
                float2 corner : POSITION;
                uint   iid    : SV_InstanceID;
            };

            struct Varyings {
                float4 pos : SV_POSITION;
                float2 local : TEXCOORD0;
                float  intensity : TEXCOORD1;
            };

            Varyings vert (Attributes IN)
            {
                PointData p = _Points[IN.iid];

                // Convert point uv (0..1) to clip space (-1..1)
                float2 centerCS = p.uv * 2.0 - 1.0;

                // Convert radius in pixels to clip-space scale
                float2 radiusCS = (p.radiusPx / _TexSize) * 2.0;

                float2 cs = centerCS + IN.corner * radiusCS;

                Varyings OUT;
                OUT.pos = float4(cs, 0, 1);
                OUT.local = IN.corner;      // -1..1
                OUT.intensity = p.intensity;
                return OUT;
            }

            float frag (Varyings IN) : SV_Target
            {
                // Distance from center in "radius units"
                float r = length(IN.local); // 0 at center, ~1 at edge of radius

                // Option A (fast-ish): smooth blob
                float w = saturate(1.0 - r);
                w = w * w; // sharpen a bit

                // Option B (more Gaussian-ish, slightly heavier):
                // float sigma = 0.5; // radius units
                // float w = exp(-0.5 * (r*r) / (sigma*sigma));

                return w * IN.intensity; // single-channel heat
            }
            ENDHLSL
        }
    }
}
