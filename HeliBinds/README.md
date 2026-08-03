# HeliBinds

Separate flight controls for rotorcraft. Bind pitch, roll, yaw and collective differently on
helicopters than on fixed-wing, and the game switches between them by itself.

## Why

Nuclear Option has one set of flight bindings for every aircraft. If your helicopter control scheme
differs from your jet one, there is no way to express that — the usual workaround is an external key
remapper toggled between sorties, which brings a mode you have to remember, extra keys burned as
proxies, stuck keys on alt-tab, and modifier keys composing into chords instead of arriving as plain
presses. None of that exists inside the game's own input layer.

## What it adds

Four axis actions under **Settings → Controls → Flight Controls**:

| Action | Negative | Positive |
| --- | --- | --- |
| Helicopter Pitch | Down | Up |
| Helicopter Roll | Left | Right |
| Helicopter Yaw | Left | Right |
| Helicopter Collective | Down | Up |

Eight assignable rows in total. They are *axis* actions, not buttons, because that is what the game's
own Pitch/Roll/Yaw are — which is why one entry can hold two opposite keys, one per pole. They behave
like the built-in axes, and are cloned from them so the digital axis settings match.

On a rotorcraft, an axis with its override enabled is driven **only** by the helicopter action; the
vanilla binding for that axis is ignored. On everything else the mod is inert.

## Install

Requires [BepInEx 5](https://github.com/BepInEx/BepInEx/releases) (tested on 5.4.23.5).

Drop `HeliBinds.dll` into `BepInEx\plugins\HeliBinds\`. Launch once to generate the config files and
register the actions, then bind the poles you want.

## Config

`BepInEx\config\HeliBinds.cfg`

| Key | Default | Meaning |
| --- | --- | --- |
| `Enabled` | `true` | Master switch. |
| `UseAircraftList` | `false` | `false` = automatic rotorcraft detection. `true` = `HeliBindsAircraft.json` decides. |
| `OverridePitch` | `false` | Pitch comes from `Helicopter Pitch`. |
| `OverrideRoll` | `true` | Roll comes from `Helicopter Roll`. |
| `OverrideYaw` | `true` | Yaw comes from `Helicopter Yaw`. |
| `OverrideCollective` | `false` | Collective comes from `Helicopter Collective`. |

**The overrides are per axis, and default to off for pitch and collective on purpose.** This mod
*replaces* a binding rather than adding to it, so enabling an override for an axis you never bound
would leave that axis dead. Most people share pitch and throttle with their fixed-wing setup; the
actions are registered either way, ready for when you want them.

Config changes apply on the next launch.

## Aircraft selection

Detection uses the game's own test for "this airframe has a collective" — the same comparison the
base game uses to decide whether the invert-collective setting applies. Aircraft added by updates or
by other mods are classified without anyone editing a list.

`BepInEx\config\HeliBindsAircraft.json` is generated from the game's full aircraft roster and
pre-filled with the detection result, so rotorcraft arrive already switched on:

```json
{
  "Aircraft": {
    "AttackHelo1": { "UnitName": "SAH-46 Chicane", "HeliControls": true },
    "Fighter1": { "UnitName": "FS-12 Revoker", "HeliControls": false }
  }
}
```

Set `UseAircraftList = true` to make the file authoritative — useful to exclude something the
detection catches but you do not want treated as a helicopter, such as a VTOL. The file is generated
and kept current even while the flag is off, so switching to manual control later starts from an
up-to-date list. `UnitName` is written for readability and never read back.

Entries are matched on the aircraft definition itself rather than on a name string, so there is no
chance of one aircraft's name accidentally matching another's.

## Compatibility

Rewired persists bindings by numeric action id, and the allocation scheme most Nuclear Option mods
inherit depends on plugin load order — installing or removing an unrelated mod can shift the number
and hand your binding to someone else's action. This mod derives its ids from a hash of the action
name instead, so they are identical every launch regardless of what else is installed. If something
already occupies one, it falls back to sequential allocation and logs a warning.

Action names are prefixed with the plugin GUID, so name collisions cannot happen.

The control patches count their edits and expect an exact number. If a game update changes the
methods they target, the count stops matching, an error is logged and the original code is left
alone — the controls fall back to vanilla rather than misbehaving silently.

## Multiplayer

No new values reach the network. The same clamped control inputs travel the same path the game
already uses.

## Credits

The Rewired action-injection technique comes from
[TargetCamControl](https://github.com/9138noms/TargetCamControl); its BepInEx
adaptation was taken from [YawOnMouse](https://github.com/muji2498/YawOnMouse) (MIT), which also informed the per-aircraft
config file approach.
