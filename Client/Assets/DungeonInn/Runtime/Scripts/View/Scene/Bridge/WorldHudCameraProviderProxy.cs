using System;
using UnityEngine;

namespace DungeonInn.View.Scene.Bridge
{
    public sealed class WorldHudCameraProviderProxy :
        IWorldHudCameraProvider,
        IWorldHudCameraProviderRegistry
    {
        IWorldHudCameraProvider current;

        public Quaternion CameraRotation => current?.CameraRotation ?? Quaternion.identity;
        public float WorldUnitsPerPixel => current?.WorldUnitsPerPixel ?? 1f;

        public void Register(IWorldHudCameraProvider provider)
        {
            if (current != null)
            {
                throw new InvalidOperationException("Duplicate world HUD camera provider registration.");
            }

            current = provider;
        }

        public void Unregister(IWorldHudCameraProvider provider)
        {
            if (ReferenceEquals(current, provider))
            {
                current = null;
            }
        }
    }
}
