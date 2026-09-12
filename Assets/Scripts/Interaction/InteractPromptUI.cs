using System.Collections.Generic;
using Pinata.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinata.Interaction
{
    /// <summary>
    /// Drives the interaction UI:
    ///   1. Interactable name — TMP label positioned near/below crosshair.
    ///   2. Input prompt stack — vertical list of [icon + label] rows (e.g. bottom-right or center).
    ///      Rows are provided by Current IInteractable's GetPrompts list.
    /// </summary>
    public class InteractPromptUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInteractor interactor;

        [Header("Name Label")]
        [SerializeField] private TextMeshProUGUI nameLabel;
        [SerializeField] private CanvasGroup nameGroup;
        [SerializeField] private float nameFadeSpeed = 12f;

        [Header("Prompt Stack")]
        [SerializeField] private Transform promptStack; // Vertical Layout Group root
        [SerializeField] private InputPromptRow promptRowPrefab;
        [SerializeField] private float promptFadeSpeed = 10f;
        [SerializeField] private CanvasGroup stackGroup;

        [Header("Icons — order must match InputPrompt.IconType enum")]
        [SerializeField] private Sprite[] iconSprites; // LMB, RMB, KeyE, KeyQ, KeyF, KeyR, ScrollWheel

        private readonly List<InputPromptRow> _pool = new List<InputPromptRow>();
        private readonly List<InputPromptRow> _activeRows = new List<InputPromptRow>();

        private float _nameTargetAlpha;
        private float _stackTargetAlpha;

        private void Awake()
        {
            if (interactor == null) interactor = PlayerInteractor.Instance;

            if (nameGroup == null && nameLabel != null) nameGroup = nameLabel.GetComponent<CanvasGroup>();
            if (stackGroup == null && promptStack != null) stackGroup = promptStack.GetComponent<CanvasGroup>();

            if (nameGroup != null) nameGroup.alpha = 0f;
            if (stackGroup != null) stackGroup.alpha = 0f;
        }

        private void Update()
        {
            if (CursorModeManager.IsUnlocked)
            {
                _nameTargetAlpha = 0f;
                _stackTargetAlpha = 0f;
                if (nameGroup != null) nameGroup.alpha = 0f;
                if (stackGroup != null) stackGroup.alpha = 0f;
                return;
            }

            if (interactor == null)
            {
                interactor = PlayerInteractor.Instance;
                if (interactor == null) return;
            }

            IInteractable current = interactor.Current;

            // Collect prompts from current interactable
            List<InputPrompt> allPrompts = new List<InputPrompt>();
            if (current != null && current.GetPrompts != null)
            {
                allPrompts.AddRange(current.GetPrompts);
            }

            // Name label
            bool hasName = current != null && !string.IsNullOrEmpty(current.InteractableName);
            if (nameLabel != null)
            {
                nameLabel.text = hasName ? current.InteractableName : "";
            }
            _nameTargetAlpha = hasName ? 1f : 0f;

            // Prompt stack
            _stackTargetAlpha = allPrompts.Count > 0 ? 1f : 0f;
            if (promptStack != null && promptRowPrefab != null)
            {
                RefreshRows(allPrompts);
            }

            // Smooth alpha fading
            if (nameGroup != null)
                nameGroup.alpha = Mathf.MoveTowards(nameGroup.alpha, _nameTargetAlpha, nameFadeSpeed * Time.deltaTime);

            if (stackGroup != null)
                stackGroup.alpha = Mathf.MoveTowards(stackGroup.alpha, _stackTargetAlpha, promptFadeSpeed * Time.deltaTime);
        }

        private void RefreshRows(List<InputPrompt> prompts)
        {
            // Return active rows to pool
            foreach (var row in _activeRows)
            {
                row.gameObject.SetActive(false);
                _pool.Add(row);
            }
            _activeRows.Clear();

            // Spawn or reuse rows
            foreach (var prompt in prompts)
            {
                InputPromptRow row = GetPooledRow();
                row.Set(GetSprite(prompt), prompt.Label);
                row.transform.SetAsLastSibling();
                row.gameObject.SetActive(true);
                _activeRows.Add(row);
            }
        }

        private InputPromptRow GetPooledRow()
        {
            if (_pool.Count > 0)
            {
                var row = _pool[_pool.Count - 1];
                _pool.RemoveAt(_pool.Count - 1);
                return row;
            }
            return Instantiate(promptRowPrefab, promptStack);
        }

        private Sprite GetSprite(InputPrompt prompt)
        {
            int index = (int)prompt.Icon;
            if (prompt.Icon == InputPrompt.IconType.Custom) return prompt.CustomSprite;
            if (iconSprites != null && index >= 0 && index < iconSprites.Length) return iconSprites[index];
            return null;
        }
    }
}
