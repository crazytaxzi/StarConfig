using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace StarConfig;

public enum StarbindDeviceKind { Keyboard, Mouse, Joystick, Gamepad, Unknown }
public enum StarbindControlKind { Key, MouseButton, Axis, Button, Hat, Unknown }
public enum BindingMutationKind { RemoveInput, AddInput }

public sealed record StarbindDevice(
    int Instance,
    string ProductName,
    StarbindDeviceKind Kind,
    int Buttons,
    int Axes,
    bool IsConnected)
{
    public string InputPrefix => Kind switch
    {
        StarbindDeviceKind.Keyboard => "kb1",
        StarbindDeviceKind.Mouse => "mouse1",
        StarbindDeviceKind.Gamepad => $"gp{Instance}",
        _ => $"js{Instance}"
    };

    public string SlotLabel => Kind switch
    {
        StarbindDeviceKind.Keyboard => "KEYBOARD",
        StarbindDeviceKind.Mouse => "MOUSE",
        StarbindDeviceKind.Gamepad => $"GAMEPAD {Instance}",
        _ => $"JOY{Instance}"
    };
}

public sealed record StarbindControl(
    string Input,
    string DisplayName,
    StarbindControlKind Kind,
    int DeviceInstance,
    string DevicePrefix,
    string ShortCode)
{
    public bool IsAxis => Kind == StarbindControlKind.Axis;
}

public sealed record StarbindAction(
    string ActionMap,
    string ActionName,
    int ActionOrdinal,
    int RebindOrdinal,
    string Input,
    string Context,
    string Category,
    string Intent,
    string Behavior,
    string Description,
    IReadOnlyDictionary<string, string> Attributes)
{
    public string Identity => $"{ActionMap}|{ActionOrdinal}|{ActionName}|{RebindOrdinal}";
    public string DisplayName => StarbindActionNames.Friendly(Context, Intent, Behavior, ActionName);
    public bool IsBound => !StarbindInput.IsUnbound(Input);
}

public sealed record StarbindDeviceAxisOption(
    string DeviceProduct,
    int DeviceInstance,
    string Axis,
    double Deadzone,
    bool Inverted,
    double Exponent = 1.0);

public sealed class StarbindProfile
{
    public required string FilePath { get; init; }
    public required string ProfileName { get; init; }
    public required string Channel { get; init; }
    public required IReadOnlyList<StarbindDevice> Devices { get; init; }
    public required IReadOnlyList<StarbindAction> Actions { get; init; }
    public required IReadOnlyList<StarbindDeviceAxisOption> AxisOptions { get; init; }
}

public sealed record BindingMutation(
    BindingMutationKind Kind,
    string Context,
    string ActionMap,
    int ActionOrdinal,
    string ActionName,
    string Input);

public sealed class StateAssignment : INotifyPropertyChanged
{
    private bool _enabled;
    private StarbindAction? _selectedAction;
    private string _status = "EMPTY";
    private Brush? _statusBrush;

    public required string Context { get; init; }
    public required IReadOnlyList<StarbindAction> Choices { get; init; }
    public required IReadOnlyList<StarbindAction> ExistingAssignments { get; init; }

    public bool Enabled
    {
        get => _enabled;
        set { if (_enabled == value) return; _enabled = value; Changed(); }
    }

    public StarbindAction? SelectedAction
    {
        get => _selectedAction;
        set { if (ReferenceEquals(_selectedAction, value)) return; _selectedAction = value; Changed(); Changed(nameof(SelectedLabel)); }
    }

    public string Status
    {
        get => _status;
        set { if (_status == value) return; _status = value; Changed(); }
    }

    public Brush? StatusBrush
    {
        get => _statusBrush;
        set { if (ReferenceEquals(_statusBrush, value)) return; _statusBrush = value; Changed(); }
    }

