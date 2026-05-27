using System;
using DungeonInn.Application.SaveLoad;

namespace DungeonInn.GameSession
{
    public enum GameSessionStartMode
    {
        NewGame = 0,
        LoadGame = 1
    }

    public sealed class GameSessionStartRequest
    {
        GameSessionStartRequest(
            GameSessionStartMode mode,
            int dungeonSeed,
            int gameRandomSeed,
            int? loadSlotId,
            GameSaveData saveData)
        {
            Mode = mode;
            DungeonSeed = dungeonSeed;
            GameRandomSeed = gameRandomSeed;
            LoadSlotId = loadSlotId;
            SaveData = saveData;
        }

        public GameSessionStartMode Mode { get; }
        public int DungeonSeed { get; }
        public int GameRandomSeed { get; }
        public int? LoadSlotId { get; }
        public GameSaveData SaveData { get; }

        public static GameSessionStartRequest NewGame(int seed)
        {
            return new GameSessionStartRequest(GameSessionStartMode.NewGame, seed, seed, null, null);
        }

        public static GameSessionStartRequest LoadGame(int slotId, GameSaveData saveData)
        {
            if (saveData == null)
            {
                throw new ArgumentNullException(nameof(saveData));
            }

            return new GameSessionStartRequest(
                GameSessionStartMode.LoadGame,
                saveData.newGameSeed,
                saveData.newGameSeed,
                slotId,
                saveData);
        }
    }
}
