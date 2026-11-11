using System.Collections.Generic;
using KoreanGame.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KoreanGame.Gameplay
{
    /// <summary>
    /// Tutorial giới thiệu mechanic: vào màn có TutorialDef chưa xem thì chạy từng bước. Mỗi bước: làm mờ phần còn lại
    /// (lỗ sáng quanh chỗ cần thao tác), khung sáng ở chỗ kéo tới, tay chỉ lặp động tác mẫu, 1 câu Việt + Hàn.
    /// Overlay KHÔNG nhận chạm (người chơi thao tác bình thường); chỉ nút Bỏ qua nhận chạm.
    /// Qua bước bằng event có sẵn (dùng thẻ, xoay thẻ, đặt miếng, thắng…), kiểm tra theo loại hành động chứ không theo toạ độ,
    /// nên làm khác mẫu nhưng đúng loại vẫn qua. Thả hỏng: tay làm lại mẫu; hỏng 2 lần: nháy nút Hoàn tác.
    /// Chỉ đánh dấu đã xem khi xong hoặc bỏ qua; thoát / chơi lại giữa chừng thì lần sau chạy lại từ đầu.
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        [SerializeField] TutorialDef[] tutorials;
        [SerializeField] RectTransform overlay;        // phủ kín canvas HUD, gốc toạ độ cho vùng mờ / khung / tay
        [SerializeField] RectTransform[] dims = new RectTransform[4];
        [SerializeField] RectTransform frameA;
        [SerializeField] RectTransform frameB;
        [SerializeField] RectTransform hand;           // pivot ở đầu ngón tay
        [SerializeField] GameObject tip;               // khung câu chữ (đè lên dải khách)
        [SerializeField] Text tipVi;
        [SerializeField] Text tipKo;
        [SerializeField] Button skipButton;
        [Tooltip("Người dẫn đường: đầu bếp Việt ở Hà Nội, đầu bếp Hàn ở Seoul")]
        [SerializeField] Image guide;
        [SerializeField] Sprite guideHanoi, guideSeoul;

        LevelController controller;
        BoardView board;
        HandView handView;
        HudView hud;

        TutorialDef current;
        int step;
        float stepTime, undoFlashUntil;
        int rejects;
        bool menuOpen, pendingStart;
        static readonly Color DimColor = new Color(.24f, .14f, .09f, .5f);

        public bool Running => current != null;

        public void Init(LevelController c, BoardView b, HandView h, HudView hudView)
        {
            controller = c; board = b; handView = h; hud = hudView;
            skipButton.onClick.AddListener(Skip);
            foreach (var d in dims) d.GetComponent<Image>().color = DimColor;
            hand.anchorMin = hand.anchorMax = new Vector2(.5f, .5f);   // toạ độ tính từ tâm overlay (giữ pivot đầu ngón tay)
            Hide();
            EventBus<LevelLoadedEvent>.Subscribe(OnLoaded);
            EventBus<MenuOpenedEvent>.Subscribe(OnMenuOpened);
            EventBus<MenuClosedEvent>.Subscribe(OnMenuClosed);
            EventBus<CardUsedEvent>.Subscribe(OnCardUsed);
            EventBus<CardRotatedEvent>.Subscribe(OnCardRotated);
            EventBus<PiecePlacedEvent>.Subscribe(OnPiecePlaced);
            EventBus<LevelWonEvent>.Subscribe(OnWon);
            EventBus<MoveRejectedEvent>.Subscribe(OnRejected);
        }

        void OnDestroy()
        {
            EventBus<LevelLoadedEvent>.Unsubscribe(OnLoaded);
            EventBus<MenuOpenedEvent>.Unsubscribe(OnMenuOpened);
            EventBus<MenuClosedEvent>.Unsubscribe(OnMenuClosed);
            EventBus<CardUsedEvent>.Unsubscribe(OnCardUsed);
            EventBus<CardRotatedEvent>.Unsubscribe(OnCardRotated);
            EventBus<PiecePlacedEvent>.Unsubscribe(OnPiecePlaced);
            EventBus<LevelWonEvent>.Unsubscribe(OnWon);
            EventBus<MoveRejectedEvent>.Unsubscribe(OnRejected);
        }

        /// <summary>Danh sách id (cho nút "Xem lại hướng dẫn").</summary>
        public IEnumerable<string> Ids { get { foreach (var t in tutorials) if (t != null) yield return t.id; } }

        // ======================= Bắt đầu / kết thúc =======================

        void OnLoaded(LevelLoadedEvent e)
        {
            current = null;
            Hide();
            pendingStart = false;
            foreach (var t in tutorials)
                if (t != null && t.levelIndex == e.index && t.steps.Count > 0 && !TutorialSave.IsDone(t.id))
                {
                    current = t;
                    break;
                }
            if (current == null) return;
            step = 0;
            rejects = 0;
            if (menuOpen) pendingStart = true;
            else Begin();
        }

        void OnMenuOpened(MenuOpenedEvent e)
        {
            menuOpen = true;
            if (current != null) { pendingStart = true; Hide(); }   // menu che màn chơi: chạy lại từ đầu khi menu đóng
        }

        void OnMenuClosed(MenuClosedEvent e)
        {
            menuOpen = false;
            if (current != null && pendingStart) { pendingStart = false; step = 0; Begin(); }
        }

        void Begin()
        {
            stepTime = Time.time;
            ShowStep();
        }

        void Finish()
        {
            if (current != null) TutorialSave.MarkDone(current.id);
            current = null;
            Hide();
        }

        void Skip() => Finish();

        void Next()
        {
            step++;
            rejects = 0;
            if (current == null || step >= current.steps.Count) { Finish(); return; }
            stepTime = Time.time;
            ShowStep();
        }

        TutorialStep Step => current != null && step < current.steps.Count ? current.steps[step] : null;

        void ShowStep()
        {
            var s = Step;
            if (s == null) return;
            tip.SetActive(true);
            var L = controller.Level;
            guide.sprite = L != null && L.region == FoodRegion.Seoul ? guideSeoul : guideHanoi;
            tipVi.text = s.vi;
            tipKo.text = s.ko;
            tipKo.gameObject.SetActive(!string.IsNullOrEmpty(s.ko));
            overlay.gameObject.SetActive(true);
        }

        void Hide()
        {
            if (overlay != null) overlay.gameObject.SetActive(false);
            if (tip != null) tip.SetActive(false);
        }

        // ======================= Qua bước =======================

        void OnCardUsed(CardUsedEvent e)
        {
            var s = Step;
            if (s != null && s.done == TutDone.CardUsed && !Waiting(s) && (s.anyEffect || s.effect == e.effect)) Next();
        }

        void OnCardRotated(CardRotatedEvent e)
        {
            if (Step?.done == TutDone.CardRotated) Next();
        }

        void OnPiecePlaced(PiecePlacedEvent e)
        {
            var s = Step;
            if (s == null) return;
            if (s.done == TutDone.PiecePlaced || (s.done == TutDone.PieceIntoTray && e.intoTray)) Next();
        }

        void OnWon(LevelWonEvent e)
        {
            if (current != null) Finish();   // thắng màn = đã làm xong mechanic, dù còn bước nào
        }

        void OnRejected(MoveRejectedEvent e)
        {
            if (current == null) return;
            stepTime = Time.time;   // tay làm lại động tác mẫu từ đầu
            if (++rejects >= 2) undoFlashUntil = Time.time + 2.5f;
        }

        bool Waiting(TutorialStep s) => s.waitForCard && FindSlot(s.cardKind) < 0;

        // ======================= Vẽ mỗi khung hình =======================

        void LateUpdate()
        {
            var s = Step;
            if (s == null || menuOpen || !overlay.gameObject.activeSelf) return;
            if (s.done == TutDone.Timer && Time.time - stepTime >= s.timer) { Next(); return; }

            bool wait = Waiting(s);
            tip.SetActive(!wait);
            Rect a = default, b = default, u = default;
            bool hasA = !wait && TryTarget(s.target, s, out a);
            bool hasB = !wait && s.gesture == TutGesture.Drag && TryTarget(s.dragTo, s, out b);

            // vùng mờ có lỗ sáng quanh chỗ cần thao tác
            var full = overlay.rect;
            var hole = hasA ? Pad(a, 10) : new Rect(full.center, Vector2.zero);
            SetDims(full, hole, hasA);
            Place(frameA, hasA, Pad(a, 6));
            bool undo = Time.time < undoFlashUntil && TryTarget(TutTarget.Undo, s, out u);
            if (undo) Place(frameB, true, Pad(u, 6));
            else Place(frameB, hasB, Pad(b, 6));
            float pulse = 1 + .05f * Mathf.Sin(Time.time * 6);
            frameA.localScale = frameB.localScale = new Vector3(pulse, pulse, 1);

            // tay chỉ: chạm (ấn xuống, nảy) hoặc kéo A → B rồi mờ, lặp lại
            hand.gameObject.SetActive(hasA && s.gesture != TutGesture.None);
            if (!hand.gameObject.activeSelf) return;
            var img = hand.GetComponent<Image>();
            float t = (Time.time - stepTime) % 1.6f;
            if (s.gesture == TutGesture.Tap || !hasB)
            {
                float press = t < .25f ? t / .25f : t < .5f ? 1 - (t - .25f) / .25f : 0;
                hand.anchoredPosition = ToAnchored(a.center) + new Vector2(0, -6 * press);
                hand.localScale = Vector3.one * (1 - .12f * press);
                img.color = Color.white;
            }
            else
            {
                float k = Mathf.Clamp01((t - .2f) / 1.0f);
                k = k * k * (3 - 2 * k);
                hand.anchoredPosition = ToAnchored(Vector2.Lerp(a.center, b.center, k));
                hand.localScale = Vector3.one * (t < .2f ? .9f : 1f);
                img.color = new Color(1, 1, 1, t < 1.25f ? 1 : 1 - (t - 1.25f) / .35f);
            }
        }

        static Rect Pad(Rect r, float p) => new Rect(r.xMin - p, r.yMin - p, r.width + 2 * p, r.height + 2 * p);

        Vector2 ToAnchored(Vector2 local) => local;   // overlay pivot giữa: toạ độ local = anchoredPosition của con neo giữa

        void SetDims(Rect full, Rect hole, bool on)
        {
            // 4 khối tối: trên, dưới, trái, phải của lỗ sáng
            SetRect(dims[0], on, new Rect(full.xMin, hole.yMax, full.width, full.yMax - hole.yMax));
            SetRect(dims[1], on, new Rect(full.xMin, full.yMin, full.width, hole.yMin - full.yMin));
            SetRect(dims[2], on, new Rect(full.xMin, hole.yMin, hole.xMin - full.xMin, hole.height));
            SetRect(dims[3], on, new Rect(hole.xMax, hole.yMin, full.xMax - hole.xMax, hole.height));
        }

        static void SetRect(RectTransform rt, bool on, Rect r)
        {
            rt.gameObject.SetActive(on && r.width > 0 && r.height > 0);
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = r.min;
            rt.sizeDelta = r.size;
        }

        static void Place(RectTransform rt, bool on, Rect r)
        {
            rt.gameObject.SetActive(on);
            if (!on) return;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = r.center;
            rt.sizeDelta = r.size;
        }

        // ======================= Tìm chỗ trỏ (toạ độ local của overlay) =======================

        bool TryTarget(TutTarget t, TutorialStep s, out Rect r)
        {
            r = default;
            var st = controller.State;
            if (st == null) return false;
            switch (t)
            {
                case TutTarget.Card:
                    return UiRect(handView.SlotRect(s.byKind ? FindSlot(s.cardKind) : s.slot), out r);
                case TutTarget.QueueChip:
                    return UiRect(handView.QueueRect, out r);
                case TutTarget.Undo:
                    return UiRect(hud.UndoRect, out r);
                case TutTarget.PieceLargestOutside:
                case TutTarget.PieceSecondOutside:
                case TutTarget.PieceSmallestOutside:
                {
                    var outside = st.pieces.FindAll(p => !p.tris.TrueForAll(st.tray.Contains));
                    if (outside.Count == 0) return false;
                    outside.Sort((p, q) => q.tris.Count.CompareTo(p.tris.Count));
                    int i = t == TutTarget.PieceLargestOutside ? 0 : t == TutTarget.PieceSecondOutside ? 1 : outside.Count - 1;
                    return i < outside.Count && BoardRect(outside[i].tris, out r);
                }
                case TutTarget.TrayEmpty:
                {
                    var occ = st.Occupied();
                    var empty = st.tray.Need().FindAll(q => !occ.Contains(q));
                    return BoardRect(empty.Count > 0 ? empty : st.tray.tris, out r);
                }
                case TutTarget.Wall:
                {
                    if (st.tray.walls.Count == 0) return false;
                    var w = st.tray.walls[0];
                    return Corners(new Vector2(Mathf.Min(w.x1, w.x2) - .15f, Mathf.Min(w.y1, w.y2)), new Vector2(Mathf.Max(w.x1, w.x2) + .15f, Mathf.Max(w.y1, w.y2)), out r);
                }
                case TutTarget.FixedFood:
                {
                    if (st.tray.fixedFoods.Count == 0) return false;
                    return BoardRect(Geometry.Full(st.tray.fixedFoods[0].cells), out r);
                }
            }
            return false;
        }

        int FindSlot(CardKind kind)
        {
            var st = controller.State;
            if (st == null) return -1;
            for (int k = 0; k < st.slots.Length; k++)
            {
                int i = st.slots[k];
                if (i >= 0 && i < st.hand.Count && !st.hand[i].used && st.hand[i].def.kind == kind) return k;
            }
            return -1;
        }

        bool BoardRect(List<Tri> tris, out Rect r)
        {
            var bb = Geometry.BBox(tris);
            return Corners(new Vector2(bb.minx, bb.miny), new Vector2(bb.maxx + 1, bb.maxy + 1), out r);
        }

        bool Corners(Vector2 boardMin, Vector2 boardMax, out Rect r)
        {
            r = default;
            if (!ScreenToLocal(board.BoardToScreen(boardMin), out var p) || !ScreenToLocal(board.BoardToScreen(boardMax), out var q)) return false;
            r = Rect.MinMaxRect(Mathf.Min(p.x, q.x), Mathf.Min(p.y, q.y), Mathf.Max(p.x, q.x), Mathf.Max(p.y, q.y));
            return true;
        }

        bool UiRect(RectTransform rt, out Rect r)
        {
            r = default;
            if (rt == null) return false;
            var c = new Vector3[4];
            rt.GetWorldCorners(c);   // canvas Screen Space Overlay: toạ độ thế giới = toạ độ màn hình
            if (!ScreenToLocal(c[0], out var p) || !ScreenToLocal(c[2], out var q)) return false;
            r = Rect.MinMaxRect(p.x, p.y, q.x, q.y);
            return true;
        }

        bool ScreenToLocal(Vector2 screen, out Vector2 local) =>
            RectTransformUtility.ScreenPointToLocalPointInRectangle(overlay, screen, null, out local);
    }
}
