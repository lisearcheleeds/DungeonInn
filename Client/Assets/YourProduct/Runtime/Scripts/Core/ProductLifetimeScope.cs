using Lighthouse.Scene;
using Lighthouse.Scene.SceneCamera;
using LighthouseExtends.Font;
using LighthouseExtends.InputLayer;
using LighthouseExtends.Language;
using LighthouseExtends.ScreenStack;
using LighthouseExtends.TextTable;
using LighthouseExtends.UIComponent.CanvasSceneObject;
using LighthouseExtends.UIComponent.ExclusiveInput;
using LighthouseExtends.UIComponent.InputBlocker;
using SampleProduct.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;
using YourProduct.Infrastructure.AssetLoader;

namespace YourProduct.Core
{
    public class ProductLifetimeScope : LifetimeScope
    {
        [SerializeField] ProductLifetimeScopeSettings productLifetimeScopeSettings;
        [SerializeField] SupportedLanguageSettings supportedLanguageSettings;
        [SerializeField] LanguageFontSettings languageFontSettings;
        [SerializeField] LHCanvasSceneObject canvasSceneObjectPrefab;
        [SerializeField] LHInputBlocker inputBlockerPrefab;

        protected override void Configure(IContainerBuilder builder)
        {
            // Product
            builder.RegisterEntryPoint<ProductEntryPoint>();
            builder.RegisterInstance(productLifetimeScopeSettings);

            {
                // LightHouse
                builder.Register<SceneManager>(Lifetime.Singleton).AsImplementedInterfaces();
                builder.Register<YourProductSceneManager>(Lifetime.Singleton).As<IYourProductSceneManager>();
                builder.Register<SceneTransitionController>(Lifetime.Singleton).AsImplementedInterfaces();
                builder.Register<DefaultSceneTransitionContextFactory>(Lifetime.Singleton).AsImplementedInterfaces();
                builder.Register<SceneGroupProvider>(Lifetime.Singleton).AsImplementedInterfaces();
                builder.Register<MainSceneManager>(Lifetime.Singleton).AsImplementedInterfaces();
                builder.Register<ModuleSceneManager>(Lifetime.Singleton).AsImplementedInterfaces();
                builder.Register<SceneCameraManager>(Lifetime.Singleton).AsImplementedInterfaces();

                builder.Register<DefaultSceneTransitionSequenceProvider>(Lifetime.Singleton).AsImplementedInterfaces();
            }

            {
                // LightHouse.Extends
                builder.Register<ExclusiveInputService>(Lifetime.Singleton).AsImplementedInterfaces();

                builder.Register<LanguageService>(Lifetime.Singleton).AsImplementedInterfaces();
                builder.RegisterInstance(supportedLanguageSettings);
                builder.Register<SupportedLanguageService>(Lifetime.Singleton).AsImplementedInterfaces();
                builder.Register<TextTableService>(Lifetime.Singleton).AsImplementedInterfaces();
                builder.RegisterInstance(languageFontSettings);
                builder.Register<FontService>(Lifetime.Singleton).AsImplementedInterfaces();

                // Force eager resolution so these services register their handlers
                // to LanguageService before SetLanguage is called in ProductEntryPoint.
                builder.RegisterBuildCallback(container =>
                {
                    container.Resolve<ITextTableService>();
                    container.Resolve<IFontService>();
                });

                // Modules
                builder.Register<ScreenStackModuleProxy>(Lifetime.Singleton).AsImplementedInterfaces();
            }

            {
                // YourProduct
                builder.Register<Launcher>(Lifetime.Singleton).AsImplementedInterfaces();

                {
                    // LightHouse Require
                    builder.RegisterComponentInNewPrefab(canvasSceneObjectPrefab, Lifetime.Singleton).DontDestroyOnLoad().AsImplementedInterfaces();
                    builder.RegisterComponentInNewPrefab(inputBlockerPrefab, Lifetime.Singleton).DontDestroyOnLoad().AsImplementedInterfaces();

                    var inputActions = new InputActions();
                    builder.RegisterInstance(inputActions);
                    builder.RegisterInstance(inputActions.asset).As<InputActionAsset>();
                    builder.Register<InputLayerController>(Lifetime.Singleton).AsImplementedInterfaces();
                }

                builder.Register<YourProductAssetLoader>(Lifetime.Singleton).AsImplementedInterfaces();
            }
        }

    }
}