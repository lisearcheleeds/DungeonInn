using System.Collections.Generic;
using DungeonInn.Application.Economy;

namespace DungeonInn.Application.World
{
    public interface IInnStatusPanelScreenService
    {
        bool CanGetGuestList { get; }
        bool CanGetEconomyStatus { get; }

        void FillGuests(List<InnGuestSummary> buffer);

        InnEconomyStatus GetEconomyStatus();
    }
}
