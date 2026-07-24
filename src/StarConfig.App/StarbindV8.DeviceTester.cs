using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace StarConfig;

public sealed partial class StarbindV5Window
{
    private void OpenCorrectedDeviceTester(object sender, RoutedEventArgs e)
    {
        if (_selectedDevice is null)
        {
            SetStatus("Select a physical device first.");
            return;
        }
        new StarbindV8DeviceTestWindow(_selectedDevice, _joysticks, _hardware, _profile, _settings) { Owner = this }.Show();
    }
}

public sealed class StarbindV8DeviceTestWindow : Window
{
    private readonly StarbindDevice _device;
    private readonly JoystickService _joysticks;
    private readonly HardwareDefinitionService _hardware;
    private readonly StarbindProfile? _profile;
    private readonly StarbindV5Settings _settings;
    private readonly DispatcherTimer _timer;
    private readonly ObservableCollection<string> _events = [];
    private readonly Dictionary<string, ProgressBar> _axisBars = new(StringComparer.OrdinalIgnoreCase);
    private readonly TextBlock _status = new();
    private readonly TextBlock _pressed = new();
    private readonly ListBox _log = new();
    private JoystickSnapshot? _baseline;

    public StarbindV8DeviceTestWindow(StarbindDevice device, JoystickService joysticks, HardwareDefinitionService hardware, StarbindProfile? profile, StarbindV5Settings settings)
    {
        _device = device;
        _joysticks = joysticks;
        _hardware = hardware;
        _profile = profile;
        _settings = settings;
        Title = $"Live Input Test - {device.ProductName}";
        Width = 920;
        Height = 680;
        MinWidth = 760;
        MinHeight = 540;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = StarbindV5Window.Bg;
        Foreground = StarbindV5Window.Text;
        FontFamily = new FontFamily("Segoe UI");
        Content = Build();

        PreviewKeyDown += KeyPressed;
        PreviewKeyUp += KeyReleased;
        PreviewMouseDown += MousePressed;
        PreviewMouseUp += MouseReleased;
        PreviewMouseWheel += MouseWheelMoved;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
        _timer.Tick += PollDevice;
        Loaded += (_, _) => Start();
        Closed += (_, _) => _timer.Stop();
    }

