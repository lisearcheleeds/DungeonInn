using System;

namespace DungeonInn.Application.Event.Events
{
    public sealed class ActorAiDecisionRecorded : IGameEvent
    {
        public Guid ActorId { get; }
        public AiDecisionType DecisionType { get; }
        public AiDecisionReasonType ReasonType { get; }
        public Guid TargetActorId { get; }
        public Guid FacilityId { get; }
        public int CurrentHp { get; }
        public int MaxHp { get; }
        public int SelectedFloor { get; }
        public int Score { get; }

        public ActorAiDecisionRecorded(
            Guid actorId,
            AiDecisionType decisionType,
            AiDecisionReasonType reasonType,
            Guid targetActorId,
            Guid facilityId,
            int currentHp,
            int maxHp,
            int selectedFloor,
            int score)
        {
            ActorId = actorId;
            DecisionType = decisionType;
            ReasonType = reasonType;
            TargetActorId = targetActorId;
            FacilityId = facilityId;
            CurrentHp = currentHp;
            MaxHp = maxHp;
            SelectedFloor = selectedFloor;
            Score = score;
        }
    }
}
