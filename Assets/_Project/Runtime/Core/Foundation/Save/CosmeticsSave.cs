using UnityEngine;

namespace KoreanGame.Core
{
    /// <summary>Món trang trí đang dùng theo từng loại (hộp, thớt, theme), lưu theo id.</summary>
    public static class CosmeticsSave
    {
        const string Prefix = "cosmetics.";

        public static string Get(string category) => PlayerPrefs.GetString(Prefix + category, "");

        /// <summary>Đồ trang trí đã mua bằng vàng (PlayerPrefs "owned.&lt;id&gt;").</summary>
        public static bool Owned(string id) => PlayerPrefs.GetInt("owned." + id, 0) == 1;

        public static void Own(string id)
        {
            PlayerPrefs.SetInt("owned." + id, 1);
            PlayerPrefs.Save();
        }

        public static void Set(string category, string id)
        {
            PlayerPrefs.SetString(Prefix + category, id);
            PlayerPrefs.Save();
        }
    }
}
