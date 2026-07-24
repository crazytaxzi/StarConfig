using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace StarConfig;

public sealed partial class StarbindV5Window
{
    private UIElement BuildLayout()
    {
        var outer = new Grid();
        outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
        outer.ColumnDefinitions.Add(new ColumnDefinition());
        outer.Children.Add(BuildReferenceRail());
        var app = BuildApplicationShell();
        Grid.SetColumn(app, 1);
        outer.Children.Add(app);
        return outer;
    }

    private UIElement BuildReferenceRail()
    {
        var gradient = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1),
            GradientStops =
            {
                new GradientStop((Color)ColorConverter.ConvertFromString("#07111F"), 0),
                new GradientStop((Color)ColorConverter.ConvertFromString("#081B31"), 0.55),
                new GradientStop((Color)ColorConverter.ConvertFromString("#07121F"), 1)
            }
        };
        var rail = new Border { Background = gradient, BorderBrush = Border2, BorderThickness = new Thickness(0, 0, 1, 0) };
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(72) });
        grid.RowDefinitions.Add(new RowDefinition());
        rail.Child = grid;

        var brand = new Border { BorderBrush = Border, BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(18, 0, 14, 0) };
        var brandGrid = new Grid();
        brandGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
        brandGrid.ColumnDefinitions.Add(new ColumnDefinition());
        brandGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        brandGrid.Children.Add(new TextBlock { Text = "✦", Foreground = Cyan, FontSize = 34, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center });
        var words = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };
        words.Children.Add(new TextBlock { Text = "STARBIND", FontSize = 23, FontWeight = FontWeights.Bold, Foreground = Text });
        words.Children.Add(new TextBlock { Text = "Star Citizen Control Mapper", FontSize = 10, Foreground = Muted });
        Grid.SetColumn(words, 1);
        brandGrid.Children.Add(words);
        var version = new TextBlock { Text = "v0.7.0", Foreground = Faint, FontSize = 9, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 0, 13) };
        Grid.SetColumn(version, 2);
        brandGrid.Children.Add(version);
        brand.Child = brandGrid;
        grid.Children.Add(brand);

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        var stack = new StackPanel { Margin = new Thickness(16, 12, 14, 18) };
        stack.Children.Add(RailHeading("SCOPE OVERVIEW"));
        foreach (var item in new[]
        {
            "Detects connected controls and separates live devices from profile-only devices",
            "Loads exported Star Citizen control profiles and remembers the active profile",
            "Explains actions in plain language instead of exposing XML vocabulary",
            "Maps one physical control across Flight, Vehicle, On Foot, EVA, Turret, Mining and Salvage",
            "Detects duplicate, conflicting and semantically similar actions",
            "Suggests compatible cross-state bindings and asks before changing them",
            "Shows physical controls, live response, deadzones and response curves",
            "Validates, backs up, saves profiles and launches Star Citizen"
        }) stack.Children.Add(RailBullet(item));

        stack.Children.Add(RailHeading("KEY FEATURES", new Thickness(0, 18, 0, 8)));
        stack.Children.Add(RailFeature("◈", "Automatic Device Detection", "Shows live hardware, profile-only hardware, model, role and connection state."));
        stack.Children.Add(RailFeature("◎", "Action Intelligence", "Documents actions, intent, behavior and important differences such as Operator Mode versus Master Mode."));
        stack.Children.Add(RailFeature("⌁", "Smart Mapping Assistant", "Suggests matching actions across game states without silently merging them."));
        stack.Children.Add(RailFeature("◇", "State-Aware Binding", "Edits Flight, Vehicle, On Foot, EVA, Turret, Mining and Salvage assignments together."));
        stack.Children.Add(RailFeature("!", "Conflict Detection", "Finds duplicate inputs, unrelated intents, missing actions and risky replacements."));
        stack.Children.Add(RailFeature("▧", "Profile Management", "Imports, duplicates, exports and switches profiles for ships, roles or play styles."));
        stack.Children.Add(RailFeature("✓", "Safe and Reversible", "Creates timestamped backups and validates XML before replacing the original profile."));
        stack.Children.Add(RailFeature("▶", "One-Click Launch", "Saves the profile and opens the RSI Launcher when mapping is complete."));
        scroll.Content = stack;
        Grid.SetRow(scroll, 1);
        grid.Children.Add(scroll);
        return rail;
    }

    private static TextBlock RailHeading(string text, Thickness? margin = null) => new()
    {
        Text = text,
        Foreground = Cyan,
        FontWeight = FontWeights.Bold,
        FontSize = 13,
        Margin = margin ?? new Thickness(0, 0, 0, 8)
    };

    private static UIElement RailBullet(string text)
    {
        var row = new Grid { Margin = new Thickness(0, 0, 0, 6) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        row.ColumnDefinitions.Add(new ColumnDefinition());
        row.Children.Add(new TextBlock { Text = "•", Foreground = Text, FontSize = 12 });
        var body = new TextBlock { Text = text, Foreground = Text, FontSize = 10.5, TextWrapping = TextWrapping.Wrap, LineHeight = 15 };
        Grid.SetColumn(body, 1);
        row.Children.Add(body);
        return row;
    }

    private static UIElement RailFeature(string icon, string title, string description)
    {
        var row = new Grid { Margin = new Thickness(0, 0, 0, 11) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
        row.ColumnDefinitions.Add(new ColumnDefinition());
        var iconBox = new Border { Width = 30, Height = 30, CornerRadius = new CornerRadius(7), Background = BlueDim, BorderBrush = Blue, BorderThickness = new Thickness(1), VerticalAlignment = VerticalAlignment.Top };
        iconBox.Child = new TextBlock { Text = icon, Foreground = Cyan, FontSize = 16, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        row.Children.Add(iconBox);
        var words = new StackPanel();
        words.Children.Add(new TextBlock { Text = title, Foreground = Text, FontWeight = FontWeights.SemiBold, FontSize = 11 });
        words.Children.Add(new TextBlock { Text = description, Foreground = Muted, FontSize = 9.5, TextWrapping = TextWrapping.Wrap, LineHeight = 13 });
        Grid.SetColumn(words, 1);
        row.Children.Add(words);
        return row;
    }

    private UIElement BuildApplicationShell()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(48) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(84) });
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(238) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(52) });
        root.Children.Add(BuildTopBar());
        var devices = BuildDeviceBar();
        Grid.SetRow(devices, 1);
        root.Children.Add(devices);
        var main = BuildMainWorkspace();
        Grid.SetRow(main, 2);
        root.Children.Add(main);
        var bottom = BuildBottomWorkspace();
        Grid.SetRow(bottom, 3);
        root.Children.Add(bottom);
        var footer = BuildFooter();
        Grid.SetRow(footer, 4);
        root.Children.Add(footer);
        return root;
    }

    private UIElement BuildTopBar()
    {
        var bar = new Border { Background = Rail, BorderBrush = Border, BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(14, 0, 14, 0) };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        bar.Child = grid;

        var compactBrand = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        compactBrand.Children.Add(new TextBlock { Text = "✦", Foreground = Cyan, FontSize = 22, VerticalAlignment = VerticalAlignment.Center });
        compactBrand.Children.Add(new TextBlock { Text = "  STARBIND", Foreground = Text, FontSize = 17, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center });
        grid.Children.Add(compactBrand);

        var channel = HeaderStat("CHANNEL", _channelText);
        _channelText.Text = "NO PROFILE";
        _channelText.Foreground = Amber;
        Grid.SetColumn(channel, 1);
        grid.Children.Add(channel);

        var profile = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 14, 0) };
        profile.Children.Add(new TextBlock { Text = "Active Profile:", Foreground = Muted, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 9, 0) });
        StyleCombo(_profilePicker);
        _profilePicker.MinWidth = 330;
        _profilePicker.SelectionChanged += (_, _) => ProfilePickerChanged();
        profile.Children.Add(_profilePicker);
        Grid.SetColumn(profile, 2);
        grid.Children.Add(profile);

        var connected = HeaderStat("DEVICES", _deviceCountText);
        _deviceCountText.Text = "0 LIVE";
        _deviceCountText.Foreground = Green;
        Grid.SetColumn(connected, 3);
        grid.Children.Add(connected);

        var menu = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        menu.Children.Add(TopButton("▧  Profiles", OpenProfileManager));
        menu.Children.Add(TopButton("⚙  Settings", OpenSettingsWindow));
        menu.Children.Add(TopButton("?  Help", OpenHelpWindow));
        Grid.SetColumn(menu, 4);
        grid.Children.Add(menu);
        return bar;
    }

    private UIElement BuildDeviceBar()
    {
        var shell = new Border { Background = Bg, Padding = new Thickness(12, 6, 12, 6) };
        shell.Child = new ScrollViewer { Content = _deviceCards, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled };
        return shell;
    }

    private UIElement BuildMainWorkspace()
    {
        var grid = new Grid { Margin = new Thickness(12, 0, 12, 8) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(210) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(340) });
        grid.Children.Add(BuildControlPanel());
        var visual = BuildVisualPanel();
        visual.Margin = new Thickness(8, 0, 8, 0);
        Grid.SetColumn(visual, 1);
        grid.Children.Add(visual);
        var selected = BuildSelectedControlPanel();
        Grid.SetColumn(selected, 2);
        grid.Children.Add(selected);
        var assignment = BuildAssignmentPanel();
        assignment.Margin = new Thickness(8, 0, 0, 0);
        Grid.SetColumn(assignment, 3);
        grid.Children.Add(assignment);
        return grid;
    }

    private Border BuildControlPanel()
    {
        var controls = Card();
        controls.Padding = new Thickness(11);
        var controlsGrid = new Grid();
        controlsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        controlsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        controlsGrid.RowDefinitions.Add(new RowDefinition());
        controls.Child = controlsGrid;
        controlsGrid.Children.Add(PanelTitle("DEVICE CONTROLS"));
        StyleCombo(_devicePicker);
        _devicePicker.Margin = new Thickness(0, 7, 0, 0);
        _devicePicker.SelectionChanged += (_, _) => DevicePickerChanged();
        Grid.SetRow(_devicePicker, 1);
        controlsGrid.Children.Add(_devicePicker);
        StyleTree(_controlTree);
        _controlTree.Margin = new Thickness(0, 8, 0, 0);
        _controlTree.SelectedItemChanged += (_, _) => ControlTreeChanged();
        Grid.SetRow(_controlTree, 2);
        controlsGrid.Children.Add(_controlTree);
        return controls;
    }

    private Border BuildVisualPanel()
    {
        var visual = Card();
        var visualGrid = new Grid();
        visualGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(52) });
        visualGrid.RowDefinitions.Add(new RowDefinition());
        visualGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(46) });
        visual.Child = visualGrid;

        var visualHeader = new Grid { Margin = new Thickness(14, 0, 14, 0) };
        visualHeader.ColumnDefinitions.Add(new ColumnDefinition());
        visualHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var deviceWords = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        _deviceTitle.FontWeight = FontWeights.Bold;
        _deviceTitle.FontSize = 14;
        _deviceTitle.TextTrimming = TextTrimming.CharacterEllipsis;
        _deviceSubtitle.Foreground = Muted;
        _deviceSubtitle.FontSize = 9.5;
        _deviceSubtitle.TextTrimming = TextTrimming.CharacterEllipsis;
        deviceWords.Children.Add(_deviceTitle);
        deviceWords.Children.Add(_deviceSubtitle);
        visualHeader.Children.Add(deviceWords);
        ConfigureButton(_listenButton, "◎  LISTEN FOR INPUT", ToggleListen, BlueDim);
        _listenButton.Padding = new Thickness(13, 8, 13, 8);
        Grid.SetColumn(_listenButton, 1);
        visualHeader.Children.Add(_listenButton);
        visualGrid.Children.Add(visualHeader);

        var canvasLayer = new Grid { Background = Field, ClipToBounds = true };
        canvasLayer.Children.Add(_deviceCanvasHost);
        canvasLayer.Children.Add(BuildMappingAssistant());
        Grid.SetRow(canvasLayer, 1);
        visualGrid.Children.Add(canvasLayer);

        var tools = new Grid { Margin = new Thickness(10, 5, 10, 5) };
        tools.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        tools.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        tools.ColumnDefinitions.Add(new ColumnDefinition());
        tools.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        ConfigureButton(_view3DButton, "◇  View in 3D", OpenDevice3D, Panel2);
        _view3DButton.Margin = new Thickness(0, 0, 5, 0);
        tools.Children.Add(_view3DButton);
        ConfigureButton(_testButton, "◉  Test Device", OpenDeviceTester, Panel2);
        _testButton.Margin = new Thickness(0, 0, 10, 0);
        Grid.SetColumn(_testButton, 1);
        tools.Children.Add(_testButton);
        _showUnassigned.Margin = new Thickness(8, 0, 0, 0);
        _showUnassigned.HorizontalAlignment = HorizontalAlignment.Center;
        _showUnassigned.Checked += (_, _) => RebuildControlsAndVisual();
        _showUnassigned.Unchecked += (_, _) => RebuildControlsAndVisual();
        Grid.SetColumn(_showUnassigned, 2);
        tools.Children.Add(_showUnassigned);
        var zoomStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        zoomStack.Children.Add(new TextBlock { Text = "Zoom", Foreground = Muted, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) });
        StyleCombo(_zoomPicker);
        _zoomPicker.Width = 80;
        _zoomPicker.ItemsSource = new[] { "75%", "90%", "100%", "110%", "125%", "150%" };
        _zoomPicker.SelectedItem = "100%";
        _zoomPicker.SelectionChanged += (_, _) => ZoomChanged();
        zoomStack.Children.Add(_zoomPicker);
        Grid.SetColumn(zoomStack, 3);
        tools.Children.Add(zoomStack);
        Grid.SetRow(tools, 2);
        visualGrid.Children.Add(tools);
        return visual;
    }

    private UIElement BuildMappingAssistant()
    {
        _mappingAssistantHost.Visibility = Visibility.Collapsed;
        _mappingAssistantHost.HorizontalAlignment = HorizontalAlignment.Center;
        _mappingAssistantHost.VerticalAlignment = VerticalAlignment.Bottom;
        _mappingAssistantHost.MaxWidth = 760;
        _mappingAssistantHost.Margin = new Thickness(12);
        _mappingAssistantHost.Padding = new Thickness(12);
        _mappingAssistantHost.Background = Paint("#0A111B");
        _mappingAssistantHost.BorderBrush = Blue;
        _mappingAssistantHost.BorderThickness = new Thickness(1);
        _mappingAssistantHost.CornerRadius = new CornerRadius(6);

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var summary = new StackPanel { Width = 390 };
        summary.Children.Add(new TextBlock { Text = "DUPLICATE / SIMILAR ACTION DETECTED", Foreground = Amber, FontSize = 11, FontWeight = FontWeights.Bold });
        _mappingAssistantBody.Foreground = Muted;
        _mappingAssistantBody.FontSize = 10;
        _mappingAssistantBody.TextWrapping = TextWrapping.Wrap;
        _mappingAssistantBody.Margin = new Thickness(0, 5, 0, 5);
        summary.Children.Add(_mappingAssistantBody);
        summary.Children.Add(_mappingAssistantStates);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 7, 0, 0) };
        var keepAll = DialogButton("YES, KEEP ALL", BlueDim);
        keepAll.Click += MappingAssistantKeepAll;
        buttons.Children.Add(keepAll);
        var choose = DialogButton("NO, LET ME CHOOSE", Panel2);
        choose.Click += MappingAssistantChoose;
        buttons.Children.Add(choose);
        summary.Children.Add(buttons);
        grid.Children.Add(summary);

        _mappingAssistantChooser.Visibility = Visibility.Collapsed;
        _mappingAssistantChooser.Width = 310;
        _mappingAssistantChooser.Margin = new Thickness(12, 0, 0, 0);
        _mappingAssistantChooser.Padding = new Thickness(10);
        _mappingAssistantChooser.Background = Field;
        _mappingAssistantChooser.BorderBrush = Blue;
        _mappingAssistantChooser.BorderThickness = new Thickness(1);
        _mappingAssistantChooser.CornerRadius = new CornerRadius(5);
        var chooserStack = new StackPanel();
        chooserStack.Children.Add(new TextBlock { Text = "REMOVE FROM WHICH STATE?", Foreground = Cyan, FontWeight = FontWeights.Bold, FontSize = 10 });
        chooserStack.Children.Add(new TextBlock { Text = "Checked states keep this physical control. Unchecked states lose the input when saved.", Foreground = Muted, FontSize = 9, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 6) });
        var chooserScroll = new ScrollViewer { Content = _mappingAssistantChooserRows, MaxHeight = 120, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        chooserStack.Children.Add(chooserScroll);
        ConfigureButton(_mappingAssistantConfirm, "CONFIRM", MappingAssistantConfirm, BlueDim);
        _mappingAssistantConfirm.HorizontalAlignment = HorizontalAlignment.Center;
        _mappingAssistantConfirm.Margin = new Thickness(0, 7, 0, 0);
        chooserStack.Children.Add(_mappingAssistantConfirm);
        _mappingAssistantChooser.Child = chooserStack;
        Grid.SetColumn(_mappingAssistantChooser, 1);
        grid.Children.Add(_mappingAssistantChooser);
        _mappingAssistantHost.Child = grid;
        return _mappingAssistantHost;
    }

    private Border BuildSelectedControlPanel()
    {
        var panel = Card();
        panel.Padding = new Thickness(11);
        var stack = new StackPanel();
        panel.Child = stack;
        stack.Children.Add(PanelTitle("SELECTED CONTROL"));
        _selectedControlName.Text = "No control selected";
        _selectedControlName.FontSize = 17;
        _selectedControlName.FontWeight = FontWeights.SemiBold;
        _selectedControlName.Margin = new Thickness(0, 9, 0, 2);
        _selectedControlName.TextWrapping = TextWrapping.Wrap;
        stack.Children.Add(_selectedControlName);
        _selectedControlInput.Foreground = Cyan;
        _selectedControlInput.FontSize = 9.5;
        _selectedControlInput.TextWrapping = TextWrapping.Wrap;
        stack.Children.Add(_selectedControlInput);
        stack.Children.Add(SmallLabel("CONTROL TYPE", new Thickness(0, 14, 0, 3)));
        _selectedControlType.FontWeight = FontWeights.SemiBold;
        stack.Children.Add(_selectedControlType);
        stack.Children.Add(SmallLabel("CURRENT RESPONSE", new Thickness(0, 14, 0, 4)));
        _responseGraph.Height = 142;
        _responseGraph.Background = Field;
        stack.Children.Add(_responseGraph);
        var settings = new Grid { Margin = new Thickness(0, 10, 0, 0) };
        settings.ColumnDefinitions.Add(new ColumnDefinition());
        settings.ColumnDefinitions.Add(new ColumnDefinition());
        var deadzoneStack = new StackPanel { Margin = new Thickness(0, 0, 4, 0) };
        deadzoneStack.Children.Add(SmallLabel("DEADZONE", new Thickness(0, 0, 0, 3)));
        StyleCombo(_deadzonePicker);
        _deadzonePicker.ItemsSource = new[] { "0%", "2%", "5%", "7%", "10%", "15%", "20%" };
        _deadzonePicker.SelectedItem = "0%";
        _deadzonePicker.SelectionChanged += (_, _) => AxisSettingChanged();
        deadzoneStack.Children.Add(_deadzonePicker);
        settings.Children.Add(deadzoneStack);
        var curveStack = new StackPanel { Margin = new Thickness(4, 0, 0, 0) };
        curveStack.Children.Add(SmallLabel("CURVE", new Thickness(0, 0, 0, 3)));
        StyleCombo(_curvePicker);
        _curvePicker.ItemsSource = new[] { "Linear", "Gentle", "Aggressive", "Precision" };
        _curvePicker.SelectedItem = "Linear";
        _curvePicker.SelectionChanged += (_, _) => AxisSettingChanged();
        curveStack.Children.Add(_curvePicker);
        Grid.SetColumn(curveStack, 1);
        settings.Children.Add(curveStack);
        stack.Children.Add(settings);
        stack.Children.Add(SmallLabel("CURRENT ASSIGNMENTS", new Thickness(0, 14, 0, 3)));
        _selectedControlAssignmentCount.Text = "0";
        _selectedControlAssignmentCount.Foreground = Green;
        _selectedControlAssignmentCount.FontSize = 15;
        _selectedControlAssignmentCount.FontWeight = FontWeights.Bold;
        stack.Children.Add(_selectedControlAssignmentCount);
        return panel;
    }

    private Border BuildAssignmentPanel()
    {
        var panel = Card();
        panel.Padding = new Thickness(11);
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        var stack = new StackPanel();
        scroll.Content = stack;
        panel.Child = scroll;
        stack.Children.Add(PanelTitle("ACTION ASSIGNMENT"));
        stack.Children.Add(SmallLabel("PRIMARY ACTION", new Thickness(0, 9, 0, 4)));
        var primaryGrid = new Grid();
        primaryGrid.ColumnDefinitions.Add(new ColumnDefinition());
        primaryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
        StyleCombo(_primaryActionPicker);
        _primaryActionPicker.SelectionChanged += (_, _) => PrimaryActionChanged();
        primaryGrid.Children.Add(_primaryActionPicker);
        var more = new Button { Content = "…", Background = Panel2, Foreground = Text, BorderBrush = Border2, Margin = new Thickness(5, 0, 0, 0), Cursor = Cursors.Hand, FontSize = 17 };
        more.Click += OpenActionOptions;
        Grid.SetColumn(more, 1);
        primaryGrid.Children.Add(more);
        stack.Children.Add(primaryGrid);
        stack.Children.Add(SmallLabel("STATE BINDINGS", new Thickness(0, 12, 0, 5)));
        stack.Children.Add(_stateRows);
        ConfigureButton(_applyButton, "EDIT STATE BINDINGS", OpenStateBindingEditor, Panel2);
        _applyButton.Margin = new Thickness(0, 8, 0, 0);
        _applyButton.HorizontalAlignment = HorizontalAlignment.Stretch;
        stack.Children.Add(_applyButton);
        stack.Children.Add(new Separator { Margin = new Thickness(0, 12, 0, 10), Background = Border });
        stack.Children.Add(PanelTitle("ABOUT THIS ACTION"));
        _actionDescription.Text = "Choose a control and action.";
        _actionDescription.Foreground = Muted;
        _actionDescription.TextWrapping = TextWrapping.Wrap;
        _actionDescription.Margin = new Thickness(0, 7, 0, 8);
        stack.Children.Add(_actionDescription);
        stack.Children.Add(SmallLabel("SIMILAR ACTIONS", new Thickness(0, 4, 0, 3)));
        stack.Children.Add(_similarActions);
        ConfigureButton(_compareButton, "COMPARE ACTIONS", OpenActionComparison, Panel2);
        _compareButton.Margin = new Thickness(0, 8, 0, 0);
        _compareButton.HorizontalAlignment = HorizontalAlignment.Left;
        stack.Children.Add(_compareButton);
        return panel;
    }

    private UIElement BuildBottomWorkspace()
    {
        var grid = new Grid { Margin = new Thickness(12, 0, 12, 8) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(290) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(510) });
        grid.Children.Add(BuildWarningsPanel());
        var browser = BuildActionBrowserPanel();
        browser.Margin = new Thickness(8, 0, 8, 0);
        Grid.SetColumn(browser, 1);
        grid.Children.Add(browser);
        var overview = BuildStateOverviewPanel();
        Grid.SetColumn(overview, 2);
        grid.Children.Add(overview);
        return grid;
    }

    private Border BuildWarningsPanel()
    {
        var warnings = Card();
        warnings.Padding = new Thickness(10);
        var warningGrid = new Grid();
        warningGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        warningGrid.RowDefinitions.Add(new RowDefinition());
        warningGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        warnings.Child = warningGrid;
        warningGrid.Children.Add(PanelTitle("CONFLICTS & WARNINGS"));
        StyleList(_warnings);
        _warnings.Margin = new Thickness(0, 7, 0, 0);
        _warnings.ItemTemplate = WarningTemplate();
        Grid.SetRow(_warnings, 1);
        warningGrid.Children.Add(_warnings);
        ConfigureButton(_resolveAllButton, "RESOLVE ALL", OpenConflictResolver, Panel2);
        _resolveAllButton.Margin = new Thickness(0, 7, 0, 0);
        _resolveAllButton.HorizontalAlignment = HorizontalAlignment.Left;
        Grid.SetRow(_resolveAllButton, 2);
        warningGrid.Children.Add(_resolveAllButton);
        return warnings;
    }

    private Border BuildActionBrowserPanel()
    {
        var browser = Card();
        browser.Padding = new Thickness(10);
        var browserGrid = new Grid();
        browserGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        browserGrid.RowDefinitions.Add(new RowDefinition());
        browser.Child = browserGrid;
        var browserHeader = new Grid();
        browserHeader.ColumnDefinitions.Add(new ColumnDefinition());
        browserHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
        browserHeader.Children.Add(PanelTitle("ACTION EXPLANATION BROWSER"));
        StyleTextBox(_actionSearch);
        _actionSearch.ToolTip = "Search by friendly name, raw action, category, intent, behavior or game state";
        _actionSearch.TextChanged += (_, _) => BuildActionBrowser();
        Grid.SetColumn(_actionSearch, 1);
        browserHeader.Children.Add(_actionSearch);
        browserGrid.Children.Add(browserHeader);

        var browserBody = new Grid { Margin = new Thickness(0, 7, 0, 0) };
        browserBody.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(46, GridUnitType.Star) });
        browserBody.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(54, GridUnitType.Star) });
        StyleTree(_actionTree);
        _actionTree.SelectedItemChanged += (_, _) => BrowserActionChanged();
        browserBody.Children.Add(_actionTree);
        var detail = new Border { Background = Field, BorderBrush = Border, BorderThickness = new Thickness(1), Padding = new Thickness(9), Margin = new Thickness(7, 0, 0, 0) };
        var detailGrid = new Grid();
        detailGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        detailGrid.RowDefinitions.Add(new RowDefinition());
        detailGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _browserActionTitle.FontWeight = FontWeights.SemiBold;
        _browserActionTitle.TextWrapping = TextWrapping.Wrap;
        detailGrid.Children.Add(_browserActionTitle);
        var detailScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Margin = new Thickness(0, 5, 0, 0), Content = _browserActionBody };
        _browserActionBody.Foreground = Muted;
        _browserActionBody.TextWrapping = TextWrapping.Wrap;
        Grid.SetRow(detailScroll, 1);
        detailGrid.Children.Add(detailScroll);
        var manual = new Button { Content = "VIEW IN GAME MANUAL", Background = Panel2, Foreground = Text, BorderBrush = Border, Padding = new Thickness(10, 6, 10, 6), HorizontalAlignment = HorizontalAlignment.Left, Cursor = Cursors.Hand };
        manual.Click += OpenGameManual;
        Grid.SetRow(manual, 2);
        detailGrid.Children.Add(manual);
        detail.Child = detailGrid;
        Grid.SetColumn(detail, 1);
        browserBody.Children.Add(detail);
        Grid.SetRow(browserBody, 1);
        browserGrid.Children.Add(browserBody);
        return browser;
    }

    private Border BuildStateOverviewPanel()
    {
        var overview = Card();
        overview.Padding = new Thickness(10);
        var overviewGrid = new Grid();
        overviewGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        overviewGrid.RowDefinitions.Add(new RowDefinition());
        overview.Child = overviewGrid;
        overviewGrid.Children.Add(PanelTitle("STATE OVERVIEW FOR SELECTED CONTROL"));
        _stateOverview.ItemWidth = 118;
        _stateOverview.ItemHeight = 95;
        var stateScroll = new ScrollViewer { Content = _stateOverview, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 7, 0, 0) };
        Grid.SetRow(stateScroll, 1);
        overviewGrid.Children.Add(stateScroll);
        return overview;
    }

    private UIElement BuildFooter()
    {
        var footer = new Border { Background = Rail, BorderBrush = Border, BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(12, 0, 12, 0) };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
        footer.Child = grid;
        var statusStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        statusStack.Children.Add(new Ellipse { Width = 9, Height = 9, Fill = Green, Margin = new Thickness(0, 0, 8, 0) });
        _status.Text = "Ready";
        _status.TextTrimming = TextTrimming.CharacterEllipsis;
        statusStack.Children.Add(_status);
        _savedText.Foreground = Muted;
        _savedText.Margin = new Thickness(20, 0, 0, 0);
        statusStack.Children.Add(_savedText);
        grid.Children.Add(statusStack);
        var validate = FooterButton("✓  VALIDATE PROFILE", ValidateProfile);
        Grid.SetColumn(validate, 1);
        grid.Children.Add(validate);
        var backup = FooterButton("▧  BACKUP PROFILE", BackupProfile);
        Grid.SetColumn(backup, 2);
        grid.Children.Add(backup);
        ConfigureButton(_saveLaunchButton, "▶  SAVE & LAUNCH STAR CITIZEN", SaveAndLaunch, Green);
        _saveLaunchButton.Padding = new Thickness(18, 9, 18, 9);
        Grid.SetColumn(_saveLaunchButton, 3);
        grid.Children.Add(_saveLaunchButton);
        _saveMenuButton.Content = "▼";
        _saveMenuButton.Background = Green;
        _saveMenuButton.Foreground = Text;
        _saveMenuButton.BorderBrush = Paint("#2F9A4A");
        _saveMenuButton.Margin = new Thickness(1, 0, 0, 0);
        _saveMenuButton.Cursor = Cursors.Hand;
        _saveMenuButton.Click += OpenSaveMenu;
        Grid.SetColumn(_saveMenuButton, 4);
        grid.Children.Add(_saveMenuButton);
        return footer;
    }
}
