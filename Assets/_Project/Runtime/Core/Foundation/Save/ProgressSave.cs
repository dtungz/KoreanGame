using UnityEngine;

namespace KoreanGame.Core
{
    /// <summary>
    /// Tiến độ chơi (index từ 0), hai giá trị tách riêng:
    /// - CurrentLevel: màn đang chơi / chơi tiếp (đổi khi chơi lại màn cũ, quay về 0 sau màn cuối).
    /// - UnlockedLevel: số màn đã mở = màn cao nhất đã qua + 1, chỉ tăng. Bộ sưu tập, sổ hành trình dựa vào đây.
    /// </summary>
    public static class ProgressSave
    {
        public const string Key = "progress.level";
        public const string UnlockedKey = "progress.unlocked";

        public static int CurrentLevel
        {
            get => PlayerPrefs.GetInt(Key, 0);
            set { PlayerPrefs.SetInt(Key, value); PlayerPrefs.Save(); }
        }

        /// <summary>Số màn đã qua (màn i đã qua khi i &lt; UnlockedLevel). Save cũ chưa có thì lấy theo CurrentLevel.</summary>
        public static int UnlockedLevel
        {
            get
            {
                Migrate();
                return PlayerPrefs.GetInt(UnlockedKey);
            }
            private set { PlayerPrefs.SetInt(UnlockedKey, value); PlayerPrefs.Save(); }
        }

        /// <summary>Save cũ: chép tiến độ sang UnlockedLevel trước khi CurrentLevel bị ghi đè (gọi lúc nạp màn).</summary>
        public static void Migrate()
        {
            if (!PlayerPrefs.HasKey(UnlockedKey)) UnlockedLevel = CurrentLevel;
        }

        public static bool IsCleared(int index) => index < UnlockedLevel;

        /// <summary>Gọi khi thắng màn index: mở màn kế, không bao giờ làm tụt tiến độ.</summary>
        public static void MarkCleared(int index)
        {
            if (index + 1 > UnlockedLevel) UnlockedLevel = index + 1;
        }

        public static void Reset()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.DeleteKey(UnlockedKey);
            PlayerPrefs.Save();
        }
    }
}
