using System;

namespace DungeonInn.Application.Economy
{
    public interface IGuildManagementScreenService
    {
        GuildManagementStatusSummary GetStatus();

        FacilityUpgradeResult UpgradeFacility(Guid facilityId);
    }
}
