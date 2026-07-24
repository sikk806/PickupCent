using PickupCent.Digging;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PickupCent.EditorTools
{
    /// <summary>
    /// [테스트용 메뉴 - 실제 게임 스테이지 번호와 무관, Test1/Test2/Test3... 순서로 계속 늘어남]
    /// Test1 씬(SandLayer + DummyItem)이 있다는 전제로 도구 시스템(손/삽/금속탐지기, ToolManager)을 추가한다.
    /// 아이템 태그/추가 더미 생성은 Test3(경제 시스템)에서 스포너로 대체되므로 여기서는 다루지 않는다.
    /// </summary>
    public static class Test2ToolSetup
    {
        [MenuItem("PickupCent/Test2. 도구 시스템 씬 구성")]
        public static void Setup()
        {
            GameObject sandGO = GameObject.Find("SandLayer");
            if (sandGO == null)
            {
                Debug.LogError("[Test2ToolSetup] SandLayer가 없습니다. 먼저 'PickupCent/Test1. 파기 테스트 씬 구성'을 실행하세요.");
                return;
            }

            var mask = sandGO.GetComponent<SandMaskController>();
            var toolManager = sandGO.GetComponent<ToolManager>();
            if (toolManager == null) toolManager = sandGO.AddComponent<ToolManager>();

            var toolSo = new SerializedObject(toolManager);
            var maskProp = toolSo.FindProperty("sandMask");
            if (maskProp != null) maskProp.objectReferenceValue = mask;
            toolSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("[Test2ToolSetup] 씬 구성 완료: ToolManager 추가됨. Play 후 1/2/3 키로 도구를 바꿔보세요 " +
                      "(코인/일반 구분이 있는 아이템으로 탐지기까지 테스트하려면 Test3을 실행하세요).");
        }
    }
}
