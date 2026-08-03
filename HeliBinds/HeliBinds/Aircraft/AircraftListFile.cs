using System.Collections.Generic;

namespace HeliBinds.Aircraft;

/// <summary>
/// On-disk shape of <c>HeliBindsAircraft.json</c>. One entry per aircraft in the game's roster.
/// </summary>
public class AircraftListFile
{
    public Dictionary<string, AircraftEntry> Aircraft { get; set; } = new();
}

public class AircraftEntry
{
    /// <summary>
    /// Display name, written purely so the file is readable — nothing reads it back. Without it you
    /// have to know that "AttackHelo1" means Chicane.
    /// </summary>
    public string UnitName { get; set; }

    /// <summary>Whether the helicopter control scheme applies to this aircraft.</summary>
    public bool HeliControls { get; set; }
}
