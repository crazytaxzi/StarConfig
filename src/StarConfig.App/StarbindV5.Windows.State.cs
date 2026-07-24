using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace StarConfig;

public sealed class StateBindingDecisionWindow : Window
{
    private readonly StarbindControl _control;
    private readonly IReadOnlyList<PlannedStateBinding> _states;
    private readonly Dictionary<string, CheckBox> _checks = new(StringComparer.OrdinalIgnoreCase);
    private readonly Border _choicePanel = new();
    private readonly TextBlock _instruction = new();
    public IReadOnlyList<StateBindingDecisionResult> Results { get; private set; } = [];

    public StateBindingDecisionWindow(StarbindControl control, IReadOnlyList<PlannedStateBinding> states)
    {
        _control = control;
        _states = states;
        Title = "Duplicate / Similar Action Detected";
        Width = 760;
        Height = 590;
        MinWidth = 650;
        MinHeight = 500;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = StarbindV5Window.Bg;
        Foreground = StarbindV5Window.Text;
        FontFamily = new FontFamily("Segoe UI");
        Content = Build();
    }

    private UIElement Build()
    {
        var root = new Grid { Margin = new Thickness(16) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var heading = new StackPanel();
        heading.Children.Add(new TextBlock { Text = "DUPLICATE / SIMILAR ACTION DETECTED", Foreground = StarbindV5Window.Amber, FontWeight = FontWeights.Bold, FontSize = 14 });
        heading.Children.Add(new TextBlock
        {
            Text = $"{_control.DisplayName} ({_control.Input}) can perform related actions in several game states. Starbind will never silently merge them.",
            Foreground = StarbindV5Window.Muted,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 12)
        });
        root.Children.Add(heading);

        var summary = new Border { Background = StarbindV5Window.Panel, BorderBrush = StarbindV5Window.Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5), Padding = new Thickness(12) };
        var summaryStack = new StackPanel();
        summaryStack.Children.Add(new TextBlock { Text = "CURRENT AND SUGGESTED STATE ASSIGNMENTS", Foreground = StarbindV5Window.Cyan, FontWeight = FontWeights.Bold, FontSize = 11 });
        foreach (var state in _states.Where(state => state.Enabled || state.Action is not null))
        {
            summaryStack.Children.Add(new TextBlock
            {
                Text = $"{StateIcon(state.Context)}  {state.Context}: {state.Action?.DisplayName ?? "Not assigned"} [{state.Action?.Behavior ?? "-"}]",
                Foreground = state.HasConflict ? StarbindV5Window.Amber : StarbindV5Window.Text,
                Margin = new Thickness(0, 4, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });
        }
        summaryStack.Children.Add(new TextBlock { Text = "Do you want to keep all of these assignments?", Foreground = StarbindV5Window.Text, Margin = new Thickness(0, 12, 0, 8) });
        var decisionButtons = new StackPanel { Orientation = Orientation.Horizontal };
        var keepAll = StarbindV5Window.DialogButton("YES, KEEP ALL", StarbindV5Window.BlueDim);
        keepAll.Click += (_, _) => FinishKeepAll();
        decisionButtons.Children.Add(keepAll);
        var choose = StarbindV5Window.DialogButton("NO, LET ME CHOOSE", StarbindV5Window.Panel2);
        choose.Click += (_, _) => ShowChooser();
        decisionButtons.Children.Add(choose);
        summaryStack.Children.Add(decisionButtons);
        summary.Child = summaryStack;
        Grid.SetRow(summary, 1);
        root.Children.Add(summary);

        _choicePanel.Background = StarbindV5Window.Panel;
        _choicePanel.BorderBrush = StarbindV5Window.Blue;
        _choicePanel.BorderThickness = new Thickness(1);
        _choicePanel.CornerRadius = new CornerRadius(5);
        _choicePanel.Padding = new Thickness(12);
        _choicePanel.Margin = new Thickness(0, 12, 0, 0);
        _choicePanel.Visibility = Visibility.Collapsed;
        _choicePanel.Child = BuildChooser();
        Grid.SetRow(_choicePanel, 2);
        root.Children.Add(_choicePanel);

        var footer = new Grid { Margin = new Thickness(0, 12, 0, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition());
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _instruction.Text = "Choose Keep All or Let Me Choose.";
        _instruction.Foreground = StarbindV5Window.Muted;
        _instruction.VerticalAlignment = VerticalAlignment.Center;
        footer.Children.Add(_instruction);
        var cancel = StarbindV5Window.DialogButton("CANCEL", StarbindV5Window.Panel2);
        cancel.Click += (_, _) => { DialogResult = false; Close(); };
        Grid.SetColumn(cancel, 1);
        footer.Children.Add(cancel);
        var confirm = StarbindV5Window.DialogButton("CONFIRM", StarbindV5Window.BlueDim);
        confirm.IsEnabled = false;
        confirm.Name = "ConfirmButton";
        confirm.Click += (_, _) => FinishChooser();
        Grid.SetColumn(confirm, 2);
        footer.Children.Add(confirm);
        Grid.SetRow(footer, 3);
        root.Children.Add(footer);
        return root;
    }

    private UIElement BuildChooser()
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition());
        grid.Children.Add(new TextBlock
        {
            Text = "REMOVE FROM WHICH STATE?\nSelect the states that should KEEP this physical control. Unchecked states will have this input removed when you save.",
            Foreground = StarbindV5Window.Text,
            TextWrapping = TextWrapping.Wrap
        });
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 10, 0, 0) };
        var rows = new StackPanel();
        foreach (var state in _states)
        {
            var card = new Border { Background = StarbindV5Window.Field, BorderBrush = state.HasConflict ? StarbindV5Window.Amber : StarbindV5Window.Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4), Padding = new Thickness(9), Margin = new Thickness(0, 0, 0, 6) };
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
            row.ColumnDefinitions.Add(new ColumnDefinition());
            var check = new CheckBox { Content = state.Context, IsChecked = state.Enabled, Foreground = StarbindV5Window.Text, VerticalAlignment = VerticalAlignment.Center };
            _checks[state.Context] = check;
            row.Children.Add(check);
            var detail = new StackPanel();
            detail.Children.Add(new TextBlock { Text = state.Action?.DisplayName ?? "No action selected", Foreground = StarbindV5Window.Text, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
            detail.Children.Add(new TextBlock { Text = state.Action is null ? "This state remains unassigned." : $"{state.Action.Behavior} • {state.Action.Intent}", Foreground = state.HasConflict ? StarbindV5Window.Amber : StarbindV5Window.Muted, FontSize = 10, TextWrapping = TextWrapping.Wrap });
            Grid.SetColumn(detail, 1);
            row.Children.Add(detail);
            card.Child = row;
            rows.Children.Add(card);
        }
        scroll.Content = rows;
        Grid.SetRow(scroll, 1);
        grid.Children.Add(scroll);
        return grid;
    }

    private void ShowChooser()
    {
        _choicePanel.Visibility = Visibility.Visible;
        _instruction.Text = "Unchecked states will lose this physical input. Actions themselves are not deleted.";
        if (FindName("ConfirmButton") is Button confirm) confirm.IsEnabled = true;
    }

    private void FinishKeepAll()
    {
        Results = _states.Select(state => new StateBindingDecisionResult(state.Context, state.Action is not null, state.Action)).ToList();
        DialogResult = true;
        Close();
    }

    private void FinishChooser()
    {
        Results = _states.Select(state => new StateBindingDecisionResult(state.Context, _checks.TryGetValue(state.Context, out var check) && check.IsChecked == true && state.Action is not null, state.Action)).ToList();
        DialogResult = true;
        Close();
    }

    private static string StateIcon(string context) => context switch { "Flight" => "✈", "Vehicle" => "▣", "On Foot" => "♟", "EVA" => "◎", "Turret" => "⌖", "Mining" => "◆", "Salvage" => "◇", _ => "•" };
}

