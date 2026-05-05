using Cysharp.Threading.Tasks;

namespace DungeonInn.Application.GameLoop
{
    public interface IGameLoopUseCase
    {
        UniTask<GameLoopTickResult> ExecuteAsync(GameLoopTickRequest request);
    }
}
