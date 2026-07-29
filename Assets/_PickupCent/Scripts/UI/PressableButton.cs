using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PickupCent.UI
{
    /// <summary>
    /// 스타일 가이드 4-1의 시그니처 인터랙션: 누르는 동안 버튼이 살짝(기본 3px) 아래로 내려가고,
    /// (제공됐다면) 아래쪽 보더가 얇은 "pressed" 텍스처로 바뀐다. 클릭이 실제로 무엇을 하는지는
    /// 그대로 Button.onClick이 담당한다 — 이 컴포넌트는 순수 시각 효과만 담당한다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class PressableButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private RectTransform rt;
        private Image image;
        private Vector2 restingPosition;
        private Sprite normalSprite;
        private Sprite pressedSprite;
        private float pressDepth = 3f;
        private bool pressed;

        public void Setup(Sprite normal, Sprite pressedVariant, float depth = 3f)
        {
            rt = (RectTransform)transform;
            image = GetComponent<Image>();
            normalSprite = normal;
            pressedSprite = pressedVariant;
            pressDepth = depth;
            restingPosition = rt.anchoredPosition;
            if (image != null && normalSprite != null) image.sprite = normalSprite;
            pressed = false;
        }

        public void OnPointerDown(PointerEventData eventData) => SetPressed(true);
        public void OnPointerUp(PointerEventData eventData) => SetPressed(false);
        public void OnPointerExit(PointerEventData eventData) => SetPressed(false);

        private void SetPressed(bool value)
        {
            if (pressed == value || rt == null) return;
            pressed = value;

            rt.anchoredPosition = pressed ? restingPosition - new Vector2(0f, pressDepth) : restingPosition;

            if (image != null && pressedSprite != null)
                image.sprite = pressed ? pressedSprite : normalSprite;
        }
    }
}
