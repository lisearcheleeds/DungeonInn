using System;
using DungeonInn.Domain.Actor;

namespace DungeonInn.Master
{
    public sealed class StatusEffectSpec
    {
        public StatusEffectType Type { get; }
        public int Amount { get; }
        public float DurationSeconds { get; }
        public float TickIntervalSeconds { get; }
        public StatusEffectAggregationPolicy AggregationPolicy { get; }

        public StatusEffectSpec(
            StatusEffectType type,
            int amount,
            float durationSeconds,
            float tickIntervalSeconds,
            StatusEffectAggregationPolicy aggregationPolicy)
        {
            if (amount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            if (durationSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            }

            if (tickIntervalSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(tickIntervalSeconds));
            }

            Type = type;
            Amount = amount;
            DurationSeconds = durationSeconds;
            TickIntervalSeconds = tickIntervalSeconds;
            AggregationPolicy = aggregationPolicy;
        }
    }
}
