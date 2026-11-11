using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KoreanGame.Gameplay
{
    /// <summary>
    /// Lớp mờ phủ cả khung, chừa các lỗ sáng chữ nhật (toạ độ local của chính nó). Chia lưới theo mép các lỗ,
    /// chỉ vẽ ô không nằm trong lỗ nào. Không nhận chạm (raycastTarget = false).
    /// </summary>
    public class DimGraphic : MaskableGraphic
    {
        readonly List<Rect> holes = new List<Rect>();

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        public void SetHoles(List<Rect> list)
        {
            holes.Clear();
            holes.AddRange(list);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            var xs = new List<float> { r.xMin, r.xMax };
            var ys = new List<float> { r.yMin, r.yMax };
            foreach (var h in holes)
            {
                xs.Add(Mathf.Clamp(h.xMin, r.xMin, r.xMax)); xs.Add(Mathf.Clamp(h.xMax, r.xMin, r.xMax));
                ys.Add(Mathf.Clamp(h.yMin, r.yMin, r.yMax)); ys.Add(Mathf.Clamp(h.yMax, r.yMin, r.yMax));
            }
            xs.Sort(); ys.Sort();
            var c = (Color32)color;
            for (int i = 0; i + 1 < xs.Count; i++)
                for (int j = 0; j + 1 < ys.Count; j++)
                {
                    float x0 = xs[i], x1 = xs[i + 1], y0 = ys[j], y1 = ys[j + 1];
                    if (x1 - x0 < .01f || y1 - y0 < .01f) continue;
                    var mid = new Vector2((x0 + x1) / 2, (y0 + y1) / 2);
                    bool inHole = false;
                    foreach (var h in holes) if (h.Contains(mid)) { inHole = true; break; }
                    if (inHole) continue;
                    int v = vh.currentVertCount;
                    vh.AddVert(new Vector3(x0, y0), c, Vector2.zero);
                    vh.AddVert(new Vector3(x0, y1), c, Vector2.zero);
                    vh.AddVert(new Vector3(x1, y1), c, Vector2.zero);
                    vh.AddVert(new Vector3(x1, y0), c, Vector2.zero);
                    vh.AddTriangle(v, v + 1, v + 2);
                    vh.AddTriangle(v, v + 2, v + 3);
                }
        }
    }
}
