using PickupCent.Common;
using PickupCent.Digging;
using PickupCent.Economy;
using PickupCent.Events;
using UnityEngine;
using UnityEngine.UI;

namespace PickupCent.UI
{
    /// <summary>
    /// 맵 위에는 최소한의 정보(지역명·보유 금액/타이머/장착 도구)만 알약(pill) 배지로 화면 상단
    /// 중앙에 띄운다. 참고 목업(플레이 화면 캡처) 1번 이미지에 맞춰, 지역명과 보유 금액을 "동네 놀이터 ·
    /// 999,999원"처럼 하나의 알약에 묶어서 보여준다(이전엔 각각 별도 알약이었다).
    /// 데이터 자체(점수/타이머/도구)는 그대로 ScoreTracker/ChildrenSwarmEvent/ToolManager에서 읽어올 뿐,
    /// 새 게임 로직은 없다. "도구 내구도"에 대응하는 실제 데이터가 없어서, 그 자리는 현재 장착 도구
    /// 이름으로 대체했다(목업엔 내구도 막대가 있지만 실제 수치가 없어 막대는 그리지 않았다).
    /// </summary>
    public class HudController : MonoBehaviour
    {
        [SerializeField] private ScoreTracker scoreTracker;
        [SerializeField] private ChildrenSwarmEvent swarmEvent;
        [SerializeField] private ToolManager toolManager;

        [Tooltip("맵 위 지역명 배지에 표시할 고정 라벨(README 기준 스테이지1 = 놀이터)")]
        [SerializeField] private string regionName = "동네 놀이터";

        private Text regionMoneyText;
        private Text timerText;
        private Text toolText;

        /// <summary>
        /// 알약 텍스트에 쓰는 폰트. 아직 커스텀 폰트 에셋이 없어 지금은 Unity 내장 기본 폰트를 쓴다.
        /// 나중에 커스텀 폰트가 준비되면 LoadDefaultFont() 안의 리소스 이름만 바꾸면 된다.
        /// </summary>
        private Font defaultFont;

        private int lastScore = int.MinValue;
        private string lastTimerText;
        private ToolManager.ToolType lastTool = (ToolManager.ToolType)(-1);

        private readonly string goldHex = ColorUtility.ToHtmlStringRGB(PickupCentPalette.GoldBright);
        private readonly string blueHex = ColorUtility.ToHtmlStringRGB(PickupCentPalette.AccentBlue);

        private void Awake()
        {
            if (scoreTracker == null) scoreTracker = FindFirstObjectByType<ScoreTracker>();
            if (swarmEvent == null) swarmEvent = FindFirstObjectByType<ChildrenSwarmEvent>();
            if (toolManager == null) toolManager = FindFirstObjectByType<ToolManager>();

            defaultFont = LoadDefaultFont();

            CleanUpLegacyElements();
            BuildPills();
            EnsureAuxiliaryUI();
        }

        /// <summary>
        /// DropTableController/ComboDisplayController는 씬에 미리 배치해 둘 방법이 없어서(에디터
        /// 접근 없이 스크립트로만 작업하는 상황) 스스로 만들어져야 하는데, 이전엔 그 생성 코드를
        /// UICanvasUtility.EnsureCanvas()의 "캔버스를 처음 만드는" 분기 안에 넣어뒀었다. 문제는
        /// "UICanvas"가 이미 씬 파일에 저장돼 있어서(예전 Test5UISetup 실행 결과) Play할 때마다
        /// EnsureCanvas()가 그 분기를 타지 않고 곧장 기존 오브젝트를 찾아 반환해 버렸다는 것 —
        /// 그래서 두 컴포넌트가 생성되는 코드 자체가 한 번도 실행되지 않았다. ItemSpawner가
        /// ComboManager를 만드는 방식(이미 씬에 확실히 있는 컴포넌트의 Awake에서, 없으면 직접
        /// AddComponent)과 똑같은 패턴으로 여기서 다시 연결한다 — HudController는 UIManagers
        /// GameObject에 붙어 씬에 이미 존재하므로 Awake가 매번 확실히 실행된다.
        /// </summary>
        private static void EnsureAuxiliaryUI()
        {
            if (FindFirstObjectByType<DropTableController>() == null)
                new GameObject("DropTableController").AddComponent<DropTableController>();

            if (FindFirstObjectByType<ComboDisplayController>() == null)
                new GameObject("ComboDisplayController").AddComponent<ComboDisplayController>();
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

            regionMoneyText = CreatePill(row, "RegionMoney", 236, PickupCentPalette.Gold, FormatRegionMoney(0));
            timerText = CreatePill(row, "Timer", 108, PickupCentPalette.WoodLight, "00:00");
            toolText = CreatePill(row, "Tool", 150, PickupCentPalette.WoodLight, "손");
        }

