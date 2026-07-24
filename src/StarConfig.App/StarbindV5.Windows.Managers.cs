using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using ShapeEllipse = System.Windows.Shapes.Ellipse;
using ShapeLine = System.Windows.Shapes.Line;
using ShapeRectangle = System.Windows.Shapes.Rectangle;

namespace StarConfig;

public sealed partial class StarbindV5Window
{
    private void OpenProfileManager(object sender, RoutedEventArgs e)
    {
        var profiles = _settingsStore.DiscoverProfiles(_settings);
        var window = new ProfileManagerWindow(profiles, _profile?.FilePath) { Owner = this };
        if (window.ShowDialog() != true || string.IsNullOrWhiteSpace(window.SelectedProfile)) return;
        var selected = window.SelectedProfile;
        var folder = Path.GetDirectoryName(selected)!;
        if (!_settings.ProfileFolders.Contains(folder, StringComparer.OrdinalIgnoreCase)) _settings.ProfileFolders.Add(folder);
        _settings.LastProfile = selected;
        _settingsStore.Save(_settings);
        PopulateProfilePicker(_settingsStore.DiscoverProfiles(_settings), selected);
    }

    private void OpenSettingsWindow(object sender, RoutedEventArgs e)
    {
        var window = new StarbindSettingsWindow(_settings, _hardware.Templates) { Owner = this };
        if (window.ShowDialog() != true) return;
        _settings = window.Settings;
        _settingsStore.Save(_settings);
        _detectedDevices = _joysticks.GetConnectedDevices();
        if (_profile is not null) LoadProfile(_profile.FilePath);
        else InitializeApplication();
    }

    private void OpenHelpWindow(object sender, RoutedEventArgs e)
    {
        new StarbindHelpWindow { Owner = this }.ShowDialog();
    }

    private void OpenDeviceManager(object sender, RoutedEventArgs e)
    {
        var window = new DeviceManagerWindow(CurrentDevices(), _hardware, _settings) { Owner = this };
        if (window.ShowDialog() != true) return;
        _settings = window.Settings;
        _settingsStore.Save(_settings);
        BuildDeviceCards();
        BuildDevicePicker();
        if (_selectedDevice is not null) SelectDevice(_selectedDevice);
    }

    private void OpenDeviceTester(object sender, RoutedEventArgs e)
    {
        if (_selectedDevice is null)
        {
            SetStatus("Select a physical device first.");
            return;
        }
        new DeviceTestWindow(_selectedDevice, _joysticks, _hardware, _profile, _settings) { Owner = this }.Show();
    }

    private void OpenDevice3D(object sender, RoutedEventArgs e)
    {
        if (_selectedDevice is null || _selectedTemplate is null)
        {
            SetStatus("Select a physical device first.");
            return;
        }
        new Device3DWindow(_selectedDevice, _selectedTemplate) { Owner = this }.ShowDialog();
    }

    private void OpenActionComparison(object sender, RoutedEventArgs e)
    {
        if (_selectedAction is null || _profile is null) return;
        var peers = UniqueActions(_profile.Actions)
            .Where(candidate => candidate.Identity != _selectedAction.Identity && candidate.Intent.Equals(_selectedAction.Intent, StringComparison.OrdinalIgnoreCase))
            .OrderBy(candidate => candidate.Context)
            .ThenBy(candidate => candidate.Behavior)
            .ToList();
        new ActionComparisonWindow(_selectedAction, peers) { Owner = this }.ShowDialog();
    }

