using DungeonInn.Application.NewGame;

namespace DungeonInn.GameSession
{
    public sealed class GameSessionStartRequestStore
    {
        public GameSessionStartRequest Current { get; private set; } =
            GameSessionStartRequest.NewGame(NewGameSeedParser.DefaultSeed);

        public void Set(GameSessionStartRequest request)
        {
            Current = request;
        }
    }
}
