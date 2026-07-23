using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ShapeEllipse = System.Windows.Shapes.Ellipse;
using ShapeLine = System.Windows.Shapes.Line;
using ShapeRectangle = System.Windows.Shapes.Rectangle;

namespace StarConfig;

public sealed partial class CockpitWindow
{
    private void InitializeApplication()
    {
        foreach (var folder in _settings.LoadFolders())
            if (Directory.Exists(folder)) _mappingFolders.Add(folder);
        var lastProfile = _settings.LoadLastProfile();
        if (!string.IsNullOrWhiteSpace(lastProfile) && File.Exists(lastProfile))
            _mappingFolders.Add(System.IO.Path.GetDirectoryName(lastProfile)!);
        RefreshEverything(lastProfile);
    }

    private void RefreshEverything(string? preferredProfile = null)
    {
        StopCapture();
        _devices = _joysticks.GetConnectedDevices().ToList();
        foreach (var folder in _profiles.FindMappingsFolders()) _mappingFolders.Add(folder);
        foreach (var folder in DiscoverAdditionalMappingFolders()) _mappingFolders.Add(folder);
        BuildDeviceStripCards();
        if (_selectedDevice is null && _devices.Count > 0)
            SelectDeviceCard(new DeviceCardInfo($"JOY{_devices[0].Id}", _devices[0].Name, $"{_devices[0].Buttons} buttons • {_devices[0].Axes} axes", "J", _devices[0]));
        else
        {
            BuildControlTree();
            BuildDeviceVisual();
        }
        LoadProfilePicker(preferredProfile ?? _activeProfile ?? _settings.LoadLastProfile());
        SetStatus($"Detected {_devices.Count} joystick-class device(s), plus keyboard and mouse.");
    }