    private void OpenConflictResolver(object sender, RoutedEventArgs e)
    {
        if (_profile is null) return;
        var conflicts = FindConflictGroups();
        if (conflicts.Count == 0)
        {
            MessageBox.Show("No duplicate input groups were found.", "Starbind", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var window = new ConflictResolverWindow(conflicts) { Owner = this };
        if (window.ShowDialog() != true) return;
        try
        {
            var backup = _profiles.SaveConflictResolutions(_profile, window.Resolutions);
            var path = _profile.FilePath;
            LoadProfile(path);
            SetStatus($"Resolved {window.Resolutions.Count} conflict group(s). Backup: {Path.GetFileName(backup)}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Conflict resolution failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

public sealed class ProfileManagerWindow : Window
{
    private readonly ObservableCollection<ProfileListItem> _profiles;
    private readonly ListBox _list = new();
    public string? SelectedProfile { get; private set; }

    public ProfileManagerWindow(IReadOnlyList<string> profiles, string? activeProfile)
    {
        _profiles = new ObservableCollection<ProfileListItem>(profiles.Select(path => new ProfileListItem(path, Path.GetFileNameWithoutExtension(path), StarbindV5SettingsStore.DetectChannel(path), File.GetLastWriteTime(path))));
        Title = "Starbind Profiles";
        Width = 820;
        Height = 600;
        MinWidth = 680;
        MinHeight = 480;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = StarbindV5Window.Bg;
        Foreground = StarbindV5Window.Text;
        FontFamily = new FontFamily("Segoe UI");
        Content = Build(activeProfile);
    }

    private UIElement Build(string? activeProfile)
    {
        var root = new Grid { Margin = new Thickness(14) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var intro = new StackPanel();
        intro.Children.Add(new TextBlock { Text = "PROFILE MANAGEMENT", Foreground = StarbindV5Window.Cyan, FontWeight = FontWeights.Bold, FontSize = 14 });
        intro.Children.Add(new TextBlock { Text = "Open, import, duplicate or export Star Citizen control profiles. Starbind never needs the repository or build tools on this computer.", Foreground = StarbindV5Window.Muted, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 7, 0, 10) });
        root.Children.Add(intro);

        _list.ItemsSource = _profiles;
        _list.Background = StarbindV5Window.Field;
        _list.Foreground = StarbindV5Window.Text;
        _list.BorderBrush = StarbindV5Window.Border;
        _list.DisplayMemberPath = nameof(ProfileListItem.Name);
        _list.SelectionMode = SelectionMode.Single;
        _list.MouseDoubleClick += (_, _) => SelectAndClose();
        _list.SelectedItem = _profiles.FirstOrDefault(item => item.FilePath.Equals(activeProfile, StringComparison.OrdinalIgnoreCase)) ?? _profiles.FirstOrDefault();
        Grid.SetRow(_list, 1);
        root.Children.Add(_list);

        var buttons = new Grid { Margin = new Thickness(0, 10, 0, 0) };
        buttons.ColumnDefinitions.Add(new ColumnDefinition());
        for (var i = 0; i < 6; i++) buttons.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var details = new TextBlock { Foreground = StarbindV5Window.Muted, VerticalAlignment = VerticalAlignment.Center };
        _list.SelectionChanged += (_, _) => details.Text = _list.SelectedItem is ProfileListItem item ? $"{item.Channel}  •  {item.Modified:g}\n{item.FilePath}" : string.Empty;
        if (_list.SelectedItem is ProfileListItem initial) details.Text = $"{initial.Channel}  •  {initial.Modified:g}\n{initial.FilePath}";
        buttons.Children.Add(details);
        AddButton(buttons, 1, "IMPORT", ImportProfile);
        AddButton(buttons, 2, "DUPLICATE", DuplicateProfile);
        AddButton(buttons, 3, "EXPORT", ExportProfile);
        AddButton(buttons, 4, "OPEN FOLDER", OpenFolder);
        AddButton(buttons, 5, "CANCEL", (_, _) => { DialogResult = false; Close(); });
        AddButton(buttons, 6, "USE PROFILE", (_, _) => SelectAndClose(), StarbindV5Window.BlueDim);
        Grid.SetRow(buttons, 2);
        root.Children.Add(buttons);
        return root;
    }

    private void ImportProfile(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Import Star Citizen profile", Filter = "Star Citizen profiles (*.xml)|*.xml", Multiselect = false };
        if (dialog.ShowDialog(this) != true) return;
        var item = new ProfileListItem(dialog.FileName, Path.GetFileNameWithoutExtension(dialog.FileName), StarbindV5SettingsStore.DetectChannel(dialog.FileName), File.GetLastWriteTime(dialog.FileName));
        if (_profiles.All(existing => !existing.FilePath.Equals(item.FilePath, StringComparison.OrdinalIgnoreCase))) _profiles.Insert(0, item);
        _list.SelectedItem = item;
    }

    private void DuplicateProfile(object sender, RoutedEventArgs e)
    {
        if (_list.SelectedItem is not ProfileListItem item) return;
        var dialog = new SaveFileDialog { Title = "Duplicate control profile", Filter = "Star Citizen profile (*.xml)|*.xml", FileName = item.Name + "_copy.xml", InitialDirectory = Path.GetDirectoryName(item.FilePath) };
        if (dialog.ShowDialog(this) != true) return;
        File.Copy(item.FilePath, dialog.FileName, false);
        var duplicate = new ProfileListItem(dialog.FileName, Path.GetFileNameWithoutExtension(dialog.FileName), StarbindV5SettingsStore.DetectChannel(dialog.FileName), File.GetLastWriteTime(dialog.FileName));
        _profiles.Insert(0, duplicate);
        _list.SelectedItem = duplicate;
    }

    private void ExportProfile(object sender, RoutedEventArgs e)
    {
        if (_list.SelectedItem is not ProfileListItem item) return;
        var dialog = new SaveFileDialog { Title = "Export control profile", Filter = "Star Citizen profile (*.xml)|*.xml", FileName = Path.GetFileName(item.FilePath) };
        if (dialog.ShowDialog(this) != true) return;
        File.Copy(item.FilePath, dialog.FileName, true);
    }

    private void OpenFolder(object sender, RoutedEventArgs e)
    {
        if (_list.SelectedItem is not ProfileListItem item) return;
        Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{item.FilePath}\"") { UseShellExecute = true });
    }

    private void SelectAndClose()
    {
        if (_list.SelectedItem is not ProfileListItem item) return;
        SelectedProfile = item.FilePath;
        DialogResult = true;
        Close();
    }

    private static void AddButton(Grid grid, int column, string text, RoutedEventHandler click, Brush? background = null)
    {
        var button = StarbindV5Window.DialogButton(text, background ?? StarbindV5Window.Panel2);
        button.Click += click;
        Grid.SetColumn(button, column);
        grid.Children.Add(button);
    }
}

public sealed class StarbindSettingsWindow : Window
{
    private readonly TextBox _launcher = new();
    private readonly ListBox _folders = new();
    private readonly CheckBox _confirm = new();
    private readonly IReadOnlyList<HardwareTemplate> _templates;
    public StarbindV5Settings Settings { get; }

    public StarbindSettingsWindow(StarbindV5Settings source, IReadOnlyList<HardwareTemplate> templates)
    {
        Settings = new StarbindV5Settings
        {
            LastProfile = source.LastProfile,
            LauncherPath = source.LauncherPath,
            ProfileFolders = source.ProfileFolders.ToList(),
            DeviceRoleOverrides = new Dictionary<string, string>(source.DeviceRoleOverrides, StringComparer.OrdinalIgnoreCase),
            ConfirmBeforeWrite = source.ConfirmBeforeWrite
        };
        _templates = templates;
        Title = "Starbind Settings";
        Width = 760;
        Height = 590;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = StarbindV5Window.Bg;
        Foreground = StarbindV5Window.Text;
        FontFamily = new FontFamily("Segoe UI");
        Content = Build();
    }

    private UIElement Build()
    {
        var root = new Grid { Margin = new Thickness(14) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.Children.Add(new TextBlock { Text = "SETTINGS", Foreground = StarbindV5Window.Cyan, FontWeight = FontWeights.Bold, FontSize = 14 });
        var tabs = new TabControl { Background = StarbindV5Window.Panel, Foreground = StarbindV5Window.Text, BorderBrush = StarbindV5Window.Border, Margin = new Thickness(0, 10, 0, 0) };
        tabs.Items.Add(BuildGeneralTab());
        tabs.Items.Add(BuildFoldersTab());
        tabs.Items.Add(BuildHardwareTab());
        Grid.SetRow(tabs, 1);
        root.Children.Add(tabs);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
        var cancel = StarbindV5Window.DialogButton("CANCEL", StarbindV5Window.Panel2);
        cancel.Click += (_, _) => { DialogResult = false; Close(); };
        buttons.Children.Add(cancel);
        var save = StarbindV5Window.DialogButton("SAVE SETTINGS", StarbindV5Window.BlueDim);
        save.Click += (_, _) =>
        {
            Settings.LauncherPath = _launcher.Text.Trim();
            Settings.ConfirmBeforeWrite = _confirm.IsChecked == true;
            Settings.ProfileFolders = _folders.Items.Cast<string>().Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            DialogResult = true;
            Close();
        };
        buttons.Children.Add(save);
        Grid.SetRow(buttons, 2);
        root.Children.Add(buttons);
        return root;
    }

    private TabItem BuildGeneralTab()
    {
        var tab = new TabItem { Header = "General" };
        var stack = new StackPanel { Margin = new Thickness(12) };
        stack.Children.Add(new TextBlock { Text = "RSI LAUNCHER", Foreground = StarbindV5Window.Faint, FontSize = 9 });
        var launcherGrid = new Grid { Margin = new Thickness(0, 5, 0, 12) };
        launcherGrid.ColumnDefinitions.Add(new ColumnDefinition());
        launcherGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _launcher.Text = Settings.LauncherPath ?? string.Empty;
        _launcher.Background = StarbindV5Window.Field;
        _launcher.Foreground = StarbindV5Window.Text;
        _launcher.BorderBrush = StarbindV5Window.Border;
        _launcher.Padding = new Thickness(6);
        launcherGrid.Children.Add(_launcher);
        var browse = StarbindV5Window.DialogButton("BROWSE", StarbindV5Window.Panel2);
        browse.Click += (_, _) =>
        {
            var dialog = new OpenFileDialog { Title = "Select RSI Launcher.exe", Filter = "RSI Launcher (RSI Launcher.exe)|RSI Launcher.exe|Executables (*.exe)|*.exe" };
            if (dialog.ShowDialog(this) == true) _launcher.Text = dialog.FileName;
        };
        Grid.SetColumn(browse, 1);
        launcherGrid.Children.Add(browse);
        stack.Children.Add(launcherGrid);
        _confirm.Content = "Confirm before writing a profile";
        _confirm.Foreground = StarbindV5Window.Text;
        _confirm.IsChecked = Settings.ConfirmBeforeWrite;
        stack.Children.Add(_confirm);
        stack.Children.Add(new TextBlock { Text = "Backups and XML validation are always enabled and cannot be turned off.", Foreground = StarbindV5Window.Muted, Margin = new Thickness(0, 8, 0, 0), TextWrapping = TextWrapping.Wrap });
        tab.Content = stack;
        return tab;
    }

    private TabItem BuildFoldersTab()
    {
        var tab = new TabItem { Header = "Profile Folders" };
        var grid = new Grid { Margin = new Thickness(12) };
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _folders.ItemsSource = Settings.ProfileFolders.ToList();
        _folders.Background = StarbindV5Window.Field;
        _folders.Foreground = StarbindV5Window.Text;
        _folders.BorderBrush = StarbindV5Window.Border;
        grid.Children.Add(_folders);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 8, 0, 0) };
        var add = StarbindV5Window.DialogButton("ADD FOLDER", StarbindV5Window.Panel2);
        add.Click += (_, _) =>
        {
            var dialog = new OpenFolderDialog { Title = "Add Star Citizen mappings folder", Multiselect = false };
            if (dialog.ShowDialog(this) == true)
            {
                var items = _folders.Items.Cast<string>().ToList();
                if (!items.Contains(dialog.FolderName, StringComparer.OrdinalIgnoreCase)) items.Add(dialog.FolderName);
                _folders.ItemsSource = items;
            }
        };
        buttons.Children.Add(add);
        var remove = StarbindV5Window.DialogButton("REMOVE", StarbindV5Window.Panel2);
        remove.Click += (_, _) =>
        {
            if (_folders.SelectedItem is not string selected) return;
            var items = _folders.Items.Cast<string>().Where(item => !item.Equals(selected, StringComparison.OrdinalIgnoreCase)).ToList();
            _folders.ItemsSource = items;
        };
        buttons.Children.Add(remove);
        Grid.SetRow(buttons, 1);
        grid.Children.Add(buttons);
        tab.Content = grid;
        return tab;
    }

    private TabItem BuildHardwareTab()
    {
        var tab = new TabItem { Header = "Hardware Definitions" };
        var stack = new StackPanel { Margin = new Thickness(12) };
        stack.Children.Add(new TextBlock { Text = "Installed device definitions", Foreground = StarbindV5Window.Muted, Margin = new Thickness(0, 0, 0, 8) });
        foreach (var template in _templates)
        {
            stack.Children.Add(new TextBlock { Text = $"{template.DisplayName}  •  {template.Family}  •  {template.Controls.Count} named controls", Foreground = StarbindV5Window.Text, Margin = new Thickness(0, 3, 0, 3) });
        }
        tab.Content = new ScrollViewer { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        return tab;
    }
}

public sealed class DeviceManagerWindow : Window
{
    private readonly IReadOnlyList<StarbindDevice> _devices;
    private readonly HardwareDefinitionService _hardware;
    public StarbindV5Settings Settings { get; }

    public DeviceManagerWindow(IReadOnlyList<StarbindDevice> devices, HardwareDefinitionService hardware, StarbindV5Settings source)
    {
        _devices = devices;
        _hardware = hardware;
        Settings = new StarbindV5Settings
        {
            LastProfile = source.LastProfile,
            LauncherPath = source.LauncherPath,
            ProfileFolders = source.ProfileFolders.ToList(),
            DeviceRoleOverrides = new Dictionary<string, string>(source.DeviceRoleOverrides, StringComparer.OrdinalIgnoreCase),
            ConfirmBeforeWrite = source.ConfirmBeforeWrite
        };
        Title = "Starbind Device Manager";
        Width = 820;
        Height = 610;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = StarbindV5Window.Bg;
        Foreground = StarbindV5Window.Text;
        FontFamily = new FontFamily("Segoe UI");
        Content = Build();
    }

    private UIElement Build()
    {
        var root = new Grid { Margin = new Thickness(14) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var intro = new StackPanel();
        intro.Children.Add(new TextBlock { Text = "CONNECTED HARDWARE", Foreground = StarbindV5Window.Cyan, FontWeight = FontWeights.Bold, FontSize = 14 });
        intro.Children.Add(new TextBlock { Text = "Starbind identifies devices from the profile product string, then applies a matching named-control definition. Change the definition only when a device was classified incorrectly.", Foreground = StarbindV5Window.Muted, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 7, 0, 10) });
        root.Children.Add(intro);
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var rows = new StackPanel();
        foreach (var device in _devices)
        {
            var card = new Border { Background = StarbindV5Window.Panel, BorderBrush = StarbindV5Window.Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5), Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 7) };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(260) });
            var words = new StackPanel();
            words.Children.Add(new TextBlock { Text = device.ProductName, Foreground = StarbindV5Window.Text, FontWeight = FontWeights.Bold });
            words.Children.Add(new TextBlock { Text = $"{device.SlotLabel}  •  {device.Buttons} buttons  •  {device.Axes} axes  •  {(device.IsConnected ? "Connected" : "Profile only")}", Foreground = device.IsConnected ? StarbindV5Window.Green : StarbindV5Window.Amber, FontSize = 10 });
            grid.Children.Add(words);
            var picker = new ComboBox { ItemsSource = _hardware.Templates, DisplayMemberPath = nameof(HardwareTemplate.DisplayName), Background = StarbindV5Window.Field, Foreground = StarbindV5Window.Text, BorderBrush = StarbindV5Window.Border, Padding = new Thickness(6, 4, 6, 4), Tag = device };
            picker.SelectedItem = _hardware.Resolve(device, Settings);
            picker.SelectionChanged += (_, _) =>
            {
                if (picker.SelectedItem is HardwareTemplate template) Settings.DeviceRoleOverrides[HardwareDefinitionService.DeviceKey(device)] = template.Id;
            };
            Grid.SetColumn(picker, 1);
            grid.Children.Add(picker);
            card.Child = grid;
            rows.Children.Add(card);
        }
        scroll.Content = rows;
        Grid.SetRow(scroll, 1);
        root.Children.Add(scroll);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
        var cancel = StarbindV5Window.DialogButton("CANCEL", StarbindV5Window.Panel2);
        cancel.Click += (_, _) => { DialogResult = false; Close(); };
        buttons.Children.Add(cancel);
        var save = StarbindV5Window.DialogButton("SAVE DEVICE DEFINITIONS", StarbindV5Window.BlueDim);
        save.Click += (_, _) => { DialogResult = true; Close(); };
        buttons.Children.Add(save);
        Grid.SetRow(buttons, 2);
        root.Children.Add(buttons);
        return root;
    }
}

