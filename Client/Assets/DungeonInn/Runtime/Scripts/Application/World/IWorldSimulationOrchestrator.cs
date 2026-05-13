using System.Threading;
using Cysharp.Threading.Tasks;

namespace DungeonInn.Application.World
{
    public interface IWorldSimulationOrchestrator
    {
        UniTask<WorldSimulationInitializeResult> InitializeAsync(CancellationToken cancellationToken);
        UniTask AdvanceFrameAsync(WorldFrameAdvanceRequest request);
    }
}
