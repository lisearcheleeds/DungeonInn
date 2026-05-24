using System;
using System.Collections.Generic;
using DungeonInn.Application.World;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using VContainer;

namespace DungeonInn.Application.Items
{
    public sealed class ItemSpatialIndexService
    {
        readonly Dictionary<SpatialCellKey, List<ItemInstance>> itemsByCell = new();
        readonly Dictionary<Guid, SpatialCellKey> cellByItemId = new();
        readonly IWorldGameSettingsRepository worldGameSettingsRepository;

        [Inject]
        public ItemSpatialIndexService(IWorldGameSettingsRepository worldGameSettingsRepository)
        {
            this.worldGameSettingsRepository =
                worldGameSettingsRepository ?? throw new ArgumentNullException(nameof(worldGameSettingsRepository));
        }

        public void SyncItem(ItemInstance item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var nextCell = SpatialCellKey.From(
                item.Position,
                worldGameSettingsRepository.GetCombatBalanceSettings().SpatialIndexCellSizeMeters);
            if (cellByItemId.TryGetValue(item.InstanceId, out var currentCell) && currentCell.Equals(nextCell))
            {
                return;
            }

            RemoveItem(item.InstanceId);

            if (!itemsByCell.TryGetValue(nextCell, out var bucket))
            {
                bucket = new List<ItemInstance>();
                itemsByCell[nextCell] = bucket;
            }

            bucket.Add(item);
            cellByItemId[item.InstanceId] = nextCell;
        }

        public void RemoveItem(Guid itemInstanceId)
        {
            if (!cellByItemId.TryGetValue(itemInstanceId, out var currentCell))
            {
                return;
            }

            if (itemsByCell.TryGetValue(currentCell, out var bucket))
            {
                for (var i = bucket.Count - 1; 0 <= i; i--)
                {
                    if (!bucket[i].InstanceId.Equals(itemInstanceId))
                    {
                        continue;
                    }

                    bucket.RemoveAt(i);
                    break;
                }
            }

            cellByItemId.Remove(itemInstanceId);
        }

        public void CollectNearbyItems(
            LayerPosition position,
            int neighborCellRadius,
            List<ItemInstance> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            if (neighborCellRadius < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(neighborCellRadius));
            }

            var centerCell = SpatialCellKey.From(
                position,
                worldGameSettingsRepository.GetCombatBalanceSettings().SpatialIndexCellSizeMeters);
            for (var z = centerCell.Z - neighborCellRadius; z <= centerCell.Z + neighborCellRadius; z++)
            {
                for (var x = centerCell.X - neighborCellRadius; x <= centerCell.X + neighborCellRadius; x++)
                {
                    var cell = new SpatialCellKey(centerCell.LayerId, x, z);
                    if (!itemsByCell.TryGetValue(cell, out var bucket))
                    {
                        continue;
                    }

                    foreach (var item in bucket)
                    {
                        results.Add(item);
                    }
                }
            }
        }

        readonly struct SpatialCellKey : IEquatable<SpatialCellKey>
        {
            public int LayerId { get; }
            public int X { get; }
            public int Z { get; }

            public SpatialCellKey(int layerId, int x, int z)
            {
                LayerId = layerId;
                X = x;
                Z = z;
            }

            public static SpatialCellKey From(LayerPosition position, float cellSizeMeters)
            {
                return new SpatialCellKey(
                    position.LayerId.Value,
                    (int)Math.Floor(position.X / cellSizeMeters),
                    (int)Math.Floor(position.Z / cellSizeMeters));
            }

            public bool Equals(SpatialCellKey other)
            {
                return LayerId == other.LayerId && X == other.X && Z == other.Z;
            }

            public override bool Equals(object obj)
            {
                return obj is SpatialCellKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(LayerId, X, Z);
            }
        }
    }
}
