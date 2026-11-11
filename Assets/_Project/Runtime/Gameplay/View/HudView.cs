using KoreanGame.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KoreanGame.Gameplay
{
    /// <summary>Kệ gỗ đầu màn hình (Level N, số thẻ còn lại), nút Chơi lại / Hoàn tác. Không có chữ hướng dẫn.</summary>
    public class HudView : MonoBehaviour
    {
        [SerializeField] Text levelText;
        [Tooltip("Tuỳ chọn: bộ đếm thẻ còn lại (scene Bento Journey dùng chỗ này cho vàng)")]
        [SerializeField] Text cardsLeftText;
        [SerializeField] Button restartButton;
        [SerializeField] Button undoButton;
        public RectTransform UndoRect => (RectTransform)undoButton.transform;
        public RectTransform RestartRect => (RectTransform)restartButton.transform;

        LevelController controller;

        public void Init(LevelController c)
        {
            controller = c;
            restartButton.onClick.AddListener(controller.Restart);
            undoButton.onClick.AddListener(controller.Undo);
            EventBus<LevelLoadedEvent>.Subscribe(OnLoaded);
            EventBus<BoardChangedEvent>.Subscribe(OnChanged);
            EventBus<LanguageChangedEvent>.Subscribe(OnLanguage);
        }

        void OnDestroy()
        {
            EventBus<LevelLoadedEvent>.Unsubscribe(OnLoaded);
            EventBus<BoardChangedEvent>.Unsubscribe(OnChanged);
            EventBus<LanguageChangedEvent>.Unsubscribe(OnLanguage);
        }

        void OnLoaded(LevelLoadedEvent e)
        {
            levelText.text = LocNames.Level(e.index + 1);
            OnChanged(default);
        }

        // đổi ngôn ngữ giữa màn: vẽ lại chữ HUD + tay bài ("+2 thẻ chờ")
        void OnLanguage(LanguageChangedEvent e)
        {
            if (controller.State == null) return;
            levelText.text = LocNames.Level(controller.Index + 1);
            controller.Redraw();
        }

        void OnChanged(BoardChangedEvent e)
        {
            if (controller.State == null) return;
            if (cardsLeftText != null) cardsLeftText.text = $"×{controller.State.CardsLeft}";
            undoButton.interactable = controller.CanUndo;
        }
    }
}
