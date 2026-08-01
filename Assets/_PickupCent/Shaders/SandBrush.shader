Shader "PickupCent/SandBrush"
{
    // 브러시 중심에서 멀수록 덜 깎이는 감쇠를 적용해 마스크를 침식시키는 풀스크린 블릿 셰이더.
    // _ErosionAmount = 강도 ÷ 경도 로 계산된 1회 침식 비율(0~1), 브러시 중심에서 이 값만큼,
    // 가장자리로 갈수록 falloff(0~1)만큼 덜 깎인다.
    Properties
    {
        _MainTex ("Mask", 2D) = "white" {}
        _BrushCenter ("Brush Center (UV)", Vector) = (0.5, 0.5, 0, 0)
        _BrushRadius ("Brush Radius (UV)", Float) = 0.075
        _FalloffPower ("Falloff Power", Float) = 2
        _ErosionAmount ("Erosion Amount (0-1)", Float) = 0.5
    }
    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _BrushCenter;
            float _BrushRadius;
            float _FalloffPower;
            float _ErosionAmount;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float m = tex2D(_MainTex, i.uv).r;

                float dist = distance(i.uv, _BrushCenter.xy);
                float falloff = saturate(1.0 - dist / max(_BrushRadius, 1e-5));
                falloff = pow(falloff, max(_FalloffPower, 0.0001));

                m = saturate(m - _ErosionAmount * falloff);
                return float4(m, 0, 0, 1);
            }
            ENDCG
        }
    }
}

