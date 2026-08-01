using System;
using PickupCent.Economy;
using PickupCent.UI;
using UnityEngine;

namespace PickupCent.Digging
{
    /// <summary>
    /// 손은 기본 도구이며, 나머지 도구는 상점에서 구매/수리/장착한다. 파기 공식은 README의 강도÷경도를 유지한다.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class ToolManager : MonoBehaviour
    {
        public enum ToolType { Hand, Shovel, Detector, Rake }

        [Header("도구별 강도")]
        [SerializeField] private float handStrength = 1f;
        [SerializeField] private float shovelStrength = 3f;
        [SerializeField] private float rakeStrength = 2.1f;
        [Tooltip("금속탐지기의 파기 강도. 탐지는 별도 기능이다.")]
        [SerializeField] private float detectorStrength = 0.8f;

        [Header("삽 파괴 리스크 (기획서 3-1: 빠르지만 가끔 터짐)")]
        [SerializeField, Range(0f, 1f)] private float shovelDestroyChance = 0.05f;

        [Header("금속탐지기 - 탐지(발견 표시) 전용")]
        [SerializeField] private float detectorRadius = 1.5f;
        [SerializeField] private float detectorDwellTime = 0.2f;

        [Header("상점 / 장착 상태")]
        [SerializeField] private int shovelPurchaseCost = 120;
        [SerializeField] private int rakePurchaseCost = 150;
        [SerializeField] private int detectorPurchaseCost = 180;
        [SerializeField] private int shovelRepairCost = 35;
        [SerializeField] private int rakeRepairCost = 40;
        [SerializeField] private int detectorRepairCost = 45;
        [SerializeField, Range(0f, 200f)] private float shovelDurability = 93f;
        [SerializeField, Range(0f, 200f)] private float rakeDurability = 100f;
        [SerializeField, Range(0f, 200f)] private float detectorDurability = 100f;
        [SerializeField] private bool shovelOwned;
        [SerializeField] private bool rakeOwned;
        [SerializeField] private bool detectorOwned;

        [Header("도구별 브러시 반경")]
        [SerializeField] private float handBrushRadius = 0.5f;
        [SerializeField] private float shovelBrushRadius = 0.72f;
        [SerializeField] private float rakeBrushRadius = 0.82f;
        [SerializeField] private float detectorBrushRadius = 0.42f;

        [SerializeField] private SandMaskController sandMask;
        [SerializeField] private Camera targetCamera;

        public event Action<ToolType> OnToolSwitched;

        private ToolType currentTool = ToolType.Hand;
        private float strengthBonus;
        private float brushRadiusBonus;
        private float durabilityCapacityBonus;

        public ToolType CurrentTool => currentTool;
        public float StrengthBonus => strengthBonus;
        public float BrushRadiusBonus => brushRadiusBonus;
        public bool IsDiggingTool => true;

        public float HandStrength
        {
            get => handStrength;
            set { handStrength = value; ApplyToolStats(); }
        }

        public float ShovelStrength
        {
            get => shovelStrength;
            set { shovelStrength = value; ApplyToolStats(); }
        }

        public float ShovelDestroyChance
        {
            get => shovelDestroyChance;
            set => shovelDestroyChance = Mathf.Clamp01(value);
        }

        public float DetectorRadius
        {
            get => detectorRadius;
            set => detectorRadius = value;
        }

        public float DetectorDwellTime
        {
            get => detectorDwellTime;
            set => detectorDwellTime = value;
        }

        private void Awake()
        {
            if (sandMask == null) sandMask = FindFirstObjectByType<SandMaskController>();
            if (targetCamera == null) targetCamera = Camera.main;
            ApplyToolStats();
            LogToolChanged();
        }

        private void Update()
        {
            if (PopupPauseManager.IsPausedByPopup) return;

            if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchTool(ToolType.Hand);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchTool(ToolType.Shovel);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchTool(ToolType.Detector);
            else if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchTool(ToolType.Rake);

            if (currentTool == ToolType.Detector) UpdateDetectorHoverScan();
        }

        public void SwitchTool(ToolType tool)
        {
            if (!IsToolOwned(tool))
            {
                Debug.Log($"[Tool] {ToolLabel(tool)}은(는) 상점에서 구매한 뒤 장착할 수 있습니다.");
                return;
            }

            if (currentTool == tool) return;
            currentTool = tool;
            ApplyToolStats();
            LogToolChanged();
            OnToolSwitched?.Invoke(currentTool);
        }

        private void ApplyToolStats()
        {
            ApplyToolStrength();
            ApplyToolBrushRadius();
        }

        private void ApplyToolStrength()
        {
            if (sandMask == null) return;
            float baseStrength = currentTool switch
            {
                ToolType.Shovel => shovelStrength,
                ToolType.Rake => rakeStrength,
                ToolType.Detector => detectorStrength,
                _ => handStrength
            };
            sandMask.Strength = baseStrength + strengthBonus;
        }

        private void ApplyToolBrushRadius()
        {
            if (sandMask == null) return;
            sandMask.BrushRadius = GetToolBrushRadius(currentTool);
        }

        public bool IsToolOwned(ToolType tool) => tool switch
        {
            ToolType.Hand => true,
            ToolType.Shovel => shovelOwned,
            ToolType.Rake => rakeOwned,
            ToolType.Detector => detectorOwned,
            _ => false
        };

        public bool IsToolEquipped(ToolType tool) => currentTool == tool;

        public int GetToolPurchaseCost(ToolType tool) => tool switch
        {
            ToolType.Shovel => shovelPurchaseCost,
            ToolType.Rake => rakePurchaseCost,
            ToolType.Detector => detectorPurchaseCost,
            _ => 0
        };

        public int GetToolRepairCost(ToolType tool) => tool switch
        {
            ToolType.Shovel => shovelRepairCost,
            ToolType.Rake => rakeRepairCost,
            ToolType.Detector => detectorRepairCost,
            _ => 0
        };

        public float GetToolDurability(ToolType tool) => tool switch
        {
            ToolType.Shovel => shovelDurability,
            ToolType.Rake => rakeDurability,
            ToolType.Detector => detectorDurability,
            _ => GetToolMaxDurability(tool)
        };

        public float GetToolMaxDurability(ToolType tool) => tool == ToolType.Hand ? 100f : 100f + durabilityCapacityBonus;

        public float GetToolBrushRadius(ToolType tool)
        {
            float baseRadius = tool switch
            {
                ToolType.Shovel => shovelBrushRadius,
                ToolType.Rake => rakeBrushRadius,
                ToolType.Detector => detectorBrushRadius,
                _ => handBrushRadius
            };
            return Mathf.Max(0.05f, baseRadius + brushRadiusBonus);
        }

        public bool CanRepairTool(ToolType tool) => IsToolOwned(tool) && tool != ToolType.Hand && GetToolDurability(tool) < GetToolMaxDurability(tool);

        public bool TryPurchaseTool(ToolType tool, ScoreTracker tracker)
        {
            if (tool == ToolType.Hand) return true;
            if (IsToolOwned(tool)) return true;
            if (tracker == null) return false;

            int cost = GetToolPurchaseCost(tool);
            if (tracker.Score < cost)
            {
                Debug.Log($"[Tool] 구매 실패: {ToolLabel(tool)} 필요 {cost}, 보유 {tracker.Score}");
                return false;
            }

            tracker.Spend(cost, $"{ToolLabel(tool)} 구매");
            if (tool == ToolType.Shovel) shovelOwned = true;
            else if (tool == ToolType.Rake) rakeOwned = true;
            else if (tool == ToolType.Detector) detectorOwned = true;
            SetDurability(tool, GetToolMaxDurability(tool));
            Debug.Log($"[Tool] 구매 완료: {ToolLabel(tool)}");
            return true;
        }

        public bool TryRepairTool(ToolType tool, ScoreTracker tracker)
        {
            if (!CanRepairTool(tool) || tracker == null) return false;

            int cost = GetToolRepairCost(tool);
            if (tracker.Score < cost)
            {
                Debug.Log($"[Tool] 수리 실패: {ToolLabel(tool)} 필요 {cost}, 보유 {tracker.Score}");
                return false;
            }

            tracker.Spend(cost, $"{ToolLabel(tool)} 수리");
            SetDurability(tool, GetToolMaxDurability(tool));
            Debug.Log($"[Tool] 수리 완료: {ToolLabel(tool)}");
            return true;
        }

        public bool TryEquipTool(ToolType tool)
        {
            if (!IsToolOwned(tool)) return false;
            SwitchTool(tool);
            return true;
        }

        public void AddStrengthBonus(float amount)
        {
            strengthBonus += amount;
            ApplyToolStrength();
            Debug.Log($"[Tool] 파기 강도 보너스 +{amount} (누적 {strengthBonus}) — 손/삽 공통 적용");
        }

        public void ReduceShovelDestroyChance(float amount)
        {
            shovelDestroyChance = Mathf.Max(0f, shovelDestroyChance - amount);
            Debug.Log($"[Tool] 삽 파괴 확률 -{amount:P1} → 현재 {shovelDestroyChance:P1}");
        }

        public void AddDetectorRadius(float amount)
        {
            detectorRadius += amount;
            Debug.Log($"[Tool] 탐지 범위 +{amount} → 현재 {detectorRadius}");
        }

        public void AddDurabilityCapacityBonus(float amount)
        {
            durabilityCapacityBonus += amount;
            Debug.Log($"[Tool] 도구 최대 내구도 +{amount} (누적 {durabilityCapacityBonus})");
        }

        public void AddBrushRadiusBonus(float amount)
        {
            brushRadiusBonus += amount;
            ApplyToolBrushRadius();
            Debug.Log($"[Tool] 브러시 반경 보너스 +{amount} (누적 {brushRadiusBonus})");
        }

        private void SetDurability(ToolType tool, float value)
        {
            if (tool == ToolType.Shovel) shovelDurability = value;
            else if (tool == ToolType.Rake) rakeDurability = value;
            else if (tool == ToolType.Detector) detectorDurability = value;
        }

        private void LogToolChanged()
        {
            Debug.Log($"[Tool] 현재 도구: {ToolLabel(currentTool)}");
        }

        public static string ToolLabel(ToolType tool) => tool switch
        {
            ToolType.Hand => "손",
            ToolType.Shovel => "플라스틱 삽",
            ToolType.Rake => "갈퀴",
            ToolType.Detector => "금속탐지기",
            _ => tool.ToString()
        };

        private void UpdateDetectorHoverScan()
        {
            if (targetCamera == null) return;

            float planeZ = sandMask != null ? sandMask.transform.position.z : 0f;
            Vector3 screen = Input.mousePosition;
            screen.z = Mathf.Abs(targetCamera.transform.position.z - planeZ);
            Vector2 world = targetCamera.ScreenToWorldPoint(screen);

            float dt = Time.deltaTime;
            var items = FindObjectsByType<DiggableItem>(FindObjectsSortMode.None);
            foreach (var item in items)
                item.UpdateDetectorHover(world, detectorRadius, detectorDwellTime, dt);
        }
    }
}


