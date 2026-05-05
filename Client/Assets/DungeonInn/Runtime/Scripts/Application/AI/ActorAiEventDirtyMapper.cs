namespace DungeonInn.Application.AI
{
    public sealed class ActorAiEventDirtyMapper
    {
        readonly ActorAiDirtyRules dirtyRules = new();

        public ActorAiDirtyFlags Map(ActorAiEventType eventType)
        {
            return dirtyRules.GetDirtyFlags(eventType);
        }
    }
}
