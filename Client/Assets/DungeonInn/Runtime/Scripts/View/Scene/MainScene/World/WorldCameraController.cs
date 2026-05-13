using System;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldCameraController
    {
        readonly WorldCameraSettings settings;

        Camera camera;
        bool initialized;
        bool isRotating;
        Vector2 moveInput;
        Vector2 lookDelta;
        Vector2 zoomDelta;
        float yawDegrees;
        float pitchDegrees;

        public float CurrentYawDegrees => yawDegrees;

        [Inject]
        public WorldCameraController(WorldCameraSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public void BindCamera(Camera camera)
        {
            this.camera = camera ?? throw new ArgumentNullException(nameof(camera));
            initialized = false;
        }

        public void SetMoveInput(Vector2 value)
        {
            moveInput = value;
        }

        public void SetRotating(bool value)
        {
            isRotating = value;
        }

        public void AddLookDelta(Vector2 value)
        {
            lookDelta += value;
        }

        public void AddZoomDelta(Vector2 value)
        {
            zoomDelta += value;
        }

        public void ResetInputState()
        {
            moveInput = Vector2.zero;
            lookDelta = Vector2.zero;
            zoomDelta = Vector2.zero;
            isRotating = false;
        }

        public void UpdateCamera(float deltaSeconds)
        {
            if (camera == null)
            {
                return;
            }

            if (!initialized)
            {
                InitializeCamera(camera);
            }

            UpdateRotation();
            UpdatePosition(camera, deltaSeconds);
            UpdateZoom(camera);
            ApplyRotation(camera);
        }

        void InitializeCamera(Camera camera)
        {
            yawDegrees = camera.transform.eulerAngles.y;
            pitchDegrees = camera.transform.eulerAngles.x;
            initialized = true;
        }

        void UpdateRotation()
        {
            if (!isRotating)
            {
                lookDelta = Vector2.zero;
                return;
            }

            yawDegrees += lookDelta.x * settings.RotationSensitivity;
            lookDelta = Vector2.zero;
        }

        void UpdatePosition(Camera camera, float deltaSeconds)
        {
            if (moveInput.sqrMagnitude <= 0f)
            {
                return;
            }

            var yawRotation = Quaternion.Euler(0f, yawDegrees, 0f);
            var forward = yawRotation * Vector3.forward;
            var right = yawRotation * Vector3.right;
            var moveDirection = right * moveInput.x + forward * moveInput.y;
            camera.transform.position += moveDirection * settings.MoveSpeed * deltaSeconds;
        }

        void UpdateZoom(Camera camera)
        {
            if (zoomDelta.sqrMagnitude <= 0f)
            {
                return;
            }

            camera.orthographicSize = Mathf.Clamp(
                camera.orthographicSize - zoomDelta.y * settings.ZoomSensitivity,
                settings.MinOrthographicSize,
                settings.MaxOrthographicSize);
            zoomDelta = Vector2.zero;
        }

        void ApplyRotation(Camera camera)
        {
            camera.transform.rotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
        }
    }
}
