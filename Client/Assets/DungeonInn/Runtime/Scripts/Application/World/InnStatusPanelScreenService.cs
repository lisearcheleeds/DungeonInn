using System;
using System.Collections.Generic;
using DungeonInn.Application.Economy;
using VContainer;

namespace DungeonInn.Application.World
{
    public sealed class InnStatusPanelScreenService : IInnStatusPanelScreenService
    {
        readonly GetInnGuestListUseCase getInnGuestListUseCase;
        readonly GetInnEconomyStatusUseCase getInnEconomyStatusUseCase;

        [Inject]
        public InnStatusPanelScreenService(
            GetInnGuestListUseCase getInnGuestListUseCase,
            GetInnEconomyStatusUseCase getInnEconomyStatusUseCase)
        {
            this.getInnGuestListUseCase =
                getInnGuestListUseCase ?? throw new ArgumentNullException(nameof(getInnGuestListUseCase));
            this.getInnEconomyStatusUseCase =
                getInnEconomyStatusUseCase ?? throw new ArgumentNullException(nameof(getInnEconomyStatusUseCase));
        }

        public bool CanGetGuestList => getInnGuestListUseCase.CanExecute;
        public bool CanGetEconomyStatus => getInnEconomyStatusUseCase.CanExecute;

        public void FillGuests(List<InnGuestSummary> buffer)
        {
            getInnGuestListUseCase.Execute(buffer);
        }

        public InnEconomyStatus GetEconomyStatus()
        {
            return getInnEconomyStatusUseCase.Execute();
        }
    }
}
