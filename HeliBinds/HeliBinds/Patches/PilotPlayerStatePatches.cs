using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using HeliBinds.Helpers;
using Rewired;

namespace HeliBinds.Patches;

public class PilotPlayerStatePatches
{
    /// <summary>
    /// The three flight axes are read once each, right at the end of the method:
    /// <code>
    /// pitchInput += player.GetAxis("Pitch");
    /// rollInput  += player.GetAxis("Roll");
    /// yawInput   += player.GetAxis("Yaw");
    /// </code>
    /// Each read is redirected to <see cref="HeliAxisReader"/>, which decides per call whether to
    /// forward to the vanilla action or read the helicopter one instead. The same method also reads
    /// "Pan View" and "Tilt View" through the identical Rewired call, so the match is keyed on the
    /// action string and leaves those alone.
    /// </summary>
    [HarmonyPatch(typeof(PilotPlayerState), "PlayerAxisControls")]
    static class PlayerAxisControls
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
            SwapAxisReads(
                instructions,
                "PilotPlayerState.PlayerAxisControls",
                rewiredMethod: nameof(Player.GetAxis),
                replacement: AccessTools.Method(typeof(HeliAxisReader), nameof(HeliAxisReader.GetAxis)),
                actionNames: new HashSet<string> { "Pitch", "Roll", "Yaw" },
                expected: 3);
    }

    /// <summary>
    /// The throttle is not a straight pass-through like the flight axes. It feeds an integrator that
    /// decides, from the delta between this frame's raw value and the previous one, whether it is
    /// looking at a physical throttle lever (jump to the absolute position) or a held key (ramp at
    /// one unit per second):
    /// <code>
    /// float num4 = Mathf.Abs(num - num2);
    /// if (num4 > 0f &amp;&amp; num4 &lt; 0.5f) simulatedThrottle = num;
    /// else if (Mathf.Abs(num) > 0.5f) simulatedThrottle += Mathf.Clamp(...);
    /// </code>
    /// So both the current and previous reads have to be redirected together. Redirect only one and
    /// that comparison measures the helicopter axis against the fixed-wing one, and the heuristic
    /// flips between its two branches at random. Both go through the same decision in
    /// <see cref="HeliAxisReader"/> for exactly this reason.
    ///
    /// The method reads "Custom Axis 1" through the same two Rewired calls; matching on the action
    /// string leaves it untouched.
    /// </summary>
    [HarmonyPatch(typeof(PilotPlayerState), "PlayerThrottleAxis1Controls")]
    static class PlayerThrottleAxis1Controls
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var throttle = new HashSet<string> { "Throttle" };

            // Materialised once: a failed pass hands back what it was given, and the second pass
            // must not re-enumerate a sequence that may only be walked a single time.
            var codes = new List<CodeInstruction>(instructions);

            var afterCurrent = SwapAxisReads(
                codes,
                "PilotPlayerState.PlayerThrottleAxis1Controls",
                rewiredMethod: nameof(Player.GetAxisRaw),
                replacement: AccessTools.Method(typeof(HeliAxisReader), nameof(HeliAxisReader.GetAxisRaw)),
                actionNames: throttle,
                expected: 1);

            return SwapAxisReads(
                afterCurrent,
                "PilotPlayerState.PlayerThrottleAxis1Controls",
                rewiredMethod: nameof(Player.GetAxisRawPrev),
                replacement: AccessTools.Method(typeof(HeliAxisReader), nameof(HeliAxisReader.GetAxisRawPrev)),
                actionNames: throttle,
                expected: 1);
        }
    }

    /// <summary>
    /// Retargets calls to <c>Rewired.Player.&lt;rewiredMethod&gt;(string)</c> whose action string is
    /// in <paramref name="actionNames"/>. Only the call target changes: the replacement is a static
    /// taking the same (Player, string) the instance call already had on the stack, so the stack
    /// shape is untouched and no branches move.
    /// </summary>
    private static IEnumerable<CodeInstruction> SwapAxisReads(
        IEnumerable<CodeInstruction> instructions,
        string where,
        string rewiredMethod,
        MethodInfo replacement,
        HashSet<string> actionNames,
        int expected)
    {
        var codes = new List<CodeInstruction>(instructions);

        int patched = 0;
        string lastString = null;

        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Ldstr)
            {
                lastString = codes[i].operand as string;
                continue;
            }

            if (codes[i].opcode != OpCodes.Call && codes[i].opcode != OpCodes.Callvirt)
                continue;

            if (codes[i].operand is not MethodInfo method)
                continue;

            if (method.DeclaringType != typeof(Player) || method.Name != rewiredMethod)
                continue;

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != 1 || parameters[0].ParameterType != typeof(string))
                continue;

            if (lastString == null || !actionNames.Contains(lastString))
                continue;

            var call = new CodeInstruction(OpCodes.Call, replacement);
            call.labels.AddRange(codes[i].labels);
            call.blocks.AddRange(codes[i].blocks);
            codes[i] = call;
            patched++;
        }

        if (patched != expected)
        {
            Plugin.Logger.LogError(
                $"{where}: expected {expected} call(s) to Player.{rewiredMethod} for "
                + $"[{string.Join(", ", actionNames)}], found {patched}. The game has probably been "
                + "updated — leaving this method unpatched, controls stay vanilla.");
            return instructions;
        }

        return codes;
    }
}
