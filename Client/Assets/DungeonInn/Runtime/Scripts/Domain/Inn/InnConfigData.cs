namespace DungeonInn.Domain.Inn
{
    public class InnConfigData
    {
        public int BaseFee { get; init; }
        public int MaxTip { get; init; }
        public float TipThreshold { get; init; }
        public int InitialLandSizeX { get; init; }
        public int InitialLandSizeZ { get; init; }
        public int ExpansionStepSize { get; init; }
        public int LandPurchaseCost { get; init; }
        public int InitialFunds { get; init; }
        public int InitialBedCount { get; init; }
    }
}