public sealed record StateBindingDecisionResult(string Context, bool Enabled, StarbindAction? Action);

public sealed class ActionComparisonWindow : Window
{
    public ActionComparisonWindow(StarbindAction selected, IReadOnlyList<StarbindAction> peers)
    {
        Title = "Compare Actions";
        Width = 920;
        Height = 620;
        MinWidth = 760;
        MinHeight = 500;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = StarbindV5Window.Bg;
        Foreground = StarbindV5Window.Text;
        FontFamily = new FontFamily("Segoe UI");
        Content = Build(selected, peers);
    }

    private UIElement Build(StarbindAction selected, IReadOnlyList<StarbindAction> peers)
    {
        var root = new Grid { Margin = new Thickness(14) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.Children.Add(new TextBlock { Text = $"COMPARE ACTIONS: {selected.DisplayName}", FontSize = 15, FontWeight = FontWeights.Bold, Foreground = StarbindV5Window.Cyan });
        var scroll = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled, Margin = new Thickness(0, 12, 0, 0) };
        var cards = new StackPanel { Orientation = Orientation.Horizontal };
        cards.Children.Add(ActionCard(selected, true));
        foreach (var peer in peers.Take(8)) cards.Children.Add(ActionCard(peer, false));
        scroll.Content = cards;
        Grid.SetRow(scroll, 1);
        root.Children.Add(scroll);
        var close = StarbindV5Window.DialogButton("CLOSE", StarbindV5Window.Panel2);
        close.HorizontalAlignment = HorizontalAlignment.Right;
        close.Click += (_, _) => Close();
        Grid.SetRow(close, 2);
        root.Children.Add(close);
        return root;
    }

