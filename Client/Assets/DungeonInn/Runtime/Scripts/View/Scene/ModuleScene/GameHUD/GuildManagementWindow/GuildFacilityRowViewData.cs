using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD.ScreenStack
{
    public sealed class GuildFacilityRowViewData
    {
        public GuildFacilityRowViewData(
            Guid facilityId,
            string description,
            IReadOnlyList<string> lineup,
            bool canUpgrade,
            string upgradeStatus)
        {
            FacilityId = facilityId;
            Description = string.IsNullOrWhiteSpace(description)
                ? throw new ArgumentException("Facility description is required.", nameof(description))
                : description;
            Lineup = (lineup ?? Array.Empty<string>()).ToArray();
            CanUpgrade = canUpgrade;
            UpgradeStatus = string.IsNullOrWhiteSpace(upgradeStatus) ? "Unavailable" : upgradeStatus;
        }

        public Guid FacilityId { get; }
        public string Description { get; }
        public IReadOnlyList<string> Lineup { get; }
        public bool CanUpgrade { get; }
        public string UpgradeStatus { get; }
    }
}
