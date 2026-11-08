using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace KoreanGame.Gameplay
{
    /// <summary>Hình học lưới tam giác, port từ prototype play.html.</summary>
    public static class Geometry
    {
        public const int Cols = 7, Rows = 10;

        public static readonly int[] T4 = { 0, 1, 2, 3 };

        public static void Neighbors(Tri q, Neighbor[] buf)
        {
            int x = q.x, y = q.y, t = q.t;
            bool even = t % 2 == 0;
            buf[0] = new Neighbor(new Tri(x, y, (t + 1) % 4), new EdgeKey(even ? EdgeKind.A : EdgeKind.D, x, y));
            buf[1] = new Neighbor(new Tri(x, y, (t + 3) % 4), new EdgeKey(even ? EdgeKind.D : EdgeKind.A, x, y));
            switch (t)
            {
                case 0: buf[2] = new Neighbor(new Tri(x, y - 1, 2), new EdgeKey(EdgeKind.H, x, y)); break;
                case 1: buf[2] = new Neighbor(new Tri(x + 1, y, 3), new EdgeKey(EdgeKind.V, x + 1, y)); break;
                case 2: buf[2] = new Neighbor(new Tri(x, y + 1, 0), new EdgeKey(EdgeKind.H, x, y + 1)); break;
                default: buf[2] = new Neighbor(new Tri(x - 1, y, 1), new EdgeKey(EdgeKind.V, x, y)); break;
            }
        }

        public static Neighbor[] Neighbors(Tri q)
        {
            var buf = new Neighbor[3];
            Neighbors(q, buf);
            return buf;
        }

        /// <summary>Các cặp tam giác nằm hai bên một cạnh.</summary>
        public static List<(Tri a, Tri b)> EdgePairs(EdgeKey k)
        {
            int x = k.x, y = k.y;
            var r = new List<(Tri, Tri)>(2);
            switch (k.kind)
            {
                case EdgeKind.H: r.Add((new Tri(x, y - 1, 2), new Tri(x, y, 0))); break;
                case EdgeKind.V: r.Add((new Tri(x - 1, y, 1), new Tri(x, y, 3))); break;
                case EdgeKind.D: r.Add((new Tri(x, y, 1), new Tri(x, y, 2))); r.Add((new Tri(x, y, 3), new Tri(x, y, 0))); break;
                default: r.Add((new Tri(x, y, 0), new Tri(x, y, 1))); r.Add((new Tri(x, y, 2), new Tri(x, y, 3))); break;
            }
            return r;
        }

        /// <summary>Tách tập tam giác thành các khối liền, không đi qua cạnh bị chặn.</summary>
        public static List<List<Tri>> Components(IReadOnlyList<Tri> tris, HashSet<EdgeKey> blocked)
        {
            var set = new HashSet<Tri>(tris);
            var seen = new HashSet<Tri>();
            var output = new List<List<Tri>>();
            var stack = new Stack<Tri>();
            var nb = new Neighbor[3];
            foreach (var q in tris)
            {
                if (!seen.Add(q)) continue;
                var comp = new List<Tri>();
                stack.Push(q);
                while (stack.Count > 0)
                {
                    var c = stack.Pop();
                    comp.Add(c);
                    Neighbors(c, nb);
                    for (int i = 0; i < 3; i++)
                    {
                        var n = nb[i];
                        if (set.Contains(n.tri) && !(blocked != null && blocked.Contains(n.edge)) && seen.Add(n.tri)) stack.Push(n.tri);
                    }
                }
                output.Add(comp);
            }
            return output;
        }

        public static List<Seg> UnitSegments(IReadOnlyList<Vector2Int[]> polys, int ox, int oy)
        {
            var segs = new List<Seg>();
            foreach (var pl in polys)
            {
                for (int i = 0; i < pl.Length - 1; i++)
                {
                    int x1 = pl[i].x, y1 = pl[i].y, x2 = pl[i + 1].x, y2 = pl[i + 1].y;
                    int dx = System.Math.Sign(x2 - x1), dy = System.Math.Sign(y2 - y1);
                    while (x1 != x2 || y1 != y2)
                    {
                        segs.Add(new Seg(x1 + ox, y1 + oy, x1 + dx + ox, y1 + dy + oy));
                        x1 += dx; y1 += dy;
                    }
                }
            }
            return segs;
        }

        public static HashSet<EdgeKey> SegKeys(IEnumerable<Seg> segs)
        {
            var s = new HashSet<EdgeKey>();
            foreach (var g in segs)
            {
                int x = Mathf.Min(g.x1, g.x2), y = Mathf.Min(g.y1, g.y2);
                if (g.y1 == g.y2) s.Add(new EdgeKey(EdgeKind.H, x, y));
                else if (g.x1 == g.x2) s.Add(new EdgeKey(EdgeKind.V, x, y));
                else s.Add(new EdgeKey((g.x2 - g.x1) * (g.y2 - g.y1) > 0 ? EdgeKind.D : EdgeKind.A, x, y));
            }
            return s;
        }

        public static HashSet<EdgeKey> WallKeys(IEnumerable<Wall> walls)
        {
            var polys = new List<Vector2Int[]>();
            foreach (var w in walls) polys.Add(new[] { new Vector2Int(w.x1, w.y1), new Vector2Int(w.x2, w.y2) });
            return SegKeys(UnitSegments(polys, 0, 0));
        }

        public static BBox BBox(IReadOnlyList<Tri> tris)
        {
            var b = new BBox { minx = int.MaxValue, miny = int.MaxValue, maxx = int.MinValue, maxy = int.MinValue };
            foreach (var q in tris)
            {
                if (q.x < b.minx) b.minx = q.x;
                if (q.x > b.maxx) b.maxx = q.x;
                if (q.y < b.miny) b.miny = q.y;
                if (q.y > b.maxy) b.maxy = q.y;
            }
            return b;
        }

        public static List<Tri> Shift(IReadOnlyList<Tri> tris, int dx, int dy)
        {
            var r = new List<Tri>(tris.Count);
            foreach (var q in tris) r.Add(q.Shift(dx, dy));
            return r;
        }

        public static List<Tri> Normalize(IReadOnlyList<Tri> tris)
        {
            var b = BBox(tris);
            return Shift(tris, -b.minx, -b.miny);
        }

        public static List<Tri> RotCW(IReadOnlyList<Tri> tris)
        {
            var r = new List<Tri>(tris.Count);
            foreach (var q in tris) r.Add(new Tri(-q.y - 1, q.x, (q.t + 1) % 4));
            return r;
        }

        public static List<Tri> FlipH(IReadOnlyList<Tri> tris)
        {
            var r = new List<Tri>(tris.Count);
            foreach (var q in tris) r.Add(new Tri(-q.x - 1, q.y, q.t % 2 == 1 ? 4 - q.t : q.t));
            return r;
        }

        public static List<Tri> FlipV(IReadOnlyList<Tri> tris)
        {
            var r = new List<Tri>(tris.Count);
            foreach (var q in tris) r.Add(new Tri(q.x, -q.y - 1, q.t % 2 == 1 ? q.t : 2 - q.t));
            return r;
        }

        /// <summary>Xoay (dir 0 = theo chiều kim đồng hồ, 1 = ngược) hoặc lật (dir 0 = ngang, 1 = dọc).</summary>
        public static List<Tri> TransformFn(CardKind kind, int dir, IReadOnlyList<Tri> tris)
        {
            if (kind == CardKind.Rot) return dir == 1 ? RotCW(RotCW(RotCW(tris))) : RotCW(tris);
            return dir == 1 ? FlipV(tris) : FlipH(tris);
        }

        public static List<Tri> Full(IEnumerable<Vector2Int> cells)
        {
            var r = new List<Tri>();
            foreach (var c in cells) for (int t = 0; t < 4; t++) r.Add(new Tri(c.x, c.y, t));
            return r;
        }

        public static List<Tri> FullRect(int w, int h)
        {
            var r = new List<Tri>();
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) for (int t = 0; t < 4; t++) r.Add(new Tri(x, y, t));
            return r;
        }

        public struct CardGeometry
        {
            public List<Vector2Int[]> polys;
            public int w, h;
        }

        /// <summary>Đường cắt của thẻ sau khi xoay rot × 90°, dời về gốc (0, 0).</summary>
        public static CardGeometry CardGeom(IReadOnlyList<Vector2Int[]> src, int rot)
        {
            var polys = new List<Vector2Int[]>(src.Count);
            foreach (var pl in src) polys.Add((Vector2Int[])pl.Clone());
            for (int r = 0; r < rot; r++)
                foreach (var pl in polys)
                    for (int i = 0; i < pl.Length; i++) pl[i] = new Vector2Int(-pl[i].y, pl[i].x);
            int mx = int.MaxValue, my = int.MaxValue, Mx = int.MinValue, My = int.MinValue;
            foreach (var pl in polys) foreach (var p in pl) { mx = Mathf.Min(mx, p.x); my = Mathf.Min(my, p.y); }
            foreach (var pl in polys)
                for (int i = 0; i < pl.Length; i++)
                {
                    pl[i] = new Vector2Int(pl[i].x - mx, pl[i].y - my);
                    Mx = Mathf.Max(Mx, pl[i].x); My = Mathf.Max(My, pl[i].y);
                }
            return new CardGeometry { polys = polys, w = Mx, h = My };
        }

        /// <summary>Tam giác chứa điểm (wx, wy) theo toạ độ thớt.</summary>
        public static Tri TriAt(float wx, float wy)
        {
            int x = Mathf.FloorToInt(wx), y = Mathf.FloorToInt(wy);
            float fx = wx - x, fy = wy - y;
            int t = fy < fx ? (fy < 1 - fx ? 0 : 1) : (fy < 1 - fx ? 3 : 2);
            return new Tri(x, y, t);
        }

        public static bool CrossesWall(IReadOnlyList<Tri> tris, HashSet<EdgeKey> wallKeys)
        {
            if (wallKeys == null || wallKeys.Count == 0) return false;
            var s = new HashSet<Tri>(tris);
            var nb = new Neighbor[3];
            foreach (var q in tris)
            {
                Neighbors(q, nb);
                for (int i = 0; i < 3; i++) if (wallKeys.Contains(nb[i].edge) && s.Contains(nb[i].tri)) return true;
            }
            return false;
        }

        public static bool InBoard(Tri q) => q.x >= 0 && q.x < Cols && q.y >= 0 && q.y < Rows;

        /// <summary>Khoá hình dạng (không phụ thuộc thứ tự), dùng cho solver.</summary>
        public static string ShapeKey(IReadOnlyList<Tri> tris)
        {
            var l = new List<Tri>(tris);
            l.Sort();
            var sb = new StringBuilder(l.Count * 7);
            foreach (var q in l) sb.Append(q.x).Append(',').Append(q.y).Append(',').Append(q.t).Append(';');
            return sb.ToString();
        }

        /// <summary>3 đỉnh của tam giác, toạ độ thớt (y hướng xuống).</summary>
        public static void TriPts(Tri q, out Vector2 a, out Vector2 b, out Vector2 c)
        {
            a = Corner(q.x, q.y, q.t);
            b = Corner(q.x, q.y, (q.t + 1) % 4);
            c = new Vector2(q.x + .5f, q.y + .5f);
        }

        static Vector2 Corner(int x, int y, int i)
        {
            switch (i)
            {
                case 0: return new Vector2(x, y);
                case 1: return new Vector2(x + 1, y);
                case 2: return new Vector2(x + 1, y + 1);
                default: return new Vector2(x, y + 1);
            }
        }

        /// <summary>Các cạnh ngoài của một tập tam giác (để vẽ viền).</summary>
        public static List<(Vector2 p, Vector2 q)> Boundary(IReadOnlyList<Tri> tris)
        {
            var s = new HashSet<Tri>(tris);
            var output = new List<(Vector2, Vector2)>();
            var nb = new Neighbor[3];
            foreach (var q in tris)
            {
                TriPts(q, out var a, out var b, out var c);
                Neighbors(q, nb);
                for (int i = 0; i < 3; i++)
                {
                    var n = nb[i].tri;
                    if (s.Contains(n)) continue;
                    if (n.x == q.x && n.y == q.y) output.Add(n.t == (q.t + 1) % 4 ? (b, c) : (a, c));
                    else output.Add((a, b));
                }
            }
            return output;
        }
    }
}
