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
        if (_selectedDevice is null) { SetStatus("Select a physical device first."); return; }
        new StarbindV8DeviceTestWindow(_selectedDevice, _joysticks) { Owner = this }.Show();
    }
}

public sealed class StarbindV8DeviceTestWindow : Window
{
    private readonly StarbindDevice _device;
    private readonly JoystickService _joysticks;
    private readonly DispatcherTimer _timer;
    private readonly ObservableCollection<string> _events = [];
    private readonly Dictionary<string, ProgressBar> _axes = new(StringComparer.OrdinalIgnoreCase);
    private readonly TextBlock _status = new();
    private readonly TextBlock _pressed = new();
    private JoystickSnapshot? _baseline;

    public StarbindV8DeviceTestWindow(StarbindDevice device, JoystickService joysticks)
    {
        _device = device;
        _joysticks = joysticks;
        Title = $"Live Input Test - {device.ProductName}";
        Width = 900;
        Height = 660;
        MinWidth = 740;
        MinHeight = 520;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = StarbindV5Window.Bg;
        Foreground = StarbindV5Window.Text;
        FontFamily = new FontFamily("Segoe UI");
        Content = Build();
        PreviewKeyDown += KeyDownObserved;
        PreviewKeyUp += KeyUpObserved;
        PreviewMouseDown += MouseDownObserved;
        PreviewMouseUp += MouseUpObserved;
        PreviewMouseWheel += MouseWheelObserved;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
        _timer.Tick += Poll;
        Loaded += (_, _) => Start();
        Closed += (_, _) => _timer.Stop();
    }

