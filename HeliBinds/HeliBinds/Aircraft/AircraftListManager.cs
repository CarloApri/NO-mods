using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using Newtonsoft.Json;

namespace HeliBinds.Aircraft;

/// <summary>
/// Owns <c>HeliBindsAircraft.json</c>: the per-aircraft override list.
///
/// Two deliberate departures from how the other mods in this ecosystem do it.
///
/// The roster comes from <c>Encyclopedia.aircraft</c> — the authoritative list the game itself walks
/// in <c>CameraChaseState.Initialize</c> — rather than <c>Resources.FindObjectsOfTypeAll</c>, which
/// only sees aircraft already loaded into memory and therefore fills in gradually over many sessions.
///
/// And the file is generated *pre-filled with the auto-detection result*, so rotorcraft arrive
/// already switched on. A list that starts empty is a list nobody ever populates.
/// </summary>
public class AircraftListManager
{
    private readonly string _path;
    private AircraftListFile _file = new();

    // Resolved against the definition object itself, never against a name string. Both the roster
    // and the aircraft you are flying hand us an AircraftDefinition, so there is no reason to match
    // on text and inherit the "(Clone)" suffix and substring-collision problems that come with it.
    private readonly Dictionary<AircraftDefinition, bool> _byDefinition = new();

    public bool Ready { get; private set; }

    public AircraftListManager()
    {
        _path = Path.Combine(Paths.ConfigPath, "HeliBindsAircraft.json");
    }

    /// <summary>
    /// Attempts to load/refresh the list. Returns false while the Encyclopedia is still loading, so
    /// the caller can retry; succeeds exactly once.
    /// </summary>
    public bool TryBuild()
    {
        Encyclopedia encyclopedia = Encyclopedia.i;
        if (encyclopedia == null || encyclopedia.aircraft == null || encyclopedia.aircraft.Count == 0)
            return false;

        Load();

        bool dirty = false;
        _byDefinition.Clear();

        foreach (AircraftDefinition definition in encyclopedia.aircraft)
        {
            if (definition == null)
                continue;

            string key = KeyFor(definition);
            bool detected = IsRotorcraft(definition);

            if (!_file.Aircraft.TryGetValue(key, out AircraftEntry entry))
            {
                // New aircraft — from a game update or another mod. Seed it with what the
                // auto-detection would have decided, so the file stays useful without editing.
                entry = new AircraftEntry { UnitName = definition.unitName, HeliControls = detected };
                _file.Aircraft[key] = entry;
                dirty = true;
                Plugin.Logger.LogInfo(
                    $"Discovered {key} ({definition.unitName}) -> heliControls={detected}");
            }
            else if (entry.UnitName != definition.unitName)
            {
                // Keep the human-readable label current without touching the user's choice.
                entry.UnitName = definition.unitName;
                dirty = true;
            }

            _byDefinition[definition] = entry.HeliControls;
        }

        if (dirty)
            Save();

        Ready = true;
        return true;
    }

    /// <summary>
    /// Whether this aircraft uses the helicopter control scheme, honouring the list when the user
    /// asked for it and falling back to auto-detection for anything the list has never seen.
    /// </summary>
    public bool UsesHeliControls(AircraftDefinition definition)
    {
        if (definition == null)
            return false;

        if (Plugin.UseAircraftList.Value && _byDefinition.TryGetValue(definition, out bool listed))
            return listed;

        return IsRotorcraft(definition);
    }

    /// <summary>
    /// The game's own test for "this airframe has a collective": <c>PilotPlayerState.EnterState</c>
    /// sets its <c>collective</c> flag from exactly this comparison, and that flag is what gates
    /// <c>PlayerSettings.invertCollective</c>. Borrowing it means the mod groups aircraft the way
    /// the base game already does rather than inventing a rule.
    /// </summary>
    public static bool IsRotorcraft(AircraftDefinition definition)
    {
        AircraftParameters parameters = definition != null ? definition.aircraftParameters : null;
        return parameters != null && parameters.takeoffDistance == 0f;
    }

    /// <summary>
    /// <c>jsonKey</c> is what the game uses to resolve definitions out of saved missions, so it is
    /// unique by requirement rather than by hope. Falls back to the asset name if a definition
    /// leaves it blank.
    /// </summary>
    private static string KeyFor(AircraftDefinition definition) =>
        string.IsNullOrEmpty(definition.jsonKey) ? definition.name : definition.jsonKey;

    private void Load()
    {
        if (!File.Exists(_path))
        {
            _file = new AircraftListFile();
            return;
        }

        try
        {
            string json = File.ReadAllText(_path);
            _file = JsonConvert.DeserializeObject<AircraftListFile>(json) ?? new AircraftListFile();
            _file.Aircraft ??= new Dictionary<string, AircraftEntry>();
        }
        catch (Exception e)
        {
            // Never clobber a file we failed to understand — the user may have hand-edited it into
            // a state worth recovering. Run on detection instead and say so.
            Plugin.Logger.LogError(
                $"Could not read {_path}: {e.Message}. Falling back to automatic detection; "
                + "the file will not be overwritten.");
            _file = new AircraftListFile();
        }
    }

    private void Save()
    {
        try
        {
            string json = JsonConvert.SerializeObject(_file, Formatting.Indented);
            File.WriteAllText(_path, json);
            Plugin.Logger.LogInfo($"Wrote {_path}");
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"Could not write {_path}: {e.Message}");
        }
    }
}
