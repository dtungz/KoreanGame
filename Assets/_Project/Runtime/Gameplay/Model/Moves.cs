using System.Collections.Generic;
using UnityEngine;

namespace KoreanGame.Gameplay
{
    public class CutSplit
    {
        public Piece piece;
        public List<List<Tri>> parts;
    }

    /// <summary>Kết quả xem trước khi kéo thẻ. Dùng cho cả hiển thị và áp dụng.</summary>
    public class MoveResult
    {
        public CardKind kind;
        public bool valid;
        public string why = "";
        public int ox, oy;
        public List<Seg> segs = new List<Seg>();
        public List<CutSplit> splits = new List<CutSplit>();
        public List<(Piece a, Piece b)> joins = new List<(Piece, Piece)>();
        public List<Piece> touched = new List<Piece>();
        public Piece target;
        public List<Tri> tris;
        public Vector2 point;
    }

    public enum WinState { None, Won, WallBlocked }

    /// <summary>Luật chơi trên BoardState, port từ prototype play.html.</summary>
    public static class Moves
    {
        static int Round(float v) => Mathf.FloorToInt(v + .5f);   // giống Math.round của JS

        /// <summary>Đánh giá thẻ khi kéo tới điểm (wx, wy) trên thớt.</summary>
        public static MoveResult EvaluateCard(BoardState s, HandCard card, float wx, float wy)
        {
            if (card.def.IsFunction) return EvaluateFn(s, card, wx, wy);
            var g = Geometry.CardGeom(card.def.Polys, card.rot);
            int ox = Round(wx - g.w / 2f), oy = Round(wy - g.h / 2f);
            return card.def.kind == CardKind.Glue ? EvaluateGlue(s, card, ox, oy) : EvaluateCut(s, card, ox, oy);
        }

        /// <summary>Điểm neo (ox, oy) của đường cắt khi kéo tới (wx, wy); dùng để biết khi nào cần tính lại.</summary>
        public static Vector2Int CardOrigin(HandCard card, float wx, float wy)
        {
            var g = Geometry.CardGeom(card.def.Polys, card.rot);
            return new Vector2Int(Round(wx - g.w / 2f), Round(wy - g.h / 2f));
        }

        public static MoveResult EvaluateCut(BoardState s, HandCard card, int ox, int oy)
        {
            var g = Geometry.CardGeom(card.def.Polys, card.rot);
            var res = new MoveResult { kind = CardKind.Cut, ox = ox, oy = oy, segs = Geometry.UnitSegments(g.polys, ox, oy) };
            var ek = Geometry.SegKeys(res.segs);
            foreach (var p in s.pieces)
            {
                var parts = Geometry.Components(p.tris, ek);
                if (parts.Count > 1) res.splits.Add(new CutSplit { piece = p, parts = parts });
            }
            res.valid = res.splits.Count > 0;
            return res;
        }

        public static MoveResult EvaluateGlue(BoardState s, HandCard card, int ox, int oy)
        {
            var g = Geometry.CardGeom(card.def.Polys, card.rot);
            var res = new MoveResult { kind = CardKind.Glue, ox = ox, oy = oy, segs = Geometry.UnitSegments(g.polys, ox, oy) };
            var owner = new Dictionary<Tri, Piece>();
            foreach (var p in s.pieces) foreach (var q in p.tris) owner[q] = p;
            foreach (var k in Geometry.SegKeys(res.segs))
                foreach (var (a, b) in Geometry.EdgePairs(k))
                    if (owner.TryGetValue(a, out var fa) && owner.TryGetValue(b, out var fb) && fa != fb)
                    {
                        res.joins.Add((fa, fb));
                        if (!res.touched.Contains(fa)) res.touched.Add(fa);
                        if (!res.touched.Contains(fb)) res.touched.Add(fb);
                    }
            res.valid = res.joins.Count > 0;
            return res;
        }

        public static MoveResult EvaluateFn(BoardState s, HandCard card, float wx, float wy)
        {
            var kind = card.def.kind;
            var res = new MoveResult { kind = kind, point = new Vector2(wx, wy) };
            var target = s.PieceAt(Geometry.TriAt(wx, wy));
            if (target == null) return res;
            res.target = target;
            var b = Geometry.BBox(target.tris);
            float cx = (b.minx + b.maxx + 1) / 2f, cy = (b.miny + b.maxy + 1) / 2f;
            if (kind == CardKind.Copy)
            {
                if (target.tris.Count > 8) { res.why = "Chỉ nhân bản được miếng tối đa 2 ô."; return res; }
                res.tris = NearestFree(s, Geometry.Normalize(target.tris), cx, cy, null);
                res.valid = res.tris != null;
                if (!res.valid) res.why = "Không còn chỗ trống trên thớt.";
                return res;
            }
            res.tris = NearestFree(s, Geometry.Normalize(Geometry.TransformFn(kind, card.rot % 2, target.tris)), cx, cy, target);
            res.valid = res.tris != null;
            if (!res.valid) res.why = "Không đủ chỗ để biến hình miếng này.";
            return res;
        }