    private UIElement Build()
    {
        var root = new Grid { Margin = new Thickness(16) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(250) });
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var heading = new StackPanel();
        heading.Children.Add(new TextBlock { Text = "LIVE INPUT TEST", Foreground = StarbindV5Window.Cyan, FontSize = 16, FontWeight = FontWeights.Bold });
        heading.Children.Add(new TextBlock { Text = $"{_device.ProductName}  •  {_device.SlotLabel}", FontSize = 13, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 4, 0, 0) });
        _status.Foreground = _device.IsConnected ? StarbindV5Window.Green : StarbindV5Window.Amber;
        _status.TextWrapping = TextWrapping.Wrap;
        _status.Margin = new Thickness(0, 5, 0, 10);
        heading.Children.Add(_status);
        root.Children.Add(heading);

        var live = new Grid { Margin = new Thickness(0, 4, 0, 10) };
        live.ColumnDefinitions.Add(new ColumnDefinition());
        live.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
        var axesCard = Card("AXES / MOVEMENT");
        var axes = (StackPanel)axesCard.Child;
        foreach (var axis in new[] { "X", "Y", "Z", "RX", "U / Slider 1", "V / Slider 2" })
        {
            var row = new Grid { Margin = new Thickness(0, 3, 0, 3) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.Children.Add(new TextBlock { Text = axis, Foreground = StarbindV5Window.Muted, VerticalAlignment = VerticalAlignment.Center });
            var bar = new ProgressBar { Minimum = 0, Maximum = 65535, Height = 18, Background = StarbindV5Window.Field, Foreground = StarbindV5Window.Blue };
            _axisBars[axis] = bar;
            Grid.SetColumn(bar, 1);
            row.Children.Add(bar);
            axes.Children.Add(row);
        }
        live.Children.Add(axesCard);

        var buttonCard = Card("BUTTONS / KEYS");
        var buttons = (StackPanel)buttonCard.Child;
        _pressed.Text = "Nothing pressed";
        _pressed.Foreground = StarbindV5Window.Muted;
        _pressed.TextWrapping = TextWrapping.Wrap;
        _pressed.FontSize = 14;
        buttons.Children.Add(_pressed);
        buttons.Children.Add(new TextBlock
        {
            Text = "Keyboard and mouse testing works while this window is focused. Joystick-class devices are polled continuously.",
            Foreground = StarbindV5Window.Faint,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 12, 0, 0)
        });
        Grid.SetColumn(buttonCard, 1);
        live.Children.Add(buttonCard);
        Grid.SetRow(live, 1);
        root.Children.Add(live);

        var logCard = Card("ACTIVITY LOG");
        var logHost = (StackPanel)logCard.Child;
        _log.ItemsSource = _events;
        _log.Background = StarbindV5Window.Field;
        _log.Foreground = StarbindV5Window.Text;
        _log.BorderBrush = StarbindV5Window.Border;
        _log.FontFamily = new FontFamily("Consolas");
        _log.Height = 270;
        logHost.Children.Add(_log);
        Grid.SetRow(logCard, 2);
        root.Children.Add(logCard);

        var footer = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
        var clear = StarbindV5Window.DialogButton("CLEAR LOG", StarbindV5Window.Panel2);
        clear.Click += (_, _) => _events.Clear();
        footer.Children.Add(clear);
        var close = StarbindV5Window.DialogButton("CLOSE", StarbindV5Window.Panel2);
        close.Click += (_, _) => Close();
        footer.Children.Add(close);
        Grid.SetRow(footer, 3);
        root.Children.Add(footer);
        return root;
    }

    private static Border Card(string title)
    {
        var card = new Border { Background = StarbindV5Window.Panel, BorderBrush = StarbindV5Window.Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6), Padding = new Thickness(12), Margin = new Thickness(0, 0, 10, 0) };
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = title, Foreground = StarbindV5Window.Cyan, FontSize = 10, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 8) });
        card.Child = stack;
        return card;
    }

    private void Start()
    {
        Focus();
        if (_device.Kind is StarbindDeviceKind.Keyboard)
        {
            _status.Text = "Ready. Press and release keyboard keys in this window.";
            _status.Foreground = StarbindV5Window.Green;
            return;
        }
        if (_device.Kind is StarbindDeviceKind.Mouse)
        {
            _status.Text = "Ready. Click, scroll and move the mouse inside this window.";
            _status.Foreground = StarbindV5Window.Green;
            return;
        }

        _baseline = _joysticks.GetSnapshot(_device.Instance);
        if (_baseline is null)
        {
            _status.Text = "This device exists in the loaded profile but is not available to the live input API. Reconnect it, verify its device definition, then reopen the tester.";
            _status.Foreground = StarbindV5Window.Amber;
            _pressed.Text = "No live device data";
            return;
        }
        _status.Text = "Live. Move axes, hats and buttons to verify the device before assigning controls.";
        _status.Foreground = StarbindV5Window.Green;
        _timer.Start();
    }

    private void PollDevice(object? sender, EventArgs e)
    {
        if (_baseline is null) return;
        var current = _joysticks.GetSnapshot(_device.Instance);
        if (current is null)
        {
            _status.Text = "The device stopped responding. Check its connection.";
            _status.Foreground = StarbindV5Window.Red;
            _timer.Stop();
            return;
        }

        _axisBars["X"].Value = current.X;
        _axisBars["Y"].Value = current.Y;
        _axisBars["Z"].Value = current.Z;
        _axisBars["RX"].Value = current.R;
        _axisBars["U / Slider 1"].Value = current.U;
        _axisBars["V / Slider 2"].Value = current.V;

        var pressed = Enumerable.Range(0, 32).Where(bit => (current.Buttons & (1u << bit)) != 0).Select(bit => $"Button {bit + 1}").ToList();
        if (current.Pov != 0xFFFF) pressed.Add($"Hat {current.Pov / 100d:0} degrees");
        _pressed.Text = pressed.Count == 0 ? "Nothing pressed" : string.Join("  •  ", pressed);
        _pressed.Foreground = pressed.Count == 0 ? StarbindV5Window.Muted : StarbindV5Window.Green;

        var activity = _joysticks.DetectActivity(_device.Instance, _baseline);
        if (activity is not null) AddEvent($"{activity.Description,-24} {activity.Input}");
        _baseline = current;
    }

    private void KeyPressed(object sender, KeyEventArgs e)
    {
        if (_device.Kind != StarbindDeviceKind.Keyboard) return;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        _pressed.Text = $"Pressed: {key}";
        _pressed.Foreground = StarbindV5Window.Green;
        AddEvent($"KEY DOWN                 kb1_{KeyName(key)}");
        e.Handled = true;
    }

    private void KeyReleased(object sender, KeyEventArgs e)
    {
        if (_device.Kind != StarbindDeviceKind.Keyboard) return;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        AddEvent($"KEY UP                   kb1_{KeyName(key)}");
        _pressed.Text = "Nothing pressed";
        _pressed.Foreground = StarbindV5Window.Muted;
        e.Handled = true;
    }

    private void MousePressed(object sender, MouseButtonEventArgs e)
    {
        if (_device.Kind != StarbindDeviceKind.Mouse) return;
        var number = e.ChangedButton switch { MouseButton.Left => 1, MouseButton.Right => 2, MouseButton.Middle => 3, MouseButton.XButton1 => 4, MouseButton.XButton2 => 5, _ => 0 };
        if (number == 0) return;
        _pressed.Text = $"Pressed: Mouse Button {number}";
        _pressed.Foreground = StarbindV5Window.Green;
        AddEvent($"MOUSE DOWN               mouse1_button{number}");
        e.Handled = true;
    }

    private void MouseReleased(object sender, MouseButtonEventArgs e)
    {
        if (_device.Kind != StarbindDeviceKind.Mouse) return;
        _pressed.Text = "Nothing pressed";
        _pressed.Foreground = StarbindV5Window.Muted;
        AddEvent($"MOUSE UP                 {e.ChangedButton}");
        e.Handled = true;
    }

    private void MouseWheelMoved(object sender, MouseWheelEventArgs e)
    {
        if (_device.Kind != StarbindDeviceKind.Mouse) return;
        AddEvent($"MOUSE WHEEL              {(e.Delta > 0 ? "up" : "down")}");
        _pressed.Text = e.Delta > 0 ? "Wheel Up" : "Wheel Down";
        e.Handled = true;
    }

    private void AddEvent(string text)
    {
        _events.Insert(0, $"{DateTime.Now:HH:mm:ss.fff}  {text}");
        while (_events.Count > 150) _events.RemoveAt(_events.Count - 1);
    }

    private static string KeyName(Key key) => key switch
    {
        Key.Space => "space", Key.LeftCtrl => "lctrl", Key.RightCtrl => "rctrl",
        Key.LeftShift => "lshift", Key.RightShift => "rshift", Key.LeftAlt => "lalt",
        Key.RightAlt => "ralt", Key.Return => "enter", Key.Back => "backspace",
        _ => key.ToString().ToLowerInvariant()
    };
}