public sealed class StarbindHelpWindow : Window
{
    public StarbindHelpWindow()
    {
        Title = "Starbind Help";
        Width = 760;
        Height = 640;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = StarbindV5Window.Bg;
        Foreground = StarbindV5Window.Text;
        FontFamily = new FontFamily("Segoe UI");
        Content = Build();
    }

    private UIElement Build()
    {
        var stack = new StackPanel { Margin = new Thickness(16) };
        stack.Children.Add(new TextBlock { Text = "STARBIND QUICK GUIDE", Foreground = StarbindV5Window.Cyan, FontWeight = FontWeights.Bold, FontSize = 16 });
        stack.Children.Add(Section("1. Select a physical device", "Device cards use the product names stored in your Star Citizen profile. Choose a card, a named control in the tree, a labeled hotspot, or Listen for Input."));
        stack.Children.Add(Section("2. Choose the control's intent", "Pick a primary action. Starbind suggests actions with the same intent and the same behavior type in other game states. Axis, Toggle, Cycle, Hold and Direct actions remain separate."));
        stack.Children.Add(Section("3. Review game states", "Use Edit State Bindings to keep all suggested assignments or choose exactly which states should retain the control."));
        stack.Children.Add(Section("4. Resolve conflicts", "Conflicts & Warnings shows duplicated and unrelated assignments. Resolve All lets you choose one action to keep for every duplicate input group."));
        stack.Children.Add(Section("5. Save safely", "Validate, back up, then save. Every write creates a timestamped backup and validates the new XML before it replaces the original."));
        stack.Children.Add(Section("Axis tuning", "Deadzone and response curve are written to the profile's deviceoptions entry. Test Device shows live input before you commit changes."));
        stack.Children.Add(new TextBlock { Text = "Starbind edits exported layout_*_exported.xml profiles. Keep the game's own export as your source profile and retain the Starbind Backups folder.", Foreground = StarbindV5Window.Amber, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 18, 0, 0) });
        var close = StarbindV5Window.DialogButton("CLOSE", StarbindV5Window.Panel2);
        close.HorizontalAlignment = HorizontalAlignment.Right;
        close.Margin = new Thickness(0, 18, 0, 0);
        close.Click += (_, _) => Close();
        stack.Children.Add(close);
        return new ScrollViewer { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    }

