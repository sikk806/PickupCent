using PickupCent.Common;
using UnityEngine;
using UnityEngine.UI;

namespace PickupCent.UI
{
    /// <summary>
    /// HTML prototype의 titleScreen에 해당하는 첫 화면. 기존 게임 화면 위에 오버레이로만 올라가며 메인 Scene은 유지한다.
    /// </summary>
    public class MainTitleScreenController : MonoBehaviour
    {
        private static MainTitleScreenController instance;
        private GameObject overlayRoot;
        private bool showing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInScene()
        {
            if (FindFirstObjectByType<MainTitleScreenController>() != null) return;
            new GameObject("MainTitleScreenController").AddComponent<MainTitleScreenController>();
        }

        public static void ShowTitleFromAnywhere()
        {
            if (instance != null) instance.ShowTitle();
        }

        private void Awake()
        {
            instance = this;
            BuildUI();
        }

        private void Start()
        {
            ShowTitle();
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
            if (showing) PopupPauseManager.PopPause();
        }

        private void BuildUI()
        {
            var stageRoot = UICanvasUtility.EnsureStageRoot();
            overlayRoot = new GameObject("TitleScreen", typeof(RectTransform));
            overlayRoot.transform.SetParent(stageRoot, false);
            Stretch((RectTransform)overlayRoot.transform);
            var bg = overlayRoot.AddComponent<Image>();
            bg.color = new Color(58f / 255f, 42f / 255f, 28f / 255f, 0.98f);
            bg.raycastTarget = true;
            overlayRoot.AddComponent<CanvasGroup>().blocksRaycasts = true;

            var panel = new GameObject("Panel", typeof(RectTransform));
            panel.transform.SetParent(overlayRoot.transform, false);
            var panelRt = (RectTransform)panel.transform;
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(560f, 300f);

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            CreateBrand(panel.transform);
            CreateTagline(panel.transform);
            CreateStartButton(panel.transform);
            CreateHints(panel.transform);
            overlayRoot.SetActive(false);
        }

        private void CreateBrand(Transform parent)
        {
            var go = new GameObject("Brand", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredWidth = 520f;
            go.GetComponent<LayoutElement>().preferredHeight = 72f;
            var text = go.AddComponent<Text>();
            text.font = PickupCentFonts.Title;
            text.text = "PickupCent\nSAND FINDER · PROTOTYPE";
            text.color = PickupCentPalette.GoldBright;
            text.fontSize = 38;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.lineSpacing = 0.72f;
        }

        private void CreateTagline(Transform parent)
        {
            var go = new GameObject("Tagline", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredWidth = 540f;
            go.GetComponent<LayoutElement>().preferredHeight = 74f;
            var text = go.AddComponent<Text>();
            text.font = PickupCentFonts.Default;
            text.text = "넓은 모래밭을 직접 긁어 돈과 물건을 찾고, 번 돈으로 도구·패시브·자동화를 사서\n더 좋은 진행을 만드는 캐주얼 디깅 게임입니다.\n기본 도구는 손 — 무한 내구도지만 파기 범위가 좁습니다.";
            text.color = PickupCentPalette.Cream;
            text.fontSize = 14;
            text.alignment = TextAnchor.MiddleCenter;
            text.lineSpacing = 1.18f;
        }

        private void CreateStartButton(Transform parent)
        {
            var go = new GameObject("StartButton", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredWidth = 150f;
            go.GetComponent<LayoutElement>().preferredHeight = 48f;
            var normal = ProceduralSprites.CreateGradientButtonSliced(48, 14f, PickupCentPalette.GoldBright, PickupCentPalette.Gold, 4f, PickupCentPalette.ButtonBottomBorder);
            var pressed = ProceduralSprites.CreateGradientButtonSliced(48, 14f, PickupCentPalette.GoldBright, PickupCentPalette.Gold, 1f, PickupCentPalette.ButtonBottomBorder);
            var visual = UICanvasUtility.CreatePressableSurface(go.transform, normal, pressed, out var button, out _);
            button.onClick.AddListener(HideTitle);
            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(visual.transform, false);
            Stretch((RectTransform)labelGO.transform);
            var text = labelGO.AddComponent<Text>();
            text.font = PickupCentFonts.Title;
            text.text = "파러 가기";
            text.color = PickupCentPalette.WoodDark;
            text.fontSize = 18;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
        }

        private void CreateHints(Transform parent)
        {
            var go = new GameObject("ControlsHint", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredWidth = 540f;
            go.GetComponent<LayoutElement>().preferredHeight = 34f;
            var text = go.AddComponent<Text>();
            text.font = PickupCentFonts.Default;
            text.text = "좌클릭+드래그 모래 파기    우클릭+드래그 둘러보기    ESC 메뉴 / 일시정지";
            text.color = PickupCentPalette.WithAlpha(Color.white, 0.5f);
            text.fontSize = 12;
            text.alignment = TextAnchor.MiddleCenter;
        }

        private void ShowTitle()
        {
            if (overlayRoot == null || showing) return;
            showing = true;
            overlayRoot.SetActive(true);
            overlayRoot.transform.SetAsLastSibling();
            SetGameplayUiVisible(false);
            PopupPauseManager.PushPause();
        }

        private void HideTitle()
        {
            if (overlayRoot == null || !showing) return;
            showing = false;
            overlayRoot.SetActive(false);
            SetGameplayUiVisible(true);
            PopupPauseManager.PopPause();
        }

        private static void SetGameplayUiVisible(bool visible)
        {
            var canvas = UICanvasUtility.EnsureCanvas().transform;
            var stage = canvas.Find("StageRoot");
            var topHud = stage != null ? stage.Find("TopHudRow") : canvas.Find("TopHudRow");
            if (topHud != null) topHud.gameObject.SetActive(visible);
            var side = canvas.Find("SidePanel");
            if (side != null) side.gameObject.SetActive(visible);
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}

