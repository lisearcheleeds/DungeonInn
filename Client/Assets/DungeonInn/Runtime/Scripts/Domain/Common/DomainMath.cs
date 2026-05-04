namespace DungeonInn.Domain.Common
{
    public static class DomainMath
    {
        public static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            if (max < value)
            {
                return max;
            }

            return value;
        }
    }
}
