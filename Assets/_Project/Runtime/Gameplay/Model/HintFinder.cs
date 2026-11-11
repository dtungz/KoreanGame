using System.Collections.Generic;
using UnityEngine;

namespace KoreanGame.Gameplay
{
    public enum HintKind { None, Stuck, Place, Card }

    /// <summary>Nước đi gợi ý. Card: dùng thẻ ở ô slot với hướng rot (res = xem trước đường cắt / miếng bị biến hình). Place: kéo piece vào target.</summary>
    public class Hint
    {
        public HintKind kind;
        public int slot = -1;
        public int rot;
        public bool needRotate;
        public MoveResult res;
        public Piece piece;
        public List<Tri> target;
    }

    /// <summary>
    /// Tìm nước đi kế tiếp đúng từ trạng thái đang chơi (dùng cho nút Gợi ý và đường cắt đậm của tutorial).
    /// Nước đúng = sau khi đi, LevelSolver.Solvable còn true. Không đổi trạng thái thật (thử trên bản sao).
    /// </summary>
    public static class HintFinder
    {
        const int Budget = 6000;   // số trạng thái solver được duyệt cho mỗi nước thử

        public static Hint Find(BoardState s)
        {
            if (!LevelSolver.Solvable(s, Budget * 4)) return new Hint { kind = HintKind.Stuck };
            var place = PlaceNow(s);
            if (place != null) return place;
            for (int k = 0; k < s.slots.Length; k++)
            {
                var h = ForSlot(s, k, false);
                if (h != null) return h;
            }
            return new Hint { kind = HintKind.None };
        }

        /// <summary>Nước dùng thẻ ở ô k còn giữ được lời giải. onlyCurrentRot: chỉ thử hướng thẻ hiện tại (tutorial vẽ đường cắt đậm).</summary>
        public static Hint ForSlot(BoardState s, int k, bool onlyCurrentRot)
        {
            int i = k >= 0 && k < s.slots.Length ? s.slots[k] : -1;
            if (i < 0 || s.hand[i].used) return null;
            var def = s.hand[i].def;
            var cache = new Dictionary<string, bool>();
            var outside = Outside(s);

            if (def.IsFunction)
            {
                // Xoay / Lật: hướng = rot % 2 (Xoay ↻ / ↺, Lật ngang / dọc); Nhân bản không có hướng
                int curDir = s.hand[i].rot % 2;
                var dirsList = new List<int>();
                if (def.kind == CardKind.Copy) dirsList.Add(0);
                else { dirsList.Add(curDir); if (!onlyCurrentRot) dirsList.Add(1 - curDir); }
                foreach (var p in outside)
                {
                    var b = Geometry.BBox(p.tris);
                    float wx = (b.minx + b.maxx + 1) / 2f, wy = (b.miny + b.maxy + 1) / 2f;
                    foreach (int d in dirsList)
                    {
                        var card = new HandCard { def = def, rot = d };
                        var res = Moves.EvaluateFn(s, card, wx, wy);
                        if (!res.valid) continue;
                        if (Works(s, i, st => Moves.EvaluateFn(st, card, wx, wy), cache))
                            return new Hint { kind = HintKind.Card, slot = k, rot = d, needRotate = def.kind != CardKind.Copy && d != curDir, res = res, piece = res.target };
                    }
                }
                return null;
            }

            int cur = s.hand[i].rot;
            var rots = new List<int> { cur };
            if (!onlyCurrentRot && def.CanRotate) for (int r = 0; r < 4; r++) if (r != cur) rots.Add(r);
            var bb = Union(s.pieces);
            foreach (int rot in rots)
            {
                var card = new HandCard { def = def, rot = rot };
                var g = Geometry.CardGeom(def.Polys, rot);
                for (int oy = bb.miny - g.h; oy <= bb.maxy + 1; oy++)
                    for (int ox = bb.minx - g.w; ox <= bb.maxx + 1; ox++)
                    {
                        int x = ox, y = oy;
                        MoveResult Eval(BoardState st) => def.kind == CardKind.Glue ? Moves.EvaluateGlue(st, card, x, y) : Moves.EvaluateCut(st, card, x, y);
                        var res = Eval(s);
                        if (!res.valid) continue;
                        if (Works(s, i, Eval, cache))
                            return new Hint { kind = HintKind.Card, slot = k, rot = rot, needRotate = rot != cur, res = res };
                    }
            }
            return null;
        }

