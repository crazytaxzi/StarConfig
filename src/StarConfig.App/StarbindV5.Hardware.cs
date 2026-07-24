namespace StarConfig;

public sealed class HardwareDefinitionService
{
    private readonly IReadOnlyList<HardwareTemplate> _templates;

    public HardwareDefinitionService()
    {
        _templates =
        [
            BuildVkbGladiator(),
            BuildVkbStecs(),
            BuildLogitechRudder(),
            BuildThrustmasterT16000(),
            BuildVirpilStick(),
            BuildGenericJoystick(),
            BuildKeyboard(),
            BuildMouse(),
            BuildGamepad()
        ];
    }

    public HardwareTemplate Resolve(StarbindDevice device, StarbindV5Settings settings)
    {
        if (settings.DeviceRoleOverrides.TryGetValue(DeviceKey(device), out var overrideId))
        {
            var overridden = _templates.FirstOrDefault(template => template.Id.Equals(overrideId, StringComparison.OrdinalIgnoreCase));
            if (overridden is not null) return overridden;
        }

        return device.Kind switch
        {
            StarbindDeviceKind.Keyboard => _templates.First(template => template.Family == HardwareFamily.Keyboard),
            StarbindDeviceKind.Mouse => _templates.First(template => template.Family == HardwareFamily.Mouse),
            StarbindDeviceKind.Gamepad => _templates.First(template => template.Family == HardwareFamily.Gamepad),
            _ => _templates.FirstOrDefault(template => template.Family is HardwareFamily.Joystick or HardwareFamily.Throttle or HardwareFamily.Pedals
                && template.Matches(device.ProductName)) ?? _templates.First(template => template.Id == "generic-joystick")
        };
    }

    public IReadOnlyList<HardwareTemplate> Templates => _templates;

    public static string DeviceKey(StarbindDevice device) => $"{device.Kind}|{device.Instance}|{device.ProductName}";

    public IReadOnlyList<StarbindControl> BuildControls(StarbindDevice device, StarbindProfile? profile, StarbindV5Settings settings)
    {
        var template = Resolve(device, settings);
        var controls = new Dictionary<string, StarbindControl>(StringComparer.OrdinalIgnoreCase);
        foreach (var definition in template.Controls)
        {
            var input = ComposeInput(device, definition.InputSuffix);
            controls[input] = new StarbindControl(input, definition.FriendlyName, definition.Kind, device.Instance, device.InputPrefix, definition.InputSuffix.ToUpperInvariant());
        }

        if (profile is not null)
        {
            foreach (var action in profile.Actions.Where(action => action.IsBound && action.Input.StartsWith(device.InputPrefix + "_", StringComparison.OrdinalIgnoreCase)))
            {
                var input = StarbindInput.Normalize(action.Input);
                if (controls.ContainsKey(input)) continue;
                var (_, suffix) = StarbindInput.Split(input);
                controls[input] = new StarbindControl(input, FriendlyFallback(suffix), StarbindInput.KindOf(input), device.Instance, device.InputPrefix, suffix.ToUpperInvariant());
            }
        }

        return controls.Values
            .OrderBy(control => GroupOrder(template, control))
            .ThenBy(control => NaturalOrder(control.Input))
            .ToList();
    }

    public HardwareControlDefinition? FindDefinition(StarbindDevice device, StarbindControl control, StarbindV5Settings settings)
    {
        var template = Resolve(device, settings);
        var (_, suffix) = StarbindInput.Split(control.Input);
        return template.Controls.FirstOrDefault(item => item.InputSuffix.Equals(suffix, StringComparison.OrdinalIgnoreCase));
    }

    private static string ComposeInput(StarbindDevice device, string suffix) => $"{device.InputPrefix}_{suffix}";

