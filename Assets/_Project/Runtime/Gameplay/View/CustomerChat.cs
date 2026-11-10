using DG.Tweening;
using KoreanGame.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KoreanGame.Gameplay
{
    /// <summary>
    /// Khách nói chuyện trong bong bóng phía trên thớt: vào màn (và khi Main Menu đóng) nói câu của màn
    /// (câu riêng của level hoặc ngẫu nhiên), chạm avatar thì nói câu khác, thắng thì khen.
    /// Bong bóng chỉ chứa lời thoại (hình mục tiêu đã có sẵn trên thớt). Không ảnh hưởng luật chơi.
    /// </summary>
    public class CustomerChat : MonoBehaviour
    {
        [SerializeField] CustomerLines lines;
        [SerializeField] Button avatarButton;
        [SerializeField] RectTransform bubble;
        [SerializeField] Text speechText;

        LevelController controller;
        string last, levelLine;

        public void Init(LevelController c)
        {
            controller = c;
            avatarButton.onClick.AddListener(() => Say(PickTalk()));
            EventBus<LevelLoadedEvent>.Subscribe(OnLoaded);
            EventBus<LevelWonEvent>.Subscribe(OnWon);
            EventBus<MenuClosedEvent>.Subscribe(OnMenuClosed);
            speechText.text = "";
        }

        void OnDestroy()
        {
            EventBus<LevelLoadedEvent>.Unsubscribe(OnLoaded);
            EventBus<LevelWonEvent>.Unsubscribe(OnWon);
            EventBus<MenuClosedEvent>.Unsubscribe(OnMenuClosed);
        }

        void OnDisable() => this.KillTweens(true);

        void OnLoaded(LevelLoadedEvent e)
        {
            var own = controller.Level != null ? controller.Level.customerLine : null;
            levelLine = string.IsNullOrWhiteSpace(own) ? PickTalk() : own;
            Say(levelLine);
        }

        // màn được nạp sau lưng Main Menu: khi menu đóng, bong bóng nảy lại để người chơi để ý
        void OnMenuClosed(MenuClosedEvent e) => Say(levelLine);

        void OnWon(LevelWonEvent e) => Say(Pick(lines.win));

        void Say(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            last = text;
            speechText.text = text;
            this.KillTweens(true);
            bubble.localScale = Vector3.one * .92f;
            bubble.DOScale(1f, .18f).SetEase(Ease.OutBack).Guard(this);
        }

        string PickTalk()
        {
            int a = lines.chitChat?.Length ?? 0, b = lines.foodTalk?.Length ?? 0;
            if (a + b == 0) return null;
            for (int tries = 0; tries < 4; tries++)
            {
                int i = Random.Range(0, a + b);
                var s = i < a ? lines.chitChat[i] : lines.foodTalk[i - a];
                if (s != last || a + b == 1) return s;
            }
            return a > 0 ? lines.chitChat[0] : lines.foodTalk[0];
        }

        static string Pick(string[] pool) => pool == null || pool.Length == 0 ? null : pool[Random.Range(0, pool.Length)];
    }
}
