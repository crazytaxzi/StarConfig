namespace StarConfig;

public sealed class StarbindActionKnowledge
{
    public StarbindActionInfo Explain(string mapName, string actionName)
    {
        var context = StarbindProfileService.ClassifyContext(mapName, actionName);
        var lower = actionName.ToLowerInvariant();
        var behavior = lower.Contains("toggle") ? "Toggle"
            : lower.Contains("cycle") || lower.Contains("next") || lower.Contains("prev") || lower.Contains("inc_") || lower.Contains("dec_") || lower.Contains("increase") || lower.Contains("decrease") ? "Cycle"
            : lower.Contains("hold") ? "Hold"
            : lower.Contains("abs") || lower.Contains("axis") || IsAxisAction(lower) ? "Axis"
            : "Direct";

        var intent = Intent(lower, mapName.ToLowerInvariant());
        var category = Category(intent, lower, mapName.ToLowerInvariant());
        var description = Description(context, intent, behavior, actionName, mapName);
        return new StarbindActionInfo(context, category, intent, behavior, description);
    }

    private static bool IsAxisAction(string lower) => lower is "gp_movex" or "gp_movey" or "gp_rotatepitch" or "gp_rotateyaw" or "v_roll" or "v_yaw" or "v_pitch" or "v_strafe_lateral" or "v_strafe_longitudinal" or "v_strafe_vertical" or "v_mining_throttle";

    private static string Intent(string action, string map)
    {
        if (action is "v_strafe_longitudinal" or "gp_movey" || action.Contains("throttleabs") || action.Contains("moveforward") || action.Contains("moveback")) return "Move Forward / Backward";
        if (action is "v_strafe_lateral" or "gp_movex" || action.Contains("moveleft") || action.Contains("moveright")) return "Move Left / Right";
        if (action is "v_strafe_vertical" || action.Contains("strafe_up") || action.Contains("strafe_down")) return "Move Up / Down";
        if (action.Contains("pitch")) return "Rotate Pitch";
        if (action.Contains("yaw")) return "Rotate Yaw";
        if (action.Contains("roll")) return "Rotate Roll";
        if (action is "attack1" || action.Contains("attack1") || action.Contains("weapon_fire_group_1")) return "Primary Fire";
        if (action.Contains("attacksecondary") || action.Contains("weapon_fire_group_2")) return "Secondary Fire";
        if (action.Contains("operator_mode")) return "Operator Mode";
        if (action.Contains("master_mode")) return "Master Mode";
        if (action.Contains("quantum") || action.Contains("qdrive")) return "Quantum Drive";
        if (action.Contains("scan")) return "Scanning";
        if (action.Contains("mining_throttle")) return "Mining Power";
        if (action.Contains("interaction") || action.Contains("pc_select")) return "Interact";
        if (action.Contains("jump")) return "Jump";
        if (action.Contains("sprint") || action.Contains("afterburner")) return "Boost / Sprint";
        if (action.Contains("crouch")) return "Crouch";
        if (action.Contains("reload")) return "Reload";
        if (map.Contains("target")) return "Targeting";
        return StarbindText.Humanize(action);
    }

    private static string Category(string intent, string action, string map)
    {
        if (intent.StartsWith("Move", StringComparison.OrdinalIgnoreCase) || intent.StartsWith("Rotate", StringComparison.OrdinalIgnoreCase) || intent.Contains("Boost")) return "Movement";
        if (intent.Contains("Fire") || map.Contains("missile") || map.Contains("defensive")) return "Weapons";
        if (intent.Contains("Target") || map.Contains("target")) return "Targeting";
        if (intent.Contains("Scan") || map.Contains("radar")) return "Scanning";
        if (map.Contains("mining")) return "Mining";
        if (map.Contains("salvage")) return "Salvage";
        if (action.Contains("view")) return "Camera / View";
        if (intent.Contains("Interact")) return "Interaction";
        return "Systems";
    }

    private static string Description(string context, string intent, string behavior, string actionName, string mapName)
    {
        var core = intent switch
        {
            "Move Forward / Backward" => context switch
            {
                "Flight" => "Translates the ship forward or backward along its longitudinal axis.",
                "Vehicle" => "Controls forward and reverse ground-vehicle movement.",
                "On Foot" => "Moves the character forward or backward.",
                _ => "Moves forward or backward in the current game state."
            },
            "Move Left / Right" => context == "Flight" ? "Translates the ship left or right without changing its facing." : "Moves left or right in the current game state.",
            "Move Up / Down" => "Translates vertically up or down.",
            "Rotate Pitch" => "Rotates the view or vehicle nose up and down.",
            "Rotate Yaw" => "Rotates the view or vehicle left and right.",
            "Rotate Roll" => "Rolls the ship around its forward axis.",
            "Primary Fire" => "Activates the primary weapon or primary attack for this state.",
            "Secondary Fire" => "Activates the secondary weapon or alternate attack for this state.",
            "Operator Mode" => "Changes or cycles the active operator task mode inside the current master mode.",
            "Master Mode" => "Changes the ship's broad operating mode. This is distinct from operator mode.",
            "Quantum Drive" => "Controls quantum-travel mode or quantum-drive engagement.",
            "Scanning" => "Controls scanning, pinging, or scan-focus behavior.",
            "Mining Power" => "Controls the mining laser's power level.",
            _ => $"{StarbindText.Humanize(actionName)} in the {StarbindText.Humanize(mapName)} action map."
        };
        return $"{core} Behavior: {behavior}. Starbind keeps toggle, cycle, hold, direct, and axis actions separate even when their names look similar.";
    }
}

public sealed record StarbindActionInfo(string Context, string Category, string Intent, string Behavior, string Description);
