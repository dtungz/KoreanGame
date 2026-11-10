using UnityEngine;

namespace KoreanGame.Core
{
    /// <summary>
    /// Bộ sưu tập: món đặc trưng đã mở và món phụ đã gặp (theo id). Chỉ thêm, không bao giờ mất khi chơi lại màn cũ.
    /// Lưu dạng "|id1|id2|" trong PlayerPrefs.
    /// </summary>
    public static class CollectionSave
    {
        public const string DishKey = "collection.dishes";
        public const string SideKey = "collection.sides";

        public static bool HasDish(string id) => Has(DishKey, id);
        public static bool HasSide(string id) => Has(SideKey, id);

        /// <summary>Thêm món đặc trưng; true nếu là lần đầu (cần hiện popup món mới).</summary>
        public static bool AddDish(string id) => Add(DishKey, id);
        public static bool AddSide(string id) => Add(SideKey, id);

        public static void Reset()
        {
            PlayerPrefs.DeleteKey(DishKey);
            PlayerPrefs.DeleteKey(SideKey);
            PlayerPrefs.Save();
        }

        // luôn bắt đầu bằng "|" (kể cả khi key có sẵn nhưng rỗng) để tìm "|id|" không nhầm chuỗi con
        static string Get(string key)
        {
            var s = PlayerPrefs.GetString(key, "");
            return s.StartsWith("|") ? s : "|" + s;
        }

        static bool Has(string key, string id) => !string.IsNullOrEmpty(id) && Get(key).Contains("|" + id + "|");

        static bool Add(string key, string id)
        {
            if (string.IsNullOrEmpty(id) || Has(key, id)) return false;
            PlayerPrefs.SetString(key, Get(key) + id + "|");
            PlayerPrefs.Save();
            return true;
        }
    }
}
