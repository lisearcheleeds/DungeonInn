using System;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Guild;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class ChargeInnFeeUseCase
    {
        readonly IGameEventBus eventBus;

        [Inject]
        public ChargeInnFeeUseCase(IGameEventBus eventBus)
        {
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public bool Execute(Actor actor, AdventurerGuild guild)
        {
            var fee = GameConstants.InnFeePerStay;

            if (!actor.Inventory.TrySpendGold(fee))
            {
                return false;
            }

            guild.Inventory.AddGold(fee);
            eventBus.Publish(new InnFeeCharged(actor.Id, fee, actor.Inventory.Gold, guild.Inventory.Gold));
            return true;
        }
    }
}
