using System;
using PickupCent.Common;
using PickupCent.Economy;
using UnityEngine;

namespace PickupCent.Digging
{
    /// <summary>
    /// 파낼 수 있는 아이템 하나. 몸 주변 9개 체크포인트의 마스크 값을 확인해서
    /// 75% 이상이 알파 10 이하(=뚫림)가 되면 습득 처리한다. 손/삽/금속탐지기 전부 이 파기 경로를
    /// 공유하므로 도구 종류나 아이템 종류(코인/구슬/딱지)와 무관하게 파면 습득할 수 있다.
    /// itemDefinition이 있으면(ItemSpawner가 생성) 그 값으로 점수/탐지가능여부를 판단하고,
    /// 없으면(Test1/Test2의 고정 더미) 예전처럼 값 없이 습득 로그만 남긴다 — 하위 호환용.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class DiggableItem : MonoBehaviour
    {
        [SerializeField] private SandMaskController sandMask;
        [SerializeField] private ToolManager toolManager;
        [SerializeField] private ItemDefinition itemDefinition;

        [Header("금속탐지기 발견 표시(마커) - 습득과 무관한 정보성 표시")]
        [SerializeField] private Color spottedMarkerColor = new Color(1f, 0.95f, 0.2f);
        [SerializeField] private float spottedMarkerSize = 0.3f;

        [Tooltip("체크포인트 3x3 격자가 퍼지는 반경(로컬 단위)")]
        [SerializeField] private float checkRadius = 0.4f;
        [SerializeField, Range(0f, 1f)] private float pickupRatio = 0.75f;
        [SerializeField] private float checkInterval = 0.15f;
        [SerializeField] private bool verboseLogging = true;
        [SerializeField] private float displayZ = 0.5f;

        [Header("테스트 전용 (반복 테스트 편의 기능)")]
        [Tooltip(
            "켜두면, 습득된 아이템이 다시 덮여 노출도가 75% 밑으로 떨어질 때 습득 상태를 취소하고 " +
            "'미습득'으로 되돌려서 씬 재시작 없이 반복 테스트할 수 있게 한다. " +
            "주의: ItemSpawner가 붙어 있으면 습득 즉시 이 오브젝트가 새 위치/종류로 재배치되므로 " +
            "실제 플레이에서는 이 되돌림이 발동할 일이 거의 없다(스포너 없이 단독 테스트할 때를 위한 기능). " +
            "실제 게임에서는 한 번 습득하면 영구적으로 사라지고 보상도 취소되지 않으므로, " +
            "경제 시스템이 최종 확정되면 이 옵션을 끄거나 관련 코드를 제거할 것.")]
        [SerializeField] private bool testOnlyAllowReacquire = true;

        /// <summary>정상 습득(파괴되지 않음) 시 발생. 스포너가 점수 지급/재배치에 사용.</summary>
        public event Action<DiggableItem> OnAcquired;
        /// <summary>삽 파괴 리스크로 소실됐을 때 발생.</summary>
        public event Action<DiggableItem> OnDestroyedByRisk;

        public ItemDefinition Definition => itemDefinition;

        private SpriteRenderer sr;
        private SpriteRenderer spottedMarker;
        private Vector2[] checkpointOffsets;
        private int lastExposedCount = -1;
        private float timer;
        private bool found;
        private bool destroyed;
        private bool spotted;
        private float detectorHoverTimer;

        private string DisplayName => itemDefinition != null ? itemDefinition.itemName : name;

        private void OnValidate() => GenerateOffsets();

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            if (sandMask == null) sandMask = FindFirstObjectByType<SandMaskController>();
            if (toolManager == null) toolManager = FindFirstObjectByType<ToolManager>();
            EnsureSpottedMarker();
            GenerateOffsets();
            if (itemDefinition != null) ApplyVisual();
        }

        /// <summary>ItemSpawner가 새 아이템으로 (재)배치할 때 호출. 상태를 전부 초기화한다.</summary>
        public void Initialize(ItemDefinition def, Vector2 worldPosition)
        {
            itemDefinition = def;
            transform.position = new Vector3(worldPosition.x, worldPosition.y, displayZ);

            found = false;
            destroyed = false;
            lastExposedCount = -1;
            timer = 0f;
            HideSpottedMarker();

            if (sr == null) sr = GetComponent<SpriteRenderer>();
            ApplyVisual();
        }

        private void EnsureSpottedMarker()
        {
            if (spottedMarker != null) return;

            var markerGO = new GameObject("SpottedMarker");
            markerGO.transform.SetParent(transform, false);
            // 사각의 모래 레이어(z=0)보다 카메라에 더 가깝게 둬서, 덜 파낸 상태에서도 표시가 가려지지 않게 한다.
            markerGO.transform.localPosition = new Vector3(0f, 0.5f, -1f);

            spottedMarker = markerGO.AddComponent<SpriteRenderer>();
            spottedMarker.sprite = ProceduralSprites.CreateCircle(32, spottedMarkerColor, spottedMarkerSize);
            spottedMarker.sortingOrder = 10;
            markerGO.SetActive(false);
        }

