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
        public static readonly Color Gold = HexColor("#E0A92E");
        public static readonly Color GoldBright = HexColor("#FFD166");
        public static readonly Color Ink = HexColor("#2E2115");
        public static readonly Color PanelBgSolid = HexColor("#241A11");
        public static readonly Color ButtonBottomBorder = HexColor("#A06D15");

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

        public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        private static Color HexColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out var c)) return c;
            return Color.magenta; // 파싱 실패 시 눈에 띄게
        }
    }
}
