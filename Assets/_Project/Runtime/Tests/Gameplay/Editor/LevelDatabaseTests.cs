using KoreanGame.EditorTools;
using KoreanGame.Gameplay;
using NUnit.Framework;
using UnityEditor;

namespace KoreanGame.Tests
{
    /// <summary>
    /// Mọi level trong LevelDatabase phải giải được và cần thẻ (GDD 7.3). Level sinh tự động không được có thẻ thừa;
    /// màn dạy (isTutorial) được phép có thẻ bẫy / thẻ dạy hàng chờ như prototype (level 10, 14).
    /// </summary>
    public class LevelDatabaseTests
    {
        static LevelDatabase Db => AssetDatabase.LoadAssetAtPath<LevelDatabase>(LevelImporter.DatabasePath);

        [Test]
        public void DatabaseHas40Levels()
        {
            Assert.IsNotNull(Db, "Chưa có LevelDatabase");
            Assert.AreEqual(40, Db.Count);
            for (int i = 0; i < Db.Count; i++) Assert.IsNotNull(Db.levels[i], $"Level {i + 1} trống");
        }

        [Test]
        public void EveryLevelIsSolvableAndNeedsAllCards()
        {
            var db = Db;
            Assert.IsNotNull(db);
            for (int i = 0; i < db.Count; i++)
            {
                var L = db.levels[i];
                var r = LevelSolver.Solve(L);
                Assert.IsTrue(r.solved, $"Level {i + 1} không giải được");
                Assert.IsFalse(LevelSolver.Solve(L, cardsOverride: new System.Collections.Generic.List<CardDef>()).solved, $"Level {i + 1} không cần thẻ");
                if (L.isTutorial) continue;
                for (int k = 0; k < L.cards.Count; k++)
                    Assert.IsFalse(LevelSolver.Solve(L, skip: k, maxNodes: 120000).solved, $"Level {i + 1}: thẻ {L.cards[k].displayName} là thẻ thừa");
            }
        }
    }
}
