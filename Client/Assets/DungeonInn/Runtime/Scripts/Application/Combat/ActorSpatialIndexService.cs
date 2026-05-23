using System;
using System.Collections.Generic;
using DungeonInn.Application.World;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Map;
using VContainer;

namespace DungeonInn.Application.Combat
{
    public sealed class ActorSpatialIndexService
    {
        readonly Dictionary<SpatialCellKey, List<Actor>> actorsByCell = new();
        readonly Dictionary<Guid, SpatialCellKey> cellByActorId = new();
        readonly HashSet<Guid> dirtyActorIds = new();
        readonly CombatBalanceSettings combatBalanceSettings;

        public int DirtyActorCount => dirtyActorIds.Count;
        public int Revision { get; private set; }

        [Inject]
        public ActorSpatialIndexService(CombatBalanceSettings combatBalanceSettings)
        {
            this.combatBalanceSettings = combatBalanceSettings
                ?? throw new ArgumentNullException(nameof(combatBalanceSettings));
        }

        public void SyncActor(Actor actor)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            dirtyActorIds.Add(actor.Id);
            Revision++;

            if (!CanIndex(actor))
            {
                RemoveFromCell(actor.Id);
                return;
            }

            var nextCell = SpatialCellKey.From(actor.Position, combatBalanceSettings.SpatialIndexCellSizeMeters);
            if (cellByActorId.TryGetValue(actor.Id, out var currentCell) && currentCell.Equals(nextCell))
            {
                return;
            }

            RemoveFromCell(actor.Id);

            if (!actorsByCell.TryGetValue(nextCell, out var bucket))
            {
                bucket = new List<Actor>();
                actorsByCell[nextCell] = bucket;
            }

            bucket.Add(actor);
            cellByActorId[actor.Id] = nextCell;
        }

        public void RemoveActor(Guid actorId)
        {
            dirtyActorIds.Add(actorId);
            Revision++;
            RemoveFromCell(actorId);
        }

        public void CollectNearbyActors(
            LayerPosition position,
            int neighborCellRadius,
            List<Actor> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            if (neighborCellRadius < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(neighborCellRadius));
            }

            var centerCell = SpatialCellKey.From(position, combatBalanceSettings.SpatialIndexCellSizeMeters);
            for (var z = centerCell.Z - neighborCellRadius; z <= centerCell.Z + neighborCellRadius; z++)
            {
                for (var x = centerCell.X - neighborCellRadius; x <= centerCell.X + neighborCellRadius; x++)
                {
                    var cell = new SpatialCellKey(centerCell.LayerId, x, z);
                    if (!actorsByCell.TryGetValue(cell, out var bucket))
                    {
                        continue;
                    }

                    foreach (var actor in bucket)
                    {
                        results.Add(actor);
                    }
                }
            }
        }

        public void ConsumeDirtyActorIds(List<Guid> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();
            foreach (var actorId in dirtyActorIds)
            {
                results.Add(actorId);
            }

            dirtyActorIds.Clear();
        }

        static bool CanIndex(Actor actor)
        {
            return 0 < actor.Hp && !actor.Position.LayerId.Equals(MapLayerId.Ground);
        }

        void RemoveFromCell(Guid actorId)
        {
            if (!cellByActorId.TryGetValue(actorId, out var currentCell))
            {
                return;
            }

            if (actorsByCell.TryGetValue(currentCell, out var bucket))
            {
                for (var i = bucket.Count - 1; 0 <= i; i--)
                {
                    if (!bucket[i].Id.Equals(actorId))
                    {
                        continue;
                    }

                    bucket.RemoveAt(i);
                    break;
                }
            }

            cellByActorId.Remove(actorId);
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
