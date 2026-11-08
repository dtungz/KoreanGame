using KoreanGame.Core;
using KoreanGame.Gameplay;
using UnityEditor;
using UnityEngine;

namespace KoreanGame.EditorTools
{
    /// <summary>
    /// Debug chuyển level, chỉ nằm trong menu Tools (không hiện trên màn hình game, không vào bản build).
    /// Dùng khi đang Play. Phím tắt: Ctrl+Alt+→ / ← chuyển màn, Ctrl+Alt+R chơi lại, Ctrl+Alt+W thắng ngay.
    /// </summary>
    public static class LevelDebugMenu
    {
        const string Root = "Tools/KoreanGame/Debug/";

        public static LevelController Controller => Application.isPlaying ? Object.FindObjectOfType<LevelController>() : null;

        static bool Playing() => Controller != null && Controller.State != null;

        [MenuItem(Root + "Next Level %&RIGHT", false, 1)]
        public static void Next() => Controller?.Next();

        [MenuItem(Root + "Previous Level %&LEFT", false, 2)]
        public static void Prev() => Controller?.Prev();

        [MenuItem(Root + "Restart Level %&r", false, 3)]
        public static void Restart() => Controller?.Restart();

        [MenuItem(Root + "Win Now %&w", false, 20)]
        public static void Win() => Controller?.ForceWin();

        [MenuItem(Root + "Log Solution", false, 21)]
        public static void LogSolution()
        {
            var c = Controller;
            var r = LevelSolver.Solve(c.Level);
            Debug.Log(r.solved
                ? $"[Debug] Level {c.Index + 1} lời giải: {string.Join(" → ", r.path)}"
                : $"[Debug] Level {c.Index + 1}: solver không tìm được lời giải!");
        }

        [MenuItem(Root + "Go To Level… (Level Browser)", false, 40)]
        public static void GoTo() => LevelBrowserWindow.Open();

        [MenuItem(Root + "Reset Progress", false, 60)]
        public static void ResetProgress()
        {
            ProgressSave.Reset();
            Controller?.Load(0);
            Debug.Log("[Debug] Đã xoá tiến độ, về Level 1.");
        }

        [MenuItem(Root + "Next Level %&RIGHT", true)]
        [MenuItem(Root + "Previous Level %&LEFT", true)]
        [MenuItem(Root + "Restart Level %&r", true)]
        [MenuItem(Root + "Win Now %&w", true)]
        [MenuItem(Root + "Log Solution", true)]
        static bool ValidatePlaying() => Playing();
    }
}
