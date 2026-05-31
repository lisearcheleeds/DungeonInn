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
        readonly IWorldGameSettingsRepository worldGameSettingsRepository;

        [Inject]
        public ChargeInnFeeUseCase(
            IEventPublisher eventPublisher,
            IWorldGameSettingsRepository worldGameSettingsRepository)
        {
            this.eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            this.worldGameSettingsRepository =
                worldGameSettingsRepository ?? throw new ArgumentNullException(nameof(worldGameSettingsRepository));
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

            var innBalanceSettings = worldGameSettingsRepository.GetInnBalanceSettings();
            var fee = innBalanceSettings.FeePerStay;

            var chargedFee = Math.Min(fee, actor.Inventory.Gold);
            actor.TrySpendGold(chargedFee);
            facility.ReceiveUsageFee(chargedFee);
            eventPublisher.Publish(new InnFeeCharged(actor.Id, chargedFee, actor.Inventory.Gold, facility.Inventory.Gold));
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
