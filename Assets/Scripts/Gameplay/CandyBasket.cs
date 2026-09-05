using System;
using System.Collections;
using Pinata.Candy;
using Pinata.Effects;
using Pinata.Player;
using UnityEngine;

namespace Pinata.Gameplay
{
    public class CandyBasket : MonoBehaviour
    {
        [Header("Basket Config")]
        [SerializeField] private CandyType candyType = CandyType.Blue;
        [SerializeField] private int targetCapacity = 10;
        [SerializeField] private int basePayoutPerItem = 4;
        [SerializeField] private float fullBonusMultiplier = 1.35f;

        [Header("Visual Elements")]
        [SerializeField] private Transform fillVisual;
        [SerializeField] private MeshRenderer basketRenderer;
        [SerializeField] private Color basketColor = Color.cyan;

        [Header("Audio & FX")]
        [SerializeField] private AudioClip depositSound;
        [SerializeField] private AudioClip sellSound;
        [SerializeField] private ParticleSystem sellConfetti;
        [SerializeField] private AudioSource audioSource;

        private int _currentCount = 0;
        private bool _isPlayerNear = false;
        private Vector3 _originalFillScale = Vector3.one;

        public CandyType Type => candyType;
        public int CurrentCount => _currentCount;
        public int TargetCapacity => targetCapacity;
        public bool IsFull => _currentCount >= targetCapacity;
        public bool HasItems => _currentCount > 0;
        public bool IsPlayerNear => _isPlayerNear;

        public event Action<CandyBasket> OnBasketUpdated;
        public event Action<CandyBasket, int> OnBasketSold;

        public int CurrentSaleValue
        {
            get
            {
                float mult = EconomyManager.Instance != null ? EconomyManager.Instance.CurrentPriceMultiplier : 1.0f;
                int baseVal = Mathf.RoundToInt(_currentCount * basePayoutPerItem * mult);
                if (IsFull)
                {
                    baseVal = Mathf.RoundToInt(baseVal * fullBonusMultiplier);
                }
                return Mathf.Max(baseVal, 1);
            }
        }

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            }

            if (fillVisual != null)
            {
                _originalFillScale = fillVisual.localScale;
                UpdateFillVisual(immediate: true);
            }
        }

        public void Initialize(CandyType type, Color color, int capacity = 10)
        {
            candyType = type;
            basketColor = color;
            targetCapacity = capacity;
            UpdateFillVisual(immediate: true);
        }

        public void SetupBasket(CandyType type, Color color, Transform fill, MeshRenderer renderer, ParticleSystem confetti, int capacity = 10, int payout = 5)
        {
            candyType = type;
            basketColor = color;
            fillVisual = fill;
            basketRenderer = renderer;
            sellConfetti = confetti;
            targetCapacity = capacity;
            basePayoutPerItem = payout;
            if (fillVisual != null)
            {
                _originalFillScale = fillVisual.localScale;
                UpdateFillVisual(immediate: true);
            }
        }

        public bool TryDepositFromInventory()
        {
            if (PlayerInventory.Instance == null || IsFull) return false;

            int carried = PlayerInventory.Instance.GetCount(candyType);
            if (carried <= 0) return false;

            int needed = targetCapacity - _currentCount;
            int toDeposit = Mathf.Min(needed, carried);

            int removed = PlayerInventory.Instance.RemoveCandies(candyType, toDeposit);
            if (removed > 0)
            {
                _currentCount += removed;
                UpdateFillVisual(immediate: false);

                if (depositSound != null && audioSource != null)
                {
                    audioSource.pitch = 0.95f + (_currentCount / (float)targetCapacity) * 0.3f;
                    audioSource.PlayOneShot(depositSound, 0.7f);
                }

                OnBasketUpdated?.Invoke(this);
                return true;
            }

            return false;
        }

        public bool TrySell()
        {
            if (_currentCount <= 0) return false;

            int payout = CurrentSaleValue;
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddCash(payout);
            }

            if (sellSound != null && audioSource != null)
            {
                audioSource.pitch = 1.0f;
                audioSource.PlayOneShot(sellSound, 0.9f);
            }

            if (sellConfetti != null)
            {
                sellConfetti.Play();
            }

            int soldCount = _currentCount;
            _currentCount = 0;
            UpdateFillVisual(immediate: false);

            OnBasketSold?.Invoke(this, payout);
            OnBasketUpdated?.Invoke(this);
            return true;
        }

        private void UpdateFillVisual(bool immediate)
        {
            if (fillVisual == null) return;

            float fillRatio = (float)_currentCount / Mathf.Max(targetCapacity, 1);
            Vector3 targetScale = new Vector3(_originalFillScale.x, _originalFillScale.y * fillRatio, _originalFillScale.z);

            if (immediate)
            {
                fillVisual.localScale = targetScale;
                fillVisual.gameObject.SetActive(_currentCount > 0);
            }
            else
            {
                fillVisual.gameObject.SetActive(true);
                StopAllCoroutines();
                StartCoroutine(AnimateFillScale(targetScale));
            }
        }

        private IEnumerator AnimateFillScale(Vector3 targetScale)
        {
            Vector3 startScale = fillVisual.localScale;
            float elapsed = 0f;
            float duration = 0.18f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = Mathf.Sin(t * Mathf.PI * 0.5f); // Ease out
                fillVisual.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }

            fillVisual.localScale = targetScale;
            if (_currentCount == 0)
            {
                fillVisual.gameObject.SetActive(false);
            }
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
            }
        }
    }
}