    private static UIElement Section(string title, string body)
    {
        var stack = new StackPanel { Margin = new Thickness(0, 14, 0, 0) };
        stack.Children.Add(new TextBlock { Text = title, Foreground = StarbindV5Window.Text, FontWeight = FontWeights.Bold });
        stack.Children.Add(new TextBlock { Text = body, Foreground = StarbindV5Window.Muted, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0) });
        return stack;
    }
}

public sealed class DeviceTestWindow : Window
{
    private readonly StarbindDevice _device;
    private readonly JoystickService _joysticks;
    private readonly HardwareDefinitionService _hardware;
    private readonly StarbindProfile? _profile;
    private readonly StarbindV5Settings _settings;
    private readonly DispatcherTimer _timer;
    private readonly ObservableCollection<DeviceActivityRow> _rows = [];
    private readonly ListBox _log = new();
    private JoystickSnapshot? _baseline;

    public DeviceTestWindow(StarbindDevice device, JoystickService joysticks, HardwareDefinitionService hardware, StarbindProfile? profile, StarbindV5Settings settings)
    {
        _device = device;
        _joysticks = joysticks;
        _hardware = hardware;
        _profile = profile;
        _settings = settings;
        Title = $"Test Device - {device.ProductName}";
        Width = 780;
        Height = 560;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = StarbindV5Window.Bg;
        Foreground = StarbindV5Window.Text;
        FontFamily = new FontFamily("Consolas");
        Content = Build();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
        _timer.Tick += Tick;
        Loaded += (_, _) => { _baseline = _joysticks.GetSnapshot(_device.Instance); _timer.Start(); };
        Closed += (_, _) => _timer.Stop();
    }

