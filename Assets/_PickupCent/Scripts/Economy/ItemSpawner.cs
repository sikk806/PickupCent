using System;
using System.Collections.Generic;
using PickupCent.Digging;
using PickupCent.UI;
using UnityEngine;

namespace PickupCent.Economy
{
    /// <summary>
    /// itemPool(5종 ItemDefinition)을 가중치 기반으로 뽑아 필드 위에 기본적으로 itemCount개를 유지한다.
    /// </summary>
    public class ItemSpawner : MonoBehaviour
    {
        public event Action<ItemDefinition> OnItemPickedUp;
        public event Action<ItemDefinition, int> OnItemPickedUpWithAmount;
        public event Action<DiggableItem> OnItemSpawned;
        public event Action OnSpawnBurst;
        public event Action OnDropWeightsChanged;

        [SerializeField] private SandMaskController sandMask;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private ScoreTracker scoreTracker;
        [SerializeField] private ComboManager comboManager;
        [SerializeField] private ItemDefinition[] itemPool;
        [SerializeField] private Sprite sparkleSprite1;
        [SerializeField] private Sprite sparkleSprite2;
        [SerializeField] private int itemCount = 20;
        [Tooltip("필드 가장자리로부터 스폰을 피할 여백(월드 단위)")]
        [SerializeField] private float edgeMargin = 0f;

        [Header("지형지물 스폰 편향")]
        [SerializeField] private TerrainFeature[] terrainFeatures;
        [Tooltip("평상시 스폰에서 지형지물 주변을 고를 확률(나머지는 필드 전체 균등)")]
        [SerializeField, Range(0f, 1f)] private float terrainBiasChance = 0f;

        private readonly List<DiggableItem> activeItems = new List<DiggableItem>();
        private float totalWeight;
        private float rareFindWeightBonus;

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
        public float RareFindWeightBonus => rareFindWeightBonus;

        public void SetSparkleSprites(Sprite first, Sprite second)
        {
            sparkleSprite1 = first;
            sparkleSprite2 = second;
            foreach (var item in activeItems)
                if (item != null) item.SetSpottedMarkerSprites(sparkleSprite1, sparkleSprite2);
        }

        private void Awake()
        {
            if (sandMask == null) sandMask = FindFirstObjectByType<SandMaskController>();
            if (targetCamera == null) targetCamera = Camera.main;
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
            if (itemPool == null || itemPool.Length == 0) return null;

            float adjustedTotal = 0f;
            foreach (var def in itemPool)
                if (def != null) adjustedTotal += AdjustedWeight(def);
            if (adjustedTotal <= 0f) return null;

            float r = UnityEngine.Random.value * adjustedTotal;
            float acc = 0f;
            foreach (var def in itemPool)
            {
                if (def == null) continue;
                acc += AdjustedWeight(def);
                if (r <= acc) return def;
            }
            return itemPool[itemPool.Length - 1];
        }

        public float GetAdjustedWeight(ItemDefinition def)
        {
            return AdjustedWeight(def);
        }

        public bool IsRareFindBonusTarget(ItemDefinition def)
        {
            return def != null && (def.value >= 10 || !def.detectableByMetalDetector);
        }

        private float AdjustedWeight(ItemDefinition def)
        {
            float weight = Mathf.Max(0f, def.spawnWeight);
            return IsRareFindBonusTarget(def) ? weight * (1f + rareFindWeightBonus) : weight;
        }

        public void AddRareFindWeightBonus(float amount)
        {
            rareFindWeightBonus = Mathf.Max(0f, rareFindWeightBonus + amount);
            Debug.Log($"[ItemSpawner] 발견 확률 보너스 +{amount:P0} → 현재 +{rareFindWeightBonus:P0}, 기본 totalWeight={totalWeight:0.##}");
            OnDropWeightsChanged?.Invoke();
        }

