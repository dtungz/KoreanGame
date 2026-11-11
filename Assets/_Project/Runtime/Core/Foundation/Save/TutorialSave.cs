using UnityEngine;

namespace KoreanGame.Core
{
    /// <summary>Tutorial nào đã xem (xong hoặc bỏ qua): PlayerPrefs "tut.&lt;id&gt;" = 1. Mỗi mechanic chỉ dạy một lần.</summary>
    public static class TutorialSave
    {
        const string Prefix = "tut.";

        public static bool IsDone(string id) => PlayerPrefs.GetInt(Prefix + id, 0) == 1;

        public static void MarkDone(string id)
        {
            PlayerPrefs.SetInt(Prefix + id, 1);
            PlayerPrefs.Save();
        }

        /// <summary>Xem lại hướng dẫn: xoá cờ của các tutorial cho trước.</summary>
        public static void Reset(System.Collections.Generic.IEnumerable<string> ids)
        {
            foreach (var id in ids) PlayerPrefs.DeleteKey(Prefix + id);
            PlayerPrefs.Save();
        }
    }
}
