using System.Collections;
using Pinata.Candy;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pinata.Tools
{
    public class BroomTool : MonoBehaviour, ITool
    {
        [Header("Broom Settings")]
        [SerializeField] private float sweepForce = 4.2f;
        [SerializeField] private float sweepReach = 2.6f;
        [SerializeField] private float sweepRadius = 1.5f;
        [SerializeField] private float sweepCooldown = 0.32f;

        [Header("Animation Settings")]
        [SerializeField] private Transform broomModel;
        [SerializeField] private Vector3 idleLocalPos = new Vector3(0.34f, -0.32f, 0.48f);
        [SerializeField] private Vector3 idleLocalRot = new Vector3(25f, 89f, 295f);

        [Header("Audio")]
        [SerializeField] private AudioClip sweepSound;
        [SerializeField] private AudioSource audioSource;

        private Camera _mainCamera;
        private bool _isSweeping = false;
        private float _lastSweepTime = -10f;
        private readonly Collider[] _overlapBuffer = new Collider[40];

        public string ToolName => "Broom";
        public int SlotIndex => 1;
        public bool IsBusy => _isSweeping;

        public void OnEquip()
        {
            gameObject.SetActive(true);
            _isSweeping = false;
            if (broomModel != null)
            {
                broomModel.localPosition = idleLocalPos;
                broomModel.localEulerAngles = idleLocalRot;
            }
        }

        public void OnUnequip()
        {
            StopAllCoroutines();
            _isSweeping = false;
            if (broomModel != null)
            {
                broomModel.localPosition = idleLocalPos;
                broomModel.localEulerAngles = idleLocalRot;
            }
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

            if (broomModel != null)
            {
                broomModel.localPosition = idleLocalPos;
                broomModel.localEulerAngles = idleLocalRot;
            }
        }

        private void Update()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            // Continuous sweeping when holding Left Click
            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                TrySweep();
            }
        }

        public void TrySweep()
        {
            if (Time.time < _lastSweepTime + sweepCooldown || _isSweeping) return;

            _lastSweepTime = Time.time;
            StartCoroutine(SweepRoutine());
        }

        private IEnumerator SweepRoutine()
        {
            _isSweeping = true;

            if (sweepSound != null && audioSource != null)
            {
                audioSource.pitch = Random.Range(0.92f, 1.08f);
                audioSource.PlayOneShot(sweepSound, 0.65f);
            }

            // Phase 1: Windup / Lift & Cock (lift bristles off floor and draw back)
            float windupTime = 0.07f;
            float elapsed = 0f;
            Vector3 windupPos = idleLocalPos + new Vector3(0.06f, 0.08f, -0.07f);
            Vector3 windupRot = idleLocalRot + new Vector3(-8f, 16f, -10f);

            while (elapsed < windupTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / windupTime);
                if (broomModel != null)
                {
                    broomModel.localPosition = Vector3.Lerp(idleLocalPos, windupPos, t);
                    broomModel.localRotation = Quaternion.Euler(Vector3.Lerp(idleLocalRot, windupRot, t));
                }
                yield return null;
            }

            // Phase 2: Downstroke & Sweeping Arc across floor
            float sweepTime = 0.16f;
            elapsed = 0f;
            Vector3 sweepPos = idleLocalPos + new Vector3(-0.46f, -0.06f, 0.14f);
            Vector3 sweepRot = idleLocalRot + new Vector3(12f, -34f, 22f);

            bool pushed = false;

            while (elapsed < sweepTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / sweepTime;
                // Curved sweeping acceleration
                float curvedT = Mathf.Sin(t * Mathf.PI * 0.5f);

                // Dip down in mid-stroke so bristles firmly drag across floor surface
                float dipY = Mathf.Sin(t * Mathf.PI) * 0.04f;
                Vector3 currentPos = Vector3.Lerp(windupPos, sweepPos, curvedT);
                currentPos.y -= dipY;

                if (broomModel != null)
                {
                    broomModel.localPosition = currentPos;
                    broomModel.localRotation = Quaternion.Euler(Vector3.Lerp(windupRot, sweepRot, curvedT));
                }

                // Apply physics push at peak ground contact
                if (!pushed && t >= 0.45f)
                {
                    pushed = true;
                    PerformSweepPhysics();
                }

                yield return null;
            }

            // Phase 3: Wrist Flick / Forward Gather (propels candies neatly together)
            float flickTime = 0.06f;
            elapsed = 0f;
            Vector3 flickPos = sweepPos + new Vector3(0.06f, 0.04f, 0.08f);
            Vector3 flickRot = sweepRot + new Vector3(6f, -8f, 8f);

            while (elapsed < flickTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / flickTime);
                if (broomModel != null)
                {
                    broomModel.localPosition = Vector3.Lerp(sweepPos, flickPos, t);
                    broomModel.localRotation = Quaternion.Euler(Vector3.Lerp(sweepRot, flickRot, t));
                }
                yield return null;
            }

            // Phase 4: Recovery Arc (lift off floor and return smoothly to idle stance)
            float recoveryTime = 0.15f;
            elapsed = 0f;
            while (elapsed < recoveryTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / recoveryTime;
                t = t * t * (3f - 2f * t); // Smoothstep
                if (broomModel != null)
                {
                    broomModel.localPosition = Vector3.Lerp(flickPos, idleLocalPos, t);
                    broomModel.localRotation = Quaternion.Euler(Vector3.Lerp(flickRot, idleLocalRot, t));
                }
                yield return null;
            }

            if (broomModel != null)
            {
                broomModel.localPosition = idleLocalPos;
                broomModel.localRotation = Quaternion.Euler(idleLocalRot);
            }

            _isSweeping = false;
        }

        private void PerformSweepPhysics()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return;

            // Target sweep zone on the floor in front of player
            Vector3 forward = _mainCamera.transform.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 sweepCenter = transform.position + forward * sweepReach;
            sweepCenter.y = 0.25f;

            int count = Physics.OverlapSphereNonAlloc(sweepCenter, sweepRadius, _overlapBuffer);
            for (int i = 0; i < count; i++)
            {
                var col = _overlapBuffer[i];
                if (col == null) continue;

                var candy = col.GetComponentInParent<CandyItem>();
                if (candy != null && candy.Rigidbody != null)
                {
                    var rb = candy.Rigidbody;

                    // Compute push direction: forward and inwards towards player's focal center line
                    Vector3 toCandy = rb.position - sweepCenter;
                    toCandy.y = 0;

                    Vector3 pushDir = forward * 0.85f + (forward - toCandy * 0.35f).normalized * 0.35f;
                    pushDir.y = 0.08f; // Minimal vertical hop so it glides along floor rather than flying
                    pushDir.Normalize();

                    // Apply impulse with lateral damping for clean controlled gathering
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x * 0.3f, rb.linearVelocity.y * 0.5f, rb.linearVelocity.z * 0.3f);
                    rb.AddForce(pushDir * sweepForce, ForceMode.Impulse);
                    rb.AddTorque(Random.insideUnitSphere * 2.5f, ForceMode.Impulse);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_mainCamera != null)
            {
                Vector3 forward = _mainCamera.transform.forward;
                forward.y = 0f;
                forward.Normalize();
                Vector3 sweepCenter = transform.position + forward * sweepReach;
                sweepCenter.y = 0.3f;

                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(sweepCenter, sweepRadius);
            }
        }
    }
}
