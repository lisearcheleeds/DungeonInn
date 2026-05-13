using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using LighthouseExtends.Addressable;
using LighthouseExtends.ScreenStack;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DungeonInn.View.Scene.ModuleScene.ScreenStack
{
    public sealed class ScreenStackInstanceFactory : IScreenStackInstanceFactory, IDisposable
    {
        readonly IObjectResolver objectResolver;
        readonly IAssetScope assetScope;

        [Inject]
        public ScreenStackInstanceFactory(
            IObjectResolver objectResolver,
            IAssetManager assetManager)
        {
            this.objectResolver = objectResolver ?? throw new ArgumentNullException(nameof(objectResolver));
            assetScope = (assetManager ?? throw new ArgumentNullException(nameof(assetManager))).CreateScope();
        }

        async UniTask<TScreenStack> IScreenStackInstanceFactory.CreateScreenStackInstance<TScreenStack>(
            string screenStackAddress,
            IScreenStackData data,
            CancellationToken ct)
        {
            var handle = await assetScope.LoadAsync<GameObject>(screenStackAddress, ct);
            var gameObject = objectResolver.Instantiate(handle.Asset);
            return gameObject.GetComponents<MonoBehaviour>().OfType<TScreenStack>().First();
        }

        public void Dispose() => assetScope.Dispose();
    }
}
