using System;
using System.Collections.Generic;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Combat;
using VContainer;

namespace DungeonInn.Application.Combat
{
    public sealed class AttackAreaTargetResolver
    {
        readonly ActorSpatialIndexService actorSpatialIndexService;
        readonly List<Actor> candidates = new();
        readonly List<Actor> targets = new();

        [Inject]
        public AttackAreaTargetResolver(ActorSpatialIndexService actorSpatialIndexService)
        {
            this.actorSpatialIndexService = actorSpatialIndexService
                ?? throw new ArgumentNullException(nameof(actorSpatialIndexService));
        }

        public IReadOnlyList<Actor> ResolveTargets(AreaEffectInstance areaEffect)
        {
            if (areaEffect == null)
            {
                throw new ArgumentNullException(nameof(areaEffect));
            }

            targets.Clear();
            candidates.Clear();
            actorSpatialIndexService.CollectNearbyActors(
                areaEffect.CenterPosition,
                CalculateNeighborCellRadius(areaEffect),
                candidates);

            foreach (var actor in candidates)
            {
                if (actor.Hp <= 0 ||
                    actor.Id.Equals(areaEffect.AttackerActorId) ||
                    actor.Faction.Id == areaEffect.SourceFactionId ||
                    !actor.Position.LayerId.Equals(areaEffect.CenterPosition.LayerId) ||
                    !areaEffect.CanHitActor(actor.Id) ||
                    !Contains(areaEffect, actor))
                {
                    continue;
                }

                targets.Add(actor);
            }

            return targets;
        }

        static int CalculateNeighborCellRadius(AreaEffectInstance areaEffect)
        {
            return Math.Max(
                1,
                (int)Math.Ceiling(
                    CalculateBoundingRadius(areaEffect.AreaSpec) /
                    GameConstants.ActorSpatialIndexCellSizeMeters));
        }

        static float CalculateBoundingRadius(AttackAreaSpec areaSpec)
        {
            switch (areaSpec.Shape)
            {
                case AttackAreaShape.Circle:
                case AttackAreaShape.Fan:
                    return areaSpec.RadiusMeters;
                case AttackAreaShape.Rectangle:
                    var halfWidth = areaSpec.WidthMeters * 0.5f;
                    var halfLength = areaSpec.LengthMeters * 0.5f;
                    return (float)Math.Sqrt(halfWidth * halfWidth + halfLength * halfLength);
                default:
                    throw new ArgumentOutOfRangeException(nameof(areaSpec));
            }
        }

        static bool Contains(AreaEffectInstance areaEffect, Actor actor)
        {
            switch (areaEffect.AreaSpec.Shape)
            {
                case AttackAreaShape.Circle:
                    var radius = areaEffect.AreaSpec.RadiusMeters;
                    return areaEffect.CenterPosition.DistanceSquaredTo(actor.Position) <= radius * radius;
                case AttackAreaShape.Rectangle:
                    return ContainsRectangle(areaEffect, actor);
                case AttackAreaShape.Fan:
                    return ContainsFan(areaEffect, actor);
                default:
                    throw new ArgumentOutOfRangeException(nameof(areaEffect));
            }
        }

        static bool ContainsRectangle(AreaEffectInstance areaEffect, Actor actor)
        {
            var halfWidth = areaEffect.AreaSpec.WidthMeters * 0.5f;
            var halfLength = areaEffect.AreaSpec.LengthMeters * 0.5f;
            return Math.Abs(actor.Position.X - areaEffect.CenterPosition.X) <= halfWidth &&
                Math.Abs(actor.Position.Z - areaEffect.CenterPosition.Z) <= halfLength;
        }

        static bool ContainsFan(AreaEffectInstance areaEffect, Actor actor)
        {
            var radius = areaEffect.AreaSpec.RadiusMeters;
            var dx = Math.Abs(actor.Position.X - areaEffect.CenterPosition.X);
            var dz = actor.Position.Z - areaEffect.CenterPosition.Z;
            var distSq = dx * dx + dz * dz;
            if (radius * radius < distSq)
            {
                return false;
            }

            if (dz < 0f)
            {
                return false;
            }

            if (distSq <= 0f)
            {
                return true;
            }

            var cos = areaEffect.HalfAngleCos;
            if (cos <= 0.0)
            {
                return true;
            }

            return distSq * cos * cos <= dz * dz;
        }
    }
}
