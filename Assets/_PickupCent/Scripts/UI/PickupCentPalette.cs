using UnityEngine;

namespace PickupCent.UI
{
    /// <summary>
    /// PickupCent_UI_스타일가이드.md 1장의 색상 팔레트를 코드 상수로 등록한 것.
    /// 앞으로 UI를 만들 때는 색을 직접 새로 정하지 말고 이 클래스의 값을 재사용한다.
    /// </summary>
    public static class PickupCentPalette
    {
        public static readonly Color WoodDark = HexColor("#4A3323");
        public static readonly Color WoodLight = HexColor("#8A6440");
        public static readonly Color Cream = HexColor("#F6ECD9");

        /// <summary>버튼 그라디언트 상단색. 참고 목업(플레이 화면 캡처)과 비교해 기존보다 한 톤 밝고
        /// 채도 높은 골드로 조정 — "밝은 골드 그라디언트" 요청 반영.</summary>
        public static readonly Color Gold = HexColor("#F2B93B");
        public static readonly Color GoldBright = HexColor("#FFDD77");
        public static readonly Color Ink = HexColor("#2E2115");
        public static readonly Color PanelBgSolid = HexColor("#241A11");

        /// <summary>버튼 아래쪽 강조 보더(그라디언트보다 짙은 톤) — 목업의 눌린 버튼 밑단 색과 맞춤.</summary>
        public static readonly Color ButtonBottomBorder = HexColor("#B87F12");

        /// <summary>습득 피드백 말풍선 배경(연노랑) — 목업 2번 이미지의 팝업 색.</summary>
        public static readonly Color PopupBg = HexColor("#FFF1C2");

        /// <summary>습득 피드백 말풍선 테두리(짙은 노랑).</summary>
        public static readonly Color PopupBorder = HexColor("#E8C158");

        /// <summary>말풍선 안의 텍스트 색(짙은 갈색, 연노랑 배경 위에서 대비를 위해 Ink보다 진하게).</summary>
        public static readonly Color PopupText = HexColor("#4A2E10");

        /// <summary>사이드패널 블록(.sp-block) 배경 — rgba(36,26,17,0.88).</summary>
        public static readonly Color PanelBlockBg = WithAlpha(PanelBgSolid, 0.88f);

        /// <summary>상단 HUD 알약(pill) 배경 — rgba(46,33,21,0.78).</summary>
        public static readonly Color HudPillBg = new Color(46f / 255f, 33f / 255f, 21f / 255f, 0.78f);

        /// <summary>공용 얇은 흰색 반투명 테두리.</summary>
        public static readonly Color BorderThin = new Color(1f, 1f, 1f, 0.16f);

        /// <summary>목록 항목(.list-item) 배경 — 블록보다 한 단계 더 옅은 반투명 흰색.</summary>
        public static readonly Color ListItemBg = new Color(1f, 1f, 1f, 0.055f);

        /// <summary>보조(비강조) 버튼 배경 — 반투명 흰색.</summary>
        public static readonly Color SecondaryButtonBg = new Color(1f, 1f, 1f, 0.16f);

        /// <summary>현재 적용/선택 중인 항목을 표시하는 옅은 하늘색 테두리.</summary>
        public static readonly Color HighlightBorder = new Color(0.55f, 0.82f, 0.95f, 0.9f);

        /// <summary>웹 프로토타입(sand_finder_prototype)의 .region-name/.drop-chance 텍스트 색(#bfe3ff) —
        /// 지역명 배지·드랍표 확률 수치에 쓰는 옅은 하늘색.</summary>
        public static readonly Color AccentBlue = HexColor("#BFE3FF");

        /// <summary>웹 프로토타입의 .sp-value.combo 텍스트 색(#ff8a65) — 콤보 표시 전용 주황색.</summary>
        public static readonly Color ComboOrange = HexColor("#FF8A65");

        /// <summary>웹 프로토타입의 콤보 화염 이펙트(comboFireGlow 키프레임) 어두운 쪽 색.</summary>
        public static readonly Color FireGlowDark = HexColor("#FF5A14");

        /// <summary>웹 프로토타입의 콤보 화염 이펙트 밝은 쪽 색.</summary>
        public static readonly Color FireGlowBright = HexColor("#FFAA28");

        public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        private static Color HexColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out var c)) return c;
            return Color.magenta; // 파싱 실패 시 눈에 띄게
        }
    }
}
