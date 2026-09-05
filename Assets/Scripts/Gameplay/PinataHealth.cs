using System;
using System.Collections;
using Pinata.Candy;
using Pinata.Effects;
using UnityEngine;

namespace Pinata.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    public class PinataHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;

        [Header("Drop Settings")]
        [SerializeField] private int minCandyCount = 15;
        [SerializeField] private int maxCandyCount = 25;
        [SerializeField] private float explosionForce = 5.5f;
        [SerializeField] private float explosionRadius = 2.5f;

        [Header("Juice & Feedback")]
        [SerializeField] private ParticleSystem hitConfettiPrefab;
        [SerializeField] private ParticleSystem deathExplosionPrefab;
        [SerializeField] private Renderer[] targetRenderers;
        [SerializeField] private Color hitFlashColor = Color.white;

        [Header("Rope & Ragdoll")]
        [SerializeField] private Transform attachmentPoint;
        [SerializeField] private Rigidbody[] ragdollLimbs;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private AudioClip breakSound;

        private Rigidbody _rb;
        private bool _isDead = false;
        private AudioSource _audioSource;
        private Color[] _originalColors;
        private MeshDeformer[] _deformers;

        public event Action<float, float> OnHealthChanged; // current, max
        public event Action OnDestroyed;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => _isDead;
        public Transform AttachmentPoint => attachmentPoint != null ? attachmentPoint : transform;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();

            if (targetRenderers == null || targetRenderers.Length == 0)
            {
                targetRenderers = GetComponentsInChildren<Renderer>();
            }

            _deformers = GetComponentsInChildren<MeshDeformer>();

            CacheOriginalColors();
            currentHealth = maxHealth;
        }

        private void CacheOriginalColors()
        {
            if (targetRenderers == null) return;
            _originalColors = new Color[targetRenderers.Length];
            for (int i = 0; i < targetRenderers.Length; i++)
            {
                if (targetRenderers[i] != null && targetRenderers[i].material != null)
                {
                    _originalColors[i] = targetRenderers[i].material.color;
                }
            }
        }

        public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection, float impulseForce = 8f)
        {
            if (_isDead) return;

            currentHealth = Mathf.Max(0, currentHealth - damage);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            // Apply physical impulse so the pinata swings
            if (_rb != null)
            {
                _rb.AddForceAtPosition(hitDirection * impulseForce, hitPoint, ForceMode.Impulse);
            }

            // Mesh Deformation based on strike force and impact location
            if (_deformers != null && _deformers.Length > 0)
            {
                for (int i = 0; i < _deformers.Length; i++)
                {
                    if (_deformers[i] != null && _deformers[i].gameObject.activeInHierarchy)
                    {
                        _deformers[i].Deform(hitPoint, hitDirection * impulseForce, radius: 0.65f, forceScale: 0.035f);
                    }
                }
            }

            // Juice: Camera shake & Hitstop
            if (JuiceEffects.Instance != null)
            {
                JuiceEffects.Instance.DoHitstop(0.04f);
                JuiceEffects.Instance.ShakeCamera(0.12f, 0.08f);
            }

            // Particle confetti
            if (hitConfettiPrefab != null)
            {
                ParticleSystem ps = Instantiate(hitConfettiPrefab, hitPoint, Quaternion.LookRotation(hitDirection));
                Destroy(ps.gameObject, 1.5f);
            }

            // Play audio
            if (hitSound != null && _audioSource != null)
            {
                _audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.15f);
                _audioSource.PlayOneShot(hitSound, 0.8f);
            }

            // Visual hit flash
            StartCoroutine(FlashRoutine());

            // Check destruction
            if (currentHealth <= 0f)
            {
                Explode();
            }
        }

        private IEnumerator FlashRoutine()
        {
            if (targetRenderers == null) yield break;

            for (int i = 0; i < targetRenderers.Length; i++)
            {
                if (targetRenderers[i] != null && targetRenderers[i].material != null)
                {
                    targetRenderers[i].material.color = hitFlashColor;
                }
            }

            yield return new WaitForSeconds(0.06f);

            for (int i = 0; i < targetRenderers.Length; i++)
            {
                if (targetRenderers[i] != null && targetRenderers[i].material != null && _originalColors != null && i < _originalColors.Length)
                {
                    targetRenderers[i].material.color = _originalColors[i];
                }
            }
        }

        private void Explode()
        {
            _isDead = true;

            // Extra juicy hitstop & shake for destruction
            if (JuiceEffects.Instance != null)
            {
                JuiceEffects.Instance.DoHitstop(0.08f);
                JuiceEffects.Instance.ShakeCamera(0.3f, 0.25f);
            }

            // Break sound
            if (breakSound != null && _audioSource != null)
            {
                _audioSource.pitch = 1.0f;
                _audioSource.PlayOneShot(breakSound, 1.0f);
            }

            // Death explosion particles
            if (deathExplosionPrefab != null)
            {
                ParticleSystem ps = Instantiate(deathExplosionPrefab, transform.position, Quaternion.identity);
                Destroy(ps.gameObject, 2.5f);
            }

            // Slice body mesh in half with real-time mesh cutting!
            SliceBodyOnDeath();

            // Fling ragdoll limbs as physical cardboard gibs!
            DetachRagdollGibs();

            // Erupt candies!
            SpawnEruptedCandies();

            OnDestroyed?.Invoke();

            // Disable all child renderers & colliders immediately so NOTHING hovers in the air
            foreach (var rend in GetComponentsInChildren<Renderer>())
            {
                if (rend != null) rend.enabled = false;
            }
            foreach (var col in GetComponentsInChildren<Collider>())
            {
                if (col != null) col.enabled = false;
            }

            Destroy(gameObject, 0.05f);
        }

        private void SliceBodyOnDeath()
        {
            MeshFilter mf = GetComponentInChildren<MeshFilter>();
            if (mf != null)
            {
                var result = MeshCutter.Cut(mf.gameObject, mf.transform.position, Vector3.right);
                if (result.success)
                {
                    if (result.pieceA != null)
                    {
                        var rb = result.pieceA.GetComponent<Rigidbody>();
                        if (rb != null) rb.AddExplosionForce(explosionForce * 2.5f, transform.position, explosionRadius, 0.5f, ForceMode.Impulse);
                    }
                    if (result.pieceB != null)
                    {
                        var rb = result.pieceB.GetComponent<Rigidbody>();
                        if (rb != null) rb.AddExplosionForce(explosionForce * 2.5f, transform.position, explosionRadius, 0.5f, ForceMode.Impulse);
                    }
                }
            }
        }

        private void DetachRagdollGibs()
        {
            if (ragdollLimbs == null) return;

            foreach (var limb in ragdollLimbs)
            {
                if (limb == null) continue;

                limb.transform.SetParent(null);
                var joint = limb.GetComponent<Joint>();
                if (joint != null) Destroy(joint);

                limb.isKinematic = false;
                limb.useGravity = true;
                limb.AddExplosionForce(explosionForce * 2.0f, transform.position, explosionRadius, 0.4f, ForceMode.Impulse);
                limb.AddTorque(UnityEngine.Random.insideUnitSphere * 15f, ForceMode.Impulse);

                StartCoroutine(ShrinkAndDestroyGib(limb.gameObject, 3.5f));
            }
        }

        private IEnumerator ShrinkAndDestroyGib(GameObject gib, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (gib == null) yield break;

            Vector3 startScale = gib.transform.localScale;
            float elapsed = 0f;
            float duration = 0.35f;

            while (elapsed < duration)
            {
                if (gib == null) yield break;
                elapsed += Time.deltaTime;
                gib.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / duration);
                yield return null;
            }

            if (gib != null) Destroy(gib);
        }

        private static readonly CandyType[] AvailableCandyTypes = new[]
        {
            CandyType.Blue,
            CandyType.Green,
            CandyType.Orange,
            CandyType.Pink,
            CandyType.Purple,
            CandyType.Yellow
        };

        private void SpawnEruptedCandies()
        {
            int candyCount = UnityEngine.Random.Range(minCandyCount, maxCandyCount + 1);
            Vector3 center = transform.position;

            for (int i = 0; i < candyCount; i++)
            {
                CandyType type = AvailableCandyTypes[UnityEngine.Random.Range(0, AvailableCandyTypes.Length)];

                Vector3 spawnPos = center + UnityEngine.Random.insideUnitSphere * 0.4f;
                Quaternion rot = UnityEngine.Random.rotation;

                CandyItem candy = null;
                if (CandyPool.Instance != null)
                {
                    candy = CandyPool.Instance.Spawn(type, spawnPos, rot);
                }

                if (candy != null && candy.Rigidbody != null)
                {
                    candy.Rigidbody.AddExplosionForce(explosionForce, center, explosionRadius, 0.4f, ForceMode.Impulse);
                }
            }
        }

        public void ResetPinata()
        {
            _isDead = false;
            currentHealth = maxHealth;
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }

            if (_deformers != null)
            {
                for (int i = 0; i < _deformers.Length; i++)
                {
                    if (_deformers[i] != null) _deformers[i].ResetDeformation();
                }
            }

            gameObject.SetActive(true);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }
}
