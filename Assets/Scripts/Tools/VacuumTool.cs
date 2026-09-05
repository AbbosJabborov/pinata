using System.Collections;
using Pinata.Candy;
using Pinata.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pinata.Tools
{
    public class VacuumTool : MonoBehaviour, ITool
    {
        [Header("Suction Stats")]
        [SerializeField] private float reachDistance = 4.2f;
        [SerializeField] private float suctionRadius = 2.2f;
        [SerializeField] private float suctionForce = 16f;
        [SerializeField] private float captureDistance = 0.48f;

        [Header("Animation Settings")]
        [SerializeField] private Transform vacuumModel;
        [SerializeField] private Transform nozzleTip;
        [SerializeField] private Vector3 idleLocalPos = new Vector3(0.34f, -0.36f, 0.62f);
        [SerializeField] private Vector3 idleLocalRot = new Vector3(25f, -20f, 15f);

        [Header("Audio & FX")]
        [SerializeField] private AudioClip vacuumHumSound;
        [SerializeField] private AudioClip slurpSound;
        [SerializeField] private ParticleSystem suctionParticles;
        [SerializeField] private AudioSource loopAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        private Camera _mainCamera;
        private bool _isSuctionActive = false;
        private readonly Collider[] _overlapBuffer = new Collider[60];

        public string ToolName => "Vacuum";
        public int SlotIndex => 4;
        public bool IsBusy => false;

        public float ReachDistance { get => reachDistance; set => reachDistance = value; }
        public float SuctionForce { get => suctionForce; set => suctionForce = value; }
        public Transform VacuumModel { get => vacuumModel; set => vacuumModel = value; }
        public Transform NozzleTip { get => nozzleTip; set => nozzleTip = value; }
        public ParticleSystem SuctionParticles { get => suctionParticles; set => suctionParticles = value; }

        public void OnEquip()
        {
            gameObject.SetActive(true);
            _isSuctionActive = false;
            if (vacuumModel != null)
            {
                vacuumModel.localPosition = idleLocalPos;
                vacuumModel.localEulerAngles = idleLocalRot;
            }
            StopSuction();
        }

        public void OnUnequip()
        {
            StopSuction();
            if (vacuumModel != null)
            {
                vacuumModel.localPosition = idleLocalPos;
                vacuumModel.localEulerAngles = idleLocalRot;
            }
            gameObject.SetActive(false);
        }

        private void Awake()
        {
            _mainCamera = Camera.main;

            if (loopAudioSource == null)
            {
                var sources = GetComponents<AudioSource>();
                if (sources.Length > 0) loopAudioSource = sources[0];
                else loopAudioSource = gameObject.AddComponent<AudioSource>();
            }
            loopAudioSource.loop = true;
            loopAudioSource.playOnAwake = false;

            if (sfxAudioSource == null)
            {
                var sources = GetComponents<AudioSource>();
                if (sources.Length > 1) sfxAudioSource = sources[1];
                else sfxAudioSource = gameObject.AddComponent<AudioSource>();
            }

            if (vacuumModel != null)
            {
                vacuumModel.localPosition = idleLocalPos;
                vacuumModel.localEulerAngles = idleLocalRot;
            }
        }

        private void Update()
        {
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                if (_isSuctionActive) StopSuction();
                return;
            }

            bool wantsSuction = Mouse.current != null && Mouse.current.leftButton.isPressed;

            if (wantsSuction)
            {
                if (!_isSuctionActive) StartSuction();
                PerformSuctionPhysics();

                // Subtle nozzle vibration while sucking
                if (vacuumModel != null)
                {
                    float shakeX = (Mathf.PerlinNoise(Time.time * 30f, 0f) - 0.5f) * 0.008f;
                    float shakeY = (Mathf.PerlinNoise(0f, Time.time * 30f) - 0.5f) * 0.008f;
                    vacuumModel.localPosition = idleLocalPos + new Vector3(shakeX, shakeY, -0.015f);
                }
            }
            else
            {
                if (_isSuctionActive) StopSuction();

                if (vacuumModel != null)
                {
                    vacuumModel.localPosition = Vector3.Lerp(vacuumModel.localPosition, idleLocalPos, Time.deltaTime * 10f);
                    vacuumModel.localRotation = Quaternion.Slerp(vacuumModel.localRotation, Quaternion.Euler(idleLocalRot), Time.deltaTime * 10f);
                }
            }
        }

        private void StartSuction()
        {
            _isSuctionActive = true;

            if (loopAudioSource != null && vacuumHumSound != null)
            {
                loopAudioSource.clip = vacuumHumSound;
                loopAudioSource.volume = 0.55f;
                loopAudioSource.pitch = 1.0f;
                loopAudioSource.Play();
            }

            if (suctionParticles != null)
            {
                suctionParticles.Play();
            }
        }

        private void StopSuction()
        {
            _isSuctionActive = false;

            if (loopAudioSource != null && loopAudioSource.isPlaying)
            {
                loopAudioSource.Stop();
            }

            if (suctionParticles != null && suctionParticles.isPlaying)
            {
                suctionParticles.Stop();
            }
        }

        private void PerformSuctionPhysics()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return;

            bool isFull = PlayerInventory.Instance != null && PlayerInventory.Instance.IsFull;
            if (isFull)
            {
                // Can't suck more if backpack is full
                return;
            }

            Vector3 suctionOrigin = nozzleTip != null ? nozzleTip.position : _mainCamera.transform.position + _mainCamera.transform.forward * 0.8f;
            Vector3 forward = _mainCamera.transform.forward;
            Vector3 searchCenter = suctionOrigin + forward * (reachDistance * 0.5f);

            int count = Physics.OverlapSphereNonAlloc(searchCenter, reachDistance * 0.65f, _overlapBuffer);
            for (int i = 0; i < count; i++)
            {
                var col = _overlapBuffer[i];
                if (col == null) continue;

                var candy = col.GetComponentInParent<CandyItem>();
                if (candy == null || candy.IsBeingCollected || candy.Rigidbody == null) continue;

                Vector3 toCandy = candy.transform.position - suctionOrigin;
                float dist = toCandy.magnitude;

                // Check angle within forward cone
                float angle = Vector3.Angle(forward, toCandy);
                if (angle > 55f && dist > 1.2f) continue;

                if (dist <= captureDistance)
                {
                    // Instant capture into backpack!
                    CaptureCandy(candy);
                }
                else
                {
                    // Pull candy toward nozzle tip with centripetal force
                    Vector3 pullDir = -toCandy.normalized;
                    float forceStrength = suctionForce * Mathf.Clamp01(1.0f - (dist / reachDistance)) + 4.0f;

                    var rb = candy.Rigidbody;
                    rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, pullDir * forceStrength, Time.deltaTime * 6f);
                    rb.AddForce(pullDir * forceStrength, ForceMode.Acceleration);

                    // Add subtle swirl
                    Vector3 swirl = Vector3.Cross(pullDir, Vector3.up).normalized * 1.5f;
                    rb.AddForce(swirl, ForceMode.Acceleration);
                }
            }
        }

        private void CaptureCandy(CandyItem candy)
        {
            if (candy == null || candy.IsBeingCollected) return;

            bool added = PlayerInventory.Instance != null && PlayerInventory.Instance.TryAddCandy(candy);
            if (!added) return;

            if (sfxAudioSource != null && slurpSound != null)
            {
                sfxAudioSource.pitch = Random.Range(1.05f, 1.25f);
                sfxAudioSource.PlayOneShot(slurpSound, 0.7f);
            }

            candy.Collect((collected) =>
            {
                if (CandyPool.Instance != null)
                {
                    CandyPool.Instance.Despawn(collected);
                }
                else
                {
                    Destroy(collected.gameObject);
                }
            });
        }

        private void OnDrawGizmosSelected()
        {
            if (_mainCamera != null)
            {
                Gizmos.color = Color.cyan;
                Vector3 origin = nozzleTip != null ? nozzleTip.position : _mainCamera.transform.position;
                Gizmos.DrawWireSphere(origin + _mainCamera.transform.forward * (reachDistance * 0.5f), reachDistance * 0.5f);
            }
        }
    }
}
