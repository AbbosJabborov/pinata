using Pinata.Gameplay;
using Pinata.Packaging;
using UnityEngine;
using UnityEngine.UI;

namespace Pinata.UI
{
    /// <summary>
    /// Lean, Inspector-wired HUD: cash counter, box fill meter, and the upgrade
    /// terminal modal. Replaces the old runtime-built HUD that was hardcoded to
    /// the basket/bat economy from a different project.
    ///
    /// Assign all fields in the Editor on a Canvas prefab - nothing here builds
    /// UI at runtime, so what you see in the Inspector is what you get in game.
    /// </summary>
    public class WarehouseHUD : MonoBehaviour
    {
        [Header("Cash")]
        [SerializeField] private Text cashText;

        [Header("Box Fill")]
        [SerializeField] private Text boxTierText;
        [SerializeField] private Image boxFillBar; // Image.type = Filled, horizontal
        [SerializeField] private Text boxFillText; // "12 / 35"

        [Header("Upgrade Terminal Modal")]
        [SerializeField] private UpgradeKiosk kiosk;
        [SerializeField] private GameObject upgradeModalRoot;
        [SerializeField] private Text modalCashText;

        [SerializeField] private Text boxTierRow;
        [SerializeField] private Button boxTierButton;

        [SerializeField] private Text toolUnlockRow;
        [SerializeField] private Button toolUnlockButton;

        [SerializeField] private Text hopperRow;
        [SerializeField] private Button hopperButton;

        [SerializeField] private Text vacuumRow;
        [SerializeField] private Button vacuumButton;

        [SerializeField] private Text multiplierRow;
        [SerializeField] private Button multiplierButton;

        private CardboardBox _box;

        private void Awake()
        {
            if (kiosk == null) kiosk = FindObjectOfType<UpgradeKiosk>();
            if (upgradeModalRoot != null) upgradeModalRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnCashChanged += HandleCashChanged;
                EconomyManager.Instance.OnUpgradePurchased += HandleUpgradePurchased;
                UpdateCashDisplay(EconomyManager.Instance.Cash);
            }

            if (kiosk != null) kiosk.OnKioskStateChanged += HandleKioskStateChanged;

            _box = CardboardBox.Instance;
            if (_box != null)
            {
                _box.OnFillChanged += HandleBoxFillChanged;
                _box.OnLevelUpgraded += HandleBoxLevelUpgraded;
                HandleBoxFillChanged(_box.CurrentFill, _box.Capacity);
                HandleBoxLevelUpgraded(_box.CurrentLevel);
            }

            if (boxTierButton != null) boxTierButton.onClick.AddListener(BuyBoxTier);
            if (toolUnlockButton != null) toolUnlockButton.onClick.AddListener(BuyToolUnlock);
            if (hopperButton != null) hopperButton.onClick.AddListener(BuyHopper);
            if (vacuumButton != null) vacuumButton.onClick.AddListener(BuyVacuum);
            if (multiplierButton != null) multiplierButton.onClick.AddListener(BuyMultiplier);
        }

        private void OnDisable()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnCashChanged -= HandleCashChanged;
                EconomyManager.Instance.OnUpgradePurchased -= HandleUpgradePurchased;
            }

            if (kiosk != null) kiosk.OnKioskStateChanged -= HandleKioskStateChanged;

            if (_box != null)
            {
                _box.OnFillChanged -= HandleBoxFillChanged;
                _box.OnLevelUpgraded -= HandleBoxLevelUpgraded;
            }

            if (boxTierButton != null) boxTierButton.onClick.RemoveListener(BuyBoxTier);
            if (toolUnlockButton != null) toolUnlockButton.onClick.RemoveListener(BuyToolUnlock);
            if (hopperButton != null) hopperButton.onClick.RemoveListener(BuyHopper);
            if (vacuumButton != null) vacuumButton.onClick.RemoveListener(BuyVacuum);
            if (multiplierButton != null) multiplierButton.onClick.RemoveListener(BuyMultiplier);
        }

        // --- Persistent HUD ---

        private void HandleCashChanged(int newBalance, int delta)
        {
            UpdateCashDisplay(newBalance);
        }

        private void UpdateCashDisplay(int balance)
        {
            if (cashText != null) cashText.text = $"${balance:N0}";
            if (modalCashText != null) modalCashText.text = $"${balance:N0}";
        }

        private void HandleBoxFillChanged(int current, int max)
        {
            if (boxFillBar != null && max > 0) boxFillBar.fillAmount = (float)current / max;
            if (boxFillText != null) boxFillText.text = $"{current} / {max}";
        }

        private void HandleBoxLevelUpgraded(int newLevel)
        {
            if (boxTierText != null) boxTierText.text = $"Box — Tier {newLevel}";
        }

        // --- Upgrade Modal ---

        private void HandleKioskStateChanged(bool isOpen)
        {
            if (upgradeModalRoot != null) upgradeModalRoot.SetActive(isOpen);
            if (isOpen) RefreshModal();
        }

        private void HandleUpgradePurchased(string upgradeName, int newLevel)
        {
            RefreshModal();
        }

        private void RefreshModal()
        {
            var em = EconomyManager.Instance;
            if (em == null) return;

            UpdateCashDisplay(em.Cash);

            SetRow(boxTierRow, boxTierButton, "Box Tier", em.BoxTierLevel, em.GetBoxTierUpgradeCost());

            string nextTool = em.GetNextToolName();
            int toolCost = em.GetToolUnlockCost();
            if (toolUnlockRow != null)
            {
                toolUnlockRow.text = nextTool != null
                    ? $"Unlock {nextTool} — ${toolCost}"
                    : "All tools unlocked";
            }
            if (toolUnlockButton != null)
            {
                toolUnlockButton.interactable = toolCost >= 0 && em.Cash >= toolCost;
                toolUnlockButton.gameObject.SetActive(nextTool != null);
            }

            SetRow(hopperRow, hopperButton, "Hopper Capacity", em.HopperLevel, em.GetHopperUpgradeCost());
            SetRow(vacuumRow, vacuumButton, "Vacuum Power", em.VacuumLevel, em.GetVacuumUpgradeCost());
            SetRow(multiplierRow, multiplierButton, "Candy Value", em.ValueMultiplierLevel, em.GetMultiplierUpgradeCost());
        }

        private void SetRow(Text row, Button button, string label, int level, int cost)
        {
            if (row != null)
            {
                row.text = cost >= 0 ? $"{label} Lv {level} — ${cost}" : $"{label} Lv {level} (MAX)";
            }
            if (button != null)
            {
                button.interactable = cost >= 0 && EconomyManager.Instance != null && EconomyManager.Instance.Cash >= cost;
                button.gameObject.SetActive(cost >= 0);
            }
        }

        private void BuyBoxTier() { EconomyManager.Instance?.TryUpgradeBoxTier(); RefreshModal(); }
        private void BuyToolUnlock() { EconomyManager.Instance?.TryUnlockNextTool(); RefreshModal(); }
        private void BuyHopper() { EconomyManager.Instance?.TryUpgradeHopper(); RefreshModal(); }
        private void BuyVacuum() { EconomyManager.Instance?.TryUpgradeVacuum(); RefreshModal(); }
        private void BuyMultiplier() { EconomyManager.Instance?.TryUpgradeMultiplier(); RefreshModal(); }
    }
}
