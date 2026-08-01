using System;
using System.Collections.Generic;
using UnityEngine;

namespace PickupCent.Economy
{
    /// <summary>
    /// 콤보 카운트/타이머와 그에 따른 지급 배율을 관리한다. 아이템을 습득할 때마다
    /// RegisterPickupAndGetAmount()를 호출해 콤보를 1 올리고 그 즉시 배율이 적용된 최종 지급액을
    /// 계산한다. comboWindowSeconds 안에 다음 습득이 없으면 콤보는 자동으로 0으로 리셋된다.
    /// 배율은 comboStepSize(기본 10) 단위 계단식이다 — 10~19콤보=+10%, 20~29콤보=+20%, ...
    /// 최근 습득한 아이템 최대 5개(웹 프로토타입의 comboHistory와 동일한 개수)를 기록해 UI가
    /// 콤보 히스토리 슬롯으로 보여줄 수 있게 한다.
    /// </summary>
    public class ComboManager : MonoBehaviour
    {
        private const int HistoryCapacity = 5;

        [Tooltip("이 시간(초) 안에 다음 아이템을 습득하지 못하면 콤보가 0으로 리셋된다.")]
        [SerializeField] private float comboWindowSeconds = 2.5f;

        [Tooltip("배율이 한 단계 오르는 콤보 간격(10이면 10/20/30콤보마다 상승)")]
        [SerializeField] private int comboStepSize = 10;

        [Tooltip("단계 하나당 추가되는 배율(0.1 = +10%)")]
        [SerializeField] private float multiplierPerStep = 0.1f;

        private int combo;
        private float timer;
        private readonly List<ItemDefinition> history = new List<ItemDefinition>(HistoryCapacity);

        public int Combo => combo;

        /// <summary>다음 리셋까지 남은 시간의 비율(1=방금 습득, 0=곧 리셋) — 콤보 바 표시용.</summary>
        public float RemainingRatio => combo > 0 && comboWindowSeconds > 0f ? Mathf.Clamp01(timer / comboWindowSeconds) : 0f;

        /// <summary>최근 습득한 아이템 최대 5개(오래된 것부터). 콤보 히스토리 슬롯 UI가 읽는다.</summary>
        public IReadOnlyList<ItemDefinition> History => history;

        /// <summary>콤보 수치가 바뀔 때마다(증가든 0으로 리셋되든) 발생 — UI 표시용.</summary>
        public event Action<int> OnComboChanged;

        /// <summary>
        /// 씬에 이 컴포넌트를 미리 배치해 둘 방법이 없는 상황(에디터 접근 없이 스크립트로만 작업)을
        /// 위한 자가 부트스트랩 — 이미 있으면 그걸 쓰고, 없으면 새로 만든다.
        /// </summary>
        public static ComboManager EnsureInstance()
        {
            var existing = FindFirstObjectByType<ComboManager>();
            if (existing != null) return existing;

            var go = new GameObject("ComboManager");
            return go.AddComponent<ComboManager>();
        }

        /// <summary>아이템을 습득했을 때 호출한다. 콤보를 1 올리고, 그 즉시 배율을 적용한 최종 지급액을 반환한다.</summary>
        public int RegisterPickupAndGetAmount(ItemDefinition def)
        {
            combo++;
            timer = comboWindowSeconds;

            history.Add(def);
            if (history.Count > HistoryCapacity) history.RemoveAt(0);

            int step = combo / comboStepSize;
            float multiplier = 1f + step * multiplierPerStep;
            int finalAmount = Mathf.RoundToInt((def != null ? def.value : 0) * multiplier);

            OnComboChanged?.Invoke(combo);
            return finalAmount;
        }

        private void Update()
        {
            if (combo <= 0) return;

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                combo = 0;
                history.Clear();
                OnComboChanged?.Invoke(combo);
            }
        }
    }
}
