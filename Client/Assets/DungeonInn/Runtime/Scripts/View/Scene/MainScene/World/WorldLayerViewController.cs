using System;
using UnityEngine.InputSystem;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldLayerViewController : IDisposable
    {
        readonly MapLayerViewRegistry layerViewRegistry;
        readonly InputAction previousLayerAction;
        readonly InputAction nextLayerAction;

        [Inject]
        public WorldLayerViewController(MapLayerViewRegistry layerViewRegistry)
        {
            this.layerViewRegistry = layerViewRegistry ?? throw new ArgumentNullException(nameof(layerViewRegistry));
            previousLayerAction = new InputAction("PreviousWorldLayer", InputActionType.Button, "<Keyboard>/q");
            nextLayerAction = new InputAction("NextWorldLayer", InputActionType.Button, "<Keyboard>/e");
            previousLayerAction.Enable();
            nextLayerAction.Enable();
        }

        public void UpdateLayerSelection()
        {
            if (previousLayerAction.WasPressedThisFrame())
            {
                layerViewRegistry.SelectPreviousLayer();
            }

            if (nextLayerAction.WasPressedThisFrame())
            {
                layerViewRegistry.SelectNextLayer();
            }
        }

        public void Dispose()
        {
            previousLayerAction.Dispose();
            nextLayerAction.Dispose();
        }
    }
}
