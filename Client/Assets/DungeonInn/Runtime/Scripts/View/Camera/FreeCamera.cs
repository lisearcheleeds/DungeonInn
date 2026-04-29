using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonInn.Runtime.Scripts.View.Camera
{
    public class FreeCamera : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 20f;
        [SerializeField] float rotateSpeed = 0.2f;

        Vector2 lastMousePosition;
        bool isDragging;

        void Update()
        {
            HandleMovement();
            HandleRotation();
        }

        void HandleMovement()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            var delta = Vector3.zero;
            if (kb.wKey.isPressed) delta += transform.forward;
            if (kb.sKey.isPressed) delta -= transform.forward;
            if (kb.aKey.isPressed) delta -= transform.right;
            if (kb.dKey.isPressed) delta += transform.right;
            if (kb.eKey.isPressed) delta += Vector3.up;
            if (kb.qKey.isPressed) delta -= Vector3.up;

            if (delta != Vector3.zero)
                transform.position += delta.normalized * (moveSpeed * Time.deltaTime);
        }

        void HandleRotation()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.rightButton.wasPressedThisFrame)
            {
                isDragging = true;
                lastMousePosition = mouse.position.ReadValue();
            }
            else if (mouse.rightButton.wasReleasedThisFrame)
            {
                isDragging = false;
            }

            if (!isDragging || !mouse.rightButton.isPressed) return;

            var currentPos = mouse.position.ReadValue();
            var mouseDelta = currentPos - lastMousePosition;
            lastMousePosition = currentPos;

            var euler = transform.eulerAngles;
            euler.y += mouseDelta.x * rotateSpeed;

            float newPitch = euler.x - mouseDelta.y * rotateSpeed;
            if (newPitch > 180f) newPitch -= 360f;
            euler.x = Mathf.Clamp(newPitch, -80f, 80f);

            transform.eulerAngles = euler;
        }
    }
}
