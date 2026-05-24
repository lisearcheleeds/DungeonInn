using System.Threading;
using Cysharp.Threading.Tasks;
using DungeonInn.View.Scene.MainScene.World;

namespace DungeonInn.Tests.EditMode
{
    public sealed class FixedLayerPositionViewSettingsRepository : ILayerPositionViewSettingsRepository
    {
        readonly LayerPositionViewSettings settings;

        public FixedLayerPositionViewSettingsRepository(LayerPositionViewSettings settings)
        {
            this.settings = settings;
        }

        public UniTask LoadAsync(CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

        public LayerPositionViewSettings Get()
        {
            return settings;
        }
    }
}