        private string FormatRegionMoney(int score)
        {
            // 웹 프로토타입 기준: 지역명은 옅은 하늘색(.region-name), 금액만 골드(.num)로 강조한다.
            return $"<color=#{blueHex}>{regionName}</color> · <b><color=#{goldHex}>{score:N0}원</color></b>";
        }

        /// <summary>알약(pill) 배지 하나를 만든다 — 옅은 흰 테두리 + 짙은 갈색 반투명 배경 위에
        /// 작은 강조색 아이콘 원 + 리치 텍스트(굵게/색 강조는 &lt;b&gt;/&lt;color&gt; 태그로 표현) 한 줄을 배치한다.</summary>
        private Text CreatePill(Transform parent, string name, int pillWidth, Color iconColor, string initialText)
        {
            const int pillHeight = 40;

            var pillGO = new GameObject($"Pill_{name}", typeof(RectTransform));
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
            contentRt.offsetMin = new Vector2(12f, 0f);
            contentRt.offsetMax = new Vector2(-14f, 0f);
            var hlg = contentGO.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            // IconSlot은 흐름 항목(HorizontalLayoutGroup의 세로 늘리기 대상)이고, 실제 원(Icon)은
            // 고정 정사각형으로 가운데 앵커링해서 세로로 늘어난 타원이 되지 않게 한다.
            var iconSlotGO = new GameObject("IconSlot", typeof(RectTransform));
            iconSlotGO.transform.SetParent(contentGO.transform, false);
            iconSlotGO.AddComponent<LayoutElement>().preferredWidth = 18f;

            var iconGO = new GameObject("Icon", typeof(RectTransform));
            iconGO.transform.SetParent(iconSlotGO.transform, false);
            var iconRt = iconGO.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(16f, 16f);
            var iconImage = iconGO.AddComponent<Image>();
            iconImage.sprite = ProceduralSprites.CreateCircle(32, iconColor, 1f);

            var valueGO = new GameObject("Value", typeof(RectTransform));
            valueGO.transform.SetParent(contentGO.transform, false);
            var valueText = valueGO.AddComponent<Text>();
            valueText.font = defaultFont;
            valueText.text = initialText;
            valueText.fontSize = 16;
            valueText.alignment = TextAnchor.MiddleLeft;
            valueText.color = PickupCentPalette.Cream;
            valueText.supportRichText = true;
            valueGO.AddComponent<LayoutElement>().flexibleWidth = 1;

            return valueText;
        }

        private void Update()
        {
            if (scoreTracker != null && regionMoneyText != null && scoreTracker.Score != lastScore)
            {
                lastScore = scoreTracker.Score;
                regionMoneyText.text = FormatRegionMoney(lastScore);
            }

            if (swarmEvent != null && timerText != null)
            {
                string text = swarmEvent.IsEventRunning ? "진행중" : FormatTimer(swarmEvent.SecondsUntilNextEvent);
                if (text != lastTimerText)
                {
                    lastTimerText = text;
                    timerText.text = text;
                }
            }

            if (toolManager != null && toolText != null && toolManager.CurrentTool != lastTool)
            {
                lastTool = toolManager.CurrentTool;
                toolText.text = lastTool switch
                {
                    ToolManager.ToolType.Hand => "손",
                    ToolManager.ToolType.Shovel => "삽",
                    ToolManager.ToolType.Detector => "금속탐지기",
                    _ => lastTool.ToString()
                };
            }
        }

        private static string FormatTimer(float seconds)
        {
            int total = Mathf.Max(0, Mathf.CeilToInt(seconds));
            int minutes = total / 60;
            int secs = total % 60;
            return $"{minutes:00}:{secs:00}";
        }
    }
}
