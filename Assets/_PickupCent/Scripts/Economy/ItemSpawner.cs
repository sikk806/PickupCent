using System.Collections.Generic;
using PickupCent.Digging;
using UnityEngine;

namespace PickupCent.Economy
{
    /// <summary>
    /// itemPool(5종 ItemDefinition)을 가중치 기반으로 뽑아 필드 위에 항상 itemCount개를 유지한다.
    /// 습득되면 점수를 지급하고 같은 오브젝트를 재사용해 다른 자리로 재배치한다(파괴된 경우는 점수 없이 재배치만).
    /// </summary>
    public class ItemSpawner : MonoBehaviour
    {
        [SerializeField] private SandMaskController sandMask;
        [SerializeField] private ScoreTracker scoreTracker;
        [SerializeField] private ItemDefinition[] itemPool;
        [SerializeField] private int itemCount = 5;
        [Tooltip("필드 가장자리로부터 스폰을 피할 여백(월드 단위)")]
        [SerializeField] private float edgeMargin = 0.8f;

        private readonly List<DiggableItem> activeItems = new List<DiggableItem>();
        private float totalWeight;

        private void Awake()
        {
            if (sandMask == null) sandMask = FindFirstObjectByType<SandMaskController>();
            if (scoreTracker == null) scoreTracker = FindFirstObjectByType<ScoreTracker>();
            RecalculateWeights();
        }

        private void Start()
        {
            for (int i = 0; i < itemCount; i++) SpawnOne();
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

            float r = Random.value * totalWeight;
            float acc = 0f;
            foreach (var def in itemPool)
            {
                if (def == null) continue;
                acc += Mathf.Max(0f, def.spawnWeight);
                if (r <= acc) return def;
            }
            return itemPool[itemPool.Length - 1];
        }

        private Vector2 RandomSpawnPosition()
        {
            if (sandMask == null) return Vector2.zero;

            Vector2 field = sandMask.FieldSize;
            Vector2 half = new Vector2(
                Mathf.Max(0f, field.x * 0.5f - edgeMargin),
                Mathf.Max(0f, field.y * 0.5f - edgeMargin));
            Vector2 center = sandMask.transform.position;

            float x = Random.Range(-half.x, half.x);
            float y = Random.Range(-half.y, half.y);
            return center + new Vector2(x, y);
        }

        private void SpawnOne()
        {
            var def = PickWeighted();
            if (def == null)
            {
                Debug.LogWarning("[ItemSpawner] itemPool이 비어 있어 스폰할 수 없습니다.");
                return;
            }

            var go = new GameObject("Item");
            var item = go.AddComponent<DiggableItem>();
            item.Initialize(def, RandomSpawnPosition());
            item.OnAcquired += HandleAcquired;
            item.OnDestroyedByRisk += HandleDestroyedByRisk;
            activeItems.Add(item);
        }

        private void HandleAcquired(DiggableItem item)
        {
            var def = item.Definition;
            if (scoreTracker != null && def != null) scoreTracker.Add(def.value, def.itemName);
            Respawn(item);
        }

        private void HandleDestroyedByRisk(DiggableItem item)
        {
            Respawn(item);
        }

        private void Respawn(DiggableItem item)
        {
            var def = PickWeighted();
            if (def == null) return;
            item.Initialize(def, RandomSpawnPosition());
        }
    }
}
