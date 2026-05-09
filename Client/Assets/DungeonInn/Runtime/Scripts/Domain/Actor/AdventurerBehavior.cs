using System;
using System.Collections.Generic;
using DungeonInn.Domain.Item;

namespace DungeonInn.Domain.Actor
{
    public sealed class AdventurerBehavior : IActorBehavior, IActorDropSource
    {
        public IReadOnlyList<ActorDropEntry> DropTable => Array.Empty<ActorDropEntry>();
        public AdventurerLifecycleState LifecycleState { get; private set; }
        public int Stress { get; private set; }
        public int ExplorationRoomArrivalCount { get; private set; }

        public AdventurerBehavior(int stress)
            : this(stress, AdventurerLifecycleState.Arrived)
        {
        }

        public AdventurerBehavior(int stress, AdventurerLifecycleState lifecycleState)
        {
            Stress = Math.Max(0, stress);
            LifecycleState = lifecycleState;
        }

        public void ChangeLifecycleState(AdventurerLifecycleState lifecycleState)
        {
            LifecycleState = lifecycleState;
        }

        public void RecordExplorationRoomArrival()
        {
            ExplorationRoomArrivalCount++;
        }

        public void ResetExplorationRoomArrivalCount()
        {
            ExplorationRoomArrivalCount = 0;
        }

        public void ReduceStress(int amount)
        {
            Stress = Math.Max(0, Stress - Math.Max(0, amount));
        }
    }
}
