using System.Collections.Generic;
using UnityEngine;

namespace KoreanGame.Gameplay
{
    /// <summary>
    /// Dựng mesh 2D theo toạ độ thớt (y hướng xuống) → local (x, -y). UV = toạ độ thớt để vân cơm lặp theo ô.
    /// Vẽ theo thứ tự thêm vào (shader Sprites không ghi depth).
    /// </summary>
    public class MeshBuilder
    {
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Color32> colors = new List<Color32>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<int>[] subs;

        public MeshBuilder(int subMeshCount = 1)
        {
            subs = new List<int>[subMeshCount];
            for (int i = 0; i < subMeshCount; i++) subs[i] = new List<int>();
        }

        public Vector2 Offset;

        int V(Vector2 p, Color c)
        {
            p += Offset;
            verts.Add(new Vector3(p.x, -p.y, 0));
            colors.Add(c);
            uvs.Add(new Vector2(p.x, -p.y));
            return verts.Count - 1;
        }

        public void Triangle(int sub, Vector2 a, Vector2 b, Vector2 c, Color col)
        {
            int i = V(a, col), j = V(b, col), k = V(c, col);
            subs[sub].Add(i); subs[sub].Add(j); subs[sub].Add(k);
        }

        public void Quad(int sub, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color col)
        {
            Triangle(sub, a, b, c, col);
            Triangle(sub, a, c, d, col);
        }

        public void Rect(int sub, float x, float y, float w, float h, Color col) =>
            Quad(sub, new Vector2(x, y), new Vector2(x + w, y), new Vector2(x + w, y + h), new Vector2(x, y + h), col);

        /// <summary>Tô tập tam giác; shrink &gt; 0 thì co mỗi tam giác về tâm.</summary>
        public void Tris(int sub, IEnumerable<Tri> tris, Color col, Vector2 shift = default, float shrink = 0)
        {
            foreach (var q in tris)
            {
                Geometry.TriPts(q, out var a, out var b, out var c);
                if (shrink > 0)
                {
                    var m = (a + b + c) / 3f;
                    a = Vector2.MoveTowards(a, m, shrink); b = Vector2.MoveTowards(b, m, shrink); c = Vector2.MoveTowards(c, m, shrink);
                }
                Triangle(sub, a + shift, b + shift, c + shift, col);
            }
        }

        public void Disc(int sub, Vector2 center, float r, Color col, int seg = 10)
        {
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * Mathf.PI * 2 / seg, a1 = (i + 1) * Mathf.PI * 2 / seg;
                Triangle(sub, center, center + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * r, center + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * r, col);
            }
        }

        /// <summary>Đoạn thẳng dày, đầu tròn.</summary>
        public void Line(int sub, Vector2 p, Vector2 q, float width, Color col, bool round = true)
        {
            var d = q - p;
            if (d.sqrMagnitude < 1e-6f) return;
            var n = new Vector2(-d.y, d.x).normalized * (width / 2);
            Quad(sub, p + n, q + n, q - n, p - n, col);
            if (round) { Disc(sub, p, width / 2, col, 8); Disc(sub, q, width / 2, col, 8); }
        }

        public void DashedLine(int sub, Vector2 p, Vector2 q, float width, Color col, float dash, float gap)
        {
            float len = Vector2.Distance(p, q);
            var dir = (q - p) / len;
            for (float s = 0; s < len; s += dash + gap)
                Line(sub, p + dir * s, p + dir * Mathf.Min(len, s + dash), width, col);
        }

        public void Lines(int sub, List<(Vector2 p, Vector2 q)> segs, float width, Color col, Vector2 shift = default)
        {
            foreach (var (p, q) in segs) Line(sub, p + shift, q + shift, width, col);
        }

        public void Apply(Mesh m)
        {
            m.Clear();
            m.indexFormat = verts.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            m.SetVertices(verts);
            m.SetColors(colors);
            m.SetUVs(0, uvs);
            m.subMeshCount = subs.Length;
            for (int i = 0; i < subs.Length; i++) m.SetTriangles(subs[i], i);
            m.RecalculateBounds();
        }
    }

    public static class Palette
    {
        public static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

        public static readonly Color Grid = Hex("#DEC8A8");
        public static readonly Color RiceLip = Hex("#CDB797");
        public static readonly Color RiceEdge = Hex("#D9CAB2");
        public static readonly Color TinWell = Hex("#B3A396");
        public static readonly Color TinWellShadow = Hex("#8C7F74");
        public static readonly Color TinWall = Hex("#DACEC3");
        public static readonly Color TinRim = Hex("#E5D8CD");
        public static readonly Color TinRimShadow = Hex("#C4B8AC");
        public static readonly Color TinOutline = Hex("#968C84");
        public static readonly Color CutValid = Hex("#E2563F");
        public static readonly Color CutInvalid = Hex("#8C7B6E");
        public static readonly Color Green = Hex("#3E9A4C");
        public static readonly Color Red = Hex("#D6332A");

        public static readonly Color[] Tint =
        {
            new Color(240 / 255f, 110 / 255f, 60 / 255f, .42f),
            new Color(80 / 255f, 140 / 255f, 230 / 255f, .38f),
            new Color(90 / 255f, 180 / 255f, 100 / 255f, .38f),
            new Color(210 / 255f, 170 / 255f, 40 / 255f, .42f),
        };

        public static Color FamilyColor(CardFamily f)
        {
            switch (f)
            {
                case CardFamily.Mold: return Hex("#E8873C");
                case CardFamily.Diag: return Hex("#8A63D6");
                case CardFamily.Glue: return Green;
                default: return Hex("#E2675A");
            }
        }
    }
}
