using System;
using System.Collections.Generic;
using UnityEngine;

namespace KoreanGame.Gameplay
{
    public enum ShopCategory { Box, Board, Theme }

    /// <summary>Một món trang trí trong Cửa hàng: mở theo tiến độ (qua đủ màn), rồi mua bằng vàng (price).</summary>
    [Serializable]
    public class ShopItem
    {
        [Tooltip("Mã cố định (lưu lựa chọn của người chơi)")]
        public string id;
        public string displayName;
        public ShopCategory category;
        [Tooltip("Số màn phải qua để mở (0 = có sẵn)")]
        public int unlockAfter;
        [Tooltip("Giá vàng sau khi đã mở (0 = miễn phí)")]
        public int price;
        [Tooltip("Ảnh xem trước trong Cửa hàng (nhân với iconTint)")]
        public Sprite icon;
        public Color iconTint = Color.white;

        [Header("Hộp: vành, viền, lòng ngăn (để trống = hộp thiếc gốc)")]
        public bool customBox;
        public Color boxRim = Color.white, boxOutline = Color.gray, boxWell = Color.gray;

        [Header("Thớt: màu nhân lên khung thớt")]
        public Color boardTint = Color.white;

        [Header("Theme: nền Main Menu")]
        public Sprite menuBackground;
    }

    /// <summary>Danh mục Cửa hàng: hộp cơm, thớt, theme menu. Món đầu tiên mỗi loại là mặc định.</summary>
    [CreateAssetMenu(menuName = "KoreanGame/Shop Catalog", fileName = "ShopCatalog")]
    public class ShopCatalog : ScriptableObject
    {
        public List<ShopItem> items = new List<ShopItem>();

        public List<ShopItem> Of(ShopCategory c) => items.FindAll(i => i != null && i.category == c);

        public ShopItem Default(ShopCategory c) => items.Find(i => i != null && i.category == c);

        public ShopItem Find(string id) => items.Find(i => i != null && i.id == id);
    }
}
