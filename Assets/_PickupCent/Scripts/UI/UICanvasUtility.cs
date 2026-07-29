using PickupCent.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PickupCent.UI
{
    /// <summary>
    /// 여러 개의 self-building UI 컴포넌트(HudController, ToolBarController, ShopPanelController 등)가
    /// 공통으로 필요로 하는 Canvas / 상단 HUD 줄 / 사이드패널 컨테이너를 찾거나 만드는 공용 헬퍼.
    /// "먼저 이름으로 찾고, 없으면 만든다" 패턴이라 어떤 컴포넌트가 먼저 Awake돼도 안전하다.
    /// </summary>
    public static class UICanvasUtility
    {
        public static GameObject EnsureCanvas()
        {
            var canvasGO = GameObject.Find("UICanvas");
            if (canvasGO != null) return canvasGO;

            canvasGO = new GameObject("UICanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            EnsureEventSystem();
            return canvasGO;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        /// <summary>화면 오른쪽에 세로로 sp-block 카드가 쌓이는 사이드패널 컨테이너.</summary>
        public static Transform EnsureSidePanel()
        {
            var canvasGO = EnsureCanvas();
            var existing = canvasGO.transform.Find("SidePanel");
            if (existing != null) return existing;

            var panelGO = new GameObject("SidePanel", typeof(RectTransform));
            panelGO.transform.SetParent(canvasGO.transform, false);

            var rt = panelGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(-272f, 16f);
            rt.offsetMax = new Vector2(-16f, -16f);

            var layout = panelGO.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperCenter;

            return panelGO.transform;
        }

        /// <summary>화면 상단 중앙에 HUD 알약들이 가로로 나란히 놓이는 컨테이너.</summary>
        public static Transform EnsureTopHudRow()
        {
            var canvasGO = EnsureCanvas();
            var existing = canvasGO.transform.Find("TopHudRow");
            if (existing != null) return existing;

            var rowGO = new GameObject("TopHudRow", typeof(RectTransform));
            rowGO.transform.SetParent(canvasGO.transform, false);

            var rt = rowGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -16f);
            rt.sizeDelta = new Vector2(800f, 44f);

            var layout = rowGO.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;

            return rowGO.transform;
        }

        /// <summary>
        /// 스타일 가이드 4-2의 사이드패널 블록(.sp-block) 카드 하나를 만든다 — 어두운 반투명 배경 +
        /// 옅은 테두리 + (선택적) 제목 + 세로로 쌓이는 내용물 컨테이너. 도구바/상점 등 여러 컨트롤러가
        /// 공통으로 재사용한다. 반환값은 실제 버튼/행을 채워 넣을 컨텐츠 컨테이너(Transform)다.
        /// </summary>
        public static Transform CreateBlockCard(Transform sidePanel, string title)
        {
            var cardGO = new GameObject($"Block_{(string.IsNullOrEmpty(title) ? "Card" : title)}", typeof(RectTransform));
            cardGO.transform.SetParent(sidePanel, false);
            cardGO.AddComponent<LayoutElement>().flexibleWidth = 1;

            // 카드 자신의 배경 = 테두리 색(전체를 채움), 그 위에 2px 안쪽으로 들어간 배경색 패널을
            // 겹쳐서 얇은 테두리처럼 보이게 한다 — HUD 알약(pill)과 동일한 트릭.
            var borderImage = cardGO.AddComponent<Image>();
            borderImage.sprite = ProceduralSprites.CreateRoundedRectSliced(64, 14f, PickupCentPalette.BorderThin);
            borderImage.type = Image.Type.Sliced;

            var bgGO = new GameObject("Background", typeof(RectTransform));
            bgGO.transform.SetParent(cardGO.transform, false);
            var bgRt = bgGO.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = new Vector2(2f, 2f);
            bgRt.offsetMax = new Vector2(-2f, -2f);
            var bgImage = bgGO.AddComponent<Image>();
            bgImage.sprite = ProceduralSprites.CreateRoundedRectSliced(64, 12f, PickupCentPalette.PanelBlockBg);
            bgImage.type = Image.Type.Sliced;
            // 부모 VerticalLayoutGroup이 이 배경을 쌓이는 항목으로 취급하지 않도록 레이아웃에서 제외한다
            // (배경은 카드 전체를 덮는 장식용 오버레이일 뿐, 세로로 쌓이는 콘텐츠가 아니다).
            bgGO.AddComponent<LayoutElement>().ignoreLayout = true;

            var vlg = cardGO.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(14, 14, 12, 14);
            vlg.spacing = 8f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;
            cardGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            if (!string.IsNullOrEmpty(title))
            {
                var titleGO = new GameObject("Title", typeof(RectTransform));
                titleGO.transform.SetParent(cardGO.transform, false);
                var titleText = titleGO.AddComponent<Text>();
                titleText.font = PickupCentFonts.Title;
                titleText.fontStyle = FontStyle.Bold;
                titleText.fontSize = 18;
                titleText.color = PickupCentPalette.GoldBright;
                titleText.text = title;
                titleText.alignment = TextAnchor.MiddleLeft;
                titleGO.AddComponent<LayoutElement>().preferredHeight = 24;
            }

            var contentContainerGO = new GameObject("Content", typeof(RectTransform));
            contentContainerGO.transform.SetParent(cardGO.transform, false);
            var contentVlg = contentContainerGO.AddComponent<VerticalLayoutGroup>();
            contentVlg.spacing = 8f;
            contentVlg.childControlWidth = true;
            contentVlg.childControlHeight = true;
            contentVlg.childForceExpandWidth = true;
            contentVlg.childForceExpandHeight = false;
            var contentFitter = contentContainerGO.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return contentContainerGO.transform;
        }

        /// <summary>
        /// 레이아웃 그룹이 위치/크기를 관리하는 "슬롯"(outer)을 건드리지 않고, 그 안을 꽉 채우는
        /// 실제 시각 버튼(Visual)을 만든다. PressableButton이 눌림 효과로 움직이는 대상은 항상 이
        /// Visual 하나뿐이고, 이 Visual은 offset이 전부 0인 완전 스트레치라 anchoredPosition이
        /// 항상 (0,0)에서 시작하는 것이 보장된다 — 그래서 부모 VerticalLayoutGroup/HorizontalLayoutGroup의
        /// 재배치 타이밍과 절대 충돌하지 않는다(outer의 anchoredPosition을 직접 건드리면 레이아웃이
        /// 다시 계산될 때 눌림 효과가 씹히거나 복귀 위치가 어긋날 수 있음).
        /// 아이콘/라벨 등 실제 콘텐츠는 outer가 아니라 반환된 Visual의 자식으로 넣어야 한다.
        /// </summary>
        public static GameObject CreatePressableSurface(Transform outer, Sprite normalSprite, Sprite pressedSprite,
            out Button button, out Image image)
        {
            var visualGO = new GameObject("Visual", typeof(RectTransform));
            visualGO.transform.SetParent(outer, false);
            var rt = (RectTransform)visualGO.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            image = visualGO.AddComponent<Image>();
            image.sprite = normalSprite;
            image.type = Image.Type.Sliced;

            button = visualGO.AddComponent<Button>();
            var pressable = visualGO.AddComponent<PressableButton>();
            pressable.Setup(normalSprite, pressedSprite);

            return visualGO;
        }
    }
}
