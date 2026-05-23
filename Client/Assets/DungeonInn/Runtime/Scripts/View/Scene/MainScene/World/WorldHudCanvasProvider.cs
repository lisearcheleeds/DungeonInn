using System;
using DungeonInn.View.Scene.ModuleScene.WorldUI;
using UnityEngine;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldHudCanvasProvider : IInitializable, IDisposable
    {
        Canvas hudCanvas;
        GameObject fallbackCanvasObject;

        public Canvas HUDCanvas => hudCanvas;

        public void Initialize()
        {
            var hudScene = Object.FindFirstObjectByType<WorldUIModuleScene>();
            if (hudScene != null && hudScene.HUDCanvas != null)
            {
                hudCanvas = hudScene.HUDCanvas;
                return;
            }

            Debug.LogWarning(
                "[World] WorldUIModuleScene.HUDCanvas was not found. Using a fallback HUD canvas for development/test execution.");
            fallbackCanvasObject = new GameObject("WorldUICanvas_Fallback");
            hudCanvas = fallbackCanvasObject.AddComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        public void Dispose()
        {
            if (fallbackCanvasObject == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                Object.DestroyImmediate(fallbackCanvasObject);
                return;
            }
#endif
            Object.Destroy(fallbackCanvasObject);
        }
    }
}
