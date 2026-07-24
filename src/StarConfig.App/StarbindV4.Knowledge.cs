namespace StarConfig;

public sealed class StarbindActionKnowledge
{
    private static readonly (string[] Tokens, string Intent, string Category, string Description)[] Rules =
    [
        (["strafe_longitudinal", "throttleabs", "moveforward", "moveback", "forward_backward"], "Move Forward / Backward", "Movement", "Moves forward or backward relative to the current orientation."),
        (["strafe_lateral", "movex", "moveleft", "moveright", "left_right"], "Move Left / Right", "Movement", "Moves left or right without changing the current facing."),
        (["strafe_vertical", "strafe_up", "strafe_down", "moveup", "movedown"], "Move Up / Down", "Movement", "Moves vertically up or down."),
        (["pitch"], "Rotate Pitch", "Movement", "Rotates the view, character or vehicle nose up and down."),
        (["yaw"], "Rotate Yaw", "Movement", "Rotates the view, character or vehicle left and right."),
        (["roll"], "Rotate Roll", "Movement", "Rolls the ship or vehicle around its forward axis."),
        (["afterburner", "sprint", "boost"], "Boost / Sprint", "Movement", "Applies the faster movement or boost function for the current state."),
        (["jump"], "Jump", "Movement", "Makes the character jump or perform the equivalent upward movement."),
        (["crouch", "stance_crouch"], "Crouch", "Movement", "Changes the character to or from a crouched stance."),
        (["prone"], "Prone", "Movement", "Changes the character to or from a prone stance."),
        (["attack1", "weapon_fire_group_1", "fire_primary"], "Primary Fire", "Weapons", "Activates the primary weapon or attack for this game state."),
        (["attacksecondary", "weapon_fire_group_2", "fire_secondary"], "Secondary Fire", "Weapons", "Activates the secondary weapon or alternate attack for this game state."),
        (["reload"], "Reload", "Weapons", "Reloads the currently equipped weapon or tool."),
        (["melee"], "Melee Attack", "Weapons", "Performs the applicable close-range attack."),
        (["missile", "ordnance"], "Missile Control", "Weapons", "Controls missile selection, lock, firing or ordnance behavior."),
        (["countermeasure", "flare", "noise", "decoy"], "Countermeasures", "Defensive", "Deploys or manages defensive countermeasures."),
        (["shield"], "Shield Control", "Defensive", "Adjusts, focuses or redistributes shield power."),
        (["target"], "Targeting", "Targeting", "Selects, cycles, pins or manages a target."),
        (["scan", "ping", "radar"], "Scanning", "Scanning", "Controls scanning, radar, pinging or scan focus."),
        (["operator_mode"], "Operator Mode", "Modes", "Changes the active operator task inside the current master mode."),
        (["master_mode"], "Master Mode", "Modes", "Changes the ship's broad operating mode. This is separate from operator mode."),
        (["quantum", "qdrive"], "Quantum Drive", "Navigation", "Controls quantum travel mode, route engagement or quantum drive operation."),
        (["autoland", "landing", "landing_gear"], "Landing", "Flight Systems", "Controls landing assistance, landing gear or landing requests."),
        (["atc", "loading_area_request", "hangar_request"], "ATC Request", "Flight Systems", "Requests landing, takeoff or loading-area service from local traffic control."),
        (["cruise"], "Cruise Control", "Flight Systems", "Controls cruise speed or cruise-control engagement."),
        (["speed_limiter"], "Speed Limiter", "Flight Systems", "Adjusts or toggles the ship speed limiter."),
        (["coupled"], "Coupled Mode", "Flight Systems", "Controls coupled or decoupled flight behavior."),
        (["esp"], "ESP", "Flight Systems", "Controls enhanced stick precision assistance."),
        (["gforce", "g_safe"], "G-Safe", "Flight Systems", "Controls acceleration safety assistance."),
        (["power", "capacitor"], "Power Management", "Ship Systems", "Adjusts ship power, capacitors or subsystem allocation."),
        (["engine"], "Engine Control", "Ship Systems", "Starts, stops or manages ship engines."),
        (["flight_ready"], "Flight Ready", "Ship Systems", "Sets the ship into its flight-ready state."),
        (["door", "port", "lock"], "Doors and Locks", "Ship Systems", "Controls doors, ports or ship locking functions."),
        (["interaction", "pc_select", "use", "interact"], "Interact", "Interaction", "Activates the highlighted interaction, item or interface element."),
        (["inner_thought"], "Interaction Wheel", "Interaction", "Opens or navigates the contextual interaction interface."),
        (["inventory"], "Inventory", "Interaction", "Opens or manages inventory."),
        (["mobiglas"], "mobiGlas", "Interface", "Opens or controls mobiGlas."),
        (["map"], "Map", "Interface", "Opens or controls the map interface."),
        (["chat"], "Chat", "Interface", "Opens, closes or navigates text chat."),
        (["push_to_talk", "pushtotalk", "voip"], "Push to Talk", "Communication", "Transmits voice communication while the control is active."),
        (["foip", "head_tracking", "optical_tracking"], "Head / Face Tracking", "Camera / View", "Controls face, head or optical tracking features."),
        (["view", "camera", "look"], "Camera / View", "Camera / View", "Controls camera position, view mode or look direction."),
        (["zoom"], "Zoom", "Camera / View", "Adjusts view, scope or camera zoom."),
        (["mining_throttle", "mining_power", "laser_power"], "Mining Power", "Mining", "Controls mining laser output power."),
        (["mining_mode", "mining_laser", "mining"], "Mining Tool", "Mining", "Controls mining mode, mining tools or mining laser functions."),
        (["salvage"], "Salvage Tool", "Salvage", "Controls salvage mode, scraping, fracture or salvage-tool functions."),
        (["tractor"], "Tractor Beam", "Cargo / Utility", "Controls tractor-beam activation, movement or strength."),
        (["cargo"], "Cargo Control", "Cargo / Utility", "Controls cargo-related functions."),
        (["turret"], "Turret Control", "Turret", "Controls turret movement, firing or turret operation."),
        (["eject"], "Eject", "Emergency", "Activates the ejection system. This action should remain intentionally distinct."),
        (["self_destruct"], "Self Destruct", "Emergency", "Arms, disarms or confirms self-destruction. This action should remain intentionally distinct."),
        (["respawn", "suicide"], "Respawn", "Emergency", "Triggers the applicable respawn or character reset action."),
        (["toggle_flashlight", "flashlight"], "Flashlight", "Equipment", "Toggles or controls the equipped flashlight."),
        (["helmet"], "Helmet", "Equipment", "Equips, removes or manages the helmet."),
        (["weapon_select", "select_weapon", "holster"], "Weapon Selection", "Equipment", "Selects, equips or holsters a weapon."),
        (["gadget", "grenade", "medpen"], "Consumable / Gadget", "Equipment", "Selects or uses the applicable gadget or consumable.")
    ];

