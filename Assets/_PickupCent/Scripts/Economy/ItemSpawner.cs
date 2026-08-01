using System;
using System.Collections.Generic;
using PickupCent.Digging;
using UnityEngine;

namespace PickupCent.Economy
{
    /// <summary>
    /// itemPool(5종 ItemDefinition)을 가중치 기반으로 뽑아 필드 위에 기본적으로 itemCount개를 유지한다.
    /// 평상시 스폰 위치는 지형지물(TerrainFeature) 주변으로 편향되거나 필드 전체에서 균등하게 뽑힌다.
    /// 습득되면 점수를 지급하고, 활성 개수가 기본치 이하일 때만 같은 오브젝트를 재사용해 재배치한다
    /// (파괴된 경우는 점수 없이 동일하게 처리). TriggerSpawnBurst()로 일시적으로 개수를 2배까지 늘릴 수 있고,
    /// 그동안은 재보충을 멈춰서 습득할 때마다 자연스럽게 기본치로 줄어들게 한다.
    /// </summary>
    public class ItemSpawner : MonoBehaviour
    {
        /// <summary>정상 습득(점수 지급)될 때마다 발생. UI 습득 피드백 텍스트가 구독한다.</summary>
        public event Action<ItemDefinition> OnItemPickedUp;
        /// <summary>새 DiggableItem 오브젝트가 생성될 때(최초 채우기·버스트 추가 스폰 포함) 발생 —
        /// 사운드 매니저가 그 아이템 인스턴스의 OnDestroyedByRisk/OnSpotted를 구독하는 데 쓴다.</summary>
        public event Action<DiggableItem> OnItemSpawned;
        /// <summary>스폰 버스트로 아이템이 실제로 1개 이상 추가됐을 때 발생 — 사운드 등 알림용.</summary>
        public event Action OnSpawnBurst;

        [SerializeField] private SandMaskController sandMask;
        [SerializeField] private ScoreTracker scoreTracker;
        [SerializeField] private ComboManager comboManager;
        [SerializeField] private ItemDefinition[] itemPool;
        [SerializeField] private int itemCount = 5;
        [Tooltip("필드 가장자리로부터 스폰을 피할 여백(월드 단위)")]
        [SerializeField] private float edgeMargin = 0.8f;

        [Header("지형지물 스폰 편향")]
        [SerializeField] private TerrainFeature[] terrainFeatures;
        [Tooltip("평상시 스폰에서 지형지물 주변을 고를 확률(나머지는 필드 전체 균등)")]
        [SerializeField, Range(0f, 1f)] private float terrainBiasChance = 0.6f;

        private readonly List<DiggableItem> activeItems = new List<DiggableItem>();
        private float totalWeight;

        // --- 디버그 패널 등에서 실시간 조절하기 위한 get/set 프로퍼티 ---

        /// <summary>기본 유지 개수(Max). 낮추거나 높여도 기존 아이템을 즉시 강제로 맞추진 않고,
        /// 이후의 습득/버스트 판정부터 새 값을 사용한다.</summary>
        public int ItemCount
        {
            get => itemCount;
            set => itemCount = Mathf.Max(0, value);
        }

        public float TerrainBiasChance
        {
            get => terrainBiasChance;
            set => terrainBiasChance = Mathf.Clamp01(value);
        }

        /// <summary>드랍표 UI 등에서 확률 표시용으로 읽기 위한 접근자. 스폰 로직 자체는 그대로 private itemPool을 쓴다.</summary>
        public IReadOnlyList<ItemDefinition> ItemPool => itemPool;

        private void Awake()
        {
            if (sandMask == null) sandMask = FindFirstObjectByType<SandMaskController>();
            if (scoreTracker == null) scoreTracker = FindFirstObjectByType<ScoreTracker>();
            if (comboManager == null) comboManager = ComboManager.EnsureInstance();
            RecalculateWeights();
        }

        private void Start()
        {
            for (int i = 0; i < itemCount; i++) SpawnOne(null);
        }

        private void RecalculateWeights()
        {
            totalWeight = 0f;
            if (itemPool == null) return;
            foreach (var def in itemPool)
                if (def != null) totalWeight += Mathf.Max(0f, def.spawnWeight);
        }

        private ItemDefinition PickWeighted()
        {
            if (itemPool == null || itemPool.Length == 0 || totalWeight <= 0f) return null;

            float r = UnityEngine.Random.value * totalWeight;
            float acc = 0f;
            foreach (var def in itemPool)
            {
                if (def == null) continue;
                acc += Mathf.Max(0f, def.spawnWeight);
                if (r <= acc) return def;
            }
            return itemPool[itemPool.Length - 1];
        }

        // --- 스폰 위치 후보 ---

        /// <summary>burstBand가 있으면 "지형지물 주변 + 방금 지나간 밴드"가 합쳐진 후보군에서, 없으면 평상시 후보군에서 고른다.</summary>
        private Vector2 PickSpawnPosition(Rect? burstBand)
        {
            if (burstBand.HasValue)
            {
                bool hasTerrain = terrainFeatures != null && terrainFeatures.Length > 0;
                bool pickTerrain = hasTerrain && UnityEngine.Random.value < 0.5f;
                return pickTerrain ? RandomPositionNearTerrainFeature() : RandomPositionInBand(burstBand.Value);
            }

            return RandomBiasedSpawnPosition();
        }

