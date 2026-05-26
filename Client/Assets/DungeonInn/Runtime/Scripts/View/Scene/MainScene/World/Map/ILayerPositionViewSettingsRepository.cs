using System.Threading;
using Cysharp.Threading.Tasks;

namespace DungeonInn.View.Scene.MainScene.World
{
    public interface ILayerPositionViewSettingsRepository
    {
        UniTask LoadAsync(CancellationToken cancellationToken);
        LayerPositionViewSettings Get();
    }
}
