using System;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldCameraController
    {
        const float FollowPositionLerpSpeed = 8f;
        const float ZoomLerpSpeed = 5f;
        const float FocusPlaneY = 0f;
        const float MinimumFocusDistance = 0.01f;
        const float FocusPlaneDirectionEpsilon = 0.0001f;

        readonly IWorldCameraSettingsRepository settingsRepository;

        Camera camera;
        bool initialized;
        bool isRotating;
        Vector2 moveInput;
        Vector2 lookDelta;
        Vector2 zoomDelta;
        float yawDegrees;
        float pitchDegrees;
        bool isFollowing;
        Vector3 followTargetPosition;
        bool hasTargetOrthographicSize;
        float targetOrthographicSize;
        Vector3 focusPoint;
        float focusDistance;

        public float CurrentYawDegrees => yawDegrees;
        public Quaternion CurrentCameraRotation => camera != null ? camera.transform.rotation : Quaternion.identity;
        public float ActorViewportMargin => settingsRepository.Get().ActorViewportMargin;

        [Inject]
        public WorldCameraController(IWorldCameraSettingsRepository settingsRepository)
        {
            this.settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
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

        public void BeginFollow(float zoomRatio)
        {
            isFollowing = true;
            hasTargetOrthographicSize = true;
            var settings = settingsRepository.Get();
            targetOrthographicSize = settings.InitialOrthographicSize * zoomRatio;
        }

        public void UpdateFollowPosition(Vector3 worldPosition)
        {
            followTargetPosition = worldPosition;
        }

        public void EndFollow()
        {
            isFollowing = false;
            if (camera == null)
            {
                hasTargetOrthographicSize = false;
                return;
            }

            hasTargetOrthographicSize = true;
            targetOrthographicSize = settingsRepository.Get().InitialOrthographicSize;
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

            var rotationChanged = UpdateRotation();
            ApplyRotation(camera);
            var focusChanged = UpdateFocusPoint(deltaSeconds);
            if (rotationChanged || focusChanged)
            {
                ApplyCameraPosition(camera);
            }

            UpdateZoom(camera, deltaSeconds);
        }

        public bool IsWorldPositionVisible(Vector3 worldPosition, float viewportMargin)
        {
            if (camera == null)
            {
                return true;
            }

            var viewportPosition = camera.WorldToViewportPoint(worldPosition);
            return 0f < viewportPosition.z &&
                -viewportMargin <= viewportPosition.x &&
                viewportPosition.x <= 1f + viewportMargin &&
                -viewportMargin <= viewportPosition.y &&
                viewportPosition.y <= 1f + viewportMargin;
        }

        public Vector3 WorldToScreenPoint(Vector3 worldPosition)
        {
            if (camera == null)
            {
                return Vector3.zero;
            }

            return camera.WorldToScreenPoint(worldPosition);
        }

        void InitializeCamera(Camera camera)
        {
            var settings = settingsRepository.Get();
            yawDegrees = settings.InitialYawDegrees;
            pitchDegrees = settings.InitialPitchDegrees;
            camera.transform.position = settings.InitialPosition;
            camera.orthographicSize = settings.InitialOrthographicSize;
            ApplyRotation(camera);
            focusPoint = ResolveCenterFocusPoint(camera);
            focusDistance = Mathf.Max(
                MinimumFocusDistance,
                Vector3.Dot(focusPoint - camera.transform.position, camera.transform.forward));
            initialized = true;
        }

        bool UpdateRotation()
        {
            var settings = settingsRepository.Get();
            if (!isRotating)
            {
                lookDelta = Vector2.zero;
                return false;
            }

            var yawDelta = lookDelta.x * settings.RotationSensitivity;
            yawDegrees += yawDelta;
            lookDelta = Vector2.zero;
            return 0f < Mathf.Abs(yawDelta);
        }

        bool UpdateFocusPoint(float deltaSeconds)
        {
            var settings = settingsRepository.Get();
            if (isFollowing)
            {
                if (deltaSeconds <= 0f)
                {
                    return false;
                }

                focusPoint = Vector3.Lerp(
                    focusPoint,
                    ToFocusPlanePoint(followTargetPosition),
                    FollowPositionLerpSpeed * deltaSeconds);
                return true;
            }

            if (moveInput.sqrMagnitude <= 0f)
            {
                return false;
            }

            var yawRotation = Quaternion.Euler(0f, yawDegrees, 0f);
            var forward = yawRotation * Vector3.forward;
            var right = yawRotation * Vector3.right;
            var moveDirection = right * moveInput.x + forward * moveInput.y;
            focusPoint += moveDirection * settings.MoveSpeed * deltaSeconds;
            return true;
        }

        void UpdateZoom(Camera camera, float deltaSeconds)
        {
            var settings = settingsRepository.Get();
            if (hasTargetOrthographicSize)
            {
                camera.orthographicSize = Mathf.Lerp(
                    camera.orthographicSize,
                    targetOrthographicSize,
                    ZoomLerpSpeed * deltaSeconds);

                if (Mathf.Abs(camera.orthographicSize - targetOrthographicSize) < 0.01f)
                {
                    camera.orthographicSize = targetOrthographicSize;
                    if (!isFollowing)
                    {
                        hasTargetOrthographicSize = false;
                    }
                }

                zoomDelta = Vector2.zero;
                return;
            }

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

        void ApplyCameraPosition(Camera camera)
        {
            camera.transform.position = focusPoint - camera.transform.forward * focusDistance;
        }

        Vector3 ResolveCenterFocusPoint(Camera camera)
        {
            var settings = settingsRepository.Get();
            var direction = camera.transform.forward;
            if (Mathf.Abs(direction.y) < FocusPlaneDirectionEpsilon)
            {
                return ToFocusPlanePoint(camera.transform.position + direction * settings.InitialOrthographicSize);
            }

            var distance = (FocusPlaneY - camera.transform.position.y) / direction.y;
            if (distance <= 0f)
            {
                return ToFocusPlanePoint(camera.transform.position + direction * settings.InitialOrthographicSize);
            }

            return camera.transform.position + direction * distance;
        }

        static Vector3 ToFocusPlanePoint(Vector3 worldPosition)
        {
            return new Vector3(worldPosition.x, FocusPlaneY, worldPosition.z);
        }
    }
}
