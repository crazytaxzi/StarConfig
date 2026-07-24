using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ShapeEllipse = System.Windows.Shapes.Ellipse;

namespace StarConfig;

public sealed partial class StarbindV5Window
{
    private void BuildActionBrowser()
    {
        _actionTree.Items.Clear();
        if (_profile is null) return;
        var term = _actionSearch.Text.Trim();
        var actions = UniqueActions(_profile.Actions)
            .Where(action => string.IsNullOrWhiteSpace(term)
                || new[] { action.DisplayName, action.ActionName, action.ActionMap, action.Context, action.Category, action.Intent, action.Behavior, action.Description }
                    .Any(value => value.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        foreach (var context in ContextOrder.Where(context => actions.Any(action => action.Context.Equals(context, StringComparison.OrdinalIgnoreCase))))
        {
            var contextNode = TreeGroup(ContextGlyph(context) + "  " + context.ToUpperInvariant());
            foreach (var categoryGroup in actions.Where(action => action.Context.Equals(context, StringComparison.OrdinalIgnoreCase)).GroupBy(action => action.Category).OrderBy(group => group.Key))
            {
                var categoryNode = TreeGroup(categoryGroup.Key);
                foreach (var action in categoryGroup.OrderBy(action => action.DisplayName))
                {
                    var header = new DockPanel { LastChildFill = true };
                    var behavior = new Border
                    {
                        Background = BehaviorBrush(action.Behavior),
                        BorderBrush = Border,
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(3),
                        Padding = new Thickness(5, 1, 5, 1),
                        Margin = new Thickness(7, 0, 0, 0),
                        Child = new TextBlock { Text = action.Behavior.ToUpperInvariant(), FontSize = 8, Foreground = Muted }
                    };
                    DockPanel.SetDock(behavior, Dock.Right);
                    header.Children.Add(behavior);
                    header.Children.Add(new TextBlock { Text = action.DisplayName, TextTrimming = TextTrimming.CharacterEllipsis });
                    categoryNode.Items.Add(new TreeViewItem { Header = header, Tag = action, Foreground = Text, Padding = new Thickness(2) });
                }
                categoryNode.IsExpanded = !string.IsNullOrWhiteSpace(term) || categoryGroup.Key == "Movement";
                contextNode.Items.Add(categoryNode);
            }
            contextNode.IsExpanded = !string.IsNullOrWhiteSpace(term) || context == "Flight";
            _actionTree.Items.Add(contextNode);
        }
    }

    private void BrowserActionChanged()
    {
        if (_actionTree.SelectedItem is not TreeViewItem item || item.Tag is not StarbindAction action) return;
        SelectAction(action);
        _suppressUi = true;
        _primaryActionPicker.SelectedItem = FindActionChoice(_primaryActionPicker, action);
        _suppressUi = false;
    }

    private void SelectAction(StarbindAction action)
    {
        _selectedAction = action;
        var explanation = ActionExplanation(action);
        _actionDescription.Text = explanation;
        _browserActionTitle.Text = $"{action.DisplayName} ({action.Context})";
        _browserActionBody.Text = $"{explanation}\n\nCategory: {action.Category}\nBehavior: {action.Behavior}\nIntent: {action.Intent}\nAction map: {action.ActionMap}\nRaw action: {action.ActionName}";
        _similarActions.Children.Clear();
        var peers = _profile is null ? [] : UniqueActions(_profile.Actions)
            .Where(candidate => candidate.Identity != action.Identity && !string.IsNullOrWhiteSpace(action.Intent) && candidate.Intent.Equals(action.Intent, StringComparison.OrdinalIgnoreCase))
            .OrderBy(candidate => candidate.Context)
            .ThenBy(candidate => candidate.Behavior)
            .Take(8)
            .ToList();
        if (peers.Count == 0)
        {
            _similarActions.Children.Add(new TextBlock { Text = "No known semantic peers. This action stays independent.", Foreground = Faint, TextWrapping = TextWrapping.Wrap });
        }
        else
        {
            foreach (var peer in peers)
            {
                var compatible = peer.Behavior.Equals(action.Behavior, StringComparison.OrdinalIgnoreCase);
                _similarActions.Children.Add(new TextBlock
                {
                    Text = $"{peer.Context}: {peer.DisplayName} [{peer.Behavior}]",
                    Foreground = compatible ? Muted : Amber,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 1, 0, 1),
                    ToolTip = compatible ? "Compatible behavior" : "Similar intent, different behavior. Starbind will not merge these automatically."
                });
            }
        }
        _compareButton.IsEnabled = peers.Count > 0;
    }

    private void BuildStateRows()
    {
        _stateRows.Children.Clear();
        _visibleStates.Clear();
        _statePickers.Clear();
        _stateChecks.Clear();
        if (_profile is null || _selectedControl is null)
        {
            _stateRows.Children.Add(new TextBlock { Text = "Select a profile and physical control.", Foreground = Muted });
            return;
        }

        var plan = GetOrCreatePlan(_selectedControl);
        foreach (var context in ContextOrder.Where(context => _profile.Actions.Any(action => action.Context.Equals(context, StringComparison.OrdinalIgnoreCase))))
        {
            var state = plan.States[context];
            _visibleStates[context] = state;
            var row = new Grid { Margin = new Thickness(0, 0, 0, 5) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(76) });
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(26) });
            var check = new CheckBox { Content = context, Foreground = Text, VerticalAlignment = VerticalAlignment.Center, IsChecked = state.Enabled, Tag = state };
            check.Checked += StateCheckChanged;
            check.Unchecked += StateCheckChanged;
            _stateChecks[context] = check;
            row.Children.Add(check);
            var choices = state.Choices.Select(action => new ActionChoice(action, $"{action.DisplayName} [{action.Behavior}]")).ToList();
            var picker = new ComboBox
            {
                ItemsSource = choices,
                SelectedItem = state.Action is null ? null : choices.FirstOrDefault(choice => SameAction(choice.Action, state.Action)),
                Background = Field,
                Foreground = Text,
                BorderBrush = Border,
                Padding = new Thickness(5, 3, 5, 3),
                Tag = state,
                ToolTip = "Choose the action for this game state"
            };
            picker.SelectionChanged += StatePickerChanged;
            _statePickers[context] = picker;
            Grid.SetColumn(picker, 1);
            row.Children.Add(picker);
            var dot = new ShapeEllipse { Width = 9, Height = 9, Fill = StateBrush(state), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            dot.ToolTip = state.Status;
            Grid.SetColumn(dot, 2);
            row.Children.Add(dot);
            _stateRows.Children.Add(row);
        }

