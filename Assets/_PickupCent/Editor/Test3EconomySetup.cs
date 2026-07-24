using PickupCent.Digging;
using PickupCent.Economy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PickupCent.EditorTools
{
    /// <summary>
    /// [테스트용 메뉴 - 실제 게임 스테이지 번호와 무관, Test1/Test2/Test3... 순서로 계속 늘어남]
    /// 아이템 경제 시스템(ScriptableObject 아이템 정의 5종 + ItemSpawner + ScoreTracker)을 구성한다.
    /// Test1(파기)·Test2(도구) 셋업이 먼저 실행돼 있어야 하며, 그때 쓰던 고정 더미
    /// (DummyItem, DummyItem_Generic)는 스포너로 대체되므로 이 메뉴가 제거한다.
    /// </summary>
    public static class Test3EconomySetup
    {
        private const string ItemFolder = "Assets/_PickupCent/Data/Items";

        [MenuItem("PickupCent/Test3. 아이템 경제 씬 구성")]
        public static void Setup()
        {
            GameObject sandGO = GameObject.Find("SandLayer");
            if (sandGO == null)
            {
                Debug.LogError("[Test3EconomySetup] SandLayer가 없습니다. 먼저 'PickupCent/Test1. 파기 테스트 씬 구성'을 실행하세요.");
                return;
            }

            var mask = sandGO.GetComponent<SandMaskController>();
            var toolManager = sandGO.GetComponent<ToolManager>();
            if (toolManager == null)
            {
                Debug.LogError("[Test3EconomySetup] ToolManager가 없습니다. 먼저 'PickupCent/Test2. 도구 시스템 씬 구성'을 실행하세요.");
                return;
            }

            // 기존 고정 더미 제거 (ItemSpawner가 대체)
            var oldCoin = GameObject.Find("DummyItem");
            if (oldCoin != null) Object.DestroyImmediate(oldCoin);
            var oldGeneric = GameObject.Find("DummyItem_Generic");
            if (oldGeneric != null) Object.DestroyImmediate(oldGeneric);

            var items = CreateDefaultItemDefinitions();

            GameObject scoreGO = GameObject.Find("ScoreTracker");
            if (scoreGO == null) scoreGO = new GameObject("ScoreTracker");
            var scoreTracker = scoreGO.GetComponent<ScoreTracker>();
            if (scoreTracker == null) scoreTracker = scoreGO.AddComponent<ScoreTracker>();

            GameObject spawnerGO = GameObject.Find("ItemSpawner");
            if (spawnerGO == null) spawnerGO = new GameObject("ItemSpawner");
            var spawner = spawnerGO.GetComponent<ItemSpawner>();
            if (spawner == null) spawner = spawnerGO.AddComponent<ItemSpawner>();

            var so = new SerializedObject(spawner);
            var maskProp = so.FindProperty("sandMask");
            if (maskProp != null) maskProp.objectReferenceValue = mask;
            var scoreProp = so.FindProperty("scoreTracker");
            if (scoreProp != null) scoreProp.objectReferenceValue = scoreTracker;

            var poolProp = so.FindProperty("itemPool");
            if (poolProp != null)
            {
                poolProp.arraySize = items.Length;
                for (int i = 0; i < items.Length; i++)
                    poolProp.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("[Test3EconomySetup] 씬 구성 완료: 아이템 정의 5종(브론즈/실버/골드/구슬/딱지) 생성, " +
                      "ScoreTracker + ItemSpawner 배치, 기존 고정 더미 제거. Play하면 자동으로 아이템이 스폰됩니다.");
        }

        private static ItemDefinition[] CreateDefaultItemDefinitions()
        {
            EnsureFolder();

            return new[]
            {
                CreateOrLoad("BronzeCoin", "브론즈 코인", 1, 50f, true,
                    ItemDefinition.ItemShape.Circle, new Color(0.72f, 0.45f, 0.20f), 0.9f),
                CreateOrLoad("SilverCoin", "실버 코인", 5, 25f, true,
                    ItemDefinition.ItemShape.Circle, new Color(0.75f, 0.76f, 0.78f), 0.95f),
                CreateOrLoad("GoldCoin", "골드 코인", 15, 10f, true,
                    ItemDefinition.ItemShape.Circle, new Color(0.95f, 0.80f, 0.20f), 1f),
                CreateOrLoad("Marble", "구슬", 2, 40f, false,
                    ItemDefinition.ItemShape.Circle, new Color(0.30f, 0.55f, 0.85f), 0.7f),
                CreateOrLoad("Ddakji", "딱지", 4, 15f, false,
                    ItemDefinition.ItemShape.Square, new Color(0.80f, 0.25f, 0.25f), 1.1f),
            };
        }

        private static void EnsureFolder()
        {
            if (AssetDatabase.IsValidFolder(ItemFolder)) return;
            if (!AssetDatabase.IsValidFolder("Assets/_PickupCent/Data"))
                AssetDatabase.CreateFolder("Assets/_PickupCent", "Data");
            AssetDatabase.CreateFolder("Assets/_PickupCent/Data", "Items");
        }

        private static ItemDefinition CreateOrLoad(string assetName, string displayName, int value, float weight,
            bool detectable, ItemDefinition.ItemShape shape, Color color, float size)
        {
            string path = $"{ItemFolder}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (existing != null) return existing;

            var def = ScriptableObject.CreateInstance<ItemDefinition>();
            def.itemName = displayName;
            def.value = value;
            def.spawnWeight = weight;
            def.detectableByMetalDetector = detectable;
            def.shape = shape;
            def.displayColor = color;
            def.displaySize = size;

            AssetDatabase.CreateAsset(def, path);
            return def;
        }
    }
}
