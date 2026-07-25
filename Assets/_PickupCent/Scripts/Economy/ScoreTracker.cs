using UnityEngine;

namespace PickupCent.Economy
{
    /// <summary>
    /// 누적 점수(돈)를 추적한다. 화면 표시는 PickupCent.UI.HudController가 Score를 읽어서 담당한다.
    /// </summary>
    public class ScoreTracker : MonoBehaviour
    {
        [SerializeField] private int score;

        public int Score => score;

        public void Add(int amount, string itemName)
        {
            score += amount;
            Debug.Log($"[Score] +{amount} ({itemName}) → 누적 {score}");
        }

        /// <summary>강화 구매 등으로 점수를 소비한다. 잔액 확인은 호출 측(UpgradeManager)에서 먼저 한다.</summary>
        public void Spend(int amount)
        {
            score -= amount;
            Debug.Log($"[Score] -{amount} (강화 구매) → 누적 {score}");
        }
    }
}
