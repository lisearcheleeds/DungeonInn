using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonInn.Application.Economy
{
    public sealed class FacilityUpgradePreviewSummary
    {
        public FacilityUpgradePreviewSummary(
            bool hasNextUpgrade,
            bool canUpgrade,
            int fromLevel,
            int toLevel,
            int nextQuality,
            int nextCapacity,
            IReadOnlyList<FacilityUpgradeCostSummary> costs,
            string unavailableReason)
        {
            HasNextUpgrade = hasNextUpgrade;
            CanUpgrade = canUpgrade;
            FromLevel = Math.Max(0, fromLevel);
            ToLevel = Math.Max(0, toLevel);
            NextQuality = Math.Max(0, nextQuality);
            NextCapacity = Math.Max(0, nextCapacity);
            Costs = (costs ?? Array.Empty<FacilityUpgradeCostSummary>()).ToArray();
            UnavailableReason = unavailableReason ?? string.Empty;
        }

        public bool HasNextUpgrade { get; }
        public bool CanUpgrade { get; }
        public int FromLevel { get; }
        public int ToLevel { get; }
        public int NextQuality { get; }
        public int NextCapacity { get; }
        public IReadOnlyList<FacilityUpgradeCostSummary> Costs { get; }
        public string UnavailableReason { get; }
    }
}
