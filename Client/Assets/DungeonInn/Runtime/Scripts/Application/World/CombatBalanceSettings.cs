using System;

namespace DungeonInn.Application.World
{
    public sealed class CombatBalanceSettings
    {
        public float ProjectileHitRadiusMeters { get; }
        public float EncounterRangeMeters { get; }
        public float SpatialIndexCellSizeMeters { get; }

        public CombatBalanceSettings(
            float projectileHitRadiusMeters,
            float encounterRangeMeters,
            float spatialIndexCellSizeMeters)
        {
            ProjectileHitRadiusMeters = Math.Max(0f, projectileHitRadiusMeters);
            EncounterRangeMeters = Math.Max(0f, encounterRangeMeters);
            SpatialIndexCellSizeMeters = Math.Max(0.01f, spatialIndexCellSizeMeters);
        }

        public static CombatBalanceSettings CreateDefault()
        {
            return new CombatBalanceSettings(0.5f, 20f, 20f);
        }
    }
}
