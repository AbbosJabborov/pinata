using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pinata.Tools
{
    public class ToolManager : MonoBehaviour
    {
        [Header("Tools List")]
        [SerializeField] private List<MonoBehaviour> toolComponents = new List<MonoBehaviour>();
        [SerializeField] private Transform toolSocket;
        [SerializeField] private int initialSlot = 0;

        [Header("Switch Animation")]
        [SerializeField] private float switchDuration = 0.10f;
        [SerializeField] private float lowerDistance = 0.30f;

        private readonly List<ITool> _tools = new List<ITool>();
        private int _currentSlot = 0;
        private bool _isSwitching = false;
        private Vector3 _originalSocketPos;
        private int _unlockedCount = 1; // Slot 0 (Hand) is always unlocked

        public event Action<int, string> OnToolChanged;
        public event Action<int> OnUnlockedCountChanged;

        public int CurrentSlot => _currentSlot;
        public int UnlockedCount => _unlockedCount;
        public ITool CurrentTool => (_currentSlot >= 0 && _currentSlot < _tools.Count) ? _tools[_currentSlot] : null;

        /// <summary>Called by EconomyManager when a new tool tier is purchased.</summary>
        public void SetUnlockedCount(int count)
        {
            _unlockedCount = Mathf.Clamp(count, 1, _tools.Count);
            OnUnlockedCountChanged?.Invoke(_unlockedCount);
        }

        public bool IsSlotUnlocked(int slotIndex) => slotIndex >= 0 && slotIndex < _unlockedCount;

        public static ToolManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;

            if (toolSocket == null)
            {
                toolSocket = transform;
            }
            _originalSocketPos = toolSocket.localPosition;

            InitializeTools();
        }

        private void Start()
        {
            EquipSlot(initialSlot, immediate: true);
        }

        public void RegisterTool(MonoBehaviour toolComponent)
        {
            if (toolComponent is ITool tool && !_tools.Contains(tool))
            {
                _tools.Add(tool);
                if (!toolComponents.Contains(toolComponent))
                {
                    toolComponents.Add(toolComponent);
                }
            }
        }

        private void InitializeTools()
        {
            _tools.Clear();
            foreach (var comp in toolComponents)
            {
                if (comp is ITool tool)
                {
                    _tools.Add(tool);
                    comp.gameObject.SetActive(false);
                }
            }

            // If empty, look in children
            if (_tools.Count == 0)
            {
                var found = GetComponentsInChildren<ITool>(true);
                foreach (var tool in found)
                {
                    _tools.Add(tool);
                    if (tool is MonoBehaviour mb)
                    {
                        toolComponents.Add(mb);
                        mb.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void Update()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            HandleInput();
        }

        private void HandleInput()
        {
            if (_isSwitching) return;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.digit1Key.wasPressedThisFrame)
                {
                    SelectSlot(0);
                }
                else if (Keyboard.current.digit2Key.wasPressedThisFrame)
                {
                    SelectSlot(1);
                }
                else if (Keyboard.current.digit3Key.wasPressedThisFrame)
                {
                    SelectSlot(2);
                }
                else if (Keyboard.current.digit4Key.wasPressedThisFrame)
                {
                    SelectSlot(3);
                }
                else if (Keyboard.current.digit5Key.wasPressedThisFrame)
                {
                    SelectSlot(4);
                }
            }

            if (Mouse.current != null)
            {
                float scroll = Mouse.current.scroll.ReadValue().y;
                if (scroll > 0.5f)
                {
                    // Scroll up: previous unlocked tool
                    int prev = (_currentSlot - 1 + _unlockedCount) % _unlockedCount;
                    SelectSlot(prev);
                }
                else if (scroll < -0.5f)
                {
                    // Scroll down: next unlocked tool
                    int next = (_currentSlot + 1) % _unlockedCount;
                    SelectSlot(next);
                }
            }
        }

        public void SelectSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _tools.Count || slotIndex == _currentSlot || _isSwitching) return;
            if (!IsSlotUnlocked(slotIndex)) return;

            // If current tool is in middle of an action, check if it can be interrupted
            if (_tools[_currentSlot].IsBusy) return;

            StartCoroutine(SwitchRoutine(slotIndex));
        }

        private IEnumerator SwitchRoutine(int newSlot)
        {
            _isSwitching = true;

            // Lower current tool
            float elapsed = 0f;
            Vector3 startPos = toolSocket.localPosition;
            Vector3 loweredPos = _originalSocketPos - new Vector3(0, lowerDistance, 0);

            while (elapsed < switchDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / switchDuration;
                toolSocket.localPosition = Vector3.Lerp(startPos, loweredPos, t);
                yield return null;
            }

            // Swap tools
            _tools[_currentSlot].OnUnequip();
            _currentSlot = newSlot;
            _tools[_currentSlot].OnEquip();
            OnToolChanged?.Invoke(_currentSlot, _tools[_currentSlot].ToolName);

            // Raise new tool
            elapsed = 0f;
            while (elapsed < switchDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / switchDuration;
                toolSocket.localPosition = Vector3.Lerp(loweredPos, _originalSocketPos, t);
                yield return null;
            }

            toolSocket.localPosition = _originalSocketPos;
            _isSwitching = false;
        }

        public void EquipSlot(int slot, bool immediate = false)
        {
            if (slot < 0 || slot >= _tools.Count) return;

            for (int i = 0; i < _tools.Count; i++)
            {
                if (i == slot)
                {
                    _tools[i].OnEquip();
                }
                else
                {
                    _tools[i].OnUnequip();
                }
            }

            _currentSlot = slot;
            toolSocket.localPosition = _originalSocketPos;
            OnToolChanged?.Invoke(_currentSlot, _tools[_currentSlot].ToolName);
        }
    }
}
