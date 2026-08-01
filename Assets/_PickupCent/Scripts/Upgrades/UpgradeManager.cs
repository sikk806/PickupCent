using System;
using System.Collections.Generic;
using PickupCent.Digging;
using PickupCent.Economy;
using UnityEngine;

namespace PickupCent.Upgrades
{
    /// <summary>
    /// 강화 구매 로직. TryPurchase()가 성공하면 SandMaskController/ToolManager에 즉시 효과를 반영한다.
    /// 구매 입력은 PickupCent.UI.ShopPanelController의 버튼이 TryPurchase()를 호출하는 방식으로 처리한다
    /// (예전엔 키보드 Q/W/E/R이었으나 정식 상점 UI로 대체됨).
    /// </summary>
    public class UpgradeManager : MonoBehaviour
    {
        [SerializeField] private ScoreTracker scoreTracker;
        [SerializeField] private SandMaskController sandMask;
        [SerializeField] private ToolManager toolManager;

        [Header("강화 4종 (상점 UI에서 구매)")]
        [SerializeField] private UpgradeDefinition digStrengthDef;
        [SerializeField] private UpgradeDefinition digRangeDef;
        [SerializeField] private UpgradeDefinition shovelStabilityDef;
        [SerializeField] private UpgradeDefinition detectRangeDef;

        [Header("탐지 대상 확장 (미구현, 자리만)")]
        [Tooltip("TODO: 기획서 3-2 - 도입 여부 고려 중. 키 바인딩 없음, 로직 없음.")]
        [SerializeField] private UpgradeDefinition detectionTargetExpansionDef;

        private readonly Dictionary<UpgradeDefinition, int> levels = new Dictionary<UpgradeDefinition, int>();

        /// <summary>구매 성공 시 발생 — 사운드 등 알림용.</summary>
        public event Action<UpgradeDefinition> OnPurchaseSucceeded;
        /// <summary>구매 실패 시 발생(최대 레벨이든 점수 부족이든 둘 다) — 사운드 등 알림용.</summary>
        public event Action<UpgradeDefinition> OnPurchaseFailed;

        private void Awake()
        {
            if (scoreTracker == null) scoreTracker = FindFirstObjectByType<ScoreTracker>();
            if (sandMask == null) sandMask = FindFirstObjectByType<SandMaskController>();
            if (toolManager == null) toolManager = FindFirstObjectByType<ToolManager>();
        }

        public int GetLevel(UpgradeDefinition def)
        {
            if (def == null) return 0;
            return levels.TryGetValue(def, out var lv) ? lv : 0;
        }

        /// <summary>상점 UI의 구매 버튼이 호출한다.</summary>
        public void TryPurchase(UpgradeDefinition def)
        {
            if (def == null || scoreTracker == null) return;

            int level = GetLevel(def);
            if (level >= def.maxLevel)
            {
                Debug.Log($"[Upgrade] 구매 실패: {def.upgradeName} 이미 최대 레벨(Lv.{def.maxLevel})");
                OnPurchaseFailed?.Invoke(def);
                return;
            }

            int cost = def.GetCostForLevel(level);
            if (scoreTracker.Score < cost)
            {
                Debug.Log($"[Upgrade] 구매 실패: 점수 부족(필요 {cost}, 보유 {scoreTracker.Score})");
                OnPurchaseFailed?.Invoke(def);
                return;
            }

            scoreTracker.Spend(cost);
            level++;
            levels[def] = level;
            ApplyEffect(def);

            Debug.Log($"[Upgrade] 구매 성공: {def.upgradeName} Lv.{level} (남은 점수: {scoreTracker.Score})");
            OnPurchaseSucceeded?.Invoke(def);
        }

        private void ApplyEffect(UpgradeDefinition def)
        {
            switch (def.type)
            {
                case UpgradeDefinition.UpgradeType.DigStrength:
                    toolManager?.AddStrengthBonus(def.effectPerLevel);
                    break;

                case UpgradeDefinition.UpgradeType.DigRange:
                    toolManager?.AddBrushRadiusBonus(def.effectPerLevel);
                    break;

                case UpgradeDefinition.UpgradeType.ShovelStability:
                    toolManager?.ReduceShovelDestroyChance(def.effectPerLevel);
                    break;

                case UpgradeDefinition.UpgradeType.DetectRange:
                    toolManager?.AddDetectorRadius(def.effectPerLevel);
                    break;

                case UpgradeDefinition.UpgradeType.DetectionTargetExpansion:
                    // TODO: 탐지 대상 확장 미구현 (기획서 3-2, 도입 여부 고려 중).
                    // 구현 시 여기서 코인 외 ItemDefinition의 detectableByMetalDetector를
                    // 레벨에 따라 켜주는 방식이 될 것으로 예상.
                    break;
            }
        }

        // 상점 UI(ShopPanelController)가 각 강화의 이름/레벨/비용을 나열하는 데 쓴다.
        public UpgradeDefinition DigStrengthDef => digStrengthDef;
        public UpgradeDefinition DigRangeDef => digRangeDef;
        public UpgradeDefinition ShovelStabilityDef => shovelStabilityDef;
        public UpgradeDefinition DetectRangeDef => detectRangeDef;
    }
}

