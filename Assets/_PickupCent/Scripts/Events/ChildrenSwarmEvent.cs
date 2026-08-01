using System;
using PickupCent.Common;
using PickupCent.Digging;
using PickupCent.Economy;
using UnityEngine;

namespace PickupCent.Events
{
    /// <summary>
    /// 아이 무리 이벤트. 상점에서 구매된 뒤에만 주기 타이머가 돌고, 구매 전에는 게임 시작부터 발생하지 않는다.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class ChildrenSwarmEvent : MonoBehaviour
    {
        [Header("구매 / 주기 / 이동")]
        [SerializeField] private int purchaseCost = 280;
        [SerializeField] private bool purchased;
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

        public bool IsPurchased => purchased;
        public bool IsEventRunning => running;
        public int PurchaseCost => purchaseCost;
        public event Action OnSwarmStarted;
        public float SecondsUntilNextEvent => !purchased || running ? 0f : Mathf.Max(0f, intervalSeconds - timer);

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

        public bool TryPurchase(ScoreTracker tracker)
        {
            if (purchased) return true;
            if (tracker == null) return false;
            if (tracker.Score < purchaseCost)
            {
                Debug.Log($"[ChildrenSwarmEvent] 구매 실패: 필요 {purchaseCost}, 보유 {tracker.Score}");
                return false;
            }

            tracker.Spend(purchaseCost, "아이들 이벤트 구매");
            purchased = true;
            timer = 0f;
            Debug.Log("[ChildrenSwarmEvent] 구매 완료 — 아이들 이벤트 타이머 시작");
            return true;
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
            if (!purchased) return;

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
