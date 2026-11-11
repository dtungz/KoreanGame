using KoreanGame.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KoreanGame.Shared
{
    /// <summary>
    /// Nút "lún" khi bấm (style guide): nội dung (chữ / icon) dịch xuống 4 đơn vị cùng lúc Button đổi sang sprite
    /// bóng ngắn (SpriteSwap). Thả tay hoặc nút bị tắt thì về chỗ cũ. Bấm xong (onClick) phát tiếng sound (mặc định Click).
    /// </summary>
    public class ButtonPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] Selectable selectable;
        [SerializeField] RectTransform content;
        [SerializeField] float offset = 4;
        [SerializeField] Sfx sound = Sfx.Click;
        [Tooltip("Tắt tiếng bấm (nút đã có tiếng riêng)")]
        [SerializeField] bool silent;

        Vector2 home;
        bool down;

        public Sfx Sound { get => sound; set { sound = value; silent = false; } }
        public bool Silent { get => silent; set => silent = value; }

        void Awake()
        {
            home = content.anchoredPosition;
            if (selectable is Button b) b.onClick.AddListener(() => { if (!silent) AudioManager.PlayUi(sound); });
        }

        /// <summary>Đổi tiếng bấm của nút: có ButtonPress thì đổi ở đó, không thì gắn thêm listener.</summary>
        public static void SetSound(Button b, Sfx s)
        {
            if (b == null) return;
            var press = b.GetComponent<ButtonPress>();
            if (press != null) press.Sound = s;
            else b.onClick.AddListener(() => AudioManager.PlayUi(s));
        }

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
