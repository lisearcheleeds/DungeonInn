using DungeonInn.Domain.Inn;
using UnityEngine;

namespace DungeonInn.Infrastructure.Repository
{
    [CreateAssetMenu(menuName = "DungeonInn/Config/InnConfig")]
    public class InnConfigSO : ScriptableObject
    {
        public int BaseFee = 100;
        public int MaxTip = 50;
        public float TipThreshold = 0.7f;
        public int InitialLandSizeX = 10;
        public int InitialLandSizeZ = 10;
        public int ExpansionStepSize = 5;
        public int LandPurchaseCost = 500;
        public int InitialFunds = 1000;
        public int InitialBedCount = 4;

        public InnConfigData ToData()
        {
            return new InnConfigData
            {
                BaseFee = BaseFee,
                MaxTip = MaxTip,
                TipThreshold = TipThreshold,
                InitialLandSizeX = InitialLandSizeX,
                InitialLandSizeZ = InitialLandSizeZ,
                ExpansionStepSize = ExpansionStepSize,
                LandPurchaseCost = LandPurchaseCost,
                InitialFunds = InitialFunds,
                InitialBedCount = InitialBedCount,
            };
        }
    }
}
