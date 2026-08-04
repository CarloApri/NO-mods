using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using HeliBinds.Aircraft;

namespace HeliBinds;

public static class PluginInfo
{
    public const string PLUGIN_GUID = "HeliBinds";
    public const string PLUGIN_NAME = "HeliBinds";
    public const string PLUGIN_VERSION = "1.0.0";
}

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    public static Plugin Instance;

    // BepInEx writes config sections alphabetically and ignores the order they were bound in; keys
    // within a section do keep theirs. So the order below controls the file only within each section.
    private const string SectionConfig = "Config";
    private const string SectionOverrides = "Overrides";
    private const string SectionAircraft = "Aircraft";

    public static ConfigEntry<bool> Enabled;

    public static ConfigEntry<bool> OverridePitch;
    public static ConfigEntry<bool> OverrideRoll;
    public static ConfigEntry<bool> OverrideYaw;
    public static ConfigEntry<bool> OverrideCollective;

    public static ConfigEntry<bool> UseAircraftList;

    /// <summary>Rewired action ids, filled in by the registration patch. -1 until then.</summary>
    public static readonly Dictionary<HeliAxis, int> ActionIds = new();

    public AircraftListManager AircraftList { get; private set; }

    private bool _listReady;

    /// <summary>Rewired action name for an axis. Prefixed with the plugin GUID so it can never
    /// collide with an action registered by another mod.</summary>
    public static string ActionName(HeliAxis axis) => $"{PluginInfo.PLUGIN_GUID}::{axis}";

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;
        AircraftList = new AircraftListManager();

        Enabled = Config.Bind(
            SectionConfig,
            "Enabled",
            true,
            "Master switch for the whole mod.");

        OverridePitch = Config.Bind(
            SectionOverrides,
            "OverridePitch",
            false,
            "Drive pitch from 'Helicopter Pitch' on rotorcraft instead of the normal Pitch binding.");
        OverrideRoll = Config.Bind(
            SectionOverrides,
            "OverrideRoll",
            true,
            "Drive roll from 'Helicopter Roll' on rotorcraft instead of the normal Roll binding.");
        OverrideYaw = Config.Bind(
            SectionOverrides,
            "OverrideYaw",
            true,
            "Drive yaw from 'Helicopter Yaw' on rotorcraft instead of the normal Yaw binding.");
        OverrideCollective = Config.Bind(
            SectionOverrides,
            "OverrideCollective",
            false,
            "Drive the collective from 'Helicopter Collective' on rotorcraft instead of the normal "
            + "Throttle binding.");

        UseAircraftList = Config.Bind(
            SectionAircraft,
            "UseAircraftList",
            false,
            "When false, the helicopter controls activate on any aircraft the game reports as having "
            + "a collective (takeoffDistance == 0), so new aircraft work without any editing. When "
            + "true, HeliBindsAircraft.json decides instead. That file is generated and kept up to "
            + "date either way, pre-filled with what the automatic detection would have chosen.");

        // An override with nothing bound to it would leave that axis dead, since this mod replaces
        // the vanilla binding rather than adding to it. Hence per-axis switches, off for the two
        // most people keep shared with fixed-wing.
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

        var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        harmony.PatchAll();
    }

    private void Update()
    {
        // The Encyclopedia is not populated at plugin start, so the roster is built on the first
        // frame it becomes available and never touched again.
        if (!_listReady)
            _listReady = AircraftList.TryBuild();
    }
}
