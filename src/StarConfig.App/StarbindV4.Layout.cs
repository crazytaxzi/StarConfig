using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace StarConfig;

public sealed partial class StarbindWindow
{
    private UIElement BuildLayout()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(44) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(82) });
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(245) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(52) });

        root.Children.Add(BuildTopBar());
        var deviceBar = BuildDeviceBar();
        Grid.SetRow(deviceBar, 1);
        root.Children.Add(deviceBar);
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
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        bar.Child = grid;

        var brand = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        brand.Children.Add(new TextBlock { Text = "✦", Foreground = Cyan, FontSize = 23, VerticalAlignment = VerticalAlignment.Center });
        brand.Children.Add(new TextBlock { Text = "  STARBIND", FontSize = 20, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center });
        grid.Children.Add(brand);

        var channel = HeaderStat("CHANNEL", _channelText);
        _channelText.Text = "NO PROFILE";
        _channelText.Foreground = Amber;
        Grid.SetColumn(channel, 1);
        grid.Children.Add(channel);

        var profile = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0) };
        profile.Children.Add(new TextBlock { Text = "Active Profile:", Foreground = Muted, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
        StyleCombo(_profilePicker);
        _profilePicker.MinWidth = 300;
        _profilePicker.SelectionChanged += (_, _) => ProfilePickerChanged();
        profile.Children.Add(_profilePicker);
        Grid.SetColumn(profile, 2);
        grid.Children.Add(profile);

        var devices = HeaderStat("DEVICES", _deviceCountText);
        _deviceCountText.Text = "0 CONNECTED";
        _deviceCountText.Foreground = Green;
        Grid.SetColumn(devices, 3);
        grid.Children.Add(devices);

        var menu = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        menu.Children.Add(TopButton("▧  Profiles", OpenProfilesFolder));
        menu.Children.Add(TopButton("⚙  Settings", OpenSettings));
        menu.Children.Add(TopButton("?  Help", OpenHelp));
        Grid.SetColumn(menu, 4);
        grid.Children.Add(menu);
        return bar;
    }

    private UIElement BuildDeviceBar()
    {
        var shell = new Border { Background = Bg, Padding = new Thickness(14, 10) };
        shell.Child = new ScrollViewer { Content = _deviceCards, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled };
        return shell;
    }

    private UIElement BuildMainWorkspace()
    {
        var grid = new Grid { Margin = new Thickness(14, 0, 14, 8) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(245) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(205) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(330) });

        var controls = Card();
        controls.Padding = new Thickness(10);
        var controlsGrid = new Grid();
        controlsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        controlsGrid.RowDefinitions.Add(new RowDefinition());
        controls.Child = controlsGrid;
        controlsGrid.Children.Add(PanelTitle("DEVICE CONTROLS"));
        StyleTree(_controlTree);
        _controlTree.Margin = new Thickness(0, 8, 0, 0);
        _controlTree.SelectedItemChanged += (_, _) => ControlTreeChanged();
        Grid.SetRow(_controlTree, 1);
        controlsGrid.Children.Add(_controlTree);
        grid.Children.Add(controls);

        var visual = Card();
        visual.Margin = new Thickness(8, 0, 8, 0);
        Grid.SetColumn(visual, 1);
        var visualGrid = new Grid();
        visualGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(48) });
        visualGrid.RowDefinitions.Add(new RowDefinition());
        visualGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(42) });
        visual.Child = visualGrid;
        var visualHeader = new Grid { Margin = new Thickness(12, 0) };
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
        ConfigureButton(_listenButton, "◎  Test / Listen", ToggleListen, BlueDim);
        Grid.SetColumn(_listenButton, 1);
        visualHeader.Children.Add(_listenButton);
        visualGrid.Children.Add(visualHeader);
        _deviceCanvasHost.Background = Field;
        Grid.SetRow(_deviceCanvasHost, 1);
        visualGrid.Children.Add(_deviceCanvasHost);
        var visualTools = new Grid { Margin = new Thickness(10, 6) };
        visualTools.ColumnDefinitions.Add(new ColumnDefinition());
        visualTools.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        visualTools.Children.Add(_showUnassigned);
        var zoom = new TextBlock { Text = "Zoom  100%", Foreground = Muted, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(zoom, 1);
        visualTools.Children.Add(zoom);
        Grid.SetRow(visualTools, 2);
        visualGrid.Children.Add(visualTools);
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
        stack.Children.Add(_selectedControlInput);
        stack.Children.Add(SmallLabel("CONTROL TYPE", new Thickness(0, 14, 0, 3)));
        stack.Children.Add(_selectedControlType);
        stack.Children.Add(SmallLabel("CURRENT RESPONSE", new Thickness(0, 14, 0, 4)));
        _responseGraph.Height = 95;
        _responseGraph.Background = Field;
        stack.Children.Add(_responseGraph);
        var settings = new Grid { Margin = new Thickness(0, 10, 0, 0) };
        settings.ColumnDefinitions.Add(new ColumnDefinition());
        settings.ColumnDefinitions.Add(new ColumnDefinition());
        var deadzoneStack = new StackPanel { Margin = new Thickness(0, 0, 4, 0) };
        deadzoneStack.Children.Add(SmallLabel("DEADZONE", new Thickness(0, 0, 0, 3)));
        StyleCombo(_deadzonePicker);
        _deadzonePicker.ItemsSource = new[] { "0%", "2%", "5%", "7%", "10%", "15%", "20%" };
        _deadzonePicker.SelectedIndex = 0;
        _deadzonePicker.SelectionChanged += (_, _) => DrawResponseGraph();
        deadzoneStack.Children.Add(_deadzonePicker);
        settings.Children.Add(deadzoneStack);
        var curveStack = new StackPanel { Margin = new Thickness(4, 0, 0, 0) };
        curveStack.Children.Add(SmallLabel("CURVE", new Thickness(0, 0, 0, 3)));
        StyleCombo(_curvePicker);
        _curvePicker.ItemsSource = new[] { "Linear", "Gentle", "Aggressive" };
        _curvePicker.SelectedIndex = 0;
        _curvePicker.SelectionChanged += (_, _) => DrawResponseGraph();
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
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var stack = new StackPanel();
        scroll.Content = stack;
        panel.Child = scroll;
        stack.Children.Add(PanelTitle("ACTION ASSIGNMENT"));
        stack.Children.Add(SmallLabel("PRIMARY ACTION", new Thickness(0, 10, 0, 4)));
        StyleCombo(_primaryActionPicker);
        _primaryActionPicker.SelectionChanged += (_, _) => PrimaryActionChanged();
        stack.Children.Add(_primaryActionPicker);
        stack.Children.Add(SmallLabel("STATE BINDINGS", new Thickness(0, 12, 0, 6)));
        stack.Children.Add(_stateRows);
        ConfigureButton(_applyButton, "EDIT / APPLY STATE BINDINGS", ApplyAssignments, Panel2);
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
        return panel;
    }

    private UIElement BuildBottomWorkspace()
    {
        var grid = new Grid { Margin = new Thickness(14, 0, 14, 8) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(330) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(510) });

        var warnings = Card();
        warnings.Padding = new Thickness(10);
        var warningGrid = new Grid();
        warningGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        warningGrid.RowDefinitions.Add(new RowDefinition());
        warnings.Child = warningGrid;
        warningGrid.Children.Add(PanelTitle("CONFLICTS & WARNINGS"));
        StyleList(_warnings);
        _warnings.Margin = new Thickness(0, 7, 0, 0);
        Grid.SetRow(_warnings, 1);
        warningGrid.Children.Add(_warnings);
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
        _actionSearch.ToolTip = "Search actions";
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
        var detailStack = new StackPanel();
        _browserActionTitle.FontWeight = FontWeights.SemiBold;
        _browserActionTitle.TextWrapping = TextWrapping.Wrap;
        _browserActionBody.Foreground = Muted;
        _browserActionBody.TextWrapping = TextWrapping.Wrap;
        _browserActionBody.Margin = new Thickness(0, 6, 0, 0);
        detailStack.Children.Add(_browserActionTitle);
        detailStack.Children.Add(_browserActionBody);
        detail.Child = detailStack;
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
        var stateScroll = new ScrollViewer { Content = _stateOverview, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled, Margin = new Thickness(0, 7, 0, 0) };
        Grid.SetRow(stateScroll, 1);
        overviewGrid.Children.Add(stateScroll);
        grid.Children.Add(overview);
        return grid;
    }

    private UIElement BuildFooter()
    {
        var footer = new Border { Background = Rail, BorderBrush = Border, BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(14, 0) };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
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
        return footer;
    }
}
