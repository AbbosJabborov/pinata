using UnityEngine;
using UnityEngine.InputSystem;

namespace Pinata.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonPlayer : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 5.5f;
        [SerializeField] private float sprintSpeed = 8.5f;
        [SerializeField] private float gravity = -18.0f;
        [SerializeField] private float jumpHeight = 1.2f;

        [Header("Look")]
        [SerializeField] private Transform cameraHolder;
        [SerializeField] private float mouseSensitivity = 0.15f;
        [SerializeField] private float minPitch = -85f;
        [SerializeField] private float maxPitch = 85f;

        [Header("Held Tool Socket")]
        [SerializeField] private Transform toolSocket;

        private CharacterController _controller;
        private float _pitch = 8f;
        private Vector3 _velocity;
        private bool _isGrounded;

        public Transform CameraHolder => cameraHolder;
        public Transform ToolSocket => toolSocket;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (cameraHolder == null && Camera.main != null)
            {
                cameraHolder = Camera.main.transform;
            }
        }

        private void Start()
        {
            LockCursor(true);
        }

        private void Update()
        {
            HandleCursorLock();
            HandleLook();
            HandleMovement();
        }

        private void HandleCursorLock()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                LockCursor(false);
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                LockCursor(true);
            }
        }

        public void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void HandleLook()
        {
            if (Cursor.lockState != CursorLockMode.Locked || Mouse.current == null) return;

            Vector2 mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity;

            // Yaw (horizontal) rotates character body
            transform.Rotate(Vector3.up * mouseDelta.x);

            // Pitch (vertical) rotates camera holder
            _pitch -= mouseDelta.y;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

            if (cameraHolder != null)
            {
                cameraHolder.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }
        }

        private void HandleMovement()
        {
            _isGrounded = _controller.isGrounded;
            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f;
            }

            Vector2 inputDir = Vector2.zero;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) inputDir.y += 1f;
                if (Keyboard.current.sKey.isPressed) inputDir.y -= 1f;
                if (Keyboard.current.dKey.isPressed) inputDir.x += 1f;
                if (Keyboard.current.aKey.isPressed) inputDir.x -= 1f;
            }
            inputDir.Normalize();

            bool isSprinting = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
            float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

            Vector3 move = transform.right * inputDir.x + transform.forward * inputDir.y;
            _controller.Move(move * (currentSpeed * Time.deltaTime));

            // Jump
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && _isGrounded)
            {
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            // Gravity
            _velocity.y += gravity * Time.deltaTime;
            _controller.Move(_velocity * Time.deltaTime);
        }
    }
}
