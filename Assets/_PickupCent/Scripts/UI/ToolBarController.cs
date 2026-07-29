using System;
using PickupCent.Common;
using PickupCent.Digging;
using UnityEngine;
using UnityEngine.UI;

namespace PickupCent.UI
{
    /// <summary>
    /// 손/삽/금속탐지기 버튼 3개. 클릭하면 ToolManager.SwitchTool()을 호출 —
    /// 숫자 1/2/3 단축키도 같은 메서드를 호출하므로 두 입력 경로의 상태가 항상 일치한다.
    /// 스타일 가이드 4장 적용을 위해 버튼 시각 요소를 런타임에 직접 만든다. entries[].tool과
    /// entries[].icon은 ArtAssetLinker가 씬 파일에 저장해 둔 값이라 그대로 읽기만 하고,
    /// entries[].button/background만 새로 만든 것으로 덮어쓴다.
    /// </summary>
    public class ToolBarController : MonoBehaviour
    {
        [Serializable]
        public class ToolButtonEntry
        {
            public ToolManager.ToolType tool;
            public Button button;
            public Image background;

            [Tooltip("아트 에셋 연결 도구가 채움 — 비어있으면 라벨 텍스트만으로 표시")]
            public Sprite icon;
        }

        [SerializeField] private ToolManager toolManager;
        [SerializeField] private ToolButtonEntry[] entries;

        private void Awake()
        {
            if (toolManager == null) toolManager = FindFirstObjectByType<ToolManager>();
            BuildToolBar();
        }

        private void BuildToolBar()
        {
            if (entries == null) return;

            var content = UICanvasUtility.CreateBlockCard(UICanvasUtility.EnsureSidePanel(), "도구");

            foreach (var entry in entries)
            {
                if (entry == null) continue;
                if (entry.button != null) Destroy(entry.button.gameObject);

                CreateToolButton(content, entry, out var button, out var background);
                entry.button = button;
                entry.background = background;

                var tool = entry.tool;
                button.onClick.AddListener(() => toolManager.SwitchTool(tool));
            }
        }

        private void CreateToolButton(Transform parent, ToolButtonEntry entry, out Button button, out Image background)
        {
            var normalSprite = ProceduralSprites.CreateGradientButtonSliced(48, 12f,
                PickupCentPalette.Gold, PickupCentPalette.WoodLight, 3f, PickupCentPalette.ButtonBottomBorder);
            var pressedSprite = ProceduralSprites.CreateGradientButtonSliced(48, 12f,
                PickupCentPalette.Gold, PickupCentPalette.WoodLight, 1f, PickupCentPalette.ButtonBottomBorder);

            var go = new GameObject($"ToolButton_{entry.tool}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = 40f;

            var visual = UICanvasUtility.CreatePressableSurface(go.transform, normalSprite, pressedSprite,
                out button, out background);

            var contentGO = new GameObject("Content", typeof(RectTransform));
            contentGO.transform.SetParent(visual.transform, false);
            var contentRt = contentGO.GetComponent<RectTransform>();
            contentRt.anchorMin = Vector2.zero;
            contentRt.anchorMax = Vector2.one;
            contentRt.offsetMin = new Vector2(12f, 4f);
            contentRt.offsetMax = new Vector2(-12f, -4f);
            var hlg = contentGO.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            if (entry.icon != null)
            {
                var iconGO = new GameObject("Icon", typeof(RectTransform));
                iconGO.transform.SetParent(contentGO.transform, false);
                var iconImage = iconGO.AddComponent<Image>();
                iconImage.sprite = entry.icon;
                iconImage.preserveAspect = true;
                iconGO.AddComponent<LayoutElement>().preferredWidth = 24f;
            }

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(contentGO.transform, false);
            var label = labelGO.AddComponent<Text>();
            label.font = PickupCentFonts.Default;
            label.text = ToolLabel(entry.tool);
            label.color = PickupCentPalette.Ink;
            label.fontStyle = FontStyle.Bold;
            label.fontSize = 16;
            label.alignment = TextAnchor.MiddleLeft;
            labelGO.AddComponent<LayoutElement>().flexibleWidth = 1;
        }

        private static string ToolLabel(ToolManager.ToolType tool) => tool switch
        {
            ToolManager.ToolType.Hand => "손",
            ToolManager.ToolType.Shovel => "삽",
            ToolManager.ToolType.Detector => "금속탐지기",
            _ => tool.ToString()
        };

        private void Update()
        {
            if (toolManager == null || entries == null) return;

            foreach (var entry in entries)
            {
                if (entry?.background == null) continue;
                bool selected = entry.tool == toolManager.CurrentTool;
                entry.background.color = selected ? Color.white : new Color(1f, 1f, 1f, 0.55f);
            }
        }
    }
}
