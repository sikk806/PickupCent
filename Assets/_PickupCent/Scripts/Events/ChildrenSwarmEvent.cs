using System;
using PickupCent.Common;
using PickupCent.Digging;
using PickupCent.Economy;
using UnityEngine;

namespace PickupCent.Events
{
    /// <summary>
    /// 일정 주기로 아이 무리를 대표하는 오브젝트 하나가 맵 오른쪽 밖에서 등장해 왼쪽 밖으로
    /// 빠져나갈 때까지 가로로 이동한다. 지나간 영역(가로 밴드 = 오브젝트 높이 두께 x 이동한 전체 구간)을
    /// 기록해서, 다 지나가면 ItemSpawner.TriggerSpawnBurst()를 호출해 스폰 버스트를 일으킨다.
    /// 이동 범위는 화면(카메라 뷰포트)이 아니라 SandMaskController.FieldSize(맵 전체 크기) 기준이다 —
    /// 맵이 화면보다 커진 뒤에도 카메라가 어디를 보고 있든 맵 전체를 가로지른다.
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
        [Tooltip("아트 에셋 연결 도구가 채움 — 비어있으면 단색 사각형으로 절차적 표시")]
        [SerializeField] private Sprite artSprite;

        [SerializeField] private SandMaskController sandMask;
        [SerializeField] private ItemSpawner itemSpawner;

        private float timer;
        private bool running;
        private GameObject visual;
        private SpriteRenderer visualRenderer;
        private float minX, maxX;
        private float bandCenterY;

        public bool IsEventRunning => running;

        /// <summary>아이 무리 이벤트가 시작될 때(등장) 발생 — 사운드 등 알림용.</summary>
        public event Action OnSwarmStarted;
        public float SecondsUntilNextEvent => running ? 0f : Mathf.Max(0f, intervalSeconds - timer);

        // --- 디버그 패널 등에서 실시간 조절하기 위한 get/set 프로퍼티 ---
        public float IntervalSeconds
        {
            get => intervalSeconds;
            set => intervalSeconds = Mathf.Max(0f, value);
        }

        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = Mathf.Max(0f, value);
        }

        private void Awake()
        {
            if (sandMask == null) sandMask = FindFirstObjectByType<SandMaskController>();
            if (itemSpawner == null) itemSpawner = FindFirstObjectByType<ItemSpawner>();
            CreateVisual();
        }

        private void CreateVisual()
        {
            visual = new GameObject("ChildrenSwarmVisual");
            visual.transform.SetParent(transform, false);
            visualRenderer = visual.AddComponent<SpriteRenderer>();
            visualRenderer.sprite = artSprite != null ? artSprite : ProceduralSprites.CreateSquare(4, displayColor, 1f);
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
            if (sandMask == null)
            {
                Debug.LogWarning("[ChildrenSwarmEvent] SandMaskController가 없어 이벤트를 시작할 수 없습니다.");
                return;
            }

            running = true;

            Vector2 field = sandMask.FieldSize;
            Vector2 fieldCenter = sandMask.transform.position;
            float fieldTop = fieldCenter.y + field.y * 0.5f;
            float fieldBottom = fieldCenter.y - field.y * 0.5f;
            float fieldRight = fieldCenter.x + field.x * 0.5f;

            float halfH = bandHeight * 0.5f;
            float yMin = fieldBottom + halfH;
            float yMax = fieldTop - halfH;
            bandCenterY = yMin <= yMax ? UnityEngine.Random.Range(yMin, yMax) : (fieldTop + fieldBottom) * 0.5f;

            float startX = fieldRight + bandWidth * 0.5f + 0.1f;

            visual.transform.localScale = new Vector3(bandWidth, bandHeight, 1f);
            visual.transform.position = new Vector3(startX, bandCenterY, displayZ);
            visual.SetActive(true);

            minX = startX;
            maxX = startX;

            Debug.Log($"[ChildrenSwarmEvent] 아이 무리 등장 (Y={bandCenterY:F2})");
            OnSwarmStarted?.Invoke();
        }

        private void MoveStep()
        {
            var pos = visual.transform.position;
            pos.x -= moveSpeed * Time.deltaTime;
            visual.transform.position = pos;

            minX = Mathf.Min(minX, pos.x);
            maxX = Mathf.Max(maxX, pos.x);

            Vector2 fieldCenter = sandMask.transform.position;
            float fieldLeft = fieldCenter.x - sandMask.FieldSize.x * 0.5f;
            float exitX = fieldLeft - bandWidth * 0.5f - 0.1f;
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
    }
}
