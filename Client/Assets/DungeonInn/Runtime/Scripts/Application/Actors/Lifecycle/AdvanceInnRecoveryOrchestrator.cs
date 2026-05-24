using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Actors.Ai;
using DungeonInn.Application.Economy;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.World;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Map;
using VContainer;

namespace DungeonInn.Application.Actors.Lifecycle
{
    public sealed class AdvanceInnRecoveryOrchestrator
    {
        readonly RecoverAdventurerAtInnUseCase recoverAdventurerAtInnUseCase;
        readonly ChargeInnFeeUseCase chargeInnFeeUseCase;
        readonly DespawnAdventurerUseCase despawnAdventurerUseCase;
        readonly IEventPublisher eventPublisher;
        readonly IGameClock gameClock;
        readonly ActorProcessingCandidateService candidateService;
        readonly IWorldGameSettingsRepository worldGameSettingsRepository;
        readonly List<Guid> reservationActorIdBuffer = new();

        [Inject]
        public AdvanceInnRecoveryOrchestrator(
            RecoverAdventurerAtInnUseCase recoverAdventurerAtInnUseCase,
            ChargeInnFeeUseCase chargeInnFeeUseCase,
            DespawnAdventurerUseCase despawnAdventurerUseCase,
            IEventPublisher eventPublisher,
            IGameClock gameClock,
            ActorProcessingCandidateService candidateService,
            IWorldGameSettingsRepository worldGameSettingsRepository)
        {
            this.recoverAdventurerAtInnUseCase = recoverAdventurerAtInnUseCase
                ?? throw new ArgumentNullException(nameof(recoverAdventurerAtInnUseCase));
            this.chargeInnFeeUseCase = chargeInnFeeUseCase
                ?? throw new ArgumentNullException(nameof(chargeInnFeeUseCase));
            this.despawnAdventurerUseCase = despawnAdventurerUseCase
                ?? throw new ArgumentNullException(nameof(despawnAdventurerUseCase));
            this.eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            this.gameClock = gameClock ?? throw new ArgumentNullException(nameof(gameClock));
            this.candidateService = candidateService ?? throw new ArgumentNullException(nameof(candidateService));
            this.worldGameSettingsRepository =
                worldGameSettingsRepository ?? throw new ArgumentNullException(nameof(worldGameSettingsRepository));
        }

        public UniTask EnsureReservationsAsync(IGameWorldState worldState, int currentTick)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            var guild = worldState.Guild;
            candidateService.CollectReservationCandidates(reservationActorIdBuffer);
            for (var i = reservationActorIdBuffer.Count - 1; 0 <= i; i--)
            {
                var actorId = reservationActorIdBuffer[i];
                var actor = worldState.FindActor(actorId);
                if (actor == null)
                {
                    candidateService.RemoveActor(actorId);
                    continue;
                }

                if (actor.Behavior is not AdventurerBehavior behavior)
                {
                    candidateService.RemoveActor(actor.Id);
                    continue;
                }

                if (behavior.LifecycleState != AdventurerLifecycleState.Recovering &&
                    behavior.LifecycleState != AdventurerLifecycleState.WaitingForInn)
                {
                    candidateService.ClearReservationCandidate(actor.Id);
                    continue;
                }

                if (!actor.Position.LayerId.Equals(MapLayerId.Ground))
                {
                    candidateService.ClearReservationCandidate(actor.Id);
                    continue;
                }

                EnsureInnReservation(worldState, guild, actor, behavior, currentTick);
            }

            return UniTask.CompletedTask;
        }

        public UniTask ExecuteAsync(IGameWorldState worldState, float deltaGameSeconds)
        {
            return recoverAdventurerAtInnUseCase.ExecuteAsync(worldState, deltaGameSeconds);
        }

        void EnsureInnReservation(
            IGameWorldState worldState,
            AdventurerGuild guild,
            Actor actor,
            AdventurerBehavior behavior,
            int currentTick)
        {
            if (guild.HasActiveInnReservation(actor.Id))
            {
                if (behavior.LifecycleState == AdventurerLifecycleState.WaitingForInn)
                {
                    behavior.ClearWaitingForInn();
                    behavior.ChangeLifecycleState(AdventurerLifecycleState.Recovering);
                    candidateService.MarkRecoveryCandidate(actor.Id);
                }

                return;
            }

            foreach (var facility in guild.Facilities)
            {
                if (facility.Type != FacilityType.Inn)
                {
                    continue;
                }

                if (!guild.CanReserveInn(facility.Id))
                {
                    ChangeToWaitingForInn(worldState, actor, behavior, facility);
                    continue;
                }

                if (!chargeInnFeeUseCase.Execute(actor, guild, facility))
                {
                    behavior.ClearWaitingForInn();
                    behavior.ChangeLifecycleState(AdventurerLifecycleState.Preparing);
                    candidateService.ClearReservationCandidate(actor.Id);
                    return;
                }

                guild.ReserveInn(Guid.NewGuid(), actor, facility.Id, currentTick);
                behavior.ClearWaitingForInn();
                behavior.ChangeLifecycleState(AdventurerLifecycleState.Recovering);
                candidateService.MarkRecoveryCandidate(actor.Id);
                eventPublisher.Publish(new ActorReservedInn(actor.Id, facility.Id));
                return;
            }
        }

        void ChangeToWaitingForInn(
            IGameWorldState worldState,
            Actor actor,
            AdventurerBehavior behavior,
            Facility facility)
        {
            var wasWaiting = behavior.LifecycleState == AdventurerLifecycleState.WaitingForInn;
            var innBalanceSettings = worldGameSettingsRepository.GetInnBalanceSettings();
            behavior.StartWaitingForInn(gameClock.CurrentDay);

            var waitedDays = gameClock.CurrentDay - behavior.WaitingForInnStartedDay;
            if (innBalanceSettings.AdventurerWaitDepartureDays <= waitedDays)
            {
                candidateService.RemoveActor(actor.Id);
                despawnAdventurerUseCase.Execute(worldState, actor, waitedDays);
                return;
            }

            if (wasWaiting)
            {
                return;
            }

            eventPublisher.Publish(new InnSatisfactionChanged(
                actor.Id,
                innBalanceSettings.WaitingSatisfactionDelta,
                InnSatisfactionChangeReason.WaitingForInn));
            eventPublisher.Publish(new ActorAiDecisionRecorded(
                actor.Id,
                AiDecisionType.WaitForInn,
                AiDecisionReasonType.NoVacantInnRoom,
                default,
                facility.Id,
                0,
                0,
                0,
                0));
            eventPublisher.Publish(new ActorWaitingForInn(actor.Id, facility.Id));
        }
    }
}
