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
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(56) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(78) });
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(230) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(58) });

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
            Padding = new Thickness(16, 0, 16, 0)
        };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(285) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        bar.Child = grid;

        var brand = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        brand.Children.Add(new TextBlock { Text = "✦", Foreground = Cyan, FontSize = 27, VerticalAlignment = VerticalAlignment.Center });
        var brandWords = new StackPanel { Margin = new Thickness(9, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        brandWords.Children.Add(new TextBlock { Text = "STARBIND", FontSize = 21, FontWeight = FontWeights.Bold });
        brandWords.Children.Add(new TextBlock { Text = "STAR CITIZEN CONTROL MAPPER  •  v0.6", Foreground = Muted, FontSize = 9 });
        brand.Children.Add(brandWords);
        grid.Children.Add(brand);

        var channel = HeaderStat("CHANNEL", _channelText);
        _channelText.Text = "NO PROFILE";
        _channelText.Foreground = Amber;
        Grid.SetColumn(channel, 1);
        grid.Children.Add(channel);

        var profile = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 14, 0) };
        profile.Children.Add(new TextBlock { Text = "Active profile", Foreground = Muted, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 9, 0) });
        StyleCombo(_profilePicker);
        _profilePicker.MinWidth = 360;
        _profilePicker.SelectionChanged += (_, _) => ProfilePickerChanged();
        profile.Children.Add(_profilePicker);
        Grid.SetColumn(profile, 2);
        grid.Children.Add(profile);

        var connected = HeaderStat("DEVICE STATUS", _deviceCountText);
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
        var shell = new Border { Background = Bg, Padding = new Thickness(16, 8, 16, 8) };
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
        var grid = new Grid { Margin = new Thickness(16, 0, 16, 10) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(260) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(430) });

        var controls = BuildControlPanel();
        grid.Children.Add(controls);

        var visual = BuildVisualPanel();
        visual.Margin = new Thickness(10, 0, 10, 0);
        Grid.SetColumn(visual, 1);
        grid.Children.Add(visual);

        var rightRail = BuildRightRail();
        Grid.SetColumn(rightRail, 2);
        grid.Children.Add(rightRail);
        return grid;
    }

    private Border BuildControlPanel()
    {
        var controls = Card();
        controls.Padding = new Thickness(12);
        var controlsGrid = new Grid();
        controlsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        controlsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        controlsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        controlsGrid.RowDefinitions.Add(new RowDefinition());
        controls.Child = controlsGrid;
        controlsGrid.Children.Add(PanelTitle("DEVICE CONTROLS"));
        var hint = new TextBlock { Text = "Pick a named physical control or press Listen for Input.", Foreground = Muted, FontSize = 10, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0) };
        Grid.SetRow(hint, 1);
        controlsGrid.Children.Add(hint);
        StyleCombo(_devicePicker);
        _devicePicker.Margin = new Thickness(0, 9, 0, 0);
        _devicePicker.SelectionChanged += (_, _) => DevicePickerChanged();
        Grid.SetRow(_devicePicker, 2);
        controlsGrid.Children.Add(_devicePicker);
        StyleTree(_controlTree);
        _controlTree.Margin = new Thickness(0, 9, 0, 0);
        _controlTree.SelectedItemChanged += (_, _) => ControlTreeChanged();
        Grid.SetRow(_controlTree, 3);
        controlsGrid.Children.Add(_controlTree);
        return controls;
    }

    private Border BuildVisualPanel()
    {
        var visual = Card();
        var visualGrid = new Grid();
        visualGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(64) });
        visualGrid.RowDefinitions.Add(new RowDefinition());
        visualGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(56) });
        visual.Child = visualGrid;

        var visualHeader = new Grid { Margin = new Thickness(15, 0, 15, 0) };
        visualHeader.ColumnDefinitions.Add(new ColumnDefinition());
        visualHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var deviceWords = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        _deviceTitle.FontWeight = FontWeights.Bold;
        _deviceTitle.FontSize = 15;
        _deviceTitle.TextTrimming = TextTrimming.CharacterEllipsis;
        _deviceSubtitle.Foreground = Muted;
        _deviceSubtitle.FontSize = 10;
        _deviceSubtitle.TextTrimming = TextTrimming.CharacterEllipsis;
        deviceWords.Children.Add(_deviceTitle);
        deviceWords.Children.Add(_deviceSubtitle);
        visualHeader.Children.Add(deviceWords);
        ConfigureButton(_listenButton, "◎  LISTEN FOR INPUT", ToggleListen, BlueDim);
        _listenButton.Padding = new Thickness(14, 10, 14, 10);
        Grid.SetColumn(_listenButton, 1);
        visualHeader.Children.Add(_listenButton);
        visualGrid.Children.Add(visualHeader);

        _deviceCanvasHost.Background = Field;
        _deviceCanvasHost.ClipToBounds = true;
        Grid.SetRow(_deviceCanvasHost, 1);
        visualGrid.Children.Add(_deviceCanvasHost);

        var tools = new Grid { Margin = new Thickness(12, 7, 12, 7) };
        tools.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        tools.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        tools.ColumnDefinitions.Add(new ColumnDefinition());
        tools.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        ConfigureButton(_view3DButton, "◇  View in 3D", OpenDevice3D, Panel2);
        _view3DButton.Margin = new Thickness(0, 0, 6, 0);
        tools.Children.Add(_view3DButton);
        ConfigureButton(_testButton, "◉  Test Device", OpenDeviceTester, Panel2);
        _testButton.Margin = new Thickness(0, 0, 12, 0);
        Grid.SetColumn(_testButton, 1);
        tools.Children.Add(_testButton);
        _showUnassigned.Margin = new Thickness(10, 0, 10, 0);
        _showUnassigned.HorizontalAlignment = HorizontalAlignment.Center;
        _showUnassigned.Checked += (_, _) => RebuildControlsAndVisual();
        _showUnassigned.Unchecked += (_, _) => RebuildControlsAndVisual();
        Grid.SetColumn(_showUnassigned, 2);
        tools.Children.Add(_showUnassigned);
        var zoomStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        zoomStack.Children.Add(new TextBlock { Text = "Zoom", Foreground = Muted, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 7, 0) });
        StyleCombo(_zoomPicker);
        _zoomPicker.Width = 88;
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

    private UIElement BuildRightRail()
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(205) });
        grid.RowDefinitions.Add(new RowDefinition());
        var selected = BuildSelectedControlPanel();
        grid.Children.Add(selected);
        var assignment = BuildAssignmentPanel();
        assignment.Margin = new Thickness(0, 10, 0, 0);
        Grid.SetRow(assignment, 1);
        grid.Children.Add(assignment);
        return grid;
    }

    private Border BuildSelectedControlPanel()
    {
        var panel = Card();
        panel.Padding = new Thickness(12);
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition());
        panel.Child = root;
        root.Children.Add(PanelTitle("SELECTED CONTROL"));

        var name = new Grid { Margin = new Thickness(0, 8, 0, 8) };
        name.ColumnDefinitions.Add(new ColumnDefinition());
        name.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var words = new StackPanel();
        _selectedControlName.Text = "No control selected";
        _selectedControlName.FontSize = 18;
        _selectedControlName.FontWeight = FontWeights.SemiBold;
        words.Children.Add(_selectedControlName);
        _selectedControlInput.Foreground = Cyan;
        _selectedControlInput.FontSize = 10;
        _selectedControlInput.TextWrapping = TextWrapping.Wrap;
        words.Children.Add(_selectedControlInput);
        name.Children.Add(words);
        var counts = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
        counts.Children.Add(SmallLabel("ASSIGNMENTS", new Thickness(0)));
        _selectedControlAssignmentCount.Text = "0";
        _selectedControlAssignmentCount.FontSize = 18;
        _selectedControlAssignmentCount.FontWeight = FontWeights.Bold;
        _selectedControlAssignmentCount.Foreground = Green;
        _selectedControlAssignmentCount.HorizontalAlignment = HorizontalAlignment.Right;
        counts.Children.Add(_selectedControlAssignmentCount);
        Grid.SetColumn(counts, 1);
        name.Children.Add(counts);
        Grid.SetRow(name, 1);
        root.Children.Add(name);

        var lower = new Grid();
        lower.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(145) });
        lower.ColumnDefinitions.Add(new ColumnDefinition());
        var info = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
        info.Children.Add(SmallLabel("CONTROL TYPE", new Thickness(0, 0, 0, 3)));
        _selectedControlType.FontWeight = FontWeights.SemiBold;
        info.Children.Add(_selectedControlType);
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
        info.Children.Add(settings);
        lower.Children.Add(info);
        var response = new StackPanel();
        response.Children.Add(SmallLabel("LIVE RESPONSE", new Thickness(0, 0, 0, 4)));
        _responseGraph.Height = 94;
        _responseGraph.Background = Field;
        response.Children.Add(_responseGraph);
        Grid.SetColumn(response, 1);
        lower.Children.Add(response);
        Grid.SetRow(lower, 2);
        root.Children.Add(lower);
        return panel;
    }

    private Border BuildAssignmentPanel()
    {
        var panel = Card();
        panel.Padding = new Thickness(12);
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.Child = grid;
        grid.Children.Add(PanelTitle("ACTION ASSIGNMENT"));

        var primaryHost = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
        primaryHost.Children.Add(SmallLabel("PRIMARY ACTION", new Thickness(0, 0, 0, 4)));
        var primaryGrid = new Grid();
        primaryGrid.ColumnDefinitions.Add(new ColumnDefinition());
        primaryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(38) });
        StyleCombo(_primaryActionPicker);
        _primaryActionPicker.SelectionChanged += (_, _) => PrimaryActionChanged();
        primaryGrid.Children.Add(_primaryActionPicker);
        var more = new Button { Content = "…", Background = Panel2, Foreground = Text, BorderBrush = Border2, Margin = new Thickness(6, 0, 0, 0), Cursor = Cursors.Hand, FontSize = 18 };
        more.Click += OpenActionOptions;
        Grid.SetColumn(more, 1);
        primaryGrid.Children.Add(more);
        primaryHost.Children.Add(primaryGrid);
        Grid.SetRow(primaryHost, 1);
        grid.Children.Add(primaryHost);

        var stateTitle = new TextBlock { Text = "STATE BINDINGS", Foreground = Faint, FontSize = 9, Margin = new Thickness(0, 10, 0, 5) };
        Grid.SetRow(stateTitle, 2);
        grid.Children.Add(stateTitle);
        var stateScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = _stateRows };
        Grid.SetRow(stateScroll, 3);
        grid.Children.Add(stateScroll);

        ConfigureButton(_applyButton, "REVIEW STATE CHANGES", OpenStateBindingEditor, BlueDim);
        _applyButton.Margin = new Thickness(0, 8, 0, 0);
        _applyButton.HorizontalAlignment = HorizontalAlignment.Stretch;
        Grid.SetRow(_applyButton, 4);
        grid.Children.Add(_applyButton);

        var insight = new Border { Background = Field, BorderBrush = Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4), Padding = new Thickness(9), Margin = new Thickness(0, 9, 0, 0) };
        var insightGrid = new Grid();
        insightGrid.ColumnDefinitions.Add(new ColumnDefinition());
        insightGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var insightWords = new StackPanel();
        insightWords.Children.Add(new TextBlock { Text = "ABOUT THIS ACTION", Foreground = Faint, FontSize = 9, FontWeight = FontWeights.Bold });
        _actionDescription.Text = "Choose a control and action.";
        _actionDescription.TextWrapping = TextWrapping.Wrap;
        _actionDescription.TextTrimming = TextTrimming.CharacterEllipsis;
        _actionDescription.MaxHeight = 45;
        _actionDescription.Foreground = Muted;
        _actionDescription.Margin = new Thickness(0, 4, 10, 0);
        insightWords.Children.Add(_actionDescription);
        insightGrid.Children.Add(insightWords);
        ConfigureButton(_compareButton, "COMPARE", OpenActionComparison, Panel2);
        _compareButton.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(_compareButton, 1);
        insightGrid.Children.Add(_compareButton);
        insight.Child = insightGrid;
        Grid.SetRow(insight, 5);
        grid.Children.Add(insight);
        return panel;
    }

    private UIElement BuildBottomWorkspace()
    {
        var grid = new Grid { Margin = new Thickness(16, 0, 16, 10) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(310) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(500) });

        grid.Children.Add(BuildWarningsPanel());
        var browser = BuildActionBrowserPanel();
        browser.Margin = new Thickness(10, 0, 10, 0);
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
        warnings.Padding = new Thickness(11);
        var warningGrid = new Grid();
        warningGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        warningGrid.RowDefinitions.Add(new RowDefinition());
        warningGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        warnings.Child = warningGrid;
        warningGrid.Children.Add(PanelTitle("CONFLICTS & WARNINGS"));
        StyleList(_warnings);
        _warnings.Margin = new Thickness(0, 8, 0, 0);
        _warnings.ItemTemplate = WarningTemplate();
        Grid.SetRow(_warnings, 1);
        warningGrid.Children.Add(_warnings);
        ConfigureButton(_resolveAllButton, "RESOLVE ALL", OpenConflictResolver, Panel2);
        _resolveAllButton.Margin = new Thickness(0, 8, 0, 0);
        _resolveAllButton.HorizontalAlignment = HorizontalAlignment.Left;
        Grid.SetRow(_resolveAllButton, 2);
        warningGrid.Children.Add(_resolveAllButton);
        return warnings;
    }

    private Border BuildActionBrowserPanel()
    {
        var browser = Card();
        browser.Padding = new Thickness(11);
        var browserGrid = new Grid();
        browserGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        browserGrid.RowDefinitions.Add(new RowDefinition());
        browser.Child = browserGrid;
        var browserHeader = new Grid();
        browserHeader.ColumnDefinitions.Add(new ColumnDefinition());
        browserHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250) });
        browserHeader.Children.Add(PanelTitle("ACTION EXPLANATION BROWSER"));
        StyleTextBox(_actionSearch);
        _actionSearch.ToolTip = "Search by plain name, raw action, category, intent, behavior, or game state";
        _actionSearch.TextChanged += (_, _) => BuildActionBrowser();
        Grid.SetColumn(_actionSearch, 1);
        browserHeader.Children.Add(_actionSearch);
        browserGrid.Children.Add(browserHeader);

        var browserBody = new Grid { Margin = new Thickness(0, 8, 0, 0) };
        browserBody.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(45, GridUnitType.Star) });
        browserBody.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(55, GridUnitType.Star) });
        StyleTree(_actionTree);
        _actionTree.SelectedItemChanged += (_, _) => BrowserActionChanged();
        browserBody.Children.Add(_actionTree);
        var detail = new Border { Background = Field, BorderBrush = Border, BorderThickness = new Thickness(1), Padding = new Thickness(10), Margin = new Thickness(8, 0, 0, 0) };
        var detailGrid = new Grid();
        detailGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        detailGrid.RowDefinitions.Add(new RowDefinition());
        detailGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _browserActionTitle.FontWeight = FontWeights.SemiBold;
        _browserActionTitle.FontSize = 13;
        _browserActionTitle.TextWrapping = TextWrapping.Wrap;
        detailGrid.Children.Add(_browserActionTitle);
        var detailScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Margin = new Thickness(0, 6, 0, 0), Content = _browserActionBody };
        _browserActionBody.Foreground = Muted;
        _browserActionBody.TextWrapping = TextWrapping.Wrap;
        Grid.SetRow(detailScroll, 1);
        detailGrid.Children.Add(detailScroll);
        var manual = new Button { Content = "VIEW IN GAME MANUAL", Background = Panel2, Foreground = Text, BorderBrush = Border, Padding = new Thickness(10, 7, 10, 7), HorizontalAlignment = HorizontalAlignment.Left, Cursor = Cursors.Hand };
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
        overview.Padding = new Thickness(11);
        var overviewGrid = new Grid();
        overviewGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        overviewGrid.RowDefinitions.Add(new RowDefinition());
        overview.Child = overviewGrid;
        overviewGrid.Children.Add(PanelTitle("STATE OVERVIEW FOR SELECTED CONTROL"));
        _stateOverview.ItemWidth = 151;
        _stateOverview.ItemHeight = 87;
        var stateScroll = new ScrollViewer
        {
            Content = _stateOverview,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Margin = new Thickness(0, 8, 0, 0)
        };
        Grid.SetRow(stateScroll, 1);
        overviewGrid.Children.Add(stateScroll);
        return overview;
    }

    private UIElement BuildFooter()
    {
        var footer = new Border { Background = Rail, BorderBrush = Border, BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(16, 0, 16, 0) };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
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
        _saveLaunchButton.Padding = new Thickness(20, 10, 20, 10);
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
