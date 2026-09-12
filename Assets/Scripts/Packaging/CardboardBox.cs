using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Pinata.Candy;
using Pinata.Effects;
using Pinata.Interaction;
using Pinata.Player;

namespace Pinata.Packaging
{
    /// <summary>
    /// Represents a cardboard box capable of opening/closing its flaps,
    /// accepting candies up to a capacity, and cartoonishly bouncing & scaling on upgrade.
    /// Can automatically generate procedural visual flaps or bind to custom model transforms.
    /// Implements IInteractable to display prompt UI and support player deposit interactions.
    /// </summary>
    public class CardboardBox : MonoBehaviour, IInteractable
    {
        public static CardboardBox Instance { get; private set; }

        [Header("Flap Pivots")]
        [Tooltip("Transform pivot for the Left flap (hinged along Left edge).")]
        [SerializeField] private Transform flapLeft;
        [Tooltip("Transform pivot for the Right flap (hinged along Right edge).")]
        [SerializeField] private Transform flapRight;
        [Tooltip("Transform pivot for the Front flap (hinged along Front edge).")]
        [SerializeField] private Transform flapFront;
        [Tooltip("Transform pivot for the Back flap (hinged along Back edge).")]
        [SerializeField] private Transform flapBack;

        [Header("Flap Angles (Degrees around hinge axis)")]
        [SerializeField] private float openAngle = 110f;
        [SerializeField] private float closedAngle = 0f;
        [SerializeField] private float flapFoldDuration = 0.35f;

        [Header("Capacity & Stats")]
        [SerializeField] private int baseCapacity = 15;
        [SerializeField] private int capacityPerLevel = 10;
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private int currentFill = 0;
        [SerializeField] private bool isClosed = false;

        [Header("Visual Body Root")]
        [Tooltip("Root transform of the box body to animate for squash/stretch without offsetting world placement.")]
        [SerializeField] private Transform visualRoot;

        [Header("Candy Trigger")]
        [SerializeField] private BoxCollider intakeTrigger;

