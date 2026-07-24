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
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(46) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(86) });
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(255) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(54) });

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
        var bar = new Border
        {
            Background = Rail,
            BorderBrush = Border,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(14, 0, 14, 0)
        };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(145) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(165) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        bar.Child = grid;

        var brand = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        brand.Children.Add(new TextBlock { Text = "✦", Foreground = Cyan, FontSize = 24, VerticalAlignment = VerticalAlignment.Center });
        var brandWords = new StackPanel { Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        brandWords.Children.Add(new TextBlock { Text = "STARBIND", FontSize = 20, FontWeight = FontWeights.Bold });
        brandWords.Children.Add(new TextBlock { Text = "STAR CITIZEN CONTROL MAPPER", Foreground = Muted, FontSize = 9 });
        brand.Children.Add(brandWords);
        grid.Children.Add(brand);

        var channel = HeaderStat("CHANNEL", _channelText);
        _channelText.Text = "NO PROFILE";
        _channelText.Foreground = Amber;
        Grid.SetColumn(channel, 1);
        grid.Children.Add(channel);

        var profile = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 8, 0) };
        profile.Children.Add(new TextBlock { Text = "Active Profile:", Foreground = Muted, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
        StyleCombo(_profilePicker);
        _profilePicker.MinWidth = 330;
        _profilePicker.SelectionChanged += (_, _) => ProfilePickerChanged();
        profile.Children.Add(_profilePicker);
        Grid.SetColumn(profile, 2);
        grid.Children.Add(profile);

        var connected = HeaderStat("DEVICES", _deviceCountText);
        _deviceCountText.Text = "0 CONNECTED";
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
        var shell = new Border { Background = Bg, Padding = new Thickness(14, 10, 14, 10) };
        shell.Child = new ScrollViewer
        {
            Content = _deviceCards,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        return shell;
    }

    private UIElement BuildMainWorkspace()
    {
        var grid = new Grid { Margin = new Thickness(14, 0, 14, 8) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(245) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(205) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(350) });

        var controls = Card();
        controls.Padding = new Thickness(10);
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
        grid.Children.Add(controls);

        var visual = Card();
        visual.Margin = new Thickness(8, 0, 8, 0);
        Grid.SetColumn(visual, 1);
        var visualGrid = new Grid();
        visualGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });
        visualGrid.RowDefinitions.Add(new RowDefinition());
        visualGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(46) });
        visual.Child = visualGrid;

        var visualHeader = new Grid { Margin = new Thickness(12, 0, 12, 0) };
        visualHeader.ColumnDefinitions.Add(new ColumnDefinition());
        visualHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var deviceWords = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        _deviceTitle.FontWeight = FontWeights.Bold;
        _deviceTitle.FontSize = 13;
        _deviceSubtitle.Foreground = Muted;
        _deviceSubtitle.FontSize = 10;
        deviceWords.Children.Add(_deviceTitle);
        deviceWords.Children.Add(_deviceSubtitle);
        visualHeader.Children.Add(deviceWords);
        ConfigureButton(_listenButton, "◎  LISTEN FOR INPUT", ToggleListen, BlueDim);
        Grid.SetColumn(_listenButton, 1);
        visualHeader.Children.Add(_listenButton);
        visualGrid.Children.Add(visualHeader);

        _deviceCanvasHost.Background = Field;
        _deviceCanvasHost.ClipToBounds = true;
        Grid.SetRow(_deviceCanvasHost, 1);
        visualGrid.Children.Add(_deviceCanvasHost);

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
        grid.Children.Add(visual);

        var selected = BuildSelectedControlPanel();
        Grid.SetColumn(selected, 2);
        grid.Children.Add(selected);
        var assignment = BuildAssignmentPanel();
        Grid.SetColumn(assignment, 3);
        grid.Children.Add(assignment);
        return grid;
    }

    private UIElement BuildSelectedControlPanel()
    {
        var panel = Card();
        panel.Padding = new Thickness(10);
        var stack = new StackPanel();
        panel.Child = stack;
        stack.Children.Add(PanelTitle("SELECTED CONTROL"));
        _selectedControlName.Text = "No control selected";
        _selectedControlName.FontSize = 16;
        _selectedControlName.FontWeight = FontWeights.SemiBold;
        _selectedControlName.Margin = new Thickness(0, 9, 0, 2);
        stack.Children.Add(_selectedControlName);
        _selectedControlInput.Foreground = Cyan;
        _selectedControlInput.FontSize = 10;
        _selectedControlInput.TextWrapping = TextWrapping.Wrap;
        stack.Children.Add(_selectedControlInput);
        stack.Children.Add(SmallLabel("CONTROL TYPE", new Thickness(0, 14, 0, 3)));
        stack.Children.Add(_selectedControlType);
        stack.Children.Add(SmallLabel("CURRENT RESPONSE", new Thickness(0, 14, 0, 4)));
        _responseGraph.Height = 98;
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
        stack.Children.Add(_selectedControlAssignmentCount);
        return panel;
    }

    private UIElement BuildAssignmentPanel()
    {
        var panel = Card();
        panel.Padding = new Thickness(10);
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        var stack = new StackPanel();
        scroll.Content = stack;
        panel.Child = scroll;
        stack.Children.Add(PanelTitle("ACTION ASSIGNMENT"));
        stack.Children.Add(SmallLabel("PRIMARY ACTION", new Thickness(0, 10, 0, 4)));
        var primaryGrid = new Grid();
        primaryGrid.ColumnDefinitions.Add(new ColumnDefinition());
        primaryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) });
        StyleCombo(_primaryActionPicker);
        _primaryActionPicker.SelectionChanged += (_, _) => PrimaryActionChanged();
        primaryGrid.Children.Add(_primaryActionPicker);
        var more = new Button { Content = "…", Background = Panel2, Foreground = Text, BorderBrush = Border, Margin = new Thickness(5, 0, 0, 0), Cursor = Cursors.Hand };
        more.Click += OpenActionOptions;
        Grid.SetColumn(more, 1);
        primaryGrid.Children.Add(more);
        stack.Children.Add(primaryGrid);
        stack.Children.Add(SmallLabel("STATE BINDINGS", new Thickness(0, 12, 0, 6)));
        stack.Children.Add(_stateRows);
        ConfigureButton(_applyButton, "EDIT STATE BINDINGS", OpenStateBindingEditor, Panel2);
        _applyButton.Margin = new Thickness(0, 8, 0, 0);
        _applyButton.HorizontalAlignment = HorizontalAlignment.Stretch;
        stack.Children.Add(_applyButton);
        stack.Children.Add(new Separator { Margin = new Thickness(0, 12, 0, 10), Background = Border });
        stack.Children.Add(PanelTitle("ABOUT THIS ACTION"));
        _actionDescription.Text = "Choose a control and action.";
        _actionDescription.TextWrapping = TextWrapping.Wrap;
        _actionDescription.Foreground = Muted;
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
        var grid = new Grid { Margin = new Thickness(14, 0, 14, 8) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(330) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(520) });

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
        Grid.SetRow(_warnings, 1);
        warningGrid.Children.Add(_warnings);
        ConfigureButton(_resolveAllButton, "RESOLVE ALL", OpenConflictResolver, Panel2);
        _resolveAllButton.Margin = new Thickness(0, 7, 0, 0);
        _resolveAllButton.HorizontalAlignment = HorizontalAlignment.Left;
        Grid.SetRow(_resolveAllButton, 2);
        warningGrid.Children.Add(_resolveAllButton);
        grid.Children.Add(warnings);

        var browser = Card();
        browser.Margin = new Thickness(8, 0, 8, 0);
        browser.Padding = new Thickness(10);
        Grid.SetColumn(browser, 1);
        var browserGrid = new Grid();
        browserGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        browserGrid.RowDefinitions.Add(new RowDefinition());
        browser.Child = browserGrid;
        var browserHeader = new Grid();
        browserHeader.ColumnDefinitions.Add(new ColumnDefinition());
        browserHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(230) });
        browserHeader.Children.Add(PanelTitle("ACTION EXPLANATION BROWSER"));
        StyleTextBox(_actionSearch);
        _actionSearch.ToolTip = "Search by plain name, raw action, category, intent, behavior, or game state";
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
        _browserActionBody.Foreground = Muted;
        _browserActionBody.TextWrapping = TextWrapping.Wrap;
        _browserActionBody.Margin = new Thickness(0, 6, 0, 0);
        Grid.SetRow(_browserActionBody, 1);
        detailGrid.Children.Add(_browserActionBody);
        var manual = new Button { Content = "VIEW IN GAME MANUAL", Background = Panel2, Foreground = Text, BorderBrush = Border, Padding = new Thickness(10, 6, 10, 6), HorizontalAlignment = HorizontalAlignment.Left, Cursor = Cursors.Hand };
        manual.Click += OpenGameManual;
        Grid.SetRow(manual, 2);
        detailGrid.Children.Add(manual);
        detail.Child = detailGrid;
        Grid.SetColumn(detail, 1);
        browserBody.Children.Add(detail);
        Grid.SetRow(browserBody, 1);
        browserGrid.Children.Add(browserBody);
        grid.Children.Add(browser);

        var overview = Card();
        overview.Padding = new Thickness(10);
        Grid.SetColumn(overview, 2);
        var overviewGrid = new Grid();
        overviewGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        overviewGrid.RowDefinitions.Add(new RowDefinition());
        overview.Child = overviewGrid;
        overviewGrid.Children.Add(PanelTitle("STATE OVERVIEW FOR SELECTED CONTROL"));
        var stateScroll = new ScrollViewer
        {
            Content = _stateOverview,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Margin = new Thickness(0, 7, 0, 0)
        };
        Grid.SetRow(stateScroll, 1);
        overviewGrid.Children.Add(stateScroll);
        grid.Children.Add(overview);
        return grid;
    }

    private UIElement BuildFooter()
    {
        var footer = new Border { Background = Rail, BorderBrush = Border, BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(14, 0, 14, 0) };
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
