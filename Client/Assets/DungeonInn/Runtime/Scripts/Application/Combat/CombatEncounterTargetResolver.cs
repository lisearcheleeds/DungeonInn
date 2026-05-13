using System;
using System.Collections.Generic;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Map;
using VContainer;

namespace DungeonInn.Application.Combat
{
    public sealed class CombatEncounterTargetResolver
    {
        const float EncounterRangeMeters = 20f;
        const int NeighborCellRadius = 1;

        readonly IGameClock gameClock;
        readonly Dictionary<SpatialCellKey, List<Actor>> actorsByCell = new();
        readonly Dictionary<ActorPairKey, int> successfulLineOfSightTicks = new();
        int lineOfSightCacheTick = -1;

        [Inject]
        public CombatEncounterTargetResolver(IGameClock gameClock)
        {
            this.gameClock = gameClock ?? throw new ArgumentNullException(nameof(gameClock));
        }

        public void Rebuild(IReadOnlyList<Actor> actors)
        {
            if (actors == null)
            {
                throw new ArgumentNullException(nameof(actors));
            }

            RefreshLineOfSightCache();

            foreach (var bucket in actorsByCell.Values)
            {
                bucket.Clear();
            }

            foreach (var actor in actors)
            {
                if (actor.Hp <= 0 || actor.Position.LayerId.Equals(MapLayerId.Ground))
                {
                    continue;
                }

                var cellKey = SpatialCellKey.From(actor.Position);
                if (!actorsByCell.TryGetValue(cellKey, out var bucket))
                {
                    bucket = new List<Actor>();
                    actorsByCell[cellKey] = bucket;
                }

                bucket.Add(actor);
            }
        }

        public Actor FindNearestHostile(Dungeon dungeon, Actor actor)
        {
            if (dungeon == null)
            {
                throw new ArgumentNullException(nameof(dungeon));
            }

            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            Actor nearest = null;
            var nearestDistSq = EncounterRangeMeters * EncounterRangeMeters;
            var actorCell = SpatialCellKey.From(actor.Position);

            for (var z = actorCell.Z - NeighborCellRadius; z <= actorCell.Z + NeighborCellRadius; z++)
            {
                for (var x = actorCell.X - NeighborCellRadius; x <= actorCell.X + NeighborCellRadius; x++)
                {
                    var cellKey = new SpatialCellKey(actorCell.LayerId, x, z);
                    if (!actorsByCell.TryGetValue(cellKey, out var candidates))
                    {
                        continue;
                    }

                    foreach (var candidate in candidates)
                    {
                        if (!CanFight(actor, candidate))
                        {
                            continue;
                        }

                        var distSq = actor.Position.DistanceSquaredTo(candidate.Position);
                        if (nearestDistSq < distSq)
                        {
                            continue;
                        }

                        if (!HasLineOfSight(dungeon, actor, candidate))
                        {
                            continue;
                        }

                        nearestDistSq = distSq;
                        nearest = candidate;
                    }
                }
            }

            return nearest;
        }

        void RefreshLineOfSightCache()
        {
            if (lineOfSightCacheTick == gameClock.CurrentScheduleTick)
            {
                return;
            }

            lineOfSightCacheTick = gameClock.CurrentScheduleTick;
            successfulLineOfSightTicks.Clear();
        }

        bool HasLineOfSight(Dungeon dungeon, Actor actor, Actor candidate)
        {
            var actorPairKey = ActorPairKey.Create(actor.Id, candidate.Id);
            if (successfulLineOfSightTicks.TryGetValue(actorPairKey, out var cachedTick) &&
                cachedTick == gameClock.CurrentScheduleTick)
            {
                return true;
            }

            if (!HasLineOfSight(dungeon, actor.Position, candidate.Position))
            {
                return false;
            }

            successfulLineOfSightTicks[actorPairKey] = gameClock.CurrentScheduleTick;
            return true;
        }

        static bool CanFight(Actor actor, Actor candidate)
        {
            if (candidate.Id == actor.Id)
            {
                return false;
            }

            if (candidate.Hp <= 0)
            {
                return false;
            }

            return candidate.Position.LayerId.Equals(actor.Position.LayerId) &&
                AreHostile(actor.Faction, candidate.Faction);
        }

        static bool HasLineOfSight(Dungeon dungeon, LayerPosition from, LayerPosition to)
        {
            if (!from.LayerId.Equals(to.LayerId))
            {
                return false;
            }

            if (from.LayerId.Equals(MapLayerId.Ground))
            {
                return true;
            }

            if (!dungeon.TryGetFloor(from.LayerId.Value, out var floor))
            {
                return false;
            }

            var dx = to.X - from.X;
            var dz = to.Z - from.Z;
            var distance = (float)Math.Sqrt(dx * dx + dz * dz);
            if (distance <= 0f)
            {
                return true;
            }

            var stepMeters = floor.Layer.CellSizeMeters * 0.5f;
            var stepCount = Math.Max(1, (int)Math.Ceiling(distance / stepMeters));
            for (var i = 0; i <= stepCount; i++)
            {
                var t = (float)i / stepCount;
                var position = new LayerPosition(
                    from.LayerId,
                    from.X + dx * t,
                    from.Z + dz * t);

                if (!floor.Layer.Contains(position))
                {
                    return false;
                }

                if (!floor.IsWalkable(floor.Layer.ToGridPosition(position)))
                {
                    return false;
                }
            }

            return true;
        }

        // TODO: Resolve hostility from FactionMaster.
        static bool AreHostile(ActorFaction a, ActorFaction b)
        {
            return (a.Id == 1 && b.Id == 2) || (a.Id == 2 && b.Id == 1);
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

            public static SpatialCellKey From(LayerPosition position)
            {
                return new SpatialCellKey(
                    position.LayerId.Value,
                    (int)Math.Floor(position.X / EncounterRangeMeters),
                    (int)Math.Floor(position.Z / EncounterRangeMeters));
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

        readonly struct ActorPairKey : IEquatable<ActorPairKey>
        {
            readonly Guid actorAId;
            readonly Guid actorBId;

            ActorPairKey(Guid actorAId, Guid actorBId)
            {
                this.actorAId = actorAId;
                this.actorBId = actorBId;
            }

            public static ActorPairKey Create(Guid actorAId, Guid actorBId)
            {
                return actorAId.CompareTo(actorBId) <= 0
                    ? new ActorPairKey(actorAId, actorBId)
                    : new ActorPairKey(actorBId, actorAId);
            }

            public bool Equals(ActorPairKey other)
            {
                return actorAId.Equals(other.actorAId) && actorBId.Equals(other.actorBId);
            }

            public override bool Equals(object obj)
            {
                return obj is ActorPairKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(actorAId, actorBId);
            }
        }
    }
}
