using System.Globalization;
using System.Xml.Linq;

namespace StarConfig;

public sealed partial class StarbindProfileService
{
    private IReadOnlyList<StarbindDevice> ParseDevices(XElement profileElement, IReadOnlyList<InputDevice> detectedDevices)
    {
        var result = new List<StarbindDevice>
        {
            new(1, "Keyboard", StarbindDeviceKind.Keyboard, 0, 0, true),
            new(1, "Mouse", StarbindDeviceKind.Mouse, 5, 2, true)
        };

        foreach (var option in profileElement.Elements().Where(x => x.Name.LocalName.Equals("options", StringComparison.OrdinalIgnoreCase)))
        {
            var type = ((string?)option.Attribute("type") ?? string.Empty).ToLowerInvariant();
            if (type is not ("joystick" or "gamepad")) continue;
            var instance = (int?)option.Attribute("instance") ?? 1;
            var product = StarbindText.TrimProductGuid((string?)option.Attribute("Product"));
            var detected = detectedDevices.FirstOrDefault(x => x.Id == instance);
            if (product.Equals("Unknown device", StringComparison.OrdinalIgnoreCase) && detected is not null) product = detected.Name;
            if (product.Equals("Unknown device", StringComparison.OrdinalIgnoreCase)) product = type == "gamepad" ? $"Gamepad {instance}" : $"Joystick {instance}";
            result.Add(new StarbindDevice(instance, product, type == "gamepad" ? StarbindDeviceKind.Gamepad : StarbindDeviceKind.Joystick,
                detected?.Buttons ?? 32, detected?.Axes ?? 6, detected is not null));
        }

        foreach (var detected in detectedDevices.Where(d => result.All(x => x.Kind != StarbindDeviceKind.Joystick || x.Instance != d.Id)))
            result.Add(new StarbindDevice(detected.Id, detected.Name, StarbindDeviceKind.Joystick, detected.Buttons, detected.Axes, true));

        return result.GroupBy(x => (x.Kind, x.Instance)).Select(x => x.First()).OrderBy(x => x.Kind).ThenBy(x => x.Instance).ToList();
    }

    private IReadOnlyList<StarbindAction> ParseActions(XElement profileElement)
    {
        var result = new List<StarbindAction>();
        foreach (var actionMap in profileElement.Elements().Where(IsActionMap))
        {
            var mapName = (string?)actionMap.Attribute("name") ?? "Unknown";
            var actions = actionMap.Elements().Where(IsAction).ToList();
            for (var actionIndex = 0; actionIndex < actions.Count; actionIndex++)
            {
                var action = actions[actionIndex];
                var actionName = (string?)action.Attribute("name") ?? "Unknown";
                var info = _knowledge.Explain(mapName, actionName);
                var rebinds = action.Elements().Where(IsRebind).ToList();
                if (rebinds.Count == 0)
                {
                    result.Add(new StarbindAction(mapName, actionName, actionIndex, -1, StarbindInput.Unbound, info.Context, info.Category, info.Intent, info.Behavior, info.Description, new Dictionary<string, string>()));
                    continue;
                }
                for (var rebindIndex = 0; rebindIndex < rebinds.Count; rebindIndex++)
                {
                    var rebind = rebinds[rebindIndex];
                    var attributes = rebind.Attributes().Where(x => !x.Name.LocalName.Equals("input", StringComparison.OrdinalIgnoreCase)).ToDictionary(x => x.Name.LocalName, x => x.Value, StringComparer.OrdinalIgnoreCase);
                    result.Add(new StarbindAction(mapName, actionName, actionIndex, rebindIndex, StarbindInput.Normalize((string?)rebind.Attribute("input")), info.Context, info.Category, info.Intent, info.Behavior, info.Description, attributes));
                }
            }
        }
        return result.OrderBy(x => ContextOrder(x.Context)).ThenBy(x => x.Category).ThenBy(x => x.DisplayName).ToList();
    }

    private IReadOnlyList<StarbindDeviceAxisOption> ParseAxisOptions(XElement profileElement, IReadOnlyList<StarbindDevice> devices)
    {
        var result = new List<StarbindDeviceAxisOption>();
        foreach (var deviceOptions in profileElement.Elements().Where(x => x.Name.LocalName.Equals("deviceoptions", StringComparison.OrdinalIgnoreCase)))
        {
            var product = StarbindText.TrimProductGuid((string?)deviceOptions.Attribute("name"));
            var device = devices.FirstOrDefault(x => x.ProductName.Equals(product, StringComparison.OrdinalIgnoreCase));
            foreach (var option in deviceOptions.Elements().Where(x => x.Name.LocalName.Equals("option", StringComparison.OrdinalIgnoreCase)))
            {
                var axis = (string?)option.Attribute("input") ?? string.Empty;
                if (string.IsNullOrWhiteSpace(axis)) continue;
                var deadzone = double.TryParse((string?)option.Attribute("deadzone"), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
                result.Add(new StarbindDeviceAxisOption(product, device?.Instance ?? 1, axis, Math.Clamp(deadzone, 0, 0.95), false));
            }
        }
        return result;
    }

    public static string ClassifyContext(string mapName, string actionName)
    {
        var text = $"{mapName} {actionName}".ToLowerInvariant();
        if (text.Contains("salvage")) return "Salvage";
        if (text.Contains("mining")) return "Mining";
        if (text.Contains("turret")) return "Turret";
        if (text.Contains("eva")) return "EVA";
        if (text.Contains("vehicle") || text.Contains("groundvehicle") || text.Contains("ground_vehicle")) return "Vehicle";
        if (text.Contains("player") || text.Contains("fps") || text.Contains("actor")) return "On Foot";
        if (text.Contains("spaceship") || text.Contains("seat_") || actionName.StartsWith("v_", StringComparison.OrdinalIgnoreCase)) return "Flight";
        return "General";
    }

    private static int ContextOrder(string context) => context switch
    {
        "Flight" => 0, "Vehicle" => 1, "On Foot" => 2, "EVA" => 3, "Turret" => 4, "Mining" => 5, "Salvage" => 6, _ => 7
    };

    private static string DetectChannel(string filePath)
    {
        foreach (var segment in filePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            if (segment.Equals("LIVE", StringComparison.OrdinalIgnoreCase) || segment.Equals("PTU", StringComparison.OrdinalIgnoreCase)
                || segment.Equals("EPTU", StringComparison.OrdinalIgnoreCase) || segment.Equals("TECH-PREVIEW", StringComparison.OrdinalIgnoreCase)) return segment.ToUpperInvariant();
        return "CUSTOM";
    }

    private static bool IsActionMap(XElement element) => element.Name.LocalName.Equals("actionmap", StringComparison.OrdinalIgnoreCase);
    private static bool IsAction(XElement element) => element.Name.LocalName.Equals("action", StringComparison.OrdinalIgnoreCase);
    private static bool IsRebind(XElement element) => element.Name.LocalName.Equals("rebind", StringComparison.OrdinalIgnoreCase);

    private sealed class ContextInputComparer : IEqualityComparer<(string Context, string Input)>
    {
        public bool Equals((string Context, string Input) x, (string Context, string Input) y) => x.Context.Equals(y.Context, StringComparison.OrdinalIgnoreCase) && x.Input.Equals(y.Input, StringComparison.OrdinalIgnoreCase);
        public int GetHashCode((string Context, string Input) obj) => HashCode.Combine(obj.Context.ToLowerInvariant(), obj.Input.ToLowerInvariant());
    }
}
