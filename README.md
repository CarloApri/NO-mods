# Nuclear Option mods

Client-side BepInEx mods for [Nuclear Option](https://store.steampowered.com/app/2168680/Nuclear_Option/),
aimed at mouse-and-keyboard play.

| Mod | What it does |
| --- | --- |
| [**ChaseCamPlus**](ChaseCamPlus/) | Makes the chase camera usable on mouse and keyboard: virtual joystick in third person, a free look chase never had, a cockpit ↔ chase toggle, and the flight HUD restored in chase. |
| [**HeliBinds**](HeliBinds/) | Separate flight bindings for rotorcraft, switched automatically by aircraft — no external key remapper, no mode to remember. |

Each folder has its own README with the full config reference.

## Requirements

[BepInEx 5](https://github.com/BepInEx/BepInEx/releases), tested on 5.4.23.5.

## Install

Drop the mod's `.dll` into `BepInEx\plugins\<ModName>\`, launch once to generate its config and
register its keybinds, then assign them under **Settings → Controls → Flight Controls**.

Both mods are client-side and send nothing new over the network — the control inputs they produce
travel the same path, with the same clamping, the game already uses.

## Building

```
dotnet build ChaseCamPlus/ChaseCamPlus/ChaseCamPlus.csproj -c Release
dotnet build HeliBinds/HeliBinds/HeliBinds.csproj -c Release
```

Each project resolves the game assemblies through its own `GameDir.targets`.

## Notes on compatibility

Rewired stores keybindings by numeric action id. The allocation scheme most Nuclear Option mods
inherit assigns those ids in plugin load order, so installing or removing an unrelated mod can shift
them and silently reassign a binding you made. Both mods here derive their ids from a hash of the
action name, so the number is the same every launch no matter what else is installed, falling back to
the usual scheme with a logged warning only if something already occupies it.

Where these mods patch shared game code they count their edits and expect an exact number. If a game
update changes what they target, the mismatch is logged and the original code is left untouched, so
behaviour falls back to vanilla instead of breaking quietly.

## Credits

The technique for injecting user-assignable actions into Rewired's action set — which is what makes
these mods' keybinds appear in the game's own controls screen — comes from
[TargetCamControl](https://github.com/9138noms/TargetCamControl).

The BepInEx adaptation of that technique was taken from [YawOnMouse](https://github.com/muji2498/YawOnMouse) (MIT), which also
served as the structural reference for these projects. YawOnMouse in turn originates from a mod by
**Haika** on the Nuclear Option Discord.

Built with [BepInEx](https://github.com/BepInEx/BepInEx) and
[HarmonyX](https://github.com/BepInEx/HarmonyX).

Full notices in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

## License

[MIT](LICENSE).

Not affiliated with Shockfront Studios. Nuclear Option game assemblies are referenced at build time
only and are not included here.
