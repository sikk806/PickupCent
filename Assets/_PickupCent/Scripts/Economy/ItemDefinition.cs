using UnityEngine;

namespace PickupCent.Economy
{
    /// <summary>
    /// 습득 가능한 아이템 한 종류의 데이터. README 4장(공통 코인)·6장(스테이지1 전용 아이템) 참고.
    /// 수치는 전부 예시값이며 추후 밸런싱 대상이다.
    /// </summary>
    [CreateAssetMenu(menuName = "PickupCent/Item Definition", fileName = "NewItemDefinition")]
    public class ItemDefinition : ScriptableObject
    {
        public enum ItemShape { Circle, Square }

        [Header("기본 정보")]
        public string itemName = "New Item";

        [Tooltip("습득 시 누적 점수(가치). 임시값, 추후 밸런싱")]
        public int value = 1;

        [Tooltip("스폰 가중치(상대값). 다른 아이템 대비 등장 확률을 결정한다")]
        public float spawnWeight = 1f;

        [Tooltip("금속탐지기로 탐지 가능한지 (기획서 3장: 코인 3종만 true)")]
        public bool detectableByMetalDetector;

        [Header("표시 (아트 에셋 없이 기본 도형으로 구분)")]
        public ItemShape shape = ItemShape.Circle;
        public Color displayColor = Color.white;
        [Tooltip("월드 단위 표시 크기(지름/한 변)")]
        public float displaySize = 1f;
    }
}
