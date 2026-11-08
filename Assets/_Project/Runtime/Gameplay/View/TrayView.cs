using System.Collections.Generic;
using UnityEngine;

namespace KoreanGame.Gameplay
{
    /// <summary>Khay hộp sắt: vành bo tròn quanh hình khay, ngăn lõm theo ô, món cố định, vách ngăn, quai hai bên.</summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class TrayView : MonoBehaviour
    {
        const float Rim = 9f / 44f;

        [SerializeField] MeshFilter meshFilter;
        [SerializeField] MeshRenderer meshRenderer;
        [SerializeField] SpriteRenderer foodTemplate;
        [SerializeField] SpriteRenderer latchLeft;
        [SerializeField] SpriteRenderer latchRight;
        [SerializeField] Sprite egg, sausage, meat;

        Mesh mesh;
        readonly List<SpriteRenderer> foods = new List<SpriteRenderer>();

        void OnValidate()
        {
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        }

        public void Init(Material colorMat)
        {
            mesh = new Mesh { name = "Tray" };
            meshFilter.sharedMesh = mesh;
            meshRenderer.sharedMaterial = colorMat;
            foodTemplate.gameObject.SetActive(false);
        }

        Sprite FoodSprite(string kind) => kind == "sausage" ? sausage : kind == "meat" ? meat : egg;

        public void Build(TrayState tray)
        {
            var mb = new MeshBuilder(1);
            var tris = tray.tris;
            var edge = Geometry.Boundary(tris);

            void Shape(Color c, float width, Vector2 shift)
            {
                mb.Tris(0, tris, c, shift);
                mb.Lines(0, edge, width, c, shift);
            }

            Shape(new Color(.35f, .27f, .22f, .2f), 2 * Rim + .1f, new Vector2(0, .12f));
            Shape(Palette.TinOutline, 2 * Rim + .1f, Vector2.zero);
            Shape(Palette.TinRimShadow, 2 * Rim, Vector2.zero);
            Shape(Palette.TinRim, 2 * Rim - .06f, new Vector2(0, -.05f));
            mb.Tris(0, tris, Palette.TinWall);

            var fixedCells = new HashSet<Vector2Int>();
            foreach (var f in tray.fixedFoods) foreach (var c in f.cells) fixedCells.Add(c);

            var byCell = new Dictionary<Vector2Int, List<Tri>>();
            foreach (var q in tris)
            {
                if (!byCell.TryGetValue(q.Cell, out var l)) byCell[q.Cell] = l = new List<Tri>();
                l.Add(q);
            }
            foreach (var kv in byCell)
            {
                if (fixedCells.Contains(kv.Key)) continue;
                if (kv.Value.Count == 4) Well(mb, kv.Key.x, kv.Key.y, 1, 1);
                else
                {
                    mb.Tris(0, kv.Value, Palette.TinWellShadow, Vector2.zero, .1f);
                    mb.Tris(0, kv.Value, Palette.TinWell, new Vector2(0, .05f), .12f);
                }
            }

            int fi = 0;
            foreach (var f in tray.fixedFoods)
            {
                int x0 = int.MaxValue, y0 = int.MaxValue, x1 = int.MinValue, y1 = int.MinValue;
                foreach (var c in f.cells) { x0 = Mathf.Min(x0, c.x); y0 = Mathf.Min(y0, c.y); x1 = Mathf.Max(x1, c.x); y1 = Mathf.Max(y1, c.y); }
                float w = x1 - x0 + 1, h = y1 - y0 + 1;
                Well(mb, x0, y0, w, h);
                var sr = Food(fi++);
                sr.sprite = FoodSprite(f.kind);
                FitSprite(sr, new Vector2(x0 + w / 2, y0 + h / 2), new Vector2(w - .28f, h - .3f));
            }
            for (int i = fi; i < foods.Count; i++) foods[i].gameObject.SetActive(false);

            foreach (var w in tray.walls)
                mb.Line(0, new Vector2(w.x1, w.y1), new Vector2(w.x2, w.y2), .14f, Palette.TinOutline);

            mb.Apply(mesh);
            PlaceLatches(byCell.Keys);
        }

        static void Well(MeshBuilder mb, float x, float y, float w, float h)
        {
            mb.Rect(0, x + .1f, y + .1f, w - .2f, h - .2f, Palette.TinWellShadow);
            mb.Rect(0, x + .1f, y + .16f, w - .2f, h - .26f, Palette.TinWell);
        }

        SpriteRenderer Food(int i)
        {
            while (foods.Count <= i) foods.Add(Instantiate(foodTemplate, transform));
            foods[i].gameObject.SetActive(true);
            return foods[i];
        }

        /// <summary>Đặt sprite vừa khung (toạ độ thớt), giữ tỉ lệ.</summary>
        static void FitSprite(SpriteRenderer sr, Vector2 center, Vector2 box)
        {
            var size = sr.sprite.bounds.size;
            float s = Mathf.Min(box.x / size.x, box.y / size.y);
            sr.transform.localScale = new Vector3(s, s, 1);
            sr.transform.localPosition = new Vector3(center.x, -center.y, 0);
        }

        void PlaceLatches(IEnumerable<Vector2Int> cells)
        {
            int minx = int.MaxValue, maxx = int.MinValue;
            var ys = new Dictionary<int, (int lo, int hi)>();
            foreach (var c in cells)
            {
                minx = Mathf.Min(minx, c.x); maxx = Mathf.Max(maxx, c.x);
                ys[c.x] = ys.TryGetValue(c.x, out var r) ? (Mathf.Min(r.lo, c.y), Mathf.Max(r.hi, c.y)) : (c.y, c.y);
            }
            if (minx == int.MaxValue) return;
            float Mid(int col) => (ys[col].lo + ys[col].hi + 1) / 2f;
            const float lh = .85f;
            float lw = lh * 61f / 202f;
            FitSprite(latchLeft, new Vector2(minx - Rim - .01f - lw / 2, Mid(minx)), new Vector2(lw, lh));
            FitSprite(latchRight, new Vector2(maxx + 1 + Rim + .01f + lw / 2, Mid(maxx)), new Vector2(lw, lh));
            latchRight.flipX = true;
        }

        public void SetOrder(int baseOrder)
        {
            meshRenderer.sortingOrder = baseOrder;
            foreach (var f in foods) f.sortingOrder = baseOrder + 1;
            latchLeft.sortingOrder = latchRight.sortingOrder = baseOrder + 1;
        }
    }
}
