using System;
using System.Collections.Generic;

namespace DungeonInn.Application.World
{
    public sealed class SelectedActorInspectorViewData
    {
        public SelectedActorInspectorViewData(
            Guid actorId,
            string displayName,
            string actorKindText,
            string roleText,
            string lifecycleText,
            string goalText,
            int level,
            int currentHp,
            int maxHp,
            int currentMp,
            int maxMp,
            int fatigue,
            int injurySeverity,
            int gold,
            IReadOnlyList<SelectedActorInspectorStatLineViewData> stats,
            IReadOnlyList<SelectedActorInspectorEquipmentLineViewData> equipment,
            IReadOnlyList<SelectedActorInspectorItemViewData> inventoryItems,
            IReadOnlyList<SelectedActorInspectorEffectLineViewData> activeEffects)
        {
            ActorId = actorId;
            DisplayName = displayName ?? string.Empty;
            ActorKindText = actorKindText ?? "-";
            RoleText = roleText ?? "-";
            LifecycleText = lifecycleText ?? "-";
            GoalText = goalText ?? "-";
            Level = level;
            CurrentHp = currentHp;
            MaxHp = maxHp;
            CurrentMp = currentMp;
            MaxMp = maxMp;
            Fatigue = fatigue;
            InjurySeverity = injurySeverity;
            Gold = gold;
            Stats = CopySnapshot(stats);
            Equipment = CopySnapshot(equipment);
            InventoryItems = CopySnapshot(inventoryItems);
            ActiveEffects = CopySnapshot(activeEffects);
        }

        public Guid ActorId { get; }
        public string DisplayName { get; }
        public string ActorKindText { get; }
        public string RoleText { get; }
        public string LifecycleText { get; }
        public string GoalText { get; }
        public int Level { get; }
        public int CurrentHp { get; }
        public int MaxHp { get; }
        public int CurrentMp { get; }
        public int MaxMp { get; }
        public int Fatigue { get; }
        public int InjurySeverity { get; }
        public int Gold { get; }
        public IReadOnlyList<SelectedActorInspectorStatLineViewData> Stats { get; }
        public IReadOnlyList<SelectedActorInspectorEquipmentLineViewData> Equipment { get; }
        public IReadOnlyList<SelectedActorInspectorItemViewData> InventoryItems { get; }
        public IReadOnlyList<SelectedActorInspectorEffectLineViewData> ActiveEffects { get; }

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

    public readonly struct SelectedActorInspectorStatLineViewData
    {
        public SelectedActorInspectorStatLineViewData(string label, int value)
        {
            Label = label ?? string.Empty;
            Value = value;
        }

        public string Label { get; }
        public int Value { get; }
    }

    public readonly struct SelectedActorInspectorEquipmentLineViewData
    {
        public SelectedActorInspectorEquipmentLineViewData(string slotText, string itemName)
        {
            SlotText = slotText ?? string.Empty;
            ItemName = itemName ?? "-";
        }

        public string SlotText { get; }
        public string ItemName { get; }
    }

    public readonly struct SelectedActorInspectorItemViewData
    {
        public SelectedActorInspectorItemViewData(int itemId, string itemName, int count)
        {
            ItemId = Math.Max(0, itemId);
            ItemName = itemName ?? "-";
            Count = Math.Max(0, count);
        }

        public int ItemId { get; }
        public string ItemName { get; }
        public int Count { get; }
    }

    public readonly struct SelectedActorInspectorEffectLineViewData
    {
        public SelectedActorInspectorEffectLineViewData(
            int actorEffectMasterId,
            string displayName,
            float remainingSeconds)
        {
            ActorEffectMasterId = Math.Max(0, actorEffectMasterId);
            DisplayName = displayName ?? "-";
            RemainingSeconds = Math.Max(0f, remainingSeconds);
        }

        public int ActorEffectMasterId { get; }
        public string DisplayName { get; }
        public float RemainingSeconds { get; }
    }
}