    private UIElement Build()
    {
        var root = new Grid { Margin = new Thickness(14) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.Children.Add(new TextBlock { Text = $"LIVE DEVICE TEST  •  {_device.ProductName}  •  {_device.SlotLabel}", Foreground = StarbindV5Window.Cyan, FontWeight = FontWeights.Bold, FontFamily = new FontFamily("Segoe UI") });
        _log.ItemsSource = _rows;
        _log.DisplayMemberPath = nameof(DeviceActivityRow.Display);
        _log.Background = StarbindV5Window.Field;
        _log.Foreground = StarbindV5Window.Text;
        _log.BorderBrush = StarbindV5Window.Border;
        _log.Margin = new Thickness(0, 10, 0, 0);
        Grid.SetRow(_log, 1);
        root.Children.Add(_log);
        var footer = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
        var clear = StarbindV5Window.DialogButton("CLEAR", StarbindV5Window.Panel2);
        clear.Click += (_, _) => _rows.Clear();
        footer.Children.Add(clear);
        var close = StarbindV5Window.DialogButton("CLOSE", StarbindV5Window.Panel2);
        close.Click += (_, _) => Close();
        footer.Children.Add(close);
        Grid.SetRow(footer, 2);
        root.Children.Add(footer);
        return root;
    }

    private void Tick(object? sender, EventArgs e)
    {
        if (_device.Kind != StarbindDeviceKind.Joystick || _baseline is null) return;
        var activity = _joysticks.DetectActivity(_device.Instance, _baseline);
        var current = _joysticks.GetSnapshot(_device.Instance);
        if (activity is null || current is null) return;
        _baseline = current;
        var control = _hardware.BuildControls(_device, _profile, _settings).FirstOrDefault(item => item.Input.Equals(activity.Input, StringComparison.OrdinalIgnoreCase));
        _rows.Insert(0, new DeviceActivityRow(DateTime.Now, control?.DisplayName ?? activity.Description, activity.Input, "ACTIVE"));
        while (_rows.Count > 100) _rows.RemoveAt(_rows.Count - 1);
    }
}

public sealed class Device3DWindow : Window
{
    private readonly AxisAngleRotation3D _rotation = new(new Vector3D(0, 1, 0), 0);

