using System.Collections.Generic;
using PickupCent.Common;
using PickupCent.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace PickupCent.UI
{
    /// <summary>
    /// 웹 프로토타입(sand_finder_prototype)의 #spStatus 블록(comboRow/comboBarFill/comboHistoryRow)을
    /// 그대로 참고해 구성한다 — 콤보 표시는 상단 HUD가 아니라 사이드패널의 별도 블록이다.
    /// ComboManager.OnComboChanged를 구독해서 표시만 담당한다 — 콤보 자체의 증가/리셋/배율 계산은
    /// 전부 ComboManager가 한다. 콤보가 10 미만이면 정확한 숫자를, 10 이상이면 10단위로 잘라서
    /// 보여준다(예: 23 → "x20"). 콤보가 0/1이어도 블록은 상시 표시하고 값만 x0으로 낮춘다. 고콤보(기본 50 이상) 유지
    /// 동안엔 화면 테두리가 붉은빛/주황빛으로 펄스처럼 강조된다.
    /// </summary>
    public class ComboDisplayController : MonoBehaviour
    {
        [Tooltip("이 콤보 수치 이상 유지되는 동안 화면 테두리 강조 효과가 켜진다.")]
        [SerializeField] private int fireBorderThreshold = 50;

        [SerializeField] private ComboManager comboManager;

        private GameObject blockRoot;
        private Text comboText;
        private Image comboBarFillImage;
        private Transform historyRow;
        private Image fireBorderImage;
        private bool fireActive;

        private void Awake()
        {
            if (comboManager == null) comboManager = ComboManager.EnsureInstance();

            BuildComboBlock();
            BuildFireBorder();

            comboManager.OnComboChanged += HandleComboChanged;
            HandleComboChanged(comboManager.Combo);
        }

        private void OnDestroy()
        {
            if (comboManager != null) comboManager.OnComboChanged -= HandleComboChanged;
        }

        /// <summary>사이드패널의 콤보 상태 블록 — 제목 없이 콤보 행 + 바 + 최근 5개 히스토리 슬롯.</summary>
        private void BuildComboBlock()
        {
            var sidePanel = UICanvasUtility.EnsureSidePanel();
            var existingBlock = sidePanel.Find("Block_Card");
            Transform content;

            if (existingBlock != null)
            {
                blockRoot = existingBlock.gameObject;
                content = existingBlock.Find("Content");
                if (content == null)
                {
                    var contentGO = new GameObject("Content", typeof(RectTransform));
                    contentGO.transform.SetParent(existingBlock, false);
                    var contentVlg = contentGO.AddComponent<VerticalLayoutGroup>();
                    contentVlg.spacing = 12f;
                    contentVlg.childControlWidth = true;
                    contentVlg.childControlHeight = true;
                    contentVlg.childForceExpandWidth = true;
                    contentVlg.childForceExpandHeight = false;
                    var fitter = contentGO.AddComponent<ContentSizeFitter>();
                    fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    content = contentGO.transform;
                }
                else
                {
                    UICanvasUtility.ClearChildrenSafe(content);
                }
            }
            else
            {
                content = UICanvasUtility.CreateBlockCard(sidePanel, string.Empty);
                blockRoot = content.parent.gameObject;
            }

            blockRoot.SetActive(true);
            CreateComboRow(content);
            CreateComboBar(content);
            CreateHistoryRow(content);
        }

        private void CreateComboRow(Transform content)
        {
            var rowGO = new GameObject("ComboRow", typeof(RectTransform));
            rowGO.transform.SetParent(content, false);
            rowGO.AddComponent<LayoutElement>().preferredHeight = 20f;

            var hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(rowGO.transform, false);
            var label = labelGO.AddComponent<Text>();
            label.font = PickupCentFonts.Default;
            label.text = "콤보";
            label.color = new Color(1f, 1f, 1f, 0.55f);
            label.fontSize = 13;
            label.alignment = TextAnchor.MiddleLeft;
            labelGO.AddComponent<LayoutElement>().flexibleWidth = 1;

            var valueGO = new GameObject("Value", typeof(RectTransform));
            valueGO.transform.SetParent(rowGO.transform, false);
            comboText = valueGO.AddComponent<Text>();
            comboText.font = PickupCentFonts.Title;
            comboText.fontStyle = FontStyle.Bold;
            comboText.fontSize = 15;
            comboText.color = PickupCentPalette.ComboOrange;
            comboText.alignment = TextAnchor.MiddleRight;
            valueGO.AddComponent<LayoutElement>().preferredWidth = 60f;
        }

        /// <summary>콤보 리셋까지 남은 시간 비율(ComboManager.RemainingRatio)을 매 프레임 보여주는 바.</summary>
        private void CreateComboBar(Transform content)
        {
            const int trackHeight = 8;
            const int trackWidth = 220;

            var trackGO = new GameObject("ComboBarTrack", typeof(RectTransform));
            trackGO.transform.SetParent(content, false);
            trackGO.AddComponent<LayoutElement>().preferredHeight = trackHeight;
            var trackImage = trackGO.AddComponent<Image>();
            trackImage.sprite = ProceduralSprites.CreatePill(trackWidth, trackHeight, new Color(1f, 1f, 1f, 0.12f));

            var fillGO = new GameObject("ComboBarFill", typeof(RectTransform));
            fillGO.transform.SetParent(trackGO.transform, false);
            var fillRt = fillGO.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(1f, 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            comboBarFillImage = fillGO.AddComponent<Image>();
            // Type.Filled + Horizontal: 스프라이트를 늘리지 않고 왼쪽부터 fillAmount만큼만 드러낸다
            // (Type.Simple로 RectTransform 폭 자체를 줄이면 그라디언트 텍스처가 옆으로 찌그러진다).
            comboBarFillImage.sprite = ProceduralSprites.CreateHorizontalGradientPill(trackWidth, trackHeight,
                PickupCentPalette.ComboOrange, PickupCentPalette.GoldBright);
            comboBarFillImage.type = Image.Type.Filled;
            comboBarFillImage.fillMethod = Image.FillMethod.Horizontal;
            comboBarFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            comboBarFillImage.fillAmount = 1f;
        }

        /// <summary>최근 습득 아이템 최대 5개를 보여주는 슬롯 줄 — 빈 슬롯은 옅게 표시.</summary>
        private void CreateHistoryRow(Transform content)
        {
            var rowGO = new GameObject("HistoryRow", typeof(RectTransform));
            rowGO.transform.SetParent(content, false);
            rowGO.AddComponent<LayoutElement>().preferredHeight = 32f;

            var hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            historyRow = rowGO.transform;
            RebuildHistorySlots(comboManager != null ? comboManager.History : null);
        }

        private void RebuildHistorySlots(IReadOnlyList<ItemDefinition> history)
        {
            if (historyRow == null) return;
            UICanvasUtility.ClearChildrenSafe(historyRow);

            for (int i = 0; i < 5; i++)
            {
                ItemDefinition def = history != null && i < history.Count ? history[i] : null;

                var slotGO = new GameObject("Slot", typeof(RectTransform));
                slotGO.transform.SetParent(historyRow, false);
                var slotImage = slotGO.AddComponent<Image>();
                slotImage.sprite = ProceduralSprites.CreateRoundedRectSliced(24, 6f, PickupCentPalette.ListItemBg);
                slotImage.type = Image.Type.Sliced;
                var canvasGroup = slotGO.AddComponent<CanvasGroup>();
                canvasGroup.alpha = def != null ? 1f : 0.25f;

                if (def != null)
                {
                    var iconGO = new GameObject("Icon", typeof(RectTransform));
                    iconGO.transform.SetParent(slotGO.transform, false);
                    var iconRt = iconGO.GetComponent<RectTransform>();
                    iconRt.anchorMin = new Vector2(0.5f, 0.5f);
                    iconRt.anchorMax = new Vector2(0.5f, 0.5f);
                    iconRt.sizeDelta = new Vector2(16f, 16f);
                    iconGO.AddComponent<Image>().sprite = ProceduralSprites.CreateCircle(32, def.displayColor, 1f);
                }
            }
        }

        /// <summary>화면 가장자리를 두르는 테두리 — 평소엔 비활성, 고콤보 동안만 켜지고 서서히 깜빡인다.
        /// 웹 프로토타입의 #stage.combo-fire box-shadow 펄스 애니메이션을 화면 프레임 스프라이트로 근사한다.</summary>
        private void BuildFireBorder()
        {
            var canvasGO = UICanvasUtility.EnsureCanvas();
            var existing = canvasGO.transform.Find("ComboFireBorder");
            var go = existing != null ? existing.gameObject : new GameObject("ComboFireBorder", typeof(RectTransform));
            go.transform.SetParent(canvasGO.transform, false);
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            fireBorderImage = go.GetComponent<Image>();
            if (fireBorderImage == null) fireBorderImage = go.AddComponent<Image>();
            fireBorderImage.sprite = ProceduralSprites.CreateFrameRing(64, 16f, PickupCentPalette.FireGlowDark);
            fireBorderImage.type = Image.Type.Sliced;
            fireBorderImage.raycastTarget = false;

            go.SetActive(false);
        }

        private void HandleComboChanged(int combo)
        {
            if (blockRoot != null) blockRoot.SetActive(true);

            int displayValue = combo <= 1 ? 0 : combo < 10 ? combo : (combo / 10) * 10;
            if (comboText != null) comboText.text = "x" + displayValue;
            if (comboManager != null) RebuildHistorySlots(comboManager.History);

            fireActive = combo >= fireBorderThreshold;
            if (fireBorderImage != null) fireBorderImage.gameObject.SetActive(fireActive);
        }

        private void Update()
        {
            if (blockRoot != null && comboBarFillImage != null && comboManager != null)
            {
                comboBarFillImage.fillAmount = comboManager.RemainingRatio;
            }

            if (!fireActive || fireBorderImage == null) return;

            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 6f);
            var color = Color.Lerp(PickupCentPalette.FireGlowDark, PickupCentPalette.FireGlowBright, pulse);
            color.a = Mathf.Lerp(0.4f, 0.85f, pulse);
            fireBorderImage.color = color;
        }
    }
}
