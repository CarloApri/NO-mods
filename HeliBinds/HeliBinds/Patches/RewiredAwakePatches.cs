using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using HeliBinds.Helpers;
using Rewired;

namespace HeliBinds.Patches;

// Same approach as YawOnMouse / TargetCamControl: inject user-assignable InputActions into Rewired's
// user data before the input manager reads it, so the bindings show up in the game's own Flight
// Controls screen.
//
// These are registered as *axis* actions rather than buttons, because that is what the game's own
// Pitch/Roll/Yaw are — which is precisely why a single "Yaw" entry can hold both A and D. Rewired
// splits an axis action into a negative and a positive pole for keyboard binding, so each action
// below becomes two assignable rows named e.g. "Helicopter Yaw Left" and "Helicopter Yaw Right".
[HarmonyPatch(typeof(InputManager_Base), "Awake")]
static class RewiredAwakePatches
{
    private const string TargetCategory = "flight";

    private class AxisDefinition
    {
        public HeliAxis Axis;
        public string Descriptive;
        public string Negative;
        public string Positive;
    }

    private static readonly AxisDefinition[] Axes =
    {
        new AxisDefinition
        {
            Axis = HeliAxis.Pitch,
            Descriptive = "Helicopter Pitch",
            Negative = "Helicopter Pitch Down",
            Positive = "Helicopter Pitch Up"
        },
        new AxisDefinition
        {
            Axis = HeliAxis.Roll,
            Descriptive = "Helicopter Roll",
            Negative = "Helicopter Roll Left",
            Positive = "Helicopter Roll Right"
        },
        new AxisDefinition
        {
            Axis = HeliAxis.Yaw,
            Descriptive = "Helicopter Yaw",
            Negative = "Helicopter Yaw Left",
            Positive = "Helicopter Yaw Right"
        },
        new AxisDefinition
        {
            Axis = HeliAxis.Collective,
            Descriptive = "Helicopter Collective",
            Negative = "Helicopter Collective Down",
            Positive = "Helicopter Collective Up"
        }
    };

