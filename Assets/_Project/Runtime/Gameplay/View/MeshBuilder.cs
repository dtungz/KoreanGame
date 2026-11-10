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
        public static readonly Color RiceLip = Hex("#C9A27E");
        public static readonly Color RiceEdge = Hex("#81533B");   // viền nâu cho khối thức ăn (style guide)
        // màu hộp: đổi được theo skin hộp trong Cửa hàng (SetTraySkin); mặc định = hộp thiếc
        public static Color TinWell { get; private set; } = Hex("#B3A396");
        public static Color TinWellShadow { get; private set; } = Hex("#8C7F74");
        public static Color TinWall { get; private set; } = Hex("#DACEC3");
        public static Color TinRim { get; private set; } = Hex("#E5D8CD");
        public static Color TinRimShadow { get; private set; } = Hex("#C4B8AC");
        public static Color TinOutline { get; private set; } = Hex("#968C84");

        /// <summary>Về hộp thiếc mặc định (đúng màu gốc).</summary>
        public static void ResetTraySkin()
        {
            TinWell = Hex("#B3A396"); TinWellShadow = Hex("#8C7F74"); TinWall = Hex("#DACEC3");
            TinRim = Hex("#E5D8CD"); TinRimShadow = Hex("#C4B8AC"); TinOutline = Hex("#968C84");
        }

        /// <summary>Đổi màu hộp từ 3 màu gốc (vành, viền, lòng ngăn); các màu còn lại suy ra để giữ độ nổi khối.</summary>
        public static void SetTraySkin(Color rim, Color outline, Color well)
        {
            TinRim = rim;
            TinOutline = outline;
            TinWell = well;
            TinWall = Color.Lerp(rim, well, .3f);
            TinRimShadow = Color.Lerp(rim, outline, .4f);
            TinWellShadow = Color.Lerp(well, outline, .45f);
        }
        // Xem trước cắt (style guide Bento Journey): đường cắt nét đứt màu mực có chấm hai đầu; thả hỏng = san hô
        public static readonly Color CutValid = Hex("#3E2416");
        public static readonly Color CutInvalid = Hex("#E97855");
        public static readonly Color Green = Hex("#3E9A4C");
        public static readonly Color Red = Hex("#D6332A");

        // Style guide Bento Journey (Cute Casual 2D): tên cũ giữ nguyên để code hiện có không đổi
        public static readonly Color UiCream = Hex("#FFF8EC");       // kem sáng: panel, thẻ, nút phụ
        public static readonly Color UiCreamDark = Hex("#FFF1D9");   // kem: nền màn hình
        public static readonly Color UiPeach = Hex("#FFD9AD");       // đào: khay thẻ, bong bóng
        public static readonly Color UiOrange = Hex("#F58B42");      // cam: nút chính, thân xe
        public static readonly Color UiCoral = Hex("#E97855");       // san hô: logo, badge, đường cắt
        public static readonly Color UiSage = Hex("#A8B96B");        // xanh lá: cây, nhãn hoàn thành
        public static readonly Color UiSky = Hex("#94C8E8");         // xanh trời: đang chọn, xem trước cắt
        public static readonly Color UiBrown = Hex("#81533B");       // nâu: viền, bóng, chữ phụ
        public static readonly Color UiText = Hex("#3E2416");        // mực: chữ chính (cả trên nút cam / san hô)

        // Các phần sắp tách: phần đầu xanh trời (như style guide), phần sau khác màu để vẫn đếm được số miếng (GDD 8)
        public static readonly Color[] Tint =
        {
            new Color(148 / 255f, 200 / 255f, 232 / 255f, .55f),
            new Color(245 / 255f, 139 / 255f, 66 / 255f, .42f),
            new Color(168 / 255f, 185 / 255f, 107 / 255f, .5f),
            new Color(233 / 255f, 120 / 255f, 85 / 255f, .42f),
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
