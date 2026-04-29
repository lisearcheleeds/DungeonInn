using Cysharp.Threading.Tasks;
using Lighthouse.Scene;
using Lighthouse.Scene.SceneBase;
using LighthouseExtends.Animation;
using UnityEngine;

namespace YourProduct.View.Base
{
    [RequireComponent(typeof(LHSceneTransitionAnimatorManager))]
    public abstract class ProductCanvasModuleSceneBase : CanvasModuleSceneBase
    {
        [SerializeField] LHSceneTransitionAnimatorManager sceneTransitionAnimatorManager;

        public override void ResetInAnimation(ISceneTransitionContext context)
        {
            sceneTransitionAnimatorManager.ResetInAnimation();
        }

        protected override async UniTask InAnimation(ISceneTransitionContext context)
        {
            await sceneTransitionAnimatorManager.InAnimation();
        }

        protected override async UniTask OutAnimation(ISceneTransitionContext context)
        {
            await sceneTransitionAnimatorManager.OutAnimation();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            sceneTransitionAnimatorManager ??= GetComponent<LHSceneTransitionAnimatorManager>();
        }
#endif
    }
}
