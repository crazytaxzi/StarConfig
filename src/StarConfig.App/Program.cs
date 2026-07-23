using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;

namespace StarConfig;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
        app.Run(new MainWindow());
    }
}

public sealed class MainWindow : Window
{
    private static readonly Brush BackgroundBrush = new SolidColorBrush(Color.FromRgb(8, 15, 23));
    private static readonly Brush PanelBrush = new SolidColorBrush(Color.FromRgb(17, 25, 35));
    private static readonly Brush CardBrush = new SolidColorBrush(Color.FromRgb(23, 34, 49));
    private static readonly Brush BorderBrush = new SolidColorBrush(Color.FromRgb(49, 70, 93));
    private static readonly Brush AccentBrush = new SolidColorBrush(Color.FromRgb(50, 140, 255));
    private static readonly Brush TextBrush = new SolidColorBrush(Color.FromRgb(236, 244, 255));
    private static readonly Brush MutedBrush = new SolidColorBrush(Color.FromRgb(154, 170, 189));

    private readonly ProfileService _profiles = new();
    private readonly JoystickService _joysticks = new();
    private readonly ObservableCollection<BindingEntry> _allBindings = [];
    private readonly ObservableCollection<BindingEntry> _intentCandidates = [];
    private readonly HashSet<string> _mappingFolders = new(StringComparer.OrdinalIgnoreCase);
    private readonly DispatcherTimer _captureTimer;
    private readonly Dictionary<int, JoystickSnapshot> _baselines = [];

    private readonly ListBox _devices = new();
    private readonly ListBox _profileList = new();
    private readonly ListBox _bindings = new();
    private readonly ItemsControl _intentList = new();
    private readonly TextBox _search = new();
    private readonly TextBlock _count = new();
    private readonly TextBlock _actionName = new();
    private readonly TextBlock _actionMeta = new();
    private readonly TextBlock _description = new();
    private readonly TextBox _input = new();
    private readonly TextBlock _intentHelp = new();
    private readonly TextBlock _status = new();
    private readonly TextBlock _activeProfileText = new();
    private readonly Border _capturePanel = new();
    private readonly TextBlock _captureText = new();
    private readonly Button _listen = new();
    private readonly Button _save = new();
    private readonly Button _unbind = new();
    private readonly Button _applyIntent = new();

    private string? _activeProfile;
    private bool _capturing;

    public MainWindow()
    {
        Title = "StarConfig - Star Citizen Control Mapper";
        Width = 1500;
        Height = 900;
        MinWidth = 1120;
        MinHeight = 720;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = BackgroundBrush;
        Foreground = TextBrush;
        FontFamily = new FontFamily("Segoe UI");
        Content = BuildInterface();

        PreviewKeyDown += OnPreviewKeyDown;
        PreviewMouseDown += OnPreviewMouseDown;
        _captureTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(45) };
        _captureTimer.Tick += CaptureTimerTick;

