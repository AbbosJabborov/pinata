using System;
using Pinata.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pinata.Gameplay
{
    public class UpgradeKiosk : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField] private float interactionRadius = 2.8f;

        private bool _isPlayerNear = false;
        private bool _isOpen = false;

        public bool IsPlayerNear => _isPlayerNear;
        public bool IsOpen => _isOpen;

        public event Action<bool> OnKioskStateChanged; // (isOpen)

        private void Update()
        {
            if (_isPlayerNear)
            {
                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                {
                    ToggleKiosk();
                }
            }

            if (_isOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseKiosk();
            }
        }

        public void ToggleKiosk()
        {
            if (_isOpen) CloseKiosk();
            else OpenKiosk();
        }

        public void OpenKiosk()
        {
            _isOpen = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            OnKioskStateChanged?.Invoke(true);
        }

        public void CloseKiosk()
        {
            _isOpen = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            OnKioskStateChanged?.Invoke(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponentInParent<FirstPersonPlayer>() != null)
            {
                _isPlayerNear = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponentInParent<FirstPersonPlayer>() != null)
            {
                _isPlayerNear = false;
                if (_isOpen)
                {
                    CloseKiosk();
                }
            }
        }
    }
}
