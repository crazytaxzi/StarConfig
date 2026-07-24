using Microsoft.Win32;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace StarConfig;

public sealed partial class StarbindV5Window
{
    private void InitializeApplication()
    {
        _detectedDevices = _joysticks.GetConnectedDevices();
        var profiles = _settingsStore.DiscoverProfiles(_settings);
        PopulateProfilePicker(profiles, _settings.LastProfile);
        if (_profile is null)
        {
            BuildDeviceCards();
            BuildDevicePicker();
            SetStatus("Open an exported Star Citizen layout XML profile to begin.");
        }
        _settingsStore.Save(_settings);
    }

    private void PopulateProfilePicker(IReadOnlyList<string> profiles, string? preferred)
    {
        _suppressUi = true;
        try
        {
            var items = profiles.Select(path => new ProfileListItem(path, Path.GetFileNameWithoutExtension(path), StarbindV5SettingsStore.DetectChannel(path), File.GetLastWriteTime(path))).ToList();
            _profilePicker.ItemsSource = items;
            _profilePicker.DisplayMemberPath = nameof(ProfileListItem.Name);
            _profilePicker.SelectedItem = items.FirstOrDefault(item => item.FilePath.Equals(preferred, StringComparison.OrdinalIgnoreCase)) ?? items.FirstOrDefault();
        }
        finally { _suppressUi = false; }
        if (_profilePicker.SelectedItem is ProfileListItem selected) LoadProfile(selected.FilePath);
    }

    private void ProfilePickerChanged()
    {
        if (_suppressUi || _profilePicker.SelectedItem is not ProfileListItem selected) return;
        LoadProfile(selected.FilePath);
    }

    private void LoadProfile(string path)
    {
        try
        {
            StopListening();
            _detectedDevices = _joysticks.GetConnectedDevices();
            _profile = _profiles.Load(path, _detectedDevices);
            _settings.LastProfile = path;
            var folder = Path.GetDirectoryName(path)!;
            if (!_settings.ProfileFolders.Contains(folder, StringComparer.OrdinalIgnoreCase)) _settings.ProfileFolders.Add(folder);
            _settingsStore.Save(_settings);
            _channelText.Text = _profile.Channel;
            _channelText.Foreground = Green;
            _pendingPlans.Clear();
            _axisTunings.Clear();
            BuildDeviceCards();
            BuildDevicePicker();
            BuildActionBrowser();
            SelectInitialDeviceAndControl();
            SetStatus($"Loaded {_profile.ProfileName}: {_profile.Actions.Count:N0} binding slots across {_profile.Actions.Select(action => action.Context).Distinct().Count()} game states.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Profile load failed", MessageBoxButton.OK, MessageBoxImage.Error);
            SetStatus("Profile load failed.");
        }
    }

    private IReadOnlyList<StarbindDevice> CurrentDevices()
    {
        if (_profile is not null && _profile.Devices.Count > 0) return _profile.Devices;
        var devices = new List<StarbindDevice>
        {
            new(1, "Keyboard", StarbindDeviceKind.Keyboard, 0, 0, true),
            new(1, "Mouse", StarbindDeviceKind.Mouse, 8, 3, true)
        };
        devices.AddRange(_detectedDevices.Select(device => new StarbindDevice(device.Id, device.Name, StarbindDeviceKind.Joystick, device.Buttons, device.Axes, true)));
        return devices;
    }

