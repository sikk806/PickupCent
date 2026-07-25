using System;
using PickupCent.Digging;
using UnityEngine;
using UnityEngine.UI;

namespace PickupCent.UI
{
    /// <summary>
    /// 손/삽/금속탐지기 버튼 3개. 클릭하면 ToolManager.SwitchTool()을 호출 —
    /// 숫자 1/2/3 단축키도 같은 메서드를 호출하므로 두 입력 경로의 상태가 항상 일치한다.
    /// </summary>
    public class ToolBarController : MonoBehaviour
    {
        [Serializable]
        public class ToolButtonEntry
        {
            public ToolManager.ToolType tool;
            public Button button;
            public Image background;
        }

        [SerializeField] private ToolManager toolManager;
        [SerializeField] private ToolButtonEntry[] entries;
        [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.3f, 1f);
        [SerializeField] private Color unselectedColor = new Color(1f, 1f, 1f, 0.55f);

        private void Awake()
        {
            if (toolManager == null) toolManager = FindFirstObjectByType<ToolManager>();

            foreach (var entry in entries)
            {
                if (entry?.button == null) continue;
                var tool = entry.tool;
                entry.button.onClick.AddListener(() => toolManager.SwitchTool(tool));
            }
        }

        private void Update()
        {
            if (toolManager == null || entries == null) return;

            foreach (var entry in entries)
            {
                if (entry?.background == null) continue;
                entry.background.color = entry.tool == toolManager.CurrentTool ? selectedColor : unselectedColor;
            }
        }
    }
}
