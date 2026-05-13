using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldCameraSettings
    {
        public Vector3 InitialPosition { get; }
        public float InitialYawDegrees { get; }
        public float PitchDegrees { get; }
        public float MoveSpeed { get; }
        public float RotationSensitivity { get; }
        public float ZoomSensitivity { get; }
        public float InitialOrthographicSize { get; }
        public float MinOrthographicSize { get; }
        public float MaxOrthographicSize { get; }

        public WorldCameraSettings()
            : this(
                initialPosition: new Vector3(64f, 80f, -64f),
                initialYawDegrees: 45f,
                pitchDegrees: 60f,
                moveSpeed: 32f,
                rotationSensitivity: 0.2f,
                zoomSensitivity: 0.02f,
                initialOrthographicSize: 48f,
                minOrthographicSize: 12f,
                maxOrthographicSize: 120f)
        {
        }

        public WorldCameraSettings(
            Vector3 initialPosition,
            float initialYawDegrees,
            float pitchDegrees,
            float moveSpeed,
            float rotationSensitivity,
            float zoomSensitivity,
            float initialOrthographicSize,
            float minOrthographicSize,
            float maxOrthographicSize)
        {
            InitialPosition = initialPosition;
            InitialYawDegrees = initialYawDegrees;
            PitchDegrees = pitchDegrees;
            MoveSpeed = moveSpeed;
            RotationSensitivity = rotationSensitivity;
            ZoomSensitivity = zoomSensitivity;
            InitialOrthographicSize = initialOrthographicSize;
            MinOrthographicSize = minOrthographicSize;
            MaxOrthographicSize = maxOrthographicSize;
        }
    }
}
