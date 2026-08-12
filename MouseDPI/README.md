# MouseDPI

Scales the mouse input that drives Nuclear Option's virtual joystick, expressed as a DPI you would
like your mouse to have. Nothing is sent to the mouse and no other program is involved — the game's
own sensitivity is multiplied by a ratio you choose.

It exists because the settings screen's **Virtual Joystick Sensitivity** slider runs out. On a
800 DPI mouse the stick can be too slow to reach full deflection comfortably even with the slider at
its maximum, and the usual fix — raising the mouse's hardware DPI — means changing it back for
everything else you do with that mouse.

## What it does

The game accumulates the virtual joystick as

```
stickPosition += sensitivity * deltaTime * 30 * mouseAxis
```

and only clamps the result at the very end. That is linear in the mouse axis, so multiplying the
sensitivity and multiplying the mouse's counts per inch are the same operation — this is an exact
equivalence, not an approximation. The slider's ceiling is a limit of the settings UI, not of the
flight model, and going past it changes nothing about how the aircraft is flown.

Only the virtual joystick is affected. Free look, the orbit and free cameras and every other view
that pans with the mouse keep following the game's own **View Motion Sensitivity** setting.

Your in-game sensitivity slider still applies, multiplied by the ratio here. Leave it where it is
and tune this instead.

## Config

`BepInEx\config\MouseDPI.cfg`, written on first launch.

### `[Config]`

| Key | Default | Meaning |
| --- | --- | --- |
| `Enabled` | `true` | Master switch. With this off the mod is inert. |

### `[DPI]`

| Key | Default | Meaning |
| --- | --- | --- |
| `ActualDPI` | `800` | The DPI your mouse is really set to. |
| `SimulatedDPI` | `2400` | The DPI you want the virtual joystick to behave as if the mouse had. |

Only the ratio between the two matters. `800 → 2400` and `1600 → 4800` are the same setting; the
pair exists so the number you tune can be read as a DPI rather than as a bare multiplier. Setting
them equal gives vanilla behaviour, and a `SimulatedDPI` below `ActualDPI` slows the stick down,
which is a way to get below the slider's lowest step if you ever want it.

The defaults describe a 800 DPI mouse asked to behave like a 2400 DPI one. If your mouse is not
800 DPI, set `ActualDPI` to what it really is first — otherwise the multiplier you get will not be
the one the numbers appear to say.

Both values are read every frame, so a configuration manager can be used to find the number you want
without leaving the aircraft.

## Compatibility

The patch adds no IL of its own: it raises `PlayerSettings.virtualJoystickSensitivity` for the
duration of `PilotPlayerState.PlayerAxisControls` and restores it immediately after. That method is
also transpiled by other mouse-and-keyboard mods, including
[ChaseCamPlus](../ChaseCamPlus/) and YawOnMouse, and there is nothing here for those edits to
collide with. With ChaseCamPlus installed the scaling applies in third person too, since the stick
is fed by the same code there.

The value is restored rather than raised once at load on purpose. `ControlsMenu` reads the same
field to position its sensitivity slider, so a permanently raised value would appear there clamped
to the slider's maximum — and pressing Apply would then write that clamped number over your real
setting.

Client-side. The stick position it produces travels the same path, with the same clamping, that the
game already uses, so nothing new goes over the network.

## Install

Drop `MouseDPI.dll` into `BepInEx\plugins\MouseDPI\` and launch once to generate the config.

No keybinds to assign — the mod has no controls of its own.

## Building

```
dotnet build MouseDPI/MouseDPI/MouseDPI.csproj -c Release
```

Game assemblies are resolved through `GameDir.targets`.