    [HarmonyPrefix]
    static void Prefix(InputManager_Base __instance)
    {
        try
        {
            var userData = __instance.userData;
            if (userData == null) return;

            var categories = GetField<IList>(userData, "actionCategories");
            var actions = GetField<IList>(userData, "actions");
            if (categories == null || actions == null) return;

            object targetCat = null;
            foreach (var category in categories)
            {
                var name = GetProp<string>(category, "name");
                if (string.Equals(name, TargetCategory, StringComparison.OrdinalIgnoreCase))
                {
                    targetCat = category;
                    break;
                }
            }

            if (targetCat == null)
            {
                Plugin.Logger.LogWarning(
                    "Flight category not found — helicopter actions will not be registered");
                return;
            }

            var usedIds = new HashSet<int>();
            var existingByName = new Dictionary<string, int>();
            int highestId = 0;

            foreach (var a in actions)
            {
                var id = GetProp<int>(a, "id");
                usedIds.Add(id);
                if (id > highestId) highestId = id;

                var name = GetProp<string>(a, "name");
                if (!string.IsNullOrEmpty(name))
                    existingByName[name] = id;
            }

            int targetCatId = GetProp<int>(targetCat, "id");

            // An axis action carries behaviour settings the game's designers set in the inspector —
            // among them the digital axis simulation that turns a key press into an analog value,
            // with the sensitivity it rises at and the gravity it falls back to zero at. Build the
            // action from scratch and those land on CLR defaults; a gravity of zero means the axis
            // rises when you press and never returns when you let go.
            //
            // So clone an axis action the game already ships instead, and override only identity
            // afterwards. That way every behaviour field arrives tuned the way the built-in flight
            // axes are, including the ones we would otherwise have to guess the name of.
            object template = FindAxisTemplate(actions);
            if (template == null)
            {
                Plugin.Logger.LogWarning(
                    "No existing axis action found to use as a template. The helicopter axes will be "
                    + "built from scratch, which may leave them without the digital axis settings "
                    + "the game's own axes use — expect the axis to stick after you release the key.");
            }

            string templateName = template != null ? GetProp<string>(template, "name") : "nothing";
            var catMap = GetField<object>(userData, "actionCategoryMap");
            var addActionMethod = catMap != null
                ? AccessTools.Method(catMap.GetType(), "AddAction", new[] { typeof(int), typeof(int) })
                : null;

            foreach (AxisDefinition axis in Axes)
            {
                string actionName = Plugin.ActionName(axis.Axis);

                // Already registered (e.g. the input manager woke up twice); reuse its id.
                if (existingByName.TryGetValue(actionName, out int existingId))
                {
                    Plugin.ActionIds[axis.Axis] = existingId;
                    continue;
                }

                // Stable across sessions and load orders, so bindings survive installing or removing
                // other mods. Sequential allocation only as a last resort.
                int id = StableActionId.For(actionName);
                if (usedIds.Contains(id))
                {
                    int fallback = ++highestId;
                    Plugin.Logger.LogWarning(
                        $"Preferred action id {id} for {actionName} is taken; falling back to "
                        + $"{fallback}. That id is order-dependent, so a binding made now may not "
                        + "survive installing or removing other mods.");
                    id = fallback;
                }
                else if (id > highestId)
                {
                    highestId = id;
                }

                var actionType = typeof(InputAction);
                var action = (InputAction)Activator.CreateInstance(actionType, true);

                CopyFields(template, action, actionType);

                SetProp(actionType, action, "id", id);
                SetProp(actionType, action, "name", actionName);
                SetProp(actionType, action, "type", InputActionType.Axis);
                SetProp(actionType, action, "descriptiveName", axis.Descriptive);

                // Pole labels, so the binding screen reads "Helicopter Yaw Left" / "...Right"
                // instead of a bare "+" and "-". Set defensively: if this Rewired build names the
                // properties differently the setter simply no-ops and the labels fall back.
                SetProp(actionType, action, "negativeDescriptiveName", axis.Negative);
                SetProp(actionType, action, "positiveDescriptiveName", axis.Positive);

                SetProp(actionType, action, "categoryId", targetCatId);
                SetField(actionType, action, "_userAssignable", true);

                actions.Add(action);
                usedIds.Add(id);
                addActionMethod?.Invoke(catMap, new object[] { targetCatId, id });

                Plugin.ActionIds[axis.Axis] = id;
                Plugin.Logger.LogInfo(
                    $"Registered {axis.Descriptive} in Flight category (id {id}, "
                    + $"cloned from '{templateName}')");
            }
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"Action registration: {e}");
        }
    }

    /// <summary>
    /// Picks an existing axis action to clone behaviour from. Prefers the game's own "Roll", which
    /// is the closest analogue to what we are adding — a keyboard-driven flight axis — and falls
    /// back to any axis action at all.
    /// </summary>
    private static object FindAxisTemplate(IList actions)
    {
        object fallback = null;

        foreach (var a in actions)
        {
            if (GetProp<InputActionType>(a, "type") != InputActionType.Axis)
                continue;

            if (string.Equals(GetProp<string>(a, "name"), "Roll", StringComparison.OrdinalIgnoreCase))
                return a;

            fallback ??= a;
        }

        return fallback;
    }

    /// <summary>
    /// Copies every instance field from the template onto the new action. Deliberately name-blind:
    /// the point is to inherit settings we do not know the names of, so enumerating fields is safer
    /// than listing the ones we happen to know about.
    /// </summary>
    private static void CopyFields(object template, object target, Type actionType)
    {
        if (template == null)
            return;

        const BindingFlags flags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (FieldInfo field in actionType.GetFields(flags))
        {
            if (field.IsLiteral || field.IsInitOnly)
                continue;

            try
            {
                field.SetValue(target, field.GetValue(template));
            }
            catch (Exception e)
            {
                Plugin.Logger.LogWarning($"Could not copy InputAction field {field.Name}: {e.Message}");
            }
        }
    }

    private static T GetProp<T>(object instance, string name) =>
        (T)(AccessTools.Property(instance.GetType(), name)?.GetValue(instance) ?? default(T));

    private static void SetProp<T>(Type type, object instance, string name, T value) =>
        AccessTools.Property(type, name)?.SetValue(instance, value, null);

    private static T GetField<T>(object instance, string name) =>
        (T)(AccessTools.Field(instance.GetType(), name)?.GetValue(instance) ?? default(T));

    private static void SetField<T>(Type type, object instance, string name, T value) =>
        AccessTools.Field(type, name)?.SetValue(instance, value);
}
