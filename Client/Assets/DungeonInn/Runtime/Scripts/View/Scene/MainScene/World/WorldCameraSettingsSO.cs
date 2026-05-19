using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    [CreateAssetMenu(menuName = "DungeonInn/Visual/WorldCameraSettings")]
    public sealed class WorldCameraSettingsSO : ScriptableObject
    {
        static readonly Vector3 DefaultInitialPosition = new(64f, 80f, -64f);
        const float DefaultInitialPitchDegrees = 45f;
        const float DefaultInitialYawDegrees = 45f;
        const float DefaultInitialOrthographicSize = 48f;
        const float DefaultMoveSpeed = 32f;
        const float DefaultRotationSensitivity = 0.2f;
        const float DefaultZoomSensitivity = 0.02f;
        const float DefaultMinOrthographicSize = 12f;
        const float DefaultMaxOrthographicSize = 120f;

        [SerializeField] Vector3 initialPosition = DefaultInitialPosition;
        [SerializeField] float initialPitchDegrees = DefaultInitialPitchDegrees;
        [SerializeField] float initialYawDegrees = DefaultInitialYawDegrees;
        [SerializeField] float initialOrthographicSize = DefaultInitialOrthographicSize;
        [SerializeField] float moveSpeed = DefaultMoveSpeed;
        [SerializeField] float rotationSensitivity = DefaultRotationSensitivity;
        [SerializeField] float zoomSensitivity = DefaultZoomSensitivity;
        [SerializeField] float minOrthographicSize = DefaultMinOrthographicSize;
        [SerializeField] float maxOrthographicSize = DefaultMaxOrthographicSize;

        public WorldCameraSettings ToSettings()
        {
            return new WorldCameraSettings(
                initialPosition,
                initialPitchDegrees,
                initialYawDegrees,
                initialOrthographicSize,
                moveSpeed,
                rotationSensitivity,
                zoomSensitivity,
                minOrthographicSize,
                maxOrthographicSize);
        }

        internal static WorldCameraSettings CreateFallbackSettings()
        {
            return new WorldCameraSettings(
                DefaultInitialPosition,
                DefaultInitialPitchDegrees,
                DefaultInitialYawDegrees,
                DefaultInitialOrthographicSize,
                DefaultMoveSpeed,
                DefaultRotationSensitivity,
                DefaultZoomSensitivity,
                DefaultMinOrthographicSize,
                DefaultMaxOrthographicSize);
        }
    }
}
