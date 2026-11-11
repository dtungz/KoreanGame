using DG.Tweening;
using KoreanGame.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KoreanGame.Gameplay
{
    /// <summary>
    /// Thẻ "Món mới trong hộp!": đầu màn có món phụ người chơi chưa gặp, hiện ảnh món to + tên (đè lên dải khách,
    /// không che thớt) và nháy ô món trong hộp. Tự ẩn sau ~2,6 giây hoặc khi chạm. Thắng màn thì món vào bộ sưu tập
    /// (LevelController), lần sau gặp lại không hiện nữa. Menu đang mở thì đợi menu đóng mới hiện.
    /// </summary>
    public class SideDishToast : MonoBehaviour
    {
        [SerializeField] FoodCatalog catalog;
        [SerializeField] GameObject root;
        [SerializeField] CanvasGroup group;
        [SerializeField] RectTransform panel;
        [SerializeField] Image icon;
        [SerializeField] Text nameText;
        [SerializeField] Button closeButton;

        LevelController controller;
        SideDish pending, showing;
        bool menuOpen;

        public void Init(LevelController c)
        {
            controller = c;
            closeButton.onClick.AddListener(Hide);
            root.SetActive(false);
            EventBus<LevelLoadedEvent>.Subscribe(OnLoaded);
            EventBus<MenuOpenedEvent>.Subscribe(OnMenuOpened);
            EventBus<MenuClosedEvent>.Subscribe(OnMenuClosed);
            EventBus<LevelWonEvent>.Subscribe(OnWon);
        }

        void OnDestroy()
        {
            EventBus<LevelLoadedEvent>.Unsubscribe(OnLoaded);
            EventBus<MenuOpenedEvent>.Unsubscribe(OnMenuOpened);
            EventBus<MenuClosedEvent>.Unsubscribe(OnMenuClosed);
            EventBus<LevelWonEvent>.Unsubscribe(OnWon);
        }

        void OnDisable() => this.KillTweens();

        // menu mở ngay sau khi nạp màn (lúc vào game): giữ món lại, hiện khi menu đóng
        void OnMenuOpened(MenuOpenedEvent e)
        {
            menuOpen = true;
            if (showing != null) pending = showing;
            Hide();
        }

        void OnMenuClosed(MenuClosedEvent e)
        {
            menuOpen = false;
            if (pending != null) Show(pending, .35f);
        }

        void OnWon(LevelWonEvent e) => Hide();

        void OnLoaded(LevelLoadedEvent e)
        {
            Hide();
            pending = null;
            var L = controller.Level;
            if (L == null || catalog == null) return;
            foreach (var f in L.tray.fixedFoods)
            {
                var s = catalog.FindSide(f.kind);
                if (s == null || s.sprite == null || CollectionSave.HasSide(s.id)) continue;
                pending = s;
                break;
            }
            if (pending != null && !menuOpen) Show(pending, .45f);
        }

        void Show(SideDish s, float delay)
        {
            pending = null;
            showing = s;
            icon.sprite = s.sprite;
            nameText.text = s.displayName;
            root.SetActive(true);
            group.alpha = 0;
            panel.localScale = Vector3.one * .9f;
            this.KillTweens();
            var seq = DOTween.Sequence().Guard(this).AppendInterval(delay);
            seq.AppendCallback(() => controller.HighlightFood(s.id));
            seq.Append(DOTween.To(() => group.alpha, a => group.alpha = a, 1f, .2f));
            seq.Join(panel.DOScale(1f, .3f).SetEase(Ease.OutBack));
            seq.Append(icon.rectTransform.DOPunchScale(Vector3.one * .15f, .4f, 6, .5f));
            seq.AppendInterval(2.2f);
            seq.Append(DOTween.To(() => group.alpha, a => group.alpha = a, 0f, .25f));
            seq.AppendCallback(() => { root.SetActive(false); showing = null; });
        }

        public void Hide()
        {
            this.KillTweens();
            showing = null;
            if (icon != null) icon.rectTransform.localScale = Vector3.one;
            if (root != null) root.SetActive(false);
        }
    }
}