        /// <summary>Thử nước đi trên bản sao: còn giải được không (cache theo hình dạng kết quả).</summary>
        static bool Works(BoardState s, int handIndex, System.Func<BoardState, MoveResult> eval, Dictionary<string, bool> cache)
        {
            var c = s.Clone();
            var res = eval(c);
            if (!res.valid) return false;
            Moves.Apply(c, res);
            c.UseCard(handIndex);
            var key = Key(c);
            if (cache.TryGetValue(key, out var ok)) return ok;
            ok = LevelSolver.Solvable(c, Budget);
            cache[key] = ok;
            return ok;
        }

        static string Key(BoardState c)
        {
            var keys = new List<string>();
            foreach (var p in c.pieces) keys.Add(Geometry.ShapeKey(p.tris));
            keys.Sort(System.StringComparer.Ordinal);
            return string.Join("/", keys) + "|" + string.Join(",", c.slots) + "|" + c.queuePos;
        }

        static List<Piece> Outside(BoardState s)
        {
            var inside = s.PiecesInsideTray();
            return s.pieces.FindAll(p => !inside.Contains(p));
        }

        static BBox Union(List<Piece> pieces)
        {
            var all = new List<Tri>();
            foreach (var p in pieces) all.AddRange(p.tris);
            return Geometry.BBox(all);
        }

        /// <summary>Miếng ngoài khay xếp kín phần còn trống ngay (không cần thẻ): gợi ý chỗ đặt miếng đầu tiên.</summary>
        static Hint PlaceNow(BoardState s)
        {
            var wk = s.WallKeys();
            var outside = Outside(s);
            if (outside.Count == 0) return null;
            var covered = new HashSet<Tri>();
            foreach (var p in s.PiecesInsideTray()) foreach (var q in p.tris) covered.Add(q);
            var target = s.tray.Need().FindAll(q => !covered.Contains(q));
            target.Sort();
            var tset = new HashSet<Tri>(target);
            var pieces = outside.ConvertAll(p => { var l = new List<Tri>(p.tris); l.Sort(); return l; });
            var used = new bool[pieces.Count];
            var fill = new HashSet<Tri>();
            int firstPiece = -1;
            List<Tri> firstTarget = null;

            bool Rec()
            {
                int fi = target.FindIndex(q => !fill.Contains(q));
                if (fi < 0) return true;
                var first = target[fi];
                for (int i = 0; i < pieces.Count; i++)
                {
                    if (used[i] || pieces[i][0].t != first.t) continue;
                    var moved = Geometry.Shift(pieces[i], first.x - pieces[i][0].x, first.y - pieces[i][0].y);
                    if (!moved.TrueForAll(q => tset.Contains(q) && !fill.Contains(q)) || Geometry.CrossesWall(moved, wk)) continue;
                    used[i] = true;
                    foreach (var q in moved) fill.Add(q);
                    bool top = firstPiece < 0;
                    if (top) { firstPiece = i; firstTarget = moved; }
                    if (Rec()) return true;
                    if (top) { firstPiece = -1; firstTarget = null; }
                    used[i] = false;
                    foreach (var q in moved) fill.Remove(q);
                }
                return false;
            }

            if (!Rec() || firstPiece < 0) return null;
            return new Hint { kind = HintKind.Place, piece = outside[firstPiece], target = firstTarget };
        }
    }
}
