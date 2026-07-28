using System;
using PickupCent.Digging;
using PickupCent.Economy;
using PickupCent.Events;
using PickupCent.Upgrades;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PickupCent.Debugging
{
    /// <summary>
    /// Play 모드 전용 밸런싱 디버그 패널. F1로 열고 닫는다. 새 게임 로직은 없고,
    /// 기존 컴포넌트의 값을 읽고/실시간으로 다시 써 넣는 역할만 한다.
    /// UI 전체를 런타임에 코드로 직접 구성한다 — 에디터에서 미리 만들어두면 델리게이트(Func/Action)가
    /// 직렬화되지 않아 씬을 다시 열 때 끊어지므로, 매번 Play 시작 시 새로 짓고 새로 연결한다.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class DebugPanelController : MonoBehaviour
    {
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;

        [Header("대상 참조 (비워두면 자동으로 찾음)")]
        [SerializeField] private SandMaskController sandMask;
        [SerializeField] private ToolManager toolManager;
        [SerializeField] private ItemSpawner itemSpawner;
        [SerializeField] private ChildrenSwarmEvent swarmEvent;
        [SerializeField] private TerrainFeature[] terrainFeatures;
        [SerializeField] private UpgradeManager upgradeManager;

        private GameObject panelRoot;
        private Font font;

        private void Awake()
        {
            if (sandMask == null) sandMask = FindFirstObjectByType<SandMaskController>();
            if (toolManager == null) toolManager = FindFirstObjectByType<ToolManager>();
            if (itemSpawner == null) itemSpawner = FindFirstObjectByType<ItemSpawner>();
            if (swarmEvent == null) swarmEvent = FindFirstObjectByType<ChildrenSwarmEvent>();
            if (upgradeManager == null) upgradeManager = FindFirstObjectByType<UpgradeManager>();
            if (terrainFeatures == null || terrainFeatures.Length == 0)
                terrainFeatures = FindObjectsByType<TerrainFeature>(FindObjectsSortMode.None);

            font = GetDefaultFont();
            EnsureEventSystem();
            BuildUI();
            panelRoot.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey) && panelRoot != null)
                panelRoot.SetActive(!panelRoot.activeSelf);
        }

        // ------------------------------------------------------------------
        // UI 구성
        // ------------------------------------------------------------------

        private void BuildUI()
        {
            var canvasGO = new GameObject("DebugCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // 다른 UI(상점 등)보다 항상 위에

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            panelRoot = CreateUIObject("DebugPanel", canvasGO.transform);
            var panelImage = panelRoot.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.88f);
            var panelRt = panelRoot.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0f, 0f);
            panelRt.anchorMax = new Vector2(0f, 0f);
            panelRt.pivot = new Vector2(0f, 0f);
            panelRt.anchoredPosition = new Vector2(16, 16);
            panelRt.sizeDelta = new Vector2(360, 10); // 세로는 ContentSizeFitter가 결정

            var panelLayout = panelRoot.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(10, 10, 10, 10);
            panelLayout.spacing = 4;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;
            panelRoot.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var titleGO = CreateUIObject("Title", panelRoot.transform);
            ConfigureText(titleGO.AddComponent<Text>(), "디버그 패널 (F1로 닫기)", 16, TextAnchor.MiddleLeft, Color.white);
            titleGO.AddComponent<LayoutElement>().preferredHeight = 24;

            BuildDigSection(panelRoot.transform);
            BuildToolSection(panelRoot.transform);
            BuildSpawnSection(panelRoot.transform);
            BuildUpgradeSection(panelRoot.transform);
            BuildEventSection(panelRoot.transform);
        }

        private void BuildDigSection(Transform parent)
        {
            var content = CreateSection(parent, "파기 (SandMaskController)");
            if (sandMask == null)
            {
                CreateInfoRow(content, "SandMaskController를 찾지 못했습니다.");
                return;
            }

            CreateRow(content, "강도 (Strength)", () => sandMask.Strength, v => sandMask.Strength = v);
            CreateRow(content, "경도 (Hardness)", () => sandMask.Hardness, v => sandMask.Hardness = v);
            CreateRow(content, "브러시 반경", () => sandMask.BrushRadius, v => sandMask.BrushRadius = v);
            CreateRow(content, "되메워짐 속도(0~255/s)", () => sandMask.RegenPerSecond, v => sandMask.RegenPerSecond = v);
        }

        private void BuildToolSection(Transform parent)
        {
            var content = CreateSection(parent, "도구 (ToolManager)");
            if (toolManager == null)
            {
                CreateInfoRow(content, "ToolManager를 찾지 못했습니다.");
                return;
            }

            CreateRow(content, "손 강도", () => toolManager.HandStrength, v => toolManager.HandStrength = v);
            CreateRow(content, "삽 강도", () => toolManager.ShovelStrength, v => toolManager.ShovelStrength = v);
            CreateRow(content, "삽 파괴 확률(0~1)", () => toolManager.ShovelDestroyChance, v => toolManager.ShovelDestroyChance = v);
            CreateRow(content, "탐지 반경", () => toolManager.DetectorRadius, v => toolManager.DetectorRadius = v);
            CreateRow(content, "탐지 대기시간(초)", () => toolManager.DetectorDwellTime, v => toolManager.DetectorDwellTime = v);
        }

        private void BuildSpawnSection(Transform parent)
        {
            var content = CreateSection(parent, "스폰 (ItemSpawner)");
            if (itemSpawner == null)
            {
                CreateInfoRow(content, "ItemSpawner를 찾지 못했습니다.");
                return;
            }

            CreateRow(content, "기본 Max 개수", () => itemSpawner.ItemCount, v => itemSpawner.ItemCount = Mathf.RoundToInt(v));
            CreateRow(content, "지형지물 편향 확률(0~1)", () => itemSpawner.TerrainBiasChance, v => itemSpawner.TerrainBiasChance = v);

            if (terrainFeatures != null)
            {
                foreach (var feature in terrainFeatures)
                {
                    if (feature == null) continue;
                    var f = feature; // 클로저 캡처용 로컬 복사
                    CreateRow(content, $"{f.FeatureName} 편향 반경", () => f.BiasRadius, v => f.BiasRadius = v);
                }
            }
        }

        private void BuildUpgradeSection(Transform parent)
        {
            var content = CreateSection(parent, "강화 (UpgradeDefinition)");
            if (upgradeManager == null)
            {
                CreateInfoRow(content, "UpgradeManager를 찾지 못했습니다.");
                return;
            }

            AddUpgradeRows(content, "파기 강도 강화", upgradeManager.DigStrengthDef);
            AddUpgradeRows(content, "파기 범위 강화", upgradeManager.DigRangeDef);
            AddUpgradeRows(content, "삽 안정성 강화", upgradeManager.ShovelStabilityDef);
            AddUpgradeRows(content, "탐지 범위 강화", upgradeManager.DetectRangeDef);
        }

        private void AddUpgradeRows(Transform content, string label, UpgradeDefinition def)
        {
            if (def == null)
            {
                CreateInfoRow(content, $"{label}: 정의 없음");
                return;
            }

            CreateRow(content, $"{label} - 레벨당 비용(base)", () => def.baseCost, v => def.baseCost = Mathf.RoundToInt(v));
            CreateRow(content, $"{label} - 비용 배율(×)", () => def.costMultiplier, v => def.costMultiplier = v);
            CreateRow(content, $"{label} - 레벨당 효과치", () => def.effectPerLevel, v => def.effectPerLevel = v);
        }

        private void BuildEventSection(Transform parent)
        {
            var content = CreateSection(parent, "이벤트 (아이 무리)");
            if (swarmEvent == null)
            {
                CreateInfoRow(content, "ChildrenSwarmEvent를 찾지 못했습니다.");
                return;
            }

            CreateRow(content, "등장 주기(초)", () => swarmEvent.IntervalSeconds, v => swarmEvent.IntervalSeconds = v);
            CreateRow(content, "이동 속도", () => swarmEvent.MoveSpeed, v => swarmEvent.MoveSpeed = v);
        }

        // ------------------------------------------------------------------
        // UGUI 생성 헬퍼
        // ------------------------------------------------------------------

        /// <summary>접고 펼 수 있는 섹션을 만들고, 안에 행을 넣을 콘텐츠 Transform을 반환한다.</summary>
        private Transform CreateSection(Transform parent, string title)
        {
            var sectionGO = CreateUIObject(title + "Section", parent);
            var sectionLayout = sectionGO.AddComponent<VerticalLayoutGroup>();
            sectionLayout.childControlWidth = true;
            sectionLayout.childControlHeight = true;
            sectionLayout.childForceExpandWidth = true;
            sectionLayout.childForceExpandHeight = false;
            sectionLayout.spacing = 2;
            sectionGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var headerGO = CreateUIObject("Header", sectionGO.transform);
            headerGO.AddComponent<LayoutElement>().preferredHeight = 26;
            var headerImage = headerGO.AddComponent<Image>();
            headerImage.color = new Color(1f, 1f, 1f, 0.12f);
            var headerButton = headerGO.AddComponent<Button>();
            headerButton.targetGraphic = headerImage;

            var headerTextGO = CreateUIObject("Label", headerGO.transform);
            var headerText = ConfigureText(headerTextGO.AddComponent<Text>(), "▶ " + title, 14, TextAnchor.MiddleLeft, Color.white);
            StretchRect(headerTextGO, 8, 0, 8, 0);

            var contentGO = CreateUIObject("Content", sectionGO.transform);
            var contentLayout = contentGO.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(14, 4, 4, 4);
            contentLayout.spacing = 3;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentGO.SetActive(false); // 기본은 접힌 상태

            headerButton.onClick.AddListener(() =>
            {
                bool expand = !contentGO.activeSelf;
                contentGO.SetActive(expand);
                headerText.text = (expand ? "▼ " : "▶ ") + title;
            });

            return contentGO.transform;
        }

        private void CreateInfoRow(Transform parent, string message)
        {
            var rowGO = CreateUIObject("Info", parent);
            ConfigureText(rowGO.AddComponent<Text>(), message, 12, TextAnchor.MiddleLeft, new Color(1f, 0.6f, 0.6f));
            rowGO.AddComponent<LayoutElement>().preferredHeight = 20;
        }

        /// <summary>라벨 + 숫자 입력 필드 한 줄을 만들고 getter/setter를 연결한다.</summary>
        private void CreateRow(Transform parent, string label, Func<float> getter, Action<float> setter)
        {
            var rowGO = CreateUIObject("Row", parent);
            var hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            rowGO.AddComponent<LayoutElement>().preferredHeight = 24;

            var labelGO = CreateUIObject("Label", rowGO.transform);
            ConfigureText(labelGO.AddComponent<Text>(), label, 12, TextAnchor.MiddleLeft, Color.white);
            labelGO.AddComponent<LayoutElement>().flexibleWidth = 1;

            var fieldGO = CreateUIObject("Field", rowGO.transform);
            fieldGO.AddComponent<LayoutElement>().preferredWidth = 80;
            var fieldImage = fieldGO.AddComponent<Image>();
            fieldImage.color = new Color(1f, 1f, 1f, 0.15f);

            var inputField = fieldGO.AddComponent<InputField>();
            inputField.targetGraphic = fieldImage;
            inputField.contentType = InputField.ContentType.DecimalNumber;

            var textGO = CreateUIObject("Text", fieldGO.transform);
            var text = ConfigureText(textGO.AddComponent<Text>(), string.Empty, 12, TextAnchor.MiddleCenter, Color.white);
            StretchRect(textGO, 6, 2, 6, 2);
            inputField.textComponent = text;

            void RefreshFromSource() => inputField.text = getter().ToString("0.###");
            RefreshFromSource();

            inputField.onEndEdit.AddListener(entered =>
            {
                if (float.TryParse(entered, out float parsed)) setter(parsed);
                RefreshFromSource(); // 클램프 등으로 실제 반영값이 다를 수 있으니 다시 읽어와 표시
            });
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static Font GetDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return font;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private Text ConfigureText(Text text, string content, int fontSize, TextAnchor anchor, Color color)
        {
            text.font = font;
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static void StretchRect(GameObject go, float left, float top, float right, float bottom)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }
    }
}
