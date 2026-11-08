using System;
using System.Collections.Generic;
using KoreanGame.Gameplay;
using UnityEngine;

namespace KoreanGame.EditorTools
{
    /// <summary>Thông số sinh một màn: khối cơm chữ nhật, dãy thẻ áp dụng theo thứ tự, món cố định, số vách.</summary>
    [Serializable]
    public class GenSpec
    {
        public int riceW = 4, riceH = 2;
        public List<Vector2Int> riceCells = new List<Vector2Int>();   // khác rỗng: khối cơm hình tuỳ ý thay cho chữ nhật
        public List<string> cards = new List<string>();   // tên asset thẻ, vd "Cut_L", "Fn_Rotate"
        public string fixedKind = "";                      // "" | egg | sausage | meat
        public int walls;
        public bool silhouette;
        public bool manual;                                // level làm tay, generator bỏ qua
        public string hint = "";
    }

    public class GenCandidate
    {
        public int seed;
        public LevelAnalysis analysis;
        public LevelData level;   // ScriptableObject tạm (chưa lưu)
    }

    /// <summary>
    /// Sinh màn ngược (port genLevels của levelkit.js): từ khối cơm chữ nhật, áp ngẫu nhiên từng thẻ để chia miếng,
    /// ráp các miếng thành hình khay, thêm món cố định / vách, rồi cho solver lọc: giải được, cần thẻ, không thẻ thừa.
    /// </summary>
    public static class LevelGenerator
    {
        class Rng
        {
            uint s;
            public Rng(int seed) { s = (uint)seed; if (s == 0) s = 1; }
            public float Next() { s = unchecked(s * 1664525u + 1013904223u); return s / 4294967296f; }
            public int Range(int n) => Mathf.Min(n - 1, (int)(Next() * n));
            public void Shuffle<T>(IList<T> l) { for (int i = l.Count - 1; i > 0; i--) { int j = Range(i + 1); (l[i], l[j]) = (l[j], l[i]); } }
        }

        static List<List<Tri>> ApplyRandom(CardDef def, List<List<Tri>> pieces, Rng R)
        {
            for (int attempt = 0; attempt < 300; attempt++)
            {
                int i = R.Range(pieces.Count);
                var S = pieces[i];
                var rest = new List<List<Tri>>(pieces);
                rest.RemoveAt(i);
                switch (def.kind)
                {
                    case CardKind.Cut:
                    {
                        var g = Geometry.CardGeom(def.Polys, R.Range(4));
                        var b = Geometry.BBox(S);
                        int ox = b.minx - g.w + R.Range(b.maxx - b.minx + g.w + 2);
                        int oy = b.miny - g.h + R.Range(b.maxy - b.miny + g.h + 2);
                        var parts = Geometry.Components(S, Geometry.SegKeys(Geometry.UnitSegments(g.polys, ox, oy)));
                        if (parts.Count > 1 && parts.TrueForAll(p => p.Count >= 2))
                        {
                            foreach (var p in parts) rest.Add(Geometry.Normalize(p));
                            return rest;
                        }
                        break;
                    }
                    case CardKind.Rot:
                    case CardKind.Flip:
                    {
                        var T = Geometry.Normalize(Geometry.TransformFn(def.kind, R.Range(2), S));
                        if (Geometry.ShapeKey(T) != Geometry.ShapeKey(Geometry.Normalize(S))) { rest.Add(T); return rest; }
                        break;
                    }
                    case CardKind.Copy:
                        if (S.Count <= 8) return new List<List<Tri>>(pieces) { S };
                        break;
                    case CardKind.Glue:
                        if (pieces.Count > 1)
                        {
                            var opts = LevelSolver.Successors(def, pieces);
                            if (opts.Count > 0) return opts[R.Range(opts.Count)].shapes;
                        }
                        break;
                }
            }
            return null;
        }

        static List<Tri> PackTray(List<List<Tri>> pieces, Rng R, int maxW, int maxH)
        {
            var placed = new List<Tri>(pieces[0]);
            var order = pieces.GetRange(1, pieces.Count - 1);
            R.Shuffle(order);
            var nb = new Neighbor[3];
            foreach (var P in order)
            {
                var b = Geometry.BBox(placed);
                var pb = Geometry.BBox(P);
                var occ = new HashSet<Tri>(placed);
                float bestScore = float.MaxValue;
                List<Tri> best = null;
                for (int dx = b.minx - pb.maxx - 1; dx <= b.maxx + 1; dx++)
                    for (int dy = b.miny - pb.maxy - 1; dy <= b.maxy + 1; dy++)
                    {
                        var M = Geometry.Shift(P, dx, dy);
                        if (M.Exists(occ.Contains)) continue;
                        var mk = new HashSet<Tri>(M);
                        int touch = 0;
                        foreach (var q in placed) { Geometry.Neighbors(q, nb); if (mk.Contains(nb[2].tri)) touch++; }
                        if (touch == 0) continue;
                        var all = new List<Tri>(placed); all.AddRange(M);
                        var nbb = Geometry.BBox(all);
                        if (nbb.W > maxW || nbb.H > maxH) continue;
                        float score = nbb.W * nbb.H * 3 - touch * 2 + R.Next();
                        if (score < bestScore) { bestScore = score; best = M; }
                    }
                if (best == null) return null;
                placed.AddRange(best);
            }
            return Geometry.Normalize(placed);
        }

