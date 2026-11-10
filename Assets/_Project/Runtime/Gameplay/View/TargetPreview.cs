using System.Collections.Generic;
using KoreanGame.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KoreanGame.Gameplay
{
    /// <summary>
    /// Hình "Mẫu" thu nhỏ của màn: chính là hình khay (mục tiêu của game), ô cần lấp cơm màu trắng,
    /// món cố định màu thật, vách và viền màu nâu. Chỉ đọc dữ liệu, không đụng luật chơi.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class TargetPreview : MaskableGraphic
    {
        static readonly Color Rice = new Color32(0xFF, 0xFD, 0xF6, 0xFF);
        static readonly Color Line = new Color32(0x81, 0x53, 0x3B, 0xFF);

        LevelController controller;
        TrayState tray;
        bool locked;

        /// <summary>Vẽ khay của một level bất kỳ (bộ sưu tập). locked = chỉ hiện bóng mờ màu nâu.</summary>
        public void SetTray(TrayData data, bool isLocked)
        {
            tray = new TrayState { tris = new List<Tri>(data.tris), walls = new List<Wall>(data.walls) };
            foreach (var f in data.fixedFoods) tray.fixedFoods.Add(f.Clone());
            locked = isLocked;
            SetVerticesDirty();
        }

        public void Init(LevelController c)
        {
            controller = c;
            EventBus<LevelLoadedEvent>.Subscribe(OnLoaded);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            EventBus<LevelLoadedEvent>.Unsubscribe(OnLoaded);
        }

        void OnLoaded(LevelLoadedEvent e)
        {
            tray = controller.State.tray.Clone();   // khay có thể bị kéo đi, hình mẫu thì cố định theo màn
            SetVerticesDirty();
        }

        static Color FoodColor(string kind) =>
            kind == "sausage" ? new Color32(0xE9, 0x78, 0x55, 0xFF)
            : kind == "meat" ? new Color32(0xF2, 0xA7, 0xA0, 0xFF)
            : new Color32(0xF8, 0xD4, 0x5C, 0xFF);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (tray == null || tray.tris.Count == 0) return;
            var b = Geometry.BBox(tray.tris);
            var rect = rectTransform.rect;
            float unit = Mathf.Min(rect.width / b.W, rect.height / b.H);
            var origin = new Vector2(rect.center.x - b.W * unit / 2, rect.center.y + b.H * unit / 2);
            Vector2 P(Vector2 p) => origin + new Vector2((p.x - b.minx) * unit, -(p.y - b.miny) * unit);

            var food = new Dictionary<Tri, Color>();
            foreach (var f in tray.fixedFoods)
                foreach (var q in Geometry.Full(f.cells)) food[q] = FoodColor(f.kind);

            var shadow = new Color(Line.r, Line.g, Line.b, .28f);
            foreach (var q in tray.tris)
            {
                Geometry.TriPts(q, out var a, out var c, out var m);
                CardArtGraphic.Tri(vh, P(a), P(c), P(m), locked ? shadow : food.TryGetValue(q, out var col) ? col : Rice);
            }
            if (locked) return;   // khay chưa mở: chỉ bóng, không lộ chi tiết
            // lưới mờ trong khay
            var cells = new HashSet<Vector2Int>();
            foreach (var q in tray.tris) cells.Add(q.Cell);
            var grid = new Color(Line.r, Line.g, Line.b, .18f);
            foreach (var cl in cells)
            {
                CardArtGraphic.Line(vh, P(new Vector2(cl.x, cl.y)), P(new Vector2(cl.x + 1, cl.y)), unit * .05f, grid);
                CardArtGraphic.Line(vh, P(new Vector2(cl.x, cl.y)), P(new Vector2(cl.x, cl.y + 1)), unit * .05f, grid);
            }
            foreach (var (p, q) in Geometry.Boundary(new List<Tri>(food.Keys))) CardArtGraphic.Line(vh, P(p), P(q), unit * .07f, Line);
            foreach (var w in tray.walls) CardArtGraphic.Line(vh, P(new Vector2(w.x1, w.y1)), P(new Vector2(w.x2, w.y2)), unit * .12f, Line);
            foreach (var (p, q) in Geometry.Boundary(tray.tris)) CardArtGraphic.Line(vh, P(p), P(q), unit * .1f, Line);
        }
    }
}
