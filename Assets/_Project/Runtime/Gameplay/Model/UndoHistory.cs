using System.Collections.Generic;

namespace KoreanGame.Gameplay
{
    /// <summary>Hoàn tác miễn phí, không giới hạn (GDD 4.9): lưu bản chụp trạng thái trước mỗi thao tác.</summary>
    public class UndoHistory
    {
        readonly Stack<BoardState> stack = new Stack<BoardState>();

        public int Count => stack.Count;
        public void Push(BoardState s) => stack.Push(s.Clone());
        public BoardState Pop() => stack.Count > 0 ? stack.Pop() : null;
        public void Clear() => stack.Clear();
    }
}
