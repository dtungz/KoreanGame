using UnityEngine;

namespace KoreanGame.Shared
{
    /// <summary>
    /// Khớp khung thiết kế dọc 390×844 (1 ô thớt = 44 px) vào vùng an toàn của màn hình (tai thỏ, thanh điều hướng).
    /// Camera thớt và các khung UI được thu / dời cùng một tỉ lệ nên thớt và tay bài luôn thẳng hàng,
    /// không bị cắt ở máy hẹp hơn 9:19,5. Lớp tối của popup nằm ngoài khung nên vẫn phủ kín màn hình.
    /// </summary>
    public class DesignFrameFitter : MonoBehaviour
    {
        const float RefW = 390, RefH = 844, Cell = 44;

        [SerializeField] Camera cam;
        [Tooltip("Khung 390×844 neo giữa, cha phủ kín canvas Screen Space Overlay")]
        [SerializeField] RectTransform[] frames;

        [Header("Màn chơi (tuỳ chọn): thanh trên neo đỉnh, khay thẻ neo đáy, thớt co vừa khoảng giữa")]
        [SerializeField] RectTransform topFrame;
        [SerializeField] float topHeight = 152;
        [SerializeField] RectTransform bottomFrame;
        [SerializeField] float bottomHeight = 208;
        [Tooltip("Tâm và kích thước khung thớt theo toạ độ thế giới")]
        [SerializeField] Vector2 boardCenter, boardSize;

        Rect lastSafe;
        Vector2Int lastSize;
        float lastScale = -1;

        // CanvasScaler cập nhật scaleFactor trong Update, nên đo trong LateUpdate; chỉ tính lại khi có thay đổi
        void LateUpdate()
        {
            var safe = Screen.safeArea;
            var size = new Vector2Int(Screen.width, Screen.height);
            float s = frames.Length > 0 && frames[0] != null ? frames[0].parent.lossyScale.x : 1f;
            if (safe == lastSafe && size == lastSize && Mathf.Approximately(s, lastScale)) return;
            lastSafe = safe;
            lastSize = size;
            lastScale = s;
            Apply(safe, size);
        }

        void Apply(Rect safe, Vector2Int size)
        {
            if (safe.width <= 0 || safe.height <= 0) return;
            float px = Mathf.Min(safe.width / RefW, safe.height / RefH);   // pixel màn hình trên 1 đơn vị thiết kế
            var offset = safe.center - new Vector2(size.x, size.y) / 2f;    // tâm vùng an toàn lệch khỏi tâm màn hình (pixel)

            var screenCenter = new Vector2(size.x, size.y) / 2f;
            bool split = topFrame != null && bottomFrame != null && boardSize.x > 0 && boardSize.y > 0;

            if (cam != null && !split)
            {
                float unitsPerPx = 1f / (Cell * px);
                cam.orthographicSize = size.y / 2f * unitsPerPx;
                cam.transform.position = new Vector3(-offset.x * unitsPerPx, -offset.y * unitsPerPx, cam.transform.position.z);
            }

            // canvas Overlay: đơn vị thế giới = pixel, nên lossyScale của cha = số pixel trên 1 đơn vị canvas
            foreach (var f in frames)
            {
                if (f == null) continue;
                float s = f.parent.lossyScale.x;
                if (s <= 0) continue;
                f.localScale = Vector3.one * (px / s);
                f.anchoredPosition = offset / s;
            }

            if (!split) return;
            Place(topFrame, px, new Vector2(safe.center.x, safe.yMax - topHeight * px / 2), screenCenter);
            Place(bottomFrame, px, new Vector2(safe.center.x, safe.yMin + bottomHeight * px / 2), screenCenter);

            // thớt: vừa khoảng giữa hai khung (chừa 3% mỗi chiều), tâm thớt đặt vào tâm khoảng đó
            float midBottom = safe.yMin + bottomHeight * px, midTop = safe.yMax - topHeight * px;
            float k = Mathf.Min((midTop - midBottom) * .97f / boardSize.y, safe.width * .97f / boardSize.x);   // pixel / đơn vị thế giới
            if (cam == null || k <= 0) return;
            var midCenter = new Vector2(safe.center.x, (midTop + midBottom) / 2);
            cam.orthographicSize = size.y / (2f * k);
            var camPos = boardCenter - (midCenter - screenCenter) / k;
            cam.transform.position = new Vector3(camPos.x, camPos.y, cam.transform.position.z);
        }

        static void Place(RectTransform f, float px, Vector2 centerPx, Vector2 screenCenter)
        {
            float s = f.parent.lossyScale.x;
            if (s <= 0) return;
            f.localScale = Vector3.one * (px / s);
            f.anchoredPosition = (centerPx - screenCenter) / s;
        }
    }
}