    private static string FriendlyFallback(string suffix)
    {
        var lower = suffix.ToLowerInvariant();
        if (lower.StartsWith("button")) return "Button " + new string(lower.Where(char.IsDigit).ToArray());
        if (lower.StartsWith("hat")) return StarbindText.Humanize(lower.Replace("hat1", "Hat Switch"));
        return lower switch
        {
            "x" => "X Axis",
            "y" => "Y Axis",
            "z" => "Z Axis",
            "rotx" => "RX Axis",
            "roty" => "RY Axis",
            "rotz" => "RZ Axis",
            "slider1" => "Slider 1",
            "slider2" => "Slider 2",
            _ => StarbindText.Humanize(suffix)
        };
    }

    private static int GroupOrder(HardwareTemplate template, StarbindControl control)
    {
        var definition = template.Controls.FirstOrDefault(item => item.InputSuffix.Equals(StarbindInput.Split(control.Input).Suffix, StringComparison.OrdinalIgnoreCase));
        var group = definition?.Group ?? control.Kind.ToString();
        return group switch
        {
            "Axes" => 0,
            "Triggers" => 1,
            "Buttons" => 2,
            "Hats" => 3,
            "Encoders" => 4,
            "System Controls" => 5,
            "Keys" => 6,
            _ => 7
        };
    }

    private static int NaturalOrder(string input)
    {
        var suffix = StarbindInput.Split(input).Suffix;
        var digits = new string(suffix.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var number) ? number : suffix.GetHashCode(StringComparison.OrdinalIgnoreCase);
    }

    private static HardwareTemplate BuildVkbGladiator()
    {
        var controls = new List<HardwareControlDefinition>
        {
            Axis("x", "X Axis", 0.51, 0.90, 0.66, 0.86, "Left / Right"),
            Axis("y", "Y Axis", 0.51, 0.86, 0.65, 0.80, "Forward / Back"),
            Axis("z", "Z Axis", 0.52, 0.93, 0.68, 0.90, "Twist"),
            Axis("rotx", "RX Axis", 0.34, 0.80, 0.18, 0.78),
            Axis("roty", "RY Axis", 0.35, 0.84, 0.18, 0.84),
            Axis("rotz", "RZ Axis", 0.36, 0.88, 0.18, 0.90),
            Button("button1", "Trigger", "Triggers", 0.45, 0.28, 0.08, 0.23, "Primary trigger"),
            Button("button2", "Weapon Release", "Triggers", 0.43, 0.37, 0.06, 0.35),
            Button("button3", "Weapon 1", "Buttons", 0.63, 0.18, 0.80, 0.16),
            Button("button4", "Weapon 2", "Buttons", 0.64, 0.27, 0.81, 0.25),
            Button("button5", "Pinkie Switch", "Buttons", 0.58, 0.55, 0.82, 0.55),
            Button("button6", "Red Button", "Buttons", 0.59, 0.43, 0.82, 0.68),
            Button("button7", "Mic Switch", "Buttons", 0.40, 0.59, 0.06, 0.66),
            Button("button8", "Rapid Fire", "Buttons", 0.62, 0.34, 0.82, 0.34),
            Hat("hat1_up", "Hat Switch Up", 0.52, 0.33, 0.82, 0.39),
            Hat("hat1_right", "Hat Switch Right", 0.56, 0.36, 0.82, 0.43),
            Hat("hat1_down", "Hat Switch Down", 0.52, 0.39, 0.82, 0.47),
            Hat("hat1_left", "Hat Switch Left", 0.48, 0.36, 0.82, 0.51),
            Button("button9", "Trim Hat Up", "Hats", 0.39, 0.31, 0.08, 0.48),
            Button("button10", "Trim Hat Right", "Hats", 0.42, 0.34, 0.08, 0.52),
            Button("button11", "Trim Hat Down", "Hats", 0.39, 0.37, 0.08, 0.56),
            Button("button12", "Trim Hat Left", "Hats", 0.36, 0.34, 0.08, 0.60),
            Button("button13", "Base Button 1", "System Controls", 0.35, 0.88, 0.08, 0.82),
            Button("button14", "Base Button 2", "System Controls", 0.42, 0.88, 0.08, 0.86),
            Button("button15", "Base Button 3", "System Controls", 0.49, 0.88, 0.82, 0.78),
            Button("button16", "Base Button 4", "System Controls", 0.56, 0.88, 0.82, 0.82),
            Button("button17", "Encoder A Press", "Encoders", 0.62, 0.87, 0.82, 0.86),
            Button("button18", "Encoder B Press", "Encoders", 0.67, 0.87, 0.82, 0.90)
        };
        AddGenericButtons(controls, 19, 32);
        return new HardwareTemplate("vkb-gladiator", "VKB Gladiator NXT EVO", HardwareFamily.Joystick,
            ["Gladiator", "VKBsim Gladiator", "VKB Gladiator"], controls, "joystick");
    }

