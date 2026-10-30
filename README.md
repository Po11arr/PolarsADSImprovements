# Polar's ADS Improvements

Aim-down-sights tuning for Space Engineers hand weapons, CoD / Battlefield style. Client plugin for [Pulsar](https://github.com/SpaceGT/Pulsar).

**Multiplayer:** no server install. Only your own view and mouse speed change. The server receives your resulting aim direction, as in vanilla.

## What it does

- **FOV-based sensitivity.** Vanilla slows ironsight look speed by a flat 0.6x whatever your FOV. This plugin uses monitor distance matching instead: `atan(c * tan(zoomFov/2)) / atan(c * tan(hipFov/2))`, with `c` = 1.33 by default (the CoD / Battlefield default). It follows the live FOV, including during the zoom transition.
- **Per-weapon zoom.** One magnification per hand weapon, relative to your FOV. The list comes from the loaded definitions, so modded guns appear too. Vanilla starting values: pistols (S-10, S-20) 1.5x, launchers (RO-1, PRO-1) 1.75x, MR-8P 3x. Everything else follows Default zoom (2x).
- **Smooth zoom.** Eased (smoothstep) zoom in and out, 0.15 s by default, instead of vanilla's linear 0.075 s.

## Keys

None of its own. Aim with your usual key. Hold or toggle aim is the vanilla option in Options > Game.

## Settings

Pulsar plugin list > Polar's ADS Improvements > Settings.

| Option | Default | Range |
|---|---|---|
| FOV-based sensitivity | on | off = vanilla 0.6x |
| Monitor distance coefficient | 1.33 | 0 (zoom ratio) to 2 |
| ADS multiplier | 1.00 | 0.1 to 2 |
| Smooth zoom | on | off = vanilla linear |
| Zoom time (s) | 0.15 | 0.05 to 0.4 |
| Per-weapon zoom | on | off = vanilla fixed 40 degrees |
| Default zoom (x) | 2.00 | 1 to 6 |
| One row per weapon (x) | starting value or default | 1 to 6 |

Saved to `%AppData%\SpaceEngineers\Storage\PolarsADSImprovements.cfg`, created on first run. Settings from the old `AdsSensitivity.cfg` (version 1.1.1 and earlier) are carried over once; the old file is left in place. A damaged file is ignored (defaults are used) and left in place. Delete it to reset.

## Compatibility

- [Polar's PVP Lean](https://github.com/Po11arr/PolarsPVPLean) also patches `MyCharacter.MoveAndRotate`, but only changes strafe and roll input. They work together.
- Scrollable FOV and Zoom lower the camera FOV and scale the mouse themselves. This plugin takes that FOV as your hip FOV, so their zoom stacks on top of ADS. No shared patch targets.
- No shared patch targets with Viktor Ferenczi's plugins, Head Tracking or No Forced Camera Zoom.

## Known issues

- Modded guns join the per-weapon list once a world that loads them is open.
- Vanilla starting values are matched on model numbers in the weapon name (MR-8P, RO-1, S-10, S-20).

## How it works

| Target | Patch | Does |
|---|---|---|
| `MyCharacter.MoveAndRotate(Vector3, Vector2, float)` | prefix | While the local character is in ironsight, scales the rotation input by `target / ROTATION_SPEED_IRONSIGHTS` (read from the game, 0.6 fallback). Vanilla's own multiplier and aim assist still apply |
| `MyCameraZoomProperties.Update()` | prefix, postfix | Sets the transition time and recomputes the zoomed FOV in tan space, so magnification changes evenly. Sensitivity reads the resulting FOV, so it follows each weapon's zoom |

The zoom for the weapon in hand is looked up once per weapon and settings change, not every frame.

Every patch is guarded: an unexpected error writes one line to the game log, prefixed `[PolarsADSImprovements]`, and the plugin leaves the game's own aiming alone until the game restarts. If a patch target is missing after a game update, the plugin removes its own patches and stays off. No networking, and no file access beyond its own config.

## Bug reports

[Open an issue](https://github.com/Po11arr/PolarsADSImprovements/issues) with your Pulsar `info.log` (`%AppData%\Pulsar\Legacy\info.log`), the `[PolarsADSImprovements]` lines from the game log (`%AppData%\SpaceEngineers\SpaceEngineers_*.log`), and what you were doing.

## Development

Built on the [CometWorks client plugin template](https://github.com/CometWorks/client-plugin-template). The plugin source is in `PolarsADSImprovements`. Pulsar compiles it from source; Harmony comes from Pulsar and nothing is bundled.
