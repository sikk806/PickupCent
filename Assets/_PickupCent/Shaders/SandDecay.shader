Shader "PickupCent/SandDecay"
{
    // 마스크 전체에 "안 건드리면 서서히 되메워짐"을 적용하는 풀스크린 블릿 셰이더.
    Properties
    {
        _MainTex ("Mask", 2D) = "white" {}
        _RegenRate ("Regen Rate (0-1 per sec)", Float) = 0.024
        _DeltaTime ("Delta Time", Float) = 0.016
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
            float _RegenRate;
            float _DeltaTime;

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
                m = saturate(m + _RegenRate * _DeltaTime);
                return float4(m, 0, 0, 1);
            }
            ENDCG
        }
    }
}

