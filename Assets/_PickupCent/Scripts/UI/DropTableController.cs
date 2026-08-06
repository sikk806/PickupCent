using System.Linq;
using PickupCent.Common;
using PickupCent.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace PickupCent.UI
{
    /// <summary>
    /// 사이드패널의 "이 지역 드랍표" — ItemSpawner가 실제로 쓰는 스폰 가중치를 그대로 백분율로
    /// 환산해서 보여준다(새 확률 값을 만들지 않는다). 웹 프로토타입(sand_finder_prototype)의
    /// #dropPanel/renderDropTabs/renderDropPanel 구조를 정확한 참고 기준으로 삼았다 —
    /// "아이템"(값이 있는 것)/"쓰레기"(값이 0 이하인 것) 두 탭, 가중치 내림차순 정렬, 확률 수치는
    /// 하늘색(희귀 아이템만 골드 — 우리 로스터엔 희귀 구분이 없어 전부 하늘색), 쓰레기가 아예 없는
    /// 지역(스테이지1)에서는 탭 자체를 감춘다(프로토타입의 renderDropTabs: !hasTrash → 탭 숨김).
    /// </summary>
    public class DropTableController : MonoBehaviour
    {
        private enum Category
        {
            Item,
            Trash
        }

        [SerializeField] private ItemSpawner itemSpawner;

        private Category selectedCategory = Category.Item;
        private Transform listContainer;
        private GameObject tabsRow;
        private Image itemTabImage;
        private Text itemTabLabel;
        private Image trashTabImage;
        private Text trashTabLabel;

        private Sprite activeTabSprite;
        private Sprite inactiveTabSprite;

        private void Awake()
        {
            if (itemSpawner == null) itemSpawner = FindFirstObjectByType<ItemSpawner>();

            BuildUI();
            RefreshList();
            UICanvasUtility.RefreshSidePanelLayout();
        }

        private void BuildUI()
        {
            var content = UICanvasUtility.CreateBlockCard(UICanvasUtility.EnsureSidePanel(), "이 지역 드랍표");

            activeTabSprite = ProceduralSprites.CreateGradientButtonSliced(32, 9f,
                PickupCentPalette.GoldBright, PickupCentPalette.Gold, 0f, PickupCentPalette.Gold);
            inactiveTabSprite = ProceduralSprites.CreateRoundedRectSliced(32, 9f, PickupCentPalette.SecondaryButtonBg);

            CreateTabs(content);

            var listGO = new GameObject("List", typeof(RectTransform));
            listGO.transform.SetParent(content, false);
            var vlg = listGO.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            listGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            listContainer = listGO.transform;
        }

        private void CreateTabs(Transform content)
        {
            tabsRow = new GameObject("Tabs", typeof(RectTransform));
            tabsRow.transform.SetParent(content, false);
            tabsRow.AddComponent<LayoutElement>().preferredHeight = 28f;

            var hlg = tabsRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 5f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            CreateTabButton(tabsRow.transform, "아이템", out itemTabImage, out itemTabLabel, () => SelectCategory(Category.Item));
            CreateTabButton(tabsRow.transform, "쓰레기", out trashTabImage, out trashTabLabel, () => SelectCategory(Category.Trash));

            UpdateTabVisuals();
        }

        private void CreateTabButton(Transform parent, string label, out Image image, out Text text, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject($"Tab_{label}", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            image = go.AddComponent<Image>();
            image.type = Image.Type.Sliced;

            var button = go.AddComponent<Button>();
            button.onClick.AddListener(onClick);

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(go.transform, false);
            var labelRt = labelGO.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            text = labelGO.AddComponent<Text>();
            text.font = PickupCentFonts.Default;
            text.text = label;
            text.fontStyle = FontStyle.Bold;
            text.fontSize = 12;
            text.alignment = TextAnchor.MiddleCenter;
        }

        private void SelectCategory(Category category)
        {
            if (selectedCategory == category) return;
            selectedCategory = category;
            UpdateTabVisuals();
            RefreshList();
        }

        /// <summary>웹 프로토타입의 .drop-tab-btn / .drop-tab-btn.active 스타일 — 활성 탭만 골드
        /// 그라디언트+짙은 텍스트, 비활성 탭은 어두운 반투명 배경+옅은 회색 텍스트로 확실히 구분한다.</summary>
        private void UpdateTabVisuals()
        {
            bool itemActive = selectedCategory == Category.Item;
            if (itemTabImage != null) itemTabImage.sprite = itemActive ? activeTabSprite : inactiveTabSprite;
            if (itemTabLabel != null) itemTabLabel.color = itemActive ? PickupCentPalette.WoodDark : new Color(1f, 1f, 1f, 0.6f);

            bool trashActive = selectedCategory == Category.Trash;
            if (trashTabImage != null) trashTabImage.sprite = trashActive ? activeTabSprite : inactiveTabSprite;
            if (trashTabLabel != null) trashTabLabel.color = trashActive ? PickupCentPalette.WoodDark : new Color(1f, 1f, 1f, 0.6f);
        }

        private void RefreshList()
        {
            if (listContainer == null) return;
            foreach (Transform child in listContainer) UICanvasUtility.DestroyObjectSafe(child.gameObject);

            if (itemSpawner == null || itemSpawner.ItemPool == null) return;

            var pool = itemSpawner.ItemPool;

            // 쓰레기 항목이 하나도 없는 지역(스테이지1)에서는 탭 자체를 숨긴다 — 웹 프로토타입의
            // renderDropTabs()가 !hasTrash일 때 탭을 아예 안 그리는 것과 동일한 동작.
            bool hasTrash = pool.Any(d => d != null && d.value <= 0);
            if (tabsRow != null) tabsRow.SetActive(hasTrash);
            if (!hasTrash) selectedCategory = Category.Item;

            float total = 0f;
            foreach (var def in pool)
                if (def != null) total += Mathf.Max(0f, def.spawnWeight);
            if (total <= 0f) return;

            bool wantTrash = selectedCategory == Category.Trash;

            // 웹 프로토타입과 동일하게 가중치(등장 확률) 내림차순으로 정렬해서 보여준다.
            var list = pool
                .Where(def => def != null && (def.value <= 0) == wantTrash)
                .OrderByDescending(def => def.spawnWeight)
                .ToList();

            foreach (var def in list)
            {
                float percent = Mathf.Max(0f, def.spawnWeight) / total * 100f;
                CreateRow(def, percent);
            }

            if (list.Count == 0) CreateEmptyRow();
        }

        private void CreateRow(ItemDefinition def, float percent)
        {
            var rowGO = new GameObject($"Row_{def.itemName}", typeof(RectTransform));
            rowGO.transform.SetParent(listContainer, false);
            rowGO.AddComponent<LayoutElement>().preferredHeight = 30f;

            var bg = rowGO.AddComponent<Image>();
            bg.sprite = ProceduralSprites.CreateRoundedRectSliced(32, 8f, PickupCentPalette.ListItemBg);
            bg.type = Image.Type.Sliced;

            var hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(6, 8, 4, 4);
            hlg.spacing = 8f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            // 웹 프로토타입의 .drop-icon(28x28, 둥근 사각형, 옅은 반투명 배경) — 실제 아이템 그림이
            // 없어 배경은 중립색으로 두고, 그 위에 아이템별 색이 다른 원을 대신 넣는다.
            var iconSlotGO = new GameObject("IconSlot", typeof(RectTransform));
            iconSlotGO.transform.SetParent(rowGO.transform, false);
            iconSlotGO.AddComponent<LayoutElement>().preferredWidth = 24f;
            var iconBgImage = iconSlotGO.AddComponent<Image>();
            iconBgImage.sprite = ProceduralSprites.CreateRoundedRectSliced(24, 6f, new Color(1f, 1f, 1f, 0.06f));
            iconBgImage.type = Image.Type.Sliced;

            var iconGO = new GameObject("Icon", typeof(RectTransform));
            iconGO.transform.SetParent(iconSlotGO.transform, false);
            var iconRt = iconGO.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(14f, 14f);
            var iconImage = iconGO.AddComponent<Image>();
            iconImage.sprite = CreateItemIconSprite(def);
            iconImage.color = Color.white;

            var nameGO = new GameObject("Name", typeof(RectTransform));
            nameGO.transform.SetParent(rowGO.transform, false);
            var nameText = nameGO.AddComponent<Text>();
            nameText.font = PickupCentFonts.Default;
            nameText.text = def.itemName;
            nameText.color = PickupCentPalette.Cream;
            nameText.fontSize = 12;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameGO.AddComponent<LayoutElement>().flexibleWidth = 1;

            var pctGO = new GameObject("Percent", typeof(RectTransform));
            pctGO.transform.SetParent(rowGO.transform, false);
            var pctText = pctGO.AddComponent<Text>();
            pctText.font = PickupCentFonts.Title;
            pctText.fontStyle = FontStyle.Bold;
            pctText.text = $"{percent:0.0}%";
            // 웹 프로토타입의 .drop-chance 기본색(#bfe3ff, 하늘색) — .rare만 골드지만 우리 로스터엔
            // 희귀 구분이 없어 전부 이 색을 쓴다.
            pctText.color = PickupCentPalette.AccentBlue;
            pctText.fontSize = 12;
            pctText.alignment = TextAnchor.MiddleRight;
            pctGO.AddComponent<LayoutElement>().preferredWidth = 50f;
        }

        private void CreateEmptyRow()
        {
            var go = new GameObject("Empty", typeof(RectTransform));
            go.transform.SetParent(listContainer, false);
            go.AddComponent<LayoutElement>().preferredHeight = 28f;

            var text = go.AddComponent<Text>();
            text.font = PickupCentFonts.Default;
            text.text = "아직 없음";
            text.color = new Color(1f, 1f, 1f, 0.5f);
            text.fontSize = 13;
            text.alignment = TextAnchor.MiddleCenter;
        }

        private static Sprite CreateItemIconSprite(ItemDefinition def)
        {
            if (def == null) return null;
            if (def.artSprite != null) return def.artSprite;

            return def.shape == ItemDefinition.ItemShape.Circle
                ? ProceduralSprites.CreateCircle(28, def.displayColor, 1f)
                : ProceduralSprites.CreateSquare(28, def.displayColor, 1f);
        }
    }
}
