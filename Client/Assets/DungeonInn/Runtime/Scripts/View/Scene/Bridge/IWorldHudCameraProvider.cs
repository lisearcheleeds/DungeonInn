using UnityEngine;

namespace DungeonInn.View.Scene.Bridge
{
    public interface IWorldHudCameraProvider
    {
        Quaternion CameraRotation { get; }
        float WorldUnitsPerPixel { get; }
    }

    public interface IWorldHudCameraProviderRegistry
    {
        void Register(IWorldHudCameraProvider provider);
        void Unregister(IWorldHudCameraProvider provider);
    }
}
