using System;
using System.Collections;
using System.Collections.Generic;
using Pinata.Candy;
using Pinata.Gameplay;
using Pinata.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Pinata.UI
{
    public class WarehouseHUD : MonoBehaviour
    {
        public static WarehouseHUD Instance { get; private set; }

        private Canvas _canvas;
        private Text _cashText;
        private Text _backpackText;
        private Image _backpackFillImage;
        private Text _promptText;
        private GameObject _promptPanel;
        private GameObject _upgradeModal;
        private Text _cashDeltaText;

        // Upgrade Modal Elements
        private Text _modalCashText;
        private readonly Text[] _upgradeLevelTexts = new Text[4];
        private readonly Text[] _upgradeDescTexts = new Text[4];
        private readonly Button[] _upgradeButtons = new Button[4];
        private readonly Text[] _upgradeButtonTexts = new Text[4];

        // Nearby interaction targets
        private CandyBasket _activeBasket;
        private UpgradeKiosk _activeKiosk;

        // Color mapping for candy badges
        private static readonly Dictionary<CandyType, Color> CandyColors = new Dictionary<CandyType, Color>
        {
            { CandyType.Blue, new Color(0.2f, 0.8f, 1f) },
            { CandyType.Green, new Color(0.3f, 0.95f, 0.4f) },
            { CandyType.Orange, new Color(1f, 0.6f, 0.15f) },
            { CandyType.Pink, new Color(1f, 0.4f, 0.75f) },
            { CandyType.Yellow, new Color(1f, 0.9f, 0.2f) },
            { CandyType.Purple, new Color(0.8f, 0.4f, 1f) }
        };

        private readonly Dictionary<CandyType, Text> _candyBadgeTexts = new Dictionary<CandyType, Text>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            _canvas = GetComponentInParent<Canvas>();
            BuildHUDUI();
        }

        private void Start()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnCashChanged += HandleCashChanged;
                EconomyManager.Instance.OnUpgradePurchased += HandleUpgradePurchased;
                UpdateCashDisplay(EconomyManager.Instance.Cash, 0);
            }

            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnInventoryChanged += HandleInventoryChanged;
                UpdateBackpackDisplay(PlayerInventory.Instance.TotalCount, PlayerInventory.Instance.MaxCapacity);
            }

            UpdateUpgradeModal();
        }

        private void OnDestroy()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnCashChanged -= HandleCashChanged;
                EconomyManager.Instance.OnUpgradePurchased -= HandleUpgradePurchased;
            }
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnInventoryChanged -= HandleInventoryChanged;
            }
        }

        private void Update()
        {
            CheckInteractions();
            HandleInteractionInput();
        }

        private void CheckInteractions()
        {
            _activeBasket = null;
            _activeKiosk = null;

            var kiosks = FindObjectsByType<UpgradeKiosk>();
            foreach (var k in kiosks)
            {
                if (k.IsPlayerNear)
                {
                    _activeKiosk = k;
                    break;
                }
            }

            if (_activeKiosk == null)
            {
                var baskets = FindObjectsByType<CandyBasket>();
                float closestDist = float.MaxValue;
                Camera cam = Camera.main;
                Vector3 playerPos = cam != null ? cam.transform.position : Vector3.zero;

                foreach (var b in baskets)
                {
                    if (b.IsPlayerNear)
                    {
                        float dist = Vector3.Distance(playerPos, b.transform.position);
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            _activeBasket = b;
                        }
                    }
                }
            }

            UpdateInteractionPrompt();
        }

        private void HandleInteractionInput()
        {
            if (_upgradeModal != null && _upgradeModal.activeSelf)
            {
                if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    CloseUpgradeModal();
                }
                return;
            }

            if (_activeKiosk != null)
            {
                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                {
                    OpenUpgradeModal();
                }
            }
            else if (_activeBasket != null)
            {
                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                {
                    _activeBasket.TryDepositFromInventory();
                }
                else if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
                {
                    _activeBasket.TrySell();
                }
            }
        }

        private void UpdateInteractionPrompt()
        {
            if (_promptPanel == null || _promptText == null) return;

            if (_upgradeModal != null && _upgradeModal.activeSelf)
            {
                _promptPanel.SetActive(false);
                return;
            }

            if (_activeKiosk != null)
            {
                _promptPanel.SetActive(true);
                _promptText.text = "<color=#FFD54F><b>[E]</b></color> Open Upgrade Kiosk";
            }
            else if (_activeBasket != null)
            {
                _promptPanel.SetActive(true);
                int carried = PlayerInventory.Instance != null ? PlayerInventory.Instance.GetCount(_activeBasket.Type) : 0;
                string typeColorHex = ColorUtility.ToHtmlStringRGB(CandyColors.TryGetValue(_activeBasket.Type, out var c) ? c : Color.yellow);

                string prompt = "";
                if (carried > 0 && !_activeBasket.IsFull)
                {
                    prompt += $"<color=#{typeColorHex}><b>[E]</b> Deposit {_activeBasket.Type} ({carried} carried)</color>\n";
                }

                if (_activeBasket.HasItems)
                {
                    string bonus = _activeBasket.IsFull ? " <color=#69F0AE>(FULL BONUS!)</color>" : "";
                    prompt += $"<color=#69F0AE><b>[F]</b> Sell Crate (${_activeBasket.CurrentSaleValue}){bonus}</color>";
                }
                else if (carried == 0)
                {
                    prompt = $"<color=#{typeColorHex}>{_activeBasket.Type} Basket ({_activeBasket.CurrentCount}/{_activeBasket.TargetCapacity})</color>";
                }

                _promptText.text = prompt;
            }
            else
            {
                _promptPanel.SetActive(false);
            }
        }

        private void HandleCashChanged(int newBalance, int delta)
        {
            UpdateCashDisplay(newBalance, delta);
            UpdateUpgradeModal();
        }

        private void UpdateCashDisplay(int balance, int delta)
        {
            if (_cashText != null)
            {
                _cashText.text = $"${balance:N0}";
            }

            if (delta > 0 && _cashDeltaText != null)
            {
                _cashDeltaText.text = $"+${delta}";
                _cashDeltaText.gameObject.SetActive(true);
                StopCoroutine("FadeDeltaText");
                StartCoroutine("FadeDeltaText");
            }
        }

        private IEnumerator FadeDeltaText()
        {
            yield return new WaitForSeconds(1.2f);
            if (_cashDeltaText != null) _cashDeltaText.gameObject.SetActive(false);
        }

        private void HandleInventoryChanged(int total, int max)
        {
            UpdateBackpackDisplay(total, max);
        }

        private void UpdateBackpackDisplay(int total, int max)
        {
            if (_backpackText != null)
            {
                _backpackText.text = $"🎒 {total} / {max}";
            }

            if (_backpackFillImage != null)
            {
                float ratio = (float)total / Mathf.Max(max, 1);
                _backpackFillImage.fillAmount = ratio;
                if (ratio >= 1.0f) _backpackFillImage.color = new Color(1f, 0.3f, 0.3f);
                else if (ratio >= 0.8f) _backpackFillImage.color = new Color(1f, 0.8f, 0.2f);
                else _backpackFillImage.color = new Color(0.2f, 0.9f, 0.5f);
            }

            // Update candy badges
            if (PlayerInventory.Instance != null)
            {
                foreach (var pair in _candyBadgeTexts)
                {
                    int count = PlayerInventory.Instance.GetCount(pair.Key);
                    pair.Value.text = count.ToString();
                    pair.Value.transform.parent.gameObject.SetActive(count > 0);
                }
            }
        }

        private void HandleUpgradePurchased(string upgradeName, int newLevel)
        {
            UpdateUpgradeModal();
        }

        public void OpenUpgradeModal()
        {
            if (_upgradeModal != null)
            {
                _upgradeModal.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                UpdateUpgradeModal();
            }
        }

        public void CloseUpgradeModal()
        {
            if (_upgradeModal != null)
            {
                _upgradeModal.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void UpdateUpgradeModal()
        {
            if (EconomyManager.Instance == null || _upgradeModal == null) return;

            var em = EconomyManager.Instance;
            if (_modalCashText != null) _modalCashText.text = $"Balance: ${em.Cash:N0}";

            // 0: Backpack
            int bpCost = em.GetBackpackUpgradeCost();
            _upgradeLevelTexts[0].text = $"🎒 Backpack Lv {em.BackpackLevel}";
            int bpIdx = Mathf.Clamp(em.BackpackLevel - 1, 0, EconomyManager.BackpackCapacities.Length - 1);
            int nextBp = em.BackpackLevel < EconomyManager.BackpackCapacities.Length ? EconomyManager.BackpackCapacities[em.BackpackLevel] : EconomyManager.BackpackCapacities[bpIdx];
            _upgradeDescTexts[0].text = bpCost > 0 ? $"Cap: {EconomyManager.BackpackCapacities[bpIdx]} -> {nextBp}" : $"Cap: {EconomyManager.BackpackCapacities[bpIdx]} (MAX)";
            _upgradeButtonTexts[0].text = bpCost > 0 ? $"${bpCost}" : "MAX";
            _upgradeButtons[0].interactable = bpCost > 0 && em.Cash >= bpCost;

            // 1: Bat
            int batCost = em.GetBatUpgradeCost();
            _upgradeLevelTexts[1].text = $"🏏 Bat Power Lv {em.BatLevel}";
            int batIdx = Mathf.Clamp(em.BatLevel - 1, 0, EconomyManager.BatDamages.Length - 1);
            float nextBat = em.BatLevel < EconomyManager.BatDamages.Length ? EconomyManager.BatDamages[em.BatLevel] : EconomyManager.BatDamages[batIdx];
            _upgradeDescTexts[1].text = batCost > 0 ? $"Dmg: {EconomyManager.BatDamages[batIdx]} -> {nextBat}" : $"Dmg: {EconomyManager.BatDamages[batIdx]} (MAX)";
            _upgradeButtonTexts[1].text = batCost > 0 ? $"${batCost}" : "MAX";
            _upgradeButtons[1].interactable = batCost > 0 && em.Cash >= batCost;

            // 2: Vacuum
            int vacCost = em.GetVacuumUpgradeCost();
            _upgradeLevelTexts[2].text = $"🌪️ Shop-Vac Lv {em.VacuumLevel}";
            int vacIdx = Mathf.Clamp(em.VacuumLevel - 1, 0, EconomyManager.VacuumReaches.Length - 1);
            float nextVac = em.VacuumLevel < EconomyManager.VacuumReaches.Length ? EconomyManager.VacuumReaches[em.VacuumLevel] : EconomyManager.VacuumReaches[vacIdx];
            _upgradeDescTexts[2].text = vacCost > 0 ? $"Reach: {EconomyManager.VacuumReaches[vacIdx]:F1}m -> {nextVac:F1}m" : $"Reach: {EconomyManager.VacuumReaches[vacIdx]:F1}m (MAX)";
            _upgradeButtonTexts[2].text = vacCost > 0 ? $"${vacCost}" : "MAX";
            _upgradeButtons[2].interactable = vacCost > 0 && em.Cash >= vacCost;

            // 3: Multiplier
            int mulCost = em.GetMultiplierUpgradeCost();
            _upgradeLevelTexts[3].text = $"💰 Candy Value Lv {em.ValueMultiplierLevel}";
            int mulIdx = Mathf.Clamp(em.ValueMultiplierLevel - 1, 0, EconomyManager.ValueMultipliers.Length - 1);
            float nextMul = em.ValueMultiplierLevel < EconomyManager.ValueMultipliers.Length ? EconomyManager.ValueMultipliers[em.ValueMultiplierLevel] : EconomyManager.ValueMultipliers[mulIdx];
            _upgradeDescTexts[3].text = mulCost > 0 ? $"Payout: {EconomyManager.ValueMultipliers[mulIdx]:F2}x -> {nextMul:F2}x" : $"Payout: {EconomyManager.ValueMultipliers[mulIdx]:F2}x (MAX)";
            _upgradeButtonTexts[3].text = mulCost > 0 ? $"${mulCost}" : "MAX";
            _upgradeButtons[3].interactable = mulCost > 0 && em.Cash >= mulCost;
        }

        private void BuildHUDUI()
        {
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            // --- Top Right: Cash Badge ---
            GameObject cashPanel = new GameObject("CashBadge");
            cashPanel.transform.SetParent(transform, false);
            var cashRect = cashPanel.AddComponent<RectTransform>();
            cashRect.anchorMin = new Vector2(1, 1);
            cashRect.anchorMax = new Vector2(1, 1);
            cashRect.pivot = new Vector2(1, 1);
            cashRect.anchoredPosition = new Vector2(-25, -25);
            cashRect.sizeDelta = new Vector2(170, 50);

            var cashBg = cashPanel.AddComponent<Image>();
            cashBg.color = new Color(0.08f, 0.12f, 0.10f, 0.88f);

            GameObject cashTextObj = new GameObject("CashText");
            cashTextObj.transform.SetParent(cashPanel.transform, false);
            var ctRect = cashTextObj.AddComponent<RectTransform>();
            ctRect.anchorMin = Vector2.zero;
            ctRect.anchorMax = Vector2.one;
            ctRect.offsetMin = new Vector2(15, 0);
            ctRect.offsetMax = new Vector2(-15, 0);

            _cashText = cashTextObj.AddComponent<Text>();
            _cashText.font = defaultFont;
            _cashText.fontSize = 26;
            _cashText.fontStyle = FontStyle.Bold;
            _cashText.color = new Color(0.4f, 1f, 0.6f);
            _cashText.alignment = TextAnchor.MiddleRight;
            _cashText.text = "$0";

            // Cash Delta popup
            GameObject deltaObj = new GameObject("CashDelta");
            deltaObj.transform.SetParent(cashPanel.transform, false);
            var dtRect = deltaObj.AddComponent<RectTransform>();
            dtRect.anchorMin = new Vector2(1, 0);
            dtRect.anchorMax = new Vector2(1, 0);
            dtRect.pivot = new Vector2(1, 1);
            dtRect.anchoredPosition = new Vector2(0, -5);
            dtRect.sizeDelta = new Vector2(120, 30);
            _cashDeltaText = deltaObj.AddComponent<Text>();
            _cashDeltaText.font = defaultFont;
            _cashDeltaText.fontSize = 20;
            _cashDeltaText.fontStyle = FontStyle.Bold;
            _cashDeltaText.color = new Color(0.3f, 1f, 0.4f);
            _cashDeltaText.alignment = TextAnchor.MiddleRight;
            deltaObj.SetActive(false);

            // --- Bottom Left/Center: Backpack & Candy Counts ---
            GameObject bpPanel = new GameObject("BackpackBadge");
            bpPanel.transform.SetParent(transform, false);
            var bpRect = bpPanel.AddComponent<RectTransform>();
            bpRect.anchorMin = new Vector2(0.5f, 0);
            bpRect.anchorMax = new Vector2(0.5f, 0);
            bpRect.pivot = new Vector2(0.5f, 0);
            bpRect.anchoredPosition = new Vector2(0, 78);
            bpRect.sizeDelta = new Vector2(380, 52);

            var bpBg = bpPanel.AddComponent<Image>();
            bpBg.color = new Color(0.10f, 0.12f, 0.16f, 0.85f);

            // Fill Bar Background
            GameObject fillBg = new GameObject("FillBg");
            fillBg.transform.SetParent(bpPanel.transform, false);
            var fbRect = fillBg.AddComponent<RectTransform>();
            fbRect.anchorMin = new Vector2(0, 0);
            fbRect.anchorMax = new Vector2(1, 0);
            fbRect.pivot = new Vector2(0.5f, 0);
            fbRect.anchoredPosition = new Vector2(0, 6);
            fbRect.sizeDelta = new Vector2(-20, 10);
            var fbImg = fillBg.AddComponent<Image>();
            fbImg.color = new Color(0.2f, 0.25f, 0.3f, 0.5f);

            // Fill Bar Foreground
            GameObject fillFg = new GameObject("FillFg");
            fillFg.transform.SetParent(fillBg.transform, false);
            var fgRect = fillFg.AddComponent<RectTransform>();
            fgRect.anchorMin = Vector2.zero;
            fgRect.anchorMax = Vector2.one;
            fgRect.sizeDelta = Vector2.zero;
            _backpackFillImage = fillFg.AddComponent<Image>();
            _backpackFillImage.type = Image.Type.Filled;
            _backpackFillImage.fillMethod = Image.FillMethod.Horizontal;
            _backpackFillImage.fillAmount = 0f;
            _backpackFillImage.color = new Color(0.2f, 0.9f, 0.5f);

            // Backpack Text
            GameObject bpTextObj = new GameObject("BPText");
            bpTextObj.transform.SetParent(bpPanel.transform, false);
            var bptRect = bpTextObj.AddComponent<RectTransform>();
            bptRect.anchorMin = Vector2.zero;
            bptRect.anchorMax = Vector2.one;
            bptRect.offsetMin = new Vector2(15, 12);
            bptRect.offsetMax = new Vector2(-15, 0);
            _backpackText = bpTextObj.AddComponent<Text>();
            _backpackText.font = defaultFont;
            _backpackText.fontSize = 17;
            _backpackText.fontStyle = FontStyle.Bold;
            _backpackText.color = Color.white;
            _backpackText.alignment = TextAnchor.MiddleLeft;
            _backpackText.text = "🎒 0 / 30";

            // Candy Type Badges Container
            GameObject badgesContainer = new GameObject("CandyBadges");
            badgesContainer.transform.SetParent(bpPanel.transform, false);
            var bcRect = badgesContainer.AddComponent<RectTransform>();
            bcRect.anchorMin = new Vector2(0.45f, 0.3f);
            bcRect.anchorMax = new Vector2(0.98f, 0.95f);
            bcRect.sizeDelta = Vector2.zero;
            var layout = badgesContainer.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.spacing = 6;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            foreach (CandyType type in (CandyType[])Enum.GetValues(typeof(CandyType)))
            {
                Color col = CandyColors.TryGetValue(type, out var tc) ? tc : Color.white;
                GameObject b = new GameObject($"Badge_{type}");
                b.transform.SetParent(badgesContainer.transform, false);
                var bRect = b.AddComponent<RectTransform>();
                bRect.sizeDelta = new Vector2(38, 24);
                var bBg = b.AddComponent<Image>();
                bBg.color = new Color(col.r, col.g, col.b, 0.25f);

                GameObject dot = new GameObject("Dot");
                dot.transform.SetParent(b.transform, false);
                var dRect = dot.AddComponent<RectTransform>();
                dRect.anchorMin = new Vector2(0, 0.5f);
                dRect.anchorMax = new Vector2(0, 0.5f);
                dRect.pivot = new Vector2(0, 0.5f);
                dRect.anchoredPosition = new Vector2(4, 0);
                dRect.sizeDelta = new Vector2(10, 10);
                var dImg = dot.AddComponent<Image>();
                dImg.color = col;

                GameObject countObj = new GameObject("Count");
                countObj.transform.SetParent(b.transform, false);
                var cRect = countObj.AddComponent<RectTransform>();
                cRect.anchorMin = Vector2.zero;
                cRect.anchorMax = Vector2.one;
                cRect.offsetMin = new Vector2(16, 0);
                cRect.offsetMax = new Vector2(-3, 0);
                var cTxt = countObj.AddComponent<Text>();
                cTxt.font = defaultFont;
                cTxt.fontSize = 13;
                cTxt.fontStyle = FontStyle.Bold;
                cTxt.color = col;
                cTxt.alignment = TextAnchor.MiddleCenter;
                cTxt.text = "0";

                _candyBadgeTexts[type] = cTxt;
                b.SetActive(false);
            }

            // --- Center Screen: Context Prompt Panel ---
            _promptPanel = new GameObject("PromptPanel");
            _promptPanel.transform.SetParent(transform, false);
            var pRect = _promptPanel.AddComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0.5f, 0.5f);
            pRect.anchorMax = new Vector2(0.5f, 0.5f);
            pRect.pivot = new Vector2(0.5f, 0.5f);
            pRect.anchoredPosition = new Vector2(0, -85);
            pRect.sizeDelta = new Vector2(480, 70);

            var pBg = _promptPanel.AddComponent<Image>();
            pBg.color = new Color(0.06f, 0.08f, 0.12f, 0.88f);

            GameObject promptTextObj = new GameObject("PromptText");
            promptTextObj.transform.SetParent(_promptPanel.transform, false);
            var ptRect = promptTextObj.AddComponent<RectTransform>();
            ptRect.anchorMin = Vector2.zero;
            ptRect.anchorMax = Vector2.one;
            ptRect.offsetMin = new Vector2(15, 6);
            ptRect.offsetMax = new Vector2(-15, -6);
            _promptText = promptTextObj.AddComponent<Text>();
            _promptText.font = defaultFont;
            _promptText.fontSize = 18;
            _promptText.fontStyle = FontStyle.Bold;
            _promptText.color = Color.white;
            _promptText.alignment = TextAnchor.MiddleCenter;
            _promptText.supportRichText = true;
            _promptText.text = "";
            _promptPanel.SetActive(false);

            // --- Upgrade Modal Window ---
            BuildUpgradeModalUI(defaultFont);
        }

        private void BuildUpgradeModalUI(Font defaultFont)
        {
            _upgradeModal = new GameObject("UpgradeModal");
            _upgradeModal.transform.SetParent(transform, false);
            var mRect = _upgradeModal.AddComponent<RectTransform>();
            mRect.anchorMin = new Vector2(0.5f, 0.5f);
            mRect.anchorMax = new Vector2(0.5f, 0.5f);
            mRect.pivot = new Vector2(0.5f, 0.5f);
            mRect.anchoredPosition = Vector2.zero;
            mRect.sizeDelta = new Vector2(580, 440);

            var mBg = _upgradeModal.AddComponent<Image>();
            mBg.color = new Color(0.08f, 0.10f, 0.14f, 0.96f);

            // Modal Header
            GameObject headerObj = new GameObject("Header");
            headerObj.transform.SetParent(_upgradeModal.transform, false);
            var hRect = headerObj.AddComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0, 1);
            hRect.anchorMax = new Vector2(1, 1);
            hRect.pivot = new Vector2(0.5f, 1);
            hRect.anchoredPosition = new Vector2(0, -15);
            hRect.sizeDelta = new Vector2(-40, 45);

            var hTxt = headerObj.AddComponent<Text>();
            hTxt.font = defaultFont;
            hTxt.fontSize = 24;
            hTxt.fontStyle = FontStyle.Bold;
            hTxt.color = new Color(1f, 0.85f, 0.35f);
            hTxt.alignment = TextAnchor.MiddleLeft;
            hTxt.text = "⚡ WAREHOUSE UPGRADES";

            // Modal Cash Balance
            GameObject mCash = new GameObject("ModalCash");
            mCash.transform.SetParent(headerObj.transform, false);
            var mcRect = mCash.AddComponent<RectTransform>();
            mcRect.anchorMin = Vector2.zero;
            mcRect.anchorMax = Vector2.one;
            mcRect.sizeDelta = Vector2.zero;
            _modalCashText = mCash.AddComponent<Text>();
            _modalCashText.font = defaultFont;
            _modalCashText.fontSize = 20;
            _modalCashText.fontStyle = FontStyle.Bold;
            _modalCashText.color = new Color(0.4f, 1f, 0.6f);
            _modalCashText.alignment = TextAnchor.MiddleRight;
            _modalCashText.text = "Balance: $0";

            // 4 Upgrade Cards Container
            GameObject cardsContainer = new GameObject("Cards");
            cardsContainer.transform.SetParent(_upgradeModal.transform, false);
            var ccRect = cardsContainer.AddComponent<RectTransform>();
            ccRect.anchorMin = new Vector2(0, 0);
            ccRect.anchorMax = new Vector2(1, 1);
            ccRect.offsetMin = new Vector2(20, 60);
            ccRect.offsetMax = new Vector2(-20, -70);

            var vlg = cardsContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            for (int i = 0; i < 4; i++)
            {
                int index = i;
                GameObject card = new GameObject($"Card_{i}");
                card.transform.SetParent(cardsContainer.transform, false);
                var cdRect = card.AddComponent<RectTransform>();
                cdRect.sizeDelta = new Vector2(0, 65);
                var cImg = card.AddComponent<Image>();
                cImg.color = new Color(0.14f, 0.17f, 0.22f, 0.9f);

                // Level text
                GameObject lObj = new GameObject("Lvl");
                lObj.transform.SetParent(card.transform, false);
                var lRect = lObj.AddComponent<RectTransform>();
                lRect.anchorMin = new Vector2(0, 0.5f);
                lRect.anchorMax = new Vector2(0, 0.5f);
                lRect.pivot = new Vector2(0, 0.5f);
                lRect.anchoredPosition = new Vector2(15, 10);
                lRect.sizeDelta = new Vector2(250, 25);
                _upgradeLevelTexts[i] = lObj.AddComponent<Text>();
                _upgradeLevelTexts[i].font = defaultFont;
                _upgradeLevelTexts[i].fontSize = 17;
                _upgradeLevelTexts[i].fontStyle = FontStyle.Bold;
                _upgradeLevelTexts[i].color = Color.white;

                // Description text
                GameObject dObj = new GameObject("Desc");
                dObj.transform.SetParent(card.transform, false);
                var dRect = dObj.AddComponent<RectTransform>();
                dRect.anchorMin = new Vector2(0, 0.5f);
                dRect.anchorMax = new Vector2(0, 0.5f);
                dRect.pivot = new Vector2(0, 0.5f);
                dRect.anchoredPosition = new Vector2(15, -12);
                dRect.sizeDelta = new Vector2(280, 22);
                _upgradeDescTexts[i] = dObj.AddComponent<Text>();
                _upgradeDescTexts[i].font = defaultFont;
                _upgradeDescTexts[i].fontSize = 14;
                _upgradeDescTexts[i].color = new Color(0.7f, 0.75f, 0.82f);

                // Upgrade Button
                GameObject btnObj = new GameObject("Btn");
                btnObj.transform.SetParent(card.transform, false);
                var bRect = btnObj.AddComponent<RectTransform>();
                bRect.anchorMin = new Vector2(1, 0.5f);
                bRect.anchorMax = new Vector2(1, 0.5f);
                bRect.pivot = new Vector2(1, 0.5f);
                bRect.anchoredPosition = new Vector2(-15, 0);
                bRect.sizeDelta = new Vector2(115, 42);

                var bImg = btnObj.AddComponent<Image>();
                bImg.color = new Color(0.2f, 0.6f, 0.9f);
                _upgradeButtons[i] = btnObj.AddComponent<Button>();

                var colors = _upgradeButtons[i].colors;
                colors.normalColor = new Color(0.2f, 0.6f, 0.9f);
                colors.highlightedColor = new Color(0.3f, 0.75f, 1f);
                colors.pressedColor = new Color(0.1f, 0.45f, 0.75f);
                colors.disabledColor = new Color(0.3f, 0.35f, 0.4f, 0.5f);
                _upgradeButtons[i].colors = colors;

                GameObject btObj = new GameObject("BtnTxt");
                btObj.transform.SetParent(btnObj.transform, false);
                var btRect = btObj.AddComponent<RectTransform>();
                btRect.anchorMin = Vector2.zero;
                btRect.anchorMax = Vector2.one;
                _upgradeButtonTexts[i] = btObj.AddComponent<Text>();
                _upgradeButtonTexts[i].font = defaultFont;
                _upgradeButtonTexts[i].fontSize = 18;
                _upgradeButtonTexts[i].fontStyle = FontStyle.Bold;
                _upgradeButtonTexts[i].alignment = TextAnchor.MiddleCenter;
                _upgradeButtonTexts[i].color = Color.white;

                _upgradeButtons[i].onClick.AddListener(() => OnUpgradeClicked(index));
            }

            // Close button at bottom
            GameObject closeObj = new GameObject("CloseBtn");
            closeObj.transform.SetParent(_upgradeModal.transform, false);
            var cbRect = closeObj.AddComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(0.5f, 0);
            cbRect.anchorMax = new Vector2(0.5f, 0);
            cbRect.pivot = new Vector2(0.5f, 0);
            cbRect.anchoredPosition = new Vector2(0, 12);
            cbRect.sizeDelta = new Vector2(140, 36);

            var cbImg = closeObj.AddComponent<Image>();
            cbImg.color = new Color(0.3f, 0.35f, 0.4f);
            var closeBtn = closeObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(CloseUpgradeModal);

            GameObject ctObj = new GameObject("CTxt");
            ctObj.transform.SetParent(closeObj.transform, false);
            var cttRect = ctObj.AddComponent<RectTransform>();
            cttRect.anchorMin = Vector2.zero;
            cttRect.anchorMax = Vector2.one;
            var cTxt = ctObj.AddComponent<Text>();
            cTxt.font = defaultFont;
            cTxt.fontSize = 16;
            cTxt.fontStyle = FontStyle.Bold;
            cTxt.alignment = TextAnchor.MiddleCenter;
            cTxt.color = Color.white;
            cTxt.text = "CLOSE [ESC]";

            _upgradeModal.SetActive(false);
        }

        private void OnUpgradeClicked(int index)
        {
            if (EconomyManager.Instance == null) return;
            var em = EconomyManager.Instance;

            switch (index)
            {
                case 0: em.TryUpgradeBackpack(); break;
                case 1: em.TryUpgradeBat(); break;
                case 2: em.TryUpgradeVacuum(); break;
                case 3: em.TryUpgradeMultiplier(); break;
            }

            UpdateUpgradeModal();
        }
    }
}
