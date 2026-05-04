using System;

namespace DungeonInn.Domain.Character
{
    public sealed class CharacterStats
    {
        public int Strength { get; private set; }
        public int Dexterity { get; private set; }
        public int Constitution { get; private set; }
        public int Intelligence { get; private set; }
        public int Wisdom { get; private set; }
        public int Charisma { get; private set; }

        public CharacterStats(
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

        public void Increase(
            int strength,
            int dexterity,
            int constitution,
            int intelligence,
            int wisdom,
            int charisma)
        {
            Strength += Math.Max(0, strength);
            Dexterity += Math.Max(0, dexterity);
            Constitution += Math.Max(0, constitution);
            Intelligence += Math.Max(0, intelligence);
            Wisdom += Math.Max(0, wisdom);
            Charisma += Math.Max(0, charisma);
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
