using System.Collections;
using Pinata.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pinata.Tools
{
    public class BatTool : MonoBehaviour, ITool
    {
        [Header("Combat Stats")]
        [SerializeField] private float damage = 25f;
        [SerializeField] private float reachDistance = 2.8f;
        [SerializeField] private float hitRadius = 0.45f;
        [SerializeField] private float impulseForce = 14f;
        [SerializeField] private float swingCooldown = 0.38f;

        [Header("Animation Settings")]
        [SerializeField] private Transform batModel;
        [SerializeField] private Vector3 idleLocalPos = new Vector3(0.35f, -0.32f, 0.58f);
        [SerializeField] private Vector3 idleLocalRot = new Vector3(60f, -25f, -30f);

        [Header("Audio")]
        [SerializeField] private AudioClip swooshSound;
        [SerializeField] private AudioSource audioSource;

        private Camera _mainCamera;
        private bool _isSwinging = false;
        private float _lastSwingTime = -10f;

        public string ToolName => "Bat";
        public int SlotIndex => 0;
        public bool IsBusy => _isSwinging;

        public float Damage { get => damage; set => damage = value; }
        public float ImpulseForce { get => impulseForce; set => impulseForce = value; }

        public void OnEquip()
        {
            gameObject.SetActive(true);
            _isSwinging = false;
            if (batModel != null)
            {
                batModel.localPosition = idleLocalPos;
                batModel.localEulerAngles = idleLocalRot;
            }
        }

        public void OnUnequip()
        {
            StopAllCoroutines();
            _isSwinging = false;
            if (batModel != null)
            {
                batModel.localPosition = idleLocalPos;
                batModel.localEulerAngles = idleLocalRot;
            }
            gameObject.SetActive(false);
        }

        public static BatTool Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            _mainCamera = Camera.main;
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            }

            if (batModel != null)
            {
                batModel.localPosition = idleLocalPos;
                batModel.localEulerAngles = idleLocalRot;
            }
        }

        private void Update()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                TrySwing();
            }
        }

        public void TrySwing()
        {
            if (Time.time < _lastSwingTime + swingCooldown || _isSwinging) return;

            _lastSwingTime = Time.time;
            StartCoroutine(SwingRoutine());
        }

        private IEnumerator SwingRoutine()
        {
            _isSwinging = true;

            if (swooshSound != null && audioSource != null)
            {
                audioSource.pitch = Random.Range(0.95f, 1.15f);
                audioSource.PlayOneShot(swooshSound, 0.7f);
            }

            // Phase 1: Windup (quick draw back)
            float windupDuration = 0.08f;
            float elapsed = 0f;
            Vector3 windupPos = idleLocalPos + new Vector3(0.15f, 0.1f, -0.15f);
            Vector3 windupRot = idleLocalRot + new Vector3(-25f, 40f, -20f);

            while (elapsed < windupDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / windupDuration;
                if (batModel != null)
                {
                    batModel.localPosition = Vector3.Lerp(idleLocalPos, windupPos, t);
                    batModel.localRotation = Quaternion.Euler(Vector3.Lerp(idleLocalRot, windupRot, t));
                }
                yield return null;
            }

            // Perform Hit Detection right at the peak of the strike
            PerformHitDetection();

            // Phase 2: Strike (rapid forward swoosh across the screen)
            float strikeDuration = 0.12f;
            elapsed = 0f;
            Vector3 strikePos = idleLocalPos + new Vector3(-0.55f, 0.15f, 0.1f);
            Vector3 strikeRot = idleLocalRot + new Vector3(45f, -90f, 65f);

            while (elapsed < strikeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / strikeDuration;
                // Ease out strike
                t = Mathf.Sin(t * Mathf.PI * 0.5f);
                if (batModel != null)
                {
                    batModel.localPosition = Vector3.Lerp(windupPos, strikePos, t);
                    batModel.localRotation = Quaternion.Euler(Vector3.Lerp(windupRot, strikeRot, t));
                }
                yield return null;
            }

            // Phase 3: Recovery back to idle
            float recoveryDuration = 0.18f;
            elapsed = 0f;
            while (elapsed < recoveryDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / recoveryDuration;
                t = t * t * (3f - 2f * t); // Smoothstep
                if (batModel != null)
                {
                    batModel.localPosition = Vector3.Lerp(strikePos, idleLocalPos, t);
                    batModel.localRotation = Quaternion.Euler(Vector3.Lerp(strikeRot, idleLocalRot, t));
                }
                yield return null;
            }

            if (batModel != null)
            {
                batModel.localPosition = idleLocalPos;
                batModel.localRotation = Quaternion.Euler(idleLocalRot);
            }

            _isSwinging = false;
        }

        private void PerformHitDetection()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return;

            Ray ray = new Ray(_mainCamera.transform.position, _mainCamera.transform.forward);
            RaycastHit[] hits = Physics.SphereCastAll(ray, hitRadius, reachDistance);

            bool hitTarget = false;

            foreach (var hit in hits)
            {
                if (hit.collider.isTrigger) continue;

                PinataHealth pinata = hit.collider.GetComponentInParent<PinataHealth>();
                if (pinata != null && !pinata.IsDead)
                {
                    Vector3 hitDir = _mainCamera.transform.forward;
                    pinata.TakeDamage(damage, hit.point, hitDir, impulseForce);
                    hitTarget = true;
                    break; // Hit main pinata
                }

                // If hit loose rigidbody, apply bat knockback
                Rigidbody otherRb = hit.collider.GetComponent<Rigidbody>();
                if (otherRb != null && !otherRb.isKinematic)
                {
                    otherRb.AddForceAtPosition(_mainCamera.transform.forward * impulseForce, hit.point, ForceMode.Impulse);
                }
            }
        }
    }
}