    public Device3DWindow(StarbindDevice device, HardwareTemplate template)
    {
        Title = $"3D Device View - {device.ProductName}";
        Width = 900;
        Height = 680;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = StarbindV5Window.Bg;
        Foreground = StarbindV5Window.Text;
        FontFamily = new FontFamily("Segoe UI");
        Content = Build(device, template);
    }

    private UIElement Build(StarbindDevice device, HardwareTemplate template)
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(48) });
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(54) });
        root.Children.Add(new TextBlock { Text = $"{device.ProductName}  •  drag the slider to rotate", Foreground = StarbindV5Window.Cyan, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(14, 0, 0, 0) });
        var viewport = BuildViewport(template);
        Grid.SetRow(viewport, 1);
        root.Children.Add(viewport);
        var controls = new Grid { Margin = new Thickness(14, 8, 14, 8) };
        controls.ColumnDefinitions.Add(new ColumnDefinition());
        controls.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var slider = new Slider { Minimum = -70, Maximum = 70, Value = 0, TickFrequency = 10, IsSnapToTickEnabled = false, VerticalAlignment = VerticalAlignment.Center };
        slider.ValueChanged += (_, _) => _rotation.Angle = slider.Value;
        controls.Children.Add(slider);
        var close = StarbindV5Window.DialogButton("CLOSE", StarbindV5Window.Panel2);
        close.Click += (_, _) => Close();
        Grid.SetColumn(close, 1);
        controls.Children.Add(close);
        Grid.SetRow(controls, 2);
        root.Children.Add(controls);
        return root;
    }

    private Viewport3D BuildViewport(HardwareTemplate template)
    {
        var viewport = new Viewport3D { ClipToBounds = true };
        viewport.Camera = new PerspectiveCamera(new Point3D(0, 0, 4.4), new Vector3D(0, 0, -1), new Vector3D(0, 1, 0), 45);
        var group = new Model3DGroup();
        group.Children.Add(new AmbientLight(Color.FromRgb(130, 145, 165)));
        group.Children.Add(new DirectionalLight(Colors.White, new Vector3D(-1, -1, -2)));
        var material = new DiffuseMaterial(new SolidColorBrush(template.Family switch
        {
            HardwareFamily.Throttle => Color.FromRgb(48, 58, 70),
            HardwareFamily.Pedals => Color.FromRgb(55, 62, 70),
            _ => Color.FromRgb(42, 48, 56)
        }));
        var model = new GeometryModel3D(BuildBoxMesh(template.Family), material) { BackMaterial = material, Transform = new RotateTransform3D(_rotation) };
        group.Children.Add(model);
        viewport.Children.Add(new ModelVisual3D { Content = group });
        return viewport;
    }

    private static MeshGeometry3D BuildBoxMesh(HardwareFamily family)
    {
        var size = family switch { HardwareFamily.Pedals => new Vector3D(2.8, .8, 1.2), HardwareFamily.Throttle => new Vector3D(1.8, 2.5, 1.2), _ => new Vector3D(1.5, 3.0, 1.2) };
        var x = size.X / 2; var y = size.Y / 2; var z = size.Z / 2;
        var positions = new Point3DCollection
        {
            new(-x,-y,-z), new(x,-y,-z), new(x,y,-z), new(-x,y,-z),
            new(-x,-y,z), new(x,-y,z), new(x,y,z), new(-x,y,z)
        };
        var triangles = new Int32Collection
        {
            0,1,2, 0,2,3, 4,6,5, 4,7,6,
            0,4,5, 0,5,1, 3,2,6, 3,6,7,
            1,5,6, 1,6,2, 0,3,7, 0,7,4
        };
        return new MeshGeometry3D { Positions = positions, TriangleIndices = triangles };
    }
}
