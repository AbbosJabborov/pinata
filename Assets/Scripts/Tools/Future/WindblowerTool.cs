// PARKED FOR LATER: not part of the current 3-slot loadout (Hand / Broom / Bat).
// Broom already covers "push candy toward box" at melee range; this was meant as
// its ranged upgrade. Re-introduce once the tool roster expands past 3 and the
// loadout-selection screen exists. Not currently referenced by ToolManager.
using Pinata.Candy;
using Pinata.Tools;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pinata.Tools.Future
{
    /// <summary>
    /// Ranged directional push tool - blows candy toward the box in a cone,
    /// without picking it up. Sits between Broom (melee sweep) and Vacuum (suction + hopper).
    /// </summary>
    public class WindblowerTool : MonoBehaviour, ITool
    {
        [Header("Blow Settings")]
        [SerializeField] private float coneRange = 4.5f;
        [SerializeField] private float coneAngle = 30f; // half-angle in degrees
        [SerializeField] private float blowForce = 9f;
        [SerializeField] private float tickRate = 0.05f;

        [Header("Animation Settings")]
        [SerializeField] private Transform blowerModel;
        [SerializeField] private Vector3 idleLocalPos = new Vector3(0.32f, -0.3f, 0.55f);
        [SerializeField] private Vector3 idleLocalRot = new Vector3(15f, -12f, 8f);
        [SerializeField] private float kickbackAmount = 0.03f;

        [Header("Audio & FX")]
        [SerializeField] private AudioClip blowHumSound;
        [SerializeField] private ParticleSystem blowParticles;
        [SerializeField] private AudioSource loopAudioSource;

        private Camera _mainCamera;
        private bool _isBlowing;
        private float _nextTickTime;
        private readonly Collider[] _overlapBuffer = new Collider[40];

        public string ToolName => "Windblower";
        public int SlotIndex => 2;
        public bool IsBusy => false;

        public void OnEquip()
        {
            gameObject.SetActive(true);
            _isBlowing = false;
            if (blowerModel != null)
            {
                blowerModel.localPosition = idleLocalPos;
                blowerModel.localEulerAngles = idleLocalRot;
            }
        }

        public void OnUnequip()
        {
            StopBlowing();
            gameObject.SetActive(false);
        }

        private void Awake()
        {
            _mainCamera = Camera.main;
            if (loopAudioSource == null)
            {
                loopAudioSource = GetComponent<AudioSource>();
                if (loopAudioSource == null) loopAudioSource = gameObject.AddComponent<AudioSource>();
            }
            loopAudioSource.loop = true;
            loopAudioSource.playOnAwake = false;

            if (blowerModel != null)
            {
                blowerModel.localPosition = idleLocalPos;
                blowerModel.localEulerAngles = idleLocalRot;
            }
        }

        private void Update()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                if (!_isBlowing) StartBlowing();
                if (Time.time >= _nextTickTime)
                {
                    _nextTickTime = Time.time + tickRate;
                    BlowTick();
                }
            }
            else if (_isBlowing)
            {
                StopBlowing();
            }
        }

        private void StartBlowing()
        {
            _isBlowing = true;
            if (blowParticles != null) blowParticles.Play();
            if (loopAudioSource != null && blowHumSound != null)
            {
                loopAudioSource.clip = blowHumSound;
                loopAudioSource.volume = 0.6f;
                loopAudioSource.Play();
            }
        }

        private void StopBlowing()
        {
            _isBlowing = false;
            if (blowParticles != null) blowParticles.Stop();
            if (loopAudioSource != null) loopAudioSource.Stop();
        }

        private void BlowTick()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return;

            Vector3 origin = _mainCamera.transform.position;
            Vector3 forward = _mainCamera.transform.forward;

            int count = Physics.OverlapSphereNonAlloc(origin, coneRange, _overlapBuffer);
            for (int i = 0; i < count; i++)
            {
                var candy = _overlapBuffer[i].GetComponentInParent<CandyItem>();
                if (candy == null || candy.IsBeingCollected || candy.Rigidbody == null) continue;

                Vector3 toCandy = candy.transform.position - origin;
                float dist = toCandy.magnitude;
                if (dist < 0.05f) continue;

                float angle = Vector3.Angle(forward, toCandy);
                if (angle > coneAngle) continue;

                float distFalloff = 1f - Mathf.Clamp01(dist / coneRange);
                float angleFalloff = 1f - Mathf.Clamp01(angle / coneAngle);
                float strength = blowForce * distFalloff * angleFalloff;

                candy.Rigidbody.isKinematic = false;
                candy.Rigidbody.AddForce(forward * strength, ForceMode.Acceleration);
            }

            if (blowerModel != null)
            {
                blowerModel.localPosition = idleLocalPos + Random.insideUnitSphere * kickbackAmount;
            }
        }
    }
}
