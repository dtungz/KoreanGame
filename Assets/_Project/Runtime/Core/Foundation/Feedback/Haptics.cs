using UnityEngine;

namespace KoreanGame.Core
{
    /// <summary>Rung ngắn trên Android (VibrationEffect, API 26+). Editor / nền tảng khác: không làm gì.</summary>
    public static class Haptics
    {
        public static bool Enabled = true;

        public static void Light() => Vibrate(12, 60);
        public static void Medium() => Vibrate(22, 140);
        public static void Heavy() => Vibrate(45, 255);

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject vibrator;
        static int sdk = -1;

        static void Vibrate(long ms, int amplitude)
        {
            if (!Enabled) return;
            try
            {
                if (vibrator == null)
                {
                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                        vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    using (var ver = new AndroidJavaClass("android.os.Build$VERSION")) sdk = ver.GetStatic<int>("SDK_INT");
                }
                if (vibrator == null) return;
                // tham chiếu Handheld.Vibrate để Unity tự thêm quyền VIBRATE vào AndroidManifest (nhánh này không bao giờ chạy)
                if (Time.frameCount < 0) Handheld.Vibrate();
                if (sdk >= 26)
                {
                    using (var fx = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var effect = fx.CallStatic<AndroidJavaObject>("createOneShot", ms, amplitude))
                        vibrator.Call("vibrate", effect);
                }
                else vibrator.Call("vibrate", ms);
            }
            catch (System.Exception) { Enabled = false; }   // máy không hỗ trợ: tắt luôn, không spam log
        }
#else
        static void Vibrate(long ms, int amplitude) { }
#endif
    }
}
