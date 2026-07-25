using PickupCent.Digging;
using PickupCent.Economy;
using PickupCent.Upgrades;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PickupCent.EditorTools
{
    /// <summary>
    /// [테스트용 메뉴 - 실제 게임 스테이지 번호와 무관, Test1/Test2/Test3/Test4... 순서로 계속 늘어남]
    /// 강화 시스템(ScriptableObject 강화 정의 4종 + 탐지 대상 확장 자리 1종 + UpgradeManager)을 구성한다.
    /// Test1(파기)·Test2(도구)·Test3(경제) 셋업이 먼저 실행돼 있어야 한다.
    /// </summary>
    public static class Test4UpgradeSetup
    {
        private const string UpgradeFolder = "Assets/_PickupCent/Data/Upgrades";

        [MenuItem("PickupCent/Test4. 강화 시스템 씬 구성")]
        public static void Setup()
        {
            GameObject sandGO = GameObject.Find("SandLayer");
            if (sandGO == null)
            {
                Debug.LogError("[Test4UpgradeSetup] SandLayer가 없습니다. 먼저 'PickupCent/Test1. 파기 테스트 씬 구성'을 실행하세요.");
                return;
            }
            var mask = sandGO.GetComponent<SandMaskController>();
            var toolManager = sandGO.GetComponent<ToolManager>();
            if (toolManager == null)
            {
                Debug.LogError("[Test4UpgradeSetup] ToolManager가 없습니다. 먼저 'PickupCent/Test2. 도구 시스템 씬 구성'을 실행하세요.");
                return;
            }

            var scoreTracker = Object.FindFirstObjectByType<ScoreTracker>();
            if (scoreTracker == null)
            {
                Debug.LogError("[Test4UpgradeSetup] ScoreTracker가 없습니다. 먼저 'PickupCent/Test3. 아이템 경제 씬 구성'을 실행하세요.");
                return;
            }

            EnsureFolder();

            var digStrength = CreateOrLoad("DigStrengthUpgrade", "파기 강도 강화",
                UpgradeDefinition.UpgradeType.DigStrength, 20, 1.6f, 0.5f, 5);
            var digRange = CreateOrLoad("DigRangeUpgrade", "파기 범위 강화",
                UpgradeDefinition.UpgradeType.DigRange, 25, 1.6f, 0.15f, 5);
            var shovelStability = CreateOrLoad("ShovelStabilityUpgrade", "삽 안정성 강화",
                UpgradeDefinition.UpgradeType.ShovelStability, 30, 1.8f, 0.01f, 5);
            var detectRange = CreateOrLoad("DetectRangeUpgrade", "탐지 범위 강화",
                UpgradeDefinition.UpgradeType.DetectRange, 20, 1.5f, 0.3f, 5);
            var detectionTargetExpansion = CreateOrLoad("DetectionTargetExpansionUpgrade", "탐지 대상 확장 (미구현)",
                UpgradeDefinition.UpgradeType.DetectionTargetExpansion, 0, 1f, 0f, 0);

            GameObject upgradeGO = GameObject.Find("UpgradeManager");
            if (upgradeGO == null) upgradeGO = new GameObject("UpgradeManager");
            var upgradeManager = upgradeGO.GetComponent<UpgradeManager>();
            if (upgradeManager == null) upgradeManager = upgradeGO.AddComponent<UpgradeManager>();

            var so = new SerializedObject(upgradeManager);
            SetRef(so, "scoreTracker", scoreTracker);
            SetRef(so, "sandMask", mask);
            SetRef(so, "toolManager", toolManager);
            SetRef(so, "digStrengthDef", digStrength);
            SetRef(so, "digRangeDef", digRange);
            SetRef(so, "shovelStabilityDef", shovelStability);
            SetRef(so, "detectRangeDef", detectRange);
            SetRef(so, "detectionTargetExpansionDef", detectionTargetExpansion);
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("[Test4UpgradeSetup] 씬 구성 완료: 강화 정의 4종 + 탐지 대상 확장(미구현) 자리 생성, " +
                      "UpgradeManager 배치. Play 후 Q(파기강도)/W(파기범위)/E(삽안정성)/R(탐지범위)로 구매해 보세요.");
        }

        private static void SetRef(SerializedObject so, string propertyName, Object value)
        {
            var prop = so.FindProperty(propertyName);
            if (prop != null) prop.objectReferenceValue = value;
        }

        private static void EnsureFolder()
        {
            if (AssetDatabase.IsValidFolder(UpgradeFolder)) return;
            if (!AssetDatabase.IsValidFolder("Assets/_PickupCent/Data"))
                AssetDatabase.CreateFolder("Assets/_PickupCent", "Data");
            AssetDatabase.CreateFolder("Assets/_PickupCent/Data", "Upgrades");
        }

        private static UpgradeDefinition CreateOrLoad(string assetName, string displayName,
            UpgradeDefinition.UpgradeType type, int baseCost, float costMultiplier, float effectPerLevel, int maxLevel)
        {
            string path = $"{UpgradeFolder}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<UpgradeDefinition>(path);
            if (existing != null) return existing;

            var def = ScriptableObject.CreateInstance<UpgradeDefinition>();
            def.upgradeName = displayName;
            def.type = type;
            def.baseCost = baseCost;
            def.costMultiplier = costMultiplier;
            def.effectPerLevel = effectPerLevel;
            def.maxLevel = maxLevel;

            AssetDatabase.CreateAsset(def, path);
            return def;
        }
    }
}
