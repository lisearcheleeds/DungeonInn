using System;

namespace DungeonInn.Domain.Actor
{
    public sealed class ActorParams
    {
        public int MaxHp { get; }
        public int MaxMp { get; }
        public int MoveSpeed { get; }
        public int InjuryResistance { get; }
        public int ExplorationPower { get; }
        public int EquipmentAptitude { get; }

        public ActorParams(
            int maxHp,
            int maxMp,
            int moveSpeed,
            int injuryResistance,
            int explorationPower,
            int equipmentAptitude)
        {
            MaxHp = Math.Max(1, maxHp);
            MaxMp = Math.Max(0, maxMp);
            MoveSpeed = Math.Max(0, moveSpeed);
            InjuryResistance = Math.Max(0, injuryResistance);
            ExplorationPower = Math.Max(0, explorationPower);
            EquipmentAptitude = Math.Max(0, equipmentAptitude);
        }
    }
}
