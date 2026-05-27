using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Domain.Facility;

namespace DungeonInn.Application.Economy
{
    public sealed class FacilityManagementSummary
    {
        public FacilityManagementSummary(
            Guid facilityId,
            FacilityType facilityType,
            string name,
            int level,
            int quality,
            int capacity,
            int gold,
            int sales,
            FacilityUpgradePreviewSummary upgradePreview,
            IReadOnlyList<FacilityLineupSummary> lineup)
        {
            FacilityId = facilityId;
            FacilityType = facilityType;
            Name = string.IsNullOrWhiteSpace(name)
                ? throw new ArgumentException("Facility name is required.", nameof(name))
                : name;
            Level = Math.Max(1, level);
            Quality = Math.Max(1, quality);
            Capacity = Math.Max(1, capacity);
            Gold = Math.Max(0, gold);
            Sales = Math.Max(0, sales);
            UpgradePreview = upgradePreview ?? throw new ArgumentNullException(nameof(upgradePreview));
            Lineup = (lineup ?? Array.Empty<FacilityLineupSummary>()).ToArray();
        }

        public Guid FacilityId { get; }
        public FacilityType FacilityType { get; }
        public string Name { get; }
        public int Level { get; }
        public int Quality { get; }
        public int Capacity { get; }
        public int Gold { get; }
        public int Sales { get; }
        public FacilityUpgradePreviewSummary UpgradePreview { get; }
        public IReadOnlyList<FacilityLineupSummary> Lineup { get; }
    }
}
