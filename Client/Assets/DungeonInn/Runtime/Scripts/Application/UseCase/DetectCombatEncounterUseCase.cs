using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Map;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class DetectCombatEncounterUseCase
    {
        const float EncounterRangeMeters = 20f;

        readonly IActorCombatService actorCombatService;
        readonly IEventPublisher eventBus;

        [Inject]
        public DetectCombatEncounterUseCase(IActorCombatService actorCombatService, IEventPublisher eventBus)
        {
            this.actorCombatService = actorCombatService
                ?? throw new ArgumentNullException(nameof(actorCombatService));
            this.eventBus = eventBus
                ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public UniTask ExecuteAsync(IGameWorldState worldState)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            var actors = worldState.Actors;

            foreach (var actor in actors)
            {
                if (actor.Position.LayerId.Equals(MapLayerId.Ground))
                {
                    continue;
                }

                var combatState = actorCombatService.GetOrCreateCombatState(actor.Id);
                var hadTarget = combatState.TargetActorId.HasValue;
                var nearest = FindNearestHostile(worldState.Dungeon, actor, actors);

                if (nearest != null)
                {
                    var isNewTarget = !hadTarget || !combatState.TargetActorId.Value.Equals(nearest.Id);
                    if (hadTarget && isNewTarget)
                    {
                        eventBus.Publish(new CombatEncounterEnded(actor.Id));
                    }

                    actorCombatService.SetTarget(actor.Id, nearest.Id);

                    if (!hadTarget || isNewTarget)
                    {
                        eventBus.Publish(new ActorAiDecisionRecorded(
                            actor.Id,
                            AiDecisionType.StartCombat,
                            AiDecisionReasonType.NearestHostileInRange,
                            nearest.Id,
                            default,
                            0,
                            0,
                            0,
                            0));
                        eventBus.Publish(new CombatEncounterStarted(actor.Id, nearest.Id));
                    }
                }
                else
                {
                    actorCombatService.ClearTarget(actor.Id);
                    if (hadTarget)
                    {
                        eventBus.Publish(new CombatEncounterEnded(actor.Id));
                    }
                }
            }

            return UniTask.CompletedTask;
        }

        static Actor FindNearestHostile(Dungeon dungeon, Actor actor, IReadOnlyList<Actor> actors)
        {
            Actor nearest = null;
            var nearestDistSq = EncounterRangeMeters * EncounterRangeMeters;

            foreach (var candidate in actors)
            {
                if (candidate.Id == actor.Id)
                {
                    continue;
                }

                if (candidate.Hp <= 0)
                {
                    continue;
                }

                if (!candidate.Position.LayerId.Equals(actor.Position.LayerId))
                {
                    continue;
                }

                if (!AreHostile(actor.Faction, candidate.Faction))
                {
                    continue;
                }

                var distSq = actor.Position.DistanceSquaredTo(candidate.Position);
                if (distSq <= nearestDistSq)
                {
                    if (!HasLineOfSight(dungeon, actor.Position, candidate.Position))
                    {
                        continue;
                    }

                    nearestDistSq = distSq;
                    nearest = candidate;
                }
            }

            return nearest;
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

        // TODO: FactionMasterから取得する。
        static bool AreHostile(ActorFaction a, ActorFaction b)
        {
            return (a.Id == 1 && b.Id == 2) || (a.Id == 2 && b.Id == 1);
        }
    }
}
