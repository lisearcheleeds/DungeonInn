using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DungeonInn.Master;
using LighthouseExtends.Addressable;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class ActorVisualDefinitionLoader : IDisposable
    {
        readonly IAssetManager assetManager;
        readonly IMasterRepository masterRepository;
        readonly Dictionary<ActorVisualRequestKey, ActorVisualDefinition> loadedDefinitions = new();
        readonly Dictionary<ActorVisualRequestKey, UniTask<ActorVisualDefinition>> loadingTasks = new();
        readonly HashSet<ActorVisualRequestKey> failedLoads = new();
        readonly CancellationTokenSource disposeCancellationTokenSource = new();
        IAssetScope assetScope;
        bool disposed;

        [Inject]
        public ActorVisualDefinitionLoader(
            IAssetManager assetManager,
            IMasterRepository masterRepository)
        {
            this.assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
        }

        public bool TryGetLoaded(
            string visualId,
            int skinId,
            out ActorVisualDefinition definition)
        {
            return loadedDefinitions.TryGetValue(new ActorVisualRequestKey(visualId, skinId), out definition);
        }

        public UniTask<ActorVisualDefinition> RequestLoadAsync(
            string visualId,
            int skinId,
            CancellationToken ct)
        {
            if (disposed)
            {
                return UniTask.FromResult<ActorVisualDefinition>(null);
            }

            var key = new ActorVisualRequestKey(visualId, skinId);
            if (loadedDefinitions.TryGetValue(key, out var loaded))
            {
                return UniTask.FromResult(loaded);
            }

            if (failedLoads.Contains(key))
            {
                return UniTask.FromResult<ActorVisualDefinition>(null);
            }

            if (loadingTasks.TryGetValue(key, out var loadingTask))
            {
                return loadingTask;
            }

            var task = LoadAsync(key, ct).Preserve();
            loadingTasks.Add(key, task);
            return task;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            disposeCancellationTokenSource.Cancel();
            assetScope?.Dispose();
            loadedDefinitions.Clear();
            loadingTasks.Clear();
            failedLoads.Clear();
            disposeCancellationTokenSource.Dispose();
        }

        async UniTask<ActorVisualDefinition> LoadAsync(ActorVisualRequestKey key, CancellationToken ct)
        {
            try
            {
                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                    ct,
                    disposeCancellationTokenSource.Token);
                assetScope ??= assetManager.CreateScope();
                var master = masterRepository.GetActorVisualMaster(key.VisualId, key.SkinId);
                var handle = await assetScope.LoadAsync<ActorVisualDefinitionSO>(
                    master.VisualDefinitionAddress,
                    linkedTokenSource.Token);
                if (disposed)
                {
                    return null;
                }

                var definition = handle.Asset.ToRuntimeDefinition();
                loadedDefinitions[key] = definition;
                return definition;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception exception)
            {
                if (disposed)
                {
                    return null;
                }

                failedLoads.Add(key);
                UnityEngine.Debug.LogWarning(
                    $"[ActorVisualDefinitionLoader] Failed to load actor visual definition. VisualId={key.VisualId} SkinId={key.SkinId} Error={exception.Message}");
                return null;
            }
            finally
            {
                loadingTasks.Remove(key);
            }
        }
    }
}
