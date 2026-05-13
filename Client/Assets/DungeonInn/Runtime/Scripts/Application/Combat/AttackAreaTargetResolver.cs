using System;
using System.Collections.Generic;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Combat;

namespace DungeonInn.Application.Combat
{
    public sealed class AttackAreaTargetResolver
    {
        readonly List<Actor> targets = new();

        public IReadOnlyList<Actor> ResolveTargets(IGameWorldState worldState, AreaEffectInstance areaEffect)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            if (areaEffect == null)
            {
                throw new ArgumentNullException(nameof(areaEffect));
            }

            targets.Clear();
            foreach (var actor in worldState.Actors)
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
            if (radius * radius < areaEffect.CenterPosition.DistanceSquaredTo(actor.Position))
            {
                return false;
            }

            var dz = actor.Position.Z - areaEffect.CenterPosition.Z;
            if (dz < 0f)
            {
                return false;
            }

            var dx = Math.Abs(actor.Position.X - areaEffect.CenterPosition.X);
            var distance = (float)Math.Sqrt(dx * dx + dz * dz);
            if (distance <= 0f)
            {
                return true;
            }

            var halfAngle = areaEffect.AreaSpec.AngleDegrees * 0.5f;
            var angle = (float)(Math.Atan2(dx, dz) * 180f / Math.PI);
            return angle <= halfAngle;
        }
    }
}
