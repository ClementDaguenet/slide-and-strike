# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**Slide & Strike** — Unity 2022.3+ physics-based arcade game (PC/Windows). A penguin slides down a procedurally-generated icy track and knocks over "Petits Filous" yogurt tubes. C# scripts live in `Assets/Scripts/`. The single playable scene is `Assets/Scenes/SampleScene.unity`.

## Development workflow

- Open the project in Unity Hub (Unity 2022.3+).
- Run `git lfs pull` after cloning — 3D assets are tracked with Git LFS.
- Play-test via the Unity Editor Play button. There is no standalone build script.
- **Never modify `Assets/Scenes/SampleScene.unity` directly** for feature work — use a prefab or your own sandbox scene, then integrate into the main scene.
- One feature branch per major feature; commit messages use imperative style (`Add: ...`, `Fix: ...`).

## Architecture

### Execution order contract

Scripts use `[DefaultExecutionOrder]` to sequence physics updates:

| Order | Script | Responsibility |
|-------|--------|----------------|
| −50 | `CurvedIceTrack` | Builds track mesh before everything else (also runs in editor) |
| 50 | `PenguinSteer` | Reads steer input, rotates penguin to face direction of travel |
| 80 | `PenguinFallRecover` | Teleports penguin back onto ice if it falls below the track bounds |
| 100 | `PenguinSlideDrive` | Applies slope gravity, jump/dive, steering-assist forces |
| 120 | `PenguinPowerUpController` | Manages power-up queue and activates effects |
| 200 | `SpeedHud` | Bootstraps `PauseMenu` + `StartMenu` if absent; renders speed/score overlay |
| 450 | `StartMenu` | Freezes `Time.timeScale` on load until player clicks "Jouer" |
| 500 | `PauseMenu` | Handles Esc and `BottleScore.Finished` to show/hide pause overlay |

### Player (penguin) components

All components live on the same penguin `GameObject`:

- **`PenguinBodyCollider`** — Runs on `Awake` to build a `CapsuleCollider` from the mesh bounds and configure `Rigidbody` damping. Replace or regenerate the collider by calling `RebuildCapsuleFromMesh()`.
- **`PenguinSlideDrive`** — Core physics. Uses a 3-sample averaged raycast to determine ground normal and whether the penguin is grounded. Applies slope-following acceleration, jump (`Space`), and mid-air dive (`Space` again). Exposes `PowerUpAccelerationMultiplier` and `InputSign` for power-up use.
- **`PenguinSteer`** — Maintains a smoothed `_surfaceFlatForward` direction. Rotates the Rigidbody toward that direction each `FixedUpdate`. `InputSign` is flipped by the clone power-up for mirror behaviour. `ReadSteerInput()` is `public static` and also called by `PenguinSlideDrive`.
- **`PenguinCapsulePlacement`** (static utility) — Converts a capsule's world bottom point into a pivot position for snapping to ground surface.
- **`PenguinFallRecover`** — Looks for a `GameObject` named `"Slope"` to find the `MeshCollider`. Saves last on-track position each physics frame and teleports there if Y drops below `trackBounds.min.y − margin`.

### Power-up system

`PenguinPowerUpType` enum (color = type):

| Enum | Color | Effect component |
|------|-------|-----------------|
| `Red` | Red | `PenguinSpeedPowerUp` — boosts `PowerUpAccelerationMultiplier` |
| `Blue` | Blue | `PenguinGiantPowerUp` — scales penguin up |
| `Green` | Green | `PenguinClonePowerUp` — spawns `PenguinMirrorClone` |
| `Gray` | Gray | `PenguinMagnetPowerUp` — attracts nearby collectibles |
| `Pink` | Pink | `PenguinHeavyShieldPowerUp` — blocks `FanWindZone` forces |

`PenguinPowerUpController` maintains a queue (max 3). Press Shift to consume the front of the queue. Each power-up has an `Apply()` / `Clear()` pair and is auto-added via `GetOrAdd<T>()`. The `Changed` event drives the HUD.

`PenguinMirrorClone` — kinematic clone that mirrors the source penguin's position via `CurvedIceTrack.TryMirrorPose()`. Its `SourcePowerUps` property lets `KnockableBottle` credit the originating player.

### Track system

`CurvedIceTrack` (`[ExecuteAlways]`) procedurally builds the ice mesh from:
- An initial straight with a vertical drop
- N legs, each consisting of a straight segment followed by an arc (left/right, optionally randomized)

`CurvedIceTrackPath` (static) provides the pure-geometry helpers `AppendStraight` / `AppendArc` that output `mids[]` (centerline) and `rights[]` (per-point right vectors). The mesh generation in `CurvedIceTrack` uses these arrays to extrude walls and banks.

`CurvedIceTrack` holds a static `_activeTrack` reference. Call `CurvedIceTrack.IsNearEnd(pos, 0.985f)` or `CurvedIceTrack.TryMirrorPose(...)` from anywhere — both are guarded null-safe.

`TrackFinishTrigger` fires `BottleScore.Finish()` when the penguin (not a clone) enters the trigger AND `IsNearEnd` returns true. This prevents false finishes from mid-track colliders.

### Scoring

`BottleScore` is a static class (no `MonoBehaviour`) with `Count`, `IsFinished`, `Changed`, and `Finished` events. Calling `Finish()` is idempotent.

`KnockableBottle` starts kinematic with trigger colliders. On first valid hit:
1. Switches to dynamic physics (`ActivatePhysics`)
2. Calls `BottleScore.AddOne()`
3. Sets `_chainEnabled = true` — subsequent bottles hit by this one also score if relative velocity exceeds `chainScoreVelocityThreshold`.

### UI

All UI is built entirely in code — no UGUI prefabs or scene canvases. Each UI MonoBehaviour creates its own `Canvas`, `CanvasScaler`, and elements via `AddComponent` in `Awake`/`BuildUi()`. `SpeedHud` bootstraps `PauseMenu` and `StartMenu` with `EnsurePauseMenu()` / `EnsureStartMenu()`, so attaching `SpeedHud` to any object is sufficient. Logo texture is loaded from `Resources/UI/slide-and-strike-logo`.

## Controls reference

| Action | Key |
|--------|-----|
| Steer | Q / A / Left arrow, D / Right arrow |
| Forward nudge (idle) | Z / W / Up arrow |
| Jump / Dive | Space |
| Activate power-up | Shift |
| Pause | Esc |
| Reset position | R |
| Cycle penguin color | O |