        static List<Tri> RiceShape(GenSpec spec) =>
            spec.riceCells != null && spec.riceCells.Count > 0
                ? Geometry.Normalize(Geometry.Full(spec.riceCells))
                : Geometry.FullRect(spec.riceW, spec.riceH);

        public static List<GenCandidate> Generate(GenSpec spec, Dictionary<string, CardDef> cardSet, int want, int seed0, int tries, Func<float, bool> progress = null)
        {
            var defs = spec.cards.ConvertAll(n => cardSet[n]);
            var output = new List<GenCandidate>();
            for (int s = seed0; s < seed0 + tries && output.Count < want; s++)
            {
                if (progress != null && progress((s - seed0) / (float)tries)) break;
                var R = new Rng(s);
                var rice = RiceShape(spec);
                var pieces = new List<List<Tri>> { rice };
                bool ok = true;
                foreach (var d in defs)
                {
                    var n = ApplyRandom(d, pieces, R);
                    if (n == null) { ok = false; break; }
                    pieces = n;
                }
                if (!ok) continue;

                bool hasFixed = !string.IsNullOrEmpty(spec.fixedKind);
                var tray = PackTray(pieces, R, 5, hasFixed ? 3 : 4);
                if (tray == null) continue;
                var tb = Geometry.BBox(tray);
                var cells = new Dictionary<Vector2Int, int>();
                foreach (var q in tray) cells[q.Cell] = cells.TryGetValue(q.Cell, out var c) ? c + 1 : 1;
                int Count(int x, int y) => cells.TryGetValue(new Vector2Int(x, y), out var c) ? c : 0;

                var traySet = new List<Tri>(tray);
                var fixedFoods = new List<FixedFood>();
                if (hasFixed)
                {
                    int len = spec.fixedKind == "egg" ? 1 : 2, y = tb.maxy + 1;
                    var xs = new List<int>();
                    for (int x = tb.minx; x <= tb.maxx - len + 1; x++)
                    {
                        bool full = true;
                        for (int k = 0; k < len; k++) if (Count(x + k, tb.maxy) != 4) full = false;
                        if (full) xs.Add(x);
                    }
                    if (xs.Count == 0) continue;
                    int x0 = xs[R.Range(xs.Count)];
                    var fc = new List<Vector2Int>();
                    for (int k = 0; k < len; k++) fc.Add(new Vector2Int(x0 + k, y));
                    traySet.AddRange(Geometry.Full(fc));
                    fixedFoods.Add(new FixedFood { kind = spec.fixedKind, cells = fc });
                }

                var walls = new List<Wall>();
                if (spec.walls > 0)
                {
                    var cand = new List<Wall>();
                    foreach (var kv in cells)
                        if (kv.Value == 4 && Count(kv.Key.x + 1, kv.Key.y) == 4)
                            cand.Add(new Wall(kv.Key.x + 1, kv.Key.y, kv.Key.x + 1, kv.Key.y + 1));
                    R.Shuffle(cand);
                    walls.AddRange(cand.GetRange(0, Mathf.Min(spec.walls, cand.Count)));
                    if (walls.Count < spec.walls) continue;
                }

                int W = tb.W, H = (hasFixed ? tb.maxy + 2 : tb.maxy + 1) - tb.miny;
                var L = ScriptableObject.CreateInstance<LevelData>();
                L.hint = spec.hint;
                var rb = Geometry.BBox(rice);
                L.rice.Add(new RicePiece { tris = rice, at = new Vector2Int((Geometry.Cols - rb.W) / 2, 9 - rb.H) });
                L.tray = new TrayData { tris = traySet, at = new Vector2Int((Geometry.Cols - W) / 2, 1), walls = walls, fixedFoods = fixedFoods };
                L.cards = new List<CardDef>(defs);
                L.unlocksSilhouette = spec.silhouette;

                var a = LevelSolver.Analyze(L);
                if (!a.IsValid) { UnityEngine.Object.DestroyImmediate(L); continue; }
                L.difficulty = a.score;
                L.solutions = a.solutions;
                L.solution = string.Join(" → ", a.path);
                output.Add(new GenCandidate { seed = s, analysis = a, level = L });
            }
            output.Sort((x, y) => x.analysis.score.CompareTo(y.analysis.score));
            return output;
        }
    }
}
