using System.Collections.Generic;
using KoreanGame.Core;
using KoreanGame.Gameplay;
using UnityEditor;
using UnityEngine;

namespace KoreanGame.EditorTools
{
    /// <summary>
    /// Tạo 9 tutorial giới thiệu mechanic (phần BẮT BUỘC) thành asset trong Data/Tutorials. Asset đã có thì giữ nguyên
    /// (sửa câu / bước trực tiếp trong Inspector). Câu tiếng Hàn cần người biết tiếng Hàn kiểm tra (★ trong localization.csv).
    /// </summary>
    public static class TutorialLibrary
    {
        public const string Folder = "Assets/_Project/Data/Tutorials";

        static TutorialStep S(string key, string vi, string ko, TutTarget target, TutGesture gesture, TutTarget dragTo, TutDone done)
            => new TutorialStep { key = key, vi = vi, ko = ko, target = target, gesture = gesture, dragTo = dragTo, done = done };

        static TutorialStep Kind(TutorialStep s, CardKind k) { s.byKind = true; s.cardKind = k; return s; }
        static TutorialStep Effect(TutorialStep s, CardEffect e) { s.anyEffect = false; s.effect = e; return s; }

        static readonly TutTarget Big = TutTarget.PieceLargestOutside;

        /// <summary>(id, màn (từ 1), các bước)</summary>
        static IEnumerable<(string id, int level, TutorialStep[] steps)> All()
        {
            yield return ("cut_drag", 1, new[]
            {
                S("tut.cut_drag.step1", "Kéo thẻ lên cơm để cắt", "카드를 밥 위로 끌어 자르세요", TutTarget.Card, TutGesture.Drag, Big, TutDone.CardUsed),
                S("tut.cut_drag.step2", "Kéo miếng cơm vào hộp", "밥을 도시락에 넣으세요", Big, TutGesture.Drag, TutTarget.TrayEmpty, TutDone.PieceIntoTray),
                S("tut.cut_drag.step3", "Lấp kín hộp nhé!", "도시락을 가득 채워요!", Big, TutGesture.Drag, TutTarget.TrayEmpty, TutDone.Win),
            });
            yield return ("rotate_card", 2, new[]
            {
                S("tut.rotate_card.step1", "Chạm thẻ để xoay", "카드를 눌러 돌려요", TutTarget.Card, TutGesture.Tap, TutTarget.None, TutDone.CardRotated),
                S("tut.rotate_card.step2", "Giờ cắt nào!", "이제 잘라요!", TutTarget.Card, TutGesture.Drag, Big, TutDone.CardUsed),
            });
            yield return ("fixed_food", 4, new[]
            {
                S("tut.fixed_food.step1", "Lấp cơm quanh món có sẵn", "반찬 주위를 채워요", TutTarget.FixedFood, TutGesture.Tap, TutTarget.None, TutDone.Timer),
            });
            yield return ("wall", 5, new[]
            {
                S("tut.wall.step1", "Không đặt cơm vắt qua vách", "칸막이를 넘으면 안 돼요", TutTarget.Wall, TutGesture.Tap, TutTarget.None, TutDone.Timer),
            });
            yield return ("diagonal", 8, new[]
            {
                S("tut.diagonal.step1", "Thẻ chéo cắt xéo", "대각선으로 잘라요", TutTarget.Card, TutGesture.Drag, Big, TutDone.CardUsed),
            });
            yield return ("rotate_piece", 11, new[]
            {
                Effect(Kind(S("tut.rotate_piece.step1", "Thả thẻ lên miếng cơm", "카드를 밥 위에 놓아요", TutTarget.Card, TutGesture.Drag, Big, TutDone.CardUsed), CardKind.Rot), CardEffect.Transform),
                S("tut.rotate_piece.step2", "Vừa khít rồi!", "딱 맞아요!", Big, TutGesture.Drag, TutTarget.TrayEmpty, TutDone.Win),
            });
            yield return ("flip", 12, new[]
            {
                Effect(Kind(S("tut.flip.step1", "Lật miếng cơm cho vừa", "밥을 뒤집어 맞춰요", TutTarget.Card, TutGesture.Drag, Big, TutDone.CardUsed), CardKind.Flip), CardEffect.Transform),
                S("tut.flip.step2", "Vừa khít rồi!", "딱 맞아요!", Big, TutGesture.Drag, TutTarget.TrayEmpty, TutDone.Win),
            });
            yield return ("glue", 13, new[]
            {
                S("tut.glue.step1", "Đặt hai miếng sát nhau", "두 밥을 붙여 놓아요", TutTarget.PieceSecondOutside, TutGesture.Drag, Big, TutDone.PiecePlaced),
                Effect(Kind(S("tut.glue.step2", "Dán thành một miếng", "하나로 붙여요", TutTarget.Card, TutGesture.Drag, Big, TutDone.CardUsed), CardKind.Glue), CardEffect.Glue),
                Kind(S("tut.glue.step3", "Xoay rồi xếp vào hộp", "돌려서 넣어요", TutTarget.Card, TutGesture.None, TutTarget.None, TutDone.Win), CardKind.Rot),
            });
            var copy = Effect(Kind(S("tut.queue_copy.step2", "Nhân đôi miếng nhỏ", "작은 밥을 복사해요", TutTarget.Card, TutGesture.Drag, TutTarget.PieceSmallestOutside, TutDone.CardUsed), CardKind.Copy), CardEffect.Copy);
            copy.waitForCard = true;
            yield return ("queue_copy", 14, new[]
            {
                S("tut.queue_copy.step1", "Dùng thẻ, thẻ chờ sẽ vào", "다음 카드가 들어와요", TutTarget.QueueChip, TutGesture.None, TutTarget.None, TutDone.CardUsed),
                copy,
            });
        }

