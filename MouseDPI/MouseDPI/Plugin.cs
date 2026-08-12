using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace MouseDPI;

public static class PluginInfo
{
    public const string PLUGIN_GUID = "MouseDPI";
    public const string PLUGIN_NAME = "MouseDPI";
    public const string PLUGIN_VERSION = "1.0.0";
}

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    private const string SectionConfig = "Config";
    private const string SectionDpi = "DPI";

    public static ConfigEntry<bool> Enabled;

    public static ConfigEntry<int> ActualDPI;
    public static ConfigEntry<int> SimulatedDPI;

    private void Awake()
    {
        Logger = base.Logger;

        Enabled = Config.Bind(
            SectionConfig,
            "Enabled",
            true,
            "Master switch. With this off the mod is inert and the virtual joystick behaves exactly "
            + "as the game's own sensitivity slider says.");

        ActualDPI = Config.Bind(
            SectionDpi,
            "ActualDPI",
            800,
            "The DPI your mouse is really set to. Nothing is written to the mouse — this is only one "
            + "half of a ratio, and the mod cares about SimulatedDPI / ActualDPI, not about either "
            + "number on its own. Set it to whatever your mouse software reports so that "
            + "SimulatedDPI can be read as a DPI rather than as a multiplier.");
        SimulatedDPI = Config.Bind(
            SectionDpi,
            "SimulatedDPI",
            2400,
            "The DPI you want the virtual joystick to behave as if the mouse had. Equal to ActualDPI "
            + "means no change; double it and the same movement of the hand deflects the stick twice "
            + "as far, exactly as raising the mouse's own DPI would.\n"
            + "This is a true equivalence rather than an approximation: the game accumulates the "
            + "stick from the raw mouse axis linearly and only clamps at the end, so scaling the "
            + "input and scaling the sensitivity are the same operation. It is also why the mod can "
            + "go past the settings screen's slider — the slider's ceiling is a UI limit, not a "
            + "limit of the flight model.\n"
            + "Your in-game 'Virtual Joystick Sensitivity' setting still applies, multiplied by this. "
            + "Leave that slider where it is and tune here.");

        var multiplier = DpiScale.Multiplier;
        if (Enabled.Value && multiplier != 1f)
        {
            Logger.LogInfo(
                $"Virtual joystick input scaled by {multiplier:0.###}x "
                + $"({ActualDPI.Value} DPI -> {SimulatedDPI.Value} DPI).");
        }
        else
        {
            Logger.LogInfo("Virtual joystick input left unscaled.");
        }

        var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        harmony.PatchAll();

        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
    }
}
