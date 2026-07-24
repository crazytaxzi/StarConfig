using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StarConfig;

public enum HardwareFamily
{
    Keyboard,
    Mouse,
    Joystick,
    Throttle,
    Pedals,
    Gamepad,
    Generic
}

public sealed record HardwareControlDefinition(
    string InputSuffix,
    string FriendlyName,
    StarbindControlKind Kind,
    string Group,
    double HotspotX,
    double HotspotY,
    double LabelX,
    double LabelY,
    string? Description = null);

public sealed record HardwareTemplate(
    string Id,
    string DisplayName,
    HardwareFamily Family,
    IReadOnlyList<string> ProductMatches,
    IReadOnlyList<HardwareControlDefinition> Controls,
    string ArtworkKey)
{
    public bool Matches(string productName) => ProductMatches.Any(match => productName.Contains(match, StringComparison.OrdinalIgnoreCase));
}

public sealed record ActionChoice(StarbindAction Action, string Label)
{
    public override string ToString() => Label;
}

public sealed class PlannedStateBinding : INotifyPropertyChanged
{
    private bool _enabled;
    private StarbindAction? _action;
    private string _status = "EMPTY";

    public required string Context { get; init; }
    public required IReadOnlyList<StarbindAction> Choices { get; init; }
    public required IReadOnlyList<StarbindAction> Existing { get; init; }

    public bool Enabled
    {
        get => _enabled;
        set { if (_enabled == value) return; _enabled = value; Changed(); }
    }

    public StarbindAction? Action
    {
        get => _action;
        set { if (ReferenceEquals(_action, value)) return; _action = value; Changed(); Changed(nameof(ActionLabel)); }
    }

    public string Status
    {
        get => _status;
        set { if (_status == value) return; _status = value; Changed(); }
    }

    public string ActionLabel => Action?.DisplayName ?? "Not assigned";
    public bool HasConflict => Existing.Count > 1;

    public PlannedStateBinding Clone() => new()
    {
        Context = Context,
        Choices = Choices,
        Existing = Existing,
        Enabled = Enabled,
        Action = Action,
        Status = Status
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Changed([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class ControlBindingPlan
{
    public required string Input { get; init; }
    public required string FriendlyName { get; init; }
    public Dictionary<string, PlannedStateBinding> States { get; } = new(StringComparer.OrdinalIgnoreCase);
    public bool IsDirty { get; set; }
}

public sealed record AxisTuningChange(
    string Input,
    string DeviceProduct,
    int DeviceInstance,
    string Axis,
    double Deadzone,
    double Exponent,
    string CurveName);

public sealed record ConflictGroup(
    string Context,
    string Input,
    IReadOnlyList<StarbindAction> Actions,
    StarbindAction Recommended);

public sealed record ConflictResolution(
    string Context,
    string Input,
    StarbindAction KeepAction);

public sealed record ProfileListItem(string FilePath, string Name, string Channel, DateTime Modified)
{
    public override string ToString() => $"{Name}  [{Channel}]";
}

public sealed class StarbindV5Settings
{
    public string? LastProfile { get; set; }
    public string? LauncherPath { get; set; }
    public List<string> ProfileFolders { get; set; } = [];
    public Dictionary<string, string> DeviceRoleOverrides { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public bool ConfirmBeforeWrite { get; set; } = true;
}

public sealed record DeviceActivityRow(DateTime Time, string Control, string Input, string Value)
{
    public string Display => $"{Time:HH:mm:ss.fff}   {Control,-18} {Input,-20} {Value}";
}
