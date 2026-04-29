using Cysharp.Threading.Tasks;
using LighthouseExtends.Animation;
using LighthouseExtends.InputLayer;
using LighthouseExtends.ScreenStack;
using SampleProduct.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace YourProduct.View.Base
{
    public class ProductScreenStackBase : ScreenStackBase
    {
        [SerializeField] LHTransitionAnimator transitionAnimator;

        IScreenStackModule screenStackModule;
        IInputLayerController inputLayerController;
        InputActions inputActions;
        IInputLayer inputLayer;

        [Inject]
        public void Construct(
            IScreenStackModule screenStackModule,
            IInputLayerController inputLayerController,
            InputActions inputActions)
        {
            this.screenStackModule = screenStackModule;
            this.inputLayerController = inputLayerController;
            this.inputActions = inputActions;
        }

        protected virtual IInputLayer CreateInputLayer(InputActions inputActions)
        {
            return null;
        }

        protected virtual InputActionMap GetInputLayerActionMap(InputActions inputActions)
        {
            return inputActions.ScreenStack;
        }

        public override UniTask OnEnter(bool isResume)
        {
            var layer = CreateInputLayer(inputActions);
            var actionMap = GetInputLayerActionMap(inputActions);
            if (layer != null && actionMap != null)
            {
                inputLayer = layer;
                inputLayerController.PushLayer(inputLayer, actionMap);
            }

            return UniTask.CompletedTask;
        }

        public override UniTask OnLeave()
        {
            if (inputLayer != null)
            {
                inputLayerController.PopLayer(inputLayer);
                inputLayer = null;
            }

            return UniTask.CompletedTask;
        }

        public override void ResetInAnimation()
        {
            transitionAnimator.ResetInAnimation();
        }

        public override async UniTask PlayInAnimation()
        {
            await transitionAnimator.InAnimation();
        }

        public override void EndInAnimation()
        {
            transitionAnimator.EndInAnimation();
        }

        public override void ResetOutAnimation()
        {
            transitionAnimator.ResetOutAnimation();
        }

        public override async UniTask PlayOutAnimation()
        {
            await transitionAnimator.OutAnimation();
        }

        public override void EndOutAnimation()
        {
            transitionAnimator.EndOutAnimation();
        }
    }
}
