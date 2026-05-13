using System;
using System.Linq;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Map;
using NUnit.Framework;

namespace DungeonInn.Tests.EditMode
{
    public sealed class WorldMapLayerViewDataTests
    {
        [Test]
        public void LayerViewDataStoresCellKindsAsSnapshot()
        {
            var cellKinds = new[]
            {
                WorldMapCellViewKind.GroundWalkable,
                WorldMapCellViewKind.GroundBlocked,
                WorldMapCellViewKind.StairUp,
                WorldMapCellViewKind.StairDown
            };
            var layerData = new WorldMapLayerViewData(MapLayerId.Ground, "Ground", 2, 2, cellKinds);

            cellKinds[0] = WorldMapCellViewKind.DungeonBlocked;

            Assert.That(
                layerData.GetCellKind(new GridPosition(0, 0)),
                Is.EqualTo(WorldMapCellViewKind.GroundWalkable));
            Assert.That(
                layerData.GetCellKind(new GridPosition(1, 0)),
                Is.EqualTo(WorldMapCellViewKind.GroundBlocked));
            Assert.That(
                layerData.GetCellKind(new GridPosition(0, 1)),
                Is.EqualTo(WorldMapCellViewKind.StairUp));
            Assert.That(
                layerData.GetCellKind(new GridPosition(1, 1)),
                Is.EqualTo(WorldMapCellViewKind.StairDown));
        }

        [Test]
        public void LayerViewDataDoesNotStoreDeferredResolver()
        {
            var delegateFields = typeof(WorldMapLayerViewData)
                .GetFields(System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public)
                .Where(field => typeof(Delegate).IsAssignableFrom(field.FieldType))
                .Select(field => field.Name)
                .ToArray();

            Assert.That(delegateFields, Is.Empty);
        }
    }
}