        private Vector2 RandomBiasedSpawnPosition()
        {
            if (terrainFeatures != null && terrainFeatures.Length > 0 && UnityEngine.Random.value < terrainBiasChance)
                return RandomPositionNearTerrainFeature();
            return RandomUniformSpawnPosition();
        }

        private Vector2 RandomPositionNearTerrainFeature()
        {
            var feature = terrainFeatures[UnityEngine.Random.Range(0, terrainFeatures.Length)];
            Vector2 offset = UnityEngine.Random.insideUnitCircle * feature.BiasRadius;
            return ClampToField(feature.Position + offset);
        }

        private Vector2 RandomPositionInBand(Rect band)
        {
            float x = UnityEngine.Random.Range(band.xMin, band.xMax);
            float y = UnityEngine.Random.Range(band.yMin, band.yMax);
            return ClampToField(new Vector2(x, y));
        }

        private Vector2 RandomUniformSpawnPosition()
        {
            if (sandMask == null) return Vector2.zero;

            Vector2 field = sandMask.FieldSize;
            Vector2 half = new Vector2(
                Mathf.Max(0f, field.x * 0.5f - edgeMargin),
                Mathf.Max(0f, field.y * 0.5f - edgeMargin));
            Vector2 center = sandMask.transform.position;

            float x = UnityEngine.Random.Range(-half.x, half.x);
            float y = UnityEngine.Random.Range(-half.y, half.y);
            return center + new Vector2(x, y);
        }

        private Vector2 ClampToField(Vector2 pos)
        {
            if (sandMask == null) return pos;

            Vector2 field = sandMask.FieldSize;
            Vector2 center = sandMask.transform.position;
            Vector2 half = new Vector2(
                Mathf.Max(0f, field.x * 0.5f - edgeMargin),
                Mathf.Max(0f, field.y * 0.5f - edgeMargin));

            float x = Mathf.Clamp(pos.x, center.x - half.x, center.x + half.x);
            float y = Mathf.Clamp(pos.y, center.y - half.y, center.y + half.y);
            return new Vector2(x, y);
        }

        // --- 스폰 / 재보충 ---

        private void SpawnOne(Rect? burstBand)
        {
            var def = PickWeighted();
            if (def == null)
            {
                Debug.LogWarning("[ItemSpawner] itemPool이 비어 있어 스폰할 수 없습니다.");
                return;
            }

            var go = new GameObject("Item");
            var item = go.AddComponent<DiggableItem>();
            item.Initialize(def, PickSpawnPosition(burstBand));
            item.OnAcquired += HandleAcquired;
            item.OnDestroyedByRisk += HandleDestroyedByRisk;
            activeItems.Add(item);
            OnItemSpawned?.Invoke(item);
        }

        /// <summary>
        /// 아이 무리 이벤트가 지나간 뒤 호출. 지형지물 주변 + band를 합친 후보군으로,
        /// 활성 아이템 개수가 기본치(itemCount)의 2배가 될 때까지 추가로 스폰한다.
        /// </summary>
        public void TriggerSpawnBurst(Rect band)
        {
            int target = itemCount * 2;
            int added = 0;
            while (activeItems.Count < target)
            {
                SpawnOne(band);
                added++;
            }

            Debug.Log($"[ItemSpawner] 아이 무리 스폰 버스트 — {added}개 추가, 활성 아이템 {activeItems.Count}/{target}");
            if (added > 0) OnSpawnBurst?.Invoke();
        }

        private void HandleAcquired(DiggableItem item)
        {
            var def = item.Definition;
            if (scoreTracker != null && def != null)
            {
                int amount = comboManager != null ? comboManager.RegisterPickupAndGetAmount(def) : def.value;
                scoreTracker.Add(amount, def.itemName);
            }
            if (def != null) OnItemPickedUp?.Invoke(def);
            ResolveItem(item);
        }

        private void HandleDestroyedByRisk(DiggableItem item)
        {
            ResolveItem(item);
        }

        /// <summary>
        /// 활성 개수가 기본치를 넘는 동안(버스트로 늘어난 여분)이면 재보충하지 않고 오브젝트를 없애서
        /// 자연스럽게 기본치로 줄어들게 하고, 기본치 이하면 예전처럼 같은 자리에서 재배치한다.
        /// </summary>
        private void ResolveItem(DiggableItem item)
        {
            if (activeItems.Count > itemCount)
            {
                activeItems.Remove(item);
                item.OnAcquired -= HandleAcquired;
                item.OnDestroyedByRisk -= HandleDestroyedByRisk;
                Destroy(item.gameObject);
                return;
            }

            Respawn(item);
        }

        private void Respawn(DiggableItem item)
        {
            var def = PickWeighted();
            if (def == null) return;
            item.Initialize(def, PickSpawnPosition(null));
        }
    }
}
