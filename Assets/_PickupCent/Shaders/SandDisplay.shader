Shader "PickupCent/SandDisplay"
{
    // 모래 레이어를 실제로 그리는 셰이더. 2단계 표현:
    //  - 살짝 건드림: 마스크 값이 threshold보다 높지만 낮아지는 중 -> 불투명 유지, 색만 점점 어두워짐
    //  - 확실히 뚫림: 마스크 값이 threshold(기본 10/255) 이하로 떨어짐 -> 투명해지며 뒤의 아이템이 드러남
    Properties
    {
        _MaskTex ("Mask", 2D) = "white" {}
        _SandColor ("Sand Color", Color) = (0.76, 0.65, 0.42, 1)
        _ErodedColor ("Eroded Tint", Color) = (0.35, 0.28, 0.16, 1)
        _HoleThreshold ("Hole Threshold (0-1)", Range(0,1)) = 0.0392
        _SoftEdge ("Hole Edge Softness", Range(0.001, 0.2)) = 0.02
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MaskTex;
            fixed4 _SandColor;
            fixed4 _ErodedColor;
            float _HoleThreshold;
            float _SoftEdge;

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

            fixed4 frag(v2f i) : SV_Target
            {
                float m = tex2D(_MaskTex, i.uv).r;

                float holeAlpha = smoothstep(_HoleThreshold - _SoftEdge, _HoleThreshold + _SoftEdge, m);
                fixed3 tint = lerp(_ErodedColor.rgb, _SandColor.rgb, m);

                return fixed4(tint, holeAlpha);
            }
            ENDCG
        }
    }
}
