using System;
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
        readonly CombatEncounterTargetResolver targetResolver;
        readonly IEventPublisher eventBus;

        [Inject]
        public DetectCombatEncounterUseCase(
            IActorCombatService actorCombatService,
            CombatEncounterTargetResolver targetResolver,
            IEventPublisher eventBus)
        {
            this.actorCombatService = actorCombatService
                ?? throw new ArgumentNullException(nameof(actorCombatService));
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

            var actors = worldState.Actors;
            targetResolver.Rebuild(actors);

            foreach (var actor in actors)
            {
                if (actor.Position.LayerId.Equals(MapLayerId.Ground))
                {
                    continue;
                }

                var combatState = actorCombatService.GetOrCreateCombatState(actor.Id);
                var hadTarget = combatState.TargetActorId.HasValue;
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
    }
}
