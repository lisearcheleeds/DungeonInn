using System;

namespace DungeonInn.Domain.Actor
{
    public sealed class ActorStats
    {
        public int Strength { get; }
        public int Dexterity { get; }
        public int Constitution { get; }
        public int Intelligence { get; }
        public int Wisdom { get; }
        public int Charisma { get; }

        public ActorStats(
            int strength,
            int dexterity,
            int constitution,
            int intelligence,
            int wisdom,
            int charisma)
        {
            Strength = ValidateStat(strength, nameof(strength));
            Dexterity = ValidateStat(dexterity, nameof(dexterity));
            Constitution = ValidateStat(constitution, nameof(constitution));
            Intelligence = ValidateStat(intelligence, nameof(intelligence));
            Wisdom = ValidateStat(wisdom, nameof(wisdom));
            Charisma = ValidateStat(charisma, nameof(charisma));
        }

        public ActorStats Increase(
            int strength,
            int dexterity,
            int constitution,
            int intelligence,
            int wisdom,
            int charisma)
        {
            return new ActorStats(
                Strength + Math.Max(0, strength),
                Dexterity + Math.Max(0, dexterity),
                Constitution + Math.Max(0, constitution),
                Intelligence + Math.Max(0, intelligence),
                Wisdom + Math.Max(0, wisdom),
                Charisma + Math.Max(0, charisma));
        }

        static int ValidateStat(int value, string parameterName)
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }

            return value;
        }
    }
}
