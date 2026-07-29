Shader "PickupCent/SandDisplay"
{
    // 모래 레이어를 실제로 그리는 셰이더. 2단계 표현(마스크 값 m, 0~1 기준):
    //  - 살짝 건드림: m이 threshold보다 높지만 낮아지는 중 -> 불투명 유지, 마른→젖은 표면으로 점점 섞임
    //  - 확실히 뚫림: m이 threshold(기본 10/255) 이하로 떨어짐 -> 투명해지며 뒤의 아이템이 드러남
    //    (그 직전 구간에서는 파낸 바닥 텍스처가 드러나 보이다가 완전히 투명해진다)
    // _UseTextures가 0(SandMaskController가 텍스처 3개 중 하나라도 비어있을 때 설정)이면
    // 기존처럼 _SandColor/_ErodedColor 단색 블렌딩으로 안전하게 폴백한다.
    Properties
    {
        _MaskTex ("Mask", 2D) = "white" {}
        _SandColor ("Sand Color (fallback)", Color) = (0.76, 0.65, 0.42, 1)
        _ErodedColor ("Eroded Tint (fallback)", Color) = (0.35, 0.28, 0.16, 1)
        _HoleThreshold ("Hole Threshold (0-1)", Range(0,1)) = 0.0392
        _SoftEdge ("Hole Edge Softness", Range(0.001, 0.2)) = 0.02

        _UseTextures ("Use Textures (0/1)", Float) = 0
        _SandTex ("Dry Surface Texture", 2D) = "white" {}
        _WetTex ("Wet Surface Texture", 2D) = "white" {}
        _DugFloorTex ("Dug Floor Texture", 2D) = "white" {}
        _TextureTiling ("Texture Tiling", Float) = 4
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

            float _UseTextures;
            sampler2D _SandTex;
            sampler2D _WetTex;
            sampler2D _DugFloorTex;
            float _TextureTiling;

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

                // 알파(뚫림 여부)는 텍스처 모드든 단색 폴백이든 항상 동일한 기준으로 계산한다 —
                // 파기 판정(체크포인트)이 보는 마스크 값과 화면 표시가 항상 같은 threshold를 쓰게 하기 위함.
                float holeAlpha = smoothstep(_HoleThreshold - _SoftEdge, _HoleThreshold + _SoftEdge, m);

                fixed3 tint;
                if (_UseTextures > 0.5)
                {
                    float2 tiledUV = i.uv * _TextureTiling;
                    fixed3 dryCol = tex2D(_SandTex, tiledUV).rgb;
                    fixed3 wetCol = tex2D(_WetTex, tiledUV).rgb;
                    fixed3 dugCol = tex2D(_DugFloorTex, tiledUV).rgb;

                    // 마른→젖은 전환은 m=1(안 건드림)에서 m=wetPoint까지, 젖은→파낸 전환은
                    // m=wetPoint에서 m=_HoleThreshold까지 일어나서, 구멍이 나기 전에 파낸 바닥이
                    // 먼저 넓게 드러나 보이는 "구멍 테두리" 느낌을 준다.
                    const float wetPoint = 0.4;
                    float wetBlend = saturate(1.0 - smoothstep(wetPoint, 1.0, m));
                    float dugBlend = saturate(1.0 - smoothstep(_HoleThreshold, wetPoint, m));

                    fixed3 baseCol = lerp(dryCol, wetCol, wetBlend);
                    tint = lerp(baseCol, dugCol, dugBlend);
                }
                else
                {
                    tint = lerp(_ErodedColor.rgb, _SandColor.rgb, m);
                }

                return fixed4(tint, holeAlpha);
            }
            ENDCG
        }
    }
}
