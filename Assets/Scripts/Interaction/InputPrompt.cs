using UnityEngine;

namespace Pinata.Interaction
{
    /// <summary>
    /// A single input prompt entry — icon type + label text.
    /// Returned by IInteractable and rendered by InteractPromptUI.
    /// </summary>
    [System.Serializable]
    public class InputPrompt
    {
        public enum IconType
        {
            LMB,
            RMB,
            KeyE,
            KeyQ,
            KeyF,
            KeyR,
            ScrollWheel,
            Custom
        }

        public IconType Icon;
        public string Label;
        public Sprite CustomSprite; // used when Icon == Custom

        public InputPrompt(IconType icon, string label, Sprite customSprite = null)
        {
            Icon = icon;
            Label = label;
            CustomSprite = customSprite;
        }
    }
}
