using UnityEngine;

namespace PickupCent.Economy
{
    /// <summary>
    /// 누적 점수(돈)를 추적하는 임시 트래커.
    /// 정식 UI는 다음 단계 — 지금은 로그 + 화면 좌상단 숫자 표시 정도로만 확인한다.
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

        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(16, 16, 300, 40), $"점수: {score}", style);
        }
    }
}
