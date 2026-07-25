using PickupCent.Digging;
using PickupCent.Economy;
using PickupCent.UI;
using PickupCent.Upgrades;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PickupCent.EditorTools
{
    /// <summary>
    /// [테스트용 메뉴 - 실제 게임 스테이지 번호와 무관, Test1~Test5... 순서로 계속 늘어남]
    /// 정식 UI(HUD 점수, 도구 버튼, 상점 패널, 습득 피드백)를 Canvas로 구성한다.
    /// Test1(파기)·Test2(도구)·Test3(경제)·Test4(강화) 셋업이 먼저 실행돼 있어야 한다.
    /// 기존 임시 표시(ScoreTracker/UpgradeManager의 OnGUI, 강화 구매 키보드 Q/W/E/R)는
    /// 이미 코드에서 제거되고 이 UI로 대체되었다 — 숫자 1/2/3 도구 전환 단축키만 그대로 유지된다.
    /// </summary>
    public static class Test5UISetup
    {
        [MenuItem("PickupCent/Test5. UI 씬 구성")]
        public static void Setup()
        {
            var sandGO = GameObject.Find("SandLayer");
            if (sandGO == null)
            {
                Debug.LogError("[Test5UISetup] SandLayer가 없습니다. 먼저 Test1을 실행하세요.");
                return;
            }
            var toolManager = sandGO.GetComponent<ToolManager>();
            if (toolManager == null)
            {
                Debug.LogError("[Test5UISetup] ToolManager가 없습니다. 먼저 Test2를 실행하세요.");
                return;
            }

            var scoreTracker = Object.FindFirstObjectByType<ScoreTracker>();
            var itemSpawner = Object.FindFirstObjectByType<ItemSpawner>();
            if (scoreTracker == null || itemSpawner == null)
            {
                Debug.LogError("[Test5UISetup] ScoreTracker/ItemSpawner가 없습니다. 먼저 Test3을 실행하세요.");
                return;
            }

            var upgradeManager = Object.FindFirstObjectByType<UpgradeManager>();
            if (upgradeManager == null)
            {
                Debug.LogError("[Test5UISetup] UpgradeManager가 없습니다. 먼저 Test4를 실행하세요.");
                return;
            }

            if (GameObject.Find("UICanvas") != null)
            {
                Debug.LogWarning("[Test5UISetup] 'UICanvas'가 이미 있습니다. 중복 생성을 피하기 위해 아무 것도 하지 않았습니다. " +
                                  "다시 만들고 싶으면 씬에서 UICanvas(및 EventSystem)를 지우고 다시 실행하세요.");
                return;
            }

            EnsureEventSystem();
            var canvasGO = CreateCanvas();
            var font = GetDefaultFont();

            BuildHud(canvasGO.transform, font, toolManager, scoreTracker, itemSpawner, upgradeManager);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[Test5UISetup] 씬 구성 완료: Canvas(점수/도구바/상점/습득피드백) 생성됨. " +
                      "Play 후 마우스만으로 도구 전환·강화 구매가 가능합니다 (숫자 1/2/3 단축키도 그대로 동작).");
        }

        private static void BuildHud(Transform canvas, Font font, ToolManager toolManager, ScoreTracker scoreTracker,
            ItemSpawner itemSpawner, UpgradeManager upgradeManager)
        {
            // --- 점수 텍스트 (좌상단) ---
            var scoreGO = CreateUIObject("ScoreText", canvas);
            var scoreText = ConfigureText(scoreGO.AddComponent<Text>(), font, "점수: 0", 28, TextAnchor.UpperLeft, Color.white);
            SetRect(scoreGO, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -20), new Vector2(280, 40));

            // --- 도구 버튼 3개 (상단 중앙) ---
            var toolBarGO = CreateUIObject("ToolBar", canvas);
            SetRect(toolBarGO, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(220, 64));

            var handGO = CreateButton("HandButton", toolBarGO.transform, font, "손", new Vector2(64, 64));
            SetRect(handGO, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-78, 0), new Vector2(64, 64));
            var shovelGO = CreateButton("ShovelButton", toolBarGO.transform, font, "삽", new Vector2(64, 64));
            SetRect(shovelGO, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, 0), new Vector2(64, 64));
            var detectorGO = CreateButton("DetectorButton", toolBarGO.transform, font, "탐지기", new Vector2(64, 64));
            SetRect(detectorGO, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(78, 0), new Vector2(64, 64));

            var toolBar = toolBarGO.AddComponent<ToolBarController>();
            var tbSo = new SerializedObject(toolBar);
            tbSo.FindProperty("toolManager").objectReferenceValue = toolManager;
            var entriesProp = tbSo.FindProperty("entries");
            entriesProp.arraySize = 3;
            SetToolEntry(entriesProp.GetArrayElementAtIndex(0), ToolManager.ToolType.Hand, handGO);
            SetToolEntry(entriesProp.GetArrayElementAtIndex(1), ToolManager.ToolType.Shovel, shovelGO);
            SetToolEntry(entriesProp.GetArrayElementAtIndex(2), ToolManager.ToolType.Detector, detectorGO);
            tbSo.ApplyModifiedPropertiesWithoutUndo();

            // --- 상점 토글 버튼 (우상단) ---
            var shopToggleGO = CreateButton("ShopToggleButton", canvas, font, "상점", new Vector2(100, 40));
            SetRect(shopToggleGO, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -20), new Vector2(100, 40));

            // --- 습득 피드백 텍스트 (상단 중앙, 도구바 아래) ---
            var feedbackGO = CreateUIObject("PickupFeedbackText", canvas);
            var feedbackText = ConfigureText(feedbackGO.AddComponent<Text>(), font, string.Empty, 24,
                TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.55f));
            SetRect(feedbackGO, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -100), new Vector2(400, 40));
            feedbackGO.SetActive(false);

            // --- 상점 패널 (화면 중앙, 기본 비활성) ---
            var panelGO = CreateUIObject("ShopPanel", canvas);
            var panelImage = panelGO.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.85f);
            SetRect(panelGO, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440, 300));

            var titleGO = CreateUIObject("Title", panelGO.transform);
            ConfigureText(titleGO.AddComponent<Text>(), font, "상점", 22, TextAnchor.UpperCenter, Color.white);
            TopStretchRect(titleGO, 8, 32);

            var closeGO = CreateButton("CloseButton", panelGO.transform, font, "X", new Vector2(28, 28));
            SetRect(closeGO, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-8, -8), new Vector2(28, 28));

            var contentGO = CreateUIObject("Content", panelGO.transform);
            StretchRect(contentGO, 0, 44, 0, 10);
            var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(12, 12, 8, 8);
            vlg.spacing = 8;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;

            var digStrengthRow = CreateShopRow(contentGO.transform, font);
            var digRangeRow = CreateShopRow(contentGO.transform, font);
            var shovelStabilityRow = CreateShopRow(contentGO.transform, font);
            var detectRangeRow = CreateShopRow(contentGO.transform, font);

            SetupRow(digStrengthRow, upgradeManager.DigStrengthDef, upgradeManager);
            SetupRow(digRangeRow, upgradeManager.DigRangeDef, upgradeManager);
            SetupRow(shovelStabilityRow, upgradeManager.ShovelStabilityDef, upgradeManager);
            SetupRow(detectRangeRow, upgradeManager.DetectRangeDef, upgradeManager);

            // --- 관리 컴포넌트들을 한 곳에 모아서 배치 ---
            var managersGO = CreateUIObject("UIManagers", canvas);

            var hud = managersGO.AddComponent<HudController>();
            var hudSo = new SerializedObject(hud);
            hudSo.FindProperty("scoreTracker").objectReferenceValue = scoreTracker;
            hudSo.FindProperty("scoreText").objectReferenceValue = scoreText;
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            var shopPanel = managersGO.AddComponent<ShopPanelController>();
            var shopSo = new SerializedObject(shopPanel);
            shopSo.FindProperty("scoreTracker").objectReferenceValue = scoreTracker;
            shopSo.FindProperty("upgradeManager").objectReferenceValue = upgradeManager;
            shopSo.FindProperty("panelRoot").objectReferenceValue = panelGO;
            shopSo.FindProperty("toggleButton").objectReferenceValue = shopToggleGO.GetComponent<Button>();
            shopSo.FindProperty("closeButton").objectReferenceValue = closeGO.GetComponent<Button>();
            var rowsProp = shopSo.FindProperty("rows");
            rowsProp.arraySize = 4;
            rowsProp.GetArrayElementAtIndex(0).objectReferenceValue = digStrengthRow;
            rowsProp.GetArrayElementAtIndex(1).objectReferenceValue = digRangeRow;
            rowsProp.GetArrayElementAtIndex(2).objectReferenceValue = shovelStabilityRow;
            rowsProp.GetArrayElementAtIndex(3).objectReferenceValue = detectRangeRow;
            shopSo.ApplyModifiedPropertiesWithoutUndo();

            var feedback = managersGO.AddComponent<PickupFeedbackController>();
            var feedbackSo = new SerializedObject(feedback);
            feedbackSo.FindProperty("itemSpawner").objectReferenceValue = itemSpawner;
            feedbackSo.FindProperty("feedbackText").objectReferenceValue = feedbackText;
            feedbackSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetupRow(ShopRowView row, UpgradeDefinition def, UpgradeManager manager)
        {
            if (row == null) return;
            var so = new SerializedObject(row);
            so.FindProperty("definition").objectReferenceValue = def;
            so.FindProperty("upgradeManager").objectReferenceValue = manager;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetToolEntry(SerializedProperty entryProp, ToolManager.ToolType tool, GameObject buttonGO)
        {
            entryProp.FindPropertyRelative("tool").enumValueIndex = (int)tool;
            entryProp.FindPropertyRelative("button").objectReferenceValue = buttonGO.GetComponent<Button>();
            entryProp.FindPropertyRelative("background").objectReferenceValue = buttonGO.GetComponent<Image>();
        }

        private static ShopRowView CreateShopRow(Transform parent, Font font)
        {
            var rowGO = CreateUIObject("Row", parent);
            var hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            var rowLayout = rowGO.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = 36;

            var nameGO = CreateUIObject("Name", rowGO.transform);
            var nameText = ConfigureText(nameGO.AddComponent<Text>(), font, "-", 16, TextAnchor.MiddleLeft, Color.white);
            nameGO.AddComponent<LayoutElement>().flexibleWidth = 1;

            var levelGO = CreateUIObject("Level", rowGO.transform);
            var levelText = ConfigureText(levelGO.AddComponent<Text>(), font, "Lv.0/0", 14, TextAnchor.MiddleCenter, Color.white);
            levelGO.AddComponent<LayoutElement>().preferredWidth = 70;

            var costGO = CreateUIObject("Cost", rowGO.transform);
            var costText = ConfigureText(costGO.AddComponent<Text>(), font, "0", 14, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.5f));
            costGO.AddComponent<LayoutElement>().preferredWidth = 60;

            var buyGO = CreateButton("BuyButton", rowGO.transform, font, "구매", new Vector2(64, 32));
            buyGO.AddComponent<LayoutElement>().preferredWidth = 64;

            var view = rowGO.AddComponent<ShopRowView>();
            var so = new SerializedObject(view);
            so.FindProperty("nameText").objectReferenceValue = nameText;
            so.FindProperty("levelText").objectReferenceValue = levelText;
            so.FindProperty("costText").objectReferenceValue = costText;
            so.FindProperty("buyButton").objectReferenceValue = buyGO.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        // --- UGUI 생성 헬퍼 ---

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static GameObject CreateCanvas()
        {
            var existing = GameObject.Find("UICanvas");
            if (existing != null) return existing;

            var canvasGO = new GameObject("UICanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            return canvasGO;
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

        private static Text ConfigureText(Text text, Font font, string content, int fontSize, TextAnchor anchor, Color color)
        {
            text.font = font;
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static GameObject CreateButton(string name, Transform parent, Font font, string label, Vector2 size)
        {
            var go = CreateUIObject(name, parent);
            var image = go.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.6f);

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            var labelGO = CreateUIObject("Label", go.transform);
            var labelText = labelGO.AddComponent<Text>();
            ConfigureText(labelText, font, label, 16, TextAnchor.MiddleCenter, Color.black);
            StretchRect(labelGO, 2, 2, 2, 2);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            return go;
        }

        private static void SetRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
        }

        /// <summary>부모에 상하좌우로 꽉 채우되 left/top/right/bottom 만큼 안쪽으로 여백을 둔다.</summary>
        private static void StretchRect(GameObject go, float left, float top, float right, float bottom)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>부모 상단에 가로로 꽉 채우고, top만큼 아래로 내려서 height만큼의 높이를 갖는다.</summary>
        private static void TopStretchRect(GameObject go, float top, float height)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -top);
            rt.sizeDelta = new Vector2(0, height);
        }
    }
}
