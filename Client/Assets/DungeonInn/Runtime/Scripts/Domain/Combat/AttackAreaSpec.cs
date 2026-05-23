using System;

namespace DungeonInn.Domain.Combat
{
    public sealed class AttackAreaSpec
    {
        public AttackAreaShape Shape { get; }
        public AttackAreaDurationType DurationType { get; }
        public AttackHitIntervalType HitIntervalType { get; }
        public float WidthMeters { get; }
        public float LengthMeters { get; }
        public float RadiusMeters { get; }
        public float AngleDegrees { get; }
        public int DurationTicks { get; }
        public string PrefabAddress { get; }

        public AttackAreaSpec(
            AttackAreaShape shape,
            AttackAreaDurationType durationType,
            AttackHitIntervalType hitIntervalType,
            float widthMeters,
            float lengthMeters,
            float radiusMeters,
            float angleDegrees,
            int durationTicks,
            string prefabAddress = "")
        {
            Shape = shape;
            DurationType = durationType;
            HitIntervalType = hitIntervalType;
            WidthMeters = Math.Max(0, widthMeters);
            LengthMeters = Math.Max(0, lengthMeters);
            RadiusMeters = Math.Max(0, radiusMeters);
            AngleDegrees = Math.Max(0, angleDegrees);
            DurationTicks = Math.Max(0, durationTicks);
            PrefabAddress = prefabAddress ?? string.Empty;
        }
    }
}
