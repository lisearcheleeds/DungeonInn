using System;

namespace DungeonInn.Application.NewGame
{
    public static class NewGameSeedParser
    {
        public const int DefaultSeed = 12345;
        public const string DefaultSeedText = "12345";

        public static int ParseOrDefault(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return DefaultSeed;
            }

            return int.TryParse(text, out var seed) ? seed : DefaultSeed;
        }
    }
}
