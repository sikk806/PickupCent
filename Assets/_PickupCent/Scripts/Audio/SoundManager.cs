using System;
using System.Collections.Generic;
using PickupCent.Digging;
using PickupCent.Economy;
using PickupCent.Events;
using PickupCent.UI;
using PickupCent.Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace PickupCent.Audio
{
    /// <summary>
    /// 게임 안의 이벤트에 오디오 클립을 연결하는 중앙 사운드 매니저. 새 게임 로직은 없다 —
    /// 기존 컴포넌트들이 이미 발생시키는(또는 이번에 알림용으로 추가한) 이벤트를 구독해서
    /// 클립이 있으면 재생하고, 없으면(아직 오디오 연결 도구로 채워지지 않았으면) 조용히 넘어간다.
    /// 클립 목록은 AudioAssetLinker가 Assets/_PickupCent/Audio/audio_&lt;id&gt;.* 파일을 스캔해서 채운다.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        [Serializable]
        public class NamedClip
        {
            public string id; // "audio_<id>.wav" 파일명에서 <id> 부분과 일치
            public AudioClip clip;
        }

        [SerializeField] private List<NamedClip> clips = new List<NamedClip>();
        [SerializeField] private AudioSource oneShotSource;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        [Header("대상 참조 (비워두면 자동으로 찾음)")]
        [SerializeField] private SandDigInput digInput;
        [SerializeField] private ItemSpawner itemSpawner;
        [SerializeField] private ToolManager toolManager;
        [SerializeField] private UpgradeManager upgradeManager;
        [SerializeField] private ChildrenSwarmEvent swarmEvent;
        [SerializeField] private ShopPanelController shopPanel;

        [Header("희귀 판정 기준 (오디오 전용 — 게임 로직/밸런싱에는 영향 없음)")]
        [Tooltip("ItemDefinition.spawnWeight가 이 값 이하이면 '희귀'로 간주해 audio_collect_rare를 재생한다")]
        [SerializeField] private float rareSpawnWeightThreshold = 15f;

        private Dictionary<string, AudioClip> clipLookup;
        private readonly HashSet<DiggableItem> hookedItems = new HashSet<DiggableItem>();

        private void Awake()
        {
            BuildLookup();
            AutoFindReferences();

            if (oneShotSource == null) oneShotSource = gameObject.AddComponent<AudioSource>();
            oneShotSource.playOnAwake = false;
            oneShotSource.spatialBlend = 0f; // 2D

            SubscribeEvents();
            HookAllButtons();
        }

        private void BuildLookup()
        {
            clipLookup = new Dictionary<string, AudioClip>();
            if (clips == null) return;
            foreach (var c in clips)
            {
                if (c == null || string.IsNullOrEmpty(c.id) || c.clip == null) continue;
                clipLookup[c.id] = c.clip;
            }
        }

        private void AutoFindReferences()
        {
            if (digInput == null) digInput = FindFirstObjectByType<SandDigInput>();
            if (itemSpawner == null) itemSpawner = FindFirstObjectByType<ItemSpawner>();
            if (toolManager == null) toolManager = FindFirstObjectByType<ToolManager>();
            if (upgradeManager == null) upgradeManager = FindFirstObjectByType<UpgradeManager>();
            if (swarmEvent == null) swarmEvent = FindFirstObjectByType<ChildrenSwarmEvent>();
            if (shopPanel == null) shopPanel = FindFirstObjectByType<ShopPanelController>();
        }

        /// <summary>클립이 있으면 재생하고, 없으면 경고 없이 조용히 무시한다(요청사항).</summary>
        private void Play(string id)
        {
            if (clipLookup == null) return;
            if (clipLookup.TryGetValue(id, out var clip) && clip != null)
                oneShotSource.PlayOneShot(clip, volume);
        }

        private void SubscribeEvents()
        {
            if (digInput != null)
                digInput.OnStroke += () => Play("dig");

            if (itemSpawner != null)
            {
                itemSpawner.OnItemPickedUp += HandleItemPickedUp;
                itemSpawner.OnSpawnBurst += () => Play("spawn_burst");
                itemSpawner.OnItemSpawned += HookItem;
            }

            if (toolManager != null)
                toolManager.OnToolSwitched += _ => Play("tool_switch");

            if (upgradeManager != null)
            {
                upgradeManager.OnPurchaseSucceeded += _ => Play("upgrade_success");
                upgradeManager.OnPurchaseFailed += _ => Play("upgrade_fail");
            }

            if (swarmEvent != null)
                swarmEvent.OnSwarmStarted += () => Play("swarm_start");

            if (shopPanel != null)
                shopPanel.OnPanelToggled += isOpen => Play(isOpen ? "shop_open" : "shop_close");
        }

        /// <summary>아이템 인스턴스별 이벤트(파괴/발견)는 스포너가 새로 만들 때마다 개별 구독해야 한다.</summary>
        private void HookItem(DiggableItem item)
        {
            if (item == null || !hookedItems.Add(item)) return;
            item.OnSpotted += _ => Play("detect_found");
            item.OnDestroyedByRisk += _ => Play("shovel_break");
        }

        private void HandleItemPickedUp(ItemDefinition def)
        {
            if (def == null) return;
            bool rare = def.spawnWeight > 0f && def.spawnWeight <= rareSpawnWeightThreshold;
            Play(rare ? "collect_rare" : "collect_common");
        }

        /// <summary>UICanvas 밑의 모든 버튼에 공통 클릭 사운드를 추가로 걸어준다(기존 onClick과 별개로 추가).</summary>
        private void HookAllButtons()
        {
            var canvasGO = GameObject.Find("UICanvas");
            if (canvasGO == null) return;

            var buttons = canvasGO.GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
                button.onClick.AddListener(() => Play("ui_click"));
        }
    }
}
