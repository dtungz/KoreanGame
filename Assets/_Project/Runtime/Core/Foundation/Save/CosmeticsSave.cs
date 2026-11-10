using UnityEngine;

namespace KoreanGame.Core
{
    /// <summary>Món trang trí đang dùng theo từng loại (hộp, thớt, theme), lưu theo id.</summary>
    public static class CosmeticsSave
    {
        const string Prefix = "cosmetics.";

        public static string Get(string category) => PlayerPrefs.GetString(Prefix + category, "");

        public static void Set(string category, string id)
        {
            PlayerPrefs.SetString(Prefix + category, id);
            PlayerPrefs.Save();
        }
    }
}
