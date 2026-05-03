using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Domain.Character;
using DungeonInn.Domain.World;

namespace DungeonInn.Application.AI
{
    public class DefaultAdventurerAI : IAdventurerAI
    {
        private readonly IReadOnlyList<IAdventurerAIState> states;
        private IAdventurerAIState current;

        public DefaultAdventurerAI(IReadOnlyList<IAdventurerAIState> states)
        {
            this.states = states ?? throw new ArgumentNullException(nameof(states));
        }

        public void OnStateEnter(AdventurerCharacter character, AdventurerState state)
        {
            current = states.FirstOrDefault(s => s.StateType == state);
            current?.Enter(character);
        }

        public GridPosition? Tick(AdventurerCharacter character, float deltaTime)
        {
            return current?.Tick(character, deltaTime);
        }

        public void OnStateExit(AdventurerCharacter character, AdventurerState state)
        {
            current?.Exit(character);
        }
    }
}