    private void BuildDeviceCards()
    {
        _deviceCards.Children.Clear();
        var devices = CurrentDevices();
        var cardDevices = devices.Where(device => device.Kind is StarbindDeviceKind.Joystick or StarbindDeviceKind.Gamepad).ToList();
        if (cardDevices.Count == 0) cardDevices = devices.ToList();
        foreach (var device in cardDevices)
        {
            var template = _hardware.Resolve(device, _settings);
            var selected = _selectedDevice is not null && SameDevice(_selectedDevice, device);
            var card = new Button
            {
                Width = 226,
                Height = 64,
                Margin = new Thickness(0, 0, 8, 0),
                Padding = new Thickness(8),
                Background = selected ? BlueDim : Panel2,
                BorderBrush = selected ? Cyan : Border,
                BorderThickness = new Thickness(selected ? 2 : 1),
                Foreground = Text,
                Cursor = Cursors.Hand,
                Tag = device,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                ToolTip = device.IsConnected ? "Detected live by Windows." : "Present in the loaded Star Citizen profile but not detected live by Windows."
            };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            grid.Children.Add(BuildDeviceThumbnail(template, device));
            var words = new StackPanel { Margin = new Thickness(7, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            words.Children.Add(new TextBlock { Text = device.ProductName, FontSize = 10.5, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
            words.Children.Add(new TextBlock
            {
                Text = $"{device.SlotLabel}  •  {DeviceRole(template)}  •  {(device.IsConnected ? "LIVE" : "PROFILE ONLY")}",
                Foreground = device.IsConnected ? Muted : Amber,
                FontSize = 9,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            Grid.SetColumn(words, 1);
            grid.Children.Add(words);
            var dot = new ShapeEllipse { Width = 8, Height = 8, Fill = device.IsConnected ? Green : Amber, VerticalAlignment = VerticalAlignment.Center, ToolTip = device.IsConnected ? "Live device" : "Profile-only device" };
            Grid.SetColumn(dot, 2);
            grid.Children.Add(dot);
            card.Content = grid;
            card.Click += (_, _) => SelectDevice(device);
            _deviceCards.Children.Add(card);
        }

        var add = new Button
        {
            Content = "+\nAdd Device",
            Width = 112,
            Height = 64,
            Background = Panel2,
            Foreground = Muted,
            BorderBrush = Border,
            Cursor = Cursors.Hand
        };
        add.Click += OpenDeviceManager;
        _deviceCards.Children.Add(add);
        var physical = devices.Where(device => device.Kind is StarbindDeviceKind.Joystick or StarbindDeviceKind.Gamepad).ToList();
        var live = physical.Count(device => device.IsConnected);
        var profileOnly = physical.Count - live;
        _deviceCountText.Text = profileOnly > 0 ? $"{live} LIVE  •  {profileOnly} PROFILE" : $"{live} CONNECTED";
        _deviceCountText.Foreground = profileOnly > 0 ? Amber : Green;
    }

    private static string DeviceRole(HardwareTemplate template) => template.Family switch
    {
        HardwareFamily.Throttle => "THROTTLE",
        HardwareFamily.Pedals => "PEDALS",
        HardwareFamily.Gamepad => "GAMEPAD",
        _ => "JOYSTICK"
    };

    private UIElement BuildDeviceThumbnail(HardwareTemplate template, StarbindDevice device)
    {
        if (template.ArtworkKey == "joystick")
        {
            var image = new Image { Source = StarbindArtwork.LoadJoystick(), Stretch = Stretch.Uniform, Margin = new Thickness(2) };
            if (device.ProductName.Contains(" EVO L", StringComparison.OrdinalIgnoreCase) || device.ProductName.Contains(" LEFT", StringComparison.OrdinalIgnoreCase))
            {
                image.RenderTransformOrigin = new Point(.5, .5);
                image.RenderTransform = new ScaleTransform(-1, 1);
            }
            return new Border
            {
                Width = 52,
                Height = 52,
                Background = Field,
                BorderBrush = Border,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Child = image
            };
        }
        return DeviceArtworkFactory.BuildThumbnail(template.Family, 52, 52, Field, Cyan, Muted);
    }

    private void BuildDevicePicker()
    {
        _suppressUi = true;
        try
        {
            _devicePicker.ItemsSource = CurrentDevices();
            _devicePicker.DisplayMemberPath = nameof(StarbindDevice.ProductName);
            _devicePicker.SelectedItem = CurrentDevices().FirstOrDefault(device => _selectedDevice is not null && SameDevice(device, _selectedDevice)) ?? CurrentDevices().FirstOrDefault();
        }
        finally { _suppressUi = false; }
    }

    private void DevicePickerChanged()
    {
        if (_suppressUi || _devicePicker.SelectedItem is not StarbindDevice device) return;
        SelectDevice(device);
    }

    private void SelectInitialDeviceAndControl()
    {
        var devices = CurrentDevices();
        var device = devices.FirstOrDefault(item => item.Kind == StarbindDeviceKind.Joystick && item.IsConnected)
            ?? devices.FirstOrDefault(item => item.Kind == StarbindDeviceKind.Joystick)
            ?? devices.FirstOrDefault();
        if (device is null) return;
        SelectDevice(device);
        var controls = _hardware.BuildControls(device, _profile, _settings);
        var bound = controls.FirstOrDefault(control => CurrentAssignments(control).Any()) ?? controls.FirstOrDefault();
        if (bound is not null) SelectControl(bound);
    }

    private void SelectDevice(StarbindDevice device)
    {
        _selectedDevice = device;
        _selectedTemplate = _hardware.Resolve(device, _settings);
        _suppressUi = true;
        try { _devicePicker.SelectedItem = CurrentDevices().FirstOrDefault(item => SameDevice(item, device)); }
        finally { _suppressUi = false; }
        BuildDeviceCards();
        BuildControlTree();
        _deviceTitle.Text = $"{device.ProductName.ToUpperInvariant()} ({device.SlotLabel})";
        _deviceSubtitle.Text = $"{_selectedTemplate.DisplayName}  •  {device.Buttons} buttons  •  {device.Axes} axes  •  {(device.IsConnected ? "LIVE DEVICE" : "PROFILE ONLY")}";
        _deviceSubtitle.Foreground = device.IsConnected ? Muted : Amber;
        BuildDeviceVisual();
        var first = _hardware.BuildControls(device, _profile, _settings).FirstOrDefault(control => CurrentAssignments(control).Any())
            ?? _hardware.BuildControls(device, _profile, _settings).FirstOrDefault();
        if (first is not null) SelectControl(first);
    }

    private void BuildControlTree()
    {
        _controlTree.Items.Clear();
        if (_selectedDevice is null || _selectedTemplate is null) return;
        var controls = FilteredControls();
        var definitions = _selectedTemplate.Controls.ToDictionary(definition => definition.InputSuffix, StringComparer.OrdinalIgnoreCase);
        foreach (var group in controls.GroupBy(control => definitions.TryGetValue(StarbindInput.Split(control.Input).Suffix, out var definition) ? definition.Group : ControlGroup(control.Kind))
                     .OrderBy(group => GroupOrder(group.Key)))
        {
            var parent = TreeGroup(GroupGlyph(group.Key) + "  " + group.Key.ToUpperInvariant());
            foreach (var control in group.OrderBy(control => NaturalOrder(control.Input)))
            {
                var count = EffectiveAssignments(control).Count();
                var header = new DockPanel { LastChildFill = true };
                var badge = new TextBlock
                {
                    Text = count > 0 ? $"{count} bind{(count == 1 ? string.Empty : "s")}" : string.Empty,
                    Foreground = count > 0 ? Green : Faint,
                    FontSize = 8.5,
                    Margin = new Thickness(6, 0, 0, 0),
                    ToolTip = count > 0 ? $"{count} current assignment{(count == 1 ? string.Empty : "s")}" : "Unassigned"
                };
                DockPanel.SetDock(badge, Dock.Right);
                header.Children.Add(badge);
                header.Children.Add(new TextBlock { Text = control.DisplayName, Foreground = Text, TextTrimming = TextTrimming.CharacterEllipsis });
                parent.Items.Add(new TreeViewItem { Header = header, Tag = control, Foreground = Text, Padding = new Thickness(3) });
            }
            parent.IsExpanded = group.Key is "Axes" or "Triggers" or "Buttons";
            _controlTree.Items.Add(parent);
        }
    }

    private IReadOnlyList<StarbindControl> FilteredControls()
    {
        if (_selectedDevice is null) return [];
        var controls = _hardware.BuildControls(_selectedDevice, _profile, _settings);
        if (_showUnassigned.IsChecked != true) return controls;
        return controls.Where(control => !EffectiveAssignments(control).Any()).ToList();
    }

    private void RebuildControlsAndVisual()
    {
        BuildControlTree();
        BuildDeviceVisual();
    }

    private void ControlTreeChanged()
    {
        if (_controlTree.SelectedItem is TreeViewItem item && item.Tag is StarbindControl control) SelectControl(control);
    }

    private void SelectControl(StarbindControl control)
    {
        _selectedControl = control;
        _selectedControlName.Text = control.DisplayName;
        _selectedControlInput.Text = $"Physical: {control.Input}";
        _selectedControlType.Text = control.Kind.ToString();
        _selectedControlAssignmentCount.Text = EffectiveAssignments(control).Count().ToString(CultureInfo.InvariantCulture);
        LoadAxisSettings(control);
        BuildStateRows();
        BuildDeviceVisual();
        BuildWarnings();
        BuildStateOverview();
        DrawResponseGraph();

        var assignments = EffectiveAssignments(control).ToList();
        if (assignments.Count > 0)
        {
            SelectAction(assignments[0]);
            _suppressUi = true;
            _primaryActionPicker.SelectedItem = FindActionChoice(_primaryActionPicker, assignments[0]);
            _suppressUi = false;
        }
        else
        {
            _selectedAction = null;
            _suppressUi = true;
            _primaryActionPicker.SelectedItem = null;
            _suppressUi = false;
            _actionDescription.Text = "This physical control is unassigned. Choose a primary action or select one in the action browser.";
            _similarActions.Children.Clear();
        }
        RefreshMappingAssistant();
        SetStatus($"Selected {control.DisplayName}. Review its state assignments, then save when ready.");
    }

    private IEnumerable<StarbindAction> CurrentAssignments(StarbindControl control)
        => _profile?.Actions.Where(action => action.Input.Equals(control.Input, StringComparison.OrdinalIgnoreCase)) ?? [];

    private IEnumerable<StarbindAction> EffectiveAssignments(StarbindControl control)
    {
        if (!_pendingPlans.TryGetValue(control.Input, out var plan)) return CurrentAssignments(control);
        return plan.States.Values.Where(state => state.Enabled && state.Action is not null).Select(state => state.Action!);
    }

    private static bool SameDevice(StarbindDevice left, StarbindDevice right) => left.Kind == right.Kind && left.Instance == right.Instance && left.ProductName.Equals(right.ProductName, StringComparison.OrdinalIgnoreCase);
    private static string ControlGroup(StarbindControlKind kind) => kind switch
    {
        StarbindControlKind.Axis => "Axes",
        StarbindControlKind.Hat => "Hats",
        StarbindControlKind.Key => "Keys",
        StarbindControlKind.MouseButton => "Buttons",
        _ => "Buttons"
    };
    private static int GroupOrder(string group) => group switch { "Axes" => 0, "Triggers" => 1, "Buttons" => 2, "Hats" => 3, "Encoders" => 4, "System Controls" => 5, "Keys" => 6, _ => 7 };
    private static string GroupGlyph(string group) => group switch { "Axes" => "⌁", "Triggers" => "⌁", "Buttons" => "◉", "Hats" => "✥", "Encoders" => "◌", "System Controls" => "⚙", "Keys" => "⌨", _ => "•" };
    private static int NaturalOrder(string input)
    {
        var suffix = StarbindInput.Split(input).Suffix;
        var digits = new string(suffix.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var value) ? value : suffix.GetHashCode(StringComparison.OrdinalIgnoreCase);
    }
}
