using System.Collections.Generic;
using UnityEngine;

namespace KoreanGame.Gameplay
{
    public class Piece
    {
        public int id;
        public List<Tri> tris;

        public Piece Clone() => new Piece { id = id, tris = new List<Tri>(tris) };
        public bool Contains(Tri q) => tris.Contains(q);
    }

    public class TrayState
    {
        public List<Tri> tris = new List<Tri>();
        public List<Wall> walls = new List<Wall>();
        public List<FixedFood> fixedFoods = new List<FixedFood>();

        public TrayState Clone()
        {
            var t = new TrayState { tris = new List<Tri>(tris), walls = new List<Wall>(walls) };
            foreach (var f in fixedFoods) t.fixedFoods.Add(f.Clone());
            return t;
        }

        public void Shift(int dx, int dy)
        {
            tris = Geometry.Shift(tris, dx, dy);
            for (int i = 0; i < walls.Count; i++) walls[i] = walls[i].Shift(dx, dy);
            foreach (var f in fixedFoods)
                for (int i = 0; i < f.cells.Count; i++) f.cells[i] += new Vector2Int(dx, dy);
        }

        public List<Tri> FixedTris()
        {
            var r = new List<Tri>();
            foreach (var f in fixedFoods) r.AddRange(Geometry.Full(f.cells));
            return r;
        }

        /// <summary>Phần khay cần lấp cơm (trừ món cố định).</summary>
        public List<Tri> Need()
        {
            var fx = new HashSet<Tri>(FixedTris());
            return tris.FindAll(q => !fx.Contains(q));
        }

        public bool Contains(Tri q) => tris.Contains(q);
    }

    public class HandCard
    {
        public CardDef def;
        public int rot;
        public bool used;
        public HandCard Clone() => new HandCard { def = def, rot = rot, used = used };
    }

    /// <summary>Toàn bộ trạng thái một màn đang chơi. Clone để hoàn tác.</summary>
    public class BoardState
    {
        public const int SlotCount = 3;

        public List<Piece> pieces = new List<Piece>();
        public TrayState tray = new TrayState();
        public List<HandCard> hand = new List<HandCard>();
        public int[] slots = new int[SlotCount];
        public int queuePos;
        public int nextId = 1;

        public static BoardState FromLevel(LevelData L)
        {
            var s = new BoardState();
            foreach (var r in L.rice) s.pieces.Add(new Piece { id = s.nextId++, tris = Geometry.Shift(r.tris, r.at.x, r.at.y) });
            var at = L.tray.at;
            s.tray.tris = Geometry.Shift(L.tray.tris, at.x, at.y);
            foreach (var w in L.tray.walls) s.tray.walls.Add(w.Shift(at.x, at.y));
            foreach (var f in L.tray.fixedFoods)
            {
                var c = f.Clone();
                for (int i = 0; i < c.cells.Count; i++) c.cells[i] += at;
                s.tray.fixedFoods.Add(c);
            }
            foreach (var c in L.cards) s.hand.Add(new HandCard { def = c });
            s.DealSlots();
            return s;
        }

        public BoardState Clone()
        {
            var s = new BoardState { tray = tray.Clone(), slots = (int[])slots.Clone(), queuePos = queuePos, nextId = nextId };
            foreach (var p in pieces) s.pieces.Add(p.Clone());
            foreach (var h in hand) s.hand.Add(h.Clone());
            return s;
        }

        public void DealSlots()
        {
            for (int k = 0; k < SlotCount; k++) slots[k] = k < hand.Count ? k : -1;
            queuePos = Mathf.Min(SlotCount, hand.Count);
        }

        /// <summary>Đánh dấu thẻ i đã dùng; thẻ kế tiếp trong hàng chờ vào đúng ô đó. Trả về ô vừa được lấp (-1 nếu không).</summary>
        public int UseCard(int i)
        {
            hand[i].used = true;
            int k = System.Array.IndexOf(slots, i);
            if (k < 0) return -1;
            slots[k] = queuePos < hand.Count ? queuePos++ : -1;
            return slots[k] >= 0 ? k : -1;
        }

        public int CardsLeft => hand.FindAll(h => !h.used).Count;
        public int QueuedCount => hand.Count - queuePos;

        public Piece PieceAt(Tri q) => pieces.Find(p => p.Contains(q));

        public HashSet<Tri> Occupied(Piece except = null)
        {
            var s = new HashSet<Tri>();
            foreach (var p in pieces) if (p != except) foreach (var q in p.tris) s.Add(q);
            return s;
        }

        public HashSet<EdgeKey> WallKeys() => Geometry.WallKeys(tray.walls);

        /// <summary>Các miếng nằm gọn trong khay (đi theo khay khi kéo khay).</summary>
        public List<Piece> PiecesInsideTray()
        {
            var ts = new HashSet<Tri>(tray.tris);
            return pieces.FindAll(p => p.tris.TrueForAll(ts.Contains));
        }
    }
}
