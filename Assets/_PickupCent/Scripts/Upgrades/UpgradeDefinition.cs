using UnityEngine;

namespace PickupCent.Upgrades
{
    /// <summary>
    /// 강화 한 종류의 데이터. README 7장 참고. 수치는 전부 예시값이며 추후 밸런싱 대상이다.
    /// </summary>
    [CreateAssetMenu(menuName = "PickupCent/Upgrade Definition", fileName = "NewUpgradeDefinition")]
    public class UpgradeDefinition : ScriptableObject
    {
        public enum UpgradeType
        {
            DigStrength,     // 파기 강도 강화 (손/삽 공통)
            DigRange,        // 파기 범위 강화 (브러시 반경)
            ShovelStability, // 삽 안정성 강화 (파괴 확률 감소)
            DetectRange,     // 탐지 범위 강화

            /// <summary>
            /// 탐지 대상 확장 (기획서 3-2: "동전 외 다른 아이템도 탐지 가능하게 할지는 고려 중").
            /// 이번 단계에서는 자리만 만들어두고 로직은 구현하지 않는다 — UpgradeManager.ApplyEffect의
            /// 해당 case에 TODO로 남겨둠. 도입이 확정되면 ItemDefinition.detectableByMetalDetector를
            /// 강제로 true로 바꿔줄 대상 범위를 여기서 정의하는 방식이 될 것으로 예상.
            /// </summary>
            DetectionTargetExpansion,
        }

        [Header("기본 정보")]
        public string upgradeName = "New Upgrade";
        public UpgradeType type;

        [Header("비용 (레벨업할 때마다 costMultiplier만큼 증가)")]
        [Tooltip("Lv.0 → Lv.1로 올릴 때의 비용")]
        public int baseCost = 20;
        [Tooltip("레벨 하나 오를 때마다 비용에 곱해지는 배수. 1이면 항상 동일 비용")]
        public float costMultiplier = 1.6f;

        [Header("효과")]
        [Tooltip("레벨 1당 증가/감소하는 효과치. 강도·범위는 +값, 파괴확률 감소량도 +값(감소량 자체)으로 사용")]
        public float effectPerLevel = 1f;
        public int maxLevel = 5;

        /// <summary>currentLevel에서 다음 레벨로 올리는 데 필요한 비용.</summary>
        public int GetCostForLevel(int currentLevel)
        {
            return Mathf.RoundToInt(baseCost * Mathf.Pow(costMultiplier, currentLevel));
        }
    }
}
