using PickupCent.Economy;
using PickupCent.Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace PickupCent.UI
{
    /// <summary>상점 패널 열기/닫기와, 열려 있는 동안 각 강화 줄(ShopRowView)의 갱신을 담당한다.</summary>
    public class ShopPanelController : MonoBehaviour
    {
        [SerializeField] private ScoreTracker scoreTracker;
        [SerializeField] private UpgradeManager upgradeManager;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button toggleButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private ShopRowView[] rows;

        private void Awake()
        {
            if (scoreTracker == null) scoreTracker = FindFirstObjectByType<ScoreTracker>();
            if (upgradeManager == null) upgradeManager = FindFirstObjectByType<UpgradeManager>();

            if (toggleButton != null) toggleButton.onClick.AddListener(TogglePanel);
            if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void TogglePanel()
        {
            if (panelRoot == null) return;
            panelRoot.SetActive(!panelRoot.activeSelf);
        }

        private void ClosePanel()
        {
            if (panelRoot == null) return;
            panelRoot.SetActive(false);
        }

        private void Update()
        {
            if (panelRoot == null || !panelRoot.activeSelf || scoreTracker == null || rows == null) return;

            foreach (var row in rows)
                row?.Refresh(scoreTracker.Score);
        }
    }
}
