using KoreanGame.Core;
using UnityEngine;

namespace KoreanGame.Gameplay
{
    /// <summary>Điều phối một màn: nạp level, nhận thao tác từ input, hoàn tác, kiểm tra thắng, chuyển màn.</summary>
    public class LevelController : MonoBehaviour
    {
        LevelDatabase db;
        BoardView board;
        HandView hand;
        readonly UndoHistory history = new UndoHistory();

        public BoardState State { get; private set; }
        public int Index { get; private set; }
        public int Count => db.Count;
        public LevelData Level => db.Get(Index);
        public bool Won { get; private set; }
        public bool CanUndo => history.Count > 0 && !Won;

        public void Init(LevelDatabase database, BoardView boardView, HandView handView)
        {
            db = database;
            board = boardView;
            hand = handView;
        }

        /// <summary>Nạp level i. keepHistory = true chỉ dùng cho Chơi lại (giữ lịch sử để hoàn tác được).</summary>
        public void Load(int i, bool keepHistory = false)
        {
            Index = db.Wrap(i);
            ProgressSave.CurrentLevel = Index;
            State = BoardState.FromLevel(Level);
            if (!keepHistory) history.Clear();
            Won = false;
            board.ResetFx();
            hand.HideGhost();
            Refresh();
            // Refresh phải chạy trước event: BoardInput.CancelDrag dựa vào thớt / tay bài đã vẽ lại
            EventBus<LevelLoadedEvent>.Raise(new LevelLoadedEvent { index = Index, total = db.Count });
        }

        /// <summary>
        /// Chơi lại màn hiện tại. Hoàn tác được (GDD trụ cột 2): bàn cờ trước khi chơi lại vào lịch sử.
        /// Đã thắng hoặc chưa làm gì thì xoá lịch sử như nạp màn mới.
        /// </summary>
        public void Restart()
        {
            bool keep = !Won && history.Count > 0;
            if (keep) history.Push(State);
            Load(Index, keep);
        }
        public void Next() => Load(Index + 1);
        public void Prev() => Load(Index - 1);

        void Refresh(int enteringSlot = -1)
        {
            board.Render(State);
            hand.Render(State, enteringSlot);
            EventBus<BoardChangedEvent>.Raise(default);
        }

        public void Undo()
        {
            if (!CanUndo) return;
            State = history.Pop();
            board.ResetFx();
            Refresh();
            // Refresh phải chạy trước event: BoardInput.CancelDrag dựa vào thớt / tay bài đã vẽ lại
            EventBus<UndoEvent>.Raise(default);
        }

        public void RotateCard(int handIndex)
        {
            if (Won) return;
            var c = State.hand[handIndex];
            if (!c.def.CanRotate) return;
            history.Push(State);
            c.rot = (c.rot + 1) % 4;
            Refresh();
            EventBus<CardRotatedEvent>.Raise(default);
        }

        /// <summary>Áp dụng thẻ đã xem trước. Trả về false nếu thả hỏng (thẻ về tay, không mất).</summary>
        public bool ApplyCard(int handIndex, MoveResult res)
        {
            if (Won || res == null || !res.valid)
            {
                board.PlayRejectCard(res);
                EventBus<MoveRejectedEvent>.Raise(default);
                return false;
            }
            history.Push(State);
            Moves.Apply(State, res);
            int entering = State.UseCard(handIndex);
            Refresh(entering);
            board.PlayCardFx(res);
            EventBus<CardUsedEvent>.Raise(new CardUsedEvent
            {
                effect = res.kind == CardKind.Cut ? CardEffect.Cut : res.kind == CardKind.Glue ? CardEffect.Glue
                    : res.kind == CardKind.Copy ? CardEffect.Copy : CardEffect.Transform
            });
            CheckWin();
            return true;
        }

        public bool TryMovePiece(Piece p, int dx, int dy)
        {
            if (Won || (dx == 0 && dy == 0) || !State.pieces.Contains(p)) return false;   // p phải thuộc State hiện tại
            if (!Moves.CanPlacePiece(State, p, Geometry.Shift(p.tris, dx, dy))) return false;
            history.Push(State);
            // history giữ bản sao; p vẫn là object của State hiện tại
            Moves.MovePiece(State, p, dx, dy);
            Refresh();
            bool intoTray = p.tris.TrueForAll(State.tray.Contains);
            if (intoTray) board.Flash(p.tris, new Color(1, 1, 1, .45f), false, .25f);
            EventBus<PiecePlacedEvent>.Raise(new PiecePlacedEvent { intoTray = intoTray });
            CheckWin();
            return true;
        }

        public bool TryMoveTray(int dx, int dy)
        {
            if (Won || (dx == 0 && dy == 0) || !Moves.CanPlaceTray(State, dx, dy)) return false;
            history.Push(State);
            Moves.MoveTray(State, dx, dy);
            Refresh();
            EventBus<PiecePlacedEvent>.Raise(new PiecePlacedEvent { movedTray = true });
            CheckWin();
            return true;
        }

        void CheckWin()
        {
            var w = Moves.CheckWin(State);
            if (w == WinState.Won) Win();
            else if (w == WinState.WallBlocked)
            {
                // khay đã kín nhưng có miếng vắt qua vách: báo bằng hình + âm, không chữ (GDD 4.4, 9.2)
                board.FlashWallCross(Moves.WallCrossingPiece(State).tris, State.tray);
                EventBus<WallBlockedEvent>.Raise(default);
            }
        }

        void Win()
        {
            Won = true;
            bool last = Index == db.Count - 1;
            ProgressSave.CurrentLevel = db.Wrap(Index + 1);
            hand.HideGhost();
            board.PlayWin(State.tray);
            EventBus<BoardChangedEvent>.Raise(default);
            EventBus<LevelWonEvent>.Raise(new LevelWonEvent { index = Index, isLast = last });
        }

        /// <summary>Debug: lấp kín khay bằng một miếng cơm rồi thắng ngay.</summary>
        public void ForceWin()
        {
            if (Won) return;
            history.Push(State);
            var need = State.tray.Need();
            var inside = new System.Collections.Generic.HashSet<Tri>(need);
            State.pieces.RemoveAll(p => p.tris.Exists(inside.Contains));
            State.pieces.Add(new Piece { id = State.nextId++, tris = need });
            Refresh();
            Win();
        }
    }
}
