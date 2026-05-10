using System;
using DungeonInn.Master;

namespace DungeonInn.Domain.Actor
{
    public sealed class ActiveStatusEffect
    {
        public StatusEffectType Type { get; }
        public StatusEffectAggregationPolicy AggregationPolicy { get; }
        public int Amount { get; private set; }
        public float DurationSeconds { get; private set; }
        public float TickIntervalSeconds { get; }
        public float ElapsedSeconds { get; private set; }
        public int AppliedAmount { get; private set; }
        public bool IsExpired => DurationSeconds <= ElapsedSeconds;

        public ActiveStatusEffect(StatusEffectSpec spec)
        {
            if (spec == null)
            {
                throw new ArgumentNullException(nameof(spec));
            }

            Type = spec.Type;
            Amount = spec.Amount;
            DurationSeconds = spec.DurationSeconds;
            TickIntervalSeconds = spec.TickIntervalSeconds;
            AggregationPolicy = spec.AggregationPolicy;
        }

        public int Advance(float deltaSeconds)
        {
            if (deltaSeconds <= 0f || IsExpired)
            {
                return 0;
            }

            ElapsedSeconds = Math.Min(DurationSeconds, ElapsedSeconds + deltaSeconds);
            var tickCount = (int)Math.Floor(ElapsedSeconds / TickIntervalSeconds);
            if (IsExpired)
            {
                tickCount = GetTotalTickCount();
            }

            var nextAppliedAmount = (int)Math.Floor(Amount * (tickCount / (float)GetTotalTickCount()));
            var deltaAmount = Math.Max(0, nextAppliedAmount - AppliedAmount);
            AppliedAmount = nextAppliedAmount;
            return deltaAmount;
        }

        public void Append(StatusEffectSpec spec)
        {
            if (spec == null)
            {
                throw new ArgumentNullException(nameof(spec));
            }

            if (spec.Type != Type || spec.AggregationPolicy != AggregationPolicy || Math.Abs(spec.TickIntervalSeconds - TickIntervalSeconds) > float.Epsilon)
            {
                throw new InvalidOperationException("Status effect spec does not match.");
            }

            Amount += spec.Amount;
            DurationSeconds += spec.DurationSeconds;
        }

        public void Refresh(StatusEffectSpec spec)
        {
            if (spec == null)
            {
                throw new ArgumentNullException(nameof(spec));
            }

            if (spec.Type != Type || spec.AggregationPolicy != AggregationPolicy || Math.Abs(spec.TickIntervalSeconds - TickIntervalSeconds) > float.Epsilon)
            {
                throw new InvalidOperationException("Status effect spec does not match.");
            }

            Amount = spec.Amount;
            DurationSeconds = spec.DurationSeconds;
            ElapsedSeconds = 0f;
            AppliedAmount = 0;
        }

        int GetTotalTickCount()
        {
            return Math.Max(1, (int)Math.Ceiling(DurationSeconds / TickIntervalSeconds));
        }
    }
}
