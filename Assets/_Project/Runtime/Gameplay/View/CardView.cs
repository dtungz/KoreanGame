using DG.Tweening;
using KoreanGame.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KoreanGame.Gameplay
{
    /// <summary>Một lá thẻ trong tay bài: khung theo nhóm, hình đường cắt hoặc biểu tượng chức năng, tên, dấu xoay.</summary>
    public class CardView : MonoBehaviour
    {
        [SerializeField] RectTransform body;
        [SerializeField] CanvasGroup group;
        [SerializeField] Image frame;
        [SerializeField] CardArtGraphic art;
        [SerializeField] Image icon;
        [SerializeField] Text nameText;
        [SerializeField] GameObject rotBadge;
        [SerializeField] Sprite lineFrame, moldFrame, diagFrame, fnFrame;

        public int HandIndex { get; private set; } = -1;
        public RectTransform Rect => (RectTransform)transform;

        public void Bind(HandCard c, int handIndex, bool entering)
        {
            HandIndex = handIndex;
            var d = c.def;
            frame.sprite = d.family == CardFamily.Mold ? moldFrame : d.family == CardFamily.Diag ? diagFrame
                : d.family == CardFamily.Line ? lineFrame : fnFrame;
            nameText.text = d.displayName;
            rotBadge.SetActive(d.CanRotate);

            bool showIcon = d.icon != null;
            icon.gameObject.SetActive(showIcon);
            art.gameObject.SetActive(!d.IsFunction);
            art.Set(d, c.rot);
            if (showIcon)
            {
                icon.sprite = d.icon;
                var t = icon.rectTransform;
                t.localEulerAngles = d.kind == CardKind.Flip && c.rot % 2 == 1 ? new Vector3(0, 0, -90) : Vector3.zero;
                t.localScale = d.kind == CardKind.Rot && c.rot % 2 == 1 ? new Vector3(-1, 1, 1) : Vector3.one;
                if (d.kind == CardKind.Glue) t.localEulerAngles = c.rot % 2 == 1 ? new Vector3(0, 0, 90) : Vector3.zero;
            }

            body.KillTweens(true);
            body.anchoredPosition = Vector2.zero;
            body.localScale = Vector3.one;
            group.alpha = 1;
            if (entering)
            {
                body.anchoredPosition = new Vector2(0, -40);
                body.localScale = Vector3.one * .85f;
                group.alpha = 0;
                DOTween.To(() => body.anchoredPosition, v => body.anchoredPosition = v, Vector2.zero, .32f).SetEase(Ease.OutCubic).Guard(body);
                body.DOScale(1, .32f).SetEase(Ease.OutCubic).Guard(body);
                DOTween.To(() => group.alpha, a => group.alpha = a, 1f, .32f).Guard(body);
            }
        }

        public void SetDragging(bool on) => group.alpha = on ? .25f : 1f;

        public CanvasGroup Group => group;

        public void Shake()
        {
            body.KillTweens(true);
            body.anchoredPosition = Vector2.zero;
            float x = 0;
            var seq = DOTween.Sequence().Guard(body);
            foreach (var to in new[] { -6f, 6f, -4f, 0f })
                seq.Append(DOTween.To(() => x, v => { x = v; body.anchoredPosition = new Vector2(v, 0); }, to, .075f));
        }

        // thẻ bị ẩn / huỷ giữa chừng: dừng tween và trả về trạng thái cuối
        void OnDisable() => body.KillTweens(true);
    }
}