        static List<Tri> NearestFree(BoardState s, List<Tri> norm, float cx, float cy, Piece ignore)
        {
            var b = Geometry.BBox(norm);
            var cand = new List<(int ox, int oy, float d)>();
            for (int ox = -b.minx; ox <= Geometry.Cols - 1 - b.maxx; ox++)
                for (int oy = -b.miny; oy <= Geometry.Rows - 1 - b.maxy; oy++)
                    cand.Add((ox, oy, Mathf.Sqrt(Sq(ox + (b.maxx + 1) / 2f - cx) + Sq(oy + (b.maxy + 1) / 2f - cy))));
            cand.Sort((p, q) => p.d.CompareTo(q.d));
            foreach (var (ox, oy, _) in cand)
            {
                var t = Geometry.Shift(norm, ox, oy);
                if (CanPlacePiece(s, ignore, t)) return t;
            }
            return null;
        }

        static float Sq(float v) => v * v;

        public static void Apply(BoardState s, MoveResult res)
        {
            switch (res.kind)
            {
                case CardKind.Cut:
                    foreach (var sp in res.splits)
                    {
                        int i = s.pieces.IndexOf(sp.piece);
                        if (i < 0) continue;
                        s.pieces.RemoveAt(i);
                        for (int j = 0; j < sp.parts.Count; j++)
                            s.pieces.Insert(i + j, new Piece { id = j == 0 ? sp.piece.id : s.nextId++, tris = sp.parts[j] });
                    }
                    break;
                case CardKind.Glue:
                {
                    var parent = new Dictionary<Piece, Piece>();
                    foreach (var p in s.pieces) parent[p] = p;
                    Piece Find(Piece p) => parent[p] == p ? p : Find(parent[p]);
                    foreach (var (a, b) in res.joins) parent[Find(a)] = Find(b);
                    var groups = new List<(Piece root, List<Piece> list)>();
                    foreach (var p in s.pieces)
                    {
                        var r = Find(p);
                        int gi = groups.FindIndex(g => g.root == r);
                        if (gi < 0) groups.Add((r, new List<Piece> { p }));
                        else groups[gi].list.Add(p);
                    }
                    s.pieces = new List<Piece>();
                    foreach (var (_, list) in groups)
                    {
                        if (list.Count == 1) { s.pieces.Add(list[0]); continue; }
                        var tris = new List<Tri>();
                        foreach (var p in list) tris.AddRange(p.tris);
                        s.pieces.Add(new Piece { id = list[0].id, tris = tris });
                    }
                    break;
                }
                case CardKind.Copy:
                    s.pieces.Add(new Piece { id = s.nextId++, tris = res.tris });
                    break;
                default:
                    res.target.tris = res.tris;
                    break;
            }
        }

        public static bool CanPlacePiece(BoardState s, Piece piece, IReadOnlyList<Tri> tris)
        {
            var occ = s.Occupied(piece);
            var fx = new HashSet<Tri>(s.tray.FixedTris());
            foreach (var q in tris) if (!Geometry.InBoard(q) || occ.Contains(q) || fx.Contains(q)) return false;
            return !Straddles(s.tray, tris);
        }

        /// <summary>
        /// Miếng cơm vắt qua mép hộp: một phần nằm trong hộp, phần còn lại ở ngoài (kể cả chỉ chạm vào ô có vành hộp).
        /// Luật: miếng phải nằm gọn trong hộp hoặc ở hẳn bên ngoài.
        /// </summary>
        public static bool Straddles(TrayState tray, IReadOnlyList<Tri> tris)
        {
            var inTray = new HashSet<Tri>(tray.tris);
            var trayCells = new HashSet<Vector2Int>();
            foreach (var q in tray.tris) trayCells.Add(q.Cell);
            bool allIn = true, touches = false;
            foreach (var q in tris)
            {
                if (!inTray.Contains(q)) allIn = false;
                if (trayCells.Contains(q.Cell)) touches = true;
            }
            return !allIn && touches;
        }

        public static bool CanPlaceTray(BoardState s, int dx, int dy)
        {
            var carried = s.PiecesInsideTray();
            var movedCells = new HashSet<Vector2Int>();
            foreach (var q in s.tray.tris)
            {
                var m = q.Shift(dx, dy);
                if (!Geometry.InBoard(m)) return false;
                movedCells.Add(m.Cell);
            }
            // khay không được đè lên (hay chạm vào) miếng cơm nằm ngoài khay
            foreach (var p in s.pieces)
                if (!carried.Contains(p))
                    foreach (var q in p.tris)
                        if (movedCells.Contains(q.Cell)) return false;
            return true;
        }

        public static void MovePiece(BoardState s, Piece p, int dx, int dy) => p.tris = Geometry.Shift(p.tris, dx, dy);

        public static void MoveTray(BoardState s, int dx, int dy)
        {
            foreach (var p in s.PiecesInsideTray()) p.tris = Geometry.Shift(p.tris, dx, dy);
            s.tray.Shift(dx, dy);
        }

        public static WinState CheckWin(BoardState s)
        {
            var occ = s.Occupied();
            foreach (var q in s.tray.Need()) if (!occ.Contains(q)) return WinState.None;
            var wk = s.WallKeys();
            foreach (var p in s.pieces) if (Geometry.CrossesWall(p.tris, wk)) return WinState.WallBlocked;
            return WinState.Won;
        }
    }
}
