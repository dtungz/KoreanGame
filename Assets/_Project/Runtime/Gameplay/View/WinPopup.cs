using DG.Tweening;
using KoreanGame.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KoreanGame.Gameplay
{
    /// <summary>Popup "Hoàn thành!" ở nửa dưới màn hình để vẫn thấy hộp cơm đã đầy.</summary>
    public class WinPopup : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] CanvasGroup group;
        [SerializeField] RectTransform panel;
        [SerializeField] Text infoText;
        [SerializeField] Text nextLabel;
        [SerializeField] Button nextButton;

        LevelController controller;

        public void Init(LevelController c)
        {
            controller = c;
            nextButton.onClick.AddListener(() => { Hide(); controller.Next(); });
            root.SetActive(false);
            EventBus<LevelWonEvent>.Subscribe(OnWon);
            EventBus<LevelLoadedEvent>.Subscribe(OnLoaded);
        }

        void OnDestroy()
        {
            EventBus<LevelWonEvent>.Unsubscribe(OnWon);
            EventBus<LevelLoadedEvent>.Unsubscribe(OnLoaded);
        }

        void OnLoaded(LevelLoadedEvent e) => Hide();

        void OnDisable() => this.KillTweens();

        void OnWon(LevelWonEvent e)
        {
            infoText.text = e.gold > 0 ? Loc.T("win.info_gold", e.index + 1, e.gold) : Loc.T("win.info", e.index + 1);
            nextLabel.text = Loc.T(e.isLast ? "win.restart_all" : "win.next");
            root.SetActive(true);
            group.alpha = 0;
            panel.localScale = Vector3.one * .9f;
            this.KillTweens();
            var s = DOTween.Sequence().Guard(this).AppendInterval(e.popupDelay > 0 ? e.popupDelay : 1f);   // đợi nắp hộp đóng xong (BoardView.PlayWin)
            s.Append(DOTween.To(() => group.alpha, a => group.alpha = a, 1f, .2f));
            s.Join(panel.DOScale(1f, .25f).SetEase(Ease.OutBack));
        }

        public void Hide()
        {
            this.KillTweens();
            root.SetActive(false);
        }
    }
}
