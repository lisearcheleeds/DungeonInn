namespace DungeonInn.Domain.Combat
{
    public sealed class CombatEffectLink
    {
        public CombatEffectTriggerType TriggerType { get; }
        public int TargetNodeId { get; }

        public CombatEffectLink(CombatEffectTriggerType triggerType, int targetNodeId)
        {
            TriggerType = triggerType;
            TargetNodeId = targetNodeId;
        }
    }
}
