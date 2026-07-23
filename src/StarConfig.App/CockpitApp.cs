using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace StarConfig;

public static class CockpitProgram
{
    [STAThread]
    public static void Main()
    {
        var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
        app.Run(new CockpitWindow());
    }
}

public sealed class CockpitWindow : Window
{
    private static readonly Brush Bg = Brush("#070C13");
    private static readonly Brush Panel = Brush("#0E1723");
    private static readonly Brush Panel2 = Brush("#111F2D");
    private static readonly Brush Line = Brush("#29445E");
    private static readonly Brush Cyan = Brush("#23C9FF");
    private static readonly Brush Blue = Brush("#2F7DFF");
    private static readonly Brush Green = Brush("#38E27D");
    private static readonly Brush Amber = Brush("#FFB84A");
    private static readonly Brush Text = Brush("#F4F8FF");
    private static readonly Brush Muted = Brush("#8FA7BE");

    private readonly ProfileService _profiles = new();
    private readonly JoystickService _joysticks = new();
    private readonly ObservableCollection<BindingEntry> _all = [];
    private readonly ObservableCollection<BindingEntry> _visible = [];
    private readonly HashSet<string> _folders = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, JoystickSnapshot> _baselines = [];
    private readonly DispatcherTimer _captureTimer;

    private readonly StackPanel _deviceCards = new();
    private readonly ComboBox _profilePicker = new();
    private readonly Canvas _deviceCanvas = new();
    private readonly UniformGrid _buttonGrid = new() { Columns = 6 };
    private readonly StackPanel _axisStrip = new() { Orientation = Orientation.Horizontal };
    private readonly ListBox _actions = new();
    private readonly TextBox _search = new();
    private readonly TextBlock _actionName = new();
    private readonly TextBlock _actionMeta = new();
    private readonly TextBlock _actionDescription = new();
    private readonly TextBox _input = new();
    private readonly StackPanel _stateChecks = new();
    private readonly TextBlock _status = new();
    private readonly TextBlock _deviceTitle = new();
    private readonly TextBlock _controlTitle = new();
    private readonly TextBlock _conflictText = new();
    private readonly Button _listen = new();
    private readonly Button _save = new();
    private readonly Button _applyStates = new();

    private InputDevice? _selectedDevice;
    private BindingEntry? _selectedBinding;
    private string? _activeProfile;
    private bool _capturing;

