using HarmonyLib;

namespace MouseDPI.Patches;

public class PilotPlayerStatePatches
{
    /// <summary>
    /// <c>PilotPlayerState.PlayerAxisControls</c> accumulates the virtual joystick as
    /// <c>position += virtualJoystickSensitivity * dt * 30 * mouseAxis</c> and only clamps the
    /// result at the end, so multiplying that sensitivity is arithmetically the same thing as
    /// multiplying the mouse's counts per inch.
    ///
    /// <c>PlayerSettings.virtualJoystickSensitivity</c> is read in exactly one place in the whole
    /// game — that line — so inflating the field is a complete and side-effect-free way to do it,
    /// with no IL matching and therefore nothing to collide with the transpilers other mods put on
    /// this same method.
    ///
    /// It is inflated for the duration of the call and put straight back rather than being raised
    /// once at load, because <c>ControlsMenu</c> reads the same field to position its sensitivity
    /// slider. A permanently raised value would show there clamped to the slider's maximum, and the
    /// next time the player pressed Apply that clamped number would be written to PlayerPrefs — the
    /// mod would have quietly overwritten their real setting.
    /// </summary>
    [HarmonyPatch(typeof(PilotPlayerState), "PlayerAxisControls")]
    static class PlayerAxisControls
    {
        private static float _original;

        private static bool _inflated;

        [HarmonyPriority(Priority.First)]
        static void Prefix()
        {
            // Self-heal. A postfix does not run when the original method throws, which would leave
            // the inflated value in the field; restoring before reading it again keeps the
            // multiplier from compounding frame after frame into an unflyable aircraft.
            if (_inflated)
            {
                PlayerSettings.virtualJoystickSensitivity = _original;
                _inflated = false;
            }

            float multiplier = DpiScale.Multiplier;
            if (multiplier == 1f)
                return;

            _original = PlayerSettings.virtualJoystickSensitivity;
            PlayerSettings.virtualJoystickSensitivity = _original * multiplier;
            _inflated = true;
        }

        [HarmonyPriority(Priority.Last)]
        static void Postfix()
        {
            if (!_inflated)
                return;

            PlayerSettings.virtualJoystickSensitivity = _original;
            _inflated = false;
        }
    }
}
