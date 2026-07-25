using PickupCent.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace PickupCent.UI
{
    /// <summary>상단 HUD의 점수 표시. Economy 로직은 건드리지 않고 ScoreTracker.Score만 읽어서 표시한다.</summary>
    public class HudController : MonoBehaviour
    {
        [SerializeField] private ScoreTracker scoreTracker;
        [SerializeField] private Text scoreText;

        private int lastShown = int.MinValue;

        private void Awake()
        {
            if (scoreTracker == null) scoreTracker = FindFirstObjectByType<ScoreTracker>();
        }

        private void Update()
        {
            if (scoreTracker == null || scoreText == null) return;
            if (scoreTracker.Score == lastShown) return;

            lastShown = scoreTracker.Score;
            scoreText.text = $"점수: {lastShown}";
        }
    }
}
