using System.Threading;
using Cysharp.Threading.Tasks;

namespace DungeonInn.View.Scene.MainScene.World
{
    public interface IWorldMapViewSettingsRepository
    {
        UniTask LoadAsync(CancellationToken cancellationToken);
        WorldMapViewSettings GetWorldMapViewSettings();
    }
}