    private static UIElement ActionCard(StarbindAction action, bool selected)
    {
        var card = new Border { Width = 245, Margin = new Thickness(0, 0, 8, 0), Padding = new Thickness(12), Background = StarbindV5Window.Panel, BorderBrush = selected ? StarbindV5Window.Blue : StarbindV5Window.Border, BorderThickness = new Thickness(selected ? 2 : 1), CornerRadius = new CornerRadius(5) };
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = action.DisplayName, FontWeight = FontWeights.Bold, FontSize = 13, TextWrapping = TextWrapping.Wrap });
        stack.Children.Add(LabelValue("STATE", action.Context));
        stack.Children.Add(LabelValue("INTENT", action.Intent));
        stack.Children.Add(LabelValue("BEHAVIOR", action.Behavior));
        stack.Children.Add(LabelValue("CATEGORY", action.Category));
        stack.Children.Add(new TextBlock { Text = action.Description, Foreground = StarbindV5Window.Muted, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 0) });
        stack.Children.Add(new TextBlock { Text = action.Behavior == selected.ToString() ? "" : string.Empty });
        stack.Children.Add(new TextBlock { Text = $"Raw: {action.ActionName}", Foreground = StarbindV5Window.Faint, FontSize = 9, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 0) });
        card.Child = stack;
        return card;
    }

    private static UIElement LabelValue(string label, string value)
    {
        var stack = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
        stack.Children.Add(new TextBlock { Text = label, Foreground = StarbindV5Window.Faint, FontSize = 9 });
        stack.Children.Add(new TextBlock { Text = value, Foreground = StarbindV5Window.Text, TextWrapping = TextWrapping.Wrap });
        return stack;
    }
}

public sealed class ConflictResolverWindow : Window
{
    private readonly IReadOnlyList<ConflictGroup> _groups;
    private readonly Dictionary<ConflictGroup, ComboBox> _pickers = [];
    public IReadOnlyList<ConflictResolution> Resolutions { get; private set; } = [];

    public ConflictResolverWindow(IReadOnlyList<ConflictGroup> groups)
    {
        _groups = groups;
        Title = "Resolve Binding Conflicts";
        Width = 860;
        Height = 680;
        MinWidth = 720;
        MinHeight = 520;
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
        intro.Children.Add(new TextBlock { Text = "RESOLVE ALL DUPLICATE INPUTS", Foreground = StarbindV5Window.Amber, FontWeight = FontWeights.Bold, FontSize = 14 });
        intro.Children.Add(new TextBlock { Text = "Choose the one action that should keep each duplicated physical input. Starbind removes that input from the other actions and creates a backup first.", Foreground = StarbindV5Window.Muted, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 7, 0, 10) });
        root.Children.Add(intro);
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var rows = new StackPanel();
        foreach (var group in _groups)
        {
            var card = new Border { Background = StarbindV5Window.Panel, BorderBrush = StarbindV5Window.Amber, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5), Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 7) };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(230) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            var words = new StackPanel();
            words.Children.Add(new TextBlock { Text = $"{group.Context}  •  {group.Input}", Foreground = StarbindV5Window.Text, FontWeight = FontWeights.Bold });
            words.Children.Add(new TextBlock { Text = $"{group.Actions.Count} actions currently use this input", Foreground = StarbindV5Window.Muted, FontSize = 10 });
            grid.Children.Add(words);
            var choices = group.Actions.Select(action => new ActionChoice(action, $"{action.DisplayName} [{action.Behavior}]")).ToList();
            var picker = new ComboBox { ItemsSource = choices, SelectedItem = choices.First(choice => ReferenceEquals(choice.Action, group.Recommended) || SameAction(choice.Action, group.Recommended)), Background = StarbindV5Window.Field, Foreground = StarbindV5Window.Text, BorderBrush = StarbindV5Window.Border, Padding = new Thickness(6, 4, 6, 4) };
            _pickers[group] = picker;
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
        var confirm = StarbindV5Window.DialogButton("RESOLVE SELECTED CONFLICTS", StarbindV5Window.BlueDim);
        confirm.Click += (_, _) =>
        {
            Resolutions = _groups.Select(group => new ConflictResolution(group.Context, group.Input, ((_pickers[group].SelectedItem as ActionChoice) ?? throw new InvalidOperationException("A conflict choice is missing.")).Action)).ToList();
            DialogResult = true;
            Close();
        };
        buttons.Children.Add(confirm);
        Grid.SetRow(buttons, 2);
        root.Children.Add(buttons);
        return root;
    }

    private static bool SameAction(StarbindAction left, StarbindAction right) => left.ActionOrdinal == right.ActionOrdinal && left.ActionMap.Equals(right.ActionMap, StringComparison.OrdinalIgnoreCase) && left.ActionName.Equals(right.ActionName, StringComparison.OrdinalIgnoreCase);
}
