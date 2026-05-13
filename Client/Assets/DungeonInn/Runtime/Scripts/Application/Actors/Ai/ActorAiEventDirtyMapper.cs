namespace DungeonInn.Application.Actors.Ai
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
