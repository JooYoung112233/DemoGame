using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using TopDownMapEditor;

namespace TopDownMapEditor.Tests
{
    public class BuildingStateTests
    {
        [Test]
        public void VariantSet_GetDefault_ReturnsFirstIfNoDefault()
        {
            var set = ScriptableObject.CreateInstance<BuildingVariantSet>();
            set.variants.Add(new BuildingVariant { variantId = "day", displayName = "Day" });
            set.variants.Add(new BuildingVariant { variantId = "night", displayName = "Night" });

            var result = set.GetDefault();
            Assert.AreEqual("day", result.variantId);

            Object.DestroyImmediate(set);
        }

        [Test]
        public void VariantSet_GetDefault_ReturnsNamedDefault()
        {
            var set = ScriptableObject.CreateInstance<BuildingVariantSet>();
            set.defaultVariantId = "night";
            set.variants.Add(new BuildingVariant { variantId = "day" });
            set.variants.Add(new BuildingVariant { variantId = "night" });

            var result = set.GetDefault();
            Assert.AreEqual("night", result.variantId);

            Object.DestroyImmediate(set);
        }

        [Test]
        public void VariantSet_GetVariant_ReturnsNull_IfNotFound()
        {
            var set = ScriptableObject.CreateInstance<BuildingVariantSet>();
            set.variants.Add(new BuildingVariant { variantId = "day" });

            Assert.IsNull(set.GetVariant("missing"));

            Object.DestroyImmediate(set);
        }

        [Test]
        public void BuildingDefinition_GetOccupiedCells_SingleTile()
        {
            var def = ScriptableObject.CreateInstance<BuildingDefinition>();
            def.footprint = new Vector2Int(1, 1);

            var cells = def.GetOccupiedCells(new Vector2Int(3, 5));
            Assert.AreEqual(1, cells.Count);
            Assert.AreEqual(new Vector2Int(3, 5), cells[0]);

            Object.DestroyImmediate(def);
        }

        [Test]
        public void BuildingDefinition_GetOccupiedCells_MultiTile()
        {
            var def = ScriptableObject.CreateInstance<BuildingDefinition>();
            def.footprint = new Vector2Int(2, 3);

            var cells = def.GetOccupiedCells(new Vector2Int(0, 0));
            Assert.AreEqual(6, cells.Count);
            Assert.Contains(new Vector2Int(0, 0), cells);
            Assert.Contains(new Vector2Int(1, 2), cells);

            Object.DestroyImmediate(def);
        }

        [Test]
        public void WalkabilityData_GetSet()
        {
            var data = new WalkabilityData(10, 10);
            Assert.AreEqual(WalkableType.Walkable, data.GetCell(new Vector2Int(5, 5)));

            data.SetCell(new Vector2Int(5, 5), WalkableType.Blocked);
            Assert.AreEqual(WalkableType.Blocked, data.GetCell(new Vector2Int(5, 5)));
        }

        [Test]
        public void WalkabilityData_OutOfBounds_ReturnsBlocked()
        {
            var data = new WalkabilityData(10, 10);
            Assert.AreEqual(WalkableType.Blocked, data.GetCell(new Vector2Int(-1, 0)));
            Assert.AreEqual(WalkableType.Blocked, data.GetCell(new Vector2Int(0, 10)));
        }

        [Test]
        public void SpawnConditionEvaluator_Always_ReturnsTrue()
        {
            var evaluator = new SpawnConditionEvaluator();
            var condition = new SpawnCondition { type = SpawnConditionType.Always };
            Assert.IsTrue(evaluator.Evaluate(condition));
        }

        [Test]
        public void SpawnConditionEvaluator_EmptyList_ReturnsTrue()
        {
            var evaluator = new SpawnConditionEvaluator();
            Assert.IsTrue(evaluator.EvaluateAll(new List<SpawnCondition>()));
            Assert.IsTrue(evaluator.EvaluateAll(null));
        }

        [Test]
        public void PathfindingHelper_StraightPath()
        {
            var walk = new WalkabilityData(10, 10);
            var settings = new GridSettings(1f, 10, 10);

            var path = PathfindingHelper.FindPath(
                new Vector2Int(0, 0), new Vector2Int(3, 0), walk, settings);

            Assert.IsNotNull(path);
            Assert.AreEqual(new Vector2Int(0, 0), path[0]);
            Assert.AreEqual(new Vector2Int(3, 0), path[path.Count - 1]);
        }

        [Test]
        public void PathfindingHelper_BlockedDestination_ReturnsNull()
        {
            var walk = new WalkabilityData(10, 10);
            walk.SetCell(new Vector2Int(3, 0), WalkableType.Blocked);
            var settings = new GridSettings(1f, 10, 10);

            var path = PathfindingHelper.FindPath(
                new Vector2Int(0, 0), new Vector2Int(3, 0), walk, settings);

            Assert.IsNull(path);
        }
    }
}
