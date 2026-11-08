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

        public void Load(int i)
        {
            Index = db.Wrap(i);
            ProgressSave.CurrentLevel = Index;
            State = BoardState.FromLevel(Level);
            history.Clear();
            Won = false;
            board.ResetFx();
            hand.HideGhost();
            Refresh();
            EventBus<LevelLoadedEvent>.Raise(new LevelLoadedEvent { index = Index, total = db.Count });
        }

        public void Restart() => Load(Index);
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
            if (Won || (dx == 0 && dy == 0) || !Moves.CanPlacePiece(State, p, Geometry.Shift(p.tris, dx, dy))) return false;
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
            if (Moves.CheckWin(State) == WinState.Won) Win();
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
