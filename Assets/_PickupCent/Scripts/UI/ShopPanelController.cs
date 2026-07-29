using System;
using PickupCent.Common;
using PickupCent.Economy;
using PickupCent.Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace PickupCent.UI
{
    /// <summary>
    /// 상점 여닫기 버튼 + 강화 4종 목록을 스타일 가이드에 맞춰 스스로 구성한다. 강화 데이터는
    /// UpgradeManager가 이미 들고 있는 4개 UpgradeDefinition(DigStrengthDef 등)을 그대로 가져다 쓰므로,
    /// 예전처럼 각 줄을 씬에 미리 배치하고 인스펙터로 연결할 필요가 없다 — 전부 런타임에 만든다.
    /// </summary>
    public class ShopPanelController : MonoBehaviour
    {
        [SerializeField] private ScoreTracker scoreTracker;
        [SerializeField] private UpgradeManager upgradeManager;

        private GameObject panelRoot;
        private ShopRowView[] rows;

        /// <summary>패널이 열리거나 닫힐 때마다 발생(true=열림, false=닫힘) — 사운드 등 알림용.</summary>
        public event Action<bool> OnPanelToggled;

        private void Awake()
        {
            if (scoreTracker == null) scoreTracker = FindFirstObjectByType<ScoreTracker>();
            if (upgradeManager == null) upgradeManager = FindFirstObjectByType<UpgradeManager>();

            BuildUI();
        }

        private void BuildUI()
        {
            var sidePanel = UICanvasUtility.EnsureSidePanel();

            CreateToggleButton(sidePanel);

            var content = UICanvasUtility.CreateBlockCard(sidePanel, "상점");
            panelRoot = content.parent.gameObject;
            panelRoot.SetActive(false);

            CreateCloseButton(content);

            if (upgradeManager == null) return;

            rows = new[]
            {
                CreateRow(content, upgradeManager.DigStrengthDef),
                CreateRow(content, upgradeManager.DigRangeDef),
                CreateRow(content, upgradeManager.ShovelStabilityDef),
                CreateRow(content, upgradeManager.DetectRangeDef),
            };
        }

        private ShopRowView CreateRow(Transform content, UpgradeDefinition def)
        {
            if (def == null) return null;
            var rowGO = new GameObject($"Row_{def.upgradeName}", typeof(RectTransform));
            var row = rowGO.AddComponent<ShopRowView>();
            row.Setup(content, def, upgradeManager);
            return row;
        }

        private void CreateToggleButton(Transform sidePanel)
        {
            var normalSprite = ProceduralSprites.CreateGradientButtonSliced(48, 12f,
                PickupCentPalette.Gold, PickupCentPalette.WoodLight, 3f, PickupCentPalette.ButtonBottomBorder);
            var pressedSprite = ProceduralSprites.CreateGradientButtonSliced(48, 12f,
                PickupCentPalette.Gold, PickupCentPalette.WoodLight, 1f, PickupCentPalette.ButtonBottomBorder);

            var go = new GameObject("ShopToggleButton", typeof(RectTransform));
            go.transform.SetParent(sidePanel, false);
            go.AddComponent<LayoutElement>().preferredHeight = 44f;

            var visual = UICanvasUtility.CreatePressableSurface(go.transform, normalSprite, pressedSprite,
                out var button, out _);
            button.onClick.AddListener(TogglePanel);

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(visual.transform, false);
            var labelRt = labelGO.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var label = labelGO.AddComponent<Text>();
            label.font = PickupCentFonts.Title;
            label.text = "상점";
            label.color = PickupCentPalette.Ink;
            label.fontStyle = FontStyle.Bold;
            label.fontSize = 18;
            label.alignment = TextAnchor.MiddleCenter;
        }

        private void CreateCloseButton(Transform content)
        {
            var go = new GameObject("CloseButton", typeof(RectTransform));
            go.transform.SetParent(content, false);
            go.AddComponent<LayoutElement>().preferredHeight = 26f;

            var image = go.AddComponent<Image>();
            image.sprite = ProceduralSprites.CreateRoundedRectSliced(32, 8f, PickupCentPalette.SecondaryButtonBg);
            image.type = Image.Type.Sliced;

            var button = go.AddComponent<Button>();
            button.onClick.AddListener(ClosePanel);

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(go.transform, false);
            var labelRt = labelGO.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var label = labelGO.AddComponent<Text>();
            label.font = PickupCentFonts.Default;
            label.text = "닫기";
            label.color = PickupCentPalette.Cream;
            label.fontSize = 13;
            label.alignment = TextAnchor.MiddleCenter;
        }

        private void TogglePanel()
        {
            if (panelRoot == null) return;
            bool nowOpen = !panelRoot.activeSelf;
            panelRoot.SetActive(nowOpen);
            OnPanelToggled?.Invoke(nowOpen);
        }

        private void ClosePanel()
        {
            if (panelRoot == null) return;
            panelRoot.SetActive(false);
            OnPanelToggled?.Invoke(false);
        }

        private void Update()
        {
            if (panelRoot == null || !panelRoot.activeSelf || scoreTracker == null || rows == null) return;

            foreach (var row in rows)
                row?.Refresh(scoreTracker.Score);
        }
    }
}
