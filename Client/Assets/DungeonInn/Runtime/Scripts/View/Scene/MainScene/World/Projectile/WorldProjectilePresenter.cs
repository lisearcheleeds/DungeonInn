using System;
using System.Collections.Generic;
using DungeonInn.Application.World;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldProjectilePresenter
    {
        readonly IGameWorldStateReader worldState;
        readonly WorldProjectileViewPool projectileViewPool;
        readonly LayerPositionViewMapper positionMapper;
        readonly WorldCameraController worldCameraController;
        readonly HashSet<Guid> currentIds = new();
        readonly List<Guid> toRemove = new();
        readonly Action<Guid, ProjectileView> collectRemovedProjectileAction;

        [Inject]
        public WorldProjectilePresenter(
            IGameWorldStateReader worldState,
            WorldProjectileViewPool projectileViewPool,
            LayerPositionViewMapper positionMapper,
            WorldCameraController worldCameraController)
        {
            this.worldState = worldState ?? throw new ArgumentNullException(nameof(worldState));
            this.projectileViewPool = projectileViewPool ?? throw new ArgumentNullException(nameof(projectileViewPool));
            this.positionMapper = positionMapper ?? throw new ArgumentNullException(nameof(positionMapper));
            this.worldCameraController =
                worldCameraController ?? throw new ArgumentNullException(nameof(worldCameraController));
            collectRemovedProjectileAction = CollectRemovedProjectile;
        }

        public void UpdatePositions()
        {
            var frameCameraRotation = worldCameraController.CurrentCameraRotation;
            currentIds.Clear();
            toRemove.Clear();

            for (var index = 0; index < worldState.Projectiles.Count; index++)
            {
                var projectile = worldState.Projectiles[index];
                currentIds.Add(projectile.Id);
                var localPosition = positionMapper.ToActorLayerLocalPosition(projectile.Position);
                var projectileView = projectileViewPool.Rent(
                    projectile.Id,
                    projectile.Position.LayerId,
                    projectile.PrefabAddress);
                projectileView.SetLocalPosition(localPosition);
                projectileView.SetBillboardRotation(frameCameraRotation);
            }

            projectileViewPool.ForEach(collectRemovedProjectileAction);
            for (var index = 0; index < toRemove.Count; index++)
            {
                projectileViewPool.Return(toRemove[index]);
            }
        }

        void CollectRemovedProjectile(Guid projectileId, ProjectileView projectileView)
        {
            if (!currentIds.Contains(projectileId))
            {
                toRemove.Add(projectileId);
            }
        }
    }
}