    private static HardwareTemplate BuildVkbStecs()
    {
        var controls = new List<HardwareControlDefinition>
        {
            Axis("z", "Throttle Axis", 0.50, 0.78, 0.78, 0.78, "Main throttle travel"),
            Axis("x", "Mini Stick X", 0.58, 0.32, 0.78, 0.25),
            Axis("y", "Mini Stick Y", 0.58, 0.34, 0.78, 0.31),
            Axis("rotx", "Left Throttle", 0.42, 0.72, 0.08, 0.72),
            Axis("roty", "Right Throttle", 0.55, 0.72, 0.78, 0.70),
            Axis("slider1", "Front Dial", 0.46, 0.45, 0.08, 0.48),
            Button("button1", "Index Trigger", "Triggers", 0.62, 0.25, 0.80, 0.18),
            Button("button2", "Thumb Button", "Buttons", 0.55, 0.22, 0.80, 0.23),
            Button("button3", "Red Button", "Buttons", 0.47, 0.28, 0.08, 0.23),
            Button("button4", "Pinkie Button", "Buttons", 0.39, 0.36, 0.08, 0.32),
            Hat("hat1_up", "Hat Up", 0.55, 0.28, 0.80, 0.36),
            Hat("hat1_right", "Hat Right", 0.58, 0.31, 0.80, 0.41),
            Hat("hat1_down", "Hat Down", 0.55, 0.34, 0.80, 0.46),
            Hat("hat1_left", "Hat Left", 0.52, 0.31, 0.80, 0.51),
            Button("button5", "Encoder 1 Press", "Encoders", 0.38, 0.60, 0.08, 0.58),
            Button("button6", "Encoder 2 Press", "Encoders", 0.46, 0.60, 0.08, 0.63),
            Button("button7", "Encoder 3 Press", "Encoders", 0.55, 0.60, 0.80, 0.58),
            Button("button8", "Encoder 4 Press", "Encoders", 0.63, 0.60, 0.80, 0.63)
        };
        AddGenericButtons(controls, 9, 32);
        return new HardwareTemplate("vkb-stecs", "VKB STECS", HardwareFamily.Throttle,
            ["STECS", "VKBsim STECS", "VKB STECS"], controls, "throttle");
    }

    private static HardwareTemplate BuildLogitechRudder()
    {
        var controls = new List<HardwareControlDefinition>
        {
            Axis("x", "Rudder Axis", 0.50, 0.56, 0.79, 0.54, "Pedal yaw movement"),
            Axis("y", "Left Toe Brake", 0.30, 0.40, 0.08, 0.38),
            Axis("z", "Right Toe Brake", 0.70, 0.40, 0.79, 0.38)
        };
        return new HardwareTemplate("logitech-rudder", "Logitech Pro Rudder Pedals", HardwareFamily.Pedals,
            ["Rudder Pedals", "Logitech Pro Rudder", "Saitek Pro Flight Rudder"], controls, "pedals");
    }

