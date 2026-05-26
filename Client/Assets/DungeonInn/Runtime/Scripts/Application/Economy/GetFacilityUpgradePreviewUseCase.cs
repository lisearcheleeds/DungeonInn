using System;
using DungeonInn.Application.World;
using VContainer;

namespace DungeonInn.Application.Economy
{
    public sealed class GetFacilityUpgradePreviewUseCase
    {
        readonly IGameWorldStateReader worldState;
        readonly FacilityUpgradePreviewService previewService;

        [Inject]
        public GetFacilityUpgradePreviewUseCase(
            IGameWorldStateReader worldState,
            FacilityUpgradePreviewService previewService)
        {
            this.worldState = worldState ?? throw new ArgumentNullException(nameof(worldState));
            this.previewService = previewService ?? throw new ArgumentNullException(nameof(previewService));
        }

        public FacilityUpgradePreviewSummary Execute(Guid facilityId)
        {
            var facility = worldState.Guild.GetFacility(facilityId);
            return previewService.Create(facility.Type, facility.Level);
        }
    }
}
