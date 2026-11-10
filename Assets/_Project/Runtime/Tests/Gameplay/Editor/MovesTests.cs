using System.Collections.Generic;
using KoreanGame.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace KoreanGame.Tests
{
    public class MovesTests
    {
        readonly List<Object> created = new List<Object>();

        [TearDown]
        public void Cleanup()
        {
            foreach (var o in created) Object.DestroyImmediate(o);
            created.Clear();
        }

        CardDef Card(CardKind kind, CardFamily fam, params Vector2Int[] line)
        {
            var c = ScriptableObject.CreateInstance<CardDef>();
            c.kind = kind; c.family = fam; c.displayName = kind.ToString();
            if (line.Length > 0) c.polys.Add(new Polyline { pts = new List<Vector2Int>(line) });
            created.Add(c);
            return c;
        }

        LevelData Level(List<Tri> rice, Vector2Int riceAt, List<Tri> tray, Vector2Int trayAt, params CardDef[] cards)
        {
            var L = ScriptableObject.CreateInstance<LevelData>();
            L.rice.Add(new RicePiece { tris = rice, at = riceAt });
            L.tray.tris = tray; L.tray.at = trayAt;
            L.cards.AddRange(cards);
            created.Add(L);
            return L;
        }

        LevelData BarToSquare() => Level(Geometry.FullRect(4, 1), new Vector2Int(1, 7), Geometry.FullRect(2, 2), new Vector2Int(2, 1),
            Card(CardKind.Cut, CardFamily.Line, new Vector2Int(0, 0), new Vector2Int(0, 1)));

        [Test]
        public void CutSplitsPieceAndConsumesCard()
        {
            var s = BoardState.FromLevel(BarToSquare());
            var res = Moves.EvaluateCut(s, s.hand[0], 3, 7);
            Assert.IsTrue(res.valid);
            Moves.Apply(s, res);
            s.UseCard(0);
            Assert.AreEqual(2, s.pieces.Count);
            Assert.AreEqual(0, s.CardsLeft);
        }

        [Test]
        public void MissedCutIsInvalidAndKeepsCard()
        {
            var s = BoardState.FromLevel(BarToSquare());
            var res = Moves.EvaluateCut(s, s.hand[0], 0, 0);
            Assert.IsFalse(res.valid);
            Assert.AreEqual(1, s.CardsLeft);
        }

        [Test]
        public void UndoRestoresState()
        {
            var s = BoardState.FromLevel(BarToSquare());
            var h = new UndoHistory();
            h.Push(s);
            Moves.Apply(s, Moves.EvaluateCut(s, s.hand[0], 3, 7));
            s.UseCard(0);
            var back = h.Pop();
            Assert.AreEqual(1, back.pieces.Count);
            Assert.AreEqual(1, back.CardsLeft);
        }

        [Test]
        public void WinWhenTrayFilled()
        {
            var s = BoardState.FromLevel(BarToSquare());
            Moves.Apply(s, Moves.EvaluateCut(s, s.hand[0], 3, 7));
            Assert.AreEqual(WinState.None, Moves.CheckWin(s));
            // miếng (1..2, 7) -> (2..3, 1); miếng (3..4, 7) -> (2..3, 2)
            var a = s.pieces.Find(p => p.tris.Exists(q => q.x == 1));
            var b = s.pieces.Find(p => p != a);
            Assert.IsTrue(Moves.CanPlacePiece(s, a, Geometry.Shift(a.tris, 1, -6)));
            Moves.MovePiece(s, a, 1, -6);
            Moves.MovePiece(s, b, -1, -5);
            Assert.AreEqual(WinState.Won, Moves.CheckWin(s));
        }

        [Test]
        public void TrayCarriesPiecesInside()
        {
            var s = BoardState.FromLevel(BarToSquare());
            s.pieces[0].tris = Geometry.Shift(Geometry.FullRect(2, 1), 2, 1);
            Assert.IsTrue(Moves.CanPlaceTray(s, 0, 2));
            Moves.MoveTray(s, 0, 2);
            Assert.IsTrue(s.pieces[0].tris.Contains(new Tri(2, 3, 0)));
            Assert.IsTrue(s.tray.tris.Contains(new Tri(2, 3, 0)));
        }

        [Test]
        public void PieceCannotStraddleTrayEdge()
        {
            var s = BoardState.FromLevel(BarToSquare());   // khay 2×2 tại (2,1)
            var bar = s.pieces[0];
            Assert.IsFalse(Moves.CanPlacePiece(s, bar, Geometry.Shift(Geometry.FullRect(4, 1), 1, 1)), "vắt qua mép hộp");
            Assert.IsFalse(Moves.CanPlacePiece(s, bar, Geometry.Shift(Geometry.FullRect(4, 1), 0, 2)), "vắt qua mép hộp");
            Assert.IsTrue(Moves.CanPlacePiece(s, bar, Geometry.Shift(Geometry.FullRect(4, 1), 1, 4)), "ở ngoài hộp");
            Assert.IsTrue(Moves.CanPlacePiece(s, bar, Geometry.Shift(Geometry.FullRect(2, 1), 2, 1)), "gọn trong hộp");
        }

        [Test]
        public void TrayCannotCoverOutsidePiece()
        {
            var s = BoardState.FromLevel(BarToSquare());   // cơm 4×1 ở hàng 7
            Assert.IsFalse(Moves.CanPlaceTray(s, 0, 5), "khay đè lên cơm");
            Assert.IsFalse(Moves.CanPlaceTray(s, 0, 6), "khay đè lên cơm");
            Assert.IsTrue(Moves.CanPlaceTray(s, 0, 3));
        }

        [Test]
        public void PieceAcrossWallBlocksWin()
        {
            var L = Level(Geometry.FullRect(2, 1), new Vector2Int(2, 1), Geometry.FullRect(2, 1), new Vector2Int(2, 1));
            L.tray.walls.Add(new Wall(1, 0, 1, 1));
            var s = BoardState.FromLevel(L);
            Assert.AreEqual(WinState.WallBlocked, Moves.CheckWin(s));
        }

        [Test]
        public void WallCrossingPieceFindsPieceAcrossWall()
        {
            var L = Level(Geometry.FullRect(2, 1), new Vector2Int(2, 1), Geometry.FullRect(2, 1), new Vector2Int(2, 1));
            var s = BoardState.FromLevel(L);
            Assert.IsNull(Moves.WallCrossingPiece(s), "chưa có vách");
            s.tray.walls.Add(new Wall(3, 1, 3, 2));   // vách giữa 2 ô của khay (toạ độ thớt)
            Assert.AreSame(s.pieces[0], Moves.WallCrossingPiece(s));
        }

        [Test]
        public void GlueCannotJoinAcrossTrayEdge()
        {
            var s = BoardState.FromLevel(BarToSquare());   // khay 2×2 tại (2,1): hàng 1–2
            var glue = new HandCard { def = Card(CardKind.Glue, CardFamily.Glue, new Vector2Int(0, 0), new Vector2Int(1, 0)) };
            s.pieces = new List<Piece>
            {
                new Piece { id = 1, tris = Geometry.Shift(Geometry.FullRect(1, 1), 2, 2) },   // trong khay (hàng dưới)
                new Piece { id = 2, tris = Geometry.Shift(Geometry.FullRect(1, 1), 2, 3) },   // ngay dưới mép khay
            };
            Assert.IsFalse(Moves.EvaluateGlue(s, glue, 2, 3).valid, "một trong, một ngoài");

            s.pieces[0].tris = Geometry.Shift(Geometry.FullRect(1, 1), 2, 5);
            s.pieces[1].tris = Geometry.Shift(Geometry.FullRect(1, 1), 2, 6);
            Assert.IsTrue(Moves.EvaluateGlue(s, glue, 2, 6).valid, "cả hai ngoài khay");

            s.pieces[0].tris = Geometry.Shift(Geometry.FullRect(1, 1), 2, 1);
            s.pieces[1].tris = Geometry.Shift(Geometry.FullRect(1, 1), 2, 2);
            Assert.IsTrue(Moves.EvaluateGlue(s, glue, 2, 2).valid, "cả hai trong khay");
        }

        [Test]
        public void RotateCardTurnsPiece()
        {
            var L = Level(Geometry.FullRect(3, 1), new Vector2Int(2, 7), Geometry.FullRect(1, 3), new Vector2Int(3, 1), Card(CardKind.Rot, CardFamily.Fn));
            var s = BoardState.FromLevel(L);
            var res = Moves.EvaluateFn(s, s.hand[0], 3.5f, 7.5f);
            Assert.IsTrue(res.valid);
            Moves.Apply(s, res);
            var b = Geometry.BBox(s.pieces[0].tris);
            Assert.AreEqual(3, b.H);
        }

        [Test]
        public void HandQueueRefillsUsedSlot()
        {
            var c = Card(CardKind.Cut, CardFamily.Line, new Vector2Int(0, 0), new Vector2Int(0, 1));
            var L = Level(Geometry.FullRect(4, 1), new Vector2Int(1, 7), Geometry.FullRect(2, 2), new Vector2Int(2, 1), c, c, c, c);
            var s = BoardState.FromLevel(L);
            Assert.AreEqual(1, s.QueuedCount);
            s.UseCard(1);
            Assert.AreEqual(3, s.slots[1]);
            Assert.AreEqual(0, s.QueuedCount);
        }

        [Test]
        public void SolverSolvesSimpleLevel()
        {
            var a = LevelSolver.Analyze(BarToSquare());
            Assert.IsTrue(a.solved);
            Assert.IsFalse(a.trivial);
            Assert.IsTrue(a.AllCardsRequired);
        }
    }
}
