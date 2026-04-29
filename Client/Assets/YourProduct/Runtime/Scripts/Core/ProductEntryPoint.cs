using System.Threading;
using Cysharp.Threading.Tasks;
using Lighthouse.Scene;
using LighthouseExtends.Language;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace YourProduct.Core
{
    public class ProductEntryPoint : IAsyncStartable
    {
        readonly ProductLifetimeScope productLifetimeScope;
        readonly ProductLifetimeScopeSettings productLifetimeScopeSettings;
        readonly ILauncher launcher;
        readonly IMainSceneManager mainSceneManager;
        readonly IModuleSceneManager moduleSceneManager;
        readonly ILanguageService languageService;
        readonly ISupportedLanguageService supportedLanguageService;

        [Inject]
        public ProductEntryPoint(
            ProductLifetimeScope productLifetimeScope,
            ProductLifetimeScopeSettings productLifetimeScopeSettings,
            ILauncher launcher,
            IMainSceneManager mainSceneManager,
            IModuleSceneManager moduleSceneManager,
            ILanguageService languageService,
            ISupportedLanguageService supportedLanguageService)
        {
            this.productLifetimeScope = productLifetimeScope;
            this.productLifetimeScopeSettings = productLifetimeScopeSettings;
            this.launcher = launcher;
            this.mainSceneManager = mainSceneManager;
            this.moduleSceneManager = moduleSceneManager;
            this.languageService = languageService;
            this.supportedLanguageService = supportedLanguageService;
        }

        public async UniTask StartAsync(CancellationToken cancellation)
        {
            mainSceneManager.SetEnqueueParentLifetimeScope(() => LifetimeScope.EnqueueParent(productLifetimeScope));
            moduleSceneManager.SetEnqueueParentLifetimeScope(() => LifetimeScope.EnqueueParent(productLifetimeScope));

            await languageService.SetLanguage(ResolveInitialLanguage(Application.systemLanguage), cancellation);
            await launcher.Launch();
        }

        string ResolveInitialLanguage(SystemLanguage systemLanguage)
        {
            var code = systemLanguage switch
            {
                SystemLanguage.Japanese => "ja",
                SystemLanguage.ChineseSimplified => "zh",
                SystemLanguage.ChineseTraditional => "zh",
                SystemLanguage.Korean => "ko",
                _ => "en",
            };

            var supported = supportedLanguageService.SupportedLanguages;
            for (var i = 0; i < supported.Count; i++)
            {
                if (supported[i] == code) { return code; }
            }

            return supportedLanguageService.DefaultLanguage;
        }
    }
}

