using PickupCent.Common;
using PickupCent.Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace PickupCent.UI
{
    /// <summary>
    /// 상점 패널 안의 강화 한 줄 — 스타일 가이드 4-4의 목록 항목(.list-item) 룩으로 스스로 UI를 만든다.
    /// 예전엔 씬에 미리 배치된 후 definition/upgradeManager를 인스펙터로 연결했지만(그때 발견한
    /// 델리게이트 직렬화 버그 때문에 필드 방식으로 바꿨던 것), 이번엔 ShopPanelController가 런타임에
    /// Instantiate 없이 곧바로 GameObject를 만들고 Setup()을 호출하는 방식이라 애초에 직렬화가
    /// 필요 없다 — Setup 호출 시점에 바로 시각 요소까지 전부 구성한다.
    /// </summary>
    public class ShopRowView : MonoBehaviour
    {
        private UpgradeDefinition definition;
        private UpgradeManager upgradeManager;

        private Text levelText;
        private Text costText;
        private Button buyButton;

        public UpgradeDefinition Definition => definition;

        public void Setup(Transform parent, UpgradeDefinition def, UpgradeManager manager)
        {
            definition = def;
            upgradeManager = manager;
            transform.SetParent(parent, false);
            BuildUI();
            Refresh(0);
        }

        private void BuildUI()
        {
            gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;

            var bg = gameObject.AddComponent<Image>();
            bg.sprite = ProceduralSprites.CreateRoundedRectSliced(48, 10f, PickupCentPalette.ListItemBg);
            bg.type = Image.Type.Sliced;

            var hlg = gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(10, 10, 6, 6);
            hlg.spacing = 8f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            // 목업 3번 이미지의 "아이콘 + 이름/레벨 + 비용 + 버튼" 목록 항목 구조 — 실제 아이콘 아트가
            // 없어 강화 종류별로 색이 뚜렷하게 다른 원형 스와치를 아이콘 자리에 대신 넣는다.
            // IconSlot(레이아웃 그룹의 흐름 항목, 행 높이만큼 세로로 늘어남) 안에 실제 원(Icon)은
            // 늘어나지 않는 고정 28x28 정사각형으로 따로 앵커링해서 넣는다 — 그렇지 않으면
            // HorizontalLayoutGroup의 childForceExpandHeight 때문에 원이 세로로 늘어나 타원으로
            // 보이는 문제가 있었다(모든 항목이 서로 구분 안 되는 것처럼 보였던 원인 중 하나).
            var iconSlotGO = new GameObject("IconSlot", typeof(RectTransform));
            iconSlotGO.transform.SetParent(transform, false);
            iconSlotGO.AddComponent<LayoutElement>().preferredWidth = 32f;

            var iconGO = new GameObject("Icon", typeof(RectTransform));
            iconGO.transform.SetParent(iconSlotGO.transform, false);
            var iconRt = iconGO.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(28f, 28f);
            var iconImage = iconGO.AddComponent<Image>();
            iconImage.sprite = ProceduralSprites.CreateCircle(40, IconColorFor(definition), 1f);

            var infoGO = new GameObject("Info", typeof(RectTransform));
            infoGO.transform.SetParent(transform, false);
            infoGO.AddComponent<LayoutElement>().flexibleWidth = 1;
            var infoVlg = infoGO.AddComponent<VerticalLayoutGroup>();
            infoVlg.spacing = 2f;
            infoVlg.childAlignment = TextAnchor.MiddleLeft;
            infoVlg.childControlWidth = true;
            infoVlg.childControlHeight = true;
            infoVlg.childForceExpandWidth = true;
            infoVlg.childForceExpandHeight = false;

            var nameGO = new GameObject("Name", typeof(RectTransform));
            nameGO.transform.SetParent(infoGO.transform, false);
            var nameText = nameGO.AddComponent<Text>();
            nameText.font = PickupCentFonts.Default;
            nameText.text = definition != null ? definition.upgradeName : "-";
            nameText.color = PickupCentPalette.Cream;
            nameText.fontSize = 15;
            nameText.fontStyle = FontStyle.Bold;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameGO.AddComponent<LayoutElement>().preferredHeight = 20f;

            var levelGO = new GameObject("Level", typeof(RectTransform));
            levelGO.transform.SetParent(infoGO.transform, false);
            levelText = levelGO.AddComponent<Text>();
            levelText.font = PickupCentFonts.Default;
            levelText.color = new Color(1f, 1f, 1f, 0.6f);
            levelText.fontSize = 12;
            levelText.alignment = TextAnchor.MiddleLeft;
            levelGO.AddComponent<LayoutElement>().preferredHeight = 16f;

            var costGO = new GameObject("Cost", typeof(RectTransform));
            costGO.transform.SetParent(transform, false);
            costText = costGO.AddComponent<Text>();
            costText.font = PickupCentFonts.Title;
            costText.fontStyle = FontStyle.Bold;
            costText.color = PickupCentPalette.GoldBright;
            costText.fontSize = 15;
            costText.alignment = TextAnchor.MiddleRight;
            costGO.AddComponent<LayoutElement>().preferredWidth = 56f;

            var normalSprite = ProceduralSprites.CreateGradientButtonSliced(40, 10f,
                PickupCentPalette.Gold, PickupCentPalette.WoodLight, 3f, PickupCentPalette.ButtonBottomBorder);
            var pressedSprite = ProceduralSprites.CreateGradientButtonSliced(40, 10f,
                PickupCentPalette.Gold, PickupCentPalette.WoodLight, 1f, PickupCentPalette.ButtonBottomBorder);

            var buySlotGO = new GameObject("BuySlot", typeof(RectTransform));
            buySlotGO.transform.SetParent(transform, false);
            buySlotGO.AddComponent<LayoutElement>().preferredWidth = 56f;

            var buyVisual = UICanvasUtility.CreatePressableSurface(buySlotGO.transform, normalSprite, pressedSprite,
                out buyButton, out _);

            var buyLabelGO = new GameObject("Label", typeof(RectTransform));
            buyLabelGO.transform.SetParent(buyVisual.transform, false);
            var buyLabelRt = buyLabelGO.GetComponent<RectTransform>();
            buyLabelRt.anchorMin = Vector2.zero;
            buyLabelRt.anchorMax = Vector2.one;
            buyLabelRt.offsetMin = Vector2.zero;
            buyLabelRt.offsetMax = Vector2.zero;
            var buyLabel = buyLabelGO.AddComponent<Text>();
            buyLabel.font = PickupCentFonts.Default;
            buyLabel.text = "구매";
            buyLabel.color = PickupCentPalette.Ink;
            buyLabel.fontStyle = FontStyle.Bold;
            buyLabel.fontSize = 13;
            buyLabel.alignment = TextAnchor.MiddleCenter;

            if (upgradeManager != null && definition != null)
                buyButton.onClick.AddListener(() => upgradeManager.TryPurchase(definition));
        }

        /// <summary>
        /// 강화 4종을 한눈에 구분하기 위한 임시 색상 스와치. 팔레트의 골드/브라운 계열 색만 쓰면
        /// 항목끼리 너무 비슷해 보인다는 문제가 있었어서, 여기서는 일부러 팔레트 밖의 뚜렷하게
        /// 다른 색(주황/초록/파랑/보라)을 아이콘 전용으로 골랐다 — 실제 아이콘 아트가 들어오기 전까지의
        /// 구분용 임시 표시일 뿐, 스타일 가이드 팔레트를 대체하는 것은 아니다.
        /// </summary>
        private static Color IconColorFor(UpgradeDefinition def)
        {
            if (def == null) return PickupCentPalette.WoodLight;
            return def.type switch
            {
                UpgradeDefinition.UpgradeType.DigStrength => HexColor("#E8734A"), // 주황
                UpgradeDefinition.UpgradeType.DigRange => HexColor("#6FBF73"), // 초록
                UpgradeDefinition.UpgradeType.ShovelStability => HexColor("#5B9BD5"), // 파랑
                UpgradeDefinition.UpgradeType.DetectRange => HexColor("#B47EDE"), // 보라
                _ => PickupCentPalette.WoodLight
            };
        }

        private static Color HexColor(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.white;
        }

        /// <summary>상점 패널이 매 프레임(패널이 열려 있을 때만) 호출한다.</summary>
        public void Refresh(int currentScore)
        {
            if (definition == null || upgradeManager == null) return;

            int level = upgradeManager.GetLevel(definition);
            bool maxed = level >= definition.maxLevel;
            int cost = maxed ? 0 : definition.GetCostForLevel(level);

            if (levelText != null) levelText.text = $"Lv.{level}/{definition.maxLevel}";
            if (costText != null) costText.text = maxed ? "MAX" : cost.ToString();
            if (buyButton != null) buyButton.interactable = !maxed && currentScore >= cost;
        }
    }
}
