using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldCameraSettings
    {
        public Vector3 InitialPosition { get; }
        public float InitialPitchDegrees { get; }
        public float InitialYawDegrees { get; }
        public float InitialOrthographicSize { get; }
        public float MoveSpeed { get; }
        public float RotationSensitivity { get; }
        public float ZoomSensitivity { get; }
        public float MinOrthographicSize { get; }
        public float MaxOrthographicSize { get; }
        public float ActorViewportMargin { get; }
        public float ActorSelectionZoomRatio { get; }

        public WorldCameraSettings(
            Vector3 initialPosition,
            float initialPitchDegrees,
            float initialYawDegrees,
            float initialOrthographicSize,
            float moveSpeed,
            float rotationSensitivity,
            float zoomSensitivity,
            float minOrthographicSize,
            float maxOrthographicSize,
            float actorViewportMargin,
            float actorSelectionZoomRatio)
        {
            InitialPosition = initialPosition;
            InitialPitchDegrees = initialPitchDegrees;
            InitialYawDegrees = initialYawDegrees;
            InitialOrthographicSize = initialOrthographicSize;
            MoveSpeed = moveSpeed;
            RotationSensitivity = rotationSensitivity;
            ZoomSensitivity = zoomSensitivity;
            MinOrthographicSize = minOrthographicSize;
            MaxOrthographicSize = maxOrthographicSize;
            ActorViewportMargin = actorViewportMargin;
            ActorSelectionZoomRatio = actorSelectionZoomRatio;
        }
    }
}
