using DungeonInn.Application.GameLoop;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldPresenter : IWorldPresenter
    {
        IGameLoopUseCase gameLoopUseCase;

        [Inject]
        public void Construct(IGameLoopUseCase gameLoopUseCase)
        {
            this.gameLoopUseCase = gameLoopUseCase;
        }

        void IWorldPresenter.Setup()
        {
        }

        void IWorldPresenter.OnEnter()
        {
        }
    }
}
