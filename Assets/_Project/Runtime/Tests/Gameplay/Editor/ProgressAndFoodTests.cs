using KoreanGame.Core;
using KoreanGame.EditorTools;
using KoreanGame.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace KoreanGame.Tests
{
    /// <summary>Tiến độ tách "màn đang chơi" / "số màn đã qua", và mọi món phụ trong level đều có trong danh mục món.</summary>
    public class ProgressAndFoodTests
    {
        int? savedCurrent, savedUnlocked;

        [SetUp]
        public void Save()
        {
            savedCurrent = PlayerPrefs.HasKey(ProgressSave.Key) ? PlayerPrefs.GetInt(ProgressSave.Key) : (int?)null;
            savedUnlocked = PlayerPrefs.HasKey(ProgressSave.UnlockedKey) ? PlayerPrefs.GetInt(ProgressSave.UnlockedKey) : (int?)null;
            ProgressSave.Reset();
        }

        [TearDown]
        public void Restore()
        {
            ProgressSave.Reset();
            if (savedCurrent.HasValue) PlayerPrefs.SetInt(ProgressSave.Key, savedCurrent.Value);
            if (savedUnlocked.HasValue) PlayerPrefs.SetInt(ProgressSave.UnlockedKey, savedUnlocked.Value);
            PlayerPrefs.Save();
        }

        [Test]
        public void SignatureDishIsNewOnlyOnce()
        {
            string dishes = PlayerPrefs.GetString(CollectionSave.DishKey, null), sides = PlayerPrefs.GetString(CollectionSave.SideKey, null);
            try
            {
                CollectionSave.Reset();
                Assert.IsTrue(CollectionSave.AddDish("bun_cha"), "lần thắng đầu: món mới");
                Assert.IsFalse(CollectionSave.AddDish("bun_cha"), "chơi lại: không hiện popup nữa");
                Assert.IsFalse(CollectionSave.HasDish("bun"), "không nhầm id là chuỗi con");
                PlayerPrefs.SetString(CollectionSave.DishKey, "");    // key có sẵn nhưng rỗng
                Assert.IsTrue(CollectionSave.AddDish("pho"));
                Assert.IsFalse(CollectionSave.AddDish("pho"), "key rỗng ban đầu vẫn nhận ra món đã có");
                CollectionSave.AddSide("kimchi");
                Assert.IsTrue(CollectionSave.HasSide("kimchi"));
                Assert.IsFalse(CollectionSave.HasDish("kimchi"), "món phụ và món đặc trưng lưu riêng");
            }
            finally
            {
                CollectionSave.Reset();
                if (dishes != null) PlayerPrefs.SetString(CollectionSave.DishKey, dishes);
                if (sides != null) PlayerPrefs.SetString(CollectionSave.SideKey, sides);
                PlayerPrefs.Save();
            }
        }

        [Test]
        public void GoldSpendNeverGoesNegative()
        {
            bool had = PlayerPrefs.HasKey(GoldSave.Key);
            int saved = GoldSave.Gold;
            try
            {
                PlayerPrefs.SetInt(GoldSave.Key, 10);
                Assert.IsFalse(GoldSave.Spend(GoldSave.HintPrice), "thiếu vàng: không trừ");
                Assert.AreEqual(10, GoldSave.Gold);
                GoldSave.Add(GoldSave.WinFirst);
                Assert.IsTrue(GoldSave.Spend(GoldSave.HintPrice));
                Assert.AreEqual(10 + GoldSave.WinFirst - GoldSave.HintPrice, GoldSave.Gold);
            }
            finally
            {
                if (had) PlayerPrefs.SetInt(GoldSave.Key, saved); else PlayerPrefs.DeleteKey(GoldSave.Key);
                PlayerPrefs.Save();
            }
        }

        [Test]
        public void ShopDefaultsAreFreeOthersHavePrice()
        {
            var cat = AssetDatabase.LoadAssetAtPath<ShopCatalog>(BentoJourneyBuilder.ShopPath);
            if (cat == null) Assert.Ignore("Chưa có ShopCatalog");
            foreach (ShopCategory c in System.Enum.GetValues(typeof(ShopCategory)))
                Assert.AreEqual(0, cat.Default(c).price, $"{c}: món mặc định phải miễn phí");
            foreach (var it in cat.items)
                if (it.unlockAfter > 0) Assert.Greater(it.price, 0, $"{it.displayName}: món mở thêm phải có giá");
        }

        [Test]
        public void ReplayingOldLevelDoesNotLowerUnlocked()
        {
            ProgressSave.MarkCleared(4);
            ProgressSave.CurrentLevel = 5;
            ProgressSave.CurrentLevel = 1;      // chơi lại Màn 2
            ProgressSave.MarkCleared(1);        // thắng lại Màn 2
            Assert.AreEqual(5, ProgressSave.UnlockedLevel);
            Assert.IsTrue(ProgressSave.IsCleared(4));
            Assert.IsFalse(ProgressSave.IsCleared(5));
        }

        [Test]
        public void FinishingLastLevelKeepsAllUnlocked()
        {
            ProgressSave.MarkCleared(39);
            ProgressSave.CurrentLevel = 0;      // quay vòng về Màn 1 sau màn cuối
            Assert.AreEqual(40, ProgressSave.UnlockedLevel);
        }

        [Test]
        public void OldSaveMigratesFromCurrentLevel()
        {
            PlayerPrefs.SetInt(ProgressSave.Key, 12);   // save cũ: chỉ có màn đang chơi
            ProgressSave.Migrate();
            ProgressSave.CurrentLevel = 3;
            Assert.AreEqual(12, ProgressSave.UnlockedLevel);
        }

        [Test]
        public void LevelsHaveRegionAndMatchingSignatureDish()
        {
            var db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(LevelImporter.DatabasePath);
            var seen = new System.Collections.Generic.HashSet<DishData>();
            for (int i = 0; i < db.Count; i++)
            {
                var L = db.levels[i];
                Assert.AreEqual(FoodAssigner.RegionOf(i), L.region, $"Level {i + 1}: chưa gán vùng (chạy Tools/KoreanGame/Levels/Assign Foods/Apply)");
                if (L.unlocksDish == null) continue;
                Assert.AreEqual(L.region, L.unlocksDish.region, $"Level {i + 1}: món {L.unlocksDish.displayName} khác vùng");
                Assert.IsTrue(seen.Add(L.unlocksDish), $"Level {i + 1}: món {L.unlocksDish.displayName} mở khoá 2 lần");
                Assert.IsEmpty(L.tray.fixedFoods.FindAll(f => f.kind != "egg" && f.kind != "sausage" && f.kind != "meat"),
                    $"Level {i + 1}: màn mở khoá món đặc trưng không có ngăn món phụ");
            }
            Assert.AreEqual(8, seen.Count, "Cần đủ 8 món đặc trưng");
        }

        [Test]
        public void TraysStayInsideBoardFrame()
        {
            // viền hộp rộng ~0,2 ô: hộp ở cột 0 hoặc 6 sẽ lấn ra ngoài khung thớt
            var db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(LevelImporter.DatabasePath);
            for (int i = 0; i < db.Count; i++)
            {
                var t = db.levels[i].tray;
                var b = Geometry.BBox(Geometry.Shift(t.tris, t.at.x, t.at.y));
                Assert.IsTrue(b.minx >= 1 && b.maxx <= Geometry.Cols - 2, $"Level {i + 1}: hộp ở cột {b.minx}–{b.maxx}, chạm mép thớt");
            }
        }

        [Test]
        public void SignatureDishArtMatchesItsTray()
        {
            var db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(LevelImporter.DatabasePath);
            for (int i = 0; i < db.Count; i++)
            {
                var d = db.levels[i].unlocksDish;
                if (d == null) continue;
                Assert.IsNotNull(d.art, $"{d.displayName}: chưa có ảnh hộp");
                Assert.IsNotNull(d.icon, $"{d.displayName}: chưa có icon");
                // ảnh hộp phủ đúng khung chữ nhật của khuôn màn mở khoá (cùng tỉ lệ rộng / cao)
                var b = Geometry.BBox(db.levels[i].tray.tris);
                float want = (float)b.W / b.H, got = d.art.rect.width / d.art.rect.height;
                Assert.AreEqual(want, got, .01f, $"Level {i + 1}: ảnh {d.displayName} lệch tỉ lệ khuôn {b.W}×{b.H}");
            }
        }

        [Test]
        public void EveryFixedFoodHasCatalogSprite()
        {
            var cat = AssetDatabase.LoadAssetAtPath<FoodCatalog>(BentoJourneyBuilder.CatalogPath);
            if (cat == null) Assert.Ignore("Chưa có FoodCatalog (dựng scene Bento Journey để tạo)");
            var db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(LevelImporter.DatabasePath);
            for (int i = 0; i < db.Count; i++)
                foreach (var f in db.levels[i].tray.fixedFoods)
                    Assert.IsNotNull(cat.SideSprite(f.kind), $"Level {i + 1}: món phụ '{f.kind}' chưa có ảnh trong FoodCatalog");
        }
    }
}
