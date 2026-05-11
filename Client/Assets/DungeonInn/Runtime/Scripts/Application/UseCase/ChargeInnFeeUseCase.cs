using System;
using DungeonInn.Application.Event;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Guild;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class ChargeInnFeeUseCase
    {
        readonly ChargeInnFeeService chargeInnFeeService;

        [Inject]
        public ChargeInnFeeUseCase(ChargeInnFeeService chargeInnFeeService)
        {
            this.chargeInnFeeService = chargeInnFeeService ?? throw new ArgumentNullException(nameof(chargeInnFeeService));
        }

        public ChargeInnFeeUseCase(IEventPublisher eventPublisher)
            : this(new ChargeInnFeeService(eventPublisher))
        {
        }

        public bool Execute(Actor actor, AdventurerGuild guild, IGameWorldState worldState)
        {
            return chargeInnFeeService.Execute(actor, guild, worldState);
        }
    }
}
