using PickupCent.Common;
using PickupCent.Digging;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PickupCent.EditorTools
{
    /// <summary>
    /// [테스트용 메뉴 - 실제 게임 스테이지 번호와 무관, Test1/Test2/Test3... 순서로 계속 늘어남]
    /// 메뉴 한 번으로 파기 시스템 테스트 씬을 구성한다.
    /// (SandLayer + DummyItem 생성/재사용, 카메라를 2D 세팅으로 정리)
    /// </summary>
    public static class Test1SceneSetup
    {
        [MenuItem("PickupCent/Test1. 파기 테스트 씬 구성")]
        public static void Setup()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[Test1SceneSetup] Main Camera가 씬에 없습니다. 먼저 Main Camera를 추가하세요.");
                return;
            }

            cam.orthographic = true;
            if (cam.orthographicSize <= 0f) cam.orthographicSize = 5f;
            var camPos = cam.transform.position;
            cam.transform.position = new Vector3(camPos.x, camPos.y, -10f);

            // --- SandLayer ---
            GameObject sandGO = GameObject.Find("SandLayer");
            bool sandIsNew = sandGO == null;
            if (sandIsNew)
            {
                sandGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
                sandGO.name = "SandLayer";
            }

            var collider = sandGO.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);

            sandGO.transform.position = new Vector3(0f, 0f, 0f);
            sandGO.transform.rotation = Quaternion.identity;
            sandGO.transform.localScale = new Vector3(8f, 8f, 1f);

            var mask = sandGO.GetComponent<SandMaskController>();
            if (mask == null) mask = sandGO.AddComponent<SandMaskController>();

            var input = sandGO.GetComponent<SandDigInput>();
            if (input == null) input = sandGO.AddComponent<SandDigInput>();

            // --- DummyItem ---
            GameObject itemGO = GameObject.Find("DummyItem");
            if (itemGO == null) itemGO = new GameObject("DummyItem");

            itemGO.transform.position = new Vector3(0f, 0f, 0.5f);

            var sr = itemGO.GetComponent<SpriteRenderer>();
            if (sr == null) sr = itemGO.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSprites.CreateCircle(128, new Color(0.95f, 0.75f, 0.15f), 1.2f);
            sr.color = Color.white;

            var diggable = itemGO.GetComponent<DiggableItem>();
            if (diggable == null) diggable = itemGO.AddComponent<DiggableItem>();

            var so = new SerializedObject(diggable);
            var maskProp = so.FindProperty("sandMask");
            if (maskProp != null) maskProp.objectReferenceValue = mask;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("[Test1SceneSetup] 씬 구성 완료: SandLayer(모래 마스크) + DummyItem(더미 아이템) 준비됨. " +
                      "Play를 눌러 마우스 좌클릭 드래그로 테스트하세요.");
        }
    }
}
