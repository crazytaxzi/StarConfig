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
    private UIElement BuildInterface()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(72) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(88) });
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(300) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(54) });

        root.Children.Add(BuildHeader());
        var deviceStrip = BuildDeviceStrip();
        Grid.SetRow(deviceStrip, 1);
        root.Children.Add(deviceStrip);
        var workspace = BuildWorkspace();
        Grid.SetRow(workspace, 2);
        root.Children.Add(workspace);
        var bottom = BuildBottomDeck();
        Grid.SetRow(bottom, 3);
        root.Children.Add(bottom);
        var footer = BuildFooter();
        Grid.SetRow(footer, 4);
        root.Children.Add(footer);
        return root;
    }

    private UIElement BuildHeader()
    {
        var bar = new Border { Background = RailBg, BorderBrush = Border, BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(18, 0, 18, 0) };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(320) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(165) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        bar.Child = grid;

        var brand = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var mark = new Border { Width = 36, Height = 36, CornerRadius = new CornerRadius(7), Background = BlueDim, BorderBrush = Blue, BorderThickness = new Thickness(1), Child = new TextBlock { Text = "★", Foreground = Cyan, FontSize = 22, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } };
        brand.Children.Add(mark);
        var brandWords = new StackPanel { Margin = new Thickness(10, 0, 0, 0) };
        brandWords.Children.Add(new TextBlock { Text = "STARCONFIG", FontSize = 23, FontWeight = FontWeights.Bold });
        brandWords.Children.Add(new TextBlock { Text = "STAR CITIZEN CONTROL MAPPER", Foreground = Muted, FontSize = 10 });
        brand.Children.Add(brandWords);
        grid.Children.Add(brand);

        var channel = HeaderStat("CHANNEL", _channelText);
        _channelText.Text = "NO PROFILE";
        _channelText.Foreground = Amber;
        Grid.SetColumn(channel, 1);
        grid.Children.Add(channel);

        var profileHost = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(14, 0, 14, 0) };
        profileHost.Children.Add(new TextBlock { Text = "ACTIVE PROFILE", Foreground = Faint, FontSize = 10, Margin = new Thickness(2, 0, 0, 4) });
        StyleCombo(_profilePicker);
        _profilePicker.MinWidth = 310;
        _profilePicker.SelectionChanged += (_, _) => ProfileSelectionChanged();
        profileHost.Children.Add(_profilePicker);
        Grid.SetColumn(profileHost, 2);
        grid.Children.Add(profileHost);

        var deviceStat = HeaderStat("DEVICES", _deviceCountText);
        _deviceCountText.Text = "0 CONNECTED";
        _deviceCountText.Foreground = Green;
        Grid.SetColumn(deviceStat, 3);
        grid.Children.Add(deviceStat);

        var actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        actions.Children.Add(FlatButton("OPEN PROFILE", BrowseForProfile));
        actions.Children.Add(FlatButton("REFRESH", (_, _) => RefreshEverything()));
        Grid.SetColumn(actions, 4);
        grid.Children.Add(actions);
        return bar;
    }

    private UIElement BuildDeviceStrip()
    {
        var shell = new Border { Background = WindowBg, Padding = new Thickness(16, 10, 16, 10) };
        shell.Child = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = _deviceStrip };
        return shell;
    }

    private UIElement BuildWorkspace()
    {
        var grid = new Grid { Margin = new Thickness(16, 0, 16, 10) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(500) });

        var controlPanel = Card();
        controlPanel.Padding = new Thickness(12);
        Grid.SetColumn(controlPanel, 0);
        var controlGrid = new Grid();
        controlGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        controlGrid.RowDefinitions.Add(new RowDefinition());
        controlPanel.Child = controlGrid;
        controlGrid.Children.Add(PanelTitle("DEVICE CONTROLS"));
        StyleTree(_controlTree);
        _controlTree.SelectedItemChanged += (_, _) => ControlTreeSelectionChanged();
        _controlTree.Margin = new Thickness(0, 10, 0, 0);
        Grid.SetRow(_controlTree, 1);
        controlGrid.Children.Add(_controlTree);
        grid.Children.Add(controlPanel);

        var visualPanel = Card();
        visualPanel.Margin = new Thickness(10, 0, 10, 0);
        Grid.SetColumn(visualPanel, 1);
        var visualGrid = new Grid();
        visualGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(58) });
        visualGrid.RowDefinitions.Add(new RowDefinition());
        visualGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(46) });
        visualPanel.Child = visualGrid;
        var visualHeader = new Grid { Margin = new Thickness(16, 0, 16, 0) };
        visualHeader.ColumnDefinitions.Add(new ColumnDefinition());
        visualHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var headingStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        _deviceHeading.Text = "SELECT A DEVICE";
        _deviceHeading.FontSize = 16;
        _deviceHeading.FontWeight = FontWeights.SemiBold;
        _deviceSubheading.Foreground = Muted;
        _deviceSubheading.FontSize = 11;
        headingStack.Children.Add(_deviceHeading);
        headingStack.Children.Add(_deviceSubheading);
        visualHeader.Children.Add(headingStack);
        var visualTools = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        ConfigureButton(_listenButton, "LISTEN FOR INPUT", ListenForInput, BlueDim);
        visualTools.Children.Add(_listenButton);
        Grid.SetColumn(visualTools, 1);
        visualHeader.Children.Add(visualTools);
        visualGrid.Children.Add(visualHeader);
        _deviceVisualHost.Background = FieldBg;
        Grid.SetRow(_deviceVisualHost, 1);
        visualGrid.Children.Add(_deviceVisualHost);
        var visualFooter = new Grid { Margin = new Thickness(14, 6, 14, 6) };
        visualFooter.ColumnDefinitions.Add(new ColumnDefinition());
        visualFooter.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        visualFooter.Children.Add(new TextBlock { Text = "Select a control on the diagram or in the control tree. The center view shows useful hotspots without turning into a button spreadsheet.", Foreground = Muted, FontSize = 11, VerticalAlignment = VerticalAlignment.Center });
        var zoom = new TextBlock { Text = "FIT TO DEVICE", Foreground = Faint, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(zoom, 1);
        visualFooter.Children.Add(zoom);
        Grid.SetRow(visualFooter, 2);
        visualGrid.Children.Add(visualFooter);
        grid.Children.Add(visualPanel);

        var right = BuildAssignmentRail();
        Grid.SetColumn(right, 2);
        grid.Children.Add(right);
        return grid;
    }

    private UIElement BuildAssignmentRail()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        var stack = new StackPanel();
        scroll.Content = stack;

        var controlCard = Card();
        controlCard.Padding = new Thickness(14);
        var controlStack = new StackPanel();
        controlCard.Child = controlStack;
        controlStack.Children.Add(PanelTitle("SELECTED CONTROL"));
        _selectedControlName.Text = "No control selected";
        _selectedControlName.FontSize = 18;
        _selectedControlName.FontWeight = FontWeights.SemiBold;
        _selectedControlName.Margin = new Thickness(0, 10, 0, 2);
        controlStack.Children.Add(_selectedControlName);
        _selectedControlInput.Foreground = Cyan;
        controlStack.Children.Add(_selectedControlInput);
        var detailGrid = new Grid { Margin = new Thickness(0, 12, 0, 0) };
        detailGrid.ColumnDefinitions.Add(new ColumnDefinition());
        detailGrid.ColumnDefinitions.Add(new ColumnDefinition());
        detailGrid.Children.Add(SmallStat("CONTROL TYPE", _selectedControlType));
        var useStack = SmallStat("CURRENT ASSIGNMENTS", _selectedControlUsage);
        Grid.SetColumn(useStack, 1);
        detailGrid.Children.Add(useStack);
        controlStack.Children.Add(detailGrid);
        controlStack.Children.Add(new TextBlock { Text = "CURRENT RESPONSE", Foreground = Faint, FontSize = 10, Margin = new Thickness(0, 12, 0, 4) });
        _responseTrack.Height = 8;
        _responseTrack.Background = FieldBg;
        _responseTrack.BorderBrush = Border;
        _responseTrack.BorderThickness = new Thickness(1);
        _responseTrack.CornerRadius = new CornerRadius(4);
        var responseGrid = new Grid();
        _responseFill.Background = Blue;
        _responseFill.HorizontalAlignment = HorizontalAlignment.Left;
        _responseFill.Width = 0;
        _responseFill.CornerRadius = new CornerRadius(3);
        responseGrid.Children.Add(_responseFill);
        _responseTrack.Child = responseGrid;
        controlStack.Children.Add(_responseTrack);
        stack.Children.Add(controlCard);

        var assignments = Card();
        assignments.Padding = new Thickness(14);
        assignments.Margin = new Thickness(0, 10, 0, 0);
        var assignmentStack = new StackPanel();
        assignments.Child = assignmentStack;
        assignmentStack.Children.Add(PanelTitle("ACTION ASSIGNMENT"));
        assignmentStack.Children.Add(new TextBlock { Text = "Each row is a real game state. Existing assignments load first. Compatible actions are suggested, never silently merged.", Foreground = Muted, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 10) });
        assignmentStack.Children.Add(_stateBindingPanel);
        ConfigureButton(_applyButton, "APPLY CHECKED STATE BINDINGS", ApplyStateBindings, BlueDim);
        _applyButton.HorizontalAlignment = HorizontalAlignment.Stretch;
        _applyButton.Margin = new Thickness(0, 10, 0, 0);
        assignmentStack.Children.Add(_applyButton);
        stack.Children.Add(assignments);

        var about = Card();
        about.Padding = new Thickness(14);
        about.Margin = new Thickness(0, 10, 0, 0);
        var aboutStack = new StackPanel();
        about.Child = aboutStack;
        aboutStack.Children.Add(PanelTitle("ABOUT THIS ACTION"));
        _aboutActionName.Text = "Choose an action from the browser";
        _aboutActionName.FontWeight = FontWeights.SemiBold;
        _aboutActionName.Margin = new Thickness(0, 8, 0, 4);
        aboutStack.Children.Add(_aboutActionName);
        _aboutActionBody.Foreground = Muted;
        _aboutActionBody.TextWrapping = TextWrapping.Wrap;
        aboutStack.Children.Add(_aboutActionBody);
        aboutStack.Children.Add(new TextBlock { Text = "SIMILAR ACTIONS", Foreground = Faint, FontSize = 10, Margin = new Thickness(0, 12, 0, 4) });
        aboutStack.Children.Add(_similarActionsPanel);
        stack.Children.Add(about);
        return scroll;
    }

    private UIElement BuildBottomDeck()
    {
        var grid = new Grid { Margin = new Thickness(16, 0, 16, 10) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(500) });

        var warnings = Card();
        warnings.Padding = new Thickness(12);
        var warningGrid = new Grid();
        warningGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        warningGrid.RowDefinitions.Add(new RowDefinition());
        warnings.Child = warningGrid;
        warningGrid.Children.Add(PanelTitle("CONFLICTS & WARNINGS"));
        StyleList(_warningList);
        _warningList.Margin = new Thickness(0, 8, 0, 0);
        Grid.SetRow(_warningList, 1);
        warningGrid.Children.Add(_warningList);
        grid.Children.Add(warnings);

        var browser = Card();
        browser.Padding = new Thickness(12);
        browser.Margin = new Thickness(10, 0, 10, 0);
        Grid.SetColumn(browser, 1);
        var browserGrid = new Grid();
        browserGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        browserGrid.RowDefinitions.Add(new RowDefinition());
        browser.Child = browserGrid;
        var browserHeader = new Grid();
        browserHeader.ColumnDefinitions.Add(new ColumnDefinition());
        browserHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) });
        browserHeader.Children.Add(PanelTitle("ACTION EXPLANATION BROWSER"));
        StyleTextBox(_actionSearch);
        _actionSearch.ToolTip = "Search actions by plain language, raw name, state, behavior, or current input";
        _actionSearch.TextChanged += (_, _) => RebuildActionBrowser();
        Grid.SetColumn(_actionSearch, 1);
        browserHeader.Children.Add(_actionSearch);
        browserGrid.Children.Add(browserHeader);
        StyleTree(_actionTree);
        _actionTree.SelectedItemChanged += (_, _) => ActionTreeSelectionChanged();
        _actionTree.Margin = new Thickness(0, 8, 0, 0);
        Grid.SetRow(_actionTree, 1);
        browserGrid.Children.Add(_actionTree);

        var emptyStack = new StackPanel();
        _emptyProfileText.Text = "No exported Star Citizen control profile is loaded. StarConfig can browse directly to any layout_*_exported.xml file.";
        _emptyProfileText.TextWrapping = TextWrapping.Wrap;
        _emptyProfileText.Foreground = Muted;
        _emptyProfileText.Margin = new Thickness(0, 0, 0, 10);
        emptyStack.Children.Add(_emptyProfileText);
        emptyStack.Children.Add(PrimaryButton("OPEN PROFILE XML", BrowseForProfile));
        _emptyProfileBanner.Background = PanelAlt;
        _emptyProfileBanner.BorderBrush = Amber;
        _emptyProfileBanner.BorderThickness = new Thickness(1);
        _emptyProfileBanner.CornerRadius = new CornerRadius(6);
        _emptyProfileBanner.Padding = new Thickness(18);
        _emptyProfileBanner.HorizontalAlignment = HorizontalAlignment.Center;
        _emptyProfileBanner.VerticalAlignment = VerticalAlignment.Center;
        _emptyProfileBanner.MaxWidth = 560;
        _emptyProfileBanner.Child = emptyStack;
        Grid.SetRow(_emptyProfileBanner, 1);
        browserGrid.Children.Add(_emptyProfileBanner);
        grid.Children.Add(browser);

        var overview = Card();
        overview.Padding = new Thickness(12);
        Grid.SetColumn(overview, 2);
        var overviewGrid = new Grid();
        overviewGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        overviewGrid.RowDefinitions.Add(new RowDefinition());
        overview.Child = overviewGrid;
        overviewGrid.Children.Add(PanelTitle("STATE OVERVIEW FOR SELECTED CONTROL"));
        var overviewScroll = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = _stateOverview, Margin = new Thickness(0, 8, 0, 0) };
        Grid.SetRow(overviewScroll, 1);
        overviewGrid.Children.Add(overviewScroll);
        grid.Children.Add(overview);
        return grid;
    }

    private UIElement BuildFooter()
    {
        var footer = new Border { Background = RailBg, BorderBrush = Border, BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(16, 0, 16, 0) };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footer.Child = grid;
        var status = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        status.Children.Add(new ShapeEllipse { Width = 9, Height = 9, Fill = Green, Margin = new Thickness(0, 0, 8, 0) });
        _statusText.Text = "Ready";
        status.Children.Add(_statusText);
        _saveStateText.Foreground = Muted;
        _saveStateText.Margin = new Thickness(18, 0, 0, 0);
        status.Children.Add(_saveStateText);
        grid.Children.Add(status);
        ConfigureButton(_validateButton, "VALIDATE PROFILE", ValidateProfile);
        Grid.SetColumn(_validateButton, 1);
        grid.Children.Add(_validateButton);
        ConfigureButton(_backupButton, "BACKUP PROFILE", BackupProfile);
        Grid.SetColumn(_backupButton, 2);
        grid.Children.Add(_backupButton);
        var saveOnly = FlatButton("SAVE CHANGES", (_, _) => SaveStateBindings(false));
        Grid.SetColumn(saveOnly, 3);
        grid.Children.Add(saveOnly);
        ConfigureButton(_saveLaunchButton, "SAVE & LAUNCH STAR CITIZEN", SaveAndLaunch, Green);
        _saveLaunchButton.Padding = new Thickness(18, 9, 18, 9);
        Grid.SetColumn(_saveLaunchButton, 4);
        grid.Children.Add(_saveLaunchButton);
        return footer;
    }
}
