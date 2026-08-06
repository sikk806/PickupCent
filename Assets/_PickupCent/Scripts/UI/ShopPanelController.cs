using System;
using System.Collections.Generic;
using PickupCent.Common;
using PickupCent.Digging;
using PickupCent.Economy;
using PickupCent.Events;
using PickupCent.Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace PickupCent.UI
{
    /// <summary>
    /// HTML ?꾨줈?좏??낆쓽 shopScreen 援ъ“??留욎텣 ?곸젏. ?꾧뎄/媛뺥솕/?먮룞???곗씠?곕? ?고???UI濡??쒖떆?쒕떎.
    /// </summary>
    public class ShopPanelController : MonoBehaviour
    {
        private enum ShopTab { Tools, Passives }

        [SerializeField] private ScoreTracker scoreTracker;
        [SerializeField] private UpgradeManager upgradeManager;
        [SerializeField] private ToolManager toolManager;
        [SerializeField] private ItemSpawner itemSpawner;
        [SerializeField] private ChildrenSwarmEvent swarmEvent;

        private readonly List<Action<int>> refreshers = new List<Action<int>>();
        private GameObject overlayRoot;
        private Transform shopTabs;
        private Transform listContent;
        private ScrollRect shopScroll;
        private Image toolsTabImage;
        private Image passivesTabImage;
        private ShopTab activeTab = ShopTab.Tools;
        private bool isOpen;

        private int incomeLevel;
        private int rareFindLevel;
        private int durabilityLevel;
        private const int MaxPassiveLevel = 5;

        public event Action<bool> OnPanelToggled;

        private void Awake()
        {
            if (!Application.isPlaying) return;

            if (scoreTracker == null) scoreTracker = FindFirstObjectByType<ScoreTracker>();
            if (upgradeManager == null) upgradeManager = FindFirstObjectByType<UpgradeManager>();
            if (toolManager == null) toolManager = FindFirstObjectByType<ToolManager>();
            if (itemSpawner == null) itemSpawner = FindFirstObjectByType<ItemSpawner>();
            if (swarmEvent == null) swarmEvent = FindFirstObjectByType<ChildrenSwarmEvent>();
            BuildUI();
        }

        private void OnDestroy()
        {
            if (isOpen) PopupPauseManager.PopPause();
        }

        private void BuildUI()
        {
            var sidePanel = UICanvasUtility.EnsureSidePanel();
            CreateToggleButton(sidePanel);
            UICanvasUtility.RefreshSidePanelLayout(sidePanel);
            CreateOverlay();
            BuildActiveContent();
        }

        private void CreateOverlay()
        {
            var stageRoot = UICanvasUtility.EnsureStageRoot();
            var existing = stageRoot.Find("ShopOverlay");
            if (existing != null)
            {
                overlayRoot = existing.gameObject;
                WireExistingOverlay();
                return;
            }

            overlayRoot = new GameObject("ShopOverlay", typeof(RectTransform));
            overlayRoot.transform.SetParent(stageRoot, false);
            Stretch((RectTransform)overlayRoot.transform);
            var backdrop = overlayRoot.AddComponent<Image>();
            backdrop.raycastTarget = true;
            ConfigureOverlayBackdrop(backdrop);
            overlayRoot.AddComponent<CanvasGroup>().blocksRaycasts = true;

            var panel = new GameObject("ShopModalPanel", typeof(RectTransform));
            panel.transform.SetParent(overlayRoot.transform, false);
            var panelRt = (RectTransform)panel.transform;
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(560f, 454f);

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateCloseButton(panel.transform);
            CreateTitle(panel.transform);
            CreateSubtitle(panel.transform);
            CreateTabs(panel.transform);
            CreateList(panel.transform);
            var close = panel.transform.Find("CloseButton");
            if (close != null) close.SetAsLastSibling();
            overlayRoot.SetActive(false);
        }

        private void CreateCloseButton(Transform parent)
        {
            var go = new GameObject("CloseButton", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-2f, -2f);
            rt.sizeDelta = new Vector2(30f, 30f);
            go.AddComponent<LayoutElement>().ignoreLayout = true;
            CreateFlatButton(go.transform, "X", 16, PickupCentPalette.SecondaryButtonBg, PickupCentPalette.Cream, ClosePanel);
        }

        private void CreateTitle(Transform parent)
        {
            var go = new GameObject("ModalTitle", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = 28f;
            var text = go.AddComponent<Text>();
            text.font = PickupCentFonts.Title;
            text.text = "상점";
            text.color = PickupCentPalette.GoldBright;
            text.fontSize = 22;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
        }

        private void CreateSubtitle(Transform parent)
        {
            var go = new GameObject("ModalSub", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = 18f;
            var text = go.AddComponent<Text>();
            text.font = PickupCentFonts.Default;
            text.text = "번 돈으로 도구와 패시브를 업그레이드합니다.";
            text.color = PickupCentPalette.WithAlpha(Color.white, 0.55f);
            text.fontSize = 12;
            text.alignment = TextAnchor.MiddleCenter;
        }

        private void CreateTabs(Transform parent)
        {
            var row = new GameObject("ShopTabs", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            row.AddComponent<LayoutElement>().preferredHeight = 40f;
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            shopTabs = row.transform;

            CreateTab("도구", ShopTab.Tools, out toolsTabImage);
            CreateTab("패시브", ShopTab.Passives, out passivesTabImage);
        }

        private void CreateTab(string label, ShopTab tab, out Image image)
        {
            var go = new GameObject($"Tab_{label}", typeof(RectTransform));
            go.transform.SetParent(shopTabs, false);
            go.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var button = CreateFlatButton(go.transform, label, 13, PickupCentPalette.SecondaryButtonBg, PickupCentPalette.Cream, () => SelectTab(tab));
            image = button.GetComponent<Image>();
        }

        private void CreateList(Transform parent)
        {
            var scrollGO = new GameObject("ShopList", typeof(RectTransform));
            scrollGO.transform.SetParent(parent, false);
            var scrollLayout = scrollGO.AddComponent<LayoutElement>();
            scrollLayout.preferredHeight = 330f;
            scrollLayout.flexibleHeight = 1f;

            shopScroll = scrollGO.AddComponent<ScrollRect>();
            shopScroll.horizontal = false;
            shopScroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(scrollGO.transform, false);
            var viewportRt = (RectTransform)viewport.transform;
            Stretch(viewportRt);
            viewportRt.offsetMax = new Vector2(-4f, 0f);
            viewport.AddComponent<RectMask2D>();

            shopScroll.viewport = viewportRt;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            listContent = content.transform;
            var contentRt = (RectTransform)content.transform;
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 7f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            shopScroll.content = contentRt;
        }

        private void WireExistingOverlay()
        {
            shopTabs = FindTransform("ShopModalPanel/ShopTabs");
            ConfigureOverlayBackdrop(overlayRoot.GetComponent<Image>());
            toolsTabImage = FindImage("ShopModalPanel/ShopTabs/Tab_도구");
            passivesTabImage = FindImage("ShopModalPanel/ShopTabs/Tab_패시브");
            var autoTab = FindTransform("ShopModalPanel/ShopTabs/Tab_자동화");
            if (autoTab != null) UICanvasUtility.DestroyObjectSafe(autoTab.gameObject);
            var scrollTransform = FindTransform("ShopModalPanel/ShopList");
            shopScroll = scrollTransform != null ? scrollTransform.GetComponent<ScrollRect>() : null;
            listContent = FindTransform("ShopModalPanel/ShopList/Viewport/Content");
            WireButton("ShopModalPanel/CloseButton", ClosePanel);
            WireButton("ShopModalPanel/ShopTabs/Tab_도구", () => SelectTab(ShopTab.Tools));
            WireButton("ShopModalPanel/ShopTabs/Tab_패시브", () => SelectTab(ShopTab.Passives));
        }

        private static void ConfigureOverlayBackdrop(Image image)
        {
            if (image == null) return;
            image.color = PickupCentPalette.WoodDark;
        }

        private Transform FindTransform(string path)
        {
            return overlayRoot != null ? overlayRoot.transform.Find(path) : null;
        }

        private Image FindImage(string path)
        {
            var target = FindTransform(path);
            return target != null ? target.GetComponent<Image>() : null;
        }

        private void WireButton(string path, UnityEngine.Events.UnityAction action)
        {
            var target = FindTransform(path);
            var button = target != null ? target.GetComponent<Button>() : null;
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private void SelectTab(ShopTab tab)
        {
            if (activeTab == tab && listContent.childCount > 0) return;
            activeTab = tab;
            BuildActiveContent();
            RefreshRows();
        }

        private void BuildActiveContent()
        {
            if (listContent == null) return;
            for (int i = listContent.childCount - 1; i >= 0; i--)
                UICanvasUtility.DestroyObjectSafe(listContent.GetChild(i).gameObject);
            refreshers.Clear();
            UpdateTabVisuals();

            if (activeTab == ShopTab.Tools) BuildToolRows();
            else BuildPassiveRows();

            ForceListLayout();
            LogShopListDiagnostics();
        }

        private void BuildToolRows()
        {
            CreateSectionTitle("TOOLS");
            CreateToolRow(ToolManager.ToolType.Hand, ToolManager.ToolLabel(ToolManager.ToolType.Hand), "무한 내구도. 파기 범위가 좁습니다.");
            CreateToolRow(ToolManager.ToolType.Shovel, ToolManager.ToolLabel(ToolManager.ToolType.Shovel), "표준 도구. 손보다 훨씬 넓게 팝니다.");
            CreateToolRow(ToolManager.ToolType.Rake, ToolManager.ToolLabel(ToolManager.ToolType.Rake), "가로로 넓은 타원형으로 긁습니다.");
            CreateToolRow(ToolManager.ToolType.Detector, ToolManager.ToolLabel(ToolManager.ToolType.Detector), "근처 동전을 별 표시로 찾아냅니다.");
        }

        private void BuildPassiveRows()
        {
            CreateSectionTitle("UPGRADES");
            CreateUpgradeDefinitionRow(upgradeManager?.DigStrengthDef, "손과 도구의 파기 강도가 증가합니다.");
            CreateUpgradeDefinitionRow(upgradeManager?.DigRangeDef, "손과 도구의 파기 범위가 넓어집니다.");
            CreateUpgradeDefinitionRow(upgradeManager?.ShovelStabilityDef, "삽으로 습득할 때 아이템이 파손될 확률이 낮아집니다.");
            CreateUpgradeDefinitionRow(upgradeManager?.DetectRangeDef, "금속탐지기의 즉시 발견 반경이 넓어집니다.");

            CreateSectionTitle("PASSIVES");
            CreatePassiveRow("수익 증가", "습득 금액이 영구적으로 증가합니다.", 90, () => incomeLevel, () => { scoreTracker?.AddIncomeMultiplier(0.15f); incomeLevel++; });
            CreatePassiveRow("발견 확률 증가", "가치 있는 발견물 확률이 증가합니다.", 110, () => rareFindLevel, () => { itemSpawner?.AddRareFindWeightBonus(0.15f); rareFindLevel++; });
            CreatePassiveRow("내구도 개선", "구매한 도구들의 최대 내구도가 증가합니다.", 100, () => durabilityLevel, () => { toolManager?.AddDurabilityCapacityBonus(20f); durabilityLevel++; });
            CreateChildrenEventRow();
        }

        private void CreateSectionTitle(string title)
        {
            var go = new GameObject($"Section_{title}", typeof(RectTransform));
            go.transform.SetParent(listContent, false);
            go.AddComponent<LayoutElement>().preferredHeight = 22f;
            var text = go.AddComponent<Text>();
            text.font = PickupCentFonts.Title;
            text.text = title;
            text.color = PickupCentPalette.WithAlpha(Color.white, 0.45f);
            text.fontSize = 12;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleLeft;
        }

        private void CreateToolRow(ToolManager.ToolType tool, string name, string desc)
        {
            var row = CreateListRow($"Tool_{tool}");
            CreateLeft(row.transform, IconForTool(tool), name, desc, out var nameText, out var descText);
            var right = CreateRight(row.transform);
            var badge = CreateBadge(right, "장착중");
            var bar = CreateMiniBar(right);
            var repair = CreateSmallButton(right, "수리", 62f, () => { toolManager?.TryRepairTool(tool, scoreTracker); RefreshRows(); });
            var action = CreateSmallButton(right, "장착", 80f, () =>
            {
                if (toolManager == null) return;
                if (!toolManager.IsToolOwned(tool))
                {
                    if (toolManager.TryPurchaseTool(tool, scoreTracker))
                        toolManager.TryEquipTool(tool);
                }
                else toolManager.TryEquipTool(tool);
                RefreshRows();
            });

            refreshers.Add(score =>
            {
                if (toolManager == null) return;
                bool owned = toolManager.IsToolOwned(tool);
                bool equipped = toolManager.IsToolEquipped(tool);
                float dur = toolManager.GetToolDurability(tool);
                float max = toolManager.GetToolMaxDurability(tool);
                badge.SetActive(equipped);
                bar.SetActive(tool != ToolManager.ToolType.Hand && owned);
                SetMiniBar(bar, max <= 0f ? 0f : dur / max);
                repair.gameObject.SetActive(tool != ToolManager.ToolType.Hand && owned);
                repair.interactable = toolManager.CanRepairTool(tool) && score >= toolManager.GetToolRepairCost(tool);
                action.interactable = !equipped && (owned || score >= toolManager.GetToolPurchaseCost(tool) || tool == ToolManager.ToolType.Hand);
                SetButtonText(repair, $"수리 · {toolManager.GetToolRepairCost(tool)}");
                SetButtonText(action, equipped ? "장착중" : owned ? "장착" : $"구매 · {toolManager.GetToolPurchaseCost(tool)}");
                nameText.color = equipped ? PickupCentPalette.GoldBright : PickupCentPalette.Cream;
                descText.text = tool == ToolManager.ToolType.Hand ? desc : owned ? $"{desc} · 내구도 {Mathf.CeilToInt(dur)}/{Mathf.CeilToInt(max)}" : desc;
            });
        }

        private void CreateUpgradeDefinitionRow(UpgradeDefinition definition, string desc)
        {
            if (definition == null) return;

            var row = CreateListRow($"Upgrade_{definition.type}");
            CreateLeft(row.transform, "강", definition.upgradeName, desc, out _, out _);
            var right = CreateRight(row.transform);
            var levelText = CreateInlineText(right, "Lv.0/5", 12, PickupCentPalette.GoldBright, 52f);
            var button = CreateSmallButton(right, "강화", 100f, () =>
            {
                upgradeManager?.TryPurchase(definition);
                RefreshRows();
            });

            refreshers.Add(score =>
            {
                int level = upgradeManager != null ? upgradeManager.GetLevel(definition) : 0;
                bool maxed = level >= definition.maxLevel;
                int cost = maxed ? 0 : definition.GetCostForLevel(level);
                levelText.text = $"Lv.{level}/{definition.maxLevel}";
                SetButtonText(button, maxed ? "MAX" : score >= cost ? $"강화 · {cost}" : $"부족 · {cost}");
                button.interactable = upgradeManager != null && !maxed && score >= cost;
            });
        }

        private void CreatePassiveRow(string name, string desc, int baseCost, Func<int> getLevel, Action apply)
        {
            var row = CreateListRow($"Passive_{name}");
            CreateLeft(row.transform, "강", name, desc, out _, out _);
            var right = CreateRight(row.transform);
            var levelText = CreateInlineText(right, "Lv.0/5", 12, PickupCentPalette.GoldBright, 52f);
            var button = CreateSmallButton(right, "업그레이드", 100f, () =>
            {
                int level = getLevel();
                if (level >= MaxPassiveLevel || scoreTracker == null) return;
                int cost = Cost(baseCost, level);
                if (scoreTracker.Score < cost) return;
                scoreTracker.Spend(cost, name);
                apply();
                RefreshRows();
            });

            refreshers.Add(score =>
            {
                int level = getLevel();
                bool maxed = level >= MaxPassiveLevel;
                int cost = maxed ? 0 : Cost(baseCost, level);
                levelText.text = $"Lv.{level}/{MaxPassiveLevel}";
                SetButtonText(button, maxed ? "MAX" : score >= cost ? $"구매 · {cost}" : $"부족 · {cost}");
                button.interactable = !maxed && score >= cost;
            });
        }

        private void CreateChildrenEventRow()
        {
            if (swarmEvent == null) return;

            var row = CreateListRow("Passive_ChildrenEvent");
            CreateLeft(row.transform, "이", "아이들 등장 이벤트", "구매 후 일정 주기로 지나가며 새 발견물을 흩뿌립니다.", out var title, out var desc);
            var right = CreateRight(row.transform);
            var status = CreateInlineText(right, "미구매", 12, PickupCentPalette.GoldBright, 62f);
            var button = CreateSmallButton(right, "구매", 90f, () =>
            {
                swarmEvent?.TryPurchase(scoreTracker);
                RefreshRows();
            });

            refreshers.Add(score =>
            {
                if (swarmEvent == null) return;
                bool purchased = swarmEvent.IsPurchased;
                status.text = purchased ? (swarmEvent.IsEventRunning ? "진행중" : $"{swarmEvent.SecondsUntilNextEvent:F0}초") : "미구매";
                SetButtonText(button, purchased ? "적용중" : score >= swarmEvent.PurchaseCost ? $"구매 · {swarmEvent.PurchaseCost}" : $"부족 · {swarmEvent.PurchaseCost}");
                button.interactable = !purchased && score >= swarmEvent.PurchaseCost;
                title.color = purchased ? PickupCentPalette.GoldBright : PickupCentPalette.Cream;
                desc.text = purchased ? "아이들이 주기적으로 지나가며 새 발견물을 흩뿌립니다." : "구매 후 일정 주기로 지나가며 새 발견물을 흩뿌립니다.";
            });
        }

        private GameObject CreateListRow(string name)
        {
            var row = new GameObject(name, typeof(RectTransform));
            row.transform.SetParent(listContent, false);
            row.AddComponent<LayoutElement>().preferredHeight = 67f;
            var bg = row.AddComponent<Image>();
            bg.sprite = ProceduralSprites.CreateRoundedRectSliced(48, 11f, PickupCentPalette.ListItemBg);
            bg.type = Image.Type.Sliced;
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(12, 12, 9, 9);
            hlg.spacing = 8f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            return row;
        }

        private Transform CreateLeft(Transform parent, string icon, string name, string desc, out Text nameText, out Text descText)
        {
            var left = new GameObject("RowLeft", typeof(RectTransform));
            left.transform.SetParent(parent, false);
            left.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var hlg = left.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 9f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            var iconGO = new GameObject("Icon", typeof(RectTransform));
            iconGO.transform.SetParent(left.transform, false);
            iconGO.AddComponent<LayoutElement>().preferredWidth = 30f;
            var iconBg = iconGO.AddComponent<Image>();
            iconBg.sprite = ProceduralSprites.CreateRoundedRectSliced(32, 8f, PickupCentPalette.WithAlpha(Color.black, 0.25f));
            iconBg.type = Image.Type.Sliced;
            var iconTextGO = new GameObject("IconText", typeof(RectTransform));
            iconTextGO.transform.SetParent(iconGO.transform, false);
            Stretch((RectTransform)iconTextGO.transform);
            var iconText = iconTextGO.AddComponent<Text>();
            iconText.font = PickupCentFonts.Default;
            iconText.text = icon;
            iconText.fontSize = 16;
            iconText.alignment = TextAnchor.MiddleCenter;
            iconText.color = PickupCentPalette.Cream;

            var textCol = new GameObject("RowText", typeof(RectTransform));
            textCol.transform.SetParent(left.transform, false);
            textCol.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var vlg = textCol.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 1f;
            vlg.childAlignment = TextAnchor.MiddleLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            nameText = CreateInlineText(textCol.transform, name, 13, PickupCentPalette.Cream, 22f);
            nameText.fontStyle = FontStyle.Bold;
            descText = CreateInlineText(textCol.transform, desc, 11, PickupCentPalette.WithAlpha(Color.white, 0.55f), 20f);
            return left.transform;
        }

        private Transform CreateRight(Transform parent)
        {
            var right = new GameObject("RowRight", typeof(RectTransform));
            right.transform.SetParent(parent, false);
            right.AddComponent<LayoutElement>().preferredWidth = 216f;
            var hlg = right.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6f;
            hlg.childAlignment = TextAnchor.MiddleRight;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            return right.transform;
        }

        private Text CreateInlineText(Transform parent, string value, int size, Color color, float width)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredWidth = width;
            var text = go.AddComponent<Text>();
            text.font = PickupCentFonts.Default;
            text.text = value;
            text.color = color;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            return text;
        }

        private GameObject CreateBadge(Transform parent, string value)
        {
            var go = new GameObject("Badge", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredWidth = 54f;
            var bg = go.AddComponent<Image>();
            bg.sprite = ProceduralSprites.CreateRoundedRectSliced(32, 8f, PickupCentPalette.WithAlpha(PickupCentPalette.HighlightBorder, 0.25f));
            bg.type = Image.Type.Sliced;
            var textGO = new GameObject("Label", typeof(RectTransform));
            textGO.transform.SetParent(go.transform, false);
            Stretch((RectTransform)textGO.transform);
            var text = textGO.AddComponent<Text>();
            text.font = PickupCentFonts.Default;
            text.text = value;
            text.color = Color.white;
            text.fontSize = 10;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            return go;
        }

        private GameObject CreateMiniBar(Transform parent)
        {
            var track = new GameObject("MiniBar", typeof(RectTransform));
            track.transform.SetParent(parent, false);
            track.AddComponent<LayoutElement>().preferredWidth = 40f;
            var bg = track.AddComponent<Image>();
            bg.sprite = ProceduralSprites.CreateRoundedRectSliced(16, 4f, PickupCentPalette.SecondaryButtonBg);
            bg.type = Image.Type.Sliced;
            var fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(track.transform, false);
            var fillRt = (RectTransform)fill.transform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            var img = fill.AddComponent<Image>();
            img.sprite = ProceduralSprites.CreateRoundedRectSliced(16, 4f, PickupCentPalette.GoldBright);
            img.type = Image.Type.Sliced;
            return track;
        }

        private static void SetMiniBar(GameObject bar, float ratio)
        {
            var fill = bar.transform.Find("Fill") as RectTransform;
            if (fill != null) fill.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
        }

        private Button CreateSmallButton(Transform parent, string label, float width, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject($"Button_{label}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredWidth = width;
            return CreateFlatButton(go.transform, label, 12, PickupCentPalette.Gold, PickupCentPalette.Ink, onClick);
        }

        private Button CreateFlatButton(Transform parent, string label, int fontSize, Color bgColor, Color textColor, UnityEngine.Events.UnityAction onClick)
        {
            var image = parent.gameObject.AddComponent<Image>();
            image.sprite = ProceduralSprites.CreateRoundedRectSliced(48, 10f, bgColor);
            image.type = Image.Type.Sliced;
            image.raycastTarget = true;

            var button = parent.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(parent, false);
            Stretch((RectTransform)labelGO.transform);
            var text = labelGO.AddComponent<Text>();
            text.font = PickupCentFonts.Title;
            text.text = label;
            text.color = textColor;
            text.fontStyle = FontStyle.Bold;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return button;
        }

        private void CreateToggleButton(Transform sidePanel)
        {
            var existing = sidePanel.Find("ShopToggleButton");
            if (existing != null)
            {
                ConfigureToggleLayout(existing.gameObject);
                UICanvasUtility.ClearChildrenSafe(existing);
                CreateToggleVisual(existing);
                return;
            }

            var go = new GameObject("ShopToggleButton", typeof(RectTransform));
            go.transform.SetParent(sidePanel, false);
            ConfigureToggleLayout(go);
            CreateToggleVisual(go.transform);
        }

        private void CreateToggleVisual(Transform parent)
        {
            var normalSprite = ProceduralSprites.CreateGradientButtonSliced(48, 12f,
                PickupCentPalette.Gold, PickupCentPalette.WoodLight, 3f, PickupCentPalette.ButtonBottomBorder);
            var pressedSprite = ProceduralSprites.CreateGradientButtonSliced(48, 12f,
                PickupCentPalette.Gold, PickupCentPalette.WoodLight, 1f, PickupCentPalette.ButtonBottomBorder);

            var visual = UICanvasUtility.CreatePressableSurface(parent, normalSprite, pressedSprite, out var button, out _);
            button.onClick.AddListener(TogglePanel);

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(visual.transform, false);
            Stretch((RectTransform)labelGO.transform);
            var label = labelGO.AddComponent<Text>();
            label.font = PickupCentFonts.Title;
            label.text = "상점";
            label.color = PickupCentPalette.Ink;
            label.fontStyle = FontStyle.Bold;
            label.fontSize = 17;
            label.alignment = TextAnchor.MiddleCenter;
        }

        private static void ConfigureToggleLayout(GameObject go)
        {
            var layout = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            layout.preferredHeight = 40f;
            layout.flexibleWidth = 1f;
            if (go.transform is RectTransform rt) rt.sizeDelta = new Vector2(rt.sizeDelta.x, 40f);
        }

        private void TogglePanel()
        {
            if (isOpen) ClosePanel();
            else OpenPanel();
        }

        private void OpenPanel()
        {
            if (overlayRoot == null || isOpen) return;
            isOpen = true;
            overlayRoot.SetActive(true);
            overlayRoot.transform.SetAsLastSibling();
            activeTab = ShopTab.Tools;
            BuildActiveContent();
            PopupPauseManager.PushPause();
            RefreshRows();
            OnPanelToggled?.Invoke(true);
        }

        private void ClosePanel()
        {
            if (overlayRoot == null || !isOpen) return;
            isOpen = false;
            overlayRoot.SetActive(false);
            PopupPauseManager.PopPause();
            OnPanelToggled?.Invoke(false);
        }

        private void Update()
        {
            if (!isOpen) return;
            RefreshRows();
        }

        private void RefreshRows()
        {
            int score = scoreTracker != null ? scoreTracker.Score : 0;
            foreach (var refresher in refreshers)
                refresher.Invoke(score);
        }

        private void ForceListLayout()
        {
            if (listContent == null) return;

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)listContent);
            if (shopScroll == null) return;

            Canvas.ForceUpdateCanvases();
            shopScroll.verticalNormalizedPosition = 1f;
        }

#if UNITY_EDITOR
        private void LogShopListDiagnostics()
        {
            if (listContent == null || overlayRoot == null) return;
            int sourceCount = activeTab == ShopTab.Tools ? 4 : CountPassiveSources();
            var contentRt = (RectTransform)listContent;
            Debug.Log($"[ShopList] tab={activeTab}, source={sourceCount}, contentChildren={listContent.childCount}, contentSize={contentRt.rect.size}, overlayActive={overlayRoot.activeInHierarchy}, sibling={overlayRoot.transform.GetSiblingIndex()}");
            for (int i = 0; i < listContent.childCount; i++)
            {
                var child = listContent.GetChild(i);
                var rt = child as RectTransform;
                var group = child.GetComponentInParent<CanvasGroup>();
                Debug.Log($"[ShopList] row[{i}] name={child.name}, activeSelf={child.gameObject.activeSelf}, activeHierarchy={child.gameObject.activeInHierarchy}, size={(rt != null ? rt.rect.size : Vector2.zero)}, pos={(rt != null ? rt.anchoredPosition : Vector2.zero)}, scale={child.localScale}, alpha={(group != null ? group.alpha : 1f)}");
            }
        }

        private int CountPassiveSources()
        {
            int count = 3;
            if (upgradeManager?.DigStrengthDef != null) count++;
            if (upgradeManager?.DigRangeDef != null) count++;
            if (upgradeManager?.ShovelStabilityDef != null) count++;
            if (upgradeManager?.DetectRangeDef != null) count++;
            if (swarmEvent != null) count++;
            return count;
        }
#else
        private void LogShopListDiagnostics() { }
#endif
        private void UpdateTabVisuals()
        {
            SetTab(toolsTabImage, activeTab == ShopTab.Tools);
            SetTab(passivesTabImage, activeTab == ShopTab.Passives);
        }

        private static void SetTab(Image image, bool active)
        {
            if (image == null) return;
            image.sprite = ProceduralSprites.CreateRoundedRectSliced(48, 10f, active ? PickupCentPalette.GoldBright : PickupCentPalette.SecondaryButtonBg);
            image.color = Color.white;
        }

        private static void SetButtonText(Button button, string text)
        {
            var label = button.GetComponentInChildren<Text>();
            if (label != null) label.text = text;
        }

        private static int Cost(int baseCost, int level) => Mathf.RoundToInt(baseCost * Mathf.Pow(1.55f, level));

        private static string IconForTool(ToolManager.ToolType tool) => tool switch
        {
            ToolManager.ToolType.Hand => "손",
            ToolManager.ToolType.Shovel => "삽",
            ToolManager.ToolType.Rake => "갈",
            ToolManager.ToolType.Detector => "탐",
            _ => "?"
        };

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