        /// <summary>Tạo asset còn thiếu; trả về toàn bộ tutorial theo thứ tự màn.</summary>
        public static TutorialDef[] EnsureAll()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/_Project/Data", "Tutorials");
            var list = new List<TutorialDef>();
            foreach (var (id, level, steps) in All())
            {
                var path = $"{Folder}/Tut_{level:00}_{id}.asset";
                var t = AssetDatabase.LoadAssetAtPath<TutorialDef>(path);
                if (t == null)
                {
                    t = ScriptableObject.CreateInstance<TutorialDef>();
                    t.id = id;
                    t.levelIndex = level - 1;
                    t.steps = new List<TutorialStep>(steps);
                    AssetDatabase.CreateAsset(t, path);
                }
                list.Add(t);
            }
            AssetDatabase.SaveAssets();
            return list.ToArray();
        }

        /// <summary>
        /// Màn 2 dạy "chạm thẻ để xoay" nhưng lời giải cũ không cần xoay. Lật màn qua đường chéo (khay + khối cơm đứng),
        /// giữ thẻ Thẳng 3 nằm ngang: phải xoay thẻ 90° mới cắt đúng. Chỉ ghi khi solver xác nhận đúng 1 lời giải và
        /// lời giải có xoay thẻ; không đạt thì giữ nguyên data cũ. Trả về báo cáo.
        /// </summary>
        [MenuItem("Tools/KoreanGame/Levels/Fix Level 2 (bắt xoay thẻ)")]
        static void FixLevel2Menu() => Debug.Log("[Tutorial] " + FixLevel2());

        public static string FixLevel2()
        {
            var db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(LevelImporter.DatabasePath);
            var L = db.levels[1];
            var before = LevelSolver.Solve(L, all: true, maxNodes: 60000);
            if (before.path != null && before.path.Exists(p => p.Contains("xoay 90") || p.Contains("xoay 270")) && before.solutions == 1)
                return "Màn 2 đã bắt xoay thẻ (1 lời giải), không sửa.";

            var work = Object.Instantiate(L);
            // lật qua đường chéo: (x, y) → (y, x); tam giác trên ↔ trái, phải ↔ dưới
            int[] T = { 3, 2, 1, 0 };
            List<Tri> Tr(List<Tri> l) => l.ConvertAll(q => new Tri(q.y, q.x, T[q.t]));
            work.tray.tris = Tr(work.tray.tris);
            for (int i = 0; i < work.tray.walls.Count; i++) { var w = work.tray.walls[i]; work.tray.walls[i] = new Wall(w.y1, w.x1, w.y2, w.x2); }
            foreach (var f in work.tray.fixedFoods) f.cells = f.cells.ConvertAll(c => new Vector2Int(c.y, c.x));
            foreach (var p in work.rice) p.tris = Tr(p.tris);
            // đặt lại: khay giữa trên, khối cơm giữa dưới
            var tb = Geometry.BBox(work.tray.tris);
            work.tray.at = new Vector2Int((Geometry.Cols - tb.W) / 2 - tb.minx, 1 - tb.miny);
            foreach (var p in work.rice)
            {
                var rb = Geometry.BBox(p.tris);
                p.at = new Vector2Int((Geometry.Cols - rb.W) / 2 - rb.minx, Geometry.Rows - 1 - rb.H - rb.miny);
            }

            var a = LevelSolver.Analyze(work);
            bool rotates = a.path != null && a.path.Exists(p => p.Contains("xoay 90") || p.Contains("xoay 270"));
            string res = $"trước {before.solutions} lời giải; sau: giải được={a.solved}, {a.solutions} lời giải, thẻ cần={a.AllCardsRequired}, có xoay thẻ={rotates}";
            if (!a.solved || a.solutions != 1 || !rotates || !a.AllCardsRequired)
            {
                Object.DestroyImmediate(work);
                return "KHÔNG sửa Màn 2 (" + res + ")";
            }
            L.tray = work.tray;
            L.rice = work.rice;
            L.hint = "Chạm thẻ để xoay thẻ, rồi cắt dọc khối cơm.";
            L.difficulty = a.score;
            L.solutions = a.solutions;
            L.solution = string.Join(" → ", a.path);
            EditorUtility.SetDirty(L);
            AssetDatabase.SaveAssets();
            Object.DestroyImmediate(work);
            return "Đã sửa Màn 2 (" + res + "): " + L.solution;
        }

        [MenuItem("Tools/KoreanGame/Debug/Reset Tutorials")]
        static void ResetMenu()
        {
            var ids = new List<string>();
            foreach (var t in EnsureAll()) ids.Add(t.id);
            TutorialSave.Reset(ids);
            Debug.Log("[Tutorial] Đã xoá cờ đã xem của " + ids.Count + " tutorial.");
        }
    }
}
