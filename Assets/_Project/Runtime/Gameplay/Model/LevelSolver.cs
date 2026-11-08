using System.Collections.Generic;
using UnityEngine;

namespace KoreanGame.Gameplay
{
    public class SolveResult
    {
        public bool solved;
        public List<string> path;
        public int nodes;
        public int solutions;
        public bool capped;
    }

    public class LevelAnalysis
    {
        public bool solved;
        public List<string> path;
        public bool[] required;
        public bool trivial;
        public int score;
        public int nodes;
        public int solutions;
        public bool capped;

        public bool AllCardsRequired => System.Array.TrueForAll(required, r => r);
        public bool IsValid => solved && !trivial && AllCardsRequired;
    }

    /// <summary>
    /// Solver có hàng chờ thẻ (port levelkit.js): chỉ dùng thẻ đang ở 3 ô; dùng ô k thì thẻ kế tiếp vào ô k.
    /// Làm việc trên hình dạng miếng (không quan tâm vị trí trên thớt), rồi thử xếp kín khay chỉ bằng phép dời.
    /// </summary>
    public static class LevelSolver
    {
        class Abs
        {
            public List<Tri> need;
            public HashSet<EdgeKey> wk;
            public List<List<Tri>> rice;
        }

        static Abs LevelAbs(LevelData L)
        {
            var at = L.tray.at;
            var tray = Geometry.Shift(L.tray.tris, at.x, at.y);
            var fixedCells = new List<Vector2Int>();
            foreach (var f in L.tray.fixedFoods) foreach (var c in f.cells) fixedCells.Add(c + at);
            var fk = new HashSet<Tri>(Geometry.Full(fixedCells));
            var walls = new List<Wall>();
            foreach (var w in L.tray.walls) walls.Add(w.Shift(at.x, at.y));
            var a = new Abs { need = tray.FindAll(q => !fk.Contains(q)), wk = Geometry.WallKeys(walls), rice = new List<List<Tri>>() };
            foreach (var r in L.rice) a.rice.Add(Geometry.Normalize(Geometry.Shift(r.tris, r.at.x, r.at.y)));
            return a;
        }

        static string ShapesKey(List<List<Tri>> shapes)
        {
            var keys = new List<string>(shapes.Count);
            foreach (var s in shapes) keys.Add(Geometry.ShapeKey(s));
            keys.Sort(System.StringComparer.Ordinal);
            return string.Join("/", keys);
        }

        /// <summary>Mọi trạng thái hình dạng có thể đạt được sau khi dùng một thẻ.</summary>
        public static List<(string desc, List<List<Tri>> shapes)> Successors(CardDef def, List<List<Tri>> shapes)
        {
            var output = new List<(string, List<List<Tri>>)>();
            var seen = new HashSet<string>();

            void Push(string desc, List<List<Tri>> list)
            {
                var n = new List<List<Tri>>(list.Count);
                foreach (var s in list) n.Add(Geometry.Normalize(s));
                if (seen.Add(ShapesKey(n))) output.Add((desc, n));
            }

            List<List<Tri>> Rest(int i)
            {
                var r = new List<List<Tri>>(shapes.Count);
                for (int j = 0; j < shapes.Count; j++) if (j != i) r.Add(shapes[j]);
                return r;
            }

            for (int i = 0; i < shapes.Count; i++)
            {
                var S = shapes[i];
                switch (def.kind)
                {
                    case CardKind.Cut:
                        for (int r = 0; r < 4; r++)
                        {
                            var g = Geometry.CardGeom(def.Polys, r);
                            var b = Geometry.BBox(S);
                            for (int ox = b.minx - g.w; ox <= b.maxx + 1; ox++)
                                for (int oy = b.miny - g.h; oy <= b.maxy + 1; oy++)
                                {
                                    var parts = Geometry.Components(S, Geometry.SegKeys(Geometry.UnitSegments(g.polys, ox, oy)));
                                    if (parts.Count > 1)
                                    {
                                        var next = Rest(i);
                                        next.AddRange(parts);
                                        Push($"{def.displayName} xoay {r * 90}° tại ({ox},{oy}) lên miếng {i + 1}", next);
                                    }
                                }
                        }
                        break;
                    case CardKind.Rot:
                    case CardKind.Flip:
                        for (int d = 0; d < 2; d++)
                        {
                            var next = Rest(i);
                            next.Add(Geometry.TransformFn(def.kind, d, S));
                            string dir = def.kind == CardKind.Rot ? (d == 1 ? "↺" : "↻") : (d == 1 ? "dọc" : "ngang");
                            Push($"{def.displayName} {dir} miếng {i + 1}", next);
                        }
                        break;
                    case CardKind.Copy:
                        if (S.Count <= 8)
                        {
                            var next = new List<List<Tri>>(shapes) { S };
                            Push($"Nhân bản miếng {i + 1}", next);
                        }
                        break;
                    case CardKind.Glue:
                        for (int j = i + 1; j < shapes.Count; j++)
                        {
                            var T = shapes[j];
                            var a = Geometry.BBox(S);
                            var bb = Geometry.BBox(T);
                            var sk = new HashSet<Tri>(S);
                            var nb = new Neighbor[3];
                            for (int dx = a.minx - bb.maxx - 1; dx <= a.maxx + 1; dx++)
                                for (int dy = a.miny - bb.maxy - 1; dy <= a.maxy + 1; dy++)
                                {
                                    var M = Geometry.Shift(T, dx, dy);
                                    if (M.Exists(sk.Contains)) continue;
                                    var mk = new HashSet<Tri>(M);
                                    bool touch = false;
                                    foreach (var q in S) { Geometry.Neighbors(q, nb); if (mk.Contains(nb[2].tri)) { touch = true; break; } }
                                    if (!touch) continue;
                                    var next = new List<List<Tri>>();
                                    for (int k = 0; k < shapes.Count; k++) if (k != i && k != j) next.Add(shapes[k]);
                                    var merged = new List<Tri>(S);
                                    merged.AddRange(M);
                                    next.Add(merged);
                                    Push($"Dán miếng {i + 1} + {j + 1}", next);
                                }
                        }
                        break;
                }
            }
            return output;
        }

