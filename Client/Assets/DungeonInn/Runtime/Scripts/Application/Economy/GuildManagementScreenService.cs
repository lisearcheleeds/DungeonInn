using System;
using VContainer;

namespace DungeonInn.Application.Economy
{
    public sealed class GuildManagementScreenService : IGuildManagementScreenService
    {
        readonly GetGuildManagementStatusUseCase getGuildManagementStatusUseCase;
        readonly UpgradeFacilityUseCase upgradeFacilityUseCase;

        [Inject]
        public GuildManagementScreenService(
            GetGuildManagementStatusUseCase getGuildManagementStatusUseCase,
            UpgradeFacilityUseCase upgradeFacilityUseCase)
        {
            this.getGuildManagementStatusUseCase = getGuildManagementStatusUseCase
                ?? throw new ArgumentNullException(nameof(getGuildManagementStatusUseCase));
            this.upgradeFacilityUseCase =
                upgradeFacilityUseCase ?? throw new ArgumentNullException(nameof(upgradeFacilityUseCase));
        }

        public GuildManagementStatusSummary GetStatus()
        {
            return getGuildManagementStatusUseCase.Execute();
        }

        public FacilityUpgradeResult UpgradeFacility(Guid facilityId)
        {
            return upgradeFacilityUseCase.Execute(facilityId);
        }
    }
}
