using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    [CreateAssetMenu(menuName = "DungeonInn/Visual/WorldCameraSettings")]
    public sealed class WorldCameraSettingsSO : ScriptableObject
    {
        const float DefaultMoveSpeed = 32f;
        const float DefaultRotationSensitivity = 0.2f;
        const float DefaultZoomSensitivity = 0.02f;
        const float DefaultMinOrthographicSize = 12f;
        const float DefaultMaxOrthographicSize = 120f;

        [SerializeField] float moveSpeed = DefaultMoveSpeed;
        [SerializeField] float rotationSensitivity = DefaultRotationSensitivity;
        [SerializeField] float zoomSensitivity = DefaultZoomSensitivity;
        [SerializeField] float minOrthographicSize = DefaultMinOrthographicSize;
        [SerializeField] float maxOrthographicSize = DefaultMaxOrthographicSize;

        public WorldCameraSettings ToSettings()
        {
            return new WorldCameraSettings(
                moveSpeed,
                rotationSensitivity,
                zoomSensitivity,
                minOrthographicSize,
                maxOrthographicSize);
        }

        internal static WorldCameraSettings CreateFallbackSettings()
        {
            return new WorldCameraSettings(
                DefaultMoveSpeed,
                DefaultRotationSensitivity,
                DefaultZoomSensitivity,
                DefaultMinOrthographicSize,
                DefaultMaxOrthographicSize);
        }
    }
}
