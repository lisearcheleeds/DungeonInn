namespace DungeonInn.Application.GameLoop
{
    public sealed class SpawnScheduleState
    {
        public int LastAdventurerSpawnTick { get; set; } = -1;
        public int LastMonsterSpawnTick { get; set; } = -1;
    }
}
