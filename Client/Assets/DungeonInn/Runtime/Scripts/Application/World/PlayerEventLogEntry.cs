using System;

namespace DungeonInn.Application.World
{
    public readonly struct PlayerEventLogEntry
    {
        public PlayerEventLogEntry(float timestamp, string text, Guid? relatedActorId)
        {
            Timestamp = timestamp;
            Text = text ?? string.Empty;
            RelatedActorId = relatedActorId;
        }

        public float Timestamp { get; }
        public string Text { get; }
        public Guid? RelatedActorId { get; }
    }
}
