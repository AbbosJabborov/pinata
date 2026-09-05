using Pinata.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Pinata.UI
{
    public class ToolHUD : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ToolManager toolManager;
        [SerializeField] private HandsTool handsTool;

        [Header("Slot Containers")]
        [SerializeField] private RectTransform[] slotRects;
        [SerializeField] private Image[] slotBackgrounds;
        [SerializeField] private Text[] slotTexts;
        [SerializeField] private Text promptText;

        [Header("Styling")]
        [SerializeField] private Color activeSlotColor = new Color(1.0f, 0.75f, 0.15f, 0.85f);
        [SerializeField] private Color inactiveSlotColor = new Color(0.15f, 0.18f, 0.22f, 0.65f);
        [SerializeField] private Color activeTextColor = Color.white;
        [SerializeField] private Color inactiveTextColor = new Color(0.7f, 0.7f, 0.7f, 0.8f);

        private void Awake()
        {
            if (toolManager == null)
            {
                toolManager = FindFirstObjectByType<ToolManager>();
            }
            if (handsTool == null)
            {
                handsTool = FindFirstObjectByType<HandsTool>();
            }

            BuildUIIfMissing();
        }

        private void OnEnable()
        {
            if (toolManager != null)
            {
                toolManager.OnToolChanged += HandleToolChanged;
            }
            if (handsTool != null)
            {
                handsTool.OnPromptChanged += HandlePromptChanged;
            }
        }

        private void OnDisable()
        {
            if (toolManager != null)
            {
                toolManager.OnToolChanged -= HandleToolChanged;
            }
            if (handsTool != null)
            {
                handsTool.OnPromptChanged -= HandlePromptChanged;
            }
        }

        private void Start()
        {
            if (toolManager != null)
            {
                UpdateSlotHighlights(toolManager.CurrentSlot);
            }
        }

        private void HandleToolChanged(int slotIndex, string toolName)
        {
            UpdateSlotHighlights(slotIndex);
            if (slotIndex != 2 && promptText != null)
            {
                promptText.text = string.Empty;
            }
        }

        private void HandlePromptChanged(string prompt)
        {
            if (promptText != null)
            {
                promptText.text = prompt;
            }
        }

        public void UpdateSlotHighlights(int activeSlot)
        {
            if (slotBackgrounds == null || slotTexts == null) return;

            for (int i = 0; i < slotBackgrounds.Length; i++)
            {
                bool isActive = (i == activeSlot);
                if (slotBackgrounds[i] != null)
                {
                    slotBackgrounds[i].color = isActive ? activeSlotColor : inactiveSlotColor;
                }
                if (slotTexts[i] != null)
                {
                    slotTexts[i].color = isActive ? activeTextColor : inactiveTextColor;
                    slotTexts[i].fontStyle = isActive ? FontStyle.Bold : FontStyle.Normal;
                }
                if (slotRects != null && i < slotRects.Length && slotRects[i] != null)
                {
                    slotRects[i].localScale = isActive ? new Vector3(1.08f, 1.08f, 1f) : Vector3.one;
                }
            }
        }

        private void BuildUIIfMissing()
        {
            // If already configured in Inspector, skip
            if (slotRects != null && slotRects.Length == 5 && promptText != null) return;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Hotbar Root at Bottom-Center
            GameObject hotbarRoot = new GameObject("Hotbar_Root");
            hotbarRoot.transform.SetParent(transform, false);
            var hotbarRect = hotbarRoot.AddComponent<RectTransform>();
            hotbarRect.anchorMin = new Vector2(0.5f, 0f);
            hotbarRect.anchorMax = new Vector2(0.5f, 0f);
            hotbarRect.pivot = new Vector2(0.5f, 0f);
            hotbarRect.anchoredPosition = new Vector2(0f, 20f);
            hotbarRect.sizeDelta = new Vector2(560f, 44f);

            var layout = hotbarRoot.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            slotRects = new RectTransform[5];
            slotBackgrounds = new Image[5];
            slotTexts = new Text[5];

            string[] names = { "1: BAT", "2: BROOM", "3: HANDS", "4: SLICER", "5: VACUUM" };

            for (int i = 0; i < 5; i++)
            {
                GameObject slot = new GameObject($"Slot_{i + 1}");
                slot.transform.SetParent(hotbarRoot.transform, false);
                var rect = slot.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(98f, 38f);
                var img = slot.AddComponent<Image>();
                img.color = (i == 0) ? activeSlotColor : inactiveSlotColor;

                GameObject label = new GameObject("Label");
                label.transform.SetParent(slot.transform, false);
                var labelRect = label.AddComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.sizeDelta = Vector2.zero;

                var text = label.AddComponent<Text>();
                text.text = names[i];
                text.font = font;
                text.fontSize = 13;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = (i == 0) ? activeTextColor : inactiveTextColor;

                slotRects[i] = rect;
                slotBackgrounds[i] = img;
                slotTexts[i] = text;
            }

            // Prompt text near center of screen below crosshair
            GameObject promptObj = new GameObject("Action_Prompt");
            promptObj.transform.SetParent(transform, false);
            var pRect = promptObj.AddComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0.5f, 0.5f);
            pRect.anchorMax = new Vector2(0.5f, 0.5f);
            pRect.pivot = new Vector2(0.5f, 0.5f);
            pRect.anchoredPosition = new Vector2(0f, -60f);
            pRect.sizeDelta = new Vector2(400f, 36f);

            promptText = promptObj.AddComponent<Text>();
            promptText.text = string.Empty;
            promptText.font = font;
            promptText.fontSize = 16;
            promptText.fontStyle = FontStyle.Bold;
            promptText.alignment = TextAnchor.MiddleCenter;
            promptText.color = new Color(1f, 0.95f, 0.7f, 1f);

            var shadow = promptObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.75f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
        }
    }
}
