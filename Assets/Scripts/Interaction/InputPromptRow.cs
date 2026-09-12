using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pinata.Interaction
{
    /// <summary>
    /// One row in the input prompt stack.
    /// Prefab structure:
    ///   [InputPromptRow]  — horizontal layout group
    ///     ├─ [Icon]       — Image, e.g. 28x28 or 30x30
    ///     └─ [Label]      — TextMeshProUGUI
    ///
    /// Spawned and pooled by InteractPromptUI.
    /// </summary>
    public class InputPromptRow : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI label;

        public void Set(Sprite iconSprite, string labelText)
        {
            if (icon != null)
            {
                icon.sprite = iconSprite;
                icon.enabled = iconSprite != null;
            }

            if (label != null)
            {
                label.text = labelText;
            }
        }
    }
}