    public StarbindActionInfo Explain(string mapName, string actionName)
    {
        var context = StarbindProfileService.ClassifyContext(mapName, actionName);
        var lowerAction = actionName.ToLowerInvariant();
        var lowerMap = mapName.ToLowerInvariant();
        var combined = lowerMap + " " + lowerAction;
        var behavior = ClassifyBehavior(lowerAction, lowerMap);
        var rule = Rules.FirstOrDefault(candidate => candidate.Tokens.Any(token => combined.Contains(token, StringComparison.OrdinalIgnoreCase)));
        var intent = rule.Intent ?? StarbindText.Humanize(actionName);
        var category = ContextCategory(context, rule.Category ?? InferCategory(lowerAction, lowerMap));
        var core = rule.Description ?? $"{StarbindText.Humanize(actionName)} in the {StarbindText.Humanize(mapName)} action map.";
        var contextNote = ContextNote(context, intent);
        var distinction = intent switch
        {
            "Operator Mode" => " Operator Mode is a task-level mode and is not interchangeable with Master Mode.",
            "Master Mode" => " Master Mode is the ship's broad operating state and is not interchangeable with Operator Mode.",
            _ => string.Empty
        };
        var description = $"{contextNote}{core}{distinction} Behavior: {behavior}. Starbind keeps Axis, Toggle, Cycle, Hold and Direct actions separate.";
        return new StarbindActionInfo(context, category, intent, behavior, description);
    }

