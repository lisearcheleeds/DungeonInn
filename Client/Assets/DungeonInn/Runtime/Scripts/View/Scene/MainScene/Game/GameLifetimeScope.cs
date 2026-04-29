using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DungeonInn.Runtime.Scripts.View.Scene.MainScene.Game
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] GameScene gameScene;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(gameScene);
        }
    }
}
