using PickupCent.Common;
using PickupCent.Economy;
using PickupCent.Events;
using PickupCent.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PickupCent.EditorTools
{
    /// <summary>
    /// [테스트용 메뉴 - 실제 게임 스테이지 번호와 무관, Test1~Test6... 순서로 계속 늘어남]
    /// 지형지물(미끄럼틀/그네/벤치) 3개 + 아이 무리 스폰 버스트 이벤트를 구성한다.
    /// Test1(파기)·Test2(도구)·Test3(경제)가 먼저 실행돼 있어야 하며, Test5(UI)가 있으면
    /// 카운트다운 텍스트도 함께 배치한다(없어도 이벤트 자체는 동작).
    /// </summary>
    public static class Test6EventSetup
    {
        private const string PrefabFolder = "Assets/_PickupCent/Prefabs";

        [MenuItem("PickupCent/Test6. 아이 무리 이벤트 씬 구성")]
        public static void Setup()
        {
            var sandGO = GameObject.Find("SandLayer");
            if (sandGO == null)
            {
                Debug.LogError("[Test6EventSetup] SandLayer가 없습니다. 먼저 Test1을 실행하세요.");
                return;
            }

            var itemSpawner = Object.FindFirstObjectByType<ItemSpawner>();
            if (itemSpawner == null)
            {
                Debug.LogError("[Test6EventSetup] ItemSpawner가 없습니다. 먼저 Test3을 실행하세요.");
                return;
            }

            var font = GetDefaultFont();

            // --- 지형지물 3종 ---
            var slide = CreateTerrainFeature("Slide", "미끄럼틀", new Vector3(-2.5f, 2.5f, -0.5f),
                new Vector2(1.2f, 1.8f), new Color(0.95f, 0.55f, 0.2f), 1.2f);
            var swing = CreateTerrainFeature("Swing", "그네", new Vector3(2.5f, 2.5f, -0.5f),
                new Vector2(1.0f, 1.6f), new Color(0.3f, 0.75f, 0.35f), 1.2f);
            var bench = CreateTerrainFeature("Bench", "벤치", new Vector3(0f, -2.7f, -0.5f),
                new Vector2(1.8f, 0.6f), new Color(0.55f, 0.4f, 0.25f), 1.2f);

            var spawnerSo = new SerializedObject(itemSpawner);
            var terrainProp = spawnerSo.FindProperty("terrainFeatures");
            terrainProp.arraySize = 3;
            terrainProp.GetArrayElementAtIndex(0).objectReferenceValue = slide;
            terrainProp.GetArrayElementAtIndex(1).objectReferenceValue = swing;
            terrainProp.GetArrayElementAtIndex(2).objectReferenceValue = bench;
            spawnerSo.ApplyModifiedPropertiesWithoutUndo();

            // --- 아이 무리 이벤트 ---
            GameObject swarmGO = GameObject.Find("ChildrenSwarmEvent");
            if (swarmGO == null) swarmGO = new GameObject("ChildrenSwarmEvent");
            var swarmEvent = swarmGO.GetComponent<ChildrenSwarmEvent>();
            if (swarmEvent == null) swarmEvent = swarmGO.AddComponent<ChildrenSwarmEvent>();

            var swarmSo = new SerializedObject(swarmEvent);
            SetRef(swarmSo, "targetCamera", Camera.main);
            SetRef(swarmSo, "itemSpawner", itemSpawner);
            swarmSo.ApplyModifiedPropertiesWithoutUndo();

            // --- 카운트다운 UI (Test5가 실행돼 있을 때만) ---
            var canvasGO = GameObject.Find("UICanvas");
            if (canvasGO != null)
            {
                var textGO = GameObject.Find("SwarmCountdownText");
                if (textGO == null)
                {
                    textGO = new GameObject("SwarmCountdownText", typeof(RectTransform));
                    textGO.transform.SetParent(canvasGO.transform, false);
                }

                var text = textGO.GetComponent<Text>();
                if (text == null) text = textGO.AddComponent<Text>();
                text.font = font;
                text.fontSize = 18;
                text.alignment = TextAnchor.UpperLeft;
                text.color = Color.white;
                text.text = "다음 아이 무리: -";

                var rt = textGO.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(20, -68);
                rt.sizeDelta = new Vector2(280, 30);

                var countdown = textGO.GetComponent<SwarmEventCountdownText>();
                if (countdown == null) countdown = textGO.AddComponent<SwarmEventCountdownText>();
                var countdownSo = new SerializedObject(countdown);
                SetRef(countdownSo, "swarmEvent", swarmEvent);
                SetRef(countdownSo, "text", text);
                countdownSo.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning("[Test6EventSetup] UICanvas가 없어 카운트다운 텍스트는 건너뛰었습니다. " +
                                  "표시하려면 먼저 Test5를 실행한 뒤 이 메뉴를 다시 실행하세요.");
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[Test6EventSetup] 씬 구성 완료: 지형지물 3개 + ChildrenSwarmEvent 배치됨. " +
                      "Play 후 주기(기본 30초)마다 아이 무리가 화면을 가로지르고, 지나가면 스폰 버스트가 발생합니다.");
        }

        private static TerrainFeature CreateTerrainFeature(string goName, string displayName, Vector3 position,
            Vector2 size, Color color, float biasRadius)
        {
            var go = GameObject.Find(goName);
            if (go == null) go = new GameObject(goName);

            go.transform.position = position;

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            // 프리팹으로 이미 연결된 경우(에셋 연결 도구가 스프라이트를 넣어둔 경우) 덮어쓰지 않는다.
            if (sr.sprite == null) sr.sprite = ProceduralSprites.CreateSquare(4, color, 1f);
            sr.color = Color.white;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            var feature = go.GetComponent<TerrainFeature>();
            if (feature == null) feature = go.AddComponent<TerrainFeature>();

            var so = new SerializedObject(feature);
            so.FindProperty("featureName").stringValue = displayName;
            so.FindProperty("biasRadius").floatValue = biasRadius;
            so.ApplyModifiedPropertiesWithoutUndo();

            // 지형지물을 실제 .prefab 에셋으로 연결한다 — 아트 연결 도구가 그 프리팹의
            // SpriteRenderer에 스프라이트를 채워 넣을 수 있게 하기 위함(structure_ 접두사).
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_PickupCent"))
                    AssetDatabase.CreateFolder("Assets", "_PickupCent");
                AssetDatabase.CreateFolder("Assets/_PickupCent", "Prefabs");
            }

            string prefabPath = $"{PrefabFolder}/{goName}.prefab";
            var connected = PrefabUtility.SaveAsPrefabAssetAndConnect(go, prefabPath, InteractionMode.AutomatedAction);
            feature = connected.GetComponent<TerrainFeature>();

            return feature;
        }

        private static void SetRef(SerializedObject so, string propertyName, Object value)
        {
            var prop = so.FindProperty(propertyName);
            if (prop != null) prop.objectReferenceValue = value;
        }

        private static Font GetDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return font;
        }
    }
}
