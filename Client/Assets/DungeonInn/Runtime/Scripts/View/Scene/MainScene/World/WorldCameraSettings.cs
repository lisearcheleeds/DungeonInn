namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldCameraSettings
    {
        public float MoveSpeed { get; }
        public float RotationSensitivity { get; }
        public float ZoomSensitivity { get; }
        public float MinOrthographicSize { get; }
        public float MaxOrthographicSize { get; }

        public WorldCameraSettings()
            : this(
                moveSpeed: 32f,
                rotationSensitivity: 0.2f,
                zoomSensitivity: 0.02f,
                minOrthographicSize: 12f,
                maxOrthographicSize: 120f)
        {
        }

        public WorldCameraSettings(
            float moveSpeed,
            float rotationSensitivity,
            float zoomSensitivity,
            float minOrthographicSize,
            float maxOrthographicSize)
        {
            MoveSpeed = moveSpeed;
            RotationSensitivity = rotationSensitivity;
            ZoomSensitivity = zoomSensitivity;
            MinOrthographicSize = minOrthographicSize;
            MaxOrthographicSize = maxOrthographicSize;
        }
    }
}
