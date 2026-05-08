using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Map;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class RecoverAdventurerAtInnUseCase
    {

        readonly IGameEventBus eventBus;
        readonly Dictionary<Guid, float> accumulatedHp = new();

        [Inject]
        public RecoverAdventurerAtInnUseCase(IGameEventBus eventBus)
        {
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public UniTask ExecuteAsync(IGameWorldState worldState, float deltaGameSeconds, int currentTick)
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
                TickRecovery(guild, actor, behavior, deltaGameSeconds);
            }

            return UniTask.CompletedTask;
        }

        void EnsureInnReservation(DungeonInn.Domain.Guild.AdventurerGuild guild, Actor actor, int currentTick)
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

                guild.ReserveInn(Guid.NewGuid(), actor, facility.Id, currentTick);
                return;
            }
        }

        void TickRecovery(DungeonInn.Domain.Guild.AdventurerGuild guild, Actor actor, AdventurerBehavior behavior, float deltaGameSeconds)
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
                guild.ReleaseInnReservation(actor.Id, 0);
                behavior.ChangeLifecycleState(AdventurerLifecycleState.Preparing);
                eventBus.Publish(new ActorFullyRecovered(actor.Id));
            }
        }
    }
}
