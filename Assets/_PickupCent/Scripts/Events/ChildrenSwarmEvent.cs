using PickupCent.Common;
using PickupCent.Economy;
using UnityEngine;

namespace PickupCent.Events
{
    /// <summary>
    /// 일정 주기로 아이 무리를 대표하는 오브젝트 하나가 화면 오른쪽 밖에서 등장해 왼쪽 밖으로
    /// 빠져나갈 때까지 가로로 이동한다. 지나간 영역(가로 밴드 = 오브젝트 높이 두께 x 이동한 전체 구간)을
    /// 기록해서, 다 지나가면 ItemSpawner.TriggerSpawnBurst()를 호출해 스폰 버스트를 일으킨다.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class ChildrenSwarmEvent : MonoBehaviour
    {
        [Header("주기 / 이동 (예시값)")]
        [SerializeField] private float intervalSeconds = 30f;
        [SerializeField] private float moveSpeed = 4f;

        [Header("무리 오브젝트 크기 (밴드 세로 두께 = 이 높이)")]
        [SerializeField] private float bandWidth = 2f;
        [SerializeField] private float bandHeight = 1.5f;

        [Header("표시")]
        [SerializeField] private Color displayColor = new Color(0.9f, 0.55f, 0.7f);
        [Tooltip("모래 레이어(z=0)보다 카메라 쪽(음수)에 둬서 항상 위에 보이게 한다")]
        [SerializeField] private float displayZ = -0.5f;

        [SerializeField] private Camera targetCamera;
        [SerializeField] private ItemSpawner itemSpawner;

        private float timer;
        private bool running;
        private GameObject visual;
        private SpriteRenderer visualRenderer;
        private float minX, maxX;
        private float bandCenterY;

        public bool IsEventRunning => running;
        public float SecondsUntilNextEvent => running ? 0f : Mathf.Max(0f, intervalSeconds - timer);

        private void Awake()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (itemSpawner == null) itemSpawner = FindFirstObjectByType<ItemSpawner>();
            CreateVisual();
        }

        private void CreateVisual()
        {
            visual = new GameObject("ChildrenSwarmVisual");
            visual.transform.SetParent(transform, false);
            visualRenderer = visual.AddComponent<SpriteRenderer>();
            visualRenderer.sprite = ProceduralSprites.CreateSquare(4, displayColor, 1f);
            visualRenderer.color = Color.white;
            visual.SetActive(false);
        }

        private void Update()
        {
            if (!running)
            {
                timer += Time.deltaTime;
                if (timer >= intervalSeconds)
                {
                    timer = 0f;
                    StartEvent();
                }
                return;
            }

            MoveStep();
        }

        private void StartEvent()
        {
            if (targetCamera == null)
            {
                Debug.LogWarning("[ChildrenSwarmEvent] 카메라가 없어 이벤트를 시작할 수 없습니다.");
                return;
            }

            running = true;

            float screenTop = ScreenWorldY(1f);
            float screenBottom = ScreenWorldY(0f);
            float halfH = bandHeight * 0.5f;
            float yMin = screenBottom + halfH;
            float yMax = screenTop - halfH;
            bandCenterY = yMin <= yMax ? Random.Range(yMin, yMax) : (screenTop + screenBottom) * 0.5f;

            float screenRight = ScreenWorldX(1f);
            float startX = screenRight + bandWidth * 0.5f + 0.1f;

            visual.transform.localScale = new Vector3(bandWidth, bandHeight, 1f);
            visual.transform.position = new Vector3(startX, bandCenterY, displayZ);
            visual.SetActive(true);

            minX = startX;
            maxX = startX;

            Debug.Log($"[ChildrenSwarmEvent] 아이 무리 등장 (Y={bandCenterY:F2})");
        }

        private void MoveStep()
        {
            var pos = visual.transform.position;
            pos.x -= moveSpeed * Time.deltaTime;
            visual.transform.position = pos;

            minX = Mathf.Min(minX, pos.x);
            maxX = Mathf.Max(maxX, pos.x);

            float screenLeft = ScreenWorldX(0f);
            float exitX = screenLeft - bandWidth * 0.5f - 0.1f;
            if (pos.x <= exitX) FinishEvent();
        }

        private void FinishEvent()
        {
            running = false;
            visual.SetActive(false);

            float bandMinX = minX - bandWidth * 0.5f;
            float bandMaxX = maxX + bandWidth * 0.5f;
            var band = new Rect(bandMinX, bandCenterY - bandHeight * 0.5f, bandMaxX - bandMinX, bandHeight);

            Debug.Log("[ChildrenSwarmEvent] 아이 무리 퇴장 — 스폰 버스트 실행");
            if (itemSpawner != null) itemSpawner.TriggerSpawnBurst(band);
        }

        private float ScreenWorldX(float viewportX)
        {
            float dist = Mathf.Abs(targetCamera.transform.position.z - displayZ);
            return targetCamera.ViewportToWorldPoint(new Vector3(viewportX, 0.5f, dist)).x;
        }

        private float ScreenWorldY(float viewportY)
        {
            float dist = Mathf.Abs(targetCamera.transform.position.z - displayZ);
            return targetCamera.ViewportToWorldPoint(new Vector3(0.5f, viewportY, dist)).y;
        }
    }
}
