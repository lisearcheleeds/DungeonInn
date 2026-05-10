using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Domain.Actor;

namespace DungeonInn.Master
{
    public sealed class ActorEffectMaster
    {
        public int Id { get; }
        public string Name { get; }
        public float DurationSeconds { get; }
        public ActorEffectReapplyPolicy ReapplyPolicy { get; }
        public IReadOnlyList<StatusEffectSpec> StatusEffectSpecs { get; }

        public ActorEffectMaster(
            int id,
            string name,
            float durationSeconds,
            ActorEffectReapplyPolicy reapplyPolicy,
            IReadOnlyList<StatusEffectSpec> statusEffectSpecs)
        {
            if (id < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Actor effect name is required.", nameof(name));
            }

            if (durationSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            }

            var specs = statusEffectSpecs ?? throw new ArgumentNullException(nameof(statusEffectSpecs));
            if (specs.Count == 0)
            {
                throw new ArgumentException("Actor effect requires at least one status effect.", nameof(statusEffectSpecs));
            }

            Id = id;
            Name = name;
            DurationSeconds = durationSeconds;
            ReapplyPolicy = reapplyPolicy;
            StatusEffectSpecs = specs.ToArray();
        }
    }
}