        private void ApplyVisual()
        {
            if (sr == null || itemDefinition == null) return;
            sr.sprite = itemDefinition.shape == ItemDefinition.ItemShape.Circle
                ? ProceduralSprites.CreateCircle(64, itemDefinition.displayColor, itemDefinition.displaySize)
                : ProceduralSprites.CreateSquare(64, itemDefinition.displayColor, itemDefinition.displaySize);
            sr.color = Color.white;
        }

        private void GenerateOffsets()
        {
            checkpointOffsets = new Vector2[9];
            int i = 0;
            for (int gy = -1; gy <= 1; gy++)
                for (int gx = -1; gx <= 1; gx++)
                    checkpointOffsets[i++] = new Vector2(gx, gy) * checkRadius;
        }

        private void Update()
        {
            if (destroyed || sandMask == null) return;

            timer += Time.deltaTime;
            if (timer < checkInterval) return;
            timer = 0f;

            float threshold255 = sandMask.HoleThresholdNormalized * 255f;
            int exposed = 0;
            for (int i = 0; i < checkpointOffsets.Length; i++)
            {
                Vector2 worldPt = (Vector2)transform.position + checkpointOffsets[i];
                if (sandMask.SampleAlpha255(worldPt) <= threshold255) exposed++;
            }

            float ratio = (float)exposed / checkpointOffsets.Length;

            if (verboseLogging && exposed != lastExposedCount)
            {
                lastExposedCount = exposed;
                Debug.Log($"[DiggableItem:{DisplayName}] 노출된 체크포인트 {exposed}/9 ({ratio:P0})");
            }

            if (!found && ratio >= pickupRatio)
            {
                TryAcquire(exposed, ratio);
            }
            else if (found && ratio < pickupRatio)
            {
                // --- 테스트 전용: 실제 게임 로직이 아님 (위 tooltip 참고) ---
                if (testOnlyAllowReacquire)
                {
                    found = false;
                    Debug.Log($"[DiggableItem:{DisplayName}] [TEST] 다시 덮여서 습득 취소 → 미습득 상태로 되돌림");
                }
                // --- 테스트 전용 끝 ---
            }
        }

        private void TryAcquire(int exposed, float ratio)
        {
            bool isShovel = toolManager != null && toolManager.CurrentTool == ToolManager.ToolType.Shovel;
            if (isShovel && UnityEngine.Random.value < toolManager.ShovelDestroyChance)
            {
                destroyed = true;
                HideSpottedMarker();
                Debug.Log($"[DiggableItem:{DisplayName}] 파괴됨 (삽 파괴 확률 발동, {exposed}/9 노출)");
                OnDestroyedByRisk?.Invoke(this);
                return;
            }

            found = true;
            HideSpottedMarker();
            string valueLabel = itemDefinition != null ? $", 가치 {itemDefinition.value}" : string.Empty;
            Debug.Log($"[DiggableItem:{DisplayName}] 습득됨 ({exposed}/9 노출, {ratio:P0}{valueLabel})");
            OnAcquired?.Invoke(this);
        }

        /// <summary>
        /// 금속탐지기 장착 중 매 프레임 호출(클릭 여부 무관). 습득이 아니라 "발견 표시"만 한다 —
        /// itemDefinition.detectableByMetalDetector가 true인 아이템에 한해, 마우스가 반경 안에
        /// dwellTime 이상 연속으로 머물러야 표시가 뜬다. 반경을 벗어나면 머문 시간은 리셋된다.
        /// </summary>
        public void UpdateDetectorHover(Vector2 mouseWorldPos, float radius, float dwellTime, float deltaTime)
        {
            bool detectable = itemDefinition != null && itemDefinition.detectableByMetalDetector;
            if (!detectable || destroyed || found || spotted)
            {
                detectorHoverTimer = 0f;
                return;
            }

            bool inRange = Vector2.Distance(transform.position, mouseWorldPos) <= radius;
            if (!inRange)
            {
                detectorHoverTimer = 0f;
                return;
            }

            detectorHoverTimer += deltaTime;
            if (detectorHoverTimer >= dwellTime)
            {
                spotted = true;
                if (spottedMarker != null) spottedMarker.gameObject.SetActive(true);
                Debug.Log($"[DiggableItem:{DisplayName}] 금속탐지기에 발견 표시됨 (습득 아님, 파야 얻을 수 있음)");
            }
        }

        private void HideSpottedMarker()
        {
            spotted = false;
            detectorHoverTimer = 0f;
            if (spottedMarker != null) spottedMarker.gameObject.SetActive(false);
        }

        private void OnDrawGizmosSelected()
        {
            if (checkpointOffsets == null) GenerateOffsets();
            Gizmos.color = destroyed ? Color.black : (found ? Color.green : Color.red);
            foreach (var off in checkpointOffsets)
                Gizmos.DrawWireSphere((Vector2)transform.position + off, 0.05f);
        }
    }
}