    private static string ClassifyBehavior(string action, string map)
    {
        if (action.Contains("toggle", StringComparison.OrdinalIgnoreCase)) return "Toggle";
        if (action.Contains("cycle", StringComparison.OrdinalIgnoreCase)
            || action.Contains("next", StringComparison.OrdinalIgnoreCase)
            || action.Contains("prev", StringComparison.OrdinalIgnoreCase)
            || action.Contains("increase", StringComparison.OrdinalIgnoreCase)
            || action.Contains("decrease", StringComparison.OrdinalIgnoreCase)
            || action.Contains("inc_", StringComparison.OrdinalIgnoreCase)
            || action.Contains("dec_", StringComparison.OrdinalIgnoreCase)
            || action.EndsWith("_up", StringComparison.OrdinalIgnoreCase)
            || action.EndsWith("_down", StringComparison.OrdinalIgnoreCase)) return "Cycle";
        if (action.Contains("hold", StringComparison.OrdinalIgnoreCase)
            || action.Contains("push_to_talk", StringComparison.OrdinalIgnoreCase)
            || action.Contains("pushtotalk", StringComparison.OrdinalIgnoreCase)) return "Hold";
        if (action.Contains("abs", StringComparison.OrdinalIgnoreCase)
            || action.Contains("axis", StringComparison.OrdinalIgnoreCase)
            || IsAxisAction(action, map)) return "Axis";
        return "Direct";
    }

    private static bool IsAxisAction(string action, string map)
    {
        if (action is "gp_movex" or "gp_movey" or "gp_rotatepitch" or "gp_rotateyaw" or "v_roll" or "v_yaw" or "v_pitch" or "v_strafe_lateral" or "v_strafe_longitudinal" or "v_strafe_vertical" or "v_mining_throttle") return true;
        return action.Contains("throttle", StringComparison.OrdinalIgnoreCase)
            || action.Contains("strafe", StringComparison.OrdinalIgnoreCase)
            || action.Contains("rotate", StringComparison.OrdinalIgnoreCase)
            || action.Contains("lookx", StringComparison.OrdinalIgnoreCase)
            || action.Contains("looky", StringComparison.OrdinalIgnoreCase)
            || map.Contains("movement", StringComparison.OrdinalIgnoreCase) && action.Contains("move", StringComparison.OrdinalIgnoreCase);
    }

    private static string InferCategory(string action, string map)
    {
        if (map.Contains("movement") || action.Contains("move") || action.Contains("strafe") || action.Contains("rotate")) return "Movement";
        if (map.Contains("weapon") || map.Contains("missile") || action.Contains("attack") || action.Contains("fire")) return "Weapons";
        if (map.Contains("target")) return "Targeting";
        if (map.Contains("mining")) return "Mining";
        if (map.Contains("salvage")) return "Salvage";
        if (map.Contains("turret")) return "Turret";
        if (map.Contains("camera") || action.Contains("view")) return "Camera / View";
        if (map.Contains("interaction") || action.Contains("select")) return "Interaction";
        return "Systems";
    }

    private static string ContextCategory(string context, string category)
    {
        if (category is "Systems" or "Movement")
        {
            return context switch
            {
                "Flight" when category == "Systems" => "Ship Systems",
                "Vehicle" when category == "Systems" => "Vehicle Systems",
                "On Foot" when category == "Systems" => "Character Systems",
                _ => category
            };
        }
        return category;
    }

    private static string ContextNote(string context, string intent) => intent switch
    {
        "Move Forward / Backward" => context switch
        {
            "Flight" => "For ships, this translates along the longitudinal axis. ",
            "Vehicle" => "For ground vehicles, this controls forward and reverse movement. ",
            "On Foot" => "For characters, this controls forward and backward movement. ",
            _ => string.Empty
        },
        "Primary Fire" when context == "Mining" => "In mining, primary fire activates the mining tool rather than a weapon. ",
        "Primary Fire" when context == "Salvage" => "In salvage, primary fire activates the selected salvage tool. ",
        _ => string.Empty
    };
}

public sealed record StarbindActionInfo(string Context, string Category, string Intent, string Behavior, string Description);
