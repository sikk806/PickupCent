using PickupCent.Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace PickupCent.UI
{
    /// <summary>상점 패널 안의 강화 한 줄 — 이름/레벨/다음 비용/구매 버튼을 표시하고 갱신한다.</summary>
    public class ShopRowView : MonoBehaviour
    {
        [SerializeField] private UpgradeDefinition definition;
        [SerializeField] private UpgradeManager upgradeManager;

        [SerializeField] private Text nameText;
        [SerializeField] private Text levelText;
        [SerializeField] private Text costText;
        [SerializeField] private Button buyButton;

        public UpgradeDefinition Definition => definition;

        private void Awake()
        {
            if (upgradeManager == null) upgradeManager = FindFirstObjectByType<UpgradeManager>();
            if (nameText != null) nameText.text = definition != null ? definition.upgradeName : "-";
            if (buyButton != null)
                buyButton.onClick.AddListener(() => upgradeManager.TryPurchase(definition));
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
