using DungeonInn.Runtime.Scripts.Core;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DungeonInn.Runtime.Scripts.View.Scene.MainScene.Game
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] GameScene gameScene;

        protected override LifetimeScope FindParent()
            => LifetimeScope.Find<ProductLifetimeScope>();

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(gameScene);
        }
    }
}
