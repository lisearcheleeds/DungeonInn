using DungeonInn.Input.Layer;
using DungeonInn.View.Scene;
using DungeonInn.View.Scene.MainScene.World.Debug;
using DungeonInn.View.Scene.MainScene.World.Settings;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldLifetimeScope : LifetimeScope
    {
        [SerializeField] WorldScene worldScene;
        [SerializeField] MapMaterialSetSO mapMaterialSetSO;
        [SerializeField] ActorSpriteVisualConfigSO actorSpriteVisualConfigSO;

        protected override void Configure(IContainerBuilder builder)
        {
            // === View: シーン基盤 ===
            builder.RegisterComponent(worldScene);
            builder.RegisterComponentInHierarchy<WorldGameLoopEntryPoint>();
            builder.Register<WorldPresenter>(Lifetime.Scoped).AsImplementedInterfaces();
            builder.Register<WorldViewRoot>(Lifetime.Scoped);
            builder.Register<WorldMapViewSettingsRepository>(Lifetime.Scoped).As<IWorldMapViewSettingsRepository>();
            builder.Register<LayerPositionViewSettingsRepository>(Lifetime.Scoped).As<ILayerPositionViewSettingsRepository>();
            builder.Register<WorldCameraSettingsRepository>(Lifetime.Scoped).As<IWorldCameraSettingsRepository>();
            builder.Register<MapLayerViewRegistry>(Lifetime.Scoped);
            builder.RegisterEntryPoint<WorldActiveLayerNavigationInvalidator>();
            builder.Register<LayerPositionViewMapper>(Lifetime.Scoped);
            builder.Register<WorldCameraController>(Lifetime.Scoped);
            builder.Register<WorldAddressableViewFactory>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
            builder.Register<UnityNavMeshPathProvider>(Lifetime.Scoped).AsSelf();
            builder.RegisterEntryPoint<WorldNavigationPathProviderEntryPoint>();

            // === View: マップ描画 ===
            builder.RegisterInstance(new VisualConfigSettings(mapMaterialSetSO, actorSpriteVisualConfigSO));
            builder.Register<VisualConfigLoader>(Lifetime.Scoped);
            builder.Register<MapMaterialSet>(Lifetime.Scoped);
            builder.Register<MapTileVisualConfig>(Lifetime.Scoped);
            builder.Register<MapMeshBuildService>(Lifetime.Scoped);
            builder.Register<NavMeshBuildService>(Lifetime.Scoped);
            builder.Register<EnvironmentObjectPlacer>(Lifetime.Scoped);
            builder.Register<WorldMapView>(Lifetime.Scoped);

            // === View: アクター描画 ===
            builder.Register<ActorVisualDefinitionLoader>(Lifetime.Scoped);
            builder.Register<ActorPrefabSource>(Lifetime.Scoped);
            builder.Register<WorldActorViewPool>(Lifetime.Scoped);
            builder.Register<WorldActorViewRegistry>(Lifetime.Scoped);
            builder.Register<WorldActorPresenter>(Lifetime.Scoped);
            builder.RegisterEntryPoint<WorldActiveLayerActorViewRefresher>();
            builder.Register<ProjectilePrefabSource>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
            builder.Register<WorldProjectileViewPool>(Lifetime.Scoped);
            builder.Register<WorldProjectilePresenter>(Lifetime.Scoped);
            builder.Register<AreaEffectPrefabSource>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
            builder.Register<WorldAreaEffectViewPool>(Lifetime.Scoped);
            builder.Register<WorldAreaEffectPresenter>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
            builder.Register<ActorCombatAnimationPresenter>(Lifetime.Scoped).AsSelf().AsImplementedInterfaces();
            builder.Register<WorldLayerViewController>(Lifetime.Scoped);
            builder.Register<WorldActorSelectionInputHandler>(Lifetime.Scoped);
            builder.Register<WorldActorCameraFollowController>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();

            builder.Register<WorldActorScreenPositionProvider>(Lifetime.Scoped).AsSelf();
            builder.RegisterEntryPoint<WorldActorScreenPositionProviderEntryPoint>();
            builder.Register<WorldActiveLayerProvider>(Lifetime.Scoped).AsSelf();
            builder.RegisterEntryPoint<WorldActiveLayerProviderEntryPoint>();
#if DEBUG
            builder.RegisterEntryPoint<WorldDebugGameLogPresenter>(Lifetime.Scoped);
#endif
        }
    }
}
