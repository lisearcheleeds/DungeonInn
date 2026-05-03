namespace DungeonInn.Domain.Character
{
    public class AdventurerConfigData
    {
        public int BaseMaxHp { get; init; }
        public int BaseAttackPower { get; init; }
        public int BaseDefense { get; init; }
        public float BaseMoveSpeed { get; init; }
        public float BaseAttackSpeed { get; init; }
        public float SatisfactionThreshold { get; init; }
        public float RestDuration { get; init; }
        public float ExploreDuration { get; init; }
    }
}
