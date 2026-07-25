using PickupCent.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace PickupCent.UI
{
    /// <summary>아이템 습득 시 "이름 +가치" 텍스트를 잠깐 띄운다. ItemSpawner.OnItemPickedUp을 구독.</summary>
    public class PickupFeedbackController : MonoBehaviour
    {
        [SerializeField] private ItemSpawner itemSpawner;
        [SerializeField] private Text feedbackText;
        [SerializeField] private float displayDuration = 1.2f;

        private float timer;

        private void Awake()
        {
            if (itemSpawner == null) itemSpawner = FindFirstObjectByType<ItemSpawner>();
            if (itemSpawner != null) itemSpawner.OnItemPickedUp += HandlePickedUp;
            if (feedbackText != null) feedbackText.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (itemSpawner != null) itemSpawner.OnItemPickedUp -= HandlePickedUp;
        }

        private void HandlePickedUp(ItemDefinition def)
        {
            if (feedbackText == null || def == null) return;
            feedbackText.text = $"{def.itemName} +{def.value}";
            feedbackText.gameObject.SetActive(true);
            timer = displayDuration;
        }

        private void Update()
        {
            if (feedbackText == null || !feedbackText.gameObject.activeSelf) return;

            timer -= Time.deltaTime;
            if (timer <= 0f) feedbackText.gameObject.SetActive(false);
        }
    }
}
