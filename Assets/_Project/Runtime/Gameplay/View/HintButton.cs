using System.Threading.Tasks;
using DG.Tweening;
using KoreanGame.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KoreanGame.Gameplay
{
    /// <summary>
    /// Nút Gợi ý (cùng hàng Hoàn tác / Làm lại nhưng tách sang bên phải): chỉ nước đi kế tiếp còn giữ được lời giải.
    /// - Cần dùng thẻ: vẽ đậm đường cắt (hoặc viền miếng cần biến hình) + thẻ nảy lên.
    /// - Chỉ còn xếp miếng: viền ô cần đặt + nháy miếng cơm đó.
    /// - Màn đã hết đường thắng: nút Hoàn tác và Làm lại nảy lên.
    /// Tìm gợi ý chạy ở luồng nền trên bản sao trạng thái (màn khó có thể mất vài giây): game không đứng hình,
    /// nút nhấp nháy trong lúc chờ; người chơi đi nước khác trước khi xong thì bỏ kết quả cũ.
    /// Gợi ý tự tắt khi người chơi đi nước kế tiếp (thớt vẽ lại).
    /// Mỗi màn 1 gợi ý miễn phí: chỉ tính khi thật sự chỉ ra nước đi (nhắc Hoàn tác / Làm lại khi kẹt thì không tính);
    /// Làm lại không được thêm; sang màn khác hoặc thắng rồi chơi lại thì có lượt mới.
    /// Hết lượt miễn phí: hiện nhãn giá, mỗi gợi ý thêm trừ GoldSave.HintPrice vàng (chỉ trừ khi chỉ ra nước đi); thiếu vàng thì nút rung.
    /// </summary>
    public class HintButton : MonoBehaviour
    {
        [SerializeField] Button button;
        [Tooltip("Tuỳ chọn: nhãn giá (đồng xu + số) hiện khi đã dùng lượt miễn phí của màn")]
        [SerializeField] GameObject priceTag;
        [Tooltip("Tuỳ chọn: icon bóng đèn, chuyển xám khi đã dùng lượt miễn phí của màn")]
        [SerializeField] Image icon;
        [SerializeField] Text priceText;

        LevelController controller;
        BoardView board;
        HandView hand;
        HudView hud;

        Task<Hint> job;
        int version, jobVersion;
        int usedLevel = -1;   // màn đã dùng gợi ý (-1 = còn lượt)

        bool Used => controller != null && usedLevel == controller.Index;

        public RectTransform Rect => (RectTransform)button.transform;

        public void Init(LevelController c, BoardView b, HandView h, HudView hudView)
        {
            controller = c; board = b; hand = h; hud = hudView;
            button.onClick.AddListener(Show);
            EventBus<BoardChangedEvent>.Subscribe(OnChanged);
            EventBus<LevelLoadedEvent>.Subscribe(OnLoaded);
            EventBus<LevelWonEvent>.Subscribe(OnWon);
        }

        void OnDestroy()
        {
            EventBus<BoardChangedEvent>.Unsubscribe(OnChanged);
            EventBus<LevelLoadedEvent>.Unsubscribe(OnLoaded);
            EventBus<LevelWonEvent>.Unsubscribe(OnWon);
        }

        // sang màn khác: có lượt mới (Làm lại cùng màn thì không)
        void OnLoaded(LevelLoadedEvent e)
        {
            if (e.index != usedLevel) usedLevel = -1;
            job = null;
            Refresh();
        }

        void OnWon(LevelWonEvent e) => usedLevel = -1;   // thắng rồi: lần chơi lại sau có lượt mới

        void Refresh()
        {
            button.interactable = controller.State != null && !controller.Won;
            if (priceTag != null) priceTag.SetActive(Used && !controller.Won);
            if (icon != null) icon.color = Used ? new Color(.6f, .58f, .56f, .75f) : Color.white;   // đã dùng lượt miễn phí: bóng đèn xám
            if (priceText != null) priceText.text = GoldSave.HintPrice.ToString();
        }

        void OnDisable() => this.KillTweens(true);

        void OnChanged(BoardChangedEvent e)
        {
            version++;   // thớt đổi: kết quả đang tìm (nếu có) không còn đúng
            Refresh();
        }

        void Show()
        {
            var s = controller.State;
            if (s == null || controller.Won || job != null) return;
            if (Used && GoldSave.Gold < GoldSave.HintPrice) { Shake(); return; }   // hết lượt miễn phí, thiếu vàng
            foreach (var h in s.hand) { var _ = h.def.Polys; }   // dựng cache đường cắt ở luồng chính trước khi tìm ở luồng nền
            var snapshot = s.Clone();
            jobVersion = version;
            job = Task.Run(() => HintFinder.Find(snapshot));
        }

        void Update()
        {
            if (job == null) return;
            if (!job.IsCompleted)
            {
                float k = .5f + .5f * Mathf.Sin(Time.unscaledTime * 10);
                button.image.color = Color.Lerp(Color.white, Palette.UiPeach, k);   // đang suy nghĩ
                return;
            }
            var t = job;
            job = null;
            button.image.color = Color.white;
            if (t.IsFaulted || jobVersion != version || controller.Won) return;
            Display(t.Result);
        }

        void Display(Hint h)
        {
            if (h.kind == HintKind.Card || h.kind == HintKind.Place)   // chỉ tính khi chỉ ra nước đi
            {
                if (!Used) usedLevel = controller.Index;
                else if (!GoldSave.Spend(GoldSave.HintPrice)) { Shake(); return; }
            }
            Refresh();
            AudioManager.PlayUi(h.kind == HintKind.Card || h.kind == HintKind.Place ? Sfx.Hint : Sfx.Bad);
            switch (h.kind)
            {
                case HintKind.Card:
                    if (h.res.target != null) board.ShowHint(null, h.res.target.tris);
                    else board.ShowHint(h.res.segs, null);
                    Punch(hand.SlotRect(h.slot));
                    break;
                case HintKind.Place:
                    board.ShowHint(null, h.target);
                    board.Flash(h.piece.tris, new Color(1f, .95f, .6f, .7f), false, .45f, 2);
                    break;
                default:   // hết đường thắng (hoặc không tìm ra): nhắc Hoàn tác / Làm lại
                    Punch(hud.UndoRect);
                    Punch(hud.RestartRect);
                    break;
            }
            Punch(Rect);
        }

        void Shake()
        {
            AudioManager.PlayUi(Sfx.Bad, .8f);
            Rect.DOKill(true);
            Rect.DOShakePosition(.35f, new Vector3(8, 0, 0), 20, 0).Guard(this);
        }

        void Punch(RectTransform rt)
        {
            if (rt == null) return;
            rt.DOKill(true);
            rt.DOPunchScale(Vector3.one * .18f, .45f, 6, .5f).Guard(this);
        }
    }
}