    public CockpitWindow()
    {
        Title = "StarConfig - Visual Star Citizen Control Mapper";
        Width = 1600;
        Height = 960;
        MinWidth = 1240;
        MinHeight = 760;
        Background = Bg;
        Foreground = Text;
        FontFamily = new FontFamily("Segoe UI");
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Content = BuildUi();

        PreviewKeyDown += CaptureKeyboard;
        PreviewMouseDown += CaptureMouse;
        _captureTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(35) };
        _captureTimer.Tick += CaptureTick;
        Loaded += (_, _) => RefreshEverything();
    }

    private UIElement BuildUi()
    {
        var root = new Grid { Margin = new Thickness(14) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(260) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = Card(12);
        Grid.SetRow(header, 0);
        var hg = new Grid();
        hg.ColumnDefinitions.Add(new ColumnDefinition());
        hg.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.Child = hg;
        var brand = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        brand.Children.Add(new TextBlock { Text = "STARCONFIG", FontSize = 27, FontWeight = FontWeights.Bold });
        brand.Children.Add(new TextBlock { Text = "  CONTROL COCKPIT", FontSize = 13, Foreground = Cyan, VerticalAlignment = VerticalAlignment.Center });
        hg.Children.Add(brand);
        var topActions = new StackPanel { Orientation = Orientation.Horizontal };
        Grid.SetColumn(topActions, 1);
        topActions.Children.Add(ActionButton("ADD PROFILE FOLDER", ChooseFolder));
        topActions.Children.Add(ActionButton("REFRESH", (_, _) => RefreshEverything()));
        topActions.Children.Add(ActionButton("OPEN RSI LAUNCHER", LaunchRsi, Green));
        hg.Children.Add(topActions);
        root.Children.Add(header);

        var main = new Grid { Margin = new Thickness(0, 12, 0, 12) };
        main.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
        main.ColumnDefinitions.Add(new ColumnDefinition());
        main.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(410) });
        Grid.SetRow(main, 1);
        root.Children.Add(main);

        var left = Card(12);
        Grid.SetColumn(left, 0);
        var leftStack = new StackPanel();
        left.Child = leftStack;
        leftStack.Children.Add(Label("CONNECTED CONTROLS"));
        _deviceCards.Margin = new Thickness(0, 8, 0, 16);
        leftStack.Children.Add(_deviceCards);
        leftStack.Children.Add(Label("ACTIVE PROFILE"));
        _profilePicker.Margin = new Thickness(0, 8, 0, 12);
        _profilePicker.SelectionChanged += (_, _) => ProfileChanged();
        leftStack.Children.Add(_profilePicker);
        var hint = new TextBlock
        {
            Text = "Select a physical device, then click a pictured control or press LISTEN. StarConfig maps the physical intent first and the game actions second.",
            Foreground = Muted,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 12, 0, 0)
        };
        leftStack.Children.Add(hint);
        main.Children.Add(left);

        var cockpit = Card(0);
        cockpit.Margin = new Thickness(12, 0, 12, 0);
        Grid.SetColumn(cockpit, 1);
        var cockpitGrid = new Grid();
        cockpitGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        cockpitGrid.RowDefinitions.Add(new RowDefinition());
        cockpitGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        cockpit.Child = cockpitGrid;

        var cockpitHeader = new Grid { Margin = new Thickness(14, 12, 14, 8) };
        cockpitHeader.ColumnDefinitions.Add(new ColumnDefinition());
        cockpitHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _deviceTitle.Text = "NO DEVICE SELECTED";
        _deviceTitle.FontSize = 18;
        _deviceTitle.FontWeight = FontWeights.SemiBold;
        cockpitHeader.Children.Add(_deviceTitle);
        _controlTitle.Text = "Select a control";
        _controlTitle.Foreground = Cyan;
        Grid.SetColumn(_controlTitle, 1);
        cockpitHeader.Children.Add(_controlTitle);
        cockpitGrid.Children.Add(cockpitHeader);

        var visual = new Grid { Background = Brush("#09121D") };
        Grid.SetRow(visual, 1);
        visual.Children.Add(_deviceCanvas);
        cockpitGrid.Children.Add(visual);

        var physical = new StackPanel { Margin = new Thickness(14, 10, 14, 14) };
        Grid.SetRow(physical, 2);
        physical.Children.Add(Label("PHYSICAL CONTROLS"));
        _axisStrip.Margin = new Thickness(0, 8, 0, 8);
        physical.Children.Add(_axisStrip);
        physical.Children.Add(_buttonGrid);
        cockpitGrid.Children.Add(physical);
        main.Children.Add(cockpit);

        var right = Card(12);
        Grid.SetColumn(right, 2);
        var editor = new StackPanel();
        right.Child = new ScrollViewer { Content = editor, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        editor.Children.Add(Label("SELECTED ACTION"));
        _actionName.Text = "Choose an action below";
        _actionName.FontSize = 21;
        _actionName.FontWeight = FontWeights.SemiBold;
        _actionName.TextWrapping = TextWrapping.Wrap;
        _actionName.Margin = new Thickness(0, 8, 0, 3);
        editor.Children.Add(_actionName);
        _actionMeta.Foreground = Cyan;
        _actionMeta.TextWrapping = TextWrapping.Wrap;
        editor.Children.Add(_actionMeta);
        _actionDescription.Foreground = Muted;
        _actionDescription.TextWrapping = TextWrapping.Wrap;
        _actionDescription.Margin = new Thickness(0, 14, 0, 14);
        editor.Children.Add(_actionDescription);

        editor.Children.Add(Label("ASSIGNED PHYSICAL INPUT"));
        StyleInput(_input);
        _input.Margin = new Thickness(0, 8, 0, 8);
        editor.Children.Add(_input);
        var bindButtons = new Grid();
        bindButtons.ColumnDefinitions.Add(new ColumnDefinition());
        bindButtons.ColumnDefinitions.Add(new ColumnDefinition());
        Configure(_listen, "LISTEN", Listen);
        Configure(_save, "SAVE BINDING", (_, _) => SaveCurrent(), Green);
        Grid.SetColumn(_save, 1);
        bindButtons.Children.Add(_listen);
        bindButtons.Children.Add(_save);
        editor.Children.Add(bindButtons);

        editor.Children.Add(SeparatorLine());
        editor.Children.Add(Label("APPLY TO GAME STATES"));
        editor.Children.Add(new TextBlock { Text = "Compatible actions stay separate. Check only the contexts that should share this physical control.", Foreground = Muted, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 7, 0, 7) });
        editor.Children.Add(_stateChecks);
        Configure(_applyStates, "APPLY TO CHECKED STATES", ApplyStates, Blue);
        _applyStates.Margin = new Thickness(0, 8, 0, 0);
        editor.Children.Add(_applyStates);

        editor.Children.Add(SeparatorLine());
        editor.Children.Add(Label("CONFLICT WATCH"));
        _conflictText.Text = "No control selected.";
        _conflictText.Foreground = Muted;
        _conflictText.TextWrapping = TextWrapping.Wrap;
        _conflictText.Margin = new Thickness(0, 7, 0, 0);
        editor.Children.Add(_conflictText);
        main.Children.Add(right);

        var browser = Card(12);
        Grid.SetRow(browser, 2);
        var browserGrid = new Grid();
        browserGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        browserGrid.RowDefinitions.Add(new RowDefinition());
        browser.Child = browserGrid;
        var browserHeader = new Grid();
        browserHeader.ColumnDefinitions.Add(new ColumnDefinition());
        browserHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(420) });
        var browserTitle = new StackPanel { Orientation = Orientation.Horizontal };
        browserTitle.Children.Add(Label("ACTION BROWSER"));
        browserTitle.Children.Add(new TextBlock { Text = "  Raw action names are searchable, but no longer dominate the cockpit.", Foreground = Muted, VerticalAlignment = VerticalAlignment.Center });
        browserHeader.Children.Add(browserTitle);
        StyleInput(_search);
        _search.TextChanged += (_, _) => FilterActions();
        _search.ToolTip = "Search action, context, intent, behavior, or input";
        Grid.SetColumn(_search, 1);
        browserHeader.Children.Add(_search);
        browserGrid.Children.Add(browserHeader);
        _actions.Background = Bg;
        _actions.Foreground = Text;
        _actions.BorderBrush = Line;
        _actions.DisplayMemberPath = nameof(BindingEntry.Display);
        _actions.SelectionChanged += (_, _) => SelectAction();
        _actions.Margin = new Thickness(0, 10, 0, 0);
        Grid.SetRow(_actions, 1);
        browserGrid.Children.Add(_actions);
        root.Children.Add(browser);

        var footer = Card(9);
        Grid.SetRow(footer, 3);
        _status.Text = "Ready.";
        _status.Foreground = Muted;
        footer.Child = _status;
        root.Children.Add(footer);
        return root;
    }

    private void RefreshEverything()
    {
        foreach (var folder in _profiles.FindMappingsFolders()) _folders.Add(folder);
        BuildDeviceCards();
        LoadProfiles();
    }

    private void BuildDeviceCards()
    {
        _deviceCards.Children.Clear();
        var devices = _joysticks.GetConnectedDevices();
        foreach (var device in devices)
        {
            var button = ActionButton($"JOY{device.Id + 1}  {device.Name}\n{device.Buttons} buttons  •  {device.Axes} axes", (_, _) => SelectDevice(device));
            button.HorizontalContentAlignment = HorizontalAlignment.Left;
            button.Padding = new Thickness(10);
            _deviceCards.Children.Add(button);
        }
        if (devices.Count > 0) SelectDevice(devices[0]);
        else _deviceCards.Children.Add(new TextBlock { Text = "Keyboard and mouse available. No joystick-class device detected.", Foreground = Muted, TextWrapping = TextWrapping.Wrap });
    }

    private void SelectDevice(InputDevice device)
    {
        _selectedDevice = device;
        _deviceTitle.Text = $"JOY{device.Id + 1}  {device.Name}";
        DrawDevice(device);
        BuildPhysicalControls(device);
        _status.Text = $"Selected {device.Name}. Click a pictured control or listen for live input.";
    }

    private void DrawDevice(InputDevice device)
    {
        _deviceCanvas.Children.Clear();
        var shell = new Border { Width = 360, Height = 320, CornerRadius = new CornerRadius(80, 80, 35, 35), Background = Panel2, BorderBrush = Line, BorderThickness = new Thickness(2) };
        Canvas.SetLeft(shell, 150); Canvas.SetTop(shell, 55); _deviceCanvas.Children.Add(shell);
        var stick = new Ellipse { Width = 112, Height = 112, Fill = Brush("#182B3D"), Stroke = Cyan, StrokeThickness = 3 };
        Canvas.SetLeft(stick, 274); Canvas.SetTop(stick, 104); _deviceCanvas.Children.Add(stick);
        var hub = new Ellipse { Width = 34, Height = 34, Fill = Cyan, Opacity = .65 };
        Canvas.SetLeft(hub, 313); Canvas.SetTop(hub, 143); _deviceCanvas.Children.Add(hub);
        for (var i = 1; i <= Math.Min(12, device.Buttons); i++)
        {
            var b = new Button { Content = i.ToString(), Width = 34, Height = 34, Background = Brush("#15283A"), Foreground = Text, BorderBrush = i <= 4 ? Amber : Line, BorderThickness = new Thickness(1), FontWeight = FontWeights.Bold, Tag = i };
            b.Click += (_, _) => ChoosePhysical($"js{device.Id + 1}_button{i}", $"Button {i}");
            var angle = (i - 1) * (Math.PI * 2 / Math.Min(12, device.Buttons));
            Canvas.SetLeft(b, 330 + Math.Cos(angle) * 145 - 17);
            Canvas.SetTop(b, 215 + Math.Sin(angle) * 115 - 17);
            _deviceCanvas.Children.Add(b);
        }
        var caption = new TextBlock { Text = "CLICK A CONTROL TO ASSIGN IT", Foreground = Muted, FontSize = 12, FontWeight = FontWeights.SemiBold };
        Canvas.SetLeft(caption, 232); Canvas.SetTop(caption, 395); _deviceCanvas.Children.Add(caption);
    }

    private void BuildPhysicalControls(InputDevice device)
    {
        _buttonGrid.Children.Clear();
        _axisStrip.Children.Clear();
        foreach (var axis in new[] { "x", "y", "z", "rotz", "rotx", "roty", "slider1", "slider2" }.Take(Math.Max(2, Math.Min(8, device.Axes))))
        {
            var name = axis;
            var b = ActionButton(name.ToUpperInvariant(), (_, _) => ChoosePhysical($"js{device.Id + 1}_{name}", $"Axis {name.ToUpperInvariant()}"), Brush("#15344A"));
            b.Padding = new Thickness(11, 5, 11, 5);
            _axisStrip.Children.Add(b);
        }
        for (var i = 1; i <= device.Buttons; i++)
        {
            var n = i;
            var b = ActionButton($"B{n}", (_, _) => ChoosePhysical($"js{device.Id + 1}_button{n}", $"Button {n}"));
            b.Padding = new Thickness(7, 5, 7, 5);
            _buttonGrid.Children.Add(b);
        }
    }

    private void ChoosePhysical(string input, string description)
    {
        _input.Text = input;
        _controlTitle.Text = description;
        UpdateConflicts(input);
        _status.Text = $"Selected {description}: {input}. Choose an action and save.";
    }

    private void LoadProfiles()
    {
        var files = _profiles.GetProfiles(_folders).Select(x => new FileInfo(x)).ToList();
        _profilePicker.ItemsSource = files;
        _profilePicker.DisplayMemberPath = nameof(FileInfo.Name);
        if (files.Count > 0) _profilePicker.SelectedIndex = 0;
        else _status.Text = "No exported Star Citizen profiles found. Add a Mappings folder.";
    }

    private void ProfileChanged()
    {
        if (_profilePicker.SelectedItem is not FileInfo file) return;
        try
        {
            _activeProfile = file.FullName;
            _all.Clear();
            foreach (var b in _profiles.LoadBindings(file.FullName)) _all.Add(b);
            FilterActions();
            _status.Text = $"Loaded {file.Name} with {_all.Count:N0} actions.";
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Profile load failed", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void FilterActions()
    {
        var term = _search.Text.Trim();
        _visible.Clear();
        foreach (var b in _all.Where(b => string.IsNullOrWhiteSpace(term) || b.ActionName.Contains(term, StringComparison.OrdinalIgnoreCase) || b.Context.Contains(term, StringComparison.OrdinalIgnoreCase) || b.Intent.Contains(term, StringComparison.OrdinalIgnoreCase) || b.Input.Contains(term, StringComparison.OrdinalIgnoreCase))) _visible.Add(b);
        _actions.ItemsSource = _visible;
    }

    private void SelectAction()
    {
        if (_actions.SelectedItem is not BindingEntry b) return;
        _selectedBinding = b;
        _actionName.Text = Humanize(b.ActionName);
        _actionMeta.Text = $"{b.Context}  •  {b.Behavior}  •  {b.ActionMap}";
        _actionDescription.Text = b.Description;
        _input.Text = b.Input;
        PopulateStates(b);
        UpdateConflicts(b.Input);
        _save.IsEnabled = true;
        _listen.IsEnabled = true;
    }

    private void PopulateStates(BindingEntry selected)
    {
        _stateChecks.Children.Clear();
        var peers = string.IsNullOrWhiteSpace(selected.Intent)
            ? new List<BindingEntry> { selected }
            : _all.Where(x => x.Intent.Equals(selected.Intent, StringComparison.OrdinalIgnoreCase) && (x.Behavior == selected.Behavior || selected.Behavior == "Axis")).OrderBy(x => x.Context).ToList();
        foreach (var peer in peers)
        {
            peer.IsIntentSelected = peer.Identity == selected.Identity || peer.Input.Equals(selected.Input, StringComparison.OrdinalIgnoreCase);
            var cb = new CheckBox { Content = $"{peer.Context}: {Humanize(peer.ActionName)}", IsChecked = peer.IsIntentSelected, Foreground = Text, Margin = new Thickness(2, 4, 2, 4), Tag = peer };
            cb.Checked += (_, _) => peer.IsIntentSelected = true;
            cb.Unchecked += (_, _) => peer.IsIntentSelected = false;
            _stateChecks.Children.Add(cb);
        }
        _applyStates.IsEnabled = peers.Count > 1;
    }

    private void UpdateConflicts(string input)
    {
        if (string.IsNullOrWhiteSpace(input) || input.Equals("Unbound", StringComparison.OrdinalIgnoreCase)) { _conflictText.Text = "No assigned input to inspect."; return; }
        var clashes = _all.Where(x => x.Input.Equals(input, StringComparison.OrdinalIgnoreCase) && x.Identity != _selectedBinding?.Identity).Take(8).ToList();
        _conflictText.Foreground = clashes.Count == 0 ? Green : Amber;
        _conflictText.Text = clashes.Count == 0 ? "No duplicate bindings detected in this profile." : $"{clashes.Count} existing assignment(s):\n" + string.Join("\n", clashes.Select(x => $"• {x.Context}: {Humanize(x.ActionName)}"));
    }

    private void SaveCurrent()
    {
        if (_selectedBinding is null || _activeProfile is null) return;
        try
        {
            var backup = _profiles.SaveBindings(_activeProfile, [(_selectedBinding, Normalize(_input.Text))]);
            ProfileChanged();
            _status.Text = $"Binding saved. Backup created at {backup}";
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Save failed", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void ApplyStates(object sender, RoutedEventArgs e)
    {
        if (_activeProfile is null) return;
        var selected = _stateChecks.Children.OfType<CheckBox>().Where(x => x.IsChecked == true).Select(x => (BindingEntry)x.Tag).ToList();
        if (selected.Count == 0) return;
        try
        {
            var input = Normalize(_input.Text);
            var backup = _profiles.SaveBindings(_activeProfile, selected.Select(x => (x, input)));
            ProfileChanged();
            _status.Text = $"Applied {input} to {selected.Count} game states. Backup: {backup}";
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Cross-state save failed", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void Listen(object sender, RoutedEventArgs e)
    {
        if (_capturing) { StopCapture(); return; }
        _baselines.Clear();
        foreach (var d in _joysticks.GetConnectedDevices()) { var s = _joysticks.GetSnapshot(d.Id); if (s is not null) _baselines[d.Id] = s; }
        _capturing = true;
        _listen.Content = "CANCEL LISTENING";
        _status.Text = "Listening. Press a key, mouse button, joystick button, hat, or move an axis. Escape cancels.";
        _captureTimer.Start();
        Focus();
    }

    private void CaptureTick(object? sender, EventArgs e)
    {
        if (!_capturing) return;
        foreach (var b in _baselines) { var activity = _joysticks.DetectActivity(b.Key, b.Value); if (activity is not null) { AcceptCaptured(activity.Input, activity.Description); return; } }
    }

    private void CaptureKeyboard(object sender, KeyEventArgs e)
    {
        if (!_capturing) return;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape) { StopCapture(); e.Handled = true; return; }
        AcceptCaptured($"kb1_{key.ToString().ToLowerInvariant()}", $"Keyboard {key}");
        e.Handled = true;
    }

    private void CaptureMouse(object sender, MouseButtonEventArgs e)
    {
        if (!_capturing) return;
        var n = e.ChangedButton switch { MouseButton.Left => 1, MouseButton.Right => 2, MouseButton.Middle => 3, MouseButton.XButton1 => 4, MouseButton.XButton2 => 5, _ => 0 };
        if (n > 0) AcceptCaptured($"mouse1_button{n}", $"Mouse Button {n}");
        e.Handled = true;
    }

    private void AcceptCaptured(string input, string description)
    {
        _input.Text = input;
        _controlTitle.Text = description;
        StopCapture();
        UpdateConflicts(input);
        _status.Text = $"Captured {description}: {input}";
    }

    private void StopCapture()
    {
        _captureTimer.Stop();
        _baselines.Clear();
        _capturing = false;
        _listen.Content = "LISTEN";
    }

    private void ChooseFolder(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Choose Star Citizen Controls\\Mappings folder" };
        if (dialog.ShowDialog() != true) return;
        _folders.Add(dialog.FolderName);
        LoadProfiles();
    }

    private void LaunchRsi(object sender, RoutedEventArgs e)
    {
        var candidates = new[] { Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Roberts Space Industries", "RSI Launcher", "RSI Launcher.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "RSI Launcher", "RSI Launcher.exe") };
        var launcher = candidates.FirstOrDefault(File.Exists);
        if (launcher is null) { MessageBox.Show("RSI Launcher was not found.", "StarConfig", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        Process.Start(new ProcessStartInfo(launcher) { UseShellExecute = true });
    }

    private static Border Card(double padding) => new() { Background = Panel, BorderBrush = Line, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(padding) };
    private static TextBlock Label(string text) => new() { Text = text, FontSize = 12, FontWeight = FontWeights.Bold, Foreground = Cyan };
    private static Separator SeparatorLine() => new() { Margin = new Thickness(0, 16, 0, 16), Background = Line };
    private static Button ActionButton(string text, RoutedEventHandler click, Brush? background = null) { var b = new Button { Content = text, Background = background ?? Panel2, Foreground = Text, BorderBrush = Line, Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(3), Cursor = Cursors.Hand }; b.Click += click; return b; }
    private static void Configure(Button b, string text, RoutedEventHandler click, Brush? background = null) { b.Content = text; b.Background = background ?? Panel2; b.Foreground = Text; b.BorderBrush = Line; b.Padding = new Thickness(10, 8, 10, 8); b.Margin = new Thickness(3); b.Click += click; b.IsEnabled = false; }
    private static void StyleInput(TextBox box) { box.Background = Bg; box.Foreground = Text; box.BorderBrush = Line; box.Padding = new Thickness(9); }
    private static Brush Brush(string hex) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
    private static string Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? "Unbound" : value.Trim();
    private static string Humanize(string value) => value.Replace("v_", "").Replace("_", " ").Trim();
}
