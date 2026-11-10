using UnityEngine;

namespace KoreanGame.Core
{
    /// <summary>Cài đặt người chơi: âm thanh, rung (mặc định bật).</summary>
    public static class SettingsSave
    {
        const string SoundKey = "settings.sound";
        const string VibrationKey = "settings.vibration";

        public static bool Sound
        {
            get => PlayerPrefs.GetInt(SoundKey, 1) == 1;
            set { PlayerPrefs.SetInt(SoundKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool Vibration
        {
            get => PlayerPrefs.GetInt(VibrationKey, 1) == 1;
            set { PlayerPrefs.SetInt(VibrationKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }
    }
}
