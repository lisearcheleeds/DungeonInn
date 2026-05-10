using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Master;

namespace DungeonInn.Domain.Actor
{
    public sealed class ActorEffectInstance
    {
        readonly List<ActiveStatusEffect> statusEffects = new();

        public Guid InstanceId { get; }
        public int ActorEffectMasterId { get; }
        public float DurationSeconds { get; private set; }
        public float ElapsedSeconds { get; private set; }
        public IReadOnlyList<ActiveStatusEffect> StatusEffects => statusEffects;
        public bool IsExpired => DurationSeconds <= ElapsedSeconds && statusEffects.All(statusEffect => statusEffect.IsExpired);

        public ActorEffectInstance(ActorEffectMaster master)
        {
            if (master == null)
            {
                throw new ArgumentNullException(nameof(master));
            }

            InstanceId = Guid.NewGuid();
            ActorEffectMasterId = master.Id;
            DurationSeconds = master.DurationSeconds;

            foreach (var spec in master.StatusEffectSpecs)
            {
                statusEffects.Add(new ActiveStatusEffect(spec));
            }
        }

        public void Advance(float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
            {
                return;
            }

            ElapsedSeconds = Math.Min(DurationSeconds, ElapsedSeconds + deltaSeconds);
        }

        public void Append(ActorEffectMaster master)
        {
            RequireSameMaster(master);
            DurationSeconds += master.DurationSeconds;
            AppendStatusEffects(master);
        }

        public void Refresh(ActorEffectMaster master)
        {
            RequireSameMaster(master);
            DurationSeconds = master.DurationSeconds;
            ElapsedSeconds = 0f;
            RefreshStatusEffects(master);
        }

        void AppendStatusEffects(ActorEffectMaster master)
        {
            foreach (var spec in master.StatusEffectSpecs)
            {
                FindStatusEffect(spec).Append(spec);
            }
        }

        void RefreshStatusEffects(ActorEffectMaster master)
        {
            foreach (var spec in master.StatusEffectSpecs)
            {
                FindStatusEffect(spec).Refresh(spec);
            }
        }

        ActiveStatusEffect FindStatusEffect(StatusEffectSpec spec)
        {
            foreach (var statusEffect in statusEffects)
            {
                if (statusEffect.Type == spec.Type && statusEffect.AggregationPolicy == spec.AggregationPolicy)
                {
                    return statusEffect;
                }
            }

            throw new InvalidOperationException("Status effect does not exist.");
        }

        void RequireSameMaster(ActorEffectMaster master)
        {
            if (master == null)
            {
                throw new ArgumentNullException(nameof(master));
            }

            if (master.Id != ActorEffectMasterId)
            {
                throw new InvalidOperationException("Actor effect master does not match.");
            }
        }
    }
}
