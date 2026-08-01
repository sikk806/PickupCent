using PickupCent.Common;
using PickupCent.Digging;
using PickupCent.Economy;
using PickupCent.Events;
using UnityEngine;
using UnityEngine.UI;

namespace PickupCent.UI
{
    /// <summary>
    /// 맵 위 최소 HUD. 기존 메인 화면은 유지하고, 상단 알약 배지에 현재 상태만 얹는다.
    /// </summary>
    public class HudController : MonoBehaviour
    {
        [SerializeField] private ScoreTracker scoreTracker;
        [SerializeField] private ChildrenSwarmEvent swarmEvent;
        [SerializeField] private ToolManager toolManager;
        [SerializeField] private ComboTracker comboTracker;

        [Tooltip("맵 위 지역명 배지에 표시할 고정 라벨(README 기준 스테이지1 = 놀이터)")]
        [SerializeField] private string regionName = "놀이터";

        private Text scoreValueText;
        private Text timerValueText;
        private Text toolValueText;
        private float playSeconds;

        private Font defaultFont;
        private int lastScore = int.MinValue;
        private string lastTimerText;
        private string lastToolText;
        private ToolManager.ToolType lastTool = (ToolManager.ToolType)(-1);

        private void Awake()
        {
            if (scoreTracker == null) scoreTracker = FindFirstObjectByType<ScoreTracker>();
            if (swarmEvent == null) swarmEvent = FindFirstObjectByType<ChildrenSwarmEvent>();
            if (toolManager == null) toolManager = FindFirstObjectByType<ToolManager>();
            if (comboTracker == null) comboTracker = FindFirstObjectByType<ComboTracker>();

            defaultFont = LoadDefaultFont();

            CleanUpLegacyElements();
            BuildPills();
        }

        private static Font LoadDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return font;
        }

        private void CleanUpLegacyElements()
        {
            var oldScore = GameObject.Find("ScoreText");
            if (oldScore != null) Destroy(oldScore);
            var oldTimer = GameObject.Find("SwarmCountdownText");
            if (oldTimer != null) Destroy(oldTimer);
        }

        private void BuildPills()
        {
            var row = UICanvasUtility.EnsureTopHudRow();

            CreatePill(row, regionName, "0원", PickupCentPalette.GoldBright, out scoreValueText);
            CreatePill(row, "⏱", "00:00", PickupCentPalette.Cream, out timerValueText);
            CreatePill(row, "도구", "손 ∞", PickupCentPalette.GoldBright, out toolValueText);
        }

        private void CreatePill(Transform parent, string label, string initialValue, Color valueColor, out Text valueText)
        {
            const int pillHeight = 40;
            const int pillWidth = 174;

            var pillGO = new GameObject($"Pill_{label}", typeof(RectTransform));
            pillGO.transform.SetParent(parent, false);
            var pillRt = pillGO.GetComponent<RectTransform>();
            pillRt.sizeDelta = new Vector2(pillWidth, pillHeight);
            pillGO.AddComponent<LayoutElement>().preferredWidth = pillWidth;

            var borderImage = pillGO.AddComponent<Image>();
            borderImage.sprite = ProceduralSprites.CreatePill(pillWidth, pillHeight, PickupCentPalette.BorderThin);
            borderImage.type = Image.Type.Simple;

            var bgGO = new GameObject("Background", typeof(RectTransform));
            bgGO.transform.SetParent(pillGO.transform, false);
            var bgRt = bgGO.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = new Vector2(2f, 2f);
            bgRt.offsetMax = new Vector2(-2f, -2f);
            var bgImage = bgGO.AddComponent<Image>();
            bgImage.sprite = ProceduralSprites.CreatePill(pillWidth - 4, pillHeight - 4, PickupCentPalette.HudPillBg);

            var contentGO = new GameObject("Content", typeof(RectTransform));
            contentGO.transform.SetParent(bgGO.transform, false);
            var contentRt = contentGO.GetComponent<RectTransform>();
            contentRt.anchorMin = Vector2.zero;
            contentRt.anchorMax = Vector2.one;
            contentRt.offsetMin = new Vector2(14f, 0f);
            contentRt.offsetMax = new Vector2(-14f, 0f);
            var hlg = contentGO.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(contentGO.transform, false);
            var labelText = labelGO.AddComponent<Text>();
            labelText.font = defaultFont;
            labelText.text = label;
            labelText.fontSize = 14;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = PickupCentPalette.Cream;
            labelGO.AddComponent<LayoutElement>().flexibleWidth = 1;

            var valueGO = new GameObject("Value", typeof(RectTransform));
            valueGO.transform.SetParent(contentGO.transform, false);
            valueText = valueGO.AddComponent<Text>();
            valueText.font = defaultFont;
            valueText.text = initialValue;
            valueText.fontSize = 16;
            valueText.fontStyle = FontStyle.Bold;
            valueText.alignment = TextAnchor.MiddleRight;
            valueText.color = valueColor;
            valueGO.AddComponent<LayoutElement>().preferredWidth = 54;
        }

        private void Update()
        {
            if (!PopupPauseManager.IsPausedByPopup) playSeconds += Time.deltaTime;

            if (scoreTracker != null && scoreValueText != null && scoreTracker.Score != lastScore)
            {
                lastScore = scoreTracker.Score;
                scoreValueText.text = $"{lastScore:N0}원";
            }

            if (timerValueText != null)
            {
                string text = FormatTime(playSeconds);
                if (text != lastTimerText)
                {
                    lastTimerText = text;
                    timerValueText.text = text;
                }
            }

            if (toolManager != null && toolValueText != null)
            {
                var tool = toolManager.CurrentTool;
                string value = tool == ToolManager.ToolType.Hand
                    ? $"{ToolManager.ToolLabel(tool)} ∞"
                    : $"{ToolManager.ToolLabel(tool)} {Mathf.CeilToInt(toolManager.GetToolDurability(tool))}/{Mathf.CeilToInt(toolManager.GetToolMaxDurability(tool))}";
                if (tool != lastTool || value != lastToolText)
                {
                    lastTool = tool;
                    lastToolText = value;
                    toolValueText.text = value;
                }
            }
        }

        private static string FormatTime(float seconds)
        {
            int total = Mathf.FloorToInt(seconds);
            return $"{total / 60:00}:{total % 60:00}";
        }
    }
}

