using System;
using UnityEngine;

namespace PickupCent.Digging
{
    /// <summary>
    /// 키보드 1/2/3(또는 도구바 UI)으로 손/삽/금속탐지기를 전환한다.
    /// 세 도구 모두 SandMaskController.Strength를 갈아끼워 기존 강도÷경도 공식으로 판다 —
    /// 금속탐지기도 예외 없이 파기가 기본 동작이며, 아이템 종류 상관없이 습득 가능하다.
    /// 금속탐지기의 "탐지"는 파기와 별개의 부가 기능이다: 클릭 여부와 무관하게 매 프레임 마우스
    /// 위치를 검사해서, detectableByMetalDetector가 true인 아이템 근처에 일정 시간 이상 머무르면
    /// 그 아이템을 "발견 표시(spotted)"만 한다 — 습득은 여전히 파야 한다(DiggableItem.UpdateDetectorHover).
    /// </summary>
    [DefaultExecutionOrder(-50)] // SandMaskController(-100) 이후, SandDigInput(기본값 0) 이전에 실행
    public class ToolManager : MonoBehaviour
    {
        public enum ToolType { Hand, Shovel, Detector }

        [Header("도구별 강도 (임시값)")]
        [SerializeField] private float handStrength = 1f;
        [SerializeField] private float shovelStrength = 3f;
        [Tooltip("금속탐지기의 파기 강도. 손과 같거나 살짝 낮게(예시값)")]
        [SerializeField] private float detectorStrength = 0.8f;

        [Header("삽 파괴 리스크 (기획서 3-1: 빠르지만 가끔 터짐)")]
        [SerializeField, Range(0f, 1f)] private float shovelDestroyChance = 0.05f;

        [Header("금속탐지기 - 탐지(발견 표시) 전용, 파기와는 별개")]
        [SerializeField] private float detectorRadius = 1.5f;
        [Tooltip("마우스가 반경 안에 이 시간(초) 이상 머무르면 발견 표시가 뜬다")]
        [SerializeField] private float detectorDwellTime = 0.2f;

        [SerializeField] private SandMaskController sandMask;
        [SerializeField] private Camera targetCamera;

        /// <summary>실제로 도구가 바뀔 때(초기화 시점 제외) 발생 — 사운드 등 알림용.</summary>
        public event Action<ToolType> OnToolSwitched;

        private ToolType currentTool = ToolType.Hand;

        // 강화(파기 강도 강화)로 누적되는 보너스. 손/삽 공통 적용 — 기획서 7장: "강도 자체를 올리는 것".
        // (금속탐지기는 기획서 7장 표에 강화 대상으로 명시되지 않아 이 보너스의 영향을 받지 않는다.)
        private float strengthBonus;

        public ToolType CurrentTool => currentTool;
        public float StrengthBonus => strengthBonus;
        // 세 도구 모두 파기가 기본 동작이라 지금은 항상 true — 훗날 파기가 아닌 도구가 추가될 때를 대비해 남겨둠.
        public bool IsDiggingTool => true;

        // --- 디버그 패널 등에서 실시간 조절하기 위한 get/set 프로퍼티 ---

        public float HandStrength
        {
            get => handStrength;
            set { handStrength = value; ApplyToolStrength(); }
        }

        public float ShovelStrength
        {
            get => shovelStrength;
            set { shovelStrength = value; ApplyToolStrength(); }
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
            ApplyToolStrength();
            LogToolChanged();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchTool(ToolType.Hand);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchTool(ToolType.Shovel);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchTool(ToolType.Detector);

            if (currentTool == ToolType.Detector) UpdateDetectorHoverScan();
        }

        /// <summary>도구를 전환한다. 숫자 1/2/3 단축키와 도구바 UI 버튼이 공통으로 호출 — 상태가 항상 하나로 유지된다.</summary>
        public void SwitchTool(ToolType tool)
        {
            if (currentTool == tool) return;
            currentTool = tool;
            ApplyToolStrength();
            LogToolChanged();
            OnToolSwitched?.Invoke(currentTool);
        }

        private void ApplyToolStrength()
        {
            if (sandMask == null) return;
            float baseStrength = currentTool switch
            {
                ToolType.Shovel => shovelStrength,
                ToolType.Detector => detectorStrength,
                _ => handStrength
            };
            sandMask.Strength = baseStrength + strengthBonus;
        }

        // --- 강화 효과 적용 (UpgradeManager가 호출) ---

        /// <summary>파기 강도 강화. 손/삽 공통으로 적용되는 보너스를 누적한다.</summary>
        public void AddStrengthBonus(float amount)
        {
            strengthBonus += amount;
            ApplyToolStrength();
            Debug.Log($"[Tool] 파기 강도 보너스 +{amount} (누적 {strengthBonus}) — 손/삽 공통 적용");
        }

        /// <summary>삽 안정성 강화. 파괴 확률을 감소시킨다(0 밑으로는 안 내려감).</summary>
        public void ReduceShovelDestroyChance(float amount)
        {
            shovelDestroyChance = Mathf.Max(0f, shovelDestroyChance - amount);
            Debug.Log($"[Tool] 삽 파괴 확률 -{amount:P1} → 현재 {shovelDestroyChance:P1}");
        }

        /// <summary>탐지 범위 강화. 금속탐지기 반경을 확장한다.</summary>
        public void AddDetectorRadius(float amount)
        {
            detectorRadius += amount;
            Debug.Log($"[Tool] 탐지 범위 +{amount} → 현재 {detectorRadius}");
        }

        private void LogToolChanged()
        {
            string label = currentTool switch
            {
                ToolType.Hand => "손",
                ToolType.Shovel => "삽",
                ToolType.Detector => "금속탐지기",
                _ => currentTool.ToString()
            };
            Debug.Log($"[Tool] 현재 도구: {label}");
        }

        /// <summary>
        /// 클릭 여부와 무관하게 매 프레임 실행 — 마우스 위치가 탐지 가능한 아이템 근처에
        /// detectorDwellTime 이상 머무르면 그 아이템을 "발견 표시"한다(습득 아님).
        /// </summary>
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
