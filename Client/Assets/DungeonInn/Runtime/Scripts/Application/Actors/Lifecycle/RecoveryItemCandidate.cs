namespace DungeonInn.Application.Actors.Lifecycle
{
    public readonly struct RecoveryItemCandidate
    {
        public int ItemId { get; }
        public int ActorEffectMasterId { get; }
        public int HealAmount { get; }

        public RecoveryItemCandidate(int itemId, int actorEffectMasterId, int healAmount)
        {
            ItemId = itemId;
            ActorEffectMasterId = actorEffectMasterId;
            HealAmount = healAmount;
        }
    }
}
