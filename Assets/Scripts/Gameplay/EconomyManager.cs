using System;
using Pinata.Player;
using Pinata.Tools;
using UnityEngine;

namespace Pinata.Gameplay
{
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        [Header("Starting Funds")]
        [SerializeField] private int initialCash = 0;

        private int _cash = 0;

        // Upgrade Levels (1-indexed)
        public int BackpackLevel { get; private set; } = 1;
        public int BatLevel { get; private set; } = 1;
        public int VacuumLevel { get; private set; } = 1;
        public int ValueMultiplierLevel { get; private set; } = 1;

        public int Cash => _cash;

        public event Action<int, int> OnCashChanged; // (newBalance, delta)
        public event Action<string, int> OnUpgradePurchased; // (upgradeName, newLevel)

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            _cash = initialCash;
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

        // --- Backpack Upgrades ---
        public static readonly int[] BackpackCapacities = { 30, 60, 120, 250, 500 };
        public static readonly int[] BackpackCosts = { 75, 200, 500, 1200 };

        public int GetBackpackUpgradeCost()
        {
            if (BackpackLevel > BackpackCosts.Length) return -1; // Maxed
            return BackpackCosts[BackpackLevel - 1];
        }

        public bool TryUpgradeBackpack()
        {
            int cost = GetBackpackUpgradeCost();
            if (cost < 0 || !TrySpendCash(cost)) return false;

            BackpackLevel++;
            ApplyBackpackUpgrade();
            OnUpgradePurchased?.Invoke("Backpack", BackpackLevel);
            return true;
        }

        private void ApplyBackpackUpgrade()
        {
            if (PlayerInventory.Instance != null)
            {
                int capIndex = Mathf.Clamp(BackpackLevel - 1, 0, BackpackCapacities.Length - 1);
                PlayerInventory.Instance.MaxCapacity = BackpackCapacities[capIndex];
            }
        }

        // --- Bat Upgrades ---
        public static readonly float[] BatDamages = { 25f, 45f, 75f, 125f, 200f };
        public static readonly int[] BatCosts = { 50, 150, 400, 1000 };

        public int GetBatUpgradeCost()
        {
            if (BatLevel > BatCosts.Length) return -1;
            return BatCosts[BatLevel - 1];
        }

        public bool TryUpgradeBat()
        {
            int cost = GetBatUpgradeCost();
            if (cost < 0 || !TrySpendCash(cost)) return false;

            BatLevel++;
            ApplyBatUpgrade();
            OnUpgradePurchased?.Invoke("Bat", BatLevel);
            return true;
        }

        private void ApplyBatUpgrade()
        {
            var bat = FindAnyObjectByType<BatTool>();
            if (bat != null)
            {
                int idx = Mathf.Clamp(BatLevel - 1, 0, BatDamages.Length - 1);
                bat.Damage = BatDamages[idx];
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
            var vac = FindAnyObjectByType<VacuumTool>();
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
            ApplyBackpackUpgrade();
            ApplyBatUpgrade();
            ApplyVacuumUpgrade();
        }
    }
}
