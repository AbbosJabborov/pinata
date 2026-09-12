using Pinata.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pinata.Interaction
{
    /// <summary>
    /// Handles first-person raycasting for IInteractable targets and drives interaction events.
    /// Replaces network-dependent interactor logic with clean, single-player architecture
    /// using Unity's new InputSystem (Keyboard/Mouse).
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Raycast Settings")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float interactDistance = 3.2f;
        [SerializeField] private LayerMask interactMask = ~0; // default everything except player

        [Header("Hold UI (Optional)")]
        [Tooltip("Optional UI fill image (0-1) driven during hold interactions. Leave null if unused.")]
        [SerializeField] private UnityEngine.UI.Image holdProgressUI;

        [Header("Carry Point (Optional)")]
        [Tooltip("Attach point for carrying objects in first person.")]
        [SerializeField] private Transform carryPoint;

        [Header("QuickOutline Highlighting")]
        [SerializeField] private bool enableOutline = true;
        [SerializeField] private Color outlineColor = new Color(1f, 0.85f, 0.2f, 1f); // Vibrant warm gold
        [SerializeField] private float outlineWidth = 5f;
        [SerializeField] private Outline.Mode outlineMode = Outline.Mode.OutlineVisible;

        public static PlayerInteractor Instance { get; private set; }

        public IInteractable Current { get; private set; }
        public bool IsLookingAtInteractable => Current != null;
        public Transform CarryPoint => carryPoint;

        private IInteractable _previousInteractable;
        private Outline _currentOutline;
        private IInteractable _holdTarget;
        private float _holdTimer;
        private bool _isHolding;

        private void Awake()
        {
            if (Instance == null) Instance = this;

            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            if (carryPoint == null)
            {
                carryPoint = transform.Find("CameraRoot/CarryPoint");
                if (carryPoint == null && playerCamera != null)
                {
                    carryPoint = playerCamera.transform.Find("CarryPoint");
                }
            }

            // Exclude Player layer from interactMask if Player layer exists
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer != -1)
            {
                interactMask &= ~(1 << playerLayer);
            }
        }

        private void Update()
        {
            if (CursorModeManager.IsUnlocked)
            {
                if (_isHolding) CancelHold();
                if (Current != null)
                {
                    Current = null;
                    UpdateOutline();
                }
                return;
            }

            CheckInteractable();
            UpdateOutline();
            HandleInput();
            TickHold();
        }

        private void CheckInteractable()
        {
            Current = null;

            if (playerCamera == null)
            {
                playerCamera = Camera.main;
                if (playerCamera == null) return;
            }

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask))
            {
                Current = hit.collider.GetComponentInParent<IInteractable>();
            }

            if (_isHolding && Current != _holdTarget)
            {
                CancelHold();
            }
        }

        private void UpdateOutline()
        {
            if (!enableOutline) return;
            if (_previousInteractable == Current) return;

            // Disable outline on previous target
            if (_currentOutline != null)
            {
                _currentOutline.enabled = false;
                _currentOutline = null;
            }

            // Enable or add Outline on newly focused interactable
            if (Current is MonoBehaviour mb)
            {
                _currentOutline = mb.GetComponent<Outline>();
                if (_currentOutline == null)
                {
                    _currentOutline = mb.gameObject.AddComponent<Outline>();
                }

                _currentOutline.OutlineMode = outlineMode;
                _currentOutline.OutlineColor = outlineColor;
                _currentOutline.OutlineWidth = outlineWidth;
                _currentOutline.enabled = true;
            }

            _previousInteractable = Current;
        }

        private void HandleInput()
        {
            if (Keyboard.current == null) return;

            // Primary interact: 'E' key
            bool interactPressed = Keyboard.current.eKey.wasPressedThisFrame;
            bool interactHeld = Keyboard.current.eKey.isPressed;

            if (interactPressed && Current != null)
            {
                BeginInteract();
            }

            if (_isHolding && !interactHeld)
            {
                CancelHold();
            }
        }

        private void BeginInteract()
        {
            if (Current == null) return;

            Current.OnInteractStart(this);

            if (Current.HoldDuration <= 0f)
            {
                Current.OnInteractComplete(this);
            }
            else
            {
                _holdTarget = Current;
                _holdTimer = 0f;
                _isHolding = true;
                SetHoldUI(0f);
            }
        }

        private void TickHold()
        {
            if (!_isHolding || _holdTarget == null) return;

            _holdTimer += Time.deltaTime;
            SetHoldUI(Mathf.Clamp01(_holdTimer / _holdTarget.HoldDuration));

            if (_holdTimer >= _holdTarget.HoldDuration)
            {
                _holdTarget.OnInteractComplete(this);
                ResetHold();
            }
        }

        private void CancelHold()
        {
            _holdTarget?.OnInteractCanceled(this);
            ResetHold();
        }

        private void ResetHold()
        {
            _holdTarget = null;
            _holdTimer = 0f;
            _isHolding = false;
            SetHoldUI(0f);
        }

        private void SetHoldUI(float fill)
        {
            if (holdProgressUI != null)
                holdProgressUI.fillAmount = fill;
        }
    }
}
