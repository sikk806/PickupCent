using System.Collections.Generic;
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
        public static void DestroyObjectSafe(Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) Object.Destroy(target);
            else Object.DestroyImmediate(target);
        }

        public static void ClearChildrenSafe(Transform parent)
        {
            if (parent == null) return;

            var children = new List<GameObject>();
            foreach (Transform child in parent)
                children.Add(child.gameObject);

            foreach (var child in children)
                DestroyObjectSafe(child);
        }
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

        /// <summary>HTML의 #appWrap 안 #stage에 해당하는 고정 비율 게임 패널 UI 루트.</summary>
        public static Transform EnsureStageRoot()
        {
            var canvasGO = EnsureCanvas();
            var existing = canvasGO.transform.Find("StageRoot");
            if (existing != null)
            {
                ApplyStageRootLayout((RectTransform)existing);
                EnsureStageBorders(existing);
                existing.SetSiblingIndex(Mathf.Min(1, canvasGO.transform.childCount - 1));
                return existing;
            }

            var rootGO = new GameObject("StageRoot", typeof(RectTransform));
            rootGO.transform.SetParent(canvasGO.transform, false);
            rootGO.transform.SetSiblingIndex(Mathf.Min(1, canvasGO.transform.childCount - 1));
            var rt = rootGO.GetComponent<RectTransform>();
            ApplyStageRootLayout(rt);
            rootGO.transform.SetSiblingIndex(Mathf.Min(1, canvasGO.transform.childCount - 1));
            EnsureStageBorders(rootGO.transform);
            return rootGO.transform;
        }

        private static void ApplyStageRootLayout(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(-152f, 0f);
            rt.sizeDelta = new Vector2(960f, 700f);
        }

        private static void EnsureStageBorders(Transform stageRoot)
        {
            EnsureStageBorder(stageRoot, "Top", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 5f), new Vector2(980f, 10f));
            EnsureStageBorder(stageRoot, "Bottom", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -5f), new Vector2(980f, 10f));
            EnsureStageBorder(stageRoot, "Left", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-5f, 0f), new Vector2(10f, 720f));
            EnsureStageBorder(stageRoot, "Right", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(155f, 0f), new Vector2(315f, 720f));
        }

        private static void EnsureStageBorder(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            var existing = parent.Find($"StageBorder_{name}");
            if (existing == null)
            {
                CreateStageBorder(parent, name, anchorMin, anchorMax, position, size);
                return;
            }

            var rt = (RectTransform)existing;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            var image = existing.GetComponent<Image>();
            if (image != null) image.color = PickupCentPalette.WoodDark;
        }

        public static bool IsScreenPointInsideStage(Vector2 screenPoint)
        {
            var canvasGO = GameObject.Find("UICanvas");
            if (canvasGO == null) return true;

            var stage = canvasGO.transform.Find("StageRoot") as RectTransform;
            if (stage == null) return true;

            return RectTransformUtility.RectangleContainsScreenPoint(stage, screenPoint, null);
        }

        public static bool TryGetPlayableStageScreenRect(out Rect rect)
        {
            rect = default;

            var canvasGO = GameObject.Find("UICanvas");
            if (canvasGO == null) return false;

            var stage = canvasGO.transform.Find("StageRoot") as RectTransform;
            if (stage == null) return false;

            stage.GetWorldCorners(StageCorners);
            float left = RectTransformUtility.WorldToScreenPoint(null, StageCorners[0]).x;
            float bottom = RectTransformUtility.WorldToScreenPoint(null, StageCorners[0]).y;
            float right = RectTransformUtility.WorldToScreenPoint(null, StageCorners[2]).x;
            float top = RectTransformUtility.WorldToScreenPoint(null, StageCorners[2]).y;

            TrimByBorder(stage, "Left", ref left, ref bottom, ref right, ref top);
            TrimByBorder(stage, "Right", ref left, ref bottom, ref right, ref top);
            TrimByBorder(stage, "Bottom", ref left, ref bottom, ref right, ref top);
            TrimByBorder(stage, "Top", ref left, ref bottom, ref right, ref top);

            if (right <= left || top <= bottom) return false;

            rect = Rect.MinMaxRect(left, bottom, right, top);
            return true;
        }

        public static bool TryGetPlayableStageNormalizedRect(out Rect rect)
        {
            rect = default;

            var stage = EnsureStageRoot() as RectTransform;
            if (stage == null)
            {
                var canvasGO = GameObject.Find("UICanvas");
                if (canvasGO == null) return false;
                stage = canvasGO.transform.Find("StageRoot") as RectTransform;
                if (stage == null) return false;
            }

            Rect stageRect = stage.rect;
            if (stageRect.width <= 0f || stageRect.height <= 0f) return false;

            float left = 0f;
            float right = 1f;
            float bottom = 0f;
            float top = 1f;

            ApplyLocalBorderInset(stage, stageRect, "Left", ref left, ref bottom, ref right, ref top);
            ApplyLocalBorderInset(stage, stageRect, "Right", ref left, ref bottom, ref right, ref top);
            ApplyLocalBorderInset(stage, stageRect, "Bottom", ref left, ref bottom, ref right, ref top);
            ApplyLocalBorderInset(stage, stageRect, "Top", ref left, ref bottom, ref right, ref top);

            rect = Rect.MinMaxRect(
                Mathf.Clamp01(left),
                Mathf.Clamp01(bottom),
                Mathf.Clamp01(right),
                Mathf.Clamp01(top));
            return rect.width > 0f && rect.height > 0f;
        }

        public static bool TryGetPlayableStageWorldInsets(Camera camera, out Vector4 insets)
        {
            insets = Vector4.zero;
            if (camera == null || !camera.orthographic) return false;

            var stage = EnsureStageRoot() as RectTransform;
            if (stage == null)
            {
                var canvasGO = GameObject.Find("UICanvas");
                if (canvasGO == null) return false;
                stage = canvasGO.transform.Find("StageRoot") as RectTransform;
                if (stage == null) return false;
            }

            Rect stageRect = stage.rect;
            if (stageRect.width <= 0f || stageRect.height <= 0f) return false;

            float visibleWorldHeight = camera.orthographicSize * 2f;
            float visibleWorldWidth = visibleWorldHeight * camera.aspect;

            float left = GetBorderWorldWidth(stage, stageRect, "Left", visibleWorldWidth);
            float right = GetBorderWorldWidth(stage, stageRect, "Right", visibleWorldWidth);
            float bottom = GetBorderWorldHeight(stage, stageRect, "Bottom", visibleWorldHeight);
            float top = GetBorderWorldHeight(stage, stageRect, "Top", visibleWorldHeight);

            insets = new Vector4(left, bottom, right, top);
            return insets.x >= 0f && insets.y >= 0f && insets.z >= 0f && insets.w >= 0f;
        }

        private static readonly Vector3[] StageCorners = new Vector3[4];

        private static float GetBorderWorldWidth(
            RectTransform stage,
            Rect stageRect,
            string name,
            float visibleWorldWidth)
        {
            var border = stage.Find($"StageBorder_{name}") as RectTransform;
            if (border == null) return 0f;
            return Mathf.Clamp01(border.rect.width / stageRect.width) * visibleWorldWidth;
        }

        private static float GetBorderWorldHeight(
            RectTransform stage,
            Rect stageRect,
            string name,
            float visibleWorldHeight)
        {
            var border = stage.Find($"StageBorder_{name}") as RectTransform;
            if (border == null) return 0f;
            return Mathf.Clamp01(border.rect.height / stageRect.height) * visibleWorldHeight;
        }

        private static void ApplyLocalBorderInset(
            RectTransform stage,
            Rect stageRect,
            string name,
            ref float left,
            ref float bottom,
            ref float right,
            ref float top)
        {
            var border = stage.Find($"StageBorder_{name}") as RectTransform;
            if (border == null) return;

            border.GetWorldCorners(StageCorners);
            float bLeft = float.PositiveInfinity;
            float bBottom = float.PositiveInfinity;
            float bRight = float.NegativeInfinity;
            float bTop = float.NegativeInfinity;

            for (int i = 0; i < StageCorners.Length; i++)
            {
                Vector3 local = stage.InverseTransformPoint(StageCorners[i]);
                bLeft = Mathf.Min(bLeft, local.x);
                bBottom = Mathf.Min(bBottom, local.y);
                bRight = Mathf.Max(bRight, local.x);
                bTop = Mathf.Max(bTop, local.y);
            }

            float overlapLeft = Mathf.Max(stageRect.xMin, bLeft);
            float overlapBottom = Mathf.Max(stageRect.yMin, bBottom);
            float overlapRight = Mathf.Min(stageRect.xMax, bRight);
            float overlapTop = Mathf.Min(stageRect.yMax, bTop);
            if (overlapRight <= overlapLeft || overlapTop <= overlapBottom) return;

            if (name == "Left")
                left = Mathf.Max(left, (overlapRight - stageRect.xMin) / stageRect.width);
            else if (name == "Right")
                right = Mathf.Min(right, (overlapLeft - stageRect.xMin) / stageRect.width);
            else if (name == "Bottom")
                bottom = Mathf.Max(bottom, (overlapTop - stageRect.yMin) / stageRect.height);
            else if (name == "Top")
                top = Mathf.Min(top, (overlapBottom - stageRect.yMin) / stageRect.height);
        }

        private static void TrimByBorder(RectTransform stage, string name, ref float left, ref float bottom, ref float right, ref float top)
        {
            var border = stage.Find($"StageBorder_{name}") as RectTransform;
            if (border == null) return;

            border.GetWorldCorners(StageCorners);
            float bLeft = RectTransformUtility.WorldToScreenPoint(null, StageCorners[0]).x;
            float bBottom = RectTransformUtility.WorldToScreenPoint(null, StageCorners[0]).y;
            float bRight = RectTransformUtility.WorldToScreenPoint(null, StageCorners[2]).x;
            float bTop = RectTransformUtility.WorldToScreenPoint(null, StageCorners[2]).y;

            if (name == "Left") left = Mathf.Max(left, bRight);
            else if (name == "Right") right = Mathf.Min(right, bLeft);
            else if (name == "Bottom") bottom = Mathf.Max(bottom, bTop);
            else if (name == "Top") top = Mathf.Min(top, bBottom);
        }

        private static void CreateStageBorder(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            var go = new GameObject($"StageBorder_{name}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            var image = go.AddComponent<Image>();
            image.color = PickupCentPalette.WoodDark;
            image.raycastTarget = false;
        }

        /// <summary>화면 오른쪽에 세로로 sp-block 카드가 쌓이는 사이드패널 컨테이너.</summary>
        public static Transform EnsureSidePanel()
        {
            var canvasGO = EnsureCanvas();
            var existing = canvasGO.transform.Find("SidePanel");
            if (existing != null)
            {
                var existingRt = (RectTransform)existing;
                existingRt.anchorMin = new Vector2(0.5f, 0.5f);
                existingRt.anchorMax = new Vector2(0.5f, 0.5f);
                existingRt.pivot = new Vector2(0.5f, 0.5f);
                existingRt.anchoredPosition = new Vector2(490f, 0f);
                existingRt.sizeDelta = new Vector2(250f, 670f);
                ApplySidePanelLayout(existing.gameObject);
                RefreshSidePanelLayout(existing);
                return existing;
            }

            var panelGO = new GameObject("SidePanel", typeof(RectTransform));
            panelGO.transform.SetParent(canvasGO.transform, false);

            var rt = panelGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(490f, 0f);
            rt.sizeDelta = new Vector2(250f, 670f);

            ApplySidePanelLayout(panelGO);
            RefreshSidePanelLayout(panelGO.transform);

            return panelGO.transform;
        }

        private static void ApplySidePanelLayout(GameObject panelGO)
        {
            var layout = panelGO.GetComponent<VerticalLayoutGroup>() ?? panelGO.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperCenter;
        }

        public static void RefreshSidePanelLayout(Transform sidePanel = null)
        {
            if (sidePanel == null)
            {
                var canvasGO = GameObject.Find("UICanvas");
                sidePanel = canvasGO != null ? canvasGO.transform.Find("SidePanel") : null;
            }

            if (sidePanel == null) return;

            ApplySidePanelLayout(sidePanel.gameObject);
            ArrangeSidePanelChildren(sidePanel);

            if (sidePanel is RectTransform rt)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
                LayoutRebuilder.MarkLayoutForRebuild(rt);
            }
        }

        private static void ArrangeSidePanelChildren(Transform sidePanel)
        {
            var index = 0;
            SetChildOrder(sidePanel.Find("ShopToggleButton"), ref index);
            SetChildOrder(sidePanel.Find("PauseToggleButton"), ref index);
            SetChildOrder(FindDropTableBlock(sidePanel), ref index);
            SetChildOrder(sidePanel.Find("Block_Card"), ref index);
            SetChildOrder(sidePanel.Find("MinimapPanel"), ref index);
        }

        private static Transform FindDropTableBlock(Transform sidePanel)
        {
            foreach (Transform child in sidePanel)
            {
                if (!child.name.StartsWith("Block_")) continue;
                if (child.name == "Block_Card") continue;
                return child;
            }

            return null;
        }

        private static void SetChildOrder(Transform child, ref int index)
        {
            if (child == null) return;
            child.SetSiblingIndex(index);
            index++;
        }

        /// <summary>
        /// 모달 팝업(어두운 배경 오버레이 + 가운데 뜨는 카드)을 올려놓는 최상위 레이어.
        /// 항상 마지막 자식으로 유지해서 사이드패널/HUD보다 위에 그려지게 한다.
        /// </summary>
        public static Transform EnsureModalLayer()
        {
            var canvasGO = EnsureCanvas();
            var existing = canvasGO.transform.Find("ModalLayer");
            if (existing != null)
            {
                existing.SetAsLastSibling();
                return existing;
            }

            var layerGO = new GameObject("ModalLayer", typeof(RectTransform));
            layerGO.transform.SetParent(canvasGO.transform, false);
            var rt = layerGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            layerGO.transform.SetAsLastSibling();

            return layerGO.transform;
        }

        /// <summary>화면 상단 중앙에 HUD 알약들이 가로로 나란히 놓이는 컨테이너.</summary>
        public static Transform EnsureTopHudRow()
        {
            var stageRoot = EnsureStageRoot();
            var existing = stageRoot.Find("TopHudRow");
            if (existing != null)
            {
                var existingRt = (RectTransform)existing;
                existingRt.anchorMin = new Vector2(0.5f, 1f);
                existingRt.anchorMax = new Vector2(0.5f, 1f);
                existingRt.pivot = new Vector2(0.5f, 1f);
                existingRt.anchoredPosition = new Vector2(0f, -10f);
                existingRt.sizeDelta = new Vector2(936f, 40f);
                return existing;
            }

            var rowGO = new GameObject("TopHudRow", typeof(RectTransform));
            rowGO.transform.SetParent(stageRoot, false);

            var rt = rowGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -10f);
            rt.sizeDelta = new Vector2(936f, 40f);

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
            vlg.padding = new RectOffset(18, 18, 16, 18);
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
            contentVlg.spacing = 12f;
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
