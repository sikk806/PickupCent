using PickupCent.Common;
using PickupCent.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace PickupCent.UI
{
    /// <summary>
    /// 오른쪽 사이드 버튼과 중앙 일시정지 모달. 열릴 때 게임 시간을 멈추고 배경 입력/시야를 차단한다.
    /// </summary>
    public class PausePanelController : MonoBehaviour
    {
        [SerializeField] private ScoreTracker scoreTracker;
        [SerializeField] private ComboTracker comboTracker;

        private GameObject overlayRoot;
        private bool isOpen;
        private float playSeconds;
        private Text moneyValueText;
        private Text timeValueText;
        private Text comboValueText;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInScene()
        {
            if (FindFirstObjectByType<PausePanelController>() != null) return;
            if (FindFirstObjectByType<ScoreTracker>() == null) return;
            new GameObject("PausePanelController").AddComponent<PausePanelController>();
        }

        private void Awake()
        {
            if (scoreTracker == null) scoreTracker = FindFirstObjectByType<ScoreTracker>();
            if (comboTracker == null) comboTracker = FindFirstObjectByType<ComboTracker>();
            BuildUI();
        }

        private void OnDestroy()
        {
            if (isOpen) PopupPauseManager.PopPause();
        }

        private void Update()
        {
            if (!PopupPauseManager.IsPausedByPopup) playSeconds += Time.deltaTime;
            if ((isOpen || !PopupPauseManager.IsPausedByPopup) && Input.GetKeyDown(KeyCode.Escape)) TogglePause();
            if (isOpen) RefreshStats();
        }

        private void BuildUI()
        {
            CreateSideButton(UICanvasUtility.EnsureSidePanel());
            CreateOverlay();
        }

        private void CreateSideButton(Transform sidePanel)
        {
            var existing = sidePanel.Find("PauseToggleButton");
            if (existing != null)
            {
                var existingButton = existing.GetComponentInChildren<Button>();
                if (existingButton != null)
                {
                    existingButton.onClick.RemoveListener(TogglePause);
                    existingButton.onClick.AddListener(TogglePause);
                }
                return;
            }

            var normalSprite = ProceduralSprites.CreateGradientButtonSliced(48, 12f,
                PickupCentPalette.Gold, PickupCentPalette.WoodLight, 3f, PickupCentPalette.ButtonBottomBorder);
            var pressedSprite = ProceduralSprites.CreateGradientButtonSliced(48, 12f,
                PickupCentPalette.Gold, PickupCentPalette.WoodLight, 1f, PickupCentPalette.ButtonBottomBorder);

            var go = new GameObject("PauseToggleButton", typeof(RectTransform));
            go.transform.SetParent(sidePanel, false);
            go.AddComponent<LayoutElement>().preferredHeight = 50f;

            var visual = UICanvasUtility.CreatePressableSurface(go.transform, normalSprite, pressedSprite, out var button, out _);
            button.onClick.AddListener(TogglePause);

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(visual.transform, false);
            Stretch((RectTransform)labelGO.transform);
            var label = labelGO.AddComponent<Text>();
            label.font = PickupCentFonts.Title;
            label.text = "일시정지";
            label.color = PickupCentPalette.Ink;
            label.fontStyle = FontStyle.Bold;
            label.fontSize = 17;
            label.alignment = TextAnchor.MiddleCenter;
        }

        private void CreateOverlay()
        {
            var stageRoot = UICanvasUtility.EnsureStageRoot();
            var existing = stageRoot.Find("PauseOverlay");
            if (existing != null)
            {
                overlayRoot = existing.gameObject;
                WireExistingOverlay();
                return;
            }

            overlayRoot = new GameObject("PauseOverlay", typeof(RectTransform));
            overlayRoot.transform.SetParent(stageRoot, false);
            Stretch((RectTransform)overlayRoot.transform);
            var backdrop = overlayRoot.AddComponent<Image>();
            backdrop.color = PickupCentPalette.WithAlpha(PickupCentPalette.PanelBgSolid, 0.97f);
            backdrop.raycastTarget = true;
            overlayRoot.AddComponent<CanvasGroup>().blocksRaycasts = true;

            var modal = new GameObject("PauseModal", typeof(RectTransform));
            modal.transform.SetParent(overlayRoot.transform, false);
            var modalRt = (RectTransform)modal.transform;
            modalRt.anchorMin = new Vector2(0.5f, 0.5f);
            modalRt.anchorMax = new Vector2(0.5f, 0.5f);
            modalRt.pivot = new Vector2(0.5f, 0.5f);
            modalRt.sizeDelta = new Vector2(430f, 330f);

            var layout = modal.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            CreateTitle(modal.transform);
            CreateStatsRow(modal.transform);
            CreateButton(modal.transform, "이어하기", 118f, 50f, PickupCentPalette.Gold, PickupCentPalette.WoodLight, PickupCentPalette.Ink, ClosePause);
            CreateButton(modal.transform, "타이틀로 나가기", 104f, 34f, PickupCentPalette.SecondaryButtonBg, PickupCentPalette.SecondaryButtonBg, PickupCentPalette.Cream, ReturnToTitle);

            overlayRoot.SetActive(false);
        }

        private void CreateTitle(Transform parent)
        {
            var go = new GameObject("Title", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = 58f;
            var text = go.AddComponent<Text>();
            text.font = PickupCentFonts.Title;
            text.text = "일시정지";
            text.color = PickupCentPalette.GoldBright;
            text.fontSize = 38;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
        }

        private void CreateStatsRow(Transform parent)
        {
            var row = new GameObject("Stats", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            row.AddComponent<LayoutElement>().preferredHeight = 76f;
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childAlignment = TextAnchor.MiddleCenter;

            CreateStatCard(row.transform, "보유 금액", "0", out moneyValueText);
            CreateStatCard(row.transform, "플레이 시간", "00:00", out timeValueText);
            CreateStatCard(row.transform, "최고 콤보", "0", out comboValueText);
        }

        private void CreateStatCard(Transform parent, string label, string value, out Text valueText)
        {
            var go = new GameObject($"Stat_{label}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredWidth = 102f;

            var bg = go.AddComponent<Image>();
            bg.sprite = ProceduralSprites.CreateRoundedRectSliced(48, 10f, PickupCentPalette.SecondaryButtonBg);
            bg.type = Image.Type.Sliced;

            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.spacing = 4f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(go.transform, false);
            labelGO.AddComponent<LayoutElement>().preferredHeight = 22f;
            var labelText = labelGO.AddComponent<Text>();
            labelText.font = PickupCentFonts.Default;
            labelText.text = label;
            labelText.color = PickupCentPalette.WithAlpha(PickupCentPalette.Cream, 0.7f);
            labelText.fontSize = 12;
            labelText.alignment = TextAnchor.MiddleCenter;

            var valueGO = new GameObject("Value", typeof(RectTransform));
            valueGO.transform.SetParent(go.transform, false);
            valueGO.AddComponent<LayoutElement>().preferredHeight = 30f;
            valueText = valueGO.AddComponent<Text>();
            valueText.font = PickupCentFonts.Title;
            valueText.text = value;
            valueText.color = PickupCentPalette.GoldBright;
            valueText.fontSize = 17;
            valueText.fontStyle = FontStyle.Bold;
            valueText.alignment = TextAnchor.MiddleCenter;
        }

        private void CreateButton(Transform parent, string label, float width, float height, Color top, Color bottom, Color textColor, UnityEngine.Events.UnityAction onClick)
        {
            var normalSprite = ProceduralSprites.CreateGradientButtonSliced(48, 12f, top, bottom, 3f, PickupCentPalette.ButtonBottomBorder);
            var pressedSprite = ProceduralSprites.CreateGradientButtonSliced(48, 12f, top, bottom, 1f, PickupCentPalette.ButtonBottomBorder);
            var go = new GameObject($"Button_{label}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredWidth = width;
            go.GetComponent<LayoutElement>().preferredHeight = height;

            var visual = UICanvasUtility.CreatePressableSurface(go.transform, normalSprite, pressedSprite, out var button, out _);
            button.onClick.AddListener(onClick);

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(visual.transform, false);
            Stretch((RectTransform)labelGO.transform);
            var text = labelGO.AddComponent<Text>();
            text.font = PickupCentFonts.Title;
            text.text = label;
            text.color = textColor;
            text.fontSize = height > 40f ? 18 : 13;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
        }

        private void WireExistingOverlay()
        {
            moneyValueText = FindText("PauseModal/Stats/Stat_보유 금액/Value");
            timeValueText = FindText("PauseModal/Stats/Stat_플레이 시간/Value");
            comboValueText = FindText("PauseModal/Stats/Stat_최고 콤보/Value");
            WireButton("PauseModal/Button_이어하기/Visual", ClosePause);
            WireButton("PauseModal/Button_타이틀로 나가기/Visual", ReturnToTitle);
        }

        private Text FindText(string path)
        {
            var target = overlayRoot != null ? overlayRoot.transform.Find(path) : null;
            return target != null ? target.GetComponent<Text>() : null;
        }

        private void WireButton(string path, UnityEngine.Events.UnityAction action)
        {
            var target = overlayRoot != null ? overlayRoot.transform.Find(path) : null;
            var button = target != null ? target.GetComponent<Button>() : null;
            if (button == null) return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }
        private void TogglePause()
        {
            if (isOpen) ClosePause();
            else OpenPause();
        }

        private void OpenPause()
        {
            if (overlayRoot == null || isOpen) return;
            isOpen = true;
            overlayRoot.SetActive(true);
            PopupPauseManager.PushPause();
            RefreshStats();
        }

        private void ClosePause()
        {
            if (overlayRoot == null || !isOpen) return;
            isOpen = false;
            overlayRoot.SetActive(false);
            PopupPauseManager.PopPause();
        }

        private void ReturnToTitle()
        {
            if (overlayRoot != null) overlayRoot.SetActive(false);
            isOpen = false;
            PopupPauseManager.ForceClear();
            MainTitleScreenController.ShowTitleFromAnywhere();
        }

        private void RefreshStats()
        {
            if (comboTracker == null) comboTracker = FindFirstObjectByType<ComboTracker>();
            if (moneyValueText != null) moneyValueText.text = $"{(scoreTracker != null ? scoreTracker.Score : 0):N0}원";
            if (timeValueText != null) timeValueText.text = FormatTime(playSeconds);
            if (comboValueText != null) comboValueText.text = (comboTracker != null ? comboTracker.BestCombo : 0).ToString();
        }

        private static string FormatTime(float seconds)
        {
            int total = Mathf.FloorToInt(seconds);
            return $"{total / 60:00}:{total % 60:00}";
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}

