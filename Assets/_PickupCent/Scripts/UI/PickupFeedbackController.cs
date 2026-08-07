using PickupCent.Common;
using PickupCent.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace PickupCent.UI
{
    /// <summary>아이템 습득 시 "이름 +가치" 텍스트를 잠깐 띄운다. ItemSpawner.OnItemPickedUp을 구독.
    /// 참고 목업(2번 이미지)에 맞춰 연노랑 배경의 말풍선(꼬리 달린 팝업)으로 표시한다 —
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
            if (itemSpawner != null) itemSpawner.OnItemPickedUpWithAmount += HandlePickedUp;

            CleanUpLegacyElements();
            BuildUI();
        }

        private void OnDestroy()
        {
            if (itemSpawner != null) itemSpawner.OnItemPickedUpWithAmount -= HandlePickedUp;
        }

        /// <summary>Test5UISetup(예전 에디터 메뉴)가 만들어 뒀던 습득 피드백 텍스트는 이제 이 컴포넌트가
        /// 새로 만든 알약(pill)으로 완전히 대체됐다 — 안 쓰는 옛 텍스트 오브젝트를 지운다.</summary>
        private void CleanUpLegacyElements()
        {
            var oldFeedback = GameObject.Find("PickupFeedbackText");
            if (oldFeedback != null) UICanvasUtility.DestroyObjectSafe(oldFeedback);
        }

        private void BuildUI()
        {
            var canvasGO = UICanvasUtility.EnsureCanvas();

            const int width = 200;
            const int height = 54;

            var existing = canvasGO.transform.Find("PickupFeedback");
            pillGO = existing != null
                ? existing.gameObject
                : new GameObject("PickupFeedback", typeof(RectTransform));
            pillGO.transform.SetParent(canvasGO.transform, false);
            UICanvasUtility.ClearChildrenSafe(pillGO.transform);
            var rt = pillGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(-152f, 250f);
            rt.sizeDelta = new Vector2(width, height);

            var borderImage = pillGO.GetComponent<Image>() ?? pillGO.AddComponent<Image>();
            borderImage.sprite = ProceduralSprites.CreatePill(width, height, PickupCentPalette.PopupBorder);

            var bgGO = new GameObject("Background", typeof(RectTransform));
            bgGO.transform.SetParent(pillGO.transform, false);
            var bgRt = bgGO.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = new Vector2(2f, 2f);
            bgRt.offsetMax = new Vector2(-2f, -2f);
            var bgImage = bgGO.AddComponent<Image>();
            bgImage.sprite = ProceduralSprites.CreatePill(width - 4, height - 4, PickupCentPalette.PopupBg);

            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(bgGO.transform, false);
            var textRt = textGO.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(10f, 0f);
            textRt.offsetMax = new Vector2(-10f, -6f);
            feedbackText = textGO.AddComponent<Text>();
            feedbackText.font = PickupCentFonts.Title;
            feedbackText.fontStyle = FontStyle.Bold;
            feedbackText.fontSize = 18;
            feedbackText.color = PickupCentPalette.PopupText;
            feedbackText.alignment = TextAnchor.MiddleCenter;

            pillGO.SetActive(false);
        }

        private void HandlePickedUp(ItemDefinition def, int earnedAmount)
        {
            if (feedbackText == null || def == null) return;
            feedbackText.text = $"{def.itemName} +{earnedAmount}";
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
