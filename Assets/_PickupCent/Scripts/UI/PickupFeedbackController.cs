using PickupCent.Common;
using PickupCent.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace PickupCent.UI
{
    /// <summary>아이템 습득 시 "이름 +가치" 텍스트를 잠깐 띄운다. ItemSpawner.OnItemPickedUp을 구독.
    /// 스타일 가이드에 맞춰 상단 HUD 알약 바로 아래에 금색 강조 알약(pill) 배지로 표시한다 —
    /// 어떤 이벤트에 반응해서 무엇을 보여주는지는 그대로, 모양만 바뀐 것이다.</summary>
    public class PickupFeedbackController : MonoBehaviour
    {
        [SerializeField] private ItemSpawner itemSpawner;
        [SerializeField] private float displayDuration = 1.2f;

        private GameObject pillGO;
        private Text feedbackText;
        private float timer;

        private void Awake()
        {
            if (itemSpawner == null) itemSpawner = FindFirstObjectByType<ItemSpawner>();
            if (itemSpawner != null) itemSpawner.OnItemPickedUp += HandlePickedUp;

            BuildUI();
        }

        private void OnDestroy()
        {
            if (itemSpawner != null) itemSpawner.OnItemPickedUp -= HandlePickedUp;
        }

        private void BuildUI()
        {
            var canvasGO = UICanvasUtility.EnsureCanvas();

            const int width = 220;
            const int height = 36;

            pillGO = new GameObject("PickupFeedback", typeof(RectTransform));
            pillGO.transform.SetParent(canvasGO.transform, false);
            var rt = pillGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -70f);
            rt.sizeDelta = new Vector2(width, height);

            var borderImage = pillGO.AddComponent<Image>();
            borderImage.sprite = ProceduralSprites.CreatePill(width, height, PickupCentPalette.ButtonBottomBorder);

            var bgGO = new GameObject("Background", typeof(RectTransform));
            bgGO.transform.SetParent(pillGO.transform, false);
            var bgRt = bgGO.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = new Vector2(2f, 2f);
            bgRt.offsetMax = new Vector2(-2f, -2f);
            bgGO.AddComponent<Image>().sprite = ProceduralSprites.CreatePill(width - 4, height - 4, PickupCentPalette.PanelBgSolid);

            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(bgGO.transform, false);
            var textRt = textGO.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            feedbackText = textGO.AddComponent<Text>();
            feedbackText.font = PickupCentFonts.Title;
            feedbackText.fontStyle = FontStyle.Bold;
            feedbackText.fontSize = 18;
            feedbackText.color = PickupCentPalette.GoldBright;
            feedbackText.alignment = TextAnchor.MiddleCenter;

            pillGO.SetActive(false);
        }

        private void HandlePickedUp(ItemDefinition def)
        {
            if (feedbackText == null || def == null) return;
            feedbackText.text = $"{def.itemName} +{def.value}";
            pillGO.SetActive(true);
            timer = displayDuration;
        }

        private void Update()
        {
            if (pillGO == null || !pillGO.activeSelf) return;

            timer -= Time.deltaTime;
            if (timer <= 0f) pillGO.SetActive(false);
        }
    }
}
