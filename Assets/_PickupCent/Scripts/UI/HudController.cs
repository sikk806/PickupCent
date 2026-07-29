using PickupCent.Common;
using PickupCent.Digging;
using PickupCent.Economy;
using PickupCent.Events;
using UnityEngine;
using UnityEngine.UI;

namespace PickupCent.UI
{
    /// <summary>
    /// 스타일 가이드 3장: 맵 위에는 최소한의 정보(지역명/보유 금액/타이머/장착 도구)만 알약(pill) 배지로
    /// 화면 상단 중앙에 띄운다. 예전엔 점수만 보여줬지만(HudController) 이제 이 4가지를 전부 여기서
    /// 만든다 — 예전에 타이머 전용이었던 SwarmEventCountdownText는 이 안으로 흡수됐다.
    /// 데이터 자체(점수/타이머/도구)는 그대로 ScoreTracker/ChildrenSwarmEvent/ToolManager에서 읽어올 뿐,
    /// 새 게임 로직은 없다. "도구 내구도"에 대응하는 실제 데이터가 없어서, 그 자리는 현재 장착 도구
    /// 이름으로 대체했다(README/코드 어디에도 내구도 수치는 없음).
    /// </summary>
    public class HudController : MonoBehaviour
    {
        [SerializeField] private ScoreTracker scoreTracker;
        [SerializeField] private ChildrenSwarmEvent swarmEvent;
        [SerializeField] private ToolManager toolManager;

        [Tooltip("맵 위 지역명 배지에 표시할 고정 라벨(README 기준 스테이지1 = 놀이터)")]
        [SerializeField] private string regionName = "놀이터";

        private Text scoreValueText;
        private Text timerValueText;
        private Text toolValueText;

        /// <summary>
        /// 알약 텍스트에 쓰는 폰트. 아직 커스텀 폰트 에셋이 없어 지금은 Unity 내장 기본 폰트를 쓴다.
        /// 나중에 커스텀 폰트가 준비되면 LoadDefaultFont() 안의 리소스 이름만 바꾸면 된다.
        /// </summary>
        private Font defaultFont;

        private int lastScore = int.MinValue;
        private string lastTimerText;
        private ToolManager.ToolType lastTool = (ToolManager.ToolType)(-1);

        private void Awake()
        {
            if (scoreTracker == null) scoreTracker = FindFirstObjectByType<ScoreTracker>();
            if (swarmEvent == null) swarmEvent = FindFirstObjectByType<ChildrenSwarmEvent>();
            if (toolManager == null) toolManager = FindFirstObjectByType<ToolManager>();

            defaultFont = LoadDefaultFont();

            CleanUpLegacyElements();
            BuildPills();
        }

        /// <summary>지금은 커스텀 폰트 에셋이 없어 Unity 내장 레거시 폰트(Arial 계열)를 대신 쓴다.</summary>
        private static Font LoadDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return font;
        }

        /// <summary>이전 스타일(Test5/Test6)에서 만들어졌던 독립 ScoreText/SwarmCountdownText는
        /// 이제 이 컴포넌트가 대신하므로, 중복 표시되지 않도록 남아있으면 제거한다.</summary>
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

            CreatePill(row, "지역", regionName, PickupCentPalette.Cream, out _);
            CreatePill(row, "금액", "0", PickupCentPalette.GoldBright, out scoreValueText);
            CreatePill(row, "다음 이벤트", "-", PickupCentPalette.GoldBright, out timerValueText);
            CreatePill(row, "도구", "손", PickupCentPalette.GoldBright, out toolValueText);
        }

        /// <summary>알약(pill) 배지 하나를 만든다 — 옅은 흰 테두리 + 짙은 갈색 반투명 배경 위에
        /// "라벨(Cream, 얇게) + 값(강조색, 굵게)"을 가로로 배치한다.</summary>
        private void CreatePill(Transform parent, string label, string initialValue, Color valueColor, out Text valueText)
        {
            const int pillHeight = 40;
            const int pillWidth = 176;

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
            labelText.fontSize = 15;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = PickupCentPalette.Cream;
            labelGO.AddComponent<LayoutElement>().flexibleWidth = 1;

            var valueGO = new GameObject("Value", typeof(RectTransform));
            valueGO.transform.SetParent(contentGO.transform, false);
            valueText = valueGO.AddComponent<Text>();
            valueText.font = defaultFont;
            valueText.text = initialValue;
            valueText.fontSize = 17;
            valueText.fontStyle = FontStyle.Bold;
            valueText.alignment = TextAnchor.MiddleRight;
            valueText.color = valueColor;
            valueGO.AddComponent<LayoutElement>().preferredWidth = 56;
        }

        private void Update()
        {
            if (scoreTracker != null && scoreValueText != null && scoreTracker.Score != lastScore)
            {
                lastScore = scoreTracker.Score;
                scoreValueText.text = lastScore.ToString();
            }

            if (swarmEvent != null && timerValueText != null)
            {
                string text = swarmEvent.IsEventRunning ? "진행중" : $"{swarmEvent.SecondsUntilNextEvent:F0}초";
                if (text != lastTimerText)
                {
                    lastTimerText = text;
                    timerValueText.text = text;
                }
            }

            if (toolManager != null && toolValueText != null && toolManager.CurrentTool != lastTool)
            {
                lastTool = toolManager.CurrentTool;
                toolValueText.text = lastTool switch
                {
                    ToolManager.ToolType.Hand => "손",
                    ToolManager.ToolType.Shovel => "삽",
                    ToolManager.ToolType.Detector => "탐지기",
                    _ => lastTool.ToString()
                };
            }
        }
    }
}
