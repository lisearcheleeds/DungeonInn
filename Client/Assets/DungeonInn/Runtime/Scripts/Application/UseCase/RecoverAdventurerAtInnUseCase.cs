using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
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
    public sealed class RecoverAdventurerAtInnUseCase : IDisposable
    {
        readonly IGameEventBus eventBus;
        readonly IGameClock gameClock;
        readonly ChargeInnFeeUseCase chargeInnFeeUseCase;
        readonly Dictionary<Guid, float> accumulatedHp = new();
        readonly IDisposable deathSubscription;

        [Inject]
        public RecoverAdventurerAtInnUseCase(
            IGameEventBus eventBus,
            IGameClock gameClock,
            ChargeInnFeeUseCase chargeInnFeeUseCase)
        {
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            this.gameClock = gameClock ?? throw new ArgumentNullException(nameof(gameClock));
            this.chargeInnFeeUseCase = chargeInnFeeUseCase ?? throw new ArgumentNullException(nameof(chargeInnFeeUseCase));
            deathSubscription = eventBus.OnEvent<ActorDefeated>()
                .Subscribe(e => { accumulatedHp.Remove(e.ActorId); });
        }

        public void Dispose()
        {
            deathSubscription.Dispose();
        }

        public UniTask EnsureReservationsAsync(IGameWorldState worldState, int currentTick)
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

                EnsureInnReservation(guild, actor, currentTick);
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

        void EnsureInnReservation(AdventurerGuild guild, Actor actor, int currentTick)
        {
            if (guild.HasActiveInnReservation(actor.Id))
            {
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
                    continue;
                }

                if (!chargeInnFeeUseCase.Execute(actor, guild))
                {
                    return;
                }

                guild.ReserveInn(Guid.NewGuid(), actor, facility.Id, currentTick);
                return;
            }
        }

        void TickRecovery(AdventurerGuild guild, Actor actor, AdventurerBehavior behavior, float deltaGameSeconds)
        {
            if (!guild.HasActiveInnReservation(actor.Id))
            {
                return;
            }

            if (!accumulatedHp.TryGetValue(actor.Id, out var accumulated))
            {
                accumulated = 0f;
            }

            accumulated += actor.Params.MaxHp * GameConstants.InnHpRecoveryPercentPerMinute / 60f * deltaGameSeconds;
            var healAmount = (int)accumulated;

            if (healAmount > 0)
            {
                actor.Recover(healAmount, 0, 0, 0, 0);
                accumulated -= healAmount;
                eventBus.Publish(new ActorRecoveringAtInn(actor.Id, actor.Hp, actor.Params.MaxHp));
            }

            accumulatedHp[actor.Id] = accumulated;

            if (actor.Hp >= actor.Params.MaxHp)
            {
                accumulatedHp.Remove(actor.Id);
                guild.ReleaseInnReservation(actor.Id, gameClock.CurrentScheduleTick);
                behavior.ChangeLifecycleState(AdventurerLifecycleState.Preparing);
                eventBus.Publish(new ActorFullyRecovered(actor.Id));
            }
        }
    }
}
