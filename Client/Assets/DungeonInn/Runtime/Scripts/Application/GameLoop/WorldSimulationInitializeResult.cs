namespace DungeonInn.Application.GameLoop
{
    public readonly struct WorldSimulationInitializeResult
    {
        public WorldSimulationInitializeResult(int facilityCount, int dungeonFloorCount, int actorCount)
        {
            FacilityCount = facilityCount;
            DungeonFloorCount = dungeonFloorCount;
            ActorCount = actorCount;
        }

        public int FacilityCount { get; }
        public int DungeonFloorCount { get; }
        public int ActorCount { get; }
    }
}
