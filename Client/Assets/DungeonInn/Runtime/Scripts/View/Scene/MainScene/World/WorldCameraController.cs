using System;
using Lighthouse.Scene.SceneCamera;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldCameraController : IDisposable
    {
        readonly ISceneCameraManager sceneCameraManager;
        readonly WorldCameraSettings settings;
        readonly InputAction moveAction;
        readonly InputAction lookAction;
        readonly InputAction rotateAction;
        readonly InputAction zoomAction;

        bool initialized;
        float yawDegrees;

        public float CurrentYawDegrees => yawDegrees;

        [Inject]
        public WorldCameraController(ISceneCameraManager sceneCameraManager, WorldCameraSettings settings)
        {
            this.sceneCameraManager = sceneCameraManager ?? throw new ArgumentNullException(nameof(sceneCameraManager));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            yawDegrees = settings.InitialYawDegrees;
            moveAction = CreateMoveAction();
            lookAction = new InputAction("WorldCameraLook", InputActionType.Value, "<Mouse>/delta");
            rotateAction = new InputAction("WorldCameraRotate", InputActionType.Button, "<Mouse>/rightButton");
            zoomAction = new InputAction("WorldCameraZoom", InputActionType.Value, "<Mouse>/scroll");
            moveAction.Enable();
            lookAction.Enable();
            rotateAction.Enable();
            zoomAction.Enable();
        }

        public void UpdateCamera(float deltaSeconds)
        {
            var sceneCamera = sceneCameraManager.BaseCamera;
            if (sceneCamera == null)
            {
                return;
            }

            var camera = sceneCamera.GetCamera();
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

        public void Dispose()
        {
            moveAction.Dispose();
            lookAction.Dispose();
            rotateAction.Dispose();
            zoomAction.Dispose();
        }

        void InitializeCamera(Camera camera)
        {
            camera.orthographic = true;
            camera.orthographicSize = settings.InitialOrthographicSize;
            camera.transform.position = settings.InitialPosition;
            ApplyRotation(camera);
            initialized = true;
        }

        void UpdateRotation()
        {
            if (!rotateAction.IsPressed())
            {
                return;
            }

            var lookDelta = lookAction.ReadValue<Vector2>();
            yawDegrees += lookDelta.x * settings.RotationSensitivity;
        }

        void UpdatePosition(Camera camera, float deltaSeconds)
        {
            var moveInput = moveAction.ReadValue<Vector2>();
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
            var scroll = zoomAction.ReadValue<Vector2>();
            if (scroll.sqrMagnitude <= 0f)
            {
                return;
            }

            camera.orthographicSize = Mathf.Clamp(
                camera.orthographicSize - scroll.y * settings.ZoomSensitivity,
                settings.MinOrthographicSize,
                settings.MaxOrthographicSize);
        }

        void ApplyRotation(Camera camera)
        {
            camera.transform.rotation = Quaternion.Euler(settings.PitchDegrees, yawDegrees, 0f);
        }

        static InputAction CreateMoveAction()
        {
            var action = new InputAction("WorldCameraMove", InputActionType.Value);
            action.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            action.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            return action;
        }
    }
}
