using Microsoft.Win32;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ShapeEllipse = System.Windows.Shapes.Ellipse;
using ShapeLine = System.Windows.Shapes.Line;
using ShapePolyline = System.Windows.Shapes.Polyline;

namespace StarConfig;

public sealed partial class StarbindWindow
{
    private void Initialize()
    {
        _detectedDevices = _joysticks.GetConnectedDevices();
        var preferred = _preferences.LastProfile;
        var profiles = _profiles.FindProfiles().ToList();
        if (!string.IsNullOrWhiteSpace(preferred) && File.Exists(preferred) && !profiles.Contains(preferred, StringComparer.OrdinalIgnoreCase)) profiles.Insert(0, preferred);
        PopulateProfilePicker(profiles, preferred);
        if (_profile is null)
        {
            BuildDeviceCards();
            SetStatus("Open an exported Star Citizen layout XML profile to begin.");
        }
    }

    private void PopulateProfilePicker(IReadOnlyList<string> profiles, string? preferred)
    {
        _suppressSelectionEvents = true;
        try
        {
            var items = profiles.Select(path => new ProfileChoice(path, Path.GetFileName(path))).ToList();
            _profilePicker.ItemsSource = items;
            _profilePicker.DisplayMemberPath = nameof(ProfileChoice.Name);
            _profilePicker.SelectedItem = items.FirstOrDefault(x => x.Path.Equals(preferred, StringComparison.OrdinalIgnoreCase)) ?? items.FirstOrDefault();
        }
        finally { _suppressSelectionEvents = false; }
        if (_profilePicker.SelectedItem is ProfileChoice choice) LoadProfile(choice.Path);
    }

    private void ProfilePickerChanged()
    {
        if (_suppressSelectionEvents || _profilePicker.SelectedItem is not ProfileChoice choice) return;
        LoadProfile(choice.Path);
    }

    private void LoadProfile(string path)
    {
        try
        {
            StopListening();
            _profile = _profiles.Load(path, _detectedDevices);
            _preferences.LastProfile = path;
            _preferences.Save();
            _channelText.Text = _profile.Channel;
            _channelText.Foreground = Green;
            BuildDeviceCards();
            BuildActionBrowser();
            SelectBestInitialDeviceAndControl();
            SetStatus($"Loaded {_profile.ProfileName}: {_profile.Actions.Count:N0} binding slots across {_profile.Actions.Select(x => x.Context).Distinct().Count()} game states.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Profile load failed", MessageBoxButton.OK, MessageBoxImage.Error);
            SetStatus("Profile load failed.");
        }
    }

