using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Map;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class RecoverAdventurerAtInnUseCase
    {
        readonly IEventPublisher eventPublisher;
        readonly IGameClock gameClock;
        readonly ChargeInnFeeService chargeInnFeeService;
        readonly DespawnAdventurerService despawnAdventurerService;
        readonly AdventurerRecoveryStateService recoveryStateService;

        [Inject]
        public RecoverAdventurerAtInnUseCase(
            IEventPublisher eventPublisher,
            IGameClock gameClock,
            ChargeInnFeeService chargeInnFeeService,
            DespawnAdventurerService despawnAdventurerService,
            AdventurerRecoveryStateService recoveryStateService)
        {
            this.eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            this.gameClock = gameClock ?? throw new ArgumentNullException(nameof(gameClock));
            this.chargeInnFeeService = chargeInnFeeService ?? throw new ArgumentNullException(nameof(chargeInnFeeService));
            this.despawnAdventurerService = despawnAdventurerService ?? throw new ArgumentNullException(nameof(despawnAdventurerService));
            this.recoveryStateService = recoveryStateService ?? throw new ArgumentNullException(nameof(recoveryStateService));
        }

        public RecoverAdventurerAtInnUseCase(
            IEventPublisher eventPublisher,
            IGameClock gameClock,
            ChargeInnFeeUseCase chargeInnFeeUseCase,
            DespawnAdventurerUseCase despawnAdventurerUseCase,
            AdventurerRecoveryStateService recoveryStateService)
            : this(
                eventPublisher,
                gameClock,
                new ChargeInnFeeService(eventPublisher),
                new DespawnAdventurerService(eventPublisher),
                recoveryStateService)
        {
        }

        public UniTask EnsureReservationsAsync(IGameWorldState worldState, int currentTick)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            var guild = worldState.Guild;
            var actors = new List<Actor>(worldState.Actors);

            foreach (var actor in actors)
            {
                if (actor.Behavior is not AdventurerBehavior behavior)
                {
                    continue;
                }

                if (behavior.LifecycleState != AdventurerLifecycleState.Recovering &&
                    behavior.LifecycleState != AdventurerLifecycleState.WaitingForInn)
                {
                    continue;
                }

                if (!actor.Position.LayerId.Equals(MapLayerId.Ground))
                {
                    continue;
                }

                EnsureInnReservation(worldState, guild, actor, behavior, currentTick);
            }

            return UniTask.CompletedTask;
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

                if (!chargeInnFeeService.Execute(actor, guild, worldState))
                {
                    behavior.ClearWaitingForInn();
                    behavior.ChangeLifecycleState(AdventurerLifecycleState.Preparing);
                    return;
                }

                guild.ReserveInn(Guid.NewGuid(), actor, facility.Id, currentTick);
                behavior.ClearWaitingForInn();
                behavior.ChangeLifecycleState(AdventurerLifecycleState.Recovering);
                eventPublisher.Publish(new ActorReservedInn(actor.Id, facility.Id));
                return;
            }
        }

        void ChangeToWaitingForInn(
            IGameWorldState worldState,
            Actor actor,
            AdventurerBehavior behavior,
            DungeonInn.Domain.Facility.Facility facility)
        {
            var wasWaiting = behavior.LifecycleState == AdventurerLifecycleState.WaitingForInn;
            behavior.StartWaitingForInn(gameClock.CurrentDay);

            var waitedDays = gameClock.CurrentDay - behavior.WaitingForInnStartedDay;
            if (GameConstants.AdventurerInnWaitDepartureDays <= waitedDays)
            {
                despawnAdventurerService.Execute(worldState, actor, waitedDays);
                return;
            }

            if (wasWaiting)
            {
                return;
            }

            worldState.InnEconomy.RecordRejectedGuest(GameConstants.InnWaitingSatisfactionDelta);
            eventPublisher.Publish(new InnSatisfactionChanged(
                actor.Id,
                GameConstants.InnWaitingSatisfactionDelta,
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

        void TickRecovery(AdventurerGuild guild, Actor actor, AdventurerBehavior behavior, float deltaGameSeconds)
        {
            if (!guild.HasActiveInnReservation(actor.Id))
            {
                return;
            }

            var accumulated = recoveryStateService.GetAccumulatedHp(actor.Id);
            accumulated += actor.Params.MaxHp * GameConstants.InnHpRecoveryPercentPerMinute / 60f * deltaGameSeconds;
            var healAmount = (int)accumulated;

            if (healAmount > 0)
            {
                actor.Recover(healAmount, 0, 0, 0, 0);
                accumulated -= healAmount;
                eventPublisher.Publish(new ActorRecoveringAtInn(actor.Id, actor.Hp, actor.Params.MaxHp));
            }

            recoveryStateService.SetAccumulatedHp(actor.Id, accumulated);

            if (actor.Hp >= actor.Params.MaxHp)
            {
                recoveryStateService.Remove(actor.Id);
                guild.ReleaseInnReservation(actor.Id, gameClock.CurrentScheduleTick);
                behavior.ChangeLifecycleState(AdventurerLifecycleState.Preparing);
                eventPublisher.Publish(new ActorFullyRecovered(actor.Id));
            }
        }
    }
}
