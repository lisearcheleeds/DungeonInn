using System.Collections.Generic;
using DungeonInn.Domain.Character;
using DungeonInn.Domain.World;

namespace DungeonInn.Application.AI
{
    internal class ExploringState : IAdventurerAIState
    {
        private readonly IReadOnlyList<GridPosition> waypoints;
        private readonly float exploreDuration;
        private readonly float waypointSwitchInterval;
        private float elapsed;
        private float waypointElapsed;
        private int waypointIndex;

        public ExploringState(IReadOnlyList<GridPosition> waypoints, float exploreDuration, float waypointSwitchInterval)
        {
            this.waypoints = waypoints;
            this.exploreDuration = exploreDuration;
            this.waypointSwitchInterval = waypointSwitchInterval;
        }

        public AdventurerState StateType => AdventurerState.ExploringDungeon;

        public void Enter(AdventurerCharacter character)
        {
            elapsed = 0f;
            waypointElapsed = 0f;
            waypointIndex = 0;
        }

        public GridPosition? Tick(AdventurerCharacter character, float deltaTime)
        {
            elapsed += deltaTime;

            if (elapsed >= exploreDuration)
            {
                character.TransitionState(AdventurerState.Returning);
                return null;
            }

            if (waypoints.Count == 0)
            {
                return null;
            }

            waypointElapsed += deltaTime;

            if (waypointElapsed >= waypointSwitchInterval)
            {
                waypointIndex = (waypointIndex + 1) % waypoints.Count;
                waypointElapsed = 0f;
            }

            return waypoints[waypointIndex];
        }

        public void Exit(AdventurerCharacter character)
        {
        }
    }
}
