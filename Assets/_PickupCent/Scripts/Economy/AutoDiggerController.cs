using UnityEngine;

namespace PickupCent.Economy
{
    /// <summary>
    /// HTML 프로토타입의 자동 파기 장치에 대응하는 경량 자동화 상태. 구매 후 주기적으로 소액을 벌고 내구도를 소모한다.
    /// </summary>
    public class AutoDiggerController : MonoBehaviour
    {
        [SerializeField] private ScoreTracker scoreTracker;
        [SerializeField] private int purchaseCost = 420;
        [SerializeField] private int repairCostPerPoint = 2;
        [SerializeField] private float maxDurability = 150f;
        [SerializeField] private float durability;
        [SerializeField] private float tickInterval = 5f;
        [SerializeField] private int incomePerTick = 6;
        [SerializeField] private bool owned;

        private float timer;
        private int speedLevel;
        private int incomeLevel;
        private int durabilityLevel;

        public bool Owned => owned;
        public float Durability => durability;
        public float MaxDurability => maxDurability + durabilityLevel * 35f;
        public int PurchaseCost => purchaseCost;
        public int SpeedLevel => speedLevel;
        public int IncomeLevel => incomeLevel;
        public int DurabilityLevel => durabilityLevel;
        public int MaxUpgradeLevel => 5;
        public float NormalizedCooldown => owned ? Mathf.Clamp01(timer / Mathf.Max(0.1f, CurrentInterval)) : 0f;
        public float CurrentInterval => Mathf.Max(1.2f, tickInterval - speedLevel * 0.45f);
        public int CurrentIncome => incomePerTick + incomeLevel * 4;
        public int RepairCost => Mathf.CeilToInt(Mathf.Max(0f, MaxDurability - durability) * repairCostPerPoint);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInScene()
        {
            if (FindFirstObjectByType<AutoDiggerController>() != null) return;
            if (FindFirstObjectByType<ScoreTracker>() == null) return;
            new GameObject("AutoDiggerController").AddComponent<AutoDiggerController>();
        }

        private void Awake()
        {
            if (scoreTracker == null) scoreTracker = FindFirstObjectByType<ScoreTracker>();
        }

        private void Update()
        {
            if (!owned || durability <= 0f || scoreTracker == null) return;

            timer += Time.deltaTime;
            if (timer < CurrentInterval) return;

            timer = 0f;
            durability = Mathf.Max(0f, durability - 1f);
            scoreTracker.Add(CurrentIncome, "자동 파기 장치");
        }

        public bool TryPurchase(ScoreTracker tracker)
        {
            if (owned) return true;
            if (tracker == null || tracker.Score < purchaseCost) return false;
            tracker.Spend(purchaseCost, "자동 파기 장치 구매");
            owned = true;
            durability = MaxDurability;
            timer = 0f;
            return true;
        }

        public bool TryRepair(ScoreTracker tracker)
        {
            int cost = RepairCost;
            if (!owned || cost <= 0 || tracker == null || tracker.Score < cost) return false;
            tracker.Spend(cost, "자동 파기 장치 수리");
            durability = MaxDurability;
            return true;
        }

        public bool TryUpgradeSpeed(ScoreTracker tracker) => TryUpgrade(tracker, ref speedLevel, 140, "속도 강화");
        public bool TryUpgradeIncome(ScoreTracker tracker) => TryUpgrade(tracker, ref incomeLevel, 160, "수익 강화");
        public bool TryUpgradeDurability(ScoreTracker tracker) => TryUpgrade(tracker, ref durabilityLevel, 150, "내구도 강화");

        public int GetUpgradeCost(int level, int baseCost) => Mathf.RoundToInt(baseCost * Mathf.Pow(1.55f, level));

        private bool TryUpgrade(ScoreTracker tracker, ref int level, int baseCost, string reason)
        {
            if (!owned || level >= MaxUpgradeLevel || tracker == null) return false;
            int cost = GetUpgradeCost(level, baseCost);
            if (tracker.Score < cost) return false;
            tracker.Spend(cost, $"자동 장치 {reason}");
            level++;
            if (reason.Contains("내구도")) durability = MaxDurability;
            return true;
        }
    }
}
