using System;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Guild;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class ChargeInnFeeUseCase
    {
        readonly IEventPublisher eventBus;

        [Inject]
        public ChargeInnFeeUseCase(IEventPublisher eventBus)
        {
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public bool Execute(Actor actor, AdventurerGuild guild, IGameWorldState worldState)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            if (guild == null)
            {
                throw new ArgumentNullException(nameof(guild));
            }

            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            var fee = GameConstants.InnFeePerStay;

            if (!actor.Inventory.TrySpendGold(fee))
            {
                worldState.InnEconomy.RecordRejectedGuest(GameConstants.InnCannotPaySatisfactionDelta);
                eventBus.Publish(new InnSatisfactionChanged(
                    actor.Id,
                    GameConstants.InnCannotPaySatisfactionDelta,
                    InnSatisfactionChangeReason.CannotPayInnFee));
                return false;
            }

            guild.Inventory.AddGold(fee);
            worldState.InnEconomy.RecordStayedGuest(fee, GameConstants.InnStayedSatisfactionDelta);
            eventBus.Publish(new InnFeeCharged(actor.Id, fee, actor.Inventory.Gold, guild.Inventory.Gold));
            eventBus.Publish(new InnSatisfactionChanged(
                actor.Id,
                GameConstants.InnStayedSatisfactionDelta,
                InnSatisfactionChangeReason.StayedAtInn));
            return true;
        }
    }
}
