using NUnit.Framework;
using UnityEngine;
using TopDownMapEditor;

namespace TopDownMapEditor.Tests
{
    public class TopDownGridTests
    {
        GridSettings DefaultSettings() => new(1f, 64, 64);

        [Test]
        public void GridToWorld_Origin_ReturnsZero()
        {
            var settings = DefaultSettings();
            Vector3 world = TopDownGrid.GridToWorld(Vector2Int.zero, settings);
            Assert.AreEqual(0f, world.x, 0.001f);
            Assert.AreEqual(0f, world.z, 0.001f);
        }

        [Test]
        public void RoundTrip_GridToWorldToGrid_IsIdentity()
        {
            var settings = DefaultSettings();
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    var original = new Vector2Int(x, y);
                    Vector3 world = TopDownGrid.GridToWorld(original, settings);
                    Vector2Int result = TopDownGrid.WorldToGrid(world, settings);
                    Assert.AreEqual(original, result, $"Round-trip failed for ({x},{y})");
                }
            }
        }

        [Test]
        public void RoundTrip_WithOffset_IsIdentity()
        {
            var settings = DefaultSettings();
            settings.originOffset = new Vector3(5.5f, 0f, -3.2f);

            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    var original = new Vector2Int(x, y);
                    Vector3 world = TopDownGrid.GridToWorld(original, settings);
                    Vector2Int result = TopDownGrid.WorldToGrid(world, settings);
                    Assert.AreEqual(original, result, $"Round-trip with offset failed for ({x},{y})");
                }
            }
        }

        [Test]
        public void RoundTrip_CustomTileSize_IsIdentity()
        {
            var settings = new GridSettings(2f, 32, 32);
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    var original = new Vector2Int(x, y);
                    Vector3 world = TopDownGrid.GridToWorld(original, settings);
                    Vector2Int result = TopDownGrid.WorldToGrid(world, settings);
                    Assert.AreEqual(original, result, $"Custom tile size round-trip failed for ({x},{y})");
                }
            }
        }

        [Test]
        public void SortingOrder_BackToFront()
        {
            int behindOrder = TopDownGrid.GetSortingOrder(new Vector2Int(0, 0));
            int frontOrder = TopDownGrid.GetSortingOrder(new Vector2Int(5, 5));
            Assert.Greater(frontOrder, behindOrder);
        }

        [Test]
        public void SortingOrder_LayerOffset()
        {
            int ground = TopDownGrid.GetSortingOrder(new Vector2Int(3, 3), 0);
            int objects = TopDownGrid.GetSortingOrder(new Vector2Int(3, 3), 5);
            Assert.Greater(objects, ground);
        }

        [Test]
        public void SortingOrder_SameDiagonal_SameOrder()
        {
            int a = TopDownGrid.GetSortingOrder(new Vector2Int(2, 3));
            int b = TopDownGrid.GetSortingOrder(new Vector2Int(3, 2));
            Assert.AreEqual(a, b);
        }

        [Test]
        public void IsInBounds_Valid()
        {
            var settings = DefaultSettings();
            Assert.IsTrue(settings.IsInBounds(new Vector2Int(0, 0)));
            Assert.IsTrue(settings.IsInBounds(new Vector2Int(63, 63)));
            Assert.IsTrue(settings.IsInBounds(new Vector2Int(32, 32)));
        }

        [Test]
        public void IsInBounds_Invalid()
        {
            var settings = DefaultSettings();
            Assert.IsFalse(settings.IsInBounds(new Vector2Int(-1, 0)));
            Assert.IsFalse(settings.IsInBounds(new Vector2Int(0, -1)));
            Assert.IsFalse(settings.IsInBounds(new Vector2Int(64, 0)));
            Assert.IsFalse(settings.IsInBounds(new Vector2Int(0, 64)));
        }

        [Test]
        public void GetCellWorldCorners_ReturnsFourCorners()
        {
            var settings = DefaultSettings();
            Vector3[] corners = TopDownGrid.GetCellWorldCorners(new Vector2Int(5, 5), settings);
            Assert.AreEqual(4, corners.Length);
        }

        [Test]
        public void GetMapWorldBounds_NonZeroArea()
        {
            var settings = DefaultSettings();
            Bounds bounds = TopDownGrid.GetMapWorldBounds(settings);
            Assert.Greater(bounds.size.x, 0);
            Assert.Greater(bounds.size.z, 0);
        }
    }
}