        var allActions = UniqueActions(_profile.Actions)
            .OrderBy(action => ContextIndex(action.Context))
            .ThenBy(action => action.Category)
            .ThenBy(action => action.DisplayName)
            .Select(action => new ActionChoice(action, $"{action.DisplayName} ({action.Context}) [{action.Behavior}]")).ToList();
        _suppressUi = true;
        _primaryActionPicker.ItemsSource = allActions;
        _primaryActionPicker.SelectedItem = _selectedAction is null ? null : allActions.FirstOrDefault(choice => SameAction(choice.Action, _selectedAction));
        _suppressUi = false;
    }

    private ControlBindingPlan GetOrCreatePlan(StarbindControl control)
    {
        if (_pendingPlans.TryGetValue(control.Input, out var existingPlan)) return existingPlan;
        var plan = new ControlBindingPlan { Input = control.Input, FriendlyName = control.DisplayName };
        if (_profile is null) return plan;
        var current = CurrentAssignments(control).ToList();
        foreach (var context in ContextOrder.Where(context => _profile.Actions.Any(action => action.Context.Equals(context, StringComparison.OrdinalIgnoreCase))))
        {
            var choices = UniqueActions(_profile.Actions.Where(action => action.Context.Equals(context, StringComparison.OrdinalIgnoreCase))).OrderBy(action => action.Category).ThenBy(action => action.DisplayName).ToList();
            var existing = current.Where(action => action.Context.Equals(context, StringComparison.OrdinalIgnoreCase)).ToList();
            var selected = existing.Count == 0 ? null : choices.FirstOrDefault(choice => SameAction(choice, existing[0]));
            var state = new PlannedStateBinding
            {
                Context = context,
                Choices = choices,
                Existing = existing,
                Enabled = existing.Count > 0,
                Action = selected
            };
            UpdateStateStatus(state);
            plan.States[context] = state;
        }
        _pendingPlans[control.Input] = plan;
        return plan;
    }

    private void StateCheckChanged(object sender, RoutedEventArgs e)
    {
        if (_suppressUi || sender is not CheckBox { Tag: PlannedStateBinding state } check) return;
        state.Enabled = check.IsChecked == true;
        if (state.Enabled && state.Action is null && state.Choices.Count > 0)
        {
            state.Action = state.Choices.FirstOrDefault();
            _suppressUi = true;
            if (_statePickers.TryGetValue(state.Context, out var picker)) picker.SelectedIndex = 0;
            _suppressUi = false;
        }
        UpdateStateStatus(state);
        MarkCurrentPlanDirty();
        BuildWarnings();
        BuildStateOverview();
    }

    private void StatePickerChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressUi || sender is not ComboBox { Tag: PlannedStateBinding state } picker) return;
        state.Action = (picker.SelectedItem as ActionChoice)?.Action;
        state.Enabled = state.Action is not null;
        _suppressUi = true;
        if (_stateChecks.TryGetValue(state.Context, out var check)) check.IsChecked = state.Enabled;
        _suppressUi = false;
        UpdateStateStatus(state);
        if (state.Action is not null) SelectAction(state.Action);
        MarkCurrentPlanDirty();
        BuildWarnings();
        BuildStateOverview();
    }

    private void PrimaryActionChanged()
    {
        if (_suppressUi || _primaryActionPicker.SelectedItem is not ActionChoice choice || _selectedControl is null) return;
        var action = choice.Action;
        SelectAction(action);
        if (_visibleStates.TryGetValue(action.Context, out var primary))
        {
            primary.Action = action;
            primary.Enabled = true;
            UpdateStateStatus(primary);
            _suppressUi = true;
            if (_statePickers.TryGetValue(action.Context, out var picker)) picker.SelectedItem = FindActionChoice(picker, action);
            if (_stateChecks.TryGetValue(action.Context, out var check)) check.IsChecked = true;
            _suppressUi = false;
        }

        if (!string.IsNullOrWhiteSpace(action.Intent))
        {
            foreach (var state in _visibleStates.Values.Where(state => !state.Context.Equals(action.Context, StringComparison.OrdinalIgnoreCase) && state.Existing.Count == 0))
            {
                var peer = state.Choices.FirstOrDefault(candidate => candidate.Intent.Equals(action.Intent, StringComparison.OrdinalIgnoreCase)
                    && candidate.Behavior.Equals(action.Behavior, StringComparison.OrdinalIgnoreCase));
                if (peer is null) continue;
                state.Action = peer;
                state.Enabled = true;
                state.Status = "SUGGESTED";
                _suppressUi = true;
                if (_statePickers.TryGetValue(state.Context, out var picker)) picker.SelectedItem = FindActionChoice(picker, peer);
                if (_stateChecks.TryGetValue(state.Context, out var check)) check.IsChecked = true;
                _suppressUi = false;
            }
        }
        MarkCurrentPlanDirty();
        BuildWarnings();
        BuildStateOverview();
        SetStatus($"Suggested compatible {action.Behavior.ToLowerInvariant()} actions for '{action.Intent}'. Nothing is saved until you confirm and use Save.");
    }

    private void BuildStateOverview()
    {
        _stateOverview.Children.Clear();
        if (_selectedControl is null) return;
        foreach (var state in _visibleStates.Values)
        {
            var card = new Border
            {
                Width = 122,
                Height = 180,
                Margin = new Thickness(0, 0, 6, 0),
                Padding = new Thickness(8),
                Background = Field,
                BorderBrush = state.Enabled ? Blue : Border,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4)
            };
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = ContextGlyph(state.Context) + "  " + state.Context.ToUpperInvariant(), FontWeight = FontWeights.Bold, Foreground = state.Enabled ? Cyan : Muted });
            stack.Children.Add(new TextBlock { Text = state.Enabled ? state.Action?.DisplayName ?? "Missing action" : "Not assigned", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 0), FontSize = 11 });
            stack.Children.Add(new TextBlock { Text = $"Type: {state.Action?.Behavior ?? "-"}", Foreground = Muted, Margin = new Thickness(0, 8, 0, 0), FontSize = 10 });
            stack.Children.Add(new TextBlock { Text = $"Input: {_selectedControl.Input}", Foreground = Muted, TextWrapping = TextWrapping.Wrap, FontSize = 10 });
            stack.Children.Add(new TextBlock { Text = state.Status, Foreground = StateBrush(state), Margin = new Thickness(0, 7, 0, 0), FontSize = 9, FontWeight = FontWeights.Bold });
            card.Child = stack;
            _stateOverview.Children.Add(card);
        }
    }

    private void BuildWarnings()
    {
        var warnings = new List<StarbindWarning>();
        if (_profile is null) warnings.Add(new("!", "No profile loaded.", Amber));
        else if (_selectedControl is null) warnings.Add(new("i", "Select a physical control.", Cyan));
        else
        {
            var current = EffectiveAssignments(_selectedControl).ToList();
            foreach (var group in current.GroupBy(action => action.Context).Where(group => group.Count() > 1)) warnings.Add(new("!", $"{group.Key}: {_selectedControl.DisplayName} is assigned to {group.Count()} actions.", Amber));
            var intents = current.Select(action => action.Intent).Where(intent => !string.IsNullOrWhiteSpace(intent)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (intents.Count > 1) warnings.Add(new("!", "This control performs unrelated intents across game states.", Amber));
            foreach (var state in _visibleStates.Values.Where(state => state.Enabled && state.Action is null)) warnings.Add(new("!", $"{state.Context} is enabled without an action.", Red));
            foreach (var state in _visibleStates.Values.Where(state => state.HasConflict)) warnings.Add(new("!", $"{state.Context}: this control already has {state.Existing.Count} bindings.", Amber));
            var globalDuplicates = FindConflictGroups();
            foreach (var conflict in globalDuplicates.Take(4)) warnings.Add(new("!", $"{conflict.Context}: {conflict.Input} is bound to {conflict.Actions.Count} actions.", Amber));
            if (_pendingPlans.Values.Any(plan => plan.IsDirty) || _axisTunings.Count > 0) warnings.Add(new("•", "Unsaved changes are staged. Validate or save when ready.", Blue));
            if (warnings.Count == 0) warnings.Add(new("✓", "No critical conflicts for this control.", Green));
        }
        _warnings.ItemsSource = warnings;
        _warnings.ItemTemplate = WarningTemplate();
        _resolveAllButton.IsEnabled = FindConflictGroups().Count > 0;
    }

    private IReadOnlyList<ConflictGroup> FindConflictGroups()
    {
        if (_profile is null) return [];
        return _profile.Actions.Where(action => action.IsBound)
            .GroupBy(action => $"{action.Context}|{action.Input}", StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group =>
            {
                var actions = group.ToList();
                var recommended = actions.OrderByDescending(action => KnownActionScore(action)).ThenBy(action => action.DisplayName).First();
                return new ConflictGroup(actions[0].Context, actions[0].Input, actions, recommended);
            })
            .OrderBy(group => ContextIndex(group.Context))
            .ThenBy(group => group.Input)
            .ToList();
    }

    private void MarkCurrentPlanDirty()
    {
        if (_selectedControl is null) return;
        var plan = GetOrCreatePlan(_selectedControl);
        plan.IsDirty = true;
        MarkDirty();
    }

    private void MarkDirty()
    {
        _savedText.Text = "Unsaved changes";
        _savedText.Foreground = Amber;
    }

    private static void UpdateStateStatus(PlannedStateBinding state)
    {
        state.Status = !state.Enabled ? "OFF"
            : state.HasConflict ? "CONFLICT"
            : state.Action is null ? "MISSING"
            : state.Existing.Any(existing => SameAction(existing, state.Action)) ? "BOUND"
            : state.Status == "SUGGESTED" ? "SUGGESTED"
            : "PENDING";
    }

    private static Brush StateBrush(PlannedStateBinding state) => state.Status switch
    {
        "BOUND" => Green,
        "SUGGESTED" => Green,
        "PENDING" => Blue,
        "CONFLICT" => Amber,
        "MISSING" => Red,
        _ => Faint
    };

    private static Brush BehaviorBrush(string behavior) => behavior switch
    {
        "Axis" => BlueDim,
        "Toggle" => Paint("#4A2C68"),
        "Cycle" => Paint("#5B4320"),
        "Hold" => Paint("#244C3A"),
        _ => Field
    };

    private static IReadOnlyList<StarbindAction> UniqueActions(IEnumerable<StarbindAction> actions)
        => actions.GroupBy(action => $"{action.ActionMap}|{action.ActionOrdinal}|{action.ActionName}").Select(group => group.First()).ToList();

    private static bool SameAction(StarbindAction left, StarbindAction right)
        => left.ActionOrdinal == right.ActionOrdinal
           && left.ActionMap.Equals(right.ActionMap, StringComparison.OrdinalIgnoreCase)
           && left.ActionName.Equals(right.ActionName, StringComparison.OrdinalIgnoreCase);

    private static ActionChoice? FindActionChoice(ComboBox picker, StarbindAction action)
        => (picker.ItemsSource as IEnumerable<ActionChoice>)?.FirstOrDefault(choice => SameAction(choice.Action, action));

    private static string ContextGlyph(string context) => context switch
    {
        "Flight" => "✈",
        "Vehicle" => "▣",
        "On Foot" => "♟",
        "EVA" => "◎",
        "Turret" => "⌖",
        "Mining" => "◆",
        "Salvage" => "◇",
        _ => "•"
    };

    private static int ContextIndex(string context) => Array.FindIndex(ContextOrder, item => item.Equals(context, StringComparison.OrdinalIgnoreCase)) is var index && index >= 0 ? index : 99;
    private static int KnownActionScore(StarbindAction action) => action.Description.Contains("in the", StringComparison.OrdinalIgnoreCase) ? 0 : 2;

    private static string ActionExplanation(StarbindAction action)
    {
        var distinction = action.Intent switch
        {
            "Operator Mode" => "Operator Mode changes the current task inside a master mode. It is not the same control as Master Mode.",
            "Master Mode" => "Master Mode changes the ship's broad operating mode. It is not interchangeable with Operator Mode.",
            _ => string.Empty
        };
        return string.IsNullOrWhiteSpace(distinction) ? action.Description : action.Description + "\n\n" + distinction;
    }
}
