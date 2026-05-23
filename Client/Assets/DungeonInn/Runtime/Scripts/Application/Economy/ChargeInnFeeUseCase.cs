using DungeonInn.Application.Economy;
using DungeonInn.Application.Combat;
using System;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.World;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Guild;
using VContainer;

namespace DungeonInn.Application.Economy
{
    public sealed class ChargeInnFeeUseCase
    {
        readonly IEventPublisher eventPublisher;
        readonly InnBalanceSettings innBalanceSettings;

        [Inject]
        public ChargeInnFeeUseCase(
            IEventPublisher eventPublisher,
            InnBalanceSettings innBalanceSettings)
        {
            this.eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            this.innBalanceSettings = innBalanceSettings ?? throw new ArgumentNullException(nameof(innBalanceSettings));
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

            var fee = innBalanceSettings.FeePerStay;

            if (!actor.TrySpendGold(fee))
            {
                eventPublisher.Publish(new InnSatisfactionChanged(
                    actor.Id,
                    innBalanceSettings.CannotPaySatisfactionDelta,
                    InnSatisfactionChangeReason.CannotPayInnFee));
                return false;
            }

            facility.ReceiveUsageFee(fee);
            eventPublisher.Publish(new InnFeeCharged(actor.Id, fee, actor.Inventory.Gold, facility.Inventory.Gold));
            eventPublisher.Publish(new InnSatisfactionChanged(
                actor.Id,
                innBalanceSettings.StayedSatisfactionDelta,
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
