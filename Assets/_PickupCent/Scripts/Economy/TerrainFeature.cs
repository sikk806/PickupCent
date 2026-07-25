using UnityEngine;

namespace PickupCent.Economy
{
    /// <summary>
    /// 놀이터 지형지물(미끄럼틀/그네/벤치) 마커. 그 자체는 파거나 습득할 수 없고,
    /// ItemSpawner가 주변에 아이템 스폰을 편향시키는 기준점으로만 쓴다.
    /// </summary>
    public class TerrainFeature : MonoBehaviour
    {
        [SerializeField] private string featureName = "지형지물";
        [Tooltip("이 지형지물 주변으로 아이템 스폰이 편향되는 반경(월드 단위)")]
        [SerializeField] private float biasRadius = 1.2f;

        public string FeatureName => featureName;
        public float BiasRadius => biasRadius;
        public Vector2 Position => transform.position;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, biasRadius);
        }
    }
}
