using System;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Facility;
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

            return Execute(actor, guild, FindFirstInn(guild));
        }

        public bool Execute(Actor actor, AdventurerGuild guild, Facility facility)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            if (guild == null)
            {
                throw new ArgumentNullException(nameof(guild));
            }

            if (facility == null)
            {
                throw new ArgumentNullException(nameof(facility));
            }

            if (facility.Type != FacilityType.Inn)
            {
                throw new InvalidOperationException("Facility is not inn.");
            }

            var fee = GameConstants.InnFeePerStay;

            if (!actor.TrySpendGold(fee))
            {
                eventPublisher.Publish(new InnSatisfactionChanged(
                    actor.Id,
                    GameConstants.InnCannotPaySatisfactionDelta,
                    InnSatisfactionChangeReason.CannotPayInnFee));
                return false;
            }

            facility.Inventory.AddGold(fee);
            eventPublisher.Publish(new InnFeeCharged(actor.Id, fee, actor.Inventory.Gold, facility.Inventory.Gold));
            eventPublisher.Publish(new InnSatisfactionChanged(
                actor.Id,
                GameConstants.InnStayedSatisfactionDelta,
                InnSatisfactionChangeReason.StayedAtInn));
            return true;
        }

        static Facility FindFirstInn(AdventurerGuild guild)
        {
            foreach (var facility in guild.Facilities)
            {
                if (facility.Type == FacilityType.Inn)
                {
                    return facility;
                }
            }

            throw new InvalidOperationException("Inn facility does not exist.");
        }
    }
}
