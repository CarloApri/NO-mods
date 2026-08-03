using Rewired;

namespace HeliBinds.Helpers;

/// <summary>
/// Substituted for the game's own axis reads inside <c>PilotPlayerState</c>. Each method either
/// forwards to the vanilla action or reads the helicopter action in its place — never both, which
/// is what makes the override a replacement rather than a sum.
/// </summary>
public static class HeliAxisReader
{
    public static float GetAxis(Player player, string vanillaAction) =>
        TryHeliAction(vanillaAction, out int id) ? player.GetAxis(id) : player.GetAxis(vanillaAction);

    public static float GetAxisRaw(Player player, string vanillaAction) =>
        TryHeliAction(vanillaAction, out int id) ? player.GetAxisRaw(id) : player.GetAxisRaw(vanillaAction);

    /// <summary>
    /// The throttle logic compares this frame's raw value against the previous one to tell a
    /// physical lever from a held key. If this method and <see cref="GetAxisRaw"/> ever disagreed
    /// about which action to read, that comparison would be measuring two different axes against
    /// each other — hence both routing through the same <see cref="TryHeliAction"/> decision.
    /// </summary>
    public static float GetAxisRawPrev(Player player, string vanillaAction) =>
        TryHeliAction(vanillaAction, out int id) ? player.GetAxisRawPrev(id) : player.GetAxisRawPrev(vanillaAction);

    private static bool TryHeliAction(string vanillaAction, out int actionId)
    {
        actionId = -1;

        if (!Plugin.Enabled.Value)
            return false;

        if (!TryMapAxis(vanillaAction, out HeliAxis axis))
            return false;

        if (!OverrideEnabled(axis))
            return false;

        if (!Plugin.ActionIds.TryGetValue(axis, out actionId) || actionId < 0)
            return false;

        return LocalAircraftUsesHeliControls();
    }

    private static bool TryMapAxis(string vanillaAction, out HeliAxis axis)
    {
        switch (vanillaAction)
        {
            case "Pitch": axis = HeliAxis.Pitch; return true;
            case "Roll": axis = HeliAxis.Roll; return true;
            case "Yaw": axis = HeliAxis.Yaw; return true;
            case "Throttle": axis = HeliAxis.Collective; return true;
            default: axis = default; return false;
        }
    }

    private static bool OverrideEnabled(HeliAxis axis) => axis switch
    {
        HeliAxis.Pitch => Plugin.OverridePitch.Value,
        HeliAxis.Roll => Plugin.OverrideRoll.Value,
        HeliAxis.Yaw => Plugin.OverrideYaw.Value,
        HeliAxis.Collective => Plugin.OverrideCollective.Value,
        _ => false
    };

    private static bool LocalAircraftUsesHeliControls()
    {
        if (Plugin.Instance?.AircraftList == null)
            return false;

        if (!GameManager.GetLocalAircraft(out global::Aircraft aircraft) || aircraft == null)
            return false;

        return Plugin.Instance.AircraftList.UsesHeliControls(aircraft.definition as AircraftDefinition);
    }
}
