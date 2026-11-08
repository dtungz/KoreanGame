using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using KoreanGame.Gameplay;
using UnityEditor;
using UnityEngine;

namespace KoreanGame.EditorTools
{
    /// <summary>Sinh level 15–40 theo Data/Levels/Source/gen_specs.json, chọn ứng viên có độ khó ở giữa, lưu thành Level_NN.</summary>
    public static class LevelBatchGenerator
    {
        const string SpecPath = LevelImporter.LevelFolder + "/Source/gen_specs.json";
        const string ReportPath = LevelImporter.LevelFolder + "/Source/gen_report.txt";
        public const int FirstGenerated = 15;

        [Serializable]
        class SpecFile
        {
            public string note;
            public List<GenSpec> specs = new List<GenSpec>();
        }

        public static List<GenSpec> LoadSpecs() => JsonUtility.FromJson<SpecFile>(File.ReadAllText(SpecPath)).specs;

        [MenuItem("Tools/KoreanGame/Levels/Generate Levels 15-40")]
        public static void GenerateAll() => Run(false);

        [MenuItem("Tools/KoreanGame/Levels/Generate Missing Levels")]
        public static void GenerateMissing() => Run(true);

        static void Run(bool onlyMissing)
        {
            var specs = LoadSpecs();
            var db = LevelImporter.LoadOrCreateDatabase();
            var report = new StringBuilder();
            try
            {
                for (int i = 0; i < specs.Count; i++)
                {
                    int number = FirstGenerated + i;
                    if (specs[i].manual) { report.AppendLine(BuildManual(number, specs[i])); continue; }
                    if (onlyMissing && number - 1 < db.Count && db.levels[number - 1] != null) continue;
                    report.AppendLine(GenerateOne(number, specs[i], 1, i, specs.Count));
                }
            }
            finally { EditorUtility.ClearProgressBar(); }
            File.WriteAllText(ReportPath, report.ToString());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LevelGenerator]\n" + report);
        }

        /// <summary>
        /// Level làm tay (generator chỉ bắt đầu từ 1 khối cơm nên không tạo được màn 2 miếng):
        /// hai thanh 2×1 → khay dọc 1×4. Chỉ 1 thẻ Xoay nên phải Dán nối đuôi trước rồi xoay cả khối.
        /// </summary>
        static string BuildManual(int number, GenSpec spec)
        {
            var set = CardLibrary.EnsureAll();
            var L = LevelImporter.GetOrCreateLevel(number);
            L.hint = spec.hint;
            L.rice = new List<RicePiece>
            {
                new RicePiece { tris = Geometry.FullRect(2, 1), at = new Vector2Int(1, 7) },
                new RicePiece { tris = Geometry.FullRect(2, 1), at = new Vector2Int(4, 8) },
            };
            L.tray = new TrayData { tris = Geometry.FullRect(1, 4), at = new Vector2Int(3, 1) };
            L.cards = spec.cards.ConvertAll(n => set[n]);
            L.isTutorial = false;
            L.unlocksSilhouette = spec.silhouette;
            var a = LevelSolver.Analyze(L);
            L.difficulty = a.score;
            L.solutions = a.solutions;
            L.solution = a.path != null ? string.Join(" → ", a.path) : "";
            EditorUtility.SetDirty(L);
            var db = LevelImporter.LoadOrCreateDatabase();
            LevelImporter.SetInDatabase(db, number - 1, L);
            EditorUtility.SetDirty(db);
            return $"Level {number}: làm tay · {(a.IsValid ? "hợp lệ" : "KHÔNG hợp lệ")} · độ khó {a.score}, {a.solutions} lời giải";
        }

        /// <summary>Sinh lại một level (dùng từ Level Browser), seed khác để ra màn khác.</summary>
        public static string GenerateOne(int number, GenSpec spec, int seed0, int step = 0, int steps = 1)
        {
            var set = CardLibrary.EnsureAll();
            var cands = LevelGenerator.Generate(spec, set, 5, seed0, 400,
                p => EditorUtility.DisplayCancelableProgressBar("Sinh level", $"Level {number} ({string.Join(", ", spec.cards)})", (step + p) / steps));
            if (cands.Count == 0) return $"Level {number}: KHÔNG sinh được màn hợp lệ ({string.Join(", ", spec.cards)})";

            var pick = cands[cands.Count / 2];
            var L = LevelImporter.GetOrCreateLevel(number);
            var src = pick.level;
            L.hint = src.hint;
            L.rice = src.rice;
            L.tray = src.tray;
            L.cards = src.cards;
            L.isTutorial = false;
            L.unlocksSilhouette = src.unlocksSilhouette;
            L.difficulty = src.difficulty;
            L.solutions = src.solutions;
            L.solution = src.solution;
            EditorUtility.SetDirty(L);
            var db = LevelImporter.LoadOrCreateDatabase();
            LevelImporter.SetInDatabase(db, number - 1, L);
            EditorUtility.SetDirty(db);
            foreach (var c in cands) UnityEngine.Object.DestroyImmediate(c.level);
            return $"Level {number}: seed {pick.seed}, độ khó {pick.analysis.score}, {pick.analysis.solutions} lời giải, {cands.Count} ứng viên · {string.Join(", ", spec.cards)}";
        }
    }
}