    private static HardwareTemplate BuildThrustmasterT16000()
    {
        var controls = new List<HardwareControlDefinition>
        {
            Axis("x", "X Axis", 0.51, 0.84, 0.75, 0.83),
            Axis("y", "Y Axis", 0.51, 0.80, 0.75, 0.78),
            Axis("z", "Twist Axis", 0.51, 0.90, 0.75, 0.90),
            Axis("slider1", "Throttle Slider", 0.40, 0.90, 0.08, 0.90),
            Button("button1", "Trigger", "Triggers", 0.45, 0.30, 0.08, 0.25),
            Button("button2", "Top Button", "Buttons", 0.53, 0.21, 0.80, 0.20),
            Hat("hat1_up", "Hat Up", 0.52, 0.27, 0.80, 0.30),
            Hat("hat1_right", "Hat Right", 0.55, 0.30, 0.80, 0.35),
            Hat("hat1_down", "Hat Down", 0.52, 0.33, 0.80, 0.40),
            Hat("hat1_left", "Hat Left", 0.49, 0.30, 0.80, 0.45)
        };
        AddGenericButtons(controls, 3, 16);
        return new HardwareTemplate("t16000", "Thrustmaster T.16000M", HardwareFamily.Joystick,
            ["T.16000", "T16000", "Thrustmaster T.16000"], controls, "joystick");
    }

    private static HardwareTemplate BuildVirpilStick()
    {
        var controls = new List<HardwareControlDefinition>
        {
            Axis("x", "X Axis", 0.51, 0.88, 0.76, 0.88),
            Axis("y", "Y Axis", 0.51, 0.84, 0.76, 0.82),
            Axis("z", "Twist Axis", 0.51, 0.92, 0.76, 0.93),
            Button("button1", "Stage 1 Trigger", "Triggers", 0.45, 0.27, 0.08, 0.22),
            Button("button2", "Stage 2 Trigger", "Triggers", 0.45, 0.32, 0.08, 0.28),
            Button("button3", "Weapon Release", "Buttons", 0.60, 0.20, 0.82, 0.17),
            Button("button4", "Red Button", "Buttons", 0.63, 0.28, 0.82, 0.25),
            Hat("hat1_up", "Hat Up", 0.52, 0.32, 0.82, 0.36),
            Hat("hat1_right", "Hat Right", 0.56, 0.35, 0.82, 0.42),
            Hat("hat1_down", "Hat Down", 0.52, 0.38, 0.82, 0.48),
            Hat("hat1_left", "Hat Left", 0.48, 0.35, 0.82, 0.54)
        };
        AddGenericButtons(controls, 5, 32);
        return new HardwareTemplate("virpil-stick", "VIRPIL Flightstick", HardwareFamily.Joystick,
            ["VPC", "VIRPIL", "MongoosT", "Constellation"], controls, "joystick");
    }

    private static HardwareTemplate BuildGenericJoystick()
    {
        var controls = new List<HardwareControlDefinition>
        {
            Axis("x", "X Axis", 0.51, 0.88, 0.76, 0.88),
            Axis("y", "Y Axis", 0.51, 0.84, 0.76, 0.82),
            Axis("z", "Z Axis", 0.51, 0.92, 0.76, 0.93),
            Axis("rotx", "RX Axis", 0.36, 0.84, 0.08, 0.82),
            Axis("roty", "RY Axis", 0.36, 0.88, 0.08, 0.88),
            Axis("rotz", "RZ Axis", 0.36, 0.92, 0.08, 0.94),
            Button("button1", "Trigger", "Triggers", 0.45, 0.28, 0.08, 0.24),
            Button("button2", "Button 2", "Buttons", 0.60, 0.22, 0.82, 0.20),
            Button("button3", "Button 3", "Buttons", 0.62, 0.30, 0.82, 0.28),
            Button("button4", "Button 4", "Buttons", 0.62, 0.38, 0.82, 0.36),
            Hat("hat1_up", "Hat Up", 0.52, 0.32, 0.82, 0.44),
            Hat("hat1_right", "Hat Right", 0.56, 0.35, 0.82, 0.50),
            Hat("hat1_down", "Hat Down", 0.52, 0.38, 0.82, 0.56),
            Hat("hat1_left", "Hat Left", 0.48, 0.35, 0.82, 0.62)
        };
        AddGenericButtons(controls, 5, 32);
        return new HardwareTemplate("generic-joystick", "Generic Joystick", HardwareFamily.Joystick, [], controls, "joystick");
    }