        private Vector2 PickSpawnPosition(Rect? burstBand, ItemDefinition def)
        {
            if (burstBand.HasValue)
            {
                return RandomPositionInBand(burstBand.Value, def);
            }

            return RandomUniformSpawnPosition(def);
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

        private Vector2 RandomPositionInBand(Rect band, ItemDefinition def)
        {
            float x = UnityEngine.Random.Range(band.xMin, band.xMax);
            float y = UnityEngine.Random.Range(band.yMin, band.yMax);
            return ClampToField(new Vector2(x, y), def);
        }

        private Vector2 RandomUniformSpawnPosition(ItemDefinition def = null)
        {
            if (sandMask == null) return Vector2.zero;

            Rect rect = ShrinkRect(GetSpawnWorldRect(), GetItemSpawnMargin(def));
            float x = UnityEngine.Random.Range(rect.xMin, rect.xMax);
            float y = UnityEngine.Random.Range(rect.yMin, rect.yMax);
            return new Vector2(x, y);
        }

        private Vector2 ClampToField(Vector2 pos, ItemDefinition def = null)
        {
            if (sandMask == null) return pos;

            Rect rect = ShrinkRect(GetSpawnWorldRect(), GetItemSpawnMargin(def));
            float x = Mathf.Clamp(pos.x, rect.xMin, rect.xMax);
            float y = Mathf.Clamp(pos.y, rect.yMin, rect.yMax);
            return new Vector2(x, y);
        }

        private Rect GetSpawnWorldRect()
        {
            Vector2 field = sandMask.FieldSize;
            Vector2 center = sandMask.transform.position;
            Vector2 half = new Vector2(
                Mathf.Max(0f, field.x * 0.5f - edgeMargin),
                Mathf.Max(0f, field.y * 0.5f - edgeMargin));

            Rect fieldRect = Rect.MinMaxRect(center.x - half.x, center.y - half.y, center.x + half.x, center.y + half.y);
            if (targetCamera == null) targetCamera = Camera.main;
            if (!UICanvasUtility.TryGetPlayableStageWorldInsets(targetCamera, out Vector4 stageInsets))
                return fieldRect;

            float xMin = fieldRect.xMin + stageInsets.x;
            float yMin = fieldRect.yMin + stageInsets.y;
            float xMax = fieldRect.xMax - stageInsets.z;
            float yMax = fieldRect.yMax - stageInsets.w;

            if (xMax <= xMin || yMax <= yMin) return fieldRect;
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private static Rect ShrinkRect(Rect rect, float margin)
        {
            if (margin <= 0f) return rect;

            float xMin = rect.xMin + margin;
            float xMax = rect.xMax - margin;
            float yMin = rect.yMin + margin;
            float yMax = rect.yMax - margin;
            if (xMax <= xMin || yMax <= yMin) return rect;
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private static float GetItemSpawnMargin(ItemDefinition def)
        {
            if (def == null) return 0f;
            if (def.artSprite != null)
            {
                Bounds bounds = def.artSprite.bounds;
                return Mathf.Max(bounds.extents.x, bounds.extents.y);
            }

            return Mathf.Max(0f, def.displaySize * 0.5f);
        }

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
            item.SetSpottedMarkerSprites(sparkleSprite1, sparkleSprite2);
            item.Initialize(def, PickSpawnPosition(burstBand, def));
            item.OnAcquired += HandleAcquired;
            item.OnDestroyedByRisk += HandleDestroyedByRisk;
            activeItems.Add(item);
            OnItemSpawned?.Invoke(item);
        }

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
                OnItemPickedUpWithAmount?.Invoke(def, scoreTracker.GetEarnedAmount(amount));
            }
            if (def != null) OnItemPickedUp?.Invoke(def);
            ResolveItem(item);
        }

        private void HandleDestroyedByRisk(DiggableItem item)
        {
            ResolveItem(item);
        }

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
            item.SetSpottedMarkerSprites(sparkleSprite1, sparkleSprite2);
            item.Initialize(def, PickSpawnPosition(null, def));
        }
    }
}