        /// <summary>Các miếng có xếp kín phần cần lấp của khay không (chỉ dời, không xoay, tôn trọng vách).</summary>
        public static bool PackShapes(List<List<Tri>> shapes, List<Tri> need, HashSet<EdgeKey> wk)
        {
            var target = new List<Tri>(need);
            target.Sort();
            var tset = new HashSet<Tri>(target);
            int area = 0;
            foreach (var p in shapes) area += p.Count;
            if (area < target.Count) return false;
            var covered = new HashSet<Tri>();
            var used = new bool[shapes.Count];
            var pieces = new List<List<Tri>>(shapes.Count);
            foreach (var p in shapes) { var c = new List<Tri>(p); c.Sort(); pieces.Add(c); }

            bool Rec()
            {
                int fi = target.FindIndex(q => !covered.Contains(q));
                if (fi < 0) return true;
                var first = target[fi];
                for (int i = 0; i < pieces.Count; i++)
                {
                    if (used[i]) continue;
                    var p = pieces[i];
                    var a = p[0];
                    if (a.t != first.t) continue;
                    var moved = Geometry.Shift(p, first.x - a.x, first.y - a.y);
                    if (!moved.TrueForAll(q => tset.Contains(q) && !covered.Contains(q)) || Geometry.CrossesWall(moved, wk)) continue;
                    used[i] = true;
                    foreach (var q in moved) covered.Add(q);
                    if (Rec()) return true;
                    used[i] = false;
                    foreach (var q in moved) covered.Remove(q);
                }
                return false;
            }

            return Rec();
        }

        public static SolveResult Solve(LevelData L, int skip = -1, bool all = false, int maxNodes = 400000, List<CardDef> cardsOverride = null)
        {
            var A = LevelAbs(L);
            var src = cardsOverride ?? L.cards;
            var cards = new List<CardDef>();
            for (int i = 0; i < src.Count; i++) if (i != skip) cards.Add(src[i]);
            var seen = new HashSet<string>();
            var res = new SolveResult();

            bool Rec(List<List<Tri>> shapes, int[] slots, int qp, List<string> path)
            {
                var key = string.Join(",", slots) + "|" + qp + "|" + ShapesKey(shapes);
                if (res.nodes > maxNodes) { res.capped = true; return false; }
                if (!seen.Add(key)) return false;
                res.nodes++;
                if (PackShapes(shapes, A.need, A.wk))
                {
                    res.solutions++;
                    if (res.path == null) res.path = path;
                    return !all;
                }
                for (int k = 0; k < slots.Length; k++)
                {
                    int i = slots[k];
                    if (i < 0) continue;
                    foreach (var (desc, next) in Successors(cards[i], shapes))
                    {
                        var ns = (int[])slots.Clone();
                        int nq = qp;
                        ns[k] = nq < cards.Count ? nq++ : -1;
                        if (Rec(next, ns, nq, new List<string>(path) { desc })) return true;
                    }
                }
                return false;
            }

            var s0 = new int[BoardState.SlotCount];
            for (int k = 0; k < s0.Length; k++) s0[k] = k < cards.Count ? k : -1;
            Rec(A.rice, s0, Mathf.Min(BoardState.SlotCount, cards.Count), new List<string>());
            res.solved = res.path != null;
            return res;
        }

        /// <summary>Độ khó: số trạng thái phải duyệt / số lời giải (log), cộng số thẻ.</summary>
        public static (int score, int nodes, int solutions, bool capped) Difficulty(LevelData L)
        {
            var r = Solve(L, all: true, maxNodes: 60000);
            int score = Mathf.RoundToInt(10 * Mathf.Log(1 + r.nodes / (float)Mathf.Max(1, r.solutions), 2) + 4 * L.cards.Count);
            return (score, r.nodes, r.solutions, r.capped);
        }

        public static LevelAnalysis Analyze(LevelData L)
        {
            var r = Solve(L);
            var a = new LevelAnalysis { solved = r.solved, path = r.path, required = new bool[L.cards.Count] };
            for (int i = 0; i < L.cards.Count; i++) a.required[i] = !Solve(L, skip: i, maxNodes: 120000).solved;
            a.trivial = Solve(L, cardsOverride: new List<CardDef>()).solved;
            (a.score, a.nodes, a.solutions, a.capped) = Difficulty(L);
            return a;
        }
    }
}
