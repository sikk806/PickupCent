using PickupCent.UI;
using UnityEngine;

namespace PickupCent.Digging
{
    /// <summary>
    /// 우클릭 드래그로 카메라를 이동시킨다 — 좌클릭 파기(SandDigInput)와는 별개이며 서로 간섭하지 않는다.
    /// 이동 가능 범위는 SandMaskController.FieldSize를 기준으로 매번 계산해서, 맵 가장자리 밖이
    /// 화면에 보이지 않도록 clamp한다. 확대/축소는 없다 — 순수 상하좌우 이동만 지원한다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraPanController : MonoBehaviour
    {
        [SerializeField] private SandMaskController sandMask;

        private Camera cam;
        private bool dragging;
        private Vector3 lastMouseWorld;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (sandMask == null) sandMask = FindFirstObjectByType<SandMaskController>();
            ClampPosition();
        }

        private void Update()
        {
            if (PopupPauseManager.IsPausedByPopup)
            {
                dragging = false;
                return;
            }

            if (Input.GetMouseButtonDown(1))
            {
                dragging = true;
                lastMouseWorld = GetMouseWorldPos();
            }
            else if (dragging && Input.GetMouseButton(1))
            {
                Vector3 currentWorld = GetMouseWorldPos();
                Vector3 delta = currentWorld - lastMouseWorld;

                // 드래그 방향과 반대로 카메라를 옮겨서, 마우스 아래의 맵 지점이 계속 커서를 따라오게 한다.
                transform.position -= delta;
                ClampPosition();

                // 클램프로 실제 이동량이 달라질 수 있으니, 다음 프레임 델타 계산 기준점을 새로 잡는다.
                lastMouseWorld = GetMouseWorldPos();
            }
            else if (Input.GetMouseButtonUp(1))
            {
                dragging = false;
            }
        }

        private Vector3 GetMouseWorldPos()
        {
            float planeZ = sandMask != null ? sandMask.transform.position.z : 0f;
            Vector3 screen = Input.mousePosition;
            screen.z = Mathf.Abs(transform.position.z - planeZ);
            return cam.ScreenToWorldPoint(screen);
        }

        /// <summary>카메라 위치를 필드 범위(화면에 맵 바깥이 보이지 않는 범위) 안으로 고정한다.</summary>
        private void ClampPosition()
        {
            if (sandMask == null) return;

            Vector2 field = sandMask.FieldSize;
            Vector2 fieldCenter = sandMask.transform.position;

            float visibleHalfHeight = cam.orthographicSize;
            float visibleHalfWidth = visibleHalfHeight * cam.aspect;

            // 맵이 화면보다 작은 축은 이동 여지가 없으므로 그 축은 중앙으로 고정된다.
            float halfRangeX = Mathf.Max(0f, field.x * 0.5f - visibleHalfWidth);
            float halfRangeY = Mathf.Max(0f, field.y * 0.5f - visibleHalfHeight);

            var pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, fieldCenter.x - halfRangeX, fieldCenter.x + halfRangeX);
            pos.y = Mathf.Clamp(pos.y, fieldCenter.y - halfRangeY, fieldCenter.y + halfRangeY);
            transform.position = pos;
        }
    }
}



