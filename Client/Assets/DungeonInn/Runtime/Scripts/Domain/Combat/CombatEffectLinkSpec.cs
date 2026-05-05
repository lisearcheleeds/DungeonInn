namespace DungeonInn.Domain.Combat
{
    public sealed class CombatEffectLinkSpec
    {
        public CombatEffectTriggerType TriggerType { get; }
        public int TargetNodeId { get; }

        public CombatEffectLinkSpec(CombatEffectTriggerType triggerType, int targetNodeId)
        {
            TriggerType = triggerType;
            TargetNodeId = targetNodeId;
        }
    }
}