        RefreshEverything();
    }

    private UIElement BuildInterface()
    {
        var root = new Grid { Margin = new Thickness(12) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = Panel();
        Grid.SetRow(header, 0);
        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition());
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.Child = headerGrid;
        var brand = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        brand.Children.Add(new TextBlock { Text = "STARCONFIG", FontSize = 25, FontWeight = FontWeights.Bold });
        brand.Children.Add(new TextBlock { Text = "  WINDOWS STAR CITIZEN CONTROL MAPPER", Foreground = MutedBrush, VerticalAlignment = VerticalAlignment.Center });
        headerGrid.Children.Add(brand);
        var headerButtons = new StackPanel { Orientation = Orientation.Horizontal };
        Grid.SetColumn(headerButtons, 1);
        headerButtons.Children.Add(Button("Refresh Everything", (_, _) => RefreshEverything()));
        headerButtons.Children.Add(Button("Add Mappings Folder", ChooseFolder));
        headerButtons.Children.Add(Button("Open RSI Launcher", LaunchRsi, new SolidColorBrush(Color.FromRgb(23, 93, 42))));
        headerGrid.Children.Add(headerButtons);
        root.Children.Add(header);

        var body = new Grid { Margin = new Thickness(0, 10, 0, 10) };
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(285) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(410) });
        Grid.SetRow(body, 1);
        root.Children.Add(body);

        var left = Panel();
        Grid.SetColumn(left, 0);
        var leftGrid = new Grid();
        leftGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        leftGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(220) });
        leftGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        leftGrid.RowDefinitions.Add(new RowDefinition());
        left.Child = leftGrid;
        var deviceHeader = new DockPanel();
        deviceHeader.Children.Add(new TextBlock { Text = "CONNECTED DEVICES", FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
        var refreshDevices = Button("Refresh", (_, _) => RefreshDevices());
        refreshDevices.Padding = new Thickness(8, 3, 8, 3);
        DockPanel.SetDock(refreshDevices, Dock.Right);
        deviceHeader.Children.Add(refreshDevices);
        leftGrid.Children.Add(deviceHeader);
        StyleList(_devices);
        _devices.DisplayMemberPath = nameof(InputDevice.Summary);
        _devices.SelectionChanged += (_, _) => DeviceSelected();
        Grid.SetRow(_devices, 1);
        leftGrid.Children.Add(_devices);
        var profilesTitle = new TextBlock { Text = "STAR CITIZEN PROFILES", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 14, 0, 8) };
        Grid.SetRow(profilesTitle, 2);
        leftGrid.Children.Add(profilesTitle);
        StyleList(_profileList);
        _profileList.DisplayMemberPath = nameof(FileInfo.Name);
        _profileList.SelectionChanged += (_, _) => { if (_profileList.SelectedItem is FileInfo file) LoadProfile(file.FullName); };
        Grid.SetRow(_profileList, 3);
        leftGrid.Children.Add(_profileList);
        body.Children.Add(left);

        var center = Panel();
        center.Margin = new Thickness(10, 0, 10, 0);
        Grid.SetColumn(center, 1);
        var centerGrid = new Grid();
        centerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        centerGrid.RowDefinitions.Add(new RowDefinition());
        center.Child = centerGrid;
        var bindingHeader = new Grid();
        bindingHeader.ColumnDefinitions.Add(new ColumnDefinition());
        bindingHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(320) });
        var bindingTitle = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        bindingTitle.Children.Add(new TextBlock { Text = "BINDINGS", FontWeight = FontWeights.SemiBold });
        _count.Foreground = MutedBrush;
        _count.Margin = new Thickness(10, 0, 0, 0);
        bindingTitle.Children.Add(_count);
        bindingHeader.Children.Add(bindingTitle);
        StyleTextBox(_search);
        _search.ToolTip = "Search action, map, state, intent, or input";
        _search.TextChanged += (_, _) => ApplyFilter();
        Grid.SetColumn(_search, 1);
        bindingHeader.Children.Add(_search);
        centerGrid.Children.Add(bindingHeader);
        StyleList(_bindings);
        _bindings.DisplayMemberPath = nameof(BindingEntry.Display);
        _bindings.SelectionChanged += (_, _) => BindingSelected();
        _bindings.Margin = new Thickness(0, 10, 0, 0);
        Grid.SetRow(_bindings, 1);
        centerGrid.Children.Add(_bindings);
        body.Children.Add(center);

        var right = Panel();
        Grid.SetColumn(right, 2);
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var editor = new StackPanel();
        scroll.Content = editor;
        right.Child = scroll;
        editor.Children.Add(Heading("SELECTED ACTION"));
        _actionName.Text = "Select a binding";
        _actionName.FontSize = 20;
        _actionName.Margin = new Thickness(0, 8, 0, 4);
        _actionName.TextWrapping = TextWrapping.Wrap;
        editor.Children.Add(_actionName);
        _actionMeta.Foreground = AccentBrush;
        _actionMeta.Margin = new Thickness(0, 0, 0, 12);
        _actionMeta.TextWrapping = TextWrapping.Wrap;
        editor.Children.Add(_actionMeta);
        editor.Children.Add(Heading("WHAT IT DOES"));
        _description.Text = "Select an action to see its explanation.";
        _description.Foreground = MutedBrush;
        _description.TextWrapping = TextWrapping.Wrap;
        _description.Margin = new Thickness(0, 6, 0, 16);
        editor.Children.Add(_description);
        editor.Children.Add(Heading("ASSIGNED INPUT"));
        StyleTextBox(_input);
        _input.Margin = new Thickness(0, 6, 0, 6);
        editor.Children.Add(_input);
        var actionButtons = new Grid();
        actionButtons.ColumnDefinitions.Add(new ColumnDefinition());
        actionButtons.ColumnDefinitions.Add(new ColumnDefinition());
        actionButtons.ColumnDefinitions.Add(new ColumnDefinition());
        ConfigureEditorButton(_listen, "Listen for Input", ListenForInput);
        ConfigureEditorButton(_unbind, "Unbind", (_, _) => SaveCurrent("Unbound"));
        ConfigureEditorButton(_save, "Save Binding", (_, _) => SaveCurrent(Normalize(_input.Text)), new SolidColorBrush(Color.FromRgb(23, 93, 42)));
        Grid.SetColumn(_unbind, 1);
        Grid.SetColumn(_save, 2);
        actionButtons.Children.Add(_listen);
        actionButtons.Children.Add(_unbind);
        actionButtons.Children.Add(_save);
        editor.Children.Add(actionButtons);

        _capturePanel.Background = new SolidColorBrush(Color.FromRgb(24, 42, 59));
        _capturePanel.BorderBrush = AccentBrush;
        _capturePanel.BorderThickness = new Thickness(1);
        _capturePanel.CornerRadius = new CornerRadius(5);
        _capturePanel.Padding = new Thickness(10);
        _capturePanel.Margin = new Thickness(0, 6, 0, 0);
        _capturePanel.Visibility = Visibility.Collapsed;
        var captureStack = new StackPanel();
        captureStack.Children.Add(new TextBlock { Text = "LISTENING...", Foreground = AccentBrush, FontWeight = FontWeights.Bold });
        _captureText.TextWrapping = TextWrapping.Wrap;
        _captureText.Margin = new Thickness(0, 4, 0, 0);
        captureStack.Children.Add(_captureText);
        _capturePanel.Child = captureStack;
        editor.Children.Add(_capturePanel);

        editor.Children.Add(new Separator { Margin = new Thickness(0, 14, 0, 14) });
        editor.Children.Add(Heading("SAME INTENT IN OTHER GAME STATES"));
        _intentHelp.Text = "Select a binding. Compatible actions will appear here without merging cycle, toggle, or direct commands.";
        _intentHelp.Foreground = MutedBrush;
        _intentHelp.TextWrapping = TextWrapping.Wrap;
        _intentHelp.Margin = new Thickness(0, 6, 0, 8);
        editor.Children.Add(_intentHelp);
        _intentList.ItemsSource = _intentCandidates;
        var template = new DataTemplate(typeof(BindingEntry));
        var checkFactory = new FrameworkElementFactory(typeof(CheckBox));
        checkFactory.SetBinding(CheckBox.IsCheckedProperty, new System.Windows.Data.Binding(nameof(BindingEntry.IsIntentSelected)) { Mode = System.Windows.Data.BindingMode.TwoWay });
        checkFactory.SetBinding(ContentControl.ContentProperty, new System.Windows.Data.Binding(nameof(BindingEntry.Display)));
        checkFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(3));
        template.VisualTree = checkFactory;
        _intentList.ItemTemplate = template;
        editor.Children.Add(_intentList);
        ConfigureEditorButton(_applyIntent, "Apply Input to Checked States", ApplyIntent, new SolidColorBrush(Color.FromRgb(23, 74, 112)));
        _applyIntent.Margin = new Thickness(4, 8, 4, 4);
        editor.Children.Add(_applyIntent);
        editor.Children.Add(new Separator { Margin = new Thickness(0, 14, 0, 14) });
        editor.Children.Add(Heading("PROFILE SAFETY"));
        editor.Children.Add(new TextBlock { Text = "Every save creates a timestamped copy in a StarConfig Backups folder beside the profile. The new XML is parsed before it replaces the original.", Foreground = MutedBrush, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0) });
        body.Children.Add(right);

        var footer = Panel();
        Grid.SetRow(footer, 2);
        var footerGrid = new Grid();
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition());
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _status.Text = "Ready. Windows only.";
        _status.VerticalAlignment = VerticalAlignment.Center;
        _status.TextWrapping = TextWrapping.Wrap;
        footerGrid.Children.Add(_status);
        _activeProfileText.Foreground = MutedBrush;
        _activeProfileText.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(_activeProfileText, 1);
        footerGrid.Children.Add(_activeProfileText);
        footer.Child = footerGrid;
        root.Children.Add(footer);
        return root;
    }

    private Border Panel() => new() { Background = PanelBrush, BorderBrush = BorderBrush, BorderThickness = new Thickness(1), Padding = new Thickness(12), CornerRadius = new CornerRadius(7) };
    private TextBlock Heading(string text) => new() { Text = text, FontWeight = FontWeights.SemiBold };

    private Button Button(string text, RoutedEventHandler handler, Brush? background = null)
    {
        var button = new Button { Content = text, Background = background ?? CardBrush, Foreground = TextBrush, BorderBrush = BorderBrush, Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(4) };
        button.Click += handler;
        return button;
    }

    private void ConfigureEditorButton(Button button, string text, RoutedEventHandler handler, Brush? background = null)
    {
        button.Content = text;
        button.Background = background ?? CardBrush;
        button.Foreground = TextBrush;
        button.BorderBrush = BorderBrush;
        button.Padding = new Thickness(8, 7, 8, 7);
        button.Margin = new Thickness(4);
        button.IsEnabled = false;
        button.Click += handler;
    }

    private void StyleList(ListBox list)
    {
        list.Background = PanelBrush;
        list.Foreground = TextBrush;
        list.BorderBrush = BorderBrush;
    }

    private void StyleTextBox(TextBox box)
    {
        box.Background = BackgroundBrush;
        box.Foreground = TextBrush;
        box.BorderBrush = BorderBrush;
        box.Padding = new Thickness(8);
    }

    private void RefreshEverything()
    {
        RefreshDevices();
        foreach (var folder in _profiles.FindMappingsFolders()) _mappingFolders.Add(folder);
        LoadProfiles();
    }

    private void RefreshDevices()
    {
        var devices = _joysticks.GetConnectedDevices();
        _devices.ItemsSource = devices;
        if (devices.Count > 0) _devices.SelectedIndex = 0;
        _status.Text = devices.Count == 0 ? "Keyboard and mouse are available. No joystick-class devices were detected." : $"Detected {devices.Count} joystick-class device(s), plus keyboard and mouse.";
    }

    private void ChooseFolder(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Choose a Star Citizen Controls\\Mappings folder" };
        if (dialog.ShowDialog() != true) return;
        _mappingFolders.Add(dialog.FolderName);
        LoadProfiles();
    }

    private void LoadProfiles()
    {
        var previous = _activeProfile;
        var files = _profiles.GetProfiles(_mappingFolders).Select(path => new FileInfo(path)).ToList();
        _profileList.ItemsSource = files;
        if (files.Count == 0)
        {
            _status.Text = "No exported Star Citizen XML profiles were found. Export a profile in game or add its Mappings folder.";
            return;
        }
        _profileList.SelectedItem = files.FirstOrDefault(x => string.Equals(x.FullName, previous, StringComparison.OrdinalIgnoreCase)) ?? files[0];
    }

    private void LoadProfile(string file)
    {
        try
        {
            StopCapture();
            _activeProfile = file;
            _allBindings.Clear();
            foreach (var binding in _profiles.LoadBindings(file)) _allBindings.Add(binding);
            ApplyFilter();
            _activeProfileText.Text = Path.GetFileName(file);
            _status.Text = $"Loaded {Path.GetFileName(file)} with {_allBindings.Count:N0} actions.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Could not load profile", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyFilter()
    {
        var term = _search.Text.Trim();
        var filtered = string.IsNullOrWhiteSpace(term) ? _allBindings.ToList() : _allBindings.Where(x =>
            x.ActionName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            x.ActionMap.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            x.Context.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            x.Intent.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            x.Input.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        _bindings.ItemsSource = filtered;
        _count.Text = $"{filtered.Count:N0} shown";
    }

    private void BindingSelected()
    {
        StopCapture();
        if (_bindings.SelectedItem is not BindingEntry selected)
        {
            EnableEditor(false);
            return;
        }
        _actionName.Text = selected.ActionName;
        _actionMeta.Text = $"{selected.Context}  |  {selected.ActionMap}  |  {selected.Behavior}";
        _description.Text = selected.Description;
        _input.Text = selected.Input;
        EnableEditor(_activeProfile is not null);
        PopulateIntent(selected);
    }

    private void PopulateIntent(BindingEntry selected)
    {
        _intentCandidates.Clear();
        if (string.IsNullOrWhiteSpace(selected.Intent))
        {
            _intentHelp.Text = "No safe cross-state relationship is known for this action. It remains individually bindable.";
            _applyIntent.IsEnabled = false;
            return;
        }
        var peers = _allBindings.Where(x => x.Intent.Equals(selected.Intent, StringComparison.OrdinalIgnoreCase))
            .Where(x => x.Behavior.Equals(selected.Behavior, StringComparison.OrdinalIgnoreCase) || selected.Behavior == "Axis")
            .OrderBy(x => x.Context).ThenBy(x => x.ActionName).ToList();
        foreach (var peer in peers)
        {
            peer.IsIntentSelected = peer.Identity == selected.Identity || peer.Input.Equals(selected.Input, StringComparison.OrdinalIgnoreCase);
            _intentCandidates.Add(peer);
        }
        _intentHelp.Text = $"These actions express '{selected.Intent}' with compatible behavior. Check only the states that should use '{_input.Text}'.";
        _applyIntent.IsEnabled = peers.Count > 1;
    }

    private void EnableEditor(bool enabled)
    {
        _listen.IsEnabled = enabled;
        _save.IsEnabled = enabled;
        _unbind.IsEnabled = enabled;
    }

    private void SaveCurrent(string input)
    {
        if (_bindings.SelectedItem is BindingEntry selected) SaveChanges([(selected, input)]);
    }

    private void ApplyIntent(object sender, RoutedEventArgs e)
    {
        var checkedItems = _intentCandidates.Where(x => x.IsIntentSelected).ToList();
        if (checkedItems.Count == 0)
        {
            MessageBox.Show("Check at least one game-state action first.", "StarConfig", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var input = Normalize(_input.Text);
        var summary = string.Join(Environment.NewLine, checkedItems.Select(x => $"• {x.Context}: {x.ActionName}"));
        if (MessageBox.Show($"Apply '{input}' to these actions?\n\n{summary}", "Confirm cross-state binding", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            SaveChanges(checkedItems.Select(x => (x, input)));
    }

    private void SaveChanges(IEnumerable<(BindingEntry Binding, string Input)> changes)
    {
        if (_activeProfile is null) return;
        try
        {
            var list = changes.ToList();
            var backup = _profiles.SaveBindings(_activeProfile, list);
            LoadProfile(_activeProfile);
            _status.Text = $"Saved {list.Count} binding change(s). Backup: {backup}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Could not save binding", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ListenForInput(object sender, RoutedEventArgs e)
    {
        if (_capturing) { StopCapture(); return; }
        _baselines.Clear();
        foreach (var device in _joysticks.GetConnectedDevices())
        {
            var snapshot = _joysticks.GetSnapshot(device.Id);
            if (snapshot is not null) _baselines[device.Id] = snapshot;
        }
        _capturing = true;
        _capturePanel.Visibility = Visibility.Visible;
        _captureText.Text = "Move one axis decisively, press a joystick button, keyboard key, or mouse button. Press Escape to cancel.";
        _listen.Content = "Cancel Listening";
        _captureTimer.Start();
        Focus();
    }

    private void CaptureTimerTick(object? sender, EventArgs e)
    {
        if (!_capturing) return;
        foreach (var baseline in _baselines)
        {
            var activity = _joysticks.DetectActivity(baseline.Key, baseline.Value);
            if (activity is null) continue;
            AcceptInput(activity.Input, activity.Description);
            break;
        }
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_capturing) return;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape) { StopCapture(); e.Handled = true; return; }
        var name = key switch
        {
            Key.Space => "space", Key.LeftCtrl => "lctrl", Key.RightCtrl => "rctrl", Key.LeftShift => "lshift", Key.RightShift => "rshift",
            Key.LeftAlt => "lalt", Key.RightAlt => "ralt", Key.Return => "enter", Key.Back => "backspace", _ => key.ToString().ToLowerInvariant()
        };
        AcceptInput($"kb1_{name}", $"Keyboard {key}");
        e.Handled = true;
    }

    private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!_capturing) return;
        var button = e.ChangedButton switch { MouseButton.Left => 1, MouseButton.Right => 2, MouseButton.Middle => 3, MouseButton.XButton1 => 4, MouseButton.XButton2 => 5, _ => 0 };
        if (button == 0) return;
        AcceptInput($"mouse1_button{button}", $"Mouse Button {button}");
        e.Handled = true;
    }

    private void AcceptInput(string input, string description)
    {
        _input.Text = input;
        _captureText.Text = $"Captured {description}: {input}";
        StopCapture(true);
        _status.Text = $"Captured {description}. Review it, then save or apply it to checked states.";
    }

    private void StopCapture(bool keepPanel = false)
    {
        _captureTimer.Stop();
        _capturing = false;
        _baselines.Clear();
        _listen.Content = "Listen for Input";
        if (!keepPanel) _capturePanel.Visibility = Visibility.Collapsed;
    }

    private void DeviceSelected()
    {
        if (_devices.SelectedItem is InputDevice device)
            _status.Text = $"Selected Joy{device.Id}: {device.Name}. Use Listen for Input to capture a physical control.";
    }

    private void LaunchRsi(object sender, RoutedEventArgs e)
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Roberts Space Industries", "RSI Launcher", "RSI Launcher.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "RSI Launcher", "RSI Launcher.exe")
        };
        var launcher = candidates.FirstOrDefault(File.Exists);
        if (launcher is null)
        {
            MessageBox.Show("RSI Launcher was not found in the common Windows locations.", "StarConfig", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        Process.Start(new ProcessStartInfo(launcher) { UseShellExecute = true });
    }

    private static string Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? "Unbound" : value.Trim();
}

public sealed class BindingEntry : INotifyPropertyChanged
{
    private string _input = "Unbound";
    private bool _isIntentSelected;
    public string ActionMap { get; init; } = "Unknown";
    public string ActionName { get; init; } = "Unknown";
    public int ActionOrdinal { get; init; }
    public string Description { get; init; } = "No description is available.";
    public string Context { get; init; } = "General";
    public string Intent { get; init; } = string.Empty;
    public string Behavior { get; init; } = "Direct";
    public string Input { get => _input; set { if (_input == value) return; _input = value; Changed(); Changed(nameof(Display)); } }
    public bool IsIntentSelected { get => _isIntentSelected; set { if (_isIntentSelected == value) return; _isIntentSelected = value; Changed(); } }
    public string Display => $"[{Context}] {ActionName}  ->  {Input}";
    public string Identity => $"{ActionMap}|{ActionName}|{ActionOrdinal}";
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed record InputDevice(int Id, string Name, int Buttons, int Axes)
{
    public string Summary => $"Joy{Id}: {Name}  |  {Buttons} buttons  |  {Axes} axes";
}

public sealed record JoystickSnapshot(uint X, uint Y, uint Z, uint R, uint U, uint V, uint Buttons, uint Pov);
public sealed record InputActivity(string Input, string Description);

public sealed class JoystickService
{
    private const uint JoyNoError = 0;
    private const uint JoyReturnAll = 0xFF;
    private const uint JoyPovCentered = 0xFFFF;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct JoyCaps
    {
        public ushort ManufacturerId, ProductId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string ProductName;
        public uint XMin, XMax, YMin, YMax, ZMin, ZMax, NumberOfButtons, PeriodMin, PeriodMax, RMin, RMax, UMin, UMax, VMin, VMax, Capabilities, MaxAxes, NumberOfAxes, MaxButtons;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string RegistryKey;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string OemVxd;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JoyInfoEx
    {
        public uint Size, Flags, X, Y, Z, R, U, V, Buttons, ButtonNumber, Pov, Reserved1, Reserved2;
    }

    [DllImport("winmm.dll", CharSet = CharSet.Unicode)] private static extern uint joyGetDevCaps(uint id, ref JoyCaps caps, uint size);
    [DllImport("winmm.dll")] private static extern uint joyGetNumDevs();
    [DllImport("winmm.dll")] private static extern uint joyGetPosEx(uint id, ref JoyInfoEx info);

    public IReadOnlyList<InputDevice> GetConnectedDevices()
    {
        var result = new List<InputDevice>();
        for (uint id = 0; id < joyGetNumDevs(); id++)
        {
            var caps = new JoyCaps();
            if (joyGetDevCaps(id, ref caps, (uint)Marshal.SizeOf<JoyCaps>()) != JoyNoError) continue;
            var name = string.IsNullOrWhiteSpace(caps.ProductName) ? $"Joystick {id + 1}" : caps.ProductName.Trim();
            result.Add(new InputDevice((int)id + 1, name, (int)caps.NumberOfButtons, (int)caps.NumberOfAxes));
        }
        return result;
    }

    public JoystickSnapshot? GetSnapshot(int id)
    {
        var info = new JoyInfoEx { Size = (uint)Marshal.SizeOf<JoyInfoEx>(), Flags = JoyReturnAll };
        return joyGetPosEx((uint)(id - 1), ref info) == JoyNoError ? new JoystickSnapshot(info.X, info.Y, info.Z, info.R, info.U, info.V, info.Buttons, info.Pov) : null;
    }

    public InputActivity? DetectActivity(int id, JoystickSnapshot baseline)
    {
        var current = GetSnapshot(id);
        if (current is null) return null;
        var pressed = current.Buttons & ~baseline.Buttons;
        for (var bit = 0; bit < 32; bit++) if ((pressed & (1u << bit)) != 0) return new InputActivity($"js{id}_button{bit + 1}", $"Joy{id} Button {bit + 1}");
        var axes = new[] { ("x", current.X, baseline.X), ("y", current.Y, baseline.Y), ("z", current.Z, baseline.Z), ("rotx", current.R, baseline.R), ("slider1", current.U, baseline.U), ("slider2", current.V, baseline.V) };
        var moved = axes.Select(a => new { a.Item1, Delta = Math.Abs((long)a.Item2 - a.Item3) }).OrderByDescending(a => a.Delta).First();
        if (moved.Delta >= 9000) return new InputActivity($"js{id}_{moved.Item1}", $"Joy{id} {moved.Item1.ToUpperInvariant()} Axis");
        if (current.Pov != JoyPovCentered && current.Pov != baseline.Pov)
        {
            var direction = current.Pov switch { < 4500 or >= 31500 => "up", < 13500 => "right", < 22500 => "down", _ => "left" };
            return new InputActivity($"js{id}_hat1_{direction}", $"Joy{id} Hat 1 {direction}");
        }
        return null;
    }
}

public sealed class ProfileService
{
    private readonly ActionKnowledge _knowledge = new();

    public IReadOnlyList<string> FindMappingsFolders()
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Roberts Space Industries", "StarCitizen"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Roberts Space Industries", "StarCitizen")
        };
        foreach (var root in candidates.Where(Directory.Exists))
            foreach (var channel in Directory.EnumerateDirectories(root))
            {
                var mappings = Path.Combine(channel, "USER", "Client", "0", "Controls", "Mappings");
                if (Directory.Exists(mappings)) roots.Add(mappings);
            }
        return roots.OrderBy(x => x).ToList();
    }

    public IReadOnlyList<string> GetProfiles(IEnumerable<string> folders) => folders.Where(Directory.Exists).SelectMany(x => Directory.EnumerateFiles(x, "*.xml")).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(Path.GetFileName).ToList();

    public IReadOnlyList<BindingEntry> LoadBindings(string file)
    {
        var doc = XDocument.Load(file, LoadOptions.PreserveWhitespace);
        var result = new List<BindingEntry>();
        foreach (var map in doc.Descendants().Where(IsMap))
        {
            var mapName = (string?)map.Attribute("name") ?? "Unknown";
            var actions = map.Elements().Where(IsAction).ToList();
            for (var index = 0; index < actions.Count; index++)
            {
                var action = actions[index];
                var name = (string?)action.Attribute("name") ?? "Unknown";
                var input = (string?)action.Descendants().FirstOrDefault(IsRebind)?.Attribute("input") ?? "Unbound";
                var info = _knowledge.Explain(mapName, name);
                result.Add(new BindingEntry { ActionMap = mapName, ActionName = name, ActionOrdinal = index, Input = input, Context = Context(mapName, name), Description = info.Description, Intent = info.Intent, Behavior = info.Behavior });
            }
        }
        return result.OrderBy(x => x.Context).ThenBy(x => x.ActionName).ToList();
    }

    public string SaveBindings(string file, IEnumerable<(BindingEntry Binding, string Input)> requestedChanges)
    {
        var changes = requestedChanges.ToList();
        if (changes.Count == 0) throw new InvalidOperationException("There are no changes to save.");
        var backupFolder = Path.Combine(Path.GetDirectoryName(file)!, "StarConfig Backups");
        Directory.CreateDirectory(backupFolder);
        var backup = Path.Combine(backupFolder, $"{Path.GetFileNameWithoutExtension(file)}-{DateTime.Now:yyyyMMdd-HHmmssfff}.xml");
        File.Copy(file, backup, false);
        var doc = XDocument.Load(file, LoadOptions.PreserveWhitespace);
        foreach (var change in changes)
        {
            var map = doc.Descendants().FirstOrDefault(x => IsMap(x) && string.Equals((string?)x.Attribute("name"), change.Binding.ActionMap, StringComparison.OrdinalIgnoreCase)) ?? throw new InvalidOperationException($"Action map '{change.Binding.ActionMap}' was not found.");
            var actions = map.Elements().Where(IsAction).ToList();
            XElement? action = change.Binding.ActionOrdinal < actions.Count ? actions[change.Binding.ActionOrdinal] : null;
            if (action is not null && !string.Equals((string?)action.Attribute("name"), change.Binding.ActionName, StringComparison.OrdinalIgnoreCase)) action = null;
            action ??= actions.FirstOrDefault(x => string.Equals((string?)x.Attribute("name"), change.Binding.ActionName, StringComparison.OrdinalIgnoreCase));
            if (action is null) throw new InvalidOperationException($"Action '{change.Binding.ActionName}' was not found in '{change.Binding.ActionMap}'.");
            var rebind = action.Descendants().FirstOrDefault(IsRebind);
            var input = change.Input.Trim();
            if (string.IsNullOrWhiteSpace(input) || input.Equals("Unbound", StringComparison.OrdinalIgnoreCase)) { rebind?.Remove(); continue; }
            if (rebind is null) { rebind = new XElement("rebind"); action.Add(rebind); }
            rebind.SetAttributeValue("input", input);
        }
        var temp = file + ".starconfig.tmp";
        doc.Save(temp, SaveOptions.DisableFormatting);
        _ = XDocument.Load(temp);
        File.Move(temp, file, true);
        return backup;
    }

    private static bool IsMap(XElement x) => x.Name.LocalName.Equals("actionmap", StringComparison.OrdinalIgnoreCase);
    private static bool IsAction(XElement x) => x.Name.LocalName.Equals("action", StringComparison.OrdinalIgnoreCase);
    private static bool IsRebind(XElement x) => x.Name.LocalName.Equals("rebind", StringComparison.OrdinalIgnoreCase);
    private static string Context(string map, string action)
    {
        var text = (map + " " + action).ToLowerInvariant();
        if (text.Contains("eva")) return "EVA";
        if (text.Contains("turret")) return "Turret";
        if (text.Contains("mining")) return "Mining";
        if (text.Contains("salvage")) return "Salvage";
        if (text.Contains("vehicle") || text.Contains("ground")) return "Vehicle";
        if (text.Contains("spaceship") || text.Contains("flight") || text.Contains("ifcs")) return "Flight";
        if (text.Contains("player") || text.Contains("foot")) return "On Foot";
        return "General";
    }
}

public sealed class ActionKnowledge
{
    private static readonly Dictionary<string, ActionInfo> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        ["v_ifcs_throttleAbs"] = new("Moves the ship forward or backward with an absolute throttle axis.", "Move Forward / Backward", "Axis"),
        ["v_ifcs_strafeForwardBack"] = new("Applies forward or backward ship translation without changing facing.", "Move Forward / Backward", "Axis"),
        ["v_ifcs_pitch"] = new("Controls ship pitch, rotating the nose up or down.", "Pitch", "Axis"),
        ["v_ifcs_yaw"] = new("Controls ship yaw, rotating the nose left or right.", "Yaw", "Axis"),
        ["v_ifcs_roll"] = new("Controls ship roll around its forward axis.", "Roll", "Axis"),
        ["v_toggle_master_mode"] = new("Changes the ship between broad Master Modes such as SCM and NAV. This changes the ship operating regime.", "Master Mode", "Toggle"),
        ["v_cycle_master_mode"] = new("Cycles ship Master Modes. It changes the broad operating regime, not the active task interface.", "Master Mode", "Cycle"),
        ["v_toggle_operator_mode"] = new("Changes the active Operator Mode inside the current Master Mode, such as guns or missiles. It is not the same as changing Master Mode.", "Operator Mode", "Toggle"),
        ["v_cycle_operator_mode"] = new("Cycles Operator Modes inside the current Master Mode. It may reach a similar screen to another shortcut but operates at a different level.", "Operator Mode", "Cycle")
    };

    public ActionInfo Explain(string map, string action)
    {
        if (Known.TryGetValue(action, out var known)) return known;
        var lower = action.ToLowerInvariant();
        var behavior = lower.Contains("toggle") ? "Toggle" : lower.Contains("cycle") || lower.Contains("next") || lower.Contains("prev") ? "Cycle" : lower.Contains("hold") ? "Hold" : lower.Contains("abs") || lower.Contains("axis") ? "Axis" : "Direct";
        var text = (map + " " + action).ToLowerInvariant();
        var intent = text.Contains("forward") && text.Contains("back") || text.Contains("throttleabs") ? "Move Forward / Backward" : text.Contains("pitch") ? "Pitch" : text.Contains("yaw") ? "Yaw" : text.Contains("roll") ? "Roll" : text.Contains("attack1") ? "Primary Fire" : text.Contains("master") && text.Contains("mode") ? "Master Mode" : text.Contains("operator") && text.Contains("mode") ? "Operator Mode" : string.Empty;
        return new ActionInfo($"{Humanize(action)}. This action belongs to the {Humanize(map)} action map and behaves as a {behavior.ToLowerInvariant()} control. Similar-looking actions remain separate unless their intent and behavior are compatible.", intent, behavior);
    }

    private static string Humanize(string value) => Regex.Replace(Regex.Replace(value.Replace('_', ' '), "([a-z0-9])([A-Z])", "$1 $2"), "\\s+", " ").Trim();
}

public sealed record ActionInfo(string Description, string Intent, string Behavior);
