using PickupCent.Events;
using UnityEngine;
using UnityEngine.UI;

namespace PickupCent.UI
{
    /// <summary>다음 아이 무리 이벤트까지 남은 시간을 표시하고, 이벤트 진행 중엔 진행 표시로 바뀐다.</summary>
    public class SwarmEventCountdownText : MonoBehaviour
    {
        [SerializeField] private ChildrenSwarmEvent swarmEvent;
        [SerializeField] private Text text;

        private void Awake()
        {
            if (swarmEvent == null) swarmEvent = FindFirstObjectByType<ChildrenSwarmEvent>();
        }

        private void Update()
        {
            if (swarmEvent == null || text == null) return;

            text.text = swarmEvent.IsEventRunning
                ? "아이 무리 지나가는 중!"
                : $"다음 아이 무리: {swarmEvent.SecondsUntilNextEvent:F0}초";
        }
    }
}
