using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Economy;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.World;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Map;
using VContainer;

namespace DungeonInn.Application.Actors.Lifecycle
{
    public sealed class RecoverAdventurerAtInnUseCase
    {
        readonly IEventPublisher eventPublisher;
        readonly IGameClock gameClock;
        readonly AdventurerRecoveryStateService recoveryStateService;
        readonly ActorProcessingCandidateService candidateService;
        readonly IWorldGameSettingsRepository worldGameSettingsRepository;
        readonly FacilityEffectService facilityEffectService;

        [Inject]
        public RecoverAdventurerAtInnUseCase(
            IEventPublisher eventPublisher,
            IGameClock gameClock,
            AdventurerRecoveryStateService recoveryStateService,
            ActorProcessingCandidateService candidateService,
            IWorldGameSettingsRepository worldGameSettingsRepository,
            FacilityEffectService facilityEffectService)
        {
            this.eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            this.gameClock = gameClock ?? throw new ArgumentNullException(nameof(gameClock));
            this.recoveryStateService = recoveryStateService ?? throw new ArgumentNullException(nameof(recoveryStateService));
            this.candidateService = candidateService ?? throw new ArgumentNullException(nameof(candidateService));
            this.worldGameSettingsRepository =
                worldGameSettingsRepository ?? throw new ArgumentNullException(nameof(worldGameSettingsRepository));
            this.facilityEffectService = facilityEffectService ?? throw new ArgumentNullException(nameof(facilityEffectService));
        }

        public UniTask ExecuteAsync(IGameWorldState worldState, float deltaGameSeconds)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            var guild = worldState.Guild;
            var actors = worldState.Actors;
            foreach (var actor in actors)
            {
                if (actor.Behavior is not AdventurerBehavior behavior)
                {
                    continue;
                }

                if (behavior.LifecycleState != AdventurerLifecycleState.Recovering)
                {
                    continue;
                }

                if (!actor.Position.LayerId.Equals(MapLayerId.Ground))
                {
                    continue;
                }

                TickRecovery(guild, actor, behavior, deltaGameSeconds);
            }

            return UniTask.CompletedTask;
        }

        void TickRecovery(AdventurerGuild guild, Actor actor, AdventurerBehavior behavior, float deltaGameSeconds)
        {
            if (!guild.TryGetActiveInnReservation(actor.Id, out var reservation))
            {
                candidateService.MarkRecoveryCandidate(actor.Id);
                return;
            }

            var accumulated = recoveryStateService.GetAccumulatedHp(actor.Id);
            var innBalanceSettings = worldGameSettingsRepository.GetInnBalanceSettings();
            var inn = guild.GetFacility(reservation.InnFacilityId);
            var recoveryPercentPerMinute =
                facilityEffectService.CalculateInnHpRecoveryPercentPerMinute(inn, innBalanceSettings);
            accumulated += actor.Params.MaxHp * recoveryPercentPerMinute / 60f * deltaGameSeconds;
            var healAmount = (int)accumulated;

            if (0 < healAmount)
            {
                actor.Recover(healAmount, 0, 0, 0, 0);
                accumulated -= healAmount;
                eventPublisher.Publish(new ActorRecoveringAtInn(actor.Id, actor.Hp, actor.Params.MaxHp));
            }

            recoveryStateService.SetAccumulatedHp(actor.Id, accumulated);

            if (actor.Hp >= actor.Params.MaxHp)
            {
                recoveryStateService.Remove(actor.Id);
                candidateService.ClearRecoveryCandidate(actor.Id);
                guild.ReleaseInnReservation(actor.Id, gameClock.CurrentScheduleTick);
                behavior.ChangeLifecycleState(AdventurerLifecycleState.Preparing);
                eventPublisher.Publish(new ActorFullyRecovered(actor.Id));
            }
        }
    }
}
