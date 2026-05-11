using System;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Guild;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class ChargeInnFeeService
    {
        readonly IEventPublisher eventPublisher;

        [Inject]
        public ChargeInnFeeService(IEventPublisher eventPublisher)
        {
            this.eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        }

        public bool Execute(Actor actor, AdventurerGuild guild)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            if (guild == null)
            {
                throw new ArgumentNullException(nameof(guild));
            }

            var fee = GameConstants.InnFeePerStay;

            if (!actor.Inventory.TrySpendGold(fee))
            {
                eventPublisher.Publish(new InnSatisfactionChanged(
                    actor.Id,
                    GameConstants.InnCannotPaySatisfactionDelta,
                    InnSatisfactionChangeReason.CannotPayInnFee));
                return false;
            }

            guild.Inventory.AddGold(fee);
            eventPublisher.Publish(new InnFeeCharged(actor.Id, fee, actor.Inventory.Gold, guild.Inventory.Gold));
            eventPublisher.Publish(new InnSatisfactionChanged(
                actor.Id,
                GameConstants.InnStayedSatisfactionDelta,
                InnSatisfactionChangeReason.StayedAtInn));
            return true;
        }
    }
}
