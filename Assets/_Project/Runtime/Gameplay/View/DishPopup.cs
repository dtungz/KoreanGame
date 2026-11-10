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
            art.sprite = dish.art;
            art.enabled = dish.art != null;
            nameText.text = dish.displayName;
            regionText.text = dish.region == FoodRegion.Seoul ? "Seoul" : dish.region == FoodRegion.HaNoi ? "Hà Nội" : "";
            introText.text = dish.intro;

            root.SetActive(true);
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

        public void Hide()
        {
            this.KillTweens();
            root.SetActive(false);
        }
    }
}
