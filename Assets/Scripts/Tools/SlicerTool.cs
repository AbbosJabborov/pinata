using System.Collections;
using Pinata.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pinata.Tools
{
    public class SlicerTool : MonoBehaviour, ITool
    {
        [Header("Combat Stats")]
        [SerializeField] private float damage = 50f;
        [SerializeField] private float reachDistance = 2.8f;
        [SerializeField] private float slashCooldown = 0.35f;

        [Header("Animation Settings")]
        [SerializeField] private Transform bladeModel;
        [SerializeField] private Vector3 idleLocalPos = new Vector3(0.35f, -0.35f, 0.60f);
        [SerializeField] private Vector3 idleLocalRot = new Vector3(35f, -30f, 45f);

        [Header("Audio")]
        [SerializeField] private AudioClip sliceSound;
        [SerializeField] private AudioSource audioSource;

        private Camera _mainCamera;
        private bool _isSlashing = false;
        private float _lastSlashTime = -10f;

        public string ToolName => "Slicer";
        public int SlotIndex => 3;
        public bool IsBusy => _isSlashing;

        public void OnEquip()
        {
            gameObject.SetActive(true);
            _isSlashing = false;
            if (bladeModel != null)
            {
                bladeModel.localPosition = idleLocalPos;
                bladeModel.localEulerAngles = idleLocalRot;
            }
        }

        public void OnUnequip()
        {
            StopAllCoroutines();
            _isSlashing = false;
            if (bladeModel != null)
            {
                bladeModel.localPosition = idleLocalPos;
                bladeModel.localEulerAngles = idleLocalRot;
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

            if (bladeModel != null)
            {
                bladeModel.localPosition = idleLocalPos;
                bladeModel.localEulerAngles = idleLocalRot;
            }
        }

        private void Update()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                TrySlash();
            }
        }

        public void TrySlash()
        {
            if (Time.time < _lastSlashTime + slashCooldown || _isSlashing) return;

            _lastSlashTime = Time.time;
            StartCoroutine(SlashRoutine());
        }

        private IEnumerator SlashRoutine()
        {
            _isSlashing = true;

            if (sliceSound != null && audioSource != null)
            {
                audioSource.pitch = Random.Range(1.05f, 1.25f);
                audioSource.PlayOneShot(sliceSound, 0.7f);
            }

            // Phase 1: Rapid Windup (pull up-right)
            float windupTime = 0.06f;
            float elapsed = 0f;
            Vector3 windupPos = idleLocalPos + new Vector3(0.12f, 0.15f, -0.1f);
            Vector3 windupRot = idleLocalRot + new Vector3(-30f, 40f, -20f);

            while (elapsed < windupTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / windupTime;
                if (bladeModel != null)
                {
                    bladeModel.localPosition = Vector3.Lerp(idleLocalPos, windupPos, t);
                    bladeModel.localRotation = Quaternion.Euler(Vector3.Lerp(idleLocalRot, windupRot, t));
                }
                yield return null;
            }

            // Perform Cut Detection at peak of the swing
            PerformSliceDetection();

            // Phase 2: Diagonal Slash across screen
            float slashTime = 0.10f;
            elapsed = 0f;
            Vector3 slashPos = idleLocalPos + new Vector3(-0.55f, -0.25f, 0.2f);
            Vector3 slashRot = idleLocalRot + new Vector3(50f, -70f, -60f);

            while (elapsed < slashTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / slashTime;
                if (bladeModel != null)
                {
                    bladeModel.localPosition = Vector3.Lerp(windupPos, slashPos, Mathf.SmoothStep(0, 1, t));
                    bladeModel.localRotation = Quaternion.Euler(Vector3.Lerp(windupRot, slashRot, Mathf.SmoothStep(0, 1, t)));
                }
                yield return null;
            }

            // Phase 3: Recovery
            float recoveryTime = 0.14f;
            elapsed = 0f;
            while (elapsed < recoveryTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / recoveryTime;
                if (bladeModel != null)
                {
                    bladeModel.localPosition = Vector3.Lerp(slashPos, idleLocalPos, t);
                    bladeModel.localRotation = Quaternion.Euler(Vector3.Lerp(slashRot, idleLocalRot, t));
                }
                yield return null;
            }

            if (bladeModel != null)
            {
                bladeModel.localPosition = idleLocalPos;
                bladeModel.localEulerAngles = idleLocalRot;
            }

            _isSlashing = false;
        }

        private void PerformSliceDetection()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return;

            Ray ray = new Ray(_mainCamera.transform.position, _mainCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, reachDistance))
            {
                // Determine slice plane: perpendicular to blade sweep
                Vector3 slashDir = (_mainCamera.transform.right - _mainCamera.transform.up).normalized;
                Vector3 planeNormal = Vector3.Cross(_mainCamera.transform.forward, slashDir).normalized;

                // Check for Pinata
                PinataHealth pinata = hit.collider.GetComponentInParent<PinataHealth>();
                if (pinata != null)
                {
                    pinata.TakeDamage(damage, hit.point, _mainCamera.transform.forward, 15f);

                    // If hit the deformable mesh or compound collider, slice it!
                    MeshFilter mf = hit.collider.GetComponent<MeshFilter>();
                    if (mf == null) mf = pinata.GetComponentInChildren<MeshFilter>();
                    if (mf != null)
                    {
                        MeshCutter.Cut(mf.gameObject, hit.point, planeNormal);
                    }
                    return;
                }

                // Check for general sliceable mesh (e.g. candy or props)
                MeshFilter targetMf = hit.collider.GetComponent<MeshFilter>();
                if (targetMf != null && targetMf.sharedMesh != null)
                {
                    MeshCutter.Cut(hit.collider.gameObject, hit.point, planeNormal);
                }
            }
        }
    }
}
