using UnityEngine;

namespace PickupCent.Economy
{
    /// <summary>
    /// 누적 점수(돈)를 추적한다. 화면 표시는 PickupCent.UI.HudController가 Score를 읽어서 담당한다.
    /// </summary>
    public class ScoreTracker : MonoBehaviour
    {
        [SerializeField] private int score;
        [SerializeField] private float incomeMultiplier = 1f;

        public int Score => score;
        public float IncomeMultiplier => incomeMultiplier;

        public void Add(int amount, string itemName)
        {
            int earned = Mathf.Max(0, Mathf.RoundToInt(amount * incomeMultiplier));
            score += earned;
            Debug.Log($"[Score] +{earned} ({itemName}, 기본 {amount}, 배율 x{incomeMultiplier:0.00}) → 누적 {score}");
        }

        public void Spend(int amount)
        {
            Spend(amount, "구매");
        }

        /// <summary>강화/도구/이벤트 구매 등으로 점수를 소비한다. 잔액 확인은 호출 측에서 먼저 한다.</summary>
        public void Spend(int amount, string reason)
        {
            score -= amount;
            Debug.Log($"[Score] -{amount} ({reason}) → 누적 {score}");
        }

        public void AddIncomeMultiplier(float amount)
        {
            incomeMultiplier = Mathf.Max(0.1f, incomeMultiplier + amount);
            Debug.Log($"[Score] 수익 배율 +{amount:0.00} → x{incomeMultiplier:0.00}");
        }
    }
}