    public string SelectedLabel => SelectedAction?.DisplayName ?? "Not assigned";
    public bool HasConflict => ExistingAssignments.Count > 1;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Changed([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed record StarbindWarning(string Icon, string Text, Brush Color);

public static class StarbindActionNames
{
    private static readonly IReadOnlyDictionary<string, string> Exact = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["v_strafe_longitudinal"] = "Move Forward / Backward",
        ["v_strafe_lateral"] = "Strafe Left / Right",
        ["v_strafe_vertical"] = "Strafe Up / Down",
        ["v_pitch"] = "Pitch Up / Down",
        ["v_yaw"] = "Yaw Left / Right",
        ["v_roll"] = "Roll Left / Right",
        ["vehicle_throttle_abs"] = "Throttle - Forward / Backward",
        ["vehicle_steer"] = "Steering - Left / Right",
        ["moveforward"] = "Move Forward / Backward",
        ["moveback"] = "Move Backward",
        ["moveleft"] = "Move Left",
        ["moveright"] = "Move Right",
        ["jump"] = "Jump",
        ["sprint"] = "Sprint",
        ["crouch"] = "Crouch",
        ["reload"] = "Reload",
        ["v_afterburner"] = "Boost / Afterburner",
        ["v_weapon_fire_group_1"] = "Fire Weapon Group 1",
        ["v_weapon_fire_group_2"] = "Fire Weapon Group 2",
        ["v_missile_mode_toggle"] = "Toggle Missile Operator Mode",
        ["v_target_cycle_friendly_fwd"] = "Cycle Friendly Targets",
        ["v_target_cycle_hostile_fwd"] = "Cycle Hostile Targets",
        ["v_target_toggle_lock_index_1"] = "Lock / Unlock Target",
        ["v_operator_mode_cycle"] = "Cycle Operator Mode",
        ["v_master_mode_cycle"] = "Cycle Master Mode",
        ["v_atc_loading_area_request"] = "Request Landing / Loading Area",
        ["v_autoland"] = "Automatic Landing",
        ["v_ifcs_toggle_cruise_control"] = "Toggle Cruise Control",
        ["turret_elevation"] = "Turret Elevation",
        ["turret_azimuth"] = "Turret Left / Right",
        ["v_mining_throttle"] = "Mining Laser Power",
        ["v_mining_laser_fire"] = "Activate Mining Laser",
        ["v_salvage_beam_axis"] = "Salvage Beam Strength",
        ["v_salvage_beam_toggle"] = "Toggle Salvage Beam",
        ["foip_pushtotalk"] = "Push to Talk",
        ["v_speed_limiter_abs"] = "Speed Limiter",
        ["v_speed_limiter_rel"] = "Adjust Speed Limiter",
        ["v_landing_gear_toggle"] = "Toggle Landing Gear",
        ["v_toggle_decoupled_mode"] = "Toggle Coupled / Decoupled Mode",
        ["v_quantum_mode_toggle"] = "Toggle Quantum Operator Mode",
        ["v_quantum_drive_engage"] = "Engage Quantum Drive",
        ["v_scan_trigger_scan"] = "Start Scan",
        ["v_invoke_ping"] = "Radar Ping"
    };

    public static string Friendly(string context, string intent, string behavior, string actionName)
    {
        if (Exact.TryGetValue(actionName, out var exact)) return exact;
        if (intent.Equals("Move Forward / Backward", StringComparison.OrdinalIgnoreCase))
            return context switch { "Vehicle" => "Throttle - Forward / Backward", _ => "Move Forward / Backward" };
        if (intent.Equals("Move Left / Right", StringComparison.OrdinalIgnoreCase))
            return context switch { "Flight" or "EVA" => "Strafe Left / Right", "Vehicle" => "Steering - Left / Right", _ => "Move Left / Right" };
        if (intent.Equals("Move Up / Down", StringComparison.OrdinalIgnoreCase))
            return context is "Flight" or "EVA" ? "Strafe Up / Down" : "Move Up / Down";
        if (intent.Equals("Boost / Sprint", StringComparison.OrdinalIgnoreCase))
            return context switch { "On Foot" => "Sprint", "Flight" => "Boost / Afterburner", _ => "Boost" };
        if (intent.Equals("Operator Mode", StringComparison.OrdinalIgnoreCase)) return behavior == "Cycle" ? "Cycle Operator Mode" : behavior == "Toggle" ? "Toggle Operator Mode" : "Operator Mode";
        if (intent.Equals("Master Mode", StringComparison.OrdinalIgnoreCase)) return behavior == "Cycle" ? "Cycle Master Mode" : behavior == "Toggle" ? "Toggle Master Mode" : "Master Mode";
        if (intent.Equals("Primary Fire", StringComparison.OrdinalIgnoreCase))
            return context switch { "Mining" => "Activate Mining Laser", "Salvage" => "Activate Salvage Tool", "Flight" => "Fire Weapon Group 1", _ => "Primary Fire" };
        if (intent.Equals("Secondary Fire", StringComparison.OrdinalIgnoreCase))
            return context == "Flight" ? "Fire Weapon Group 2" : "Secondary Fire";
        if (!string.IsNullOrWhiteSpace(intent) && intent is not "Targeting" and not "Ship Systems" and not "Vehicle Systems" and not "Character Systems" and not "Systems") return intent;
        return StarbindText.Humanize(actionName);
    }
}

public static class StarbindInput
{
    public const string Unbound = "Unbound";

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return Unbound;
        var trimmed = value.Trim();
        if (trimmed.Equals(Unbound, StringComparison.OrdinalIgnoreCase)) return Unbound;
        var underscore = trimmed.IndexOf('_');
        if (underscore >= 0 && string.IsNullOrWhiteSpace(trimmed[(underscore + 1)..])) return Unbound;
        return trimmed;
    }

    public static bool IsUnbound(string? value) => Normalize(value).Equals(Unbound, StringComparison.OrdinalIgnoreCase);

    public static (string Prefix, string Suffix) Split(string input)
    {
        var normalized = Normalize(input);
        if (IsUnbound(normalized)) return (string.Empty, string.Empty);
        var index = normalized.IndexOf('_');
        return index < 0 ? (normalized, string.Empty) : (normalized[..index], normalized[(index + 1)..]);
    }

    public static StarbindControlKind KindOf(string input)
    {
        var (prefix, suffix) = Split(input);
        if (prefix.StartsWith("kb", StringComparison.OrdinalIgnoreCase)) return StarbindControlKind.Key;
        if (prefix.StartsWith("mouse", StringComparison.OrdinalIgnoreCase)) return StarbindControlKind.MouseButton;
        if (suffix.Contains("hat", StringComparison.OrdinalIgnoreCase)) return StarbindControlKind.Hat;
        if (suffix.Contains("button", StringComparison.OrdinalIgnoreCase)) return StarbindControlKind.Button;
        if (!string.IsNullOrWhiteSpace(suffix)) return StarbindControlKind.Axis;
        return StarbindControlKind.Unknown;
    }

    public static int DeviceInstance(string input)
    {
        var (prefix, _) = Split(input);
        var digits = new string(prefix.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var instance) ? instance : 1;
    }
}

public static class StarbindText
{
    public static string Humanize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        var cleaned = value.Trim();
        if (cleaned.StartsWith("v_", StringComparison.OrdinalIgnoreCase)) cleaned = cleaned[2..];
        cleaned = cleaned.Replace('_', ' ');
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, "([a-z0-9])([A-Z])", "$1 $2");
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, "\\s+", " ").Trim();
        return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(cleaned.ToLowerInvariant())
            .Replace("Ifcs", "IFCS", StringComparison.Ordinal)
            .Replace("Atc", "ATC", StringComparison.Ordinal)
            .Replace("Eva", "EVA", StringComparison.Ordinal)
            .Replace("Foip", "FOIP", StringComparison.Ordinal)
            .Replace("Qdrive", "Quantum Drive", StringComparison.Ordinal);
    }

    public static string TrimProductGuid(string? product)
    {
        if (string.IsNullOrWhiteSpace(product)) return "Unknown device";
        var cleaned = System.Text.RegularExpressions.Regex.Replace(product, "\\s*\\{[0-9A-Fa-f-]{20,}\\}\\s*$", string.Empty).Trim();
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, "\\s+", " ");
        return string.IsNullOrWhiteSpace(cleaned) ? "Unknown device" : cleaned;
    }
}
