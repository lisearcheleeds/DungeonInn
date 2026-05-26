using System.Threading;
using Cysharp.Threading.Tasks;

namespace DungeonInn.View.Scene.MainScene.World
{
    public interface IWorldCameraSettingsRepository
    {
        UniTask LoadAsync(CancellationToken cancellationToken);
        WorldCameraSettings Get();
    }
}
