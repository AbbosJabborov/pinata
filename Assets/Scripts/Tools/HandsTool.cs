using System;
using Pinata.Candy;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pinata.Tools
{
    public class HandsTool : MonoBehaviour, ITool
    {
        [Header("Interaction Settings")]
        [SerializeField] private float maxPickupDistance = 2.8f;
        [SerializeField] private float throwForce = 14f;
        [SerializeField] private float dropForce = 1.5f;
        [SerializeField] private Transform holdSocket;
        [SerializeField] private Vector3 holdLocalPos = new Vector3(0.18f, -0.22f, 0.65f);

        [Header("Audio")]
        [SerializeField] private AudioClip grabSound;
        [SerializeField] private AudioClip throwSound;
        [SerializeField] private AudioSource audioSource;

        private Camera _mainCamera;
        private CandyItem _hoveredCandy;
        private CandyItem _heldCandy;
        private Transform _heldOriginalParent;
        private bool _heldOriginalKinematic;

        public event Action<string> OnPromptChanged;

        public string ToolName => "Hands";
        public int SlotIndex => 2;
        public bool IsBusy => false;
        public bool IsHolding => _heldCandy != null;
        public CandyItem HeldCandy => _heldCandy;

        public void OnEquip()
        {
            gameObject.SetActive(true);
            UpdatePrompt();
        }

        public void OnUnequip()
        {
            if (_heldCandy != null)
            {
                DropCandy();
            }
            _hoveredCandy = null;
            OnPromptChanged?.Invoke(string.Empty);
            gameObject.SetActive(false);
        }

        private void Awake()
        {
            _mainCamera = Camera.main;
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            }

            if (holdSocket == null)
            {
                GameObject socketObj = new GameObject("HoldSocket");
                socketObj.transform.SetParent(transform);
                socketObj.transform.localPosition = holdLocalPos;
                socketObj.transform.localRotation = Quaternion.identity;
                holdSocket = socketObj.transform;
            }
        }

        private void Update()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            if (_heldCandy == null)
            {
                CheckHover();

                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && _hoveredCandy != null)
                {
                    PickUpCandy(_hoveredCandy);
                }
            }
            else
            {
                // Smoothly interpolate held candy to hold socket
                if (_heldCandy != null && holdSocket != null)
                {
                    _heldCandy.transform.position = Vector3.Lerp(_heldCandy.transform.position, holdSocket.position, Time.deltaTime * 20f);
                    _heldCandy.transform.rotation = Quaternion.Slerp(_heldCandy.transform.rotation, holdSocket.rotation, Time.deltaTime * 15f);
                }

                // Left click to throw
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    ThrowCandy();
                }
                // Right click to gently drop
                else if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                {
                    DropCandy();
                }
            }
        }

        private void CheckHover()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return;

            Ray ray = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, maxPickupDistance))
            {
                var candy = hit.collider.GetComponentInParent<CandyItem>();
                if (candy != null)
                {
                    if (_hoveredCandy != candy)
                    {
                        _hoveredCandy = candy;
                        UpdatePrompt();
                    }
                    return;
                }
            }

            if (_hoveredCandy != null)
            {
                _hoveredCandy = null;
                UpdatePrompt();
            }
        }

        public void PickUpCandy(CandyItem candy)
        {
            if (candy == null || candy.Rigidbody == null) return;

            _heldCandy = candy;
            _hoveredCandy = null;

            var rb = _heldCandy.Rigidbody;
            _heldOriginalKinematic = rb.isKinematic;
            rb.isKinematic = true;

            if (grabSound != null && audioSource != null)
            {
                audioSource.pitch = UnityEngine.Random.Range(1.05f, 1.25f);
                audioSource.PlayOneShot(grabSound, 0.7f);
            }

            UpdatePrompt();
        }

        public void ThrowCandy()
        {
            if (_heldCandy == null) return;

            var candy = _heldCandy;
            _heldCandy = null;

            if (_mainCamera == null) _mainCamera = Camera.main;
            Vector3 throwDir = _mainCamera != null ? _mainCamera.transform.forward : transform.forward;
            // Slight upward arc
            throwDir = (throwDir + Vector3.up * 0.08f).normalized;

            var rb = candy.Rigidbody;
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = throwDir * throwForce;
                rb.angularVelocity = UnityEngine.Random.insideUnitSphere * 12f;
            }

            if (throwSound != null && audioSource != null)
            {
                audioSource.pitch = UnityEngine.Random.Range(0.95f, 1.15f);
                audioSource.PlayOneShot(throwSound, 0.7f);
            }

            UpdatePrompt();
        }

        public void DropCandy()
        {
            if (_heldCandy == null) return;

            var candy = _heldCandy;
            _heldCandy = null;

            var rb = candy.Rigidbody;
            if (rb != null)
            {
                rb.isKinematic = false;
                if (_mainCamera != null)
                {
                    rb.linearVelocity = _mainCamera.transform.forward * dropForce;
                }
            }

            UpdatePrompt();
        }

        private void UpdatePrompt()
        {
            if (_heldCandy != null)
            {
                OnPromptChanged?.Invoke($"[LMB] Throw {_heldCandy.Type}  |  [RMB] Drop");
            }
            else if (_hoveredCandy != null)
            {
                OnPromptChanged?.Invoke($"[LMB] Pick up {_hoveredCandy.Type}");
            }
            else
            {
                OnPromptChanged?.Invoke(string.Empty);
            }
        }
    }
}
