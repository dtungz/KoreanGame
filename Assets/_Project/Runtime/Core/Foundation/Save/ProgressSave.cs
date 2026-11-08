using UnityEngine;

namespace KoreanGame.Core
{
    /// <summary>Tiến độ chơi: level hiện tại (index từ 0).</summary>
    public static class ProgressSave
    {
        public const string Key = "progress.level";

        public static int CurrentLevel
        {
            get => PlayerPrefs.GetInt(Key, 0);
            set { PlayerPrefs.SetInt(Key, value); PlayerPrefs.Save(); }
        }

        public static void Reset()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }
}
