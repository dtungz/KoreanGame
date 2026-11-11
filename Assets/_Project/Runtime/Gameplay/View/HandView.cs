using DG.Tweening;
using KoreanGame.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KoreanGame.Gameplay
{
    /// <summary>Tay bài 3 ô cố định + nhãn "+N thẻ chờ". Dùng thẻ ở ô k thì thẻ kế tiếp trượt vào ô k.</summary>
    public class HandView : MonoBehaviour
    {
        [SerializeField] CardView[] cards = new CardView[BoardState.SlotCount];
        [SerializeField] GameObject[] emptySlots = new GameObject[BoardState.SlotCount];
        [SerializeField] GameObject queueBadge;
        [SerializeField] Text queueText;
        [SerializeField] Canvas canvas;
        [SerializeField] RectTransform ghostLayer;   // phủ kín canvas, gốc toạ độ cho bóng thẻ
        [SerializeField] RectTransform ghostRoot;
        [SerializeField] CardView ghost;

        public void Render(BoardState s, int enteringSlot = -1)
        {
            bool any = s.CardsLeft > 0;
            for (int k = 0; k < cards.Length; k++)
            {
                int i = s.slots[k];
                bool has = any && i >= 0;
                cards[k].gameObject.SetActive(has);
                emptySlots[k].SetActive(!has);
                if (has) cards[k].Bind(s.hand[i], i, k == enteringSlot);
            }
            queueBadge.SetActive(s.QueuedCount > 0);
            queueText.text = Loc.T("ui.queued_cards", s.QueuedCount);
        }

        /// <summary>Thẻ dưới điểm chạm (trả về chỉ số trong hand, -1 nếu không có).</summary>
        public int HitTest(Vector2 screen)
        {
            var uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            foreach (var c in cards)
                if (c.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(c.Rect, screen, uiCam)) return c.HandIndex;
            return -1;
        }

        /// <summary>Ô thẻ thứ k (tutorial trỏ tay vào). null nếu ô trống.</summary>
        public RectTransform SlotRect(int k) => k >= 0 && k < cards.Length && cards[k].gameObject.activeInHierarchy ? cards[k].Rect : null;

        /// <summary>Nhãn "+N thẻ chờ" (null nếu đang ẩn).</summary>
        public RectTransform QueueRect => queueBadge.activeInHierarchy ? (RectTransform)queueBadge.transform : null;

        CardView Find(int handIndex)
        {
            foreach (var c in cards) if (c.gameObject.activeSelf && c.HandIndex == handIndex) return c;
            return null;
        }

        public void SetDragging(int handIndex, bool on) => Find(handIndex)?.SetDragging(on);
        public void Shake(int handIndex) => Find(handIndex)?.Shake();

        // ======================= Bóng thẻ bay theo ngón tay =======================

        static readonly Vector2 GhostOffset = new Vector2(46, 64);   // lệch lên-phải để không che đường cắt
        const float GhostScale = .62f;

        public void BeginGhost(HandCard card, int handIndex, Vector2 screen)
        {
            ghostRoot.KillTweens(true);
            ghost.gameObject.SetActive(true);
            ghost.Bind(card, handIndex, false);
            ghost.Group.alpha = .92f;
            ghostRoot.localScale = Vector3.one * GhostScale;
            ghostRoot.gameObject.SetActive(true);
            MoveGhost(screen);
        }

        public void MoveGhost(Vector2 screen)
        {
            if (!ghostRoot.gameObject.activeSelf) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(ghostLayer, screen, null, out var local))
                ghostRoot.anchoredPosition = local + GhostOffset;
        }

        /// <summary>used = thẻ đã dùng: "đập" xuống rồi tan; ngược lại bay về ô của nó.</summary>
        public void EndGhost(bool used, int handIndex)
        {
            if (!ghostRoot.gameObject.activeSelf) return;
            ghostRoot.KillTweens(true);
            var g = ghost.Group;
            var s = DOTween.Sequence().Guard(ghostRoot);
            if (used)
            {
                s.Append(ghostRoot.DOScale(GhostScale * 1.25f, .08f).SetEase(Ease.OutQuad));
                s.Append(ghostRoot.DOScale(GhostScale * .7f, .12f).SetEase(Ease.InQuad));
                s.Join(DOTween.To(() => g.alpha, a => { if (g != null) g.alpha = a; }, 0f, .12f));
            }
            else
            {
                var slot = Find(handIndex);
                var target = ghostRoot.anchoredPosition;
                if (slot != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(ghostLayer,
                        RectTransformUtility.WorldToScreenPoint(null, slot.Rect.TransformPoint(slot.Rect.rect.center)), null, out var local))
                    target = local;
                var from = ghostRoot.anchoredPosition;
                s.Append(DOTween.To(() => from, v => { from = v; if (ghostRoot != null) ghostRoot.anchoredPosition = v; }, target, .18f).SetEase(Ease.InOutQuad));
                s.Join(ghostRoot.DOScale(1f, .18f));
                s.Join(DOTween.To(() => g.alpha, a => { if (g != null) g.alpha = a; }, .3f, .18f));
            }
            s.OnComplete(HideGhost);
        }

        public void HideGhost()
        {
            if (ghostRoot == null) return;
            ghostRoot.KillTweens();
            ghostRoot.gameObject.SetActive(false);
        }

        void OnDisable() => HideGhost();
    }
}
