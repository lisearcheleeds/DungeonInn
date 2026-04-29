using System;
using LighthouseExtends.InputLayer;
using SampleProduct.Input;
using UnityEngine.InputSystem;

namespace YourProduct.Input.Layer
{
    public class DefaultSceneInputLayer : IInputLayer
    {
        readonly InputActions.SceneActions sceneActions;
        readonly Action onBack;

        public DefaultSceneInputLayer(InputActions actions, Action onBack)
        {
            sceneActions = actions.Scene;
            this.onBack = onBack;
        }

        public virtual bool BlocksAllInput => false;

        public virtual bool OnActionStarted(InputAction.CallbackContext callbackContext)
        {
            return false;
        }

        public virtual bool OnActionPerformed(InputAction.CallbackContext callbackContext)
        {
            if (callbackContext.action.id == sceneActions.Back.id && onBack != null)
            {
                onBack();
                return true;
            }

            return false;
        }

        public virtual bool OnActionCanceled(InputAction.CallbackContext callbackContext)
        {
            return false;
        }
    }
}