    private static HardwareTemplate BuildKeyboard()
    {
        var controls = new List<HardwareControlDefinition>();
        foreach (var key in new[] { "w", "a", "s", "d", "space", "lshift", "lctrl", "lalt", "q", "e", "r", "f", "c", "v", "x", "z", "tab", "enter", "escape", "1", "2", "3", "4", "5" })
            controls.Add(new HardwareControlDefinition(key, "Key " + StarbindText.Humanize(key), StarbindControlKind.Key, "Keys", 0, 0, 0, 0));
        return new HardwareTemplate("keyboard", "Keyboard", HardwareFamily.Keyboard, [], controls, "keyboard");
    }

    private static HardwareTemplate BuildMouse()
    {
        var controls = new List<HardwareControlDefinition>
        {
            new("x", "Mouse X", StarbindControlKind.Axis, "Axes", 0, 0, 0, 0),
            new("y", "Mouse Y", StarbindControlKind.Axis, "Axes", 0, 0, 0, 0),
            new("wheel", "Mouse Wheel", StarbindControlKind.Axis, "Axes", 0, 0, 0, 0)
        };
        for (var button = 1; button <= 8; button++) controls.Add(new HardwareControlDefinition($"button{button}", $"Mouse Button {button}", StarbindControlKind.MouseButton, "Buttons", 0, 0, 0, 0));
        return new HardwareTemplate("mouse", "Mouse", HardwareFamily.Mouse, [], controls, "mouse");
    }

    private static HardwareTemplate BuildGamepad()
    {
        var controls = new List<HardwareControlDefinition>
        {
            Axis("x", "Left Stick X", 0.35, 0.62, 0.08, 0.60),
            Axis("y", "Left Stick Y", 0.35, 0.62, 0.08, 0.66),
            Axis("rotx", "Right Stick X", 0.64, 0.65, 0.82, 0.62),
            Axis("roty", "Right Stick Y", 0.64, 0.65, 0.82, 0.68),
            Axis("z", "Left Trigger", 0.25, 0.23, 0.08, 0.20),
            Axis("rotz", "Right Trigger", 0.75, 0.23, 0.82, 0.20)
        };
        AddGenericButtons(controls, 1, 16);
        return new HardwareTemplate("gamepad", "Gamepad", HardwareFamily.Gamepad, [], controls, "gamepad");
    }

    private static HardwareControlDefinition Axis(string suffix, string name, double hotspotX, double hotspotY, double labelX, double labelY, string? description = null)
        => new(suffix, name, StarbindControlKind.Axis, "Axes", hotspotX, hotspotY, labelX, labelY, description);

    private static HardwareControlDefinition Button(string suffix, string name, string group, double hotspotX, double hotspotY, double labelX, double labelY, string? description = null)
        => new(suffix, name, StarbindControlKind.Button, group, hotspotX, hotspotY, labelX, labelY, description);

    private static HardwareControlDefinition Hat(string suffix, string name, double hotspotX, double hotspotY, double labelX, double labelY)
        => new(suffix, name, StarbindControlKind.Hat, "Hats", hotspotX, hotspotY, labelX, labelY);

    private static void AddGenericButtons(List<HardwareControlDefinition> controls, int start, int end)
    {
        for (var button = start; button <= end; button++)
        {
            var suffix = $"button{button}";
            if (controls.Any(control => control.InputSuffix.Equals(suffix, StringComparison.OrdinalIgnoreCase))) continue;
            controls.Add(new HardwareControlDefinition(suffix, $"Button {button}", StarbindControlKind.Button, "Buttons", 0, 0, 0, 0));
        }
    }
}
