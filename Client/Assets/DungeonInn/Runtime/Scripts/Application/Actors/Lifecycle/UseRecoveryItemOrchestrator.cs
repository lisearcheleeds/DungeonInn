using System;
using System.Collections.Generic;
using DungeonInn.Application.Actors.Ai;
using DungeonInn.Application.Actors.Lifecycle;
using DungeonInn.Application.Actors.Movement;
using DungeonInn.Application.Actors.Profiles;
using DungeonInn.Application.Actors.Spawn;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Dungeons;
using DungeonInn.Application.Economy;
using DungeonInn.Application.Facilities;
using DungeonInn.Application.Items;
using DungeonInn.Application.World;

using Cysharp.Threading.Tasks;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Item;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.Actors.Lifecycle
{
    public sealed class UseRecoveryItemOrchestrator
    {
        readonly UseConsumableItemUseCase useConsumableItemUseCase;
        readonly IEventPublisher eventBus;
        readonly ActorProcessingCandidateService candidateService;
        readonly IWorldGameSettingsRepository worldGameSettingsRepository;
        readonly RecoveryItemCandidateQuery recoveryItemCandidateQuery;
        readonly RecoveryItemSelectionPolicy recoveryItemSelectionPolicy;
        readonly List<Guid> actorIdBuffer = new();

        [Inject]
        public UseRecoveryItemOrchestrator(
            UseConsumableItemUseCase useConsumableItemUseCase,
            IEventPublisher eventBus,
            ActorProcessingCandidateService candidateService,
            IWorldGameSettingsRepository worldGameSettingsRepository,
            RecoveryItemCandidateQuery recoveryItemCandidateQuery,
            RecoveryItemSelectionPolicy recoveryItemSelectionPolicy)
        {
            this.useConsumableItemUseCase = useConsumableItemUseCase ?? throw new ArgumentNullException(nameof(useConsumableItemUseCase));
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            this.candidateService = candidateService ?? throw new ArgumentNullException(nameof(candidateService));
            this.worldGameSettingsRepository =
                worldGameSettingsRepository ?? throw new ArgumentNullException(nameof(worldGameSettingsRepository));
            this.recoveryItemCandidateQuery =
                recoveryItemCandidateQuery ?? throw new ArgumentNullException(nameof(recoveryItemCandidateQuery));
            this.recoveryItemSelectionPolicy =
                recoveryItemSelectionPolicy ?? throw new ArgumentNullException(nameof(recoveryItemSelectionPolicy));
        }

        public async UniTask ExecuteAsync(IGameWorldState worldState)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            candidateService.CollectRecoveryItemCandidates(actorIdBuffer);
            foreach (var actorId in actorIdBuffer)
            {
                var actor = worldState.FindActor(actorId);
                if (actor == null)
                {
                    candidateService.RemoveActor(actorId);
                    continue;
                }

                if (actor.Behavior is not AdventurerBehavior behavior ||
                    behavior.LifecycleState != AdventurerLifecycleState.Exploring)
                {
                    candidateService.ClearRecoveryItemCandidate(actor.Id);
                    continue;
                }

                var candidates = recoveryItemCandidateQuery.Execute(actor);
                if (worldGameSettingsRepository.GetAdventurerReturnPolicySettings().LowHpRatio
                    < actor.Hp / (float)actor.Params.MaxHp && !CanUseFullRecovery(actor, candidates))
                {
                    candidateService.ClearRecoveryItemCandidate(actor.Id);
                    continue;
                }

                var itemId = recoveryItemSelectionPolicy.SelectItemId(actor, candidates);
                if (itemId < 1)
                {
                    candidateService.ClearRecoveryItemCandidate(actor.Id);
                    continue;
                }

                var used = await useConsumableItemUseCase.ExecuteAsync(actor, itemId);
                if (used)
                {
                    candidateService.ClearRecoveryItemCandidate(actor.Id);
                    eventBus.Publish(new ActorAiDecisionRecorded(
                        actor.Id,
                        AiDecisionType.UseRecoveryItem,
                        AiDecisionReasonType.LowHpWithRecoveryItem,
                        default,
                        default,
                        currentHp: actor.Hp,
                        maxHp: actor.Params.MaxHp,
                        selectedFloor: 0,
                        score: 0));
                }
            }
        }

        static bool CanUseFullRecovery(Actor actor, IReadOnlyList<RecoveryItemCandidate> candidates)
        {
            var missingHp = actor.Params.MaxHp - actor.Hp;
            foreach (var candidate in candidates)
            {
                if (candidate.HealAmount <= missingHp)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