    private UIElement Build()
    {
        var root = new Grid { Margin = new Thickness(16) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(235) });
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var heading = new StackPanel();
        heading.Children.Add(new TextBlock { Text = "LIVE INPUT TEST", Foreground = StarbindV5Window.Cyan, FontSize = 16, FontWeight = FontWeights.Bold });
        heading.Children.Add(new TextBlock { Text = $"{_device.ProductName}  •  {_device.SlotLabel}", FontSize = 13, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 4, 0, 0) });
        _status.TextWrapping = TextWrapping.Wrap;
        _status.Margin = new Thickness(0, 5, 0, 10);
        heading.Children.Add(_status);
        root.Children.Add(heading);

        var live = new Grid { Margin = new Thickness(0, 4, 0, 10) };
        live.ColumnDefinitions.Add(new ColumnDefinition());
        live.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(310) });
        var axisCard = MakeCard("AXES / MOVEMENT");
        var axisStack = (StackPanel)axisCard.Child;
        foreach (var name in new[] { "X", "Y", "Z", "RX", "U / Slider 1", "V / Slider 2" })
        {
            var row = new Grid { Margin = new Thickness(0, 3, 0, 3) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(95) });
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.Children.Add(new TextBlock { Text = name, Foreground = StarbindV5Window.Muted, VerticalAlignment = VerticalAlignment.Center });
            var bar = new ProgressBar { Minimum = 0, Maximum = 65535, Height = 18, Background = StarbindV5Window.Field, Foreground = StarbindV5Window.Blue };
            _axes[name] = bar;
            Grid.SetColumn(bar, 1);
            row.Children.Add(bar);
            axisStack.Children.Add(row);
        }
        live.Children.Add(axisCard);

        var inputCard = MakeCard("BUTTONS / KEYS");
        var inputStack = (StackPanel)inputCard.Child;
        _pressed.Text = "Nothing pressed";
        _pressed.Foreground = StarbindV5Window.Muted;
        _pressed.TextWrapping = TextWrapping.Wrap;
        _pressed.FontSize = 14;
        inputStack.Children.Add(_pressed);
        inputStack.Children.Add(new TextBlock
        {
            Text = "Keyboard and mouse testing works while this window is focused. Joystick-class devices are polled continuously.",
            Foreground = StarbindV5Window.Faint,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 12, 0, 0)
        });
        Grid.SetColumn(inputCard, 1);
        live.Children.Add(inputCard);
        Grid.SetRow(live, 1);
        root.Children.Add(live);

        var logCard = MakeCard("ACTIVITY LOG");
        var log = new ListBox
        {
            ItemsSource = _events,
            Background = StarbindV5Window.Field,
            Foreground = StarbindV5Window.Text,
            BorderBrush = StarbindV5Window.Border,
            FontFamily = new FontFamily("Consolas"),
            Height = 260
        };
        ((StackPanel)logCard.Child).Children.Add(log);
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

    private static Border MakeCard(string title)
    {
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = title, Foreground = StarbindV5Window.Cyan, FontSize = 10, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 8) });
        return new Border { Background = StarbindV5Window.Panel, BorderBrush = StarbindV5Window.Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6), Padding = new Thickness(12), Margin = new Thickness(0, 0, 10, 0), Child = stack };
    }

    private void Start()
    {
        Focus();
        if (_device.Kind == StarbindDeviceKind.Keyboard)
        {
            SetLiveStatus("Ready. Press and release keyboard keys in this window.");
            return;
        }
        if (_device.Kind == StarbindDeviceKind.Mouse)
        {
            SetLiveStatus("Ready. Click, scroll and move the mouse inside this window. Tester buttons remain usable.");
            return;
        }
        _baseline = _joysticks.GetSnapshot(_device.Instance);
        if (_baseline is null)
        {
            _status.Text = "This profile device is not currently available to the live input API. Reconnect it, verify the device definition, then reopen the tester.";
            _status.Foreground = StarbindV5Window.Amber;
            _pressed.Text = "No live device data";
            return;
        }
        SetLiveStatus("Live. Move axes, hats and buttons to verify the device before assigning controls.");
        _timer.Start();
    }

    private void Poll(object? sender, EventArgs e)
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
        _axes["X"].Value = current.X; _axes["Y"].Value = current.Y; _axes["Z"].Value = current.Z;
        _axes["RX"].Value = current.R; _axes["U / Slider 1"].Value = current.U; _axes["V / Slider 2"].Value = current.V;
        var down = Enumerable.Range(0, 32).Where(bit => (current.Buttons & (1u << bit)) != 0).Select(bit => $"Button {bit + 1}").ToList();
        if (current.Pov != 0xFFFF) down.Add($"Hat {current.Pov / 100d:0} degrees");
        _pressed.Text = down.Count == 0 ? "Nothing pressed" : string.Join("  •  ", down);
        _pressed.Foreground = down.Count == 0 ? StarbindV5Window.Muted : StarbindV5Window.Green;
        var activity = _joysticks.DetectActivity(_device.Instance, _baseline);
        if (activity is not null) AddEvent($"{activity.Description,-24} {activity.Input}");
        _baseline = current;
    }

    private void KeyDownObserved(object sender, KeyEventArgs e)
    {
        if (_device.Kind != StarbindDeviceKind.Keyboard) return;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        _pressed.Text = $"Pressed: {key}";
        _pressed.Foreground = StarbindV5Window.Green;
        AddEvent($"KEY DOWN                 kb1_{KeyName(key)}");
    }

    private void KeyUpObserved(object sender, KeyEventArgs e)
    {
        if (_device.Kind != StarbindDeviceKind.Keyboard) return;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        AddEvent($"KEY UP                   kb1_{KeyName(key)}");
        _pressed.Text = "Nothing pressed";
        _pressed.Foreground = StarbindV5Window.Muted;
    }

    private void MouseDownObserved(object sender, MouseButtonEventArgs e)
    {
        if (_device.Kind != StarbindDeviceKind.Mouse || FindButtonAncestor(e.OriginalSource as DependencyObject) is not null) return;
        var number = e.ChangedButton switch { MouseButton.Left => 1, MouseButton.Right => 2, MouseButton.Middle => 3, MouseButton.XButton1 => 4, MouseButton.XButton2 => 5, _ => 0 };
        if (number == 0) return;
        _pressed.Text = $"Pressed: Mouse Button {number}";
        _pressed.Foreground = StarbindV5Window.Green;
        AddEvent($"MOUSE DOWN               mouse1_button{number}");
    }

    private void MouseUpObserved(object sender, MouseButtonEventArgs e)
    {
        if (_device.Kind != StarbindDeviceKind.Mouse || FindButtonAncestor(e.OriginalSource as DependencyObject) is not null) return;
        _pressed.Text = "Nothing pressed";
        _pressed.Foreground = StarbindV5Window.Muted;
        AddEvent($"MOUSE UP                 {e.ChangedButton}");
    }

    private void MouseWheelObserved(object sender, MouseWheelEventArgs e)
    {
        if (_device.Kind != StarbindDeviceKind.Mouse) return;
        AddEvent($"MOUSE WHEEL              {(e.Delta > 0 ? "up" : "down")}");
        _pressed.Text = e.Delta > 0 ? "Wheel Up" : "Wheel Down";
    }

    private void AddEvent(string text)
    {
        _events.Insert(0, $"{DateTime.Now:HH:mm:ss.fff}  {text}");
        while (_events.Count > 150) _events.RemoveAt(_events.Count - 1);
    }

    private void SetLiveStatus(string text) { _status.Text = text; _status.Foreground = StarbindV5Window.Green; }

    private static Button? FindButtonAncestor(DependencyObject? source)
    {
        var current = source;
        while (current is not null)
        {
            if (current is Button button) return button;
            current = current is Visual ? VisualTreeHelper.GetParent(current) : LogicalTreeHelper.GetParent(current);
        }
        return null;
    }

    private static string KeyName(Key key) => key switch
    {
        Key.Space => "space", Key.LeftCtrl => "lctrl", Key.RightCtrl => "rctrl", Key.LeftShift => "lshift", Key.RightShift => "rshift",
        Key.LeftAlt => "lalt", Key.RightAlt => "ralt", Key.Return => "enter", Key.Back => "backspace", _ => key.ToString().ToLowerInvariant()
    };
}
