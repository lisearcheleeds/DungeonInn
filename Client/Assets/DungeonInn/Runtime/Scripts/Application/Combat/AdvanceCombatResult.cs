using System.Collections.Generic;
using System.Linq;

namespace DungeonInn.Application.Combat
{
    public sealed class AdvanceCombatResult
    {
        public IReadOnlyList<CombatAttackEvent> Attacks { get; }
        public IReadOnlyList<CombatDeathEvent> Deaths { get; }

        public AdvanceCombatResult(
            IReadOnlyList<CombatAttackEvent> attacks,
            IReadOnlyList<CombatDeathEvent> deaths)
        {
            Attacks = (attacks ?? Enumerable.Empty<CombatAttackEvent>()).ToArray();
            Deaths = (deaths ?? Enumerable.Empty<CombatDeathEvent>()).ToArray();
        }
    }
}
