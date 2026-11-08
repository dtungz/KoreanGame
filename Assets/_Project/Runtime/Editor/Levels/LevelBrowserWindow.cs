using System.Collections.Generic;
using KoreanGame.App;
using KoreanGame.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KoreanGame.EditorTools
{
    /// <summary>
    /// Cửa sổ KoreanGame/Level Browser: xem 40 level (hình thu nhỏ cơm → khay), thẻ, độ khó;
    /// chơi thử từ level bất kỳ, kiểm tra cả bộ bằng solver, sinh lại level tự động.
    /// </summary>
    public class LevelBrowserWindow : EditorWindow
    {
        LevelDatabase db;
        Vector2 scroll;
        readonly Dictionary<LevelData, string> status = new Dictionary<LevelData, string>();
        int regenSeed = 1000;

        [MenuItem("Tools/KoreanGame/Level Browser %#l")]
        public static void Open() => GetWindow<LevelBrowserWindow>("Level Browser");

        void OnEnable() => db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(LevelImporter.DatabasePath);

        void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                db = (LevelDatabase)EditorGUILayout.ObjectField(db, typeof(LevelDatabase), false, GUILayout.Width(200));
                if (GUILayout.Button("Validate all", EditorStyles.toolbarButton)) ValidateAll();
                if (GUILayout.Button("Import 1-14", EditorStyles.toolbarButton)) LevelImporter.ImportPrototype();
                if (GUILayout.Button("Generate 15-40", EditorStyles.toolbarButton) &&
                    EditorUtility.DisplayDialog("Sinh lại", "Sinh lại toàn bộ level 15–40? Các level hiện tại sẽ bị ghi đè.", "Sinh", "Huỷ"))
                    LevelBatchGenerator.GenerateAll();
                GUILayout.FlexibleSpace();
            }
            DrawPlayControls();
            if (db == null) { EditorGUILayout.HelpBox("Chưa có LevelDatabase. Bấm Import 1-14.", MessageType.Warning); return; }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            for (int i = 0; i < db.Count; i++) DrawRow(i, db.levels[i]);
            EditorGUILayout.EndScrollView();
        }

        string goText = "";
        string solutionText = "";

        /// <summary>Hàng điều khiển debug khi đang Play (thay cho panel trên màn hình game).</summary>
        void DrawPlayControls()
        {
            var c = LevelDebugMenu.Controller;
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                if (c == null || c.State == null)
                {
                    GUILayout.Label("Debug: bấm ▶ Play from here ở một level, hoặc Play scene Main để điều khiển level.", EditorStyles.miniLabel);
                    return;
                }
                GUILayout.Label($"Đang chơi: Level {c.Index + 1}/{c.Count}" + (c.Won ? " (đã thắng)" : ""), EditorStyles.boldLabel, GUILayout.Width(190));
                if (GUILayout.Button("◀ Prev", GUILayout.Width(60))) { c.Prev(); solutionText = ""; }
                if (GUILayout.Button("Next ▶", GUILayout.Width(60))) { c.Next(); solutionText = ""; }
                goText = EditorGUILayout.TextField(goText, GUILayout.Width(40));
                if (GUILayout.Button("Go", GUILayout.Width(36)) && int.TryParse(goText, out int n) && n >= 1 && n <= c.Count) { c.Load(n - 1); solutionText = ""; }
                if (GUILayout.Button("Chơi lại", GUILayout.Width(64))) c.Restart();
                if (GUILayout.Button("Thắng ngay", GUILayout.Width(80))) c.ForceWin();
                if (GUILayout.Button("Lời giải", GUILayout.Width(64)))
                {
                    var r = LevelSolver.Solve(c.Level);
                    solutionText = r.solved ? string.Join(" → ", r.path) : "Solver không tìm được lời giải!";
                }
                if (GUILayout.Button("Xoá tiến độ", GUILayout.Width(80))) LevelDebugMenu.ResetProgress();
            }
            if (!string.IsNullOrEmpty(solutionText)) EditorGUILayout.HelpBox(solutionText, MessageType.Info);
        }

        void OnInspectorUpdate() { if (Application.isPlaying) Repaint(); }

        void DrawRow(int i, LevelData L)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox, GUILayout.Height(84)))
            {
                var thumb = GUILayoutUtility.GetRect(56, 80, GUILayout.Width(56), GUILayout.Height(80));
                if (L != null && Event.current.type == EventType.Repaint) DrawThumb(thumb, L);

                using (new EditorGUILayout.VerticalScope())
                {
                    if (L == null) { GUILayout.Label($"Level {i + 1}: (trống)"); return; }
                    var tags = (L.isTutorial ? " · màn dạy" : "") + (L.unlocksSilhouette ? " · ★ khay hình bóng" : "");
                    GUILayout.Label($"Level {i + 1}  ({L.name}){tags}", EditorStyles.boldLabel);
                    GUILayout.Label("Thẻ: " + string.Join(", ", L.cards.ConvertAll(c => c != null ? c.displayName : "?")), EditorStyles.miniLabel);
                    GUILayout.Label($"Độ khó {L.difficulty} · {L.solutions} lời giải" + (status.TryGetValue(L, out var s) ? " · " + s : ""), EditorStyles.miniLabel);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("▶ Play from here", GUILayout.Width(120))) PlayFrom(i);
                        if (GUILayout.Button("Select", GUILayout.Width(60))) { Selection.activeObject = L; EditorGUIUtility.PingObject(L); }
                        if (GUILayout.Button("Check", GUILayout.Width(60))) Check(L);
                        if (!L.isTutorial && i + 1 >= LevelBatchGenerator.FirstGenerated && GUILayout.Button("Regenerate", GUILayout.Width(90))) Regenerate(i);
                    }
                }
            }
        }

        static void DrawThumb(Rect r, LevelData L)
        {
            EditorGUI.DrawRect(r, new Color(.96f, .88f, .74f));
            float u = Mathf.Min(r.width / Geometry.Cols, r.height / Geometry.Rows);
            Vector3 P(Vector2 p) => new Vector3(r.x + p.x * u, r.y + p.y * u);

            void Fill(List<Tri> tris, Vector2Int at, Color col)
            {
                Handles.color = col;
                foreach (var q in tris)
                {
                    Geometry.TriPts(q.Shift(at.x, at.y), out var a, out var b, out var c);
                    Handles.DrawAAConvexPolygon(P(a), P(b), P(c));
                }
            }

            Fill(L.tray.tris, L.tray.at, new Color(.55f, .5f, .46f));
            foreach (var f in L.tray.fixedFoods)
                Fill(Geometry.Full(f.cells), L.tray.at, new Color(.95f, .6f, .2f));
            foreach (var w in L.tray.walls)
            {
                Handles.color = Color.black;
                Handles.DrawAAPolyLine(2, P(new Vector2(w.x1 + L.tray.at.x, w.y1 + L.tray.at.y)), P(new Vector2(w.x2 + L.tray.at.x, w.y2 + L.tray.at.y)));
            }
            foreach (var rp in L.rice) Fill(rp.tris, rp.at, Color.white);
        }

        static void PlayFrom(int index)
        {
            if (Application.isPlaying)
            {
                var c = FindObjectOfType<LevelController>();
                if (c != null) c.Load(index);
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (EditorSceneManager.GetActiveScene().path != SceneBuilder.ScenePath) EditorSceneManager.OpenScene(SceneBuilder.ScenePath);
            EditorPrefs.SetInt(GameBootstrap.PlayFromLevelPref, index);
            EditorApplication.isPlaying = true;
        }

        void Check(LevelData L)
        {
            var a = LevelSolver.Analyze(L);
            L.difficulty = a.score;
            L.solutions = a.solutions;
            L.solution = a.path != null ? string.Join(" → ", a.path) : "";
            EditorUtility.SetDirty(L);
            var extra = new List<string>();
            for (int k = 0; k < L.cards.Count; k++) if (!a.required[k]) extra.Add(L.cards[k].displayName);
            status[L] = !a.solved ? "✖ KHÔNG giải được" : a.trivial ? "✖ không cần thẻ" :
                extra.Count > 0 ? (L.isTutorial ? "✔ (thẻ bẫy: " : "⚠ thẻ thừa: ") + string.Join(", ", extra) + (L.isTutorial ? ")" : "") : "✔ hợp lệ";
        }

        void ValidateAll()
        {
            try
            {
                for (int i = 0; i < db.Count; i++)
                {
                    if (db.levels[i] == null) continue;
                    if (EditorUtility.DisplayCancelableProgressBar("Validate", $"Level {i + 1}", i / (float)db.Count)) break;
                    Check(db.levels[i]);
                }
            }
            finally { EditorUtility.ClearProgressBar(); }
            AssetDatabase.SaveAssets();
        }

        void Regenerate(int index)
        {
            var specs = LevelBatchGenerator.LoadSpecs();
            int si = index + 1 - LevelBatchGenerator.FirstGenerated;
            if (si < 0 || si >= specs.Count) return;
            try { Debug.Log(LevelBatchGenerator.GenerateOne(index + 1, specs[si], regenSeed, 0, 1)); }
            finally { EditorUtility.ClearProgressBar(); }
            regenSeed += 400;
            AssetDatabase.SaveAssets();
        }
    }
}
