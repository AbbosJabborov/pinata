# Piñata Liquidation Depot — Canvas & Menu Setup Guide

Companion to `PROJECT_SETUP.md`. Covers the Canvas hierarchy: persistent gameplay
HUD, the upgrade kiosk's popup panel, the pause menu, and how cursor lock / time
freeze are shared cleanly between all three so they don't fight each other.

**The key piece already in the codebase:** `Core/CursorModeManager.cs` is a
reference-counted cursor lock — any panel calls `RequestUnlock(this)` on open and
`ReleaseUnlock(this)` on close, and the cursor only re-locks once *every* requester
has released. This is what stops closing the kiosk from re-locking the mouse while
the pause menu is still open on top of it. Every menu below routes through it —
never set `Cursor.lockState` directly.

---

## 1. Canvas Hierarchy

Two Canvases, kept separate mainly because the pause menu needs to render — and
accept input — on top of the kiosk modal if both somehow overlap, and it's simpler
to reason about as its own root than as a sibling deep inside the HUD tree.

```
GameplayCanvas                          [Canvas: Screen Space - Overlay]
│                                        [CanvasScaler, GraphicRaycaster]
│
├── HUD                                 (always active during play)
│   ├── CashPanel                       [Text: cashText]
│   ├── BoxFillPanel                    [Text: boxTierText, Image: boxFillBar (Filled), Text: boxFillText]
│   ├── ToolWheelRoot                   [ToolWheel.cs]
│   │   └── ItemsCircle                 (rotates)
│   │       ├── Icon_Hand
│   │       ├── Icon_Broom
│   │       └── Icon_Bat
│   ├── InteractPromptRoot              [InteractPromptUI.cs]
│   └── Crosshair                       [CrosshairController.cs]
│
├── UpgradeModal                        [WarehouseHUD.upgradeModalRoot — starts INACTIVE]
│   ├── DimBackground                   (full-screen semi-transparent Image, blocks
│   │                                    raycasts to HUD/world behind it)
│   ├── ModalCashText
│   ├── Row_BoxTier      [Text + Button]
│   ├── Row_ToolUnlock   [Text + Button]
│   ├── Row_Hopper       [Text + Button]
│   ├── Row_Vacuum       [Text + Button]
│   ├── Row_Multiplier   [Text + Button]
│   └── CloseButton                     (optional — Escape already closes it)
│
└── WarehouseHUD                        [WarehouseHUD.cs component lives here,
                                          references everything above by Inspector drag]

PauseCanvas                             [Canvas: Screen Space - Overlay, higher
│                                         Sort Order than GameplayCanvas so it always
│                                         draws on top]
└── PausePanel                          [PauseMenu.panelRoot — starts INACTIVE]
    ├── DimBackground
    ├── ResumeButton                    [PauseMenu.resumeButton]
    └── QuitButton                      [PauseMenu.quitButton]
```

---

## 2. Component Wiring

### `WarehouseHUD` (on the `WarehouseHUD` object)
- `cashText`, `boxTierText`, `boxFillBar`, `boxFillText` → drag from `HUD/`.
- `kiosk` → the scene's `UpgradeKiosk` (a Manager object, not part of the Canvas).
- `upgradeModalRoot` → `UpgradeModal`.
- Each `Row_*`'s `Text` and `Button` → the matching `boxTierRow`/`boxTierButton` etc.
  fields. Rows auto-hide (`gameObject.SetActive(false)`) once maxed, and the Tool
  Unlock row hides once all 3 slots are unlocked.

### `UpgradeKiosk` (Manager object, from `PROJECT_SETUP.md` §3)
- Already calls `CursorModeManager.RequestUnlock(this)` / `ReleaseUnlock(this)` on
  open/close — nothing to wire here for cursor behavior, it's automatic.
- Does **not** freeze `Time.timeScale`. This is deliberate: piñatas keep swinging
  and candy keeps settling while you shop, same as most incremental-game shops. If
  you want the world to pause during the kiosk too, that's a one-line change
  (`Time.timeScale = 0f` in `OpenKiosk()`), but it wasn't asked for — flag it if you
  want that instead.

### `PauseMenu` (on the `PausePanel` object, or anywhere — it doesn't need to be a
child of the panel it controls)
- `panelRoot` → `PausePanel`.
- `resumeButton` → `ResumeButton`, `quitButton` → `QuitButton`.
- Already freezes `Time.timeScale = 0f` on pause and restores it on resume, and
  already routes cursor unlock through `CursorModeManager`.
- Escape is handled by polling in `Update()` — no Input Action binding needed for
  pause/resume specifically, though `PROJECT_SETUP.md`'s `Cancel` action can still
  exist for consistency if other UI wants it later.

---

## 3. Who owns Escape

Both `PauseMenu` and `UpgradeKiosk` poll Escape directly, so they need to yield to
each other cleanly — this is already handled in the current scripts:
- `UpgradeKiosk.Update()` only closes itself on Escape if `!PauseMenu.IsPaused`.
- `PauseMenu.Update()` only opens on Escape if the kiosk isn't the thing currently
  open (`UpgradeKiosk.Instance.IsOpen`), so hitting Escape closes the kiosk first
  rather than pausing underneath it.

Net effect: Escape closes whichever menu is topmost, never both, never neither.

---

## 4. Cursor & Time behavior summary

| State                  | Cursor            | Time.timeScale | Camera look | Movement |
|------------------------|--------------------|-----------------|-------------|----------|
| Normal gameplay        | Locked, hidden      | 1               | ✅ | ✅ |
| Kiosk open             | Unlocked, visible   | 1 (unchanged)   | ❌ | ❌ |
| Paused                 | Unlocked, visible   | 0               | ❌ | ❌ |

Look and movement already check `CursorModeManager.IsUnlocked` in
`FirstPersonPlayer.cs` and stop correctly in both menu states.

---

## 5. Gaps to close before this is airtight

Two things aren't wired to `CursorModeManager` yet and should be, or clicking a
kiosk/pause button can double as a world interaction underneath it:

1. **`PlayerInteractor.cs`** doesn't check `CursorModeManager.IsUnlocked` — add a
   guard at the top of its raycast `Update()` so it stops raycasting/holding
   progress entirely while any menu owns the cursor. Otherwise clicking "Buy" on
   the kiosk panel can also register as a world click if the raycast happens to
   line up with something interactable behind the UI.
2. **`CrosshairController.cs`** and **`InteractPromptUI.cs`** don't hide while a
   menu is open — minor visually (crosshair floating over a modal), but worth a
   `CursorModeManager.IsUnlocked` check in each so they disable themselves while
   any panel is up.

Neither breaks the current build, but both are one-line `if (CursorModeManager.IsUnlocked) return;` additions, same pattern already used in `FirstPersonPlayer.cs` — worth doing in the same pass as this Canvas setup rather than as a separate bug later.
