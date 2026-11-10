using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KoreanGame.Shared
{
    /// <summary>
    /// Nút "lún" khi bấm (style guide): nội dung (chữ / icon) dịch xuống 4 đơn vị cùng lúc Button đổi sang sprite
    /// bóng ngắn (SpriteSwap). Thả tay hoặc nút bị tắt thì về chỗ cũ.
    /// </summary>
    public class ButtonPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] Selectable selectable;
        [SerializeField] RectTransform content;
        [SerializeField] float offset = 4;

        Vector2 home;
        bool down;

        void Awake() => home = content.anchoredPosition;

        public void OnPointerDown(PointerEventData e)
        {
            if (selectable != null && !selectable.IsInteractable()) return;
            down = true;
            content.anchoredPosition = home + Vector2.down * offset;
        }

        public void OnPointerUp(PointerEventData e) => Release();

        void OnDisable() => Release();

        void Release()
        {
            if (!down) return;
            down = false;
            if (content != null) content.anchoredPosition = home;
        }
    }
}
