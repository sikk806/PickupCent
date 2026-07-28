using PickupCent.Debugging;
using PickupCent.Digging;
using PickupCent.Economy;
using PickupCent.Events;
using PickupCent.Upgrades;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PickupCent.EditorTools
{
    /// <summary>
    /// [테스트용 메뉴 - 실제 게임 스테이지 번호와 무관, Test1~Test7... 순서로 계속 늘어남]
    /// 밸런싱 디버그 패널(DebugPanelController)을 씬에 배치한다. 패널 자체는 Play 모드에 들어갈 때마다
    /// 코드로 새로 만들어지므로, 이 메뉴는 참조 연결만 담당하는 얇은 GameObject 하나만 추가한다.
    /// Test1(파기)·Test2(도구)·Test3(경제)·Test4(강화)·Test6(지형지물/이벤트)이 먼저 실행돼 있어야 한다.
    /// </summary>
    public static class Test7DebugPanelSetup
    {
        [MenuItem("PickupCent/Test7. 디버그 패널 씬 구성")]
        public static void Setup()
        {
            var sandGO = GameObject.Find("SandLayer");
            if (sandGO == null)
            {
                Debug.LogError("[Test7DebugPanelSetup] SandLayer가 없습니다. 먼저 Test1을 실행하세요.");
                return;
            }
            var mask = sandGO.GetComponent<SandMaskController>();
            var toolManager = sandGO.GetComponent<ToolManager>();
            if (toolManager == null)
            {
                Debug.LogError("[Test7DebugPanelSetup] ToolManager가 없습니다. 먼저 Test2를 실행하세요.");
                return;
            }

            var itemSpawner = Object.FindFirstObjectByType<ItemSpawner>();
            if (itemSpawner == null)
            {
                Debug.LogError("[Test7DebugPanelSetup] ItemSpawner가 없습니다. 먼저 Test3을 실행하세요.");
                return;
            }

            var upgradeManager = Object.FindFirstObjectByType<UpgradeManager>();
            if (upgradeManager == null)
            {
                Debug.LogError("[Test7DebugPanelSetup] UpgradeManager가 없습니다. 먼저 Test4를 실행하세요.");
                return;
            }

            var swarmEvent = Object.FindFirstObjectByType<ChildrenSwarmEvent>();
            var terrainFeatures = Object.FindObjectsByType<TerrainFeature>(FindObjectsSortMode.None);
            if (swarmEvent == null || terrainFeatures.Length == 0)
            {
                Debug.LogWarning("[Test7DebugPanelSetup] ChildrenSwarmEvent/TerrainFeature가 없습니다(Test6 미실행). " +
                                  "스폰·이벤트 섹션 일부가 비어 보일 수 있습니다. 계속 진행합니다.");
            }

            GameObject debugGO = GameObject.Find("DebugPanel");
            if (debugGO == null) debugGO = new GameObject("DebugPanel");
            var controller = debugGO.GetComponent<DebugPanelController>();
            if (controller == null) controller = debugGO.AddComponent<DebugPanelController>();

            var so = new SerializedObject(controller);
            SetRef(so, "sandMask", mask);
            SetRef(so, "toolManager", toolManager);
            SetRef(so, "itemSpawner", itemSpawner);
            SetRef(so, "upgradeManager", upgradeManager);
            SetRef(so, "swarmEvent", swarmEvent);

            var terrainProp = so.FindProperty("terrainFeatures");
            terrainProp.arraySize = terrainFeatures.Length;
            for (int i = 0; i < terrainFeatures.Length; i++)
                terrainProp.GetArrayElementAtIndex(i).objectReferenceValue = terrainFeatures[i];

            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[Test7DebugPanelSetup] 씬 구성 완료: DebugPanelController 배치됨. " +
                      "Play 후 F1로 디버그 패널을 열고 닫아 보세요.");
        }

        private static void SetRef(SerializedObject so, string propertyName, Object value)
        {
            var prop = so.FindProperty(propertyName);
            if (prop != null) prop.objectReferenceValue = value;
        }
    }
}
