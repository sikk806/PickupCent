using UnityEngine;

namespace PickupCent.Digging
{
    /// <summary>
    /// 키보드 1/2/3으로 손/삽/금속탐지기를 전환한다.
    /// 손·삽은 SandMaskController.Strength를 갈아끼워 기존 강도÷경도 공식에 그대로 태운다.
    /// 금속탐지기는 파지 않고, 범위 안의 아이템 중 ItemDefinition.detectableByMetalDetector가
    /// true인 것만 즉시 발견 처리한다(코인 3종만 true로 설정됨).
    /// </summary>
    [DefaultExecutionOrder(-50)] // SandMaskController(-100) 이후, SandDigInput(기본값 0) 이전에 실행
    public class ToolManager : MonoBehaviour
    {
        public enum ToolType { Hand, Shovel, Detector }

        [Header("도구별 강도 (임시값 — 스테이지2에서는 수치보다 트레이드오프 검증이 목적)")]
        [SerializeField] private float handStrength = 1f;
        [SerializeField] private float shovelStrength = 3f;

        [Header("삽 파괴 리스크 (기획서 3-1: 빠르지만 가끔 터짐)")]
        [SerializeField, Range(0f, 1f)] private float shovelDestroyChance = 0.05f;

        [Header("금속탐지기 (기획서 3장: 파지 않고 즉시 발견, 코인만 탐지)")]
        [SerializeField] private float detectorRadius = 1.5f;

        [SerializeField] private SandMaskController sandMask;
        [SerializeField] private Camera targetCamera;

        private ToolType currentTool = ToolType.Hand;

        public ToolType CurrentTool => currentTool;
        public float ShovelDestroyChance => shovelDestroyChance;
        public float DetectorRadius => detectorRadius;
        public bool IsDiggingTool => currentTool == ToolType.Hand || currentTool == ToolType.Shovel;

        private void Awake()
        {
            if (sandMask == null) sandMask = FindFirstObjectByType<SandMaskController>();
            if (targetCamera == null) targetCamera = Camera.main;
            ApplyToolStrength();
            LogToolChanged();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchTool(ToolType.Hand);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchTool(ToolType.Shovel);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchTool(ToolType.Detector);

            if (currentTool == ToolType.Detector) ScanForDetectableItems();
        }

        private void SwitchTool(ToolType tool)
        {
            if (currentTool == tool) return;
            currentTool = tool;
            ApplyToolStrength();
            LogToolChanged();
        }

        private void ApplyToolStrength()
        {
            if (sandMask == null) return;
            // 탐지기는 파지 않으므로 강도가 쓰이진 않지만, 손/삽으로 되돌아올 때를 대비해 손 강도로 맞춰둔다.
            sandMask.Strength = currentTool == ToolType.Shovel ? shovelStrength : handStrength;
        }

        private void LogToolChanged()
        {
            string label = currentTool switch
            {
                ToolType.Hand => "손",
                ToolType.Shovel => "삽",
                ToolType.Detector => "금속탐지기",
                _ => currentTool.ToString()
            };
            Debug.Log($"[Tool] 현재 도구: {label}");
        }

        private void ScanForDetectableItems()
        {
            if (targetCamera == null) return;

            float planeZ = sandMask != null ? sandMask.transform.position.z : 0f;
            Vector3 screen = Input.mousePosition;
            screen.z = Mathf.Abs(targetCamera.transform.position.z - planeZ);
            Vector2 world = targetCamera.ScreenToWorldPoint(screen);

            var items = FindObjectsByType<DiggableItem>(FindObjectsSortMode.None);
            foreach (var item in items)
                item.TryDetectorScan(world, detectorRadius);
        }
    }
}
