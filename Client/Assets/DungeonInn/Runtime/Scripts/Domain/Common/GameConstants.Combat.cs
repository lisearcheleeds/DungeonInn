namespace DungeonInn.Domain.Common
{
    public static partial class GameConstants
    {
        public const int KillExperienceRewardNumerator = 1;
        public const int KillExperienceRewardDenominator = 10;
        public const int KillExperienceRewardMinimum = 1;
        public const float ProjectileHitRadiusMeters = 0.5f;
        public const float ProjectileDefaultSpeedMetersPerSecond = 12f;
        public const float AreaEffectDefaultRadiusMeters = 3f;
        public const int AreaEffectDefaultDurationTicks = 3;
        public const float CombatEncounterRangeMeters = 20f;
        public const float ActorSpatialIndexCellSizeMeters = 20f;
    }
}
