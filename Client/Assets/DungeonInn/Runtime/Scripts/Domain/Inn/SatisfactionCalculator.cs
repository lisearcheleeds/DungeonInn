using DungeonInn.Domain.Character;

namespace DungeonInn.Domain.Inn
{
    public class SatisfactionCalculator
    {
        public Satisfaction Calculate(float density, bool isPrivateRoom)
        {
            var value = 1f - density;
            if (isPrivateRoom)
            {
                value += 0.2f;
            }

            return new Satisfaction(value);
        }
    }
}
