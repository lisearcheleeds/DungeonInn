using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Map;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class DetectCombatEncounterUseCase
    {
        readonly IActorCombatService actorCombatService;
        readonly ActorSpatialIndexService actorSpatialIndexService;
        readonly CombatEncounterTargetResolver targetResolver;
        readonly IEventPublisher eventBus;
        readonly List<Guid> dirtyActorIds = new();
        readonly List<Guid> detectionActorIds = new();
        readonly List<Guid> attackerActorIds = new();
        readonly HashSet<Guid> detectionActorIdSet = new();

        [Inject]
        public DetectCombatEncounterUseCase(
            IActorCombatService actorCombatService,
            ActorSpatialIndexService actorSpatialIndexService,
            CombatEncounterTargetResolver targetResolver,
            IEventPublisher eventBus)
        {
            this.actorCombatService = actorCombatService
                ?? throw new ArgumentNullException(nameof(actorCombatService));
            this.actorSpatialIndexService = actorSpatialIndexService
                ?? throw new ArgumentNullException(nameof(actorSpatialIndexService));
            this.targetResolver = targetResolver
                ?? throw new ArgumentNullException(nameof(targetResolver));
            this.eventBus = eventBus
                ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public UniTask ExecuteAsync(IGameWorldStateReader worldState)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            BuildDetectionActorIds();
            foreach (var actorId in detectionActorIds)
            {
                var actor = worldState.FindActor(actorId);
                if (actor == null)
                {
                    EndEncountersTargeting(worldState, actorId);
                    actorCombatService.RemoveState(actorId);
                    continue;
                }

                var combatState = actorCombatService.GetOrCreateCombatState(actor.Id);
                var hadTarget = combatState.TargetActorId.HasValue;

                if (actor.Hp <= 0 || actor.Position.LayerId.Equals(MapLayerId.Ground))
                {
                    actorCombatService.ClearTarget(actor.Id);
                    if (hadTarget)
                    {
                        eventBus.Publish(new CombatEncounterEnded(actor.Id));
                    }

                    EndEncountersTargeting(worldState, actor.Id);
                    continue;
                }

                var nearest = targetResolver.FindNearestHostile(worldState.Dungeon, actor);

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

        void BuildDetectionActorIds()
        {
            actorSpatialIndexService.ConsumeDirtyActorIds(dirtyActorIds);
            detectionActorIds.Clear();
            detectionActorIdSet.Clear();

            foreach (var actorId in dirtyActorIds)
            {
                AddDetectionActorId(actorId);

                foreach (var attackerId in actorCombatService.GetAttackers(actorId))
                {
                    AddDetectionActorId(attackerId);
                }
            }
        }

        void AddDetectionActorId(Guid actorId)
        {
            if (!detectionActorIdSet.Add(actorId))
            {
                return;
            }

            detectionActorIds.Add(actorId);
        }

        void EndEncountersTargeting(IGameWorldStateReader worldState, Guid targetActorId)
        {
            attackerActorIds.Clear();
            foreach (var attackerId in actorCombatService.GetAttackers(targetActorId))
            {
                attackerActorIds.Add(attackerId);
            }

            foreach (var attackerId in attackerActorIds)
            {
                if (worldState.FindActor(attackerId) != null)
                {
                    eventBus.Publish(new CombatEncounterEnded(attackerId));
                }
            }

            actorCombatService.ClearTargetsReferencing(targetActorId);
        }
    }
}
