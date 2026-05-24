using System.Threading;
using Cysharp.Threading.Tasks;
using DungeonInn.View.Scene.MainScene.World;

namespace DungeonInn.Tests.EditMode
{
    public sealed class FixedWorldCameraSettingsRepository : IWorldCameraSettingsRepository
    {
        readonly WorldCameraSettings settings;

        public FixedWorldCameraSettingsRepository(WorldCameraSettings settings)
        {
            this.settings = settings;
        }

        public UniTask LoadAsync(CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

        public WorldCameraSettings Get()
        {
            return settings;
        }
    }
}

