using DG.Tweening;
using KoreanGame.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KoreanGame.Gameplay
{
    /// <summary>
    /// Khách nói chuyện trong bong bóng phía trên thớt và gọi món:
    /// - Mỗi màn một khách của vùng (Hà Nội / Seoul), chọn cố định theo số màn.
    /// - Màn có món đặc trưng: gọi đúng món ("Cho mình một hộp bún chả nhé!").
    /// - Màn thường có món phụ: thường nhắc món phụ trong hộp, đôi khi tám chuyện.
    /// - Câu riêng của level (LevelData.customerLine) luôn được ưu tiên.
    /// Chạm avatar thì nói câu khác, thắng thì khen (màn có món: khen đúng món). Không ảnh hưởng luật chơi.
    /// </summary>
    public class CustomerChat : MonoBehaviour
    {
        [SerializeField] CustomerLines lines;
        [SerializeField] Button avatarButton;
        [SerializeField] RectTransform bubble;
        [SerializeField] Text speechText;
        [Header("Khách: mỗi màn một người của vùng đó (cố định theo số màn)")]
        [SerializeField] Image avatar;
        [SerializeField] Sprite[] hanoiFaces;
        [SerializeField] Sprite[] seoulFaces;
        [Tooltip("Tuỳ chọn: tên + ảnh món phụ")]
        [SerializeField] FoodCatalog catalog;

        LevelController controller;
        string last, levelLine;

        public void Init(LevelController c)
        {
            controller = c;
            avatarButton.onClick.AddListener(() => Say(Loc.Current != Lang.Vi ? ForeignPick("order") : PickTalk()));
            EventBus<LevelLoadedEvent>.Subscribe(OnLoaded);
            EventBus<LevelWonEvent>.Subscribe(OnWon);
            EventBus<MenuClosedEvent>.Subscribe(OnMenuClosed);
            EventBus<LanguageChangedEvent>.Subscribe(OnLanguage);
            speechText.text = "";
        }

        void OnDestroy()
        {
            EventBus<LevelLoadedEvent>.Unsubscribe(OnLoaded);
            EventBus<LevelWonEvent>.Unsubscribe(OnWon);
            EventBus<MenuClosedEvent>.Unsubscribe(OnMenuClosed);
            EventBus<LanguageChangedEvent>.Unsubscribe(OnLanguage);
        }

        void OnDisable() => this.KillTweens(true);

        void OnLoaded(LevelLoadedEvent e)
        {
            var L = controller.Level;
            var faces = L != null && L.region == FoodRegion.Seoul ? seoulFaces : hanoiFaces;
            if (avatar != null && faces != null && faces.Length > 0)
                avatar.sprite = faces[(e.index * 7 + 3) % faces.Length];   // trải đều, chơi lại vẫn gặp đúng người cũ

            levelLine = OrderLine();
            Say(levelLine);
        }

        /// <summary>Câu gọi món của màn. Tiếng Việt: kho câu trong CustomerLines; Hàn / Anh: bảng dịch (order.*).</summary>
        string OrderLine()
        {
            var L = controller.Level;
            var dish = L != null ? L.unlocksDish : null;
            var side = Side(L);
            if (Loc.Current != Lang.Vi)
            {
                if (dish != null) return Loc.Or(LevelKey("order"), ForeignPick("order"));
                if (side != null && Random.value < .7f)
                {
                    var n = side.Name();
                    if (Loc.Current == Lang.En && n.Length > 1) n = char.ToLowerInvariant(n[0]) + n.Substring(1);   // giữa câu tiếng Anh viết thường
                    return Loc.T($"order.side.{RegionKey}", n);
                }
                return ForeignPick("order");
            }
            if (L != null && !string.IsNullOrWhiteSpace(L.customerLine)) return L.customerLine;
            if (dish != null) return Format(lines.dishOrder, dish.Name());
            if (side != null && Random.value < .7f) return Format(lines.sideOrder, side.Name());
            return PickTalk();
        }

        string RegionKey => controller.Level != null && controller.Level.region == FoodRegion.Seoul ? "seoul" : "hanoi";

        string LevelKey(string kind) => $"{kind}.level{controller.Index + 1:00}.line";

        /// <summary>Câu ngẫu nhiên trong order.&lt;vùng&gt;.01, 02… (hoặc thanks.*) của bảng dịch, tránh lặp câu vừa nói.</summary>
        string ForeignPick(string kind)
        {
            var keys = new System.Collections.Generic.List<string>();
            for (int i = 1; i < 20; i++)
            {
                var k = $"{kind}.{RegionKey}.{i:00}";
                if (Loc.Has(k)) keys.Add(k); else break;
            }
            if (keys.Count == 0) return null;
            for (int tries = 0; tries < 4; tries++)
            {
                var s = Loc.T(keys[Random.Range(0, keys.Count)]);
                if (s != last || keys.Count == 1) return s;
            }
            return Loc.T(keys[0]);
        }

        // đổi ngôn ngữ giữa màn: khách nói lại câu gọi món bằng ngôn ngữ mới
        void OnLanguage(LanguageChangedEvent e)
        {
            if (controller == null || controller.Level == null || controller.Won) return;
            levelLine = OrderLine();
            Say(levelLine);
        }

        // màn được nạp sau lưng Main Menu: khi menu đóng, bong bóng nảy lại để người chơi để ý
        void OnMenuClosed(MenuClosedEvent e) => Say(levelLine);

        void OnWon(LevelWonEvent e)
        {
            var dish = controller.Level != null ? controller.Level.unlocksDish : null;
            if (Loc.Current != Lang.Vi) Say(dish != null ? Loc.Or(LevelKey("thanks"), ForeignPick("thanks")) : ForeignPick("thanks"));
            else Say(dish != null ? Format(lines.dishWin, dish.Name()) : Pick(lines.win));
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
