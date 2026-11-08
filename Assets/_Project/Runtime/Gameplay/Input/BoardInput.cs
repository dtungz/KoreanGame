using System.Collections.Generic;
using KoreanGame.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KoreanGame.Gameplay
{
    /// <summary>
    /// Điều khiển bằng chuột / cảm ứng (Input Manager cũ): kéo thẻ để cắt, chạm thẻ để xoay,
    /// kéo miếng cơm, kéo khay. Ngưỡng chạm 12 px (cảm ứng) / 5 px (chuột) theo khung 390×844.
    /// </summary>
    public class BoardInput : MonoBehaviour
    {
        const float RefHeight = 844f;
        const float TouchLift = 1.3f;   // đường cắt hiện cao hơn ngón tay ~1,3 ô

        enum DragType { None, Card, Piece, Tray }

        LevelController controller;
        BoardView board;
        HandView hand;

        DragType type;
        bool touch, moved;
        int pointerId;
        Vector2 startScreen, startBoard;
        int cardIndex;
        Piece piece;
        List<Piece> carried;
        MoveResult res;
        Vector2Int lastKey = new Vector2Int(int.MinValue, 0);
        Tri lastTri;

        public bool Enabled { get; set; } = true;

        public void Init(LevelController c, BoardView b, HandView h)
        {
            controller = c;
            board = b;
            hand = h;
            Input.simulateMouseWithTouches = false;
        }

        void Update()
        {
            if (controller == null || controller.State == null) return;
            if (Input.touchCount > 0)
            {
                foreach (var t in Input.touches)
                {
                    if (t.phase == TouchPhase.Began && type == DragType.None) Down(t.position, true, t.fingerId);
                    else if (t.fingerId == pointerId && type != DragType.None && touch)
                    {
                        if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary) Move(t.position);
                        else if (t.phase == TouchPhase.Ended) Up(t.position, false);
                        else if (t.phase == TouchPhase.Canceled) Up(t.position, true);
                    }
                }
                return;
            }
            if (Input.GetMouseButtonDown(0) && type == DragType.None) Down(Input.mousePosition, false, -1);
            else if (type != DragType.None && !touch)
            {
                if (Input.GetMouseButton(0)) Move(Input.mousePosition);
                if (Input.GetMouseButtonUp(0)) Up(Input.mousePosition, false);
            }
        }

        float TapPx => (touch ? 12f : 5f) * Screen.height / RefHeight;

        void Down(Vector2 pos, bool isTouch, int id)
        {
            if (!Enabled || controller.Won) return;
            touch = isTouch;
            pointerId = id;
            startScreen = pos;
            moved = false;
            res = null;
            lastKey = new Vector2Int(int.MinValue, 0);

            int ci = hand.HitTest(pos);
            if (ci >= 0) { type = DragType.Card; cardIndex = ci; return; }

            // nút / panel UI khác (hoàn tác, debug...) thì bỏ qua
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(isTouch ? id : -1)) return;

            var s = controller.State;
            startBoard = board.ScreenToBoard(pos);
            var q = Geometry.TriAt(startBoard.x, startBoard.y);
            piece = s.PieceAt(q);
            if (piece != null) { type = DragType.Piece; return; }
            if (s.tray.Contains(q) || NearTray(s, startBoard))
            {
                type = DragType.Tray;
                carried = s.PiecesInsideTray();
            }
        }

        static bool NearTray(BoardState s, Vector2 p)
        {
            foreach (var o in new[] { new Vector2(.3f, 0), new Vector2(-.3f, 0), new Vector2(0, .3f), new Vector2(0, -.3f),
                                      new Vector2(.3f, .3f), new Vector2(-.3f, .3f), new Vector2(.3f, -.3f), new Vector2(-.3f, -.3f) })
                if (s.tray.Contains(Geometry.TriAt(p.x + o.x, p.y + o.y))) return true;
            return false;
        }

        void Move(Vector2 pos)
        {
            if (!moved && Vector2.Distance(pos, startScreen) < TapPx) return;
            bool first = !moved;
            moved = true;
            var bp = board.ScreenToBoard(pos);

            if (type == DragType.Card)
            {
                if (first)
                {
                    hand.SetDragging(cardIndex, true);
                    hand.BeginGhost(controller.State.hand[cardIndex], cardIndex, pos);
                }
                hand.MoveGhost(pos);
                if (touch) bp.y -= TouchLift;
                var card = controller.State.hand[cardIndex];
                if (card.def.IsFunction)
                {
                    var tq = Geometry.TriAt(bp.x, bp.y);
                    if (res == null || !tq.Equals(lastTri)) { lastTri = tq; res = Moves.EvaluateFn(controller.State, card, bp.x, bp.y); }
                    res.point = bp;
                    board.ShowPreview(res);
                    return;
                }
                var key = Moves.CardOrigin(card, bp.x, bp.y);
                if (res == null || key != lastKey)
                {
                    lastKey = key;
                    res = card.def.kind == CardKind.Glue
                        ? Moves.EvaluateGlue(controller.State, card, key.x, key.y)
                        : Moves.EvaluateCut(controller.State, card, key.x, key.y);
                    board.ShowPreview(res);
                }
                return;
            }

            var off = bp - startBoard;
            int dx = Mathf.FloorToInt(off.x + .5f), dy = Mathf.FloorToInt(off.y + .5f);
            var s = controller.State;
            if (type == DragType.Piece)
            {
                var target = Geometry.Shift(piece.tris, dx, dy);
                board.ShowDropOutline(target, Moves.CanPlacePiece(s, piece, target));
                board.Lift(piece, false, null, off);
            }
            else if (type == DragType.Tray)
            {
                board.ShowDropOutline(Geometry.Shift(s.tray.tris, dx, dy), Moves.CanPlaceTray(s, dx, dy));
                board.Lift(null, true, carried, off);
            }
        }

        void Up(Vector2 pos, bool cancelled)
        {
            var t = type;
            type = DragType.None;
            if (t == DragType.None) return;

            if (t == DragType.Card)
            {
                hand.SetDragging(cardIndex, false);
                board.ClearPreview();
                if (cancelled) { hand.HideGhost(); return; }
                if (!moved) { controller.RotateCard(cardIndex); return; }
                bool used = controller.ApplyCard(cardIndex, res);
                hand.EndGhost(used, cardIndex);
                if (!used) hand.Shake(cardIndex);
                return;
            }

            var off = board.ScreenToBoard(pos) - startBoard;
            int dx = Mathf.FloorToInt(off.x + .5f), dy = Mathf.FloorToInt(off.y + .5f);
            bool isTray = t == DragType.Tray;
            bool tried = !cancelled && moved && (dx != 0 || dy != 0);
            bool ok = tried && (isTray ? controller.TryMoveTray(dx, dy) : controller.TryMovePiece(piece, dx, dy));
            if (ok)
            {
                // hít vào ô: trượt nốt phần lẻ còn lại, hơi nảy
                board.SnapFrom(piece, isTray, carried, off - new Vector2(dx, dy), true);
                return;
            }
            // không đặt được: bay về chỗ cũ từ vị trí đang thả
            board.Render(controller.State);
            if (moved) board.SnapFrom(piece, isTray, carried, off, false);
            if (tried && !controller.Won)
            {
                if (!isTray && Moves.Straddles(controller.State.tray, Geometry.Shift(piece.tris, dx, dy))) board.FlashTrayEdge(controller.State.tray);
                EventBus<MoveRejectedEvent>.Raise(default);
            }
        }
    }
}
