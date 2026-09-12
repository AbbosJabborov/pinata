using System;
using Pinata.Packaging;
using Pinata.Player;
using Pinata.Tools;
using UnityEngine;

namespace Pinata.Gameplay
{
    /// <summary>
    /// Central economy + progression tracker for the MVP loop:
    /// cash -> box tier -> tool unlocks (Broom -> Windblower -> Vacuum) -> hopper/vacuum stats -> payout multiplier.
    /// </summary>
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        [Header("Starting Funds")]
        [SerializeField] private int initialCash = 0;

        [Header("Direct Scene Links (Optional)")]
        [SerializeField] private CardboardBox cardboardBox;
        [SerializeField] private VacuumTool vacuumTool;
        [SerializeField] private ToolManager toolManager;

        private int _cash = 0;

        // Upgrade Levels (1-indexed)
        public int BoxTierLevel { get; private set; } = 1;
        public int HopperLevel { get; private set; } = 1;      // PlayerInventory capacity (vacuum hopper)
        public int VacuumLevel { get; private set; } = 1;      // Vacuum reach/force
        public int ValueMultiplierLevel { get; private set; } = 1;
        public int ToolUnlockCount { get; private set; } = 1;  // 1 = Hand only, 2 = +Broom, 3 = +Windblower, 4 = +Vacuum

        public int Cash => _cash;

        public event Action<int, int> OnCashChanged; // (newBalance, delta)
        public event Action<string, int> OnUpgradePurchased; // (upgradeName, newLevel)

        private void Awake()
        {
            if (Instance == null) Instance = this;
            _cash = initialCash;

            if (cardboardBox == null) cardboardBox = CardboardBox.Instance;
            if (vacuumTool == null) vacuumTool = VacuumTool.Instance;
            if (toolManager == null) toolManager = ToolManager.Instance;
        }

        private void Start()
        {
            ApplyAllUpgrades();
            OnCashChanged?.Invoke(_cash, 0);
        }

        public void AddCash(int amount)
        {
            if (amount <= 0) return;
            _cash += amount;
            OnCashChanged?.Invoke(_cash, amount);
        }

        public bool TrySpendCash(int amount)
        {
            if (amount <= 0) return true;
            if (_cash < amount) return false;

            _cash -= amount;
            OnCashChanged?.Invoke(_cash, -amount);
            return true;
        }

        // --- Box Tier Upgrades (bounce + scale, matches CardboardBox.UpgradeLevel) ---
        public static readonly int[] BoxTierCosts = { 80, 220, 550, 1300 };

        public int GetBoxTierUpgradeCost()
        {
            if (BoxTierLevel > BoxTierCosts.Length) return -1; // Maxed
            return BoxTierCosts[BoxTierLevel - 1];
        }

        public bool TryUpgradeBoxTier()
        {
            int cost = GetBoxTierUpgradeCost();
            if (cost < 0 || !TrySpendCash(cost)) return false;

            BoxTierLevel++;
            if (cardboardBox == null) cardboardBox = CardboardBox.Instance;
            cardboardBox?.UpgradeLevel();
            OnUpgradePurchased?.Invoke("BoxTier", BoxTierLevel);
            return true;
        }

        // --- Tool Unlocks (sequential: Hand -> Broom -> Bat) ---
        // 3-slot loadout for now (Hand is always free/equipped). Windblower and Vacuum
        // are parked in Tools/Future until the roster grows past 3 and a proper
        // loadout-selection screen exists to let the player choose which to bring.
        public static readonly string[] ToolUnlockNames = { "Broom", "Bat" };
        public static readonly int[] ToolUnlockCosts = { 60, 180 };

        public int GetToolUnlockCost()
        {
            if (ToolUnlockCount > ToolUnlockCosts.Length) return -1; // All unlocked
            return ToolUnlockCosts[ToolUnlockCount - 1];
        }

        public string GetNextToolName()
        {
            if (ToolUnlockCount > ToolUnlockNames.Length) return null;
            return ToolUnlockNames[ToolUnlockCount - 1];
        }

