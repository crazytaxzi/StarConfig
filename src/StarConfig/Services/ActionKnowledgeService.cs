namespace StarConfig.Services;

public sealed class ActionKnowledgeService
{
    private static readonly Dictionary<string, string> ExactDescriptions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["v_toggle_master_mode"] = "Changes the ship between broad Master Mode operating states such as SCM and NAV. This changes the ship's overall operating regime, not merely the currently selected operator function.",
        ["v_cycle_master_mode"] = "Cycles through available ship Master Modes. Unlike a direct selector, repeated presses may be required to reach the desired mode.",
        ["v_toggle_operator_mode"] = "Changes the active operator function inside the current Master Mode, such as guns, missiles, scanning, or another available ship function. It does not perform the same job as changing Master Mode.",
        ["v_cycle_operator_mode"] = "Cycles through operator functions available inside the current Master Mode. This may eventually reach the same function as a direct operator-mode command, but it is not deterministic from every starting state.",
        ["v_ifcs_throttle_abs"] = "Controls the ship's forward and reverse thrust demand as an absolute axis. Best suited to a throttle or centered longitudinal axis depending on the configured response."
    };

    public string Describe(string actionName, string context)
    {
        if (ExactDescriptions.TryGetValue(actionName, out var exact)) return exact;
        var readable = Humanize(actionName);
        var behavior = actionName.Contains("toggle", StringComparison.OrdinalIgnoreCase)
            ? "This is a toggle action: one activation changes state and the next activation normally changes it back."
            : actionName.Contains("cycle", StringComparison.OrdinalIgnoreCase)
                ? "This is a cycle action: repeated activations move through available states rather than selecting one state directly."
                : actionName.Contains("hold", StringComparison.OrdinalIgnoreCase)
                    ? "This action is intended to remain active while the control is held."
                    : "This action directly invokes the named function while its game context is active.";
        return $"{readable}. Context: {context}. {behavior}";
    }

    public string? InferIntent(string actionName)
    {
        var text = actionName.ToLowerInvariant();
        if ((text.Contains("forward") && text.Contains("back")) || text.Contains("throttle_abs") || text.Contains("longitudinal")) return "movement.forward-back";
        if ((text.Contains("left") && text.Contains("right")) || text.Contains("lateral")) return "movement.left-right";
        if (text.Contains("pitch")) return "rotation.pitch";
        if (text.Contains("yaw")) return "rotation.yaw";
        if (text.Contains("roll")) return "rotation.roll";
        if (text.Contains("fire") && (text.Contains("primary") || text.Contains("weapon_group_1"))) return "combat.primary-fire";
        if (text.Contains("interact") || text.Contains("use")) return "interaction.use";
        return null;
    }

    private static string Humanize(string value)
    {
        var cleaned = value.Replace('_', ' ').Replace('-', ' ').Trim();
        return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(cleaned);
    }
}
