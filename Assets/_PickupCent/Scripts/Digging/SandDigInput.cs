using UnityEngine;

namespace PickupCent.Digging
{
    /// <summary>
    /// 마우스 좌클릭 드래그 입력을 읽어 SandMaskController.Erode()를 호출한다.
    /// 금속탐지기가 장착된 동안에는 파는 동작 자체를 하지 않는다(ToolManager.IsDiggingTool로 판단).
    /// </summary>
    [RequireComponent(typeof(SandMaskController))]
    public class SandDigInput : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private ToolManager toolManager;
        [Tooltip("드래그 중 초당 몇 번 '한 번 쓸기'를 적용할지 (도구별로 나중에 대체될 임시값)")]
        [SerializeField] private float strokesPerSecond = 8f;

        private SandMaskController mask;
        private bool dragging;
        private float strokeTimer;

        private void Awake()
        {
            mask = GetComponent<SandMaskController>();
            if (targetCamera == null) targetCamera = Camera.main;
            if (toolManager == null) toolManager = FindFirstObjectByType<ToolManager>();
        }

        private void Update()
        {
            if (toolManager != null && !toolManager.IsDiggingTool)
            {
                dragging = false;
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                dragging = true;
                strokeTimer = 0f;
                TryErodeAtMouse();
            }
            else if (dragging && Input.GetMouseButton(0))
            {
                strokeTimer += Time.deltaTime;
                float interval = strokesPerSecond > 0f ? 1f / strokesPerSecond : 0f;
                if (strokeTimer >= interval)
                {
                    strokeTimer = 0f;
                    TryErodeAtMouse();
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                dragging = false;
            }
        }

        private void TryErodeAtMouse()
        {
            if (targetCamera == null || mask == null) return;

            Vector3 screen = Input.mousePosition;
            screen.z = Mathf.Abs(targetCamera.transform.position.z - mask.transform.position.z);
            Vector3 world = targetCamera.ScreenToWorldPoint(screen);
            mask.Erode(world);
        }
    }
}
