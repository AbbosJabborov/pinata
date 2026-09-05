# Milestone 2: Collecting, Basket Sorting, Selling & Upgrades — Implementation Plan

Our game is an **incremental item collecting and organizing game**:
```
[ 1. Smash Piñata ] ──> [ 2. Collect Candies ] ──> [ 3. Sort into Baskets ] ──> [ 4. Sell Filled Baskets ] ──> [ 5. Buy Upgrades ] ──> Repeat!
```

In this milestone, we implement the complete sorting, storing, selling, and upgrade loop.

---

## User Review Required

> [!IMPORTANT]
> - **6 Color-Coded Candy Baskets**: Placed on a dedicated Sorting & Packing table in the warehouse, one for each candy type: **Blue, Green, Orange, Pink, Yellow, Purple**.
> - **Deposit Mechanic**: When the player approaches a basket matching carried candies, a prompt appears (e.g. `[E] Deposit Blue Candies (8/15)`). Holding or pressing `E` deposits them with tactile visual feedback (candies visibly pile into the basket, counter rings up).
> - **Selling & Delivery Station**: Filled baskets trigger a `[F] Sell Basket` prompt (or can be sold via a nearby Shipping Conveyor / Pallet), converting sorted candy batches into **Cash ($)** with a completion bonus!
> - **Vacuum (Shop-Vac) Tool**: Added to Slot 4 (press `5` or scroll) so players can rapidly vacuum large spills off the floor into their backpack.
> - **Upgrade Kiosk**: A warehouse upgrade board where players spend earned cash on:
>   1. **Backpack Capacity** (30 -> 60 -> 120 -> 250 candies)
>   2. **Vacuum Suction Power & Range**
>   3. **Bat Damage** (25 -> 50 -> 100 dmg per hit)
>   4. **Candy Value Multiplier** (+20%, +50%, +100% payout)

---

## Proposed Architecture & Changes

### 1. Economy & Inventory
#### [NEW] [`PlayerInventory.cs`](file:///Users/abbos/projects/pinata/Assets/Scripts/Player/PlayerInventory.cs)
- Tracks carried candies per `CandyType` (Blue, Green, Orange, Pink, Yellow, Purple).
- Tracks `MaxCapacity` (upgradeable via `EconomyManager`).
- Methods: `TryAddCandy()`, `RemoveCandies(CandyType, count)`, `GetCount(CandyType)`, `TotalCount`.
- Events: `OnInventoryChanged`, `OnInventoryFull`.

#### [NEW] [`EconomyManager.cs`](file:///Users/abbos/projects/pinata/Assets/Scripts/Gameplay/EconomyManager.cs)
- Singleton managing player **Cash ($)** and **Upgrades**.
- Upgrade definitions:
  - Backpack Capacity (Levels 1–5)
  - Vacuum Suction (Levels 1–5)
  - Bat Strength (Levels 1–5)
  - Candy Price Multiplier (Levels 1–5)
- Events: `OnCashChanged(int newBalance)`, `OnUpgradePurchased(string upgradeId, int newLevel)`.

---

### 2. Candy Baskets & Sorting System
#### [NEW] [`CandyBasket.cs`](file:///Users/abbos/projects/pinata/Assets/Scripts/Gameplay/CandyBasket.cs)
- Attached to each of the 6 sorting baskets in the warehouse.
- Configured with target `CandyType`, `targetCapacity` (e.g. 10–15 candies), and base payout value.
- Visual fill representation:
  - Color-accented crate mesh with a dynamic 3D fill mesh (or miniature candy stack) that rises as it fills.
  - Floating 3D canvas / worldspace label showing: `Blue Mint: 7 / 10` (turns golden `READY TO SELL!` when full).
- Interaction:
  - Trigger zone detects player.
  - Press `E` to deposit matching carried candies from backpack into basket one-by-one or in bulk with audio pop effects.
  - Press `F` to sell the filled basket -> awards cash, triggers celebratory coin sound & particle confetti, resets basket to 0.

---

### 3. Collection Tools: Vacuum (Shop-Vac)
#### [NEW] [`VacuumTool.cs`](file:///Users/abbos/projects/pinata/Assets/Scripts/Tools/VacuumTool.cs)
- Implements `ITool` (Slot 4: "Vacuum").
- First-person nozzle and hose model.
- Holding Left Click casts a suction cone forward:
  - Candies in cone get pulled towards nozzle along an inward vortex.
  - Candies reaching nozzle (< 0.45m) shrink, play slurp audio, deposit into `PlayerInventory`, and return to `CandyPool`.
  - Inward wind particle effect while vacuuming.
- Respects backpack capacity: stops suction when backpack is full and shows warning.

---

### 4. Upgrade Kiosk & Shop UI
#### [NEW] [`UpgradeKiosk.cs`](file:///Users/abbos/projects/pinata/Assets/Scripts/Gameplay/UpgradeKiosk.cs)
- Interactive terminal / board in the warehouse.
- When player approaches and presses `E`: opens a clean, modern Upgrade UI menu.
- Buttons to purchase next tier of:
  - 🎒 Backpack Capacity
  - 🌪️ Vacuum Suction
  - 🏏 Bat Damage
  - 💰 Candy Value Multiplier
- Updates stats in real time.

---

### 5. Warehouse HUD & World Overlays
#### [NEW] [`WarehouseHUD.cs`](file:///Users/abbos/projects/pinata/Assets/Scripts/UI/WarehouseHUD.cs)
- **Top-Right**: Cash counter `$ 0` (with green `+$` floating animations when selling).
- **Bottom-Center**: Backpack capacity gauge (`🎒 12 / 30 Candies`) with individual candy color icons.
- **Center Prompt**: Context-sensitive interaction prompt (e.g. `[E] Deposit Orange Candies`, `[F] Sell Basket ($80)`, `[E] Open Upgrade Kiosk`).
- **Hotbar**: Updated to show all 5 tools (1: Bat, 2: Broom, 3: Hands, 4: Slicer, 5: Vacuum).

#### [MODIFY] [`SceneSetupHelper.cs`](file:///Users/abbos/projects/pinata/Assets/Scripts/Editor/SceneSetupHelper.cs)
- Build the **Sorting & Packing Station** along the warehouse wall:
  - Solid wooden packing table.
  - 6 color-coded baskets matching the 6 candies (Blue, Green, Orange, Pink, Yellow, Purple).
  - Shipping crate / delivery zone for selling.
  - Upgrade terminal on the wall.
- Add `Slot_Vacuum` to player tool socket.
- Wire `PlayerInventory`, `EconomyManager`, and `WarehouseHUD`.

---

## Verification Plan

### Automated / Editor Verification
1. Run `SceneSetupHelper.SetupPinataScene()` to generate the new sorting station, baskets, upgrade terminal, vacuum slot, and HUD.
2. Enter Play Mode:
   - **Smash Piñata**: Hit piñata with Bat; candies erupt onto floor.
   - **Vacuum / Collect**: Equip Vacuum (`5`), hold Left Click to suck up candies into backpack. Verify backpack capacity limits.
   - **Sort into Baskets**: Walk to the sorting table; approach matching baskets, press `E` to deposit. Verify visual fill and basket count increase.
   - **Sell Baskets**: Fill a basket to capacity, press `F` to sell. Verify cash increases and audio/particles play.
   - **Upgrade Terminal**: Walk to Upgrade Kiosk, press `E`, purchase Backpack upgrade with earned cash, verify capacity expands.
3. Capture in-game screenshots of the sorting table, vacuum suction, basket deposit, and upgrade menu.
