using DG.Tweening;
using KoreanGame.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KoreanGame.Gameplay
{
    /// <summary>
    /// Popup "Món mới!": thắng lần đầu màn có món đặc trưng thì giới thiệu món (ảnh hộp, tên, vùng, lời giới thiệu).
    /// Nằm trên popup Hoàn thành; bấm nút thì đóng, popup Hoàn thành bên dưới hiện ra. Chơi lại màn cũ không hiện nữa.
    /// </summary>
    public class DishPopup : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] CanvasGroup group;
        [SerializeField] RectTransform panel;
        [SerializeField] Image art;
        [SerializeField] Text nameText;
        [SerializeField] Text regionText;
        [SerializeField] Text introText;
        [SerializeField] Button okButton;
        [Tooltip("Tuỳ chọn: tiêu đề + chữ trên nút (chế độ xem thông tin đổi chữ)")]
        [SerializeField] Text titleText;
        [SerializeField] Text okLabel;

        LevelController controller;

        public void Init(LevelController c)
        {
            controller = c;
            okButton.onClick.AddListener(Hide);
            root.SetActive(false);
            EventBus<LevelWonEvent>.Subscribe(OnWon);
            EventBus<LevelLoadedEvent>.Subscribe(OnLoaded);
        }

        void OnDestroy()
        {
            EventBus<LevelWonEvent>.Unsubscribe(OnWon);
            EventBus<LevelLoadedEvent>.Unsubscribe(OnLoaded);
        }

        void OnDisable() => this.KillTweens();

        void OnLoaded(LevelLoadedEvent e) => Hide();

        void OnWon(LevelWonEvent e)
        {
            var dish = controller.Level.unlocksDish;
            if (!e.newDish || dish == null) return;
            regionText.transform.parent.gameObject.SetActive(true);
            art.color = Color.white;
            art.sprite = dish.art;
            art.enabled = dish.art != null;
            nameText.text = dish.Name();
            regionText.text = LocNames.Region(dish.region);
            introText.text = dish.Intro();

            root.SetActive(true);
            if (titleText != null) titleText.text = Loc.T("dish_popup.title");   // sau xem món (bộ sưu tập): về chữ Món mới
            if (okLabel != null) okLabel.text = Loc.T("dish_popup.ok");
            group.alpha = 0;
            group.blocksRaycasts = true;
            panel.localScale = Vector3.one * .85f;
            art.rectTransform.localEulerAngles = Vector3.zero;
            this.KillTweens();
            var s = DOTween.Sequence().Guard(this).AppendInterval(e.popupDelay > 0 ? e.popupDelay : 1f);   // như popup Hoàn thành: đợi ảnh món + nắp hộp
            s.Append(DOTween.To(() => group.alpha, a => group.alpha = a, 1f, .2f));
            s.Join(panel.DOScale(1f, .3f).SetEase(Ease.OutBack));
            s.Append(art.rectTransform.DOPunchRotation(new Vector3(0, 0, 6), .45f, 6, .5f));
        }

        /// <summary>
        /// Bộ sưu tập: xem thông tin một món (ảnh to, tên, vùng, mô tả chữ to). dim: món chưa mở (ảnh mờ / ổ khoá).
        /// region rỗng thì ẩn chip vùng.
        /// </summary>
        public void ShowInfo(string title, Sprite sprite, bool dim, string name, string region, string body, string button)
        {
            art.sprite = sprite;
            art.enabled = sprite != null;
            art.color = dim ? new Color(Palette.UiBrown.r, Palette.UiBrown.g, Palette.UiBrown.b, .45f) : Color.white;
            nameText.text = name;
            regionText.text = region;
            regionText.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(region));
            introText.text = body;

            root.SetActive(true);
            // sau khi bật: LocText của tiêu đề / nút tự điền chữ mặc định lúc OnEnable, đặt lại cho chế độ xem món
            if (titleText != null) titleText.text = title;
            if (okLabel != null) okLabel.text = button;
            group.blocksRaycasts = true;
            art.rectTransform.localEulerAngles = Vector3.zero;
            this.KillTweens();
            group.alpha = 0;
            panel.localScale = Vector3.one * .9f;
            DOTween.To(() => group.alpha, a => group.alpha = a, 1f, .15f).Guard(this);
            panel.DOScale(1f, .22f).SetEase(Ease.OutBack).Guard(this);
            AudioManager.PlayUi(Sfx.PopupOpen);
        }

        public void Hide()
        {
            this.KillTweens();
            root.SetActive(false);
        }
    }
}
