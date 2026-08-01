using System;
using PickupCent.Common;
using PickupCent.Economy;
using PickupCent.Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace PickupCent.UI
{
    /// <summary>
    /// 상점 여닫기 버튼(사이드패널에 상시 고정) + 강화 4종 목록(화면 중앙 모달 팝업)을 스스로 구성한다.
    /// 참고 목업 3번 이미지처럼, 상점 내용 자체는 사이드패널에 항상 붙어있는 인라인 섹션이 아니라
    /// 어두워진 배경 위에 뜨는 별도의 중앙 카드다 — 토글 버튼만 사이드패널에 남아있고, 눌렀을 때 뜨는
    /// 카드는 ModalLayer(최상위 레이어)에 만든다. 강화 데이터는 UpgradeManager가 이미 들고 있는 4개
    /// UpgradeDefinition을 그대로 가져다 쓴다.
    /// </summary>
    public class ShopPanelController : MonoBehaviour
    {
        [SerializeField] private ScoreTracker scoreTracker;
        [SerializeField] private UpgradeManager upgradeManager;

        private GameObject modalRoot;
        private ShopRowView[] rows;

        /// <summary>패널이 열리거나 닫힐 때마다 발생(true=열림, false=닫힘) — 사운드 등 알림용.</summary>
        public event Action<bool> OnPanelToggled;

        private void Awake()
        {
            if (scoreTracker == null) scoreTracker = FindFirstObjectByType<ScoreTracker>();
            if (upgradeManager == null) upgradeManager = FindFirstObjectByType<UpgradeManager>();

            CleanUpLegacyElements();
            BuildUI();
        }

        /// <summary>Test5UISetup(예전 에디터 메뉴)가 만들어 뒀던 상점 토글 버튼/패널은 이제 이 컴포넌트가
        /// 전부 새로 만든 것으로 완전히 대체됐다 — 화면에 옛 UI가 겹쳐 보이지 않도록 새로 만들기 전에 지운다.
        /// (아래에서 새로 만드는 토글 버튼도 같은 이름 "ShopToggleButton"을 쓰지만, 이 정리가 새로
        /// 만들기 전에 먼저 실행되므로 지금 이 시점엔 옛 것 하나만 존재해 혼동 없이 찾아낼 수 있다.)</summary>
        private void CleanUpLegacyElements()
        {
            var canvasGO = GameObject.Find("UICanvas");
            if (canvasGO == null) return;

            var oldToggle = canvasGO.transform.Find("ShopToggleButton");
            if (oldToggle != null) Destroy(oldToggle.gameObject);

            var oldPanel = canvasGO.transform.Find("ShopPanel");
            if (oldPanel != null) Destroy(oldPanel.gameObject);
        }

        private void BuildUI()
        {
            CreateToggleButton(UICanvasUtility.EnsureSidePanel());
            BuildModal();
        }

        /// <summary>
        /// 목업 3번 이미지 구조: 어두운 반투명 오버레이(바깥 클릭 시 닫힘) 위에 화면 중앙 고정폭 카드.
        /// 카드 우상단엔 X 닫기 버튼, 그 아래 카테고리 탭과 강화 목록이 세로로 쌓인다.
        /// </summary>
        private void BuildModal()
        {
            var modalLayer = UICanvasUtility.EnsureModalLayer();

            modalRoot = new GameObject("ShopModal", typeof(RectTransform));
            modalRoot.transform.SetParent(modalLayer, false);
            var modalRt = modalRoot.GetComponent<RectTransform>();
            modalRt.anchorMin = Vector2.zero;
            modalRt.anchorMax = Vector2.one;
            modalRt.offsetMin = Vector2.zero;
            modalRt.offsetMax = Vector2.zero;

            CreateOverlay(modalRoot.transform);
            var content = CreateCard(modalRoot.transform);

            CreateCategoryTab(content);

            modalRoot.SetActive(false);

            if (upgradeManager == null) return;

            rows = new[]
            {
                CreateRow(content, upgradeManager.DigStrengthDef),
                CreateRow(content, upgradeManager.DigRangeDef),
                CreateRow(content, upgradeManager.ShovelStabilityDef),
                CreateRow(content, upgradeManager.DetectRangeDef),
            };
        }

        /// <summary>카드 밖 어두운 영역 — 클릭하면 상점을 닫는다.</summary>
        private void CreateOverlay(Transform parent)
        {
            var go = new GameObject("Overlay", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var image = go.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.6f);

            var button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(ClosePanel);
        }

        /// <summary>화면 중앙에 고정폭으로 뜨는 카드. 반환값은 탭/목록을 채워 넣을 콘텐츠 컨테이너.</summary>
        private Transform CreateCard(Transform parent)
        {
            var cardGO = new GameObject("Card", typeof(RectTransform));
            cardGO.transform.SetParent(parent, false);
            var cardRt = cardGO.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.anchoredPosition = Vector2.zero;
            cardRt.sizeDelta = new Vector2(320f, 0f);
            cardGO.AddComponent<LayoutElement>().preferredWidth = 320f;

            // 카드 자신 = 테두리색 채움, 그 위 2px 안쪽으로 배경색 패널 — HUD 알약과 동일한 트릭.
            var borderImage = cardGO.AddComponent<Image>();
            borderImage.sprite = ProceduralSprites.CreateRoundedRectSliced(64, 16f, PickupCentPalette.BorderThin);
            borderImage.type = Image.Type.Sliced;

            var bgGO = new GameObject("Background", typeof(RectTransform));
            bgGO.transform.SetParent(cardGO.transform, false);
            var bgRt = bgGO.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = new Vector2(2f, 2f);
            bgRt.offsetMax = new Vector2(-2f, -2f);
            var bgImage = bgGO.AddComponent<Image>();
            bgImage.sprite = ProceduralSprites.CreateRoundedRectSliced(64, 14f, PickupCentPalette.PanelBgSolid);
            bgImage.type = Image.Type.Sliced;
            bgGO.AddComponent<LayoutElement>().ignoreLayout = true;

            var vlg = cardGO.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(18, 18, 16, 18);
            vlg.spacing = 10f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;
            cardGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateTitleRow(cardGO.transform);
            CreateCloseButton(cardGO.transform);

            var contentGO = new GameObject("Content", typeof(RectTransform));
            contentGO.transform.SetParent(cardGO.transform, false);
            var contentVlg = contentGO.AddComponent<VerticalLayoutGroup>();
            contentVlg.spacing = 8f;
            contentVlg.childControlWidth = true;
            contentVlg.childControlHeight = true;
            contentVlg.childForceExpandWidth = true;
            contentVlg.childForceExpandHeight = false;
            contentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return contentGO.transform;
        }

        private void CreateTitleRow(Transform card)
        {
            var go = new GameObject("Title", typeof(RectTransform));
            go.transform.SetParent(card, false);
            go.AddComponent<LayoutElement>().preferredHeight = 26f;

            var label = go.AddComponent<Text>();
            label.font = PickupCentFonts.Title;
            label.fontStyle = FontStyle.Bold;
            label.fontSize = 20;
            label.color = PickupCentPalette.GoldBright;
            label.text = "상점";
            label.alignment = TextAnchor.MiddleLeft;
        }

        /// <summary>카드 우상단에 절대 위치로 겹쳐지는 X 닫기 버튼 — 레이아웃 흐름에서 제외한다.</summary>
        private void CreateCloseButton(Transform card)
        {
            var go = new GameObject("CloseButton", typeof(RectTransform));
            go.transform.SetParent(card, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-10f, -10f);
            rt.sizeDelta = new Vector2(28f, 28f);
            go.AddComponent<LayoutElement>().ignoreLayout = true;

            var image = go.AddComponent<Image>();
            image.sprite = ProceduralSprites.CreateRoundedRectSliced(28, 8f, PickupCentPalette.SecondaryButtonBg);
            image.type = Image.Type.Sliced;

            var button = go.AddComponent<Button>();
            button.onClick.AddListener(ClosePanel);

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(go.transform, false);
            var labelRt = labelGO.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var label = labelGO.AddComponent<Text>();
            label.font = PickupCentFonts.Default;
            label.text = "X";
            label.color = PickupCentPalette.Cream;
            label.fontStyle = FontStyle.Bold;
            label.fontSize = 15;
            label.alignment = TextAnchor.MiddleCenter;
        }

        /// <summary>
        /// 목업 3번 이미지의 카테고리 탭(도구/패시브/자동화) 자리에 해당하는 부분. 실제로는 강화
        /// ScriptableObject 4종이 전부 한 카테고리("강화")이고, 도구 장착/패시브/자동화 같은 별도
        /// 시스템은 존재하지 않는다 — 없는 기능을 만들어내지 않기 위해, 탭 여러 개를 흉내내는 대신
        /// 실제 있는 카테고리 하나만 골드로 강조된 탭 모양으로 보여준다(탭 전환 기능 자체는 없음).
        /// </summary>
        private void CreateCategoryTab(Transform content)
        {
            var go = new GameObject("CategoryTab", typeof(RectTransform));
            go.transform.SetParent(content, false);
            go.AddComponent<LayoutElement>().preferredHeight = 32f;

            var image = go.AddComponent<Image>();
            image.sprite = ProceduralSprites.CreateGradientButtonSliced(40, 10f,
                PickupCentPalette.Gold, PickupCentPalette.WoodLight, 2f, PickupCentPalette.ButtonBottomBorder);
            image.type = Image.Type.Sliced;

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(go.transform, false);
            var labelRt = labelGO.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var label = labelGO.AddComponent<Text>();
            label.font = PickupCentFonts.Default;
            label.text = "강화";
            label.color = PickupCentPalette.Ink;
            label.fontStyle = FontStyle.Bold;
            label.fontSize = 14;
            label.alignment = TextAnchor.MiddleCenter;
        }

        private ShopRowView CreateRow(Transform content, UpgradeDefinition def)
        {
            if (def == null) return null;
            var rowGO = new GameObject($"Row_{def.upgradeName}", typeof(RectTransform));
            var row = rowGO.AddComponent<ShopRowView>();
            row.Setup(content, def, upgradeManager);
            return row;
        }

        /// <summary>사이드패널에 상시 고정된 상점 열기 버튼. 여는 대상은 이제 인라인 섹션이 아니라
        /// ModalLayer 위의 중앙 카드(modalRoot)다.</summary>
        private void CreateToggleButton(Transform sidePanel)
        {
            var normalSprite = ProceduralSprites.CreateGradientButtonSliced(48, 12f,
                PickupCentPalette.Gold, PickupCentPalette.WoodLight, 3f, PickupCentPalette.ButtonBottomBorder);
            var pressedSprite = ProceduralSprites.CreateGradientButtonSliced(48, 12f,
                PickupCentPalette.Gold, PickupCentPalette.WoodLight, 1f, PickupCentPalette.ButtonBottomBorder);

            var go = new GameObject("ShopToggleButton", typeof(RectTransform));
            go.transform.SetParent(sidePanel, false);
            go.AddComponent<LayoutElement>().preferredHeight = 44f;

            var visual = UICanvasUtility.CreatePressableSurface(go.transform, normalSprite, pressedSprite,
                out var button, out _);
            button.onClick.AddListener(TogglePanel);

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(visual.transform, false);
            var labelRt = labelGO.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var label = labelGO.AddComponent<Text>();
            label.font = PickupCentFonts.Title;
            label.text = "상점";
            label.color = PickupCentPalette.Ink;
            label.fontStyle = FontStyle.Bold;
            label.fontSize = 18;
            label.alignment = TextAnchor.MiddleCenter;
        }

        private void TogglePanel()
        {
            if (modalRoot == null) return;
            bool nowOpen = !modalRoot.activeSelf;
            modalRoot.SetActive(nowOpen);
            OnPanelToggled?.Invoke(nowOpen);
        }

        private void ClosePanel()
        {
            if (modalRoot == null) return;
            modalRoot.SetActive(false);
            OnPanelToggled?.Invoke(false);
        }

        private void Update()
        {
            if (modalRoot == null || !modalRoot.activeSelf || scoreTracker == null || rows == null) return;

            foreach (var row in rows)
                row?.Refresh(scoreTracker.Score);
        }
    }
}