        [Header("Sound / Juice")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip fillClip;
        [SerializeField] private AudioClip closeClip;
        [SerializeField] private AudioClip upgradeClip;

        // Events
        public event Action<int, int> OnFillChanged; // current, max
        public event Action OnBoxFull;
        public event Action<int> OnLevelUpgraded; // new level

        public int CurrentLevel => currentLevel;
        public int CurrentFill => currentFill;
        public int Capacity => baseCapacity + (currentLevel - 1) * capacityPerLevel;
        public bool IsClosed => isClosed;

        private Sequence flapSequence;
        private Sequence upgradeSequence;
        private Vector3 initialVisualScale = Vector3.one;
        private Vector3 initialVisualLocalPos = Vector3.zero;

        private void Awake()
        {
            if (Instance == null) Instance = this;

            if (visualRoot == null)
            {
                visualRoot = transform.Find("VisualRoot");
                if (visualRoot == null) visualRoot = transform;
            }

            initialVisualScale = visualRoot.localScale;
            initialVisualLocalPos = visualRoot.localPosition;

            SetupIntakeTrigger();
        }

        private void Start()
        {
            OpenFlaps(instant: true);
        }

        /// <summary>
        /// Ensures there's a trigger collider to catch candies dropped inside the box.
        /// </summary>
        private void SetupIntakeTrigger()
        {
            if (intakeTrigger == null)
            {
                intakeTrigger = GetComponent<BoxCollider>();
                if (intakeTrigger == null)
                {
                    intakeTrigger = gameObject.AddComponent<BoxCollider>();
                    intakeTrigger.isTrigger = true;
                    intakeTrigger.size = new Vector3(1.2f, 1.0f, 1.2f);
                    intakeTrigger.center = new Vector3(0f, 0.5f, 0f);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isClosed) return;

            CandyItem candy = other.GetComponent<CandyItem>();
            if (candy == null)
            {
                candy = other.GetComponentInParent<CandyItem>();
            }

            if (candy != null && !candy.IsBeingCollected)
            {
                AddCandy(candy);
            }
        }

        /// <summary>
        /// Adds a candy into the box.
        /// </summary>
        public bool AddCandy(CandyItem candy)
        {
            if (isClosed || currentFill >= Capacity)
                return false;

            currentFill++;
            OnFillChanged?.Invoke(currentFill, Capacity);

            // Subtle bump punch animation on fill
            if (visualRoot != null)
            {
                visualRoot.DOKill(complete: true);
                visualRoot.DOPunchScale(new Vector3(0.06f, -0.06f, 0.06f), 0.18f, 7, 0.5f);
            }

            // Despawn or hide the physical candy via pool
            if (candy != null)
            {
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

            if (audioSource != null && fillClip != null)
            {
                audioSource.pitch = UnityEngine.Random.Range(0.95f, 1.15f);
                audioSource.PlayOneShot(fillClip);
            }

            if (currentFill >= Capacity)
            {
                CloseFlaps();
                OnBoxFull?.Invoke();
            }

            return true;
        }

        /// <summary>
        /// Opens flaps smoothly or instantly.
        /// </summary>
        public void OpenFlaps(bool instant = false)
        {
            isClosed = false;
            flapSequence?.Kill();

            if (instant)
            {
                SetFlapRotation(flapLeft, -openAngle, Vector3.forward);
                SetFlapRotation(flapRight, openAngle, Vector3.forward);
                SetFlapRotation(flapFront, openAngle, Vector3.right);
                SetFlapRotation(flapBack, -openAngle, Vector3.right);
                return;
            }

            flapSequence = DOTween.Sequence();
            // Front & Back open first
            flapSequence.Append(RotateFlap(flapFront, openAngle, Vector3.right, flapFoldDuration * 0.75f));
            flapSequence.Join(RotateFlap(flapBack, -openAngle, Vector3.right, flapFoldDuration * 0.75f));
            // Then Left & Right open outward
            flapSequence.Append(RotateFlap(flapLeft, -openAngle, Vector3.forward, flapFoldDuration * 0.75f));
            flapSequence.Join(RotateFlap(flapRight, openAngle, Vector3.forward, flapFoldDuration * 0.75f));
        }

        /// <summary>
        /// Sequenced closing of flaps: Left and Right fold inward first, then Front and Back fold over them.
        /// </summary>
        public void CloseFlaps(Action onComplete = null)
        {
            isClosed = true;
            flapSequence?.Kill();

            flapSequence = DOTween.Sequence();

            // 1. Fold Left and Right flaps first
            flapSequence.Append(RotateFlap(flapLeft, closedAngle, Vector3.forward, flapFoldDuration));
            flapSequence.Join(RotateFlap(flapRight, closedAngle, Vector3.forward, flapFoldDuration));

            // 2. Fold Front and Back flaps next with slight overlap & bounce
            flapSequence.Append(RotateFlap(flapFront, closedAngle, Vector3.right, flapFoldDuration * 1.1f).SetEase(Ease.OutBounce));
            flapSequence.Join(RotateFlap(flapBack, closedAngle, Vector3.right, flapFoldDuration * 1.1f).SetEase(Ease.OutBounce));

            // Sound and completion
            flapSequence.OnComplete(() =>
            {
                if (audioSource != null && closeClip != null)
                {
                    audioSource.pitch = 1.0f;
                    audioSource.PlayOneShot(closeClip);
                }
                onComplete?.Invoke();
            });
        }

        private Tween RotateFlap(Transform flap, float angle, Vector3 axis, float duration)
        {
            if (flap == null) return null;
            Vector3 targetEuler = axis * angle;
            return flap.DOLocalRotate(targetEuler, duration).SetEase(Ease.InOutSine);
        }

        private void SetFlapRotation(Transform flap, float angle, Vector3 axis)
        {
            if (flap != null)
            {
                flap.localEulerAngles = axis * angle;
            }
        }

        /// <summary>
        /// Plays a cartoonish bouncy scale upgrade:
        /// 1. Box anticipates / squashes down.
        /// 2. Hops / hovers off the floor while stretching upward.
        /// 3. Pops into its new larger size with elastic overshoot.
        /// 4. Slams back down to rest.
        /// </summary>
        public void UpgradeLevel(int newLevel = -1)
        {
            if (newLevel <= 0)
                currentLevel++;
            else
                currentLevel = newLevel;

            // Scale factor grows by ~20% per level up to max visual ratio
            float scaleMultiplier = 1f + (currentLevel - 1) * 0.22f;
            Vector3 targetScale = initialVisualScale * scaleMultiplier;

            upgradeSequence?.Kill();
            visualRoot.DOKill(complete: true);

            upgradeSequence = DOTween.Sequence();

            // Step 1: Anticipation Squash
            upgradeSequence.Append(visualRoot.DOScale(new Vector3(targetScale.x * 1.2f, targetScale.y * 0.7f, targetScale.z * 1.2f), 0.15f).SetEase(Ease.InQuad));

            // Step 2: Launch / Hop upward & stretch tall
            upgradeSequence.Append(visualRoot.DOLocalMoveY(initialVisualLocalPos.y + 0.45f, 0.25f).SetEase(Ease.OutQuad));
            upgradeSequence.Join(visualRoot.DOScale(new Vector3(targetScale.x * 0.85f, targetScale.y * 1.35f, targetScale.z * 0.85f), 0.25f).SetEase(Ease.OutQuad));

            // Step 3: Pop at peak into new target scale with cartoon overshoot
            upgradeSequence.Append(visualRoot.DOScale(targetScale * 1.15f, 0.15f).SetEase(Ease.OutBack));

            // Step 4: Land with squash & elastic settle
            upgradeSequence.Append(visualRoot.DOLocalMoveY(initialVisualLocalPos.y, 0.2f).SetEase(Ease.InQuad));
            upgradeSequence.Append(visualRoot.DOScale(new Vector3(targetScale.x * 1.2f, targetScale.y * 0.8f, targetScale.z * 1.2f), 0.12f));
            upgradeSequence.Append(visualRoot.DOScale(targetScale, 0.25f).SetEase(Ease.OutElastic));

            // Juice Effects / Audio
            upgradeSequence.OnComplete(() =>
            {
                OnLevelUpgraded?.Invoke(currentLevel);
                if (audioSource != null && upgradeClip != null)
                {
                    audioSource.pitch = 1.0f + (currentLevel * 0.05f);
                    audioSource.PlayOneShot(upgradeClip);
                }
            });
        }

        /// <summary>
        /// Resets the box to empty state and reopens flaps.
        /// </summary>
        public void ResetBox()
        {
            currentFill = 0;
            isClosed = false;
            OnFillChanged?.Invoke(currentFill, Capacity);
            OpenFlaps();
        }

        /// <summary>
        /// Deposits candies directly from player backpack into the box.
        /// </summary>
        public bool TryDepositFromInventory()
        {
            if (isClosed || currentFill >= Capacity || PlayerInventory.Instance == null)
                return false;

            int needed = Capacity - currentFill;
            int totalDeposited = 0;

            foreach (CandyType type in (CandyType[])Enum.GetValues(typeof(CandyType)))
            {
                if (needed <= 0) break;
                int carried = PlayerInventory.Instance.GetCount(type);
                if (carried > 0)
                {
                    int take = Mathf.Min(needed, carried);
                    int removed = PlayerInventory.Instance.RemoveCandies(type, take);
                    if (removed > 0)
                    {
                        totalDeposited += removed;
                        needed -= removed;
                    }
                }
            }

            if (totalDeposited > 0)
            {
                currentFill += totalDeposited;
                OnFillChanged?.Invoke(currentFill, Capacity);

                if (visualRoot != null)
                {
                    visualRoot.DOKill(complete: true);
                    visualRoot.DOPunchScale(new Vector3(0.08f, -0.08f, 0.08f), 0.2f, 7, 0.5f);
                }

                if (audioSource != null && fillClip != null)
                {
                    audioSource.pitch = UnityEngine.Random.Range(0.95f, 1.15f);
                    audioSource.PlayOneShot(fillClip);
                }

                if (currentFill >= Capacity)
                {
                    CloseFlaps();
                    OnBoxFull?.Invoke();
                }

                return true;
            }

            return false;
        }

        #region IInteractable Implementation
        public string InteractableName => $"Cardboard Box ({currentFill}/{Capacity})";

        public List<InputPrompt> GetPrompts
        {
            get
            {
                var prompts = new List<InputPrompt>();
                if (isClosed)
                {
                    prompts.Add(new InputPrompt(InputPrompt.IconType.KeyE, "Box Sealed (Full)"));
                    return prompts;
                }

                int carried = PlayerInventory.Instance != null ? PlayerInventory.Instance.TotalCount : 0;
                if (carried > 0 && currentFill < Capacity)
                {
                    prompts.Add(new InputPrompt(InputPrompt.IconType.KeyE, $"Deposit Candy ({carried} carried)"));
                }
                else
                {
                    prompts.Add(new InputPrompt(InputPrompt.IconType.Custom, "Drop or hoover candies in"));
                }

                return prompts;
            }
        }

        public float HoldDuration => 0f;

        public void OnInteractStart(PlayerInteractor interactor) { }
        public void OnInteractCanceled(PlayerInteractor interactor) { }

        public void OnInteractComplete(PlayerInteractor interactor)
        {
            TryDepositFromInventory();
        }
        #endregion

        #if UNITY_EDITOR
        [ContextMenu("Build Procedural Box Geometry")]
        public void BuildProceduralGeometry()
        {
            // Clear existing generated children if any
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                UnityEditor.Undo.DestroyObjectImmediate(transform.GetChild(i).gameObject);
            }

            GameObject rootObj = new GameObject("VisualRoot");
            rootObj.transform.SetParent(transform, false);
            visualRoot = rootObj.transform;

            Material boxMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Cardboard.mat");
            if (boxMat == null)
            {
                boxMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                boxMat.color = new Color(0.76f, 0.60f, 0.42f); // Cardboard Kraft Brown
                if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Materials"))
                {
                    UnityEditor.AssetDatabase.CreateFolder("Assets", "Materials");
                }
                UnityEditor.AssetDatabase.CreateAsset(boxMat, "Assets/Materials/M_Cardboard.mat");
            }

            float width = 1.0f;
            float height = 0.8f;
            float length = 1.0f;
            float thickness = 0.04f;

            // Bottom base
            CreateBoxPiece("Bottom", visualRoot, new Vector3(0, thickness * 0.5f, 0), new Vector3(width, thickness, length), boxMat);
            // Side Walls
            CreateBoxPiece("WallLeft", visualRoot, new Vector3(-width * 0.5f + thickness * 0.5f, height * 0.5f, 0), new Vector3(thickness, height, length), boxMat);
            CreateBoxPiece("WallRight", visualRoot, new Vector3(width * 0.5f - thickness * 0.5f, height * 0.5f, 0), new Vector3(thickness, height, length), boxMat);
            CreateBoxPiece("WallFront", visualRoot, new Vector3(0, height * 0.5f, length * 0.5f - thickness * 0.5f), new Vector3(width, height, thickness), boxMat);
            CreateBoxPiece("WallBack", visualRoot, new Vector3(0, height * 0.5f, -length * 0.5f + thickness * 0.5f), new Vector3(width, height, thickness), boxMat);

            // Flap Hinges & Pieces
            float flapLength = length * 0.5f;

            // Flap Left (hinged along Left top edge)
            flapLeft = CreateFlap("FlapLeft", visualRoot, new Vector3(-width * 0.5f, height, 0), new Vector3(flapLength, thickness, length), new Vector3(flapLength * 0.5f, 0, 0), boxMat);
            
            // Flap Right (hinged along Right top edge)
            flapRight = CreateFlap("FlapRight", visualRoot, new Vector3(width * 0.5f, height, 0), new Vector3(flapLength, thickness, length), new Vector3(-flapLength * 0.5f, 0, 0), boxMat);

            // Flap Front (hinged along Front top edge)
            flapFront = CreateFlap("FlapFront", visualRoot, new Vector3(0, height, length * 0.5f), new Vector3(width, thickness, flapLength), new Vector3(0, 0, -flapLength * 0.5f), boxMat);

            // Flap Back (hinged along Back top edge)
            flapBack = CreateFlap("FlapBack", visualRoot, new Vector3(0, height, -length * 0.5f), new Vector3(width, thickness, flapLength), new Vector3(0, 0, flapLength * 0.5f), boxMat);

            SetupIntakeTrigger();
            UnityEditor.EditorUtility.SetDirty(gameObject);
            Debug.Log("[CardboardBox] Procedural geometry generated successfully!");
        }

        private GameObject CreateBoxPiece(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPos;
            cube.transform.localScale = scale;
            if (mat != null) cube.GetComponent<MeshRenderer>().sharedMaterial = mat;
            
            // Box pieces don't need individual colliders
            DestroyImmediate(cube.GetComponent<Collider>());
            return cube;
        }

        private Transform CreateFlap(string name, Transform parent, Vector3 pivotPos, Vector3 flapScale, Vector3 meshOffset, Material mat)
        {
            GameObject hinge = new GameObject(name);
            hinge.transform.SetParent(parent, false);
            hinge.transform.localPosition = pivotPos;

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = name + "_Mesh";
            visual.transform.SetParent(hinge.transform, false);
            visual.transform.localPosition = meshOffset;
            visual.transform.localScale = flapScale;
            if (mat != null) visual.GetComponent<MeshRenderer>().sharedMaterial = mat;
            DestroyImmediate(visual.GetComponent<Collider>());

            return hinge.transform;
        }
        #endif
    }
}