        public bool TryUnlockNextTool()
        {
            int cost = GetToolUnlockCost();
            if (cost < 0 || !TrySpendCash(cost)) return false;

            ToolUnlockCount++;
            if (toolManager == null) toolManager = ToolManager.Instance;
            toolManager?.SetUnlockedCount(ToolUnlockCount);
            OnUpgradePurchased?.Invoke("ToolUnlock", ToolUnlockCount);
            return true;
        }

        // --- Hopper Capacity Upgrades (PlayerInventory.MaxCapacity) ---
        public static readonly int[] HopperCapacities = { 30, 60, 120, 250, 500 };
        public static readonly int[] HopperCosts = { 75, 200, 500, 1200 };

        public int GetHopperUpgradeCost()
        {
            if (HopperLevel > HopperCosts.Length) return -1;
            return HopperCosts[HopperLevel - 1];
        }

        public bool TryUpgradeHopper()
        {
            int cost = GetHopperUpgradeCost();
            if (cost < 0 || !TrySpendCash(cost)) return false;

            HopperLevel++;
            ApplyHopperUpgrade();
            OnUpgradePurchased?.Invoke("Hopper", HopperLevel);
            return true;
        }

        private void ApplyHopperUpgrade()
        {
            if (PlayerInventory.Instance != null)
            {
                int capIndex = Mathf.Clamp(HopperLevel - 1, 0, HopperCapacities.Length - 1);
                PlayerInventory.Instance.MaxCapacity = HopperCapacities[capIndex];
            }
        }

        // --- Vacuum Upgrades ---
        public static readonly float[] VacuumReaches = { 3.8f, 5.0f, 6.5f, 8.5f };
        public static readonly float[] VacuumForces = { 12f, 18f, 26f, 38f };
        public static readonly int[] VacuumCosts = { 60, 180, 450 };

        public int GetVacuumUpgradeCost()
        {
            if (VacuumLevel > VacuumCosts.Length) return -1;
            return VacuumCosts[VacuumLevel - 1];
        }

        public bool TryUpgradeVacuum()
        {
            int cost = GetVacuumUpgradeCost();
            if (cost < 0 || !TrySpendCash(cost)) return false;

            VacuumLevel++;
            ApplyVacuumUpgrade();
            OnUpgradePurchased?.Invoke("Vacuum", VacuumLevel);
            return true;
        }

        private void ApplyVacuumUpgrade()
        {
            var vac = vacuumTool != null ? vacuumTool : VacuumTool.Instance;
            if (vac != null)
            {
                int idx = Mathf.Clamp(VacuumLevel - 1, 0, VacuumReaches.Length - 1);
                vac.ReachDistance = VacuumReaches[idx];
                vac.SuctionForce = VacuumForces[idx];
            }
        }

        // --- Multiplier Upgrades ---
        public static readonly float[] ValueMultipliers = { 1.0f, 1.25f, 1.6f, 2.2f };
        public static readonly int[] MultiplierCosts = { 100, 300, 800 };

        public int GetMultiplierUpgradeCost()
        {
            if (ValueMultiplierLevel > MultiplierCosts.Length) return -1;
            return MultiplierCosts[ValueMultiplierLevel - 1];
        }

        public float CurrentPriceMultiplier
        {
            get
            {
                int idx = Mathf.Clamp(ValueMultiplierLevel - 1, 0, ValueMultipliers.Length - 1);
                return ValueMultipliers[idx];
            }
        }

        public bool TryUpgradeMultiplier()
        {
            int cost = GetMultiplierUpgradeCost();
            if (cost < 0 || !TrySpendCash(cost)) return false;

            ValueMultiplierLevel++;
            OnUpgradePurchased?.Invoke("Multiplier", ValueMultiplierLevel);
            return true;
        }

        public void ApplyAllUpgrades()
        {
            ApplyHopperUpgrade();
            ApplyVacuumUpgrade();
            if (toolManager == null) toolManager = ToolManager.Instance;
            toolManager?.SetUnlockedCount(ToolUnlockCount);
        }
    }
}
