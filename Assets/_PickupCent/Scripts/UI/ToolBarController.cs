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

            var sidePanel = UICanvasUtility.EnsureSidePanel();
            var existingContent = sidePanel.Find("Block_도구/Content");
            if (existingContent != null && WireExistingButtons(existingContent)) return;

            var content = UICanvasUtility.CreateBlockCard(sidePanel, "도구");

            foreach (var entry in entries)
            {
                if (entry == null) continue;
                if (entry.button != null) UICanvasUtility.DestroyObjectSafe(entry.button.gameObject);

                CreateToolButton(content, entry, out var button, out var background);
                entry.button = button;
                entry.background = background;

                var tool = entry.tool;
                button.onClick.AddListener(() => toolManager.SwitchTool(tool));
            }
        }

        private bool WireExistingButtons(Transform content)
        {
            bool foundAny = false;
            foreach (var entry in entries)
            {
                if (entry == null) continue;
                var existing = content.Find($"ToolButton_{entry.tool}");
                var button = existing != null ? existing.GetComponentInChildren<Button>() : null;
                if (button == null) continue;
                entry.button = button;
                entry.background = button.targetGraphic as Image;
                var tool = entry.tool;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => toolManager.SwitchTool(tool));
                foundAny = true;
            }

            return foundAny;
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

            // entry.icon은 아직 실제 아트가 연결되지 않아 비어있다 — 그동안은 도구별로 색이 다른
            // 원형 스와치를 대신 넣어서 최소한 손/삽/금속탐지기가 서로 구분되게 한다(전부 같은
            // 골드 그라디언트 버튼 배경만 있으면 구분이 안 됐던 문제). IconSlot 안에 고정 24x24
            // 정사각형으로 앵커링해서 HorizontalLayoutGroup의 세로 늘리기로 인해 원이 타원으로
            // 찌그러지지 않도록 한다.
            var iconSlotGO = new GameObject("IconSlot", typeof(RectTransform));
            iconSlotGO.transform.SetParent(contentGO.transform, false);
            iconSlotGO.AddComponent<LayoutElement>().preferredWidth = 24f;

            var iconGO = new GameObject("Icon", typeof(RectTransform));
            iconGO.transform.SetParent(iconSlotGO.transform, false);

            if (entry.icon != null)
            {
                var iconRt = iconGO.GetComponent<RectTransform>();
                iconRt.anchorMin = Vector2.zero;
                iconRt.anchorMax = Vector2.one;
                iconRt.offsetMin = Vector2.zero;
                iconRt.offsetMax = Vector2.zero;
                var iconImage = iconGO.AddComponent<Image>();
                iconImage.sprite = entry.icon;
                iconImage.preserveAspect = true;
            }
            else
            {
                var iconRt = iconGO.GetComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0.5f, 0.5f);
                iconRt.anchorMax = new Vector2(0.5f, 0.5f);
                iconRt.sizeDelta = new Vector2(20f, 20f);
                var iconImage = iconGO.AddComponent<Image>();
                iconImage.sprite = ProceduralSprites.CreateCircle(36, ToolIconColorFor(entry.tool), 1f);
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

        /// <summary>실제 아이콘 아트가 연결되기 전까지 도구별 구분용으로 쓰는 임시 색상.</summary>
        private static Color ToolIconColorFor(ToolManager.ToolType tool)
        {
            return tool switch
            {
                ToolManager.ToolType.Hand => HexColor("#C99A6D"),
                ToolManager.ToolType.Shovel => HexColor("#6FA8DC"),
                ToolManager.ToolType.Detector => HexColor("#E0C341"),
                _ => PickupCentPalette.WoodLight
            };
        }

        private static Color HexColor(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.white;
        }

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
