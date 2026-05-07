using System;

namespace DungeonInn.Application.Combat
{
    public sealed class AdventurerBattleRecord
    {
        public Guid ActorId { get; }
        public int TotalCombats { get; private set; }
        public int TotalDamageDealt { get; private set; }
        public int TotalDamageTaken { get; private set; }
        public int TotalKills { get; private set; }

        public AdventurerBattleRecord(Guid actorId)
        {
            ActorId = actorId;
        }

        public void RecordCombatStarted() => TotalCombats++;
        public void RecordDamageDealt(int damage) => TotalDamageDealt += Math.Max(0, damage);
        public void RecordDamageTaken(int damage) => TotalDamageTaken += Math.Max(0, damage);
        public void RecordKill() => TotalKills++;
    }
}
