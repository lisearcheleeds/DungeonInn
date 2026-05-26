using System;
using System.Collections.Generic;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.World;
using DungeonInn.Domain.Combat;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldAreaEffectPresenter : IInitializable, IDisposable
    {
        readonly IGameWorldStateReader worldState;
        readonly WorldAreaEffectViewPool areaEffectViewPool;
        readonly LayerPositionViewMapper positionMapper;
        readonly WorldCameraController worldCameraController;
        readonly IEventSubscriber eventSubscriber;
        readonly HashSet<Guid> currentIds = new();
        readonly List<Guid> toRemove = new();
        readonly Dictionary<Guid, float> maxDurations = new();
        readonly Action<Guid, AreaEffectView> collectRemovedAreaEffectAction;
        DisposableBag bag;

        [Inject]
        public WorldAreaEffectPresenter(
            IGameWorldStateReader worldState,
            WorldAreaEffectViewPool areaEffectViewPool,
            LayerPositionViewMapper positionMapper,
            WorldCameraController worldCameraController,
            IEventSubscriber eventSubscriber)
        {
            this.worldState = worldState ?? throw new ArgumentNullException(nameof(worldState));
            this.areaEffectViewPool =
                areaEffectViewPool ?? throw new ArgumentNullException(nameof(areaEffectViewPool));
            this.positionMapper = positionMapper ?? throw new ArgumentNullException(nameof(positionMapper));
            this.worldCameraController =
                worldCameraController ?? throw new ArgumentNullException(nameof(worldCameraController));
            this.eventSubscriber = eventSubscriber ?? throw new ArgumentNullException(nameof(eventSubscriber));
            collectRemovedAreaEffectAction = CollectRemovedAreaEffect;
        }

        public void Initialize()
        {
            eventSubscriber.OnEvent<AreaEffectHit>()
                .Subscribe(OnAreaEffectHit)
                .AddTo(ref bag);
        }

        public void UpdatePositions()
        {
            var frameCameraRotation = worldCameraController.CurrentCameraRotation;
            currentIds.Clear();
            toRemove.Clear();

            for (var index = 0; index < worldState.AreaEffects.Count; index++)
            {
                var areaEffect = worldState.AreaEffects[index];
                currentIds.Add(areaEffect.Id);

                if (!maxDurations.ContainsKey(areaEffect.Id))
                {
                    var initialDuration = areaEffect.AreaSpec.DurationType == AttackAreaDurationType.Duration
                        ? (float)areaEffect.AreaSpec.DurationTicks
                        : 0f;
                    maxDurations[areaEffect.Id] = initialDuration < float.Epsilon
                        ? float.Epsilon
                        : initialDuration;
                }

                var localPosition = positionMapper.ToActorLayerLocalPosition(areaEffect.CenterPosition);
                var areaEffectView = areaEffectViewPool.Rent(
                    areaEffect.Id,
                    areaEffect.CenterPosition.LayerId,
                    areaEffect.PrefabAddress);
                areaEffectView.SetLocalPosition(localPosition);
                areaEffectView.SetBillboardRotation(frameCameraRotation);
                areaEffectView.SetShape(areaEffect.AreaSpec.Shape, areaEffect.AreaSpec.RadiusMeters);
                var normalizedProgress = areaEffect.RemainingDurationSeconds / maxDurations[areaEffect.Id];
                areaEffectView.SetNormalizedProgress(Mathf.Clamp01(normalizedProgress));
            }

            areaEffectViewPool.ForEach(collectRemovedAreaEffectAction);
            for (var index = 0; index < toRemove.Count; index++)
            {
                areaEffectViewPool.Return(toRemove[index]);
                maxDurations.Remove(toRemove[index]);
            }
        }

        public void Dispose()
        {
            bag.Dispose();
        }

        void OnAreaEffectHit(AreaEffectHit gameEvent)
        {
            if (areaEffectViewPool.TryGetActive(gameEvent.AreaEffectId, out var view))
            {
                view.TriggerHitPulse();
            }
        }

        void CollectRemovedAreaEffect(Guid areaEffectId, AreaEffectView areaEffectView)
        {
            if (!currentIds.Contains(areaEffectId))
            {
                toRemove.Add(areaEffectId);
            }
        }
    }
}
