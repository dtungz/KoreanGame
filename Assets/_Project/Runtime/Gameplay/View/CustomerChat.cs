using DG.Tweening;
using KoreanGame.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KoreanGame.Gameplay
{
    /// <summary>
    /// Khách nói chuyện trong bong bóng phía trên thớt và gọi món:
    /// - Màn có món đặc trưng: gọi đúng món ("Cho mình một hộp bún chả nhé!"), icon món ở góc avatar.
    /// - Màn thường có món phụ: thường nhắc món phụ trong hộp, đôi khi tám chuyện; icon món phụ ở góc avatar.
    /// - Câu riêng của level (LevelData.customerLine) luôn được ưu tiên.
    /// Chạm avatar thì nói câu khác, thắng thì khen (màn có món: khen đúng món). Không ảnh hưởng luật chơi.
    /// </summary>
    public class CustomerChat : MonoBehaviour
    {
        [SerializeField] CustomerLines lines;
        [SerializeField] Button avatarButton;
        [SerializeField] RectTransform bubble;
        [SerializeField] Text speechText;
        [Tooltip("Tuỳ chọn: icon món khách gọi ở góc avatar")]
        [SerializeField] Image orderIcon;
        [Tooltip("Tuỳ chọn: tên + ảnh món phụ")]
        [SerializeField] FoodCatalog catalog;

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
            if (orderIcon != null) orderIcon.gameObject.SetActive(false);
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
            var L = controller.Level;
            var dish = L != null ? L.unlocksDish : null;
            var side = Side(L);
            Sprite icon = dish != null ? dish.icon : side != null ? side.sprite : null;
            if (orderIcon != null)
            {
                orderIcon.sprite = icon;
                orderIcon.gameObject.SetActive(icon != null);
            }

            if (L != null && !string.IsNullOrWhiteSpace(L.customerLine)) levelLine = L.customerLine;
            else if (dish != null) levelLine = Format(lines.dishOrder, dish.displayName);
            else if (side != null && Random.value < .7f) levelLine = Format(lines.sideOrder, side.displayName);
            else levelLine = PickTalk();
            Say(levelLine);
        }

        // màn được nạp sau lưng Main Menu: khi menu đóng, bong bóng nảy lại để người chơi để ý
        void OnMenuClosed(MenuClosedEvent e) => Say(levelLine);

        void OnWon(LevelWonEvent e)
        {
            var dish = controller.Level != null ? controller.Level.unlocksDish : null;
            Say(dish != null ? Format(lines.dishWin, dish.displayName) : Pick(lines.win));
        }

        /// <summary>Món phụ đầu tiên của màn có trong danh mục (null nếu màn không có món phụ).</summary>
        SideDish Side(LevelData L)
        {
            if (L == null || catalog == null) return null;
            foreach (var f in L.tray.fixedFoods)
            {
                var s = catalog.FindSide(f.kind);
                if (s != null) return s;
            }
            return null;
        }

        static string Format(string[] pool, string name)
        {
            var t = Pick(pool);
            if (t == null || string.IsNullOrEmpty(name)) return t;
            // giữa câu viết thường ("một hộp bún chả"), đầu câu giữ hoa ("Bún chả thơm quá")
            var n = t.StartsWith("{0}") ? name : char.ToLower(name[0]) + name.Substring(1);
            return string.Format(t, n);
        }

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
