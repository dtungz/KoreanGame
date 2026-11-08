using UnityEngine;
using UnityEngine.UI;

namespace KoreanGame.Gameplay
{
    /// <summary>Hình đường cắt in trên thẻ: lưới 3×3 + đường cắt theo màu nhóm thẻ (giống cardArt của prototype).</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class CardArtGraphic : MaskableGraphic
    {
        const int N = 3;
        CardDef def;
        int rot;

        public void Set(CardDef d, int r)
        {
            def = d;
            rot = r;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (def == null || def.IsFunction) return;
            var rect = rectTransform.rect;
            float span = N + .9f;
            float unit = Mathf.Min(rect.width, rect.height) / span;
            var origin = new Vector2(rect.center.x - N * unit / 2, rect.center.y + N * unit / 2);
            Vector2 P(float x, float y) => origin + new Vector2(x * unit, -y * unit);

            var gridCol = new Color32(0xE6, 0xD6, 0xC2, 0xFF);
            for (int i = 0; i <= N; i++)
            {
                Line(vh, P(i, 0), P(i, N), .05f * unit, gridCol);
                Line(vh, P(0, i), P(N, i), .05f * unit, gridCol);
            }

            var g = Geometry.CardGeom(def.Polys, rot);
            int ox = Mathf.FloorToInt((N - g.w) / 2f), oy = Mathf.FloorToInt((N - g.h) / 2f);
            var col = Palette.FamilyColor(def.family);
            foreach (var pl in g.polys)
            {
                if (def.family == CardFamily.Mold && pl.Length >= 4)
                {
                    var fill = new Color(240 / 255f, 160 / 255f, 80 / 255f, .3f);
                    for (int i = 1; i < pl.Length - 2; i++)
                        Tri(vh, P(pl[0].x + ox, pl[0].y + oy), P(pl[i].x + ox, pl[i].y + oy), P(pl[i + 1].x + ox, pl[i + 1].y + oy), fill);
                }
                for (int i = 0; i < pl.Length - 1; i++)
                {
                    var a = P(pl[i].x + ox, pl[i].y + oy);
                    var b = P(pl[i + 1].x + ox, pl[i + 1].y + oy);
                    if (def.family == CardFamily.Glue) Dotted(vh, a, b, .22f * unit, col, .35f * unit);
                    else Line(vh, a, b, .22f * unit, col);
                }
                foreach (var p in pl) Disc(vh, P(p.x + ox, p.y + oy), .11f * unit, col);
            }
        }

        static void Tri(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color col)
        {
            int i = vh.currentVertCount;
            vh.AddVert(a, col, Vector2.zero);
            vh.AddVert(b, col, Vector2.zero);
            vh.AddVert(c, col, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
        }

        static void Line(VertexHelper vh, Vector2 a, Vector2 b, float w, Color col)
        {
            var d = b - a;
            if (d.sqrMagnitude < 1e-6f) return;
            var n = new Vector2(-d.y, d.x).normalized * (w / 2);
            Tri(vh, a + n, b + n, b - n, col);
            Tri(vh, a + n, b - n, a - n, col);
        }

        static void Dotted(VertexHelper vh, Vector2 a, Vector2 b, float w, Color col, float step)
        {
            float len = Vector2.Distance(a, b);
            for (float s = 0; s <= len + .01f; s += step) Disc(vh, Vector2.Lerp(a, b, s / len), w / 2, col);
        }

        static void Disc(VertexHelper vh, Vector2 c, float r, Color col)
        {
            const int seg = 10;
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * Mathf.PI * 2 / seg, a1 = (i + 1) * Mathf.PI * 2 / seg;
                Tri(vh, c, c + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * r, c + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * r, col);
            }
        }
    }
}
