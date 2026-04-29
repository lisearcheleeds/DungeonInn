using System;
using LighthouseExtends.InputLayer;
using SampleProduct.Input;
using UnityEngine.InputSystem;

namespace YourProduct.Input.Layer
{
    public class DefaultScreenStackInputLayer : IInputLayer
    {
        readonly InputActions.ScreenStackActions screenStackActions;
        readonly Action onBack;

        public DefaultScreenStackInputLayer(InputActions actions, Action onBack)
        {
            screenStackActions = actions.ScreenStack;
            this.onBack = onBack;
        }

        public virtual bool BlocksAllInput => true;

        public virtual bool OnActionStarted(InputAction.CallbackContext callbackContext)
        {
            return false;
        }

        public virtual bool OnActionPerformed(InputAction.CallbackContext callbackContext)
        {
            if (callbackContext.action.id == screenStackActions.Back.id && onBack != null)
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
