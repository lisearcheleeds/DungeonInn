using System;

namespace DungeonInn.Domain.Actor
{
    public sealed class FactionRelation
    {
        public ActorFaction From { get; }
        public ActorFaction To { get; }
        public FactionRelationType Type { get; }

        public FactionRelation(ActorFaction from, ActorFaction to, FactionRelationType type)
        {
            From = from ?? throw new ArgumentNullException(nameof(from));
            To = to ?? throw new ArgumentNullException(nameof(to));
            Type = type;
        }

        public bool IsHostile => Type == FactionRelationType.Hostile;
    }
}