    private IEnumerable<string> DiscoverAdditionalMappingFolders()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
        {
            var roots = new[]
            {
                System.IO.Path.Combine(drive.RootDirectory.FullName, "Program Files", "Roberts Space Industries", "StarCitizen"),
                System.IO.Path.Combine(drive.RootDirectory.FullName, "Roberts Space Industries", "StarCitizen"),
                System.IO.Path.Combine(drive.RootDirectory.FullName, "Games", "Roberts Space Industries", "StarCitizen"),
                System.IO.Path.Combine(drive.RootDirectory.FullName, "RSI", "StarCitizen")
            };
            foreach (var root in roots.Where(Directory.Exists))
            {
                IEnumerable<string> channels;
                try { channels = Directory.EnumerateDirectories(root); }
                catch { continue; }
                foreach (var channel in channels)
                {
                    var mappings = System.IO.Path.Combine(channel, "USER", "Client", "0", "Controls", "Mappings");
                    if (Directory.Exists(mappings) && seen.Add(mappings)) yield return mappings;
                    var legacyMappings = System.IO.Path.Combine(channel, "USER", "Controls", "Mappings");
                    if (Directory.Exists(legacyMappings) && seen.Add(legacyMappings)) yield return legacyMappings;
                }
            }
        }
    }

    private void BuildDeviceStripCards()
    {
        _deviceStrip.Children.Clear();
        AddDeviceCard(new DeviceCardInfo("KEYBOARD", "Keyboard", "Keys and modifiers", "KB", null));
        AddDeviceCard(new DeviceCardInfo("MOUSE", "Mouse", "Buttons and wheel", "M", null));
        foreach (var device in _devices)
            AddDeviceCard(new DeviceCardInfo($"JOY{device.Id}", device.Name, $"{device.Buttons} buttons • {device.Axes} axes", "J", device));
        var add = new Button { Content = "+\nADD PROFILE", Width = 132, Height = 64, Background = PanelAlt, Foreground = Muted, BorderBrush = Border, Margin = new Thickness(0, 0, 8, 0), Cursor = Cursors.Hand };
        add.Click += BrowseForProfile;
        _deviceStrip.Children.Add(add);
        _deviceCountText.Text = $"{_devices.Count + 2} CONNECTED";
    }

    private void AddDeviceCard(DeviceCardInfo info)
    {
        var button = new Button
        {
            Width = 208,
            Height = 64,
            Margin = new Thickness(0, 0, 8, 0),
            Padding = new Thickness(10),
            Background = info.Title.Equals(_selectedDeviceCardId, StringComparison.OrdinalIgnoreCase) ? BlueDim : PanelAlt,
            BorderBrush = info.Title.Equals(_selectedDeviceCardId, StringComparison.OrdinalIgnoreCase) ? Blue : Border,
            Foreground = Text,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Cursor = Cursors.Hand,
            Tag = info
        };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        var icon = new Border { Width = 34, Height = 34, CornerRadius = new CornerRadius(6), Background = FieldBg, BorderBrush = BorderStrong, BorderThickness = new Thickness(1), Child = new TextBlock { Text = info.Icon, Foreground = Cyan, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } };
        grid.Children.Add(icon);
        var words = new StackPanel { Margin = new Thickness(7, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        words.Children.Add(new TextBlock { Text = $"{info.Title}  {info.Name}", FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
        words.Children.Add(new TextBlock { Text = info.Detail, Foreground = Muted, FontSize = 10, TextTrimming = TextTrimming.CharacterEllipsis });
        Grid.SetColumn(words, 1);
        grid.Children.Add(words);
        var online = new ShapeEllipse { Width = 8, Height = 8, Fill = Green, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(online, 2);
        grid.Children.Add(online);
        button.Content = grid;
        button.Click += (_, _) => SelectDeviceCard(info);
        _deviceStrip.Children.Add(button);
    }

    private void SelectDeviceCard(DeviceCardInfo info)
    {
        _selectedDevice = info.Device;
        _selectedDeviceCardId = info.Title;
        BuildDeviceStripCards();
        BuildControlTree();
        BuildDeviceVisual();
        if (info.Device is null)
        {
            _deviceHeading.Text = info.Title;
            _deviceSubheading.Text = info.Detail;
        }
        else
        {
            _deviceHeading.Text = $"{info.Title}  {info.Device.Name}";
            _deviceSubheading.Text = info.Detail;
        }
    }

    private void BuildControlTree()
    {
        _controlTree.Items.Clear();
        if (_selectedDevice is null)
        {
            var keyboard = TreeGroup("KEYBOARD / MOUSE");
            foreach (var control in new[]
            {
                new PhysicalControl("kb1_space", "Space", "Key", "Keyboard"),
                new PhysicalControl("kb1_lctrl", "Left Ctrl", "Modifier", "Keyboard"),
                new PhysicalControl("kb1_lshift", "Left Shift", "Modifier", "Keyboard"),
                new PhysicalControl("mouse1_button1", "Mouse Button 1", "Button", "Mouse"),
                new PhysicalControl("mouse1_button2", "Mouse Button 2", "Button", "Mouse"),
                new PhysicalControl("mouse1_button3", "Mouse Button 3", "Button", "Mouse")
            }) keyboard.Items.Add(TreeControl(control));
            keyboard.IsExpanded = true;
            _controlTree.Items.Add(keyboard);
            return;
        }
        var axes = TreeGroup("AXES");
        var axisNames = new[] { ("x", "X Axis"), ("y", "Y Axis"), ("z", "Z Axis"), ("rotx", "RX Axis"), ("roty", "RY Axis"), ("rotz", "RZ Axis"), ("slider1", "Slider 1"), ("slider2", "Slider 2") };
        foreach (var (code, label) in axisNames.Take(Math.Max(2, Math.Min(axisNames.Length, _selectedDevice.Axes))))
            axes.Items.Add(TreeControl(new PhysicalControl($"js{_selectedDevice.Id}_{code}", label, "Axis", "Axes")));
        axes.IsExpanded = true;
        _controlTree.Items.Add(axes);
        var buttons = TreeGroup("BUTTONS");
        for (var i = 1; i <= _selectedDevice.Buttons; i++)
            buttons.Items.Add(TreeControl(new PhysicalControl($"js{_selectedDevice.Id}_button{i}", $"Button {i}", "Button", "Buttons")));
        buttons.IsExpanded = true;
        _controlTree.Items.Add(buttons);
        var hats = TreeGroup("HATS");
        foreach (var direction in new[] { "up", "right", "down", "left" })
            hats.Items.Add(TreeControl(new PhysicalControl($"js{_selectedDevice.Id}_hat1_{direction}", $"Hat 1 {TitleCase(direction)}", "Hat", "Hats")));
        _controlTree.Items.Add(hats);
    }

    private void BuildDeviceVisual()
    {
        _deviceVisualHost.Children.Clear();
        if (_selectedDevice is null)
        {
            var keyboardView = new Grid();
            keyboardView.Children.Add(new TextBlock { Text = "KEYBOARD + MOUSE\n\nUse LISTEN FOR INPUT to capture any key or mouse button, or pick a common control from the tree.", TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, Foreground = Muted, FontSize = 16, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, MaxWidth = 520 });
            _deviceVisualHost.Children.Add(keyboardView);
            return;
        }
        var viewbox = new Viewbox { Stretch = Stretch.Uniform, Margin = new Thickness(14) };
        var canvas = new Canvas { Width = 900, Height = 500 };
        viewbox.Child = canvas;
        _deviceVisualHost.Children.Add(viewbox);
        var leftLabels = new[] { ("Trigger", 1, 36d, 70d), ("Weapon 2", 2, 36d, 130d), ("Hat Up", 7, 36d, 205d), ("Pinky", 5, 36d, 280d) };
        var rightLabels = new[] { ("Button 3", 3, 718d, 72d), ("Button 4", 4, 718d, 132d), ("Hat Right", 8, 718d, 205d), ("Button 6", 6, 718d, 282d) };
        var baseBody = new Border { Width = 270, Height = 112, CornerRadius = new CornerRadius(26), Background = PanelAlt, BorderBrush = BorderStrong, BorderThickness = new Thickness(2) };
        Canvas.SetLeft(baseBody, 315); Canvas.SetTop(baseBody, 360); canvas.Children.Add(baseBody);
        var stem = new ShapeRectangle { Width = 72, Height = 175, RadiusX = 28, RadiusY = 28, Fill = PanelAlt, Stroke = BorderStrong, StrokeThickness = 2 };
        Canvas.SetLeft(stem, 414); Canvas.SetTop(stem, 198); canvas.Children.Add(stem);
        var head = new Border { Width = 238, Height = 190, CornerRadius = new CornerRadius(70, 70, 48, 48), Background = PanelAlt, BorderBrush = BorderStrong, BorderThickness = new Thickness(2) };
        Canvas.SetLeft(head, 331); Canvas.SetTop(head, 48); canvas.Children.Add(head);
        var stickRing = new ShapeEllipse { Width = 96, Height = 96, Fill = FieldBg, Stroke = Cyan, StrokeThickness = 3 };
        Canvas.SetLeft(stickRing, 402); Canvas.SetTop(stickRing, 88); canvas.Children.Add(stickRing);
        var stickHub = new ShapeEllipse { Width = 30, Height = 30, Fill = Cyan };
        Canvas.SetLeft(stickHub, 435); Canvas.SetTop(stickHub, 121); canvas.Children.Add(stickHub);
        foreach (var item in leftLabels) AddHotspotLabel(canvas, item.Item1, item.Item2, item.Item3, item.Item4, true);
        foreach (var item in rightLabels) AddHotspotLabel(canvas, item.Item1, item.Item2, item.Item3, item.Item4, false);
        var axisHotspot = HotspotButton("Z AXIS\nFORWARD / BACK", $"js{_selectedDevice.Id}_z", "Z Axis", 150, 52);
        Canvas.SetLeft(axisHotspot, 612); Canvas.SetTop(axisHotspot, 392); canvas.Children.Add(axisHotspot);
        var caption = new TextBlock { Text = "CLICK A LABELED HOTSPOT OR USE THE FULL CONTROL TREE", Foreground = Faint, FontSize = 11, FontWeight = FontWeights.SemiBold };
        Canvas.SetLeft(caption, 318); Canvas.SetTop(caption, 478); canvas.Children.Add(caption);
    }

    private void AddHotspotLabel(Canvas canvas, string label, int buttonNumber, double left, double top, bool pointsRight)
    {
        if (_selectedDevice is null || buttonNumber > _selectedDevice.Buttons) return;
        var input = $"js{_selectedDevice.Id}_button{buttonNumber}";
        var button = HotspotButton(label, input, $"Button {buttonNumber}", 142, 38);
        Canvas.SetLeft(button, left); Canvas.SetTop(button, top); canvas.Children.Add(button);
        var line = new ShapeLine { Stroke = BorderStrong, StrokeThickness = 1.4, X1 = pointsRight ? left + 142 : left, Y1 = top + 19, X2 = pointsRight ? 350 : 550, Y2 = top + 35 };
        canvas.Children.Add(line);
    }

    private Button HotspotButton(string label, string input, string name, double width, double height)
    {
        var selected = string.Equals(_selectedControl?.Input, input, StringComparison.OrdinalIgnoreCase);
        var button = new Button { Content = label, Width = width, Height = height, Background = selected ? BlueDim : PanelAlt, BorderBrush = selected ? Blue : BorderStrong, Foreground = Text, FontSize = 11, Cursor = Cursors.Hand, Tag = input };
        button.Click += (_, _) => SelectPhysicalControl(new PhysicalControl(input, name, input.Contains("_button", StringComparison.OrdinalIgnoreCase) ? "Button" : "Axis", "Diagram"));
        return button;
    }

    private void ControlTreeSelectionChanged()
    {
        if (_controlTree.SelectedItem is TreeViewItem item && item.Tag is PhysicalControl control) SelectPhysicalControl(control);
    }

    private void SelectPhysicalControl(PhysicalControl control)
    {
        _selectedControl = control;
        _selectedControlName.Text = control.Name;
        _selectedControlInput.Text = control.Input;
        _selectedControlType.Text = control.Type;
        _responseFill.Width = control.Type == "Axis" ? 115 : 34;
        RebuildStateAssignments();
        RebuildWarnings();
        RebuildStateOverview();
        BuildDeviceVisual();
        _applyButton.IsEnabled = _activeProfile is not null;
        _saveLaunchButton.IsEnabled = _activeProfile is not null;
        SetStatus($"Selected {control.Name}. Review its state assignments, choose actions, then apply.");
    }
}