    private void BuildDeviceCards()
    {
        _deviceCards.Children.Clear();
        var devices = _profile?.Devices ?? BuildFallbackDevices();
        foreach (var device in devices)
        {
            var selected = _selectedDevice is not null && _selectedDevice.Kind == device.Kind && _selectedDevice.Instance == device.Instance;
            var card = new Button
            {
                Width = device.Kind is StarbindDeviceKind.Keyboard or StarbindDeviceKind.Mouse ? 175 : 225,
                Height = 62,
                Margin = new Thickness(0, 0, 8, 0),
                Padding = new Thickness(8),
                Background = selected ? BlueDim : Panel2,
                BorderBrush = selected ? Blue : Border,
                Foreground = Text,
                Cursor = Cursors.Hand,
                Tag = device,
                HorizontalContentAlignment = HorizontalAlignment.Stretch
            };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(52) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            var icon = BuildDeviceThumbnail(device);
            grid.Children.Add(icon);
            var words = new StackPanel { Margin = new Thickness(7, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            words.Children.Add(new TextBlock { Text = device.ProductName, FontSize = 11, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
            words.Children.Add(new TextBlock { Text = device.SlotLabel, Foreground = Muted, FontSize = 10 });
            Grid.SetColumn(words, 1);
            grid.Children.Add(words);
            var dot = new ShapeEllipse { Width = 8, Height = 8, Fill = device.IsConnected ? Green : Amber, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(dot, 2);
            grid.Children.Add(dot);
            card.Content = grid;
            card.Click += (_, _) => SelectDevice(device);
            _deviceCards.Children.Add(card);
        }
        var add = new Button { Content = "+\nAdd Profile", Width = 120, Height = 62, Background = Panel2, Foreground = Muted, BorderBrush = Border, Cursor = Cursors.Hand };
        add.Click += BrowseProfile;
        _deviceCards.Children.Add(add);
        _deviceCountText.Text = $"{devices.Count(x => x.IsConnected)} CONNECTED";
    }

    private UIElement BuildDeviceThumbnail(StarbindDevice device)
    {
        if (device.Kind == StarbindDeviceKind.Joystick)
        {
            return new Border
            {
                Width = 46,
                Height = 46,
                Background = Field,
                BorderBrush = Border,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Child = new Image { Source = StarbindArtwork.LoadJoystick(), Stretch = Stretch.Uniform, Margin = new Thickness(2) }
            };
        }
        var glyph = device.Kind switch { StarbindDeviceKind.Keyboard => "KB", StarbindDeviceKind.Mouse => "M", StarbindDeviceKind.Gamepad => "GP", _ => "?" };
        return new Border { Width = 42, Height = 42, Background = Field, BorderBrush = Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5), Child = new TextBlock { Text = glyph, Foreground = Cyan, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } };
    }

    private IReadOnlyList<StarbindDevice> BuildFallbackDevices()
    {
        var list = new List<StarbindDevice>
        {
            new(1, "Keyboard", StarbindDeviceKind.Keyboard, 0, 0, true),
            new(1, "Mouse", StarbindDeviceKind.Mouse, 5, 2, true)
        };
        list.AddRange(_detectedDevices.Select(x => new StarbindDevice(x.Id, x.Name, StarbindDeviceKind.Joystick, x.Buttons, x.Axes, true)));
        return list;
    }

    private void SelectBestInitialDeviceAndControl()
    {
        if (_profile is null) return;
        var device = _profile.Devices.FirstOrDefault(x => x.Kind == StarbindDeviceKind.Joystick && x.IsConnected)
            ?? _profile.Devices.FirstOrDefault(x => x.Kind == StarbindDeviceKind.Joystick)
            ?? _profile.Devices.First();
        SelectDevice(device);
        var prefix = device.InputPrefix;
        var bound = _profile.Actions.FirstOrDefault(x => x.IsBound && x.Input.StartsWith(prefix + "_", StringComparison.OrdinalIgnoreCase) && StarbindInput.KindOf(x.Input) == StarbindControlKind.Axis)
            ?? _profile.Actions.FirstOrDefault(x => x.IsBound && x.Input.StartsWith(prefix + "_", StringComparison.OrdinalIgnoreCase));
        var control = bound is not null ? ControlFromInput(bound.Input) : BuildControls(device).FirstOrDefault();
        if (control is not null) SelectControl(control);
    }

    private void SelectDevice(StarbindDevice device)
    {
        _selectedDevice = device;
        BuildDeviceCards();
        BuildControlTree();
        _deviceTitle.Text = $"{device.ProductName.ToUpperInvariant()} ({device.SlotLabel})";
        _deviceSubtitle.Text = device.Kind == StarbindDeviceKind.Joystick ? $"{device.Buttons} buttons • {device.Axes} axes • profile instance {device.Instance}" : device.SlotLabel;
        BuildDeviceVisual();
        var firstControl = BuildControls(device).FirstOrDefault(control => _profile?.Actions.Any(action => action.Input.Equals(control.Input, StringComparison.OrdinalIgnoreCase)) == true)
            ?? BuildControls(device).FirstOrDefault();
        if (firstControl is not null) SelectControl(firstControl);
    }

}
