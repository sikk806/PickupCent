using UnityEngine;

namespace PickupCent.Economy
{
    /// <summary>
    /// README에 없는 보상 배율은 만들지 않고, 짧은 시간 안의 연속 수집 횟수와 최고 콤보만 추적한다.
    /// </summary>
    public class ComboTracker : MonoBehaviour
    {
        [SerializeField] private float comboWindowSeconds = 4f;
        [SerializeField] private ItemSpawner itemSpawner;

        private int currentCombo;
        private int bestCombo;
        private float remainingSeconds;

        public int CurrentCombo => currentCombo;
        public int BestCombo => bestCombo;
        public bool IsActive => currentCombo > 0 && remainingSeconds > 0f;
        public float NormalizedRemaining => comboWindowSeconds <= 0f ? 0f : Mathf.Clamp01(remainingSeconds / comboWindowSeconds);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInScene()
        {
            if (FindFirstObjectByType<ComboTracker>() != null) return;
            if (FindFirstObjectByType<ItemSpawner>() == null) return;
            new GameObject("ComboTracker").AddComponent<ComboTracker>();
        }

        private void Awake()
        {
            if (itemSpawner == null) itemSpawner = FindFirstObjectByType<ItemSpawner>();
        }

        private void OnEnable()
        {
            if (itemSpawner == null) itemSpawner = FindFirstObjectByType<ItemSpawner>();
            if (itemSpawner != null) itemSpawner.OnItemPickedUp += HandleItemPickedUp;
        }

        private void OnDisable()
        {
            if (itemSpawner != null) itemSpawner.OnItemPickedUp -= HandleItemPickedUp;
        }

        private void Update()
        {
            if (currentCombo <= 0) return;

            remainingSeconds -= Time.deltaTime;
            if (remainingSeconds <= 0f)
            {
                currentCombo = 0;
                remainingSeconds = 0f;
            }
        }

        private void HandleItemPickedUp(ItemDefinition _)
        {
            currentCombo = IsActive ? currentCombo + 1 : 1;
            remainingSeconds = comboWindowSeconds;
            if (currentCombo > bestCombo) bestCombo = currentCombo;
        }
    }
}
