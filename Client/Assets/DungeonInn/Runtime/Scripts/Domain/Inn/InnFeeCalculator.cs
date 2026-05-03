using DungeonInn.Domain.Character;

namespace DungeonInn.Domain.Inn
{
    public class InnFeeCalculator
    {
        public Money Calculate(Satisfaction satisfaction, Money baseFee, Money maxTip, float tipThreshold)
        {
            if (!satisfaction.IsAboveThreshold(tipThreshold))
            {
                return baseFee;
            }

            return baseFee + maxTip;
        }
    }
}
