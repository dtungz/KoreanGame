using System.Collections.Generic;
using KoreanGame.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KoreanGame.Gameplay
{
    /// <summary>
    /// Tutorial giới thiệu mechanic: vào màn có TutorialDef chưa xem thì chạy từng bước. Mỗi bước: làm mờ phần còn lại,
    /// chừa sáng khung câu chữ + chỗ cần kéo + chỗ thả; tay chỉ lặp động tác mẫu; 1 câu Việt + Hàn.
    /// Trong lúc tutorial chạy, nước đi làm màn hết lời giải bị huỷ (LevelController.MoveFilter) và đường cắt đúng được vẽ đậm,
    /// nên người chơi không thể đi sai. Overlay không nhận chạm; chỉ nút Bỏ qua nhận chạm.
    /// Qua bước bằng event có sẵn (dùng thẻ, xoay thẻ, đặt miếng, thắng…). Thả hỏng: tay làm lại mẫu; hỏng 2 lần: nháy Hoàn tác.
    /// Chỉ đánh dấu đã xem khi xong hoặc bỏ qua; thoát / chơi lại giữa chừng thì lần sau chạy lại từ đầu.
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        [SerializeField] TutorialDef[] tutorials;
        [SerializeField] RectTransform overlay;        // phủ kín canvas HUD, gốc toạ độ cho vùng mờ / khung / tay
        [SerializeField] DimGraphic dim;
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
        [Tooltip("Tuỳ chọn: nút Gợi ý (bước giới thiệu nút)")]
        [SerializeField] RectTransform hintButton;

        LevelController controller;
        BoardView board;
        HandView handView;
        HudView hud;

        TutorialDef current;
        int step;
        float stepTime, undoFlashUntil;
        int rejects;
        bool menuOpen, pendingStart;
        Hint cardHint, placeHint;      // nước đúng của bước hiện tại (tính lại mỗi lần thớt đổi)
        bool rotatePrompt;             // thẻ đang sai hướng: nhắc chạm thẻ để xoay (không thể cắt đúng ở hướng này)
        readonly List<Rect> holes = new List<Rect>();
        static readonly Color DimColor = new Color(.24f, .14f, .09f, .55f);

        public bool Running => current != null;

        public void Init(LevelController c, BoardView b, HandView h, HudView hudView)
        {
            controller = c; board = b; handView = h; hud = hudView;
            skipButton.onClick.AddListener(Skip);
            dim.color = DimColor;
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
            EventBus<BoardChangedEvent>.Subscribe(OnBoardChanged);
            EventBus<LanguageChangedEvent>.Subscribe(OnLanguage);
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
            EventBus<BoardChangedEvent>.Unsubscribe(OnBoardChanged);
            EventBus<LanguageChangedEvent>.Unsubscribe(OnLanguage);
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
            // trong tutorial: nước đi làm màn hết lời giải bị huỷ, người chơi không thể kẹt
            controller.MoveFilter = s => LevelSolver.Solvable(s);
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
            tipVi.text = TipText(s, false);
            tipKo.gameObject.SetActive(false);   // một ngôn ngữ mỗi lúc (đổi trong Cài đặt)
            overlay.gameObject.SetActive(true);
            UpdateHints();
        }

        /// <summary>Chữ hướng dẫn theo ngôn ngữ đang chọn: bảng dịch theo key của bước; thiếu thì dùng chữ trong asset.</summary>
        static string TipText(TutorialStep s, bool rotatePrompt)
        {
            if (rotatePrompt) return Loc.T("tut.rotate_prompt");
            string fallback = Loc.Current == Lang.Ko && !string.IsNullOrEmpty(s.ko) ? s.ko : s.vi;
            return string.IsNullOrEmpty(s.key) ? fallback : Loc.Or(s.key, fallback);
        }

        void OnLanguage(LanguageChangedEvent e)
        {
            var s = Step;
            if (s != null && tip != null && tip.activeSelf) tipVi.text = TipText(s, rotatePrompt && s.done == TutDone.CardUsed);
        }

        void Hide()
        {
            if (controller != null) controller.MoveFilter = null;
            cardHint = placeHint = null;
            if (board != null) board.ClearHint();
            if (overlay != null) overlay.gameObject.SetActive(false);
            if (tip != null) tip.SetActive(false);
        }

        // ======================= Nước đúng: đường cắt đậm, chỗ đặt miếng =======================

        void OnBoardChanged(BoardChangedEvent e)
        {
            if (current != null && !menuOpen && overlay.gameObject.activeSelf) UpdateHints();
        }

        void UpdateHints()
        {
            cardHint = placeHint = null;
            rotatePrompt = false;
            var s = Step;
            var st = controller.State;
            if (s == null || st == null || controller.Won) return;
            if (s.target == TutTarget.Card && (s.done == TutDone.CardUsed || s.done == TutDone.CardRotated) && !Waiting(s))
            {
                int k = s.byKind ? FindSlot(s.cardKind) : s.slot;
                var cur = HintFinder.ForSlot(st, k, true);
                if (s.done == TutDone.CardUsed) cardHint = cur;
                if (cur == null)
                {
                    var any = HintFinder.ForSlot(st, k, false);
                    rotatePrompt = any != null && any.needRotate;   // hướng này không cắt đúng được, hướng khác thì được
                }
            }
            else if (s.dragTo == TutTarget.TrayEmpty)
            {
                var h = HintFinder.Find(st);
                if (h.kind == HintKind.Place) placeHint = h;
            }
            bool prompt = rotatePrompt && s.done == TutDone.CardUsed;
            tipVi.text = TipText(s, prompt);
            if (rotatePrompt) board.ClearHint();
            else if (cardHint != null && cardHint.res.target != null) board.ShowHint(null, cardHint.res.target.tris);
            else if (cardHint != null) board.ShowHint(cardHint.res.segs, null);
            else if (placeHint != null) board.ShowHint(null, placeHint.target);
            else board.ClearHint();
        }

        // ======================= Qua bước =======================

        void OnCardUsed(CardUsedEvent e)
        {
            var s = Step;
            if (s != null && s.done == TutDone.CardUsed && !Waiting(s) && (s.anyEffect || s.effect == e.effect)) Next();
        }

        void OnCardRotated(CardRotatedEvent e)
        {
            var s = Step;
            if (s == null) return;
            stepTime = Time.time;
            if (s.done == TutDone.CardRotated)
            {
                // chỉ qua bước khi thẻ đã ở hướng cắt đúng được (bấm xoay quá thì ở lại, nhắc xoay tiếp)
                int k = s.byKind ? FindSlot(s.cardKind) : s.slot;
                if (HintFinder.ForSlot(controller.State, k, true) != null) { Next(); return; }
            }
            UpdateHints();   // đổi hướng thẻ: đường cắt đậm / lời nhắc xoay cập nhật theo hướng mới
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
            bool hasA = !wait && Source(s, out a);
            bool hasB = !wait && !rotatePrompt && s.gesture == TutGesture.Drag && Dest(s, out b);

            // lỗ sáng: khung câu chữ + chỗ kéo + chỗ thả (+ nút đang được nhắc)
            holes.Clear();
            if (!wait && UiRect((RectTransform)tip.transform, out var tr)) holes.Add(Pad(tr, 4));
            if (hasA) holes.Add(Pad(a, 10));
            if (hasB) holes.Add(Pad(b, 10));
            bool undo = Time.time < undoFlashUntil && TryTarget(TutTarget.Undo, s, out u);
            if (undo) holes.Add(Pad(u, 8));
            dim.SetHoles(holes);

            Place(frameA, hasA, Pad(a, 6));
            if (undo) Place(frameB, true, Pad(u, 6));
            else Place(frameB, hasB, Pad(b, 6));
            float pulse = 1 + .05f * Mathf.Sin(Time.time * 6);
            frameA.localScale = frameB.localScale = new Vector3(pulse, pulse, 1);

            // tay chỉ: chạm (ấn xuống, nảy) hoặc kéo A → B rồi mờ, lặp lại
            var gesture = rotatePrompt ? TutGesture.Tap : s.gesture;   // sai hướng: tay chạm thẻ để xoay, không kéo
            hand.gameObject.SetActive(hasA && gesture != TutGesture.None);
            if (!hand.gameObject.activeSelf) return;
            var img = hand.GetComponent<Image>();
            float t = (Time.time - stepTime) % 1.6f;
            if (gesture == TutGesture.Tap || !hasB)
            {
                float press = t < .25f ? t / .25f : t < .5f ? 1 - (t - .25f) / .25f : 0;
                hand.anchoredPosition = a.center + new Vector2(0, -6 * press);
                hand.localScale = Vector3.one * (1 - .12f * press);
                img.color = Color.white;
            }
            else
            {
                float k = Mathf.Clamp01((t - .2f) / 1.0f);
                k = k * k * (3 - 2 * k);
                hand.anchoredPosition = Vector2.Lerp(a.center, b.center, k);
                hand.localScale = Vector3.one * (t < .2f ? .9f : 1f);
                img.color = new Color(1, 1, 1, t < 1.25f ? 1 : 1 - (t - 1.25f) / .35f);
            }
        }

        static Rect Pad(Rect r, float p) => new Rect(r.xMin - p, r.yMin - p, r.width + 2 * p, r.height + 2 * p);

        static void Place(RectTransform rt, bool on, Rect r)
        {
            rt.gameObject.SetActive(on);
            if (!on) return;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = r.center;
            rt.sizeDelta = r.size;
        }

        // ======================= Tìm chỗ trỏ (toạ độ local của overlay) =======================

        /// <summary>Chỗ bắt đầu thao tác. Bước đặt miếng: miếng mà lời giải cần đặt trước.</summary>
        bool Source(TutorialStep s, out Rect r)
        {
            if (placeHint != null && s.dragTo == TutTarget.TrayEmpty && s.target != TutTarget.Card)
                return BoardRect(placeHint.piece.tris, out r);
            return TryTarget(s.target, s, out r);
        }

        /// <summary>Chỗ thả: đường cắt đúng / miếng cần biến hình / ô cần đặt miếng (nếu tính được), không thì theo dữ liệu bước.</summary>
        bool Dest(TutorialStep s, out Rect r)
        {
            if (cardHint != null)
            {
                if (cardHint.res.target != null) return BoardRect(cardHint.res.target.tris, out r);
                if (cardHint.res.segs.Count > 0) return SegRect(cardHint.res.segs, out r);
            }
            if (placeHint != null && s.dragTo == TutTarget.TrayEmpty) return BoardRect(placeHint.target, out r);
            return TryTarget(s.dragTo, s, out r);
        }

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
                case TutTarget.Restart:
                    return UiRect(hud.RestartRect, out r);
                case TutTarget.HintButton:
                    return UiRect(hintButton, out r);
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

        bool SegRect(List<Seg> segs, out Rect r)
        {
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (var g in segs)
            {
                x0 = Mathf.Min(x0, Mathf.Min(g.x1, g.x2)); x1 = Mathf.Max(x1, Mathf.Max(g.x1, g.x2));
                y0 = Mathf.Min(y0, Mathf.Min(g.y1, g.y2)); y1 = Mathf.Max(y1, Mathf.Max(g.y1, g.y2));
            }
            return Corners(new Vector2(x0 - .3f, y0 - .3f), new Vector2(x1 + .3f, y1 + .3f), out r);
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
            if (rt == null || !rt.gameObject.activeInHierarchy) return false;
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
