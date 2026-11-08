using System.Collections.Generic;
using KoreanGame.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace KoreanGame.Tests
{
    public class GeometryTests
    {
        static List<Vector2Int[]> Line(params Vector2Int[] pts) => new List<Vector2Int[]> { pts };
        static Vector2Int P(int x, int y) => new Vector2Int(x, y);

        static int PartsAfterCut(List<Tri> rice, List<Vector2Int[]> polys, int ox, int oy) =>
            Geometry.Components(rice, Geometry.SegKeys(Geometry.UnitSegments(polys, ox, oy))).Count;

        [Test]
        public void StraightCutSplitsBarInTwo()
        {
            var bar = Geometry.FullRect(4, 1);
            Assert.AreEqual(2, PartsAfterCut(bar, Line(P(0, 0), P(0, 1)), 2, 0));
        }

        [Test]
        public void CutOnBorderDoesNothing()
        {
            var bar = Geometry.FullRect(4, 1);
            Assert.AreEqual(1, PartsAfterCut(bar, Line(P(0, 0), P(0, 1)), 0, 0));
        }

        [Test]
        public void LCutCutsCorner()
        {
            var sq = Geometry.FullRect(3, 3);
            var parts = Geometry.Components(sq, Geometry.SegKeys(Geometry.UnitSegments(Line(P(2, 0), P(2, 1), P(0, 1)), 0, 0)));
            Assert.AreEqual(2, parts.Count);
            parts.Sort((a, b) => a.Count.CompareTo(b.Count));
            Assert.AreEqual(8, parts[0].Count); // 2 ô
        }

        [Test]
        public void DiagonalCutMakesTriangles()
        {
            var sq = Geometry.FullRect(2, 2);
            var parts = Geometry.Components(sq, Geometry.SegKeys(Geometry.UnitSegments(Line(P(0, 0), P(2, 2)), 0, 0)));
            Assert.AreEqual(2, parts.Count);
            Assert.AreEqual(8, parts[0].Count);
            Assert.AreEqual(8, parts[1].Count);
        }

        [Test]
        public void MoldExtractsInside()
        {
            var r = Geometry.FullRect(4, 2);
            var mold = Line(P(0, 0), P(2, 0), P(2, 2), P(0, 2), P(0, 0));
            Assert.AreEqual(2, PartsAfterCut(r, mold, 0, 0));   // lấy 2×2 ở mép: còn 2 miếng
            Assert.AreEqual(3, PartsAfterCut(r, mold, 1, 0));   // lấy 2×2 ở giữa: còn 3 miếng
        }

        [Test]
        public void RotateFourTimesIsIdentity()
        {
            var s = Geometry.Normalize(new List<Tri> { new Tri(0, 0, 1), new Tri(0, 0, 2), new Tri(1, 0, 0), new Tri(1, 1, 3) });
            var r = Geometry.Normalize(Geometry.RotCW(Geometry.RotCW(Geometry.RotCW(Geometry.RotCW(s)))));
            Assert.AreEqual(Geometry.ShapeKey(s), Geometry.ShapeKey(r));
        }

        [Test]
        public void FlipTwiceIsIdentity()
        {
            var s = Geometry.Normalize(new List<Tri> { new Tri(0, 0, 1), new Tri(0, 0, 2), new Tri(1, 0, 0), new Tri(2, 1, 3) });
            Assert.AreEqual(Geometry.ShapeKey(s), Geometry.ShapeKey(Geometry.Normalize(Geometry.FlipH(Geometry.FlipH(s)))));
            Assert.AreEqual(Geometry.ShapeKey(s), Geometry.ShapeKey(Geometry.Normalize(Geometry.FlipV(Geometry.FlipV(s)))));
        }

        [Test]
        public void RotateBarChangesOrientation()
        {
            var bar = Geometry.FullRect(3, 1);
            var b = Geometry.BBox(Geometry.Normalize(Geometry.RotCW(bar)));
            Assert.AreEqual(1, b.W);
            Assert.AreEqual(3, b.H);
        }

        [Test]
        public void TriAtPicksQuadrant()
        {
            Assert.AreEqual(new Tri(2, 3, 0), Geometry.TriAt(2.5f, 3.1f));
            Assert.AreEqual(new Tri(2, 3, 1), Geometry.TriAt(2.9f, 3.5f));
            Assert.AreEqual(new Tri(2, 3, 2), Geometry.TriAt(2.5f, 3.9f));
            Assert.AreEqual(new Tri(2, 3, 3), Geometry.TriAt(2.1f, 3.5f));
        }

        [Test]
        public void BoundaryOfCellHasFourEdges()
        {
            Assert.AreEqual(4, Geometry.Boundary(Geometry.FullRect(1, 1)).Count);
        }
    }
}
