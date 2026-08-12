using UnityEngine;

namespace MouseDPI;

/// <summary>
/// Turns the configured DPI pair into the factor the patch applies.
/// </summary>
internal static class DpiScale
{
    /// <summary>
    /// Guard rails on the result rather than on either input, so any pair of numbers that divides to
    /// something sane is accepted however it was written. The bounds are only there to keep a typo
    /// from producing a stick that cannot be centred or one that cannot be moved.
    /// </summary>
    private const float MinMultiplier = 0.01f;

    private const float MaxMultiplier = 100f;

    private static bool _warned;

    /// <summary>
    /// The factor the virtual joystick's sensitivity is multiplied by, or 1 when the mod is off or
    /// the configuration does not describe a ratio. Read fresh every time: the config can be edited
    /// in flight through a configuration manager, and the arithmetic costs nothing next to a frame.
    /// </summary>
    internal static float Multiplier
    {
        get
        {
            if (!Plugin.Enabled.Value)
                return 1f;

            int actual = Plugin.ActualDPI.Value;
            int simulated = Plugin.SimulatedDPI.Value;

            if (actual <= 0 || simulated <= 0)
            {
                if (!_warned)
                {
                    _warned = true;
                    Plugin.Logger.LogError(
                        $"ActualDPI ({actual}) and SimulatedDPI ({simulated}) must both be above "
                        + "zero. Leaving the virtual joystick unscaled.");
                }

                return 1f;
            }

            _warned = false;
            return Mathf.Clamp((float)simulated / actual, MinMultiplier, MaxMultiplier);
        }
    }
}
