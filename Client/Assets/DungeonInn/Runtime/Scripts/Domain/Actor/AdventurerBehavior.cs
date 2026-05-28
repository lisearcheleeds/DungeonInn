using System;

namespace DungeonInn.Domain.Actor
{
    public sealed class AdventurerBehavior : IActorBehavior
    {
        public AdventurerLifecycleState LifecycleState { get; private set; }
        public int Stress { get; private set; }
        public int ExplorationRoomArrivalCount { get; private set; }
        public int TargetFloorDepth { get; private set; }
        public int WaitingForInnStartedDay { get; private set; } = -1;

        public AdventurerBehavior(int stress)
            : this(stress, AdventurerLifecycleState.Arrived)
        {
        }

        public AdventurerBehavior(int stress, AdventurerLifecycleState lifecycleState)
        {
            Stress = Math.Max(0, stress);
            LifecycleState = lifecycleState;
            TargetFloorDepth = 1;
        }

        public void ChangeLifecycleState(AdventurerLifecycleState lifecycleState)
        {
            LifecycleState = lifecycleState;
        }

        public void StartWaitingForInn(int currentDay)
        {
            if (WaitingForInnStartedDay < 0)
            {
                WaitingForInnStartedDay = Math.Max(0, currentDay);
            }

            LifecycleState = AdventurerLifecycleState.WaitingForInn;
        }

        public void ClearWaitingForInn()
        {
            WaitingForInnStartedDay = -1;
        }

        public void RecordExplorationRoomArrival()
        {
            ExplorationRoomArrivalCount++;
        }

        public void ResetExplorationRoomArrivalCount()
        {
            ExplorationRoomArrivalCount = 0;
        }

        public void SetTargetFloorDepth(int targetFloorDepth)
        {
            TargetFloorDepth = Math.Max(1, targetFloorDepth);
        }

        public void ReduceStress(int amount)
        {
            Stress = Math.Max(0, Stress - Math.Max(0, amount));
        }

        public void OnRecovered(
            int hpAmount,
            int mpAmount,
            int fatigueReduction,
            int stressReduction,
            int injuryReduction)
        {
            ReduceStress(stressReduction);
        }
    }
}
