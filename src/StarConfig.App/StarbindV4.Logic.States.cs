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
    private void BuildStateRows()
    {
        _stateRows.Children.Clear();
        _stateAssignments.Clear();
        _statePickers.Clear();
        _stateChecks.Clear();
        if (_profile is null || _selectedControl is null)
        {
            _stateRows.Children.Add(new TextBlock { Text = "Select a profile and control.", Foreground = Muted });
            return;
        }
        var existing = CurrentAssignments(_selectedControl).ToList();
        foreach (var context in Contexts.Where(c => _profile.Actions.Any(a => a.Context.Equals(c, StringComparison.OrdinalIgnoreCase))))
        {
            var choices = _profile.Actions.Where(a => a.Context.Equals(context, StringComparison.OrdinalIgnoreCase))
                .GroupBy(a => $"{a.ActionMap}|{a.ActionOrdinal}|{a.ActionName}").Select(g => g.First())
                .OrderBy(a => a.Category).ThenBy(a => a.DisplayName).ToList();
            var current = existing.Where(a => a.Context.Equals(context, StringComparison.OrdinalIgnoreCase)).ToList();
            var assignment = new StateAssignment
            {
                Context = context,
                Choices = choices,
                ExistingAssignments = current,
                Enabled = current.Count > 0,
                SelectedAction = current.Count == 0 ? null : choices.FirstOrDefault(choice => SameAction(choice, current[0]))
            };
            SetAssignmentStatus(assignment);
            _stateAssignments[context] = assignment;
            var row = new Grid { Margin = new Thickness(0, 0, 0, 5) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(25) });
            var check = new CheckBox { Content = context, Foreground = Text, VerticalAlignment = VerticalAlignment.Center, IsChecked = assignment.Enabled, Tag = assignment };
            check.Checked += StateCheckChanged;
            check.Unchecked += StateCheckChanged;
            _stateChecks[context] = check;
            row.Children.Add(check);
            var picker = new ComboBox { ItemsSource = choices, DisplayMemberPath = nameof(StarbindAction.DisplayName), SelectedItem = assignment.SelectedAction, Background = Field, Foreground = Text, BorderBrush = Border, Padding = new Thickness(5, 3, 5, 3), Tag = assignment };
            picker.SelectionChanged += StatePickerChanged;
            _statePickers[context] = picker;
            Grid.SetColumn(picker, 1);
            row.Children.Add(picker);
            var dot = new ShapeEllipse { Width = 9, Height = 9, Fill = assignment.HasConflict ? Amber : assignment.Enabled ? Green : Faint, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Tag = context };
            Grid.SetColumn(dot, 2);
            row.Children.Add(dot);
            _stateRows.Children.Add(row);
        }
        var actions = _profile.Actions.GroupBy(a => $"{a.ActionMap}|{a.ActionOrdinal}|{a.ActionName}").Select(g => g.First()).OrderBy(a => a.Context).ThenBy(a => a.DisplayName).ToList();
        _primaryActionPicker.ItemsSource = actions;
        _primaryActionPicker.DisplayMemberPath = nameof(StarbindAction.DisplayName);
    }

    private void StateCheckChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox { Tag: StateAssignment assignment } check) return;
        assignment.Enabled = check.IsChecked == true;
        SetAssignmentStatus(assignment);
        BuildWarnings();
        BuildStateOverview();
    }

    private void StatePickerChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelectionEvents || sender is not ComboBox { Tag: StateAssignment assignment } picker) return;
        assignment.SelectedAction = picker.SelectedItem as StarbindAction;
        assignment.Enabled = assignment.SelectedAction is not null;
        if (_stateChecks.TryGetValue(assignment.Context, out var check)) check.IsChecked = assignment.Enabled;
        SetAssignmentStatus(assignment);
        if (assignment.SelectedAction is not null) SelectAction(assignment.SelectedAction);
        BuildWarnings();
        BuildStateOverview();
    }

    private void PrimaryActionChanged()
    {
        if (_suppressSelectionEvents || _primaryActionPicker.SelectedItem is not StarbindAction action || _selectedControl is null) return;
        SelectAction(action);
        if (_stateAssignments.TryGetValue(action.Context, out var primary))
        {
            primary.SelectedAction = action;
            primary.Enabled = true;
            _statePickers[action.Context].SelectedItem = action;
            _stateChecks[action.Context].IsChecked = true;
        }
        if (!string.IsNullOrWhiteSpace(action.Intent))
        {
            foreach (var state in _stateAssignments.Values.Where(x => !x.Context.Equals(action.Context, StringComparison.OrdinalIgnoreCase) && x.ExistingAssignments.Count == 0))
            {
                var peer = state.Choices.FirstOrDefault(candidate => candidate.Intent.Equals(action.Intent, StringComparison.OrdinalIgnoreCase) && candidate.Behavior.Equals(action.Behavior, StringComparison.OrdinalIgnoreCase));
                if (peer is null) continue;
                state.SelectedAction = peer;
                state.Enabled = true;
                _statePickers[state.Context].SelectedItem = peer;
                _stateChecks[state.Context].IsChecked = true;
                state.Status = "SUGGESTED";
                state.StatusBrush = Green;
            }
        }
        BuildWarnings();
        BuildStateOverview();
        SetStatus($"Suggested compatible {action.Behavior.ToLowerInvariant()} actions for '{action.Intent}'. Review each checked state before applying.");
    }

    private static void SetAssignmentStatus(StateAssignment assignment)
    {
        if (!assignment.Enabled) { assignment.Status = "OFF"; assignment.StatusBrush = Faint; }
        else if (assignment.HasConflict) { assignment.Status = "CONFLICT"; assignment.StatusBrush = Amber; }
        else if (assignment.SelectedAction is null) { assignment.Status = "MISSING"; assignment.StatusBrush = Red; }
        else if (assignment.ExistingAssignments.Any(x => x.ActionName == assignment.SelectedAction.ActionName)) { assignment.Status = "BOUND"; assignment.StatusBrush = Green; }
        else { assignment.Status = "PENDING"; assignment.StatusBrush = Blue; }
    }

    private void SelectAction(StarbindAction action)
    {
        _selectedAction = action;
        _actionDescription.Text = action.Description;
        _browserActionTitle.Text = $"{action.DisplayName} ({action.Context})";
        _browserActionBody.Text = $"{action.Description}\n\nCategory: {action.Category}\nBehavior: {action.Behavior}\nRaw action: {action.ActionName}";
        _similarActions.Children.Clear();
        var peers = _profile?.Actions.Where(x => x.Identity != action.Identity && !string.IsNullOrWhiteSpace(action.Intent) && x.Intent.Equals(action.Intent, StringComparison.OrdinalIgnoreCase))
            .GroupBy(x => $"{x.Context}|{x.ActionMap}|{x.ActionName}").Select(g => g.First()).OrderBy(x => x.Context).Take(6).ToList() ?? [];
        if (peers.Count == 0) _similarActions.Children.Add(new TextBlock { Text = "No compatible semantic peers.", Foreground = Faint });
        foreach (var peer in peers) _similarActions.Children.Add(new TextBlock { Text = $"{peer.Context}: {peer.DisplayName} [{peer.Behavior}]", Foreground = peer.Behavior == action.Behavior ? Muted : Amber, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 1, 0, 1) });
    }

}
