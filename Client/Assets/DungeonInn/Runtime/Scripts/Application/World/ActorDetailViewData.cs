using System;
using System.Collections.Generic;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.World
{
    public readonly struct ActorDetailViewData
    {
        public ActorDetailViewData(
            Guid actorId,
            LayerPosition position,
            string displayName,
            int level,
            int strength,
            int dexterity,
            int constitution,
            int intelligence,
            int wisdom,
            int charisma,
            int currentHp,
            int maxHp,
            int currentMp,
            int maxMp,
            int fatigue,
            int injurySeverity,
            int gold,
            IReadOnlyList<string> equipmentNames,
            IReadOnlyList<ActorEffectIconViewData> activeEffects)
        {
            ActorId = actorId;
            Position = position;
            DisplayName = displayName ?? string.Empty;
            Level = level;
            Strength = strength;
            Dexterity = dexterity;
            Constitution = constitution;
            Intelligence = intelligence;
            Wisdom = wisdom;
            Charisma = charisma;
            CurrentHp = currentHp;
            MaxHp = maxHp;
            CurrentMp = currentMp;
            MaxMp = maxMp;
            Fatigue = fatigue;
            InjurySeverity = injurySeverity;
            Gold = gold;
            EquipmentNames = CopySnapshot(equipmentNames);
            ActiveEffects = CopySnapshot(activeEffects);
        }

        public Guid ActorId { get; }
        public LayerPosition Position { get; }
        public string DisplayName { get; }
        public int Level { get; }
        public int Strength { get; }
        public int Dexterity { get; }
        public int Constitution { get; }
        public int Intelligence { get; }
        public int Wisdom { get; }
        public int Charisma { get; }
        public int CurrentHp { get; }
        public int MaxHp { get; }
        public int CurrentMp { get; }
        public int MaxMp { get; }
        public int Fatigue { get; }
        public int InjurySeverity { get; }
        public int Gold { get; }
        public IReadOnlyList<string> EquipmentNames { get; }
        public IReadOnlyList<ActorEffectIconViewData> ActiveEffects { get; }

        static T[] CopySnapshot<T>(IReadOnlyList<T> values)
        {
            if (values == null || values.Count == 0)
            {
                return Array.Empty<T>();
            }

            var snapshot = new T[values.Count];
            for (var index = 0; index < values.Count; index++)
            {
                snapshot[index] = values[index];
            }

            return snapshot;
        }
    }
}
