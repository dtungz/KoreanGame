using UnityEngine;

namespace KoreanGame.Core
{
    /// <summary>
    /// Vàng của người chơi (PlayerPrefs "gold"). Kiếm khi thắng màn, tiêu cho gợi ý thêm và đồ trang trí trong Cửa hàng.
    /// Mỗi lần đổi bắn GoldChangedEvent để chip vàng cập nhật.
    /// </summary>
    public static class GoldSave
    {
        public const string Key = "gold";

        // ---- kinh tế (đã duyệt) ----
        public const int WinFirst = 15;      // thắng màn lần đầu
        public const int WinReplay = 5;      // thắng lại màn đã qua
        public const int NewDishBonus = 40;  // mở món đặc trưng mới
        public const int HintPrice = 20;     // gợi ý thêm sau lượt miễn phí của màn

        public static int Gold => PlayerPrefs.GetInt(Key, 0);

        public static void Add(int amount)
        {
            if (amount <= 0) return;
            Set(Gold + amount);
        }

        /// <summary>Trừ vàng nếu đủ; false (không trừ) nếu thiếu.</summary>
        public static bool Spend(int amount)
        {
            if (amount < 0 || Gold < amount) return false;
            Set(Gold - amount);
            return true;
        }

        public static void Reset()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
            EventBus<GoldChangedEvent>.Raise(new GoldChangedEvent { gold = 0 });
        }

        static void Set(int v)
        {
            PlayerPrefs.SetInt(Key, v);
            PlayerPrefs.Save();
            EventBus<GoldChangedEvent>.Raise(new GoldChangedEvent { gold = v });
        }
    }
}
