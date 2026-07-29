using PickupCent.Digging;
using UnityEngine;
using UnityEngine.UI;

namespace PickupCent.UI
{
    /// <summary>
    /// 화면 우하단 고정 미니맵. 배경 패널(반투명 어두운 색 채우기 + 옅은 흰색 테두리) 위에
    /// 현재 카메라가 보고 있는 뷰포트 범위를 노란 테두리 사각형으로 표시한다.
    /// 아이템/지형지물 아이콘은 그리지 않는다 — 배경 패널 + 뷰포트 표시만 다룬다.
    /// 기존 UICanvas 레이아웃은 건드리지 않고, 그 아래에 새 자식으로 UI를 전부 런타임에 코드로 만든다
    /// (에디터에서 미리 만들어두면 씬 저장/재실행 사이에 참조가 끊길 위험이 있는 부분이 없도록,
    /// 카메라/SandMaskController 참조만 있으면 나머지는 전부 스스로 구성한다).
    /// </summary>
    public class MinimapController : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private SandMaskController sandMask;

        [Header("배치/크기")]
        [SerializeField] private float minimapWidth = 160f;
        [SerializeField] private Vector2 screenMargin = new Vector2(20f, 20f);
        [SerializeField] private float borderThickness = 2f;
        [SerializeField] private float viewportBorderThickness = 1.5f;

        [Header("색상")]
        [SerializeField] private Color backgroundColor = PickupCentPalette.HudPillBg;
        [SerializeField] private Color borderColor = PickupCentPalette.BorderThin;
        [SerializeField] private Color viewportColor = PickupCentPalette.GoldBright;

        private RectTransform panelRect;
        private RectTransform bgRect;
        private RectTransform viewportRect;

        private void Awake()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (sandMask == null) sandMask = FindFirstObjectByType<SandMaskController>();

            BuildUI();
        }

        private void BuildUI()
        {
            var canvasGO = GameObject.Find("UICanvas");
            if (canvasGO == null)
            {
                Debug.LogWarning("[MinimapController] UICanvas를 찾지 못해 미니맵을 만들 수 없습니다.");
                return;
            }

            Vector2 field = sandMask != null ? sandMask.FieldSize : Vector2.one;
            float aspect = field.x > 0f ? field.y / field.x : 1f;
            float minimapHeight = minimapWidth * aspect;

            // 패널(=테두리 역할, 바깥쪽)
            var panelGO = new GameObject("MinimapPanel", typeof(RectTransform));
            panelGO.transform.SetParent(canvasGO.transform, false);
            panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(1f, 0f);
            panelRect.anchoredPosition = new Vector2(-screenMargin.x, screenMargin.y);
            panelRect.sizeDelta = new Vector2(minimapWidth, minimapHeight);
            panelGO.AddComponent<Image>().color = borderColor;

            // 배경(안쪽, 어두운 반투명 채우기) — 테두리보다 borderThickness만큼 인셋
            var bgGO = new GameObject("Background", typeof(RectTransform));
            bgGO.transform.SetParent(panelGO.transform, false);
            bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = new Vector2(borderThickness, borderThickness);
            bgRect.offsetMax = new Vector2(-borderThickness, -borderThickness);
            bgGO.AddComponent<Image>().color = backgroundColor;

            // 뷰포트 표시(노란 테두리만, 안쪽은 투명) — 매 프레임 위치/크기 갱신
            var viewportGO = new GameObject("ViewportRect", typeof(RectTransform));
            viewportGO.transform.SetParent(bgGO.transform, false);
            viewportRect = viewportGO.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.zero;
            viewportRect.pivot = Vector2.zero;
            viewportGO.AddComponent<Image>().color = viewportColor;

            var innerGO = new GameObject("Inner", typeof(RectTransform));
            innerGO.transform.SetParent(viewportGO.transform, false);
            var innerRect = innerGO.GetComponent<RectTransform>();
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(viewportBorderThickness, viewportBorderThickness);
            innerRect.offsetMax = new Vector2(-viewportBorderThickness, -viewportBorderThickness);
            innerGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        }

        private void LateUpdate()
        {
            if (viewportRect == null || bgRect == null || sandMask == null || targetCamera == null) return;

            Vector2 field = sandMask.FieldSize;
            if (field.x <= 0f || field.y <= 0f) return;
            Vector2 fieldMin = (Vector2)sandMask.transform.position - field * 0.5f;

            float visibleHalfHeight = targetCamera.orthographicSize;
            float visibleHalfWidth = visibleHalfHeight * targetCamera.aspect;

            Vector2 camPos = targetCamera.transform.position;
            Vector2 viewMin = camPos - new Vector2(visibleHalfWidth, visibleHalfHeight);
            Vector2 viewSize = new Vector2(visibleHalfWidth * 2f, visibleHalfHeight * 2f);

            // Background의 실제 표시 영역(테두리 인셋을 뺀 크기) 기준으로 비율 환산한다.
            Vector2 panelArea = new Vector2(
                panelRect.sizeDelta.x - borderThickness * 2f,
                panelRect.sizeDelta.y - borderThickness * 2f);

            float normX = (viewMin.x - fieldMin.x) / field.x;
            float normY = (viewMin.y - fieldMin.y) / field.y;
            float normW = viewSize.x / field.x;
            float normH = viewSize.y / field.y;

            viewportRect.anchoredPosition = new Vector2(normX * panelArea.x, normY * panelArea.y);
            viewportRect.sizeDelta = new Vector2(normW * panelArea.x, normH * panelArea.y);
        }
    }
}
