using System;

namespace DungeonInn.Domain.EntityIdentity
{
    public sealed class EntityIdentity
    {
        public Guid Id { get; }
        public EntityKind Kind { get; }
        public string DisplayName { get; private set; }
        public bool IsActive { get; private set; }
        public int? RemovedAtTick { get; private set; }

        public EntityIdentity(Guid id, EntityKind kind, string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Display name is required.", nameof(displayName));
            }

            Id = id;
            Kind = kind;
            DisplayName = displayName;
            IsActive = true;
        }

        public void Rename(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Display name is required.", nameof(displayName));
            }

            DisplayName = displayName;
        }

        public void MarkRemoved(int occurredAtTick)
        {
            IsActive = false;
            RemovedAtTick = occurredAtTick;
        }
    }
}
