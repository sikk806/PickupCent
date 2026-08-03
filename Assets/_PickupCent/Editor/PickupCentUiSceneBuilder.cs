using System.Reflection;
using PickupCent.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PickupCent.EditorTools
{
    /// <summary>
    /// Runtime 생성형 UI를 현재 Scene Hierarchy에 미리 구워 두는 Editor 도구.
    /// Play Mode에서는 각 컨트롤러가 이름 기반으로 기존 UI를 재사용하므로 중복 생성을 피한다.
    /// </summary>
    public static class PickupCentUiSceneBuilder
    {
        private static bool autoBuildQueued;

        [InitializeOnLoadMethod]
        private static void QueueAutoBuild()
        {
            if (autoBuildQueued) return;
            autoBuildQueued = true;
            EditorApplication.delayCall += () =>
            {
                autoBuildQueued = false;
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (EditorSceneManager.GetActiveScene().path.Contains("_PickupCent/Scenes/Stage1 Main.unity"))
                    BuildEditableSceneUi(false);
            };
        }

        [MenuItem("PickupCent/UI/Build Editable Scene UI")]
        public static void BuildEditableSceneUiFromMenu()
        {
            BuildEditableSceneUi(true);
        }

        private static void BuildEditableSceneUi(bool logResult)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            var canvasGO = UICanvasUtility.EnsureCanvas();
            UICanvasUtility.EnsureStageRoot();
            UICanvasUtility.EnsureSidePanel();
            UICanvasUtility.EnsureTopHudRow();

            InvokeAwake(FindOrCreateController<HudController>("HudController"));
            InvokeAwake(FindOrCreateController<ToolBarController>("ToolBarController"));
            InvokeAwake(FindOrCreateController<ShopPanelController>("ShopPanelController"));
            InvokeAwake(FindOrCreateController<PausePanelController>("PausePanelController"));
            InvokeAwake(FindOrCreateController<MainTitleScreenController>("MainTitleScreenController"));
            InvokeAwake(FindOrCreateController<PickupFeedbackController>("PickupFeedbackController"));

            var stage = canvasGO.transform.Find("StageRoot");
            var side = canvasGO.transform.Find("SidePanel");
            var topHud = stage != null ? stage.Find("TopHudRow") : null;
            if (stage != null) stage.gameObject.SetActive(true);
            if (side != null) side.gameObject.SetActive(true);
            if (topHud != null) topHud.gameObject.SetActive(true);

            SetActive(stage, "TitleScreen", true);
            SetActive(stage, "ShopOverlay", false);
            SetActive(stage, "PauseOverlay", false);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            if (logResult) Debug.Log("[PickupCent UI] Editable Scene UI 생성/갱신 완료. Scene을 저장하면 Play 전에도 Hierarchy에서 UI를 볼 수 있습니다.");
        }

        private static T FindOrCreateController<T>(string objectName) where T : Component
        {
            var existing = Object.FindFirstObjectByType<T>();
            if (existing != null) return existing;

            var go = GameObject.Find(objectName);
            if (go == null) go = new GameObject(objectName);
            return go.GetComponent<T>() ?? go.AddComponent<T>();
        }

        private static void InvokeAwake(Component component)
        {
            if (component == null) return;
            var method = component.GetType().GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(component, null);
        }

        private static void SetActive(Transform parent, string childName, bool active)
        {
            var child = parent != null ? parent.Find(childName) : null;
            if (child != null) child.gameObject.SetActive(active);
        }
    }
}
