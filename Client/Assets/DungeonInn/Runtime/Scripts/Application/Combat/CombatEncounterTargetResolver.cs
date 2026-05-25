using System;
using System.Collections.Generic;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.World;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Map;
using VContainer;

namespace DungeonInn.Application.Combat
{
    public sealed class CombatEncounterTargetResolver
    {
        readonly IGameClock gameClock;
        readonly ActorSpatialIndexService actorSpatialIndexService;
        readonly List<Actor> candidates = new();
        readonly Dictionary<ActorPairKey, int> successfulLineOfSightTicks = new();
        readonly IWorldGameSettingsRepository worldGameSettingsRepository;
        int lineOfSightCacheTick = -1;
        int lineOfSightCacheSpatialIndexRevision = -1;

        [Inject]
        public CombatEncounterTargetResolver(
            IGameClock gameClock,
            ActorSpatialIndexService actorSpatialIndexService,
            IWorldGameSettingsRepository worldGameSettingsRepository)
        {
            this.gameClock = gameClock ?? throw new ArgumentNullException(nameof(gameClock));
            this.actorSpatialIndexService = actorSpatialIndexService
                ?? throw new ArgumentNullException(nameof(actorSpatialIndexService));
            this.worldGameSettingsRepository = worldGameSettingsRepository
                ?? throw new ArgumentNullException(nameof(worldGameSettingsRepository));
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
            var combatBalanceSettings = worldGameSettingsRepository.GetCombatBalanceSettings();
            var nearestDistSq = combatBalanceSettings.EncounterRangeMeters *
                combatBalanceSettings.EncounterRangeMeters;
            var neighborCellRadius = CalculateNeighborCellRadius();

            RefreshLineOfSightCache();
            candidates.Clear();
            actorSpatialIndexService.CollectNearbyActors(actor.Position, neighborCellRadius, candidates);
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

                if (!HasLineOfSightCached(dungeon, actor, candidate))
                {
                    continue;
                }

                nearestDistSq = distSq;
                nearest = candidate;
            }

            return nearest;
        }

        public bool HasLineOfSight(Dungeon dungeon, Actor actor, Actor target)
        {
            if (dungeon == null)
            {
                throw new ArgumentNullException(nameof(dungeon));
            }

            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            return HasLineOfSight(dungeon, actor.Position, target.Position);
        }

        void RefreshLineOfSightCache()
        {
            if (lineOfSightCacheTick == gameClock.CurrentScheduleTick &&
                lineOfSightCacheSpatialIndexRevision == actorSpatialIndexService.Revision)
            {
                return;
            }

            lineOfSightCacheTick = gameClock.CurrentScheduleTick;
            lineOfSightCacheSpatialIndexRevision = actorSpatialIndexService.Revision;
            successfulLineOfSightTicks.Clear();
        }

        int CalculateNeighborCellRadius()
        {
            var combatBalanceSettings = worldGameSettingsRepository.GetCombatBalanceSettings();
            return Math.Max(
                1,
                (int)Math.Ceiling(
                    combatBalanceSettings.EncounterRangeMeters /
                    combatBalanceSettings.SpatialIndexCellSizeMeters));
        }

        bool HasLineOfSightCached(Dungeon dungeon, Actor actor, Actor candidate)
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
