using KoreanGame.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KoreanGame.Gameplay
{
    /// <summary>Kệ gỗ đầu màn hình (Level N, số thẻ còn lại), nút Chơi lại / Hoàn tác. Không có chữ hướng dẫn.</summary>
    public class HudView : MonoBehaviour
    {
        [SerializeField] Text levelText;
        [SerializeField] Text cardsLeftText;
        [SerializeField] Button restartButton;
        [SerializeField] Button undoButton;

        LevelController controller;

        public void Init(LevelController c)
        {
            controller = c;
            restartButton.onClick.AddListener(controller.Restart);
            undoButton.onClick.AddListener(controller.Undo);
            EventBus<LevelLoadedEvent>.Subscribe(OnLoaded);
            EventBus<BoardChangedEvent>.Subscribe(OnChanged);
        }

        void OnDestroy()
        {
            EventBus<LevelLoadedEvent>.Unsubscribe(OnLoaded);
            EventBus<BoardChangedEvent>.Unsubscribe(OnChanged);
        }

        void OnLoaded(LevelLoadedEvent e)
        {
            levelText.text = $"Màn {e.index + 1}";
            OnChanged(default);
        }

        void OnChanged(BoardChangedEvent e)
        {
            if (controller.State == null) return;
            cardsLeftText.text = $"×{controller.State.CardsLeft}";
            undoButton.interactable = controller.CanUndo;
        }
    }
}
