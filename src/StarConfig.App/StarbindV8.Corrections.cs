using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace StarConfig;

public sealed partial class StarbindV5Window
{
    private readonly HashSet<string> _assistantAcknowledgedInputs = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<ComboBox> _correctedStatePickers = [];
    private Point _actionDragStart;
    private StarbindAction? _draggedAction;
    private bool _v8CorrectionsInstalled;
    private bool _v8RefreshInProgress;
    private bool _v8ActionNormalizationInProgress;
    private string? _v8NormalizedProfileToken;

    private void InstallV8Corrections()
    {
        if (_v8CorrectionsInstalled) return;
        _v8CorrectionsInstalled = true;

        Title = "Starbind 0.7.1 - Star Citizen Control Mapper";

        _view3DButton.Click -= OpenDevice3D;
        _view3DButton.Visibility = Visibility.Collapsed;

        _testButton.Click -= OpenDeviceTester;
        _testButton.Click += OpenCorrectedDeviceTester;
        _testButton.Content = "LIVE INPUT TEST";
        _testButton.ToolTip = "Open a real-time tester for the selected keyboard, mouse, joystick, throttle, pedals or gamepad.";

        _actionTree.PreviewMouseLeftButtonDown += BeginActionDrag;
        _actionTree.PreviewMouseMove += ContinueActionDrag;
        _deviceCanvasHost.AllowDrop = true;
        _deviceCanvasHost.PreviewDragOver += MappingTargetDragOver;
        _deviceCanvasHost.Drop += MappingTargetDrop;
        _controlTree.AllowDrop = true;
        _controlTree.PreviewDragOver += MappingTargetDragOver;
        _controlTree.Drop += MappingTargetDrop;

        _stateRows.LayoutUpdated += (_, _) => ApplyCorrectedStatePickers();
        _actionTree.LayoutUpdated += (_, _) => DeduplicateActionTree();
        _primaryActionPicker.DropDownOpened += (_, _) => RefreshPrimaryActionChoices();
        _profilePicker.SelectionChanged += (_, _) => Dispatcher.BeginInvoke(ApplyV8CorrectionPass, DispatcherPriority.Background);
        _deviceCards.LayoutUpdated += (_, _) => RefreshCorrectedDeviceThumbnails();

        _primaryActionPicker.SelectionChanged += (_, _) =>
        {
            if (_suppressUi || _selectedControl is null) return;
            _assistantAcknowledgedInputs.Remove(_selectedControl.Input);
        };
        _mappingAssistantConfirm.Click += (_, _) => AcknowledgeAndDismissMappingAssistant();
        _mappingAssistantHost.IsVisibleChanged += (_, _) =>
        {
            if (_mappingAssistantHost.Visibility == Visibility.Visible
                && _selectedControl is not null
                && _assistantAcknowledgedInputs.Contains(_selectedControl.Input))
            {
                Dispatcher.BeginInvoke(() => _mappingAssistantHost.Visibility = Visibility.Collapsed, DispatcherPriority.Background);
            }
        };

        Dispatcher.BeginInvoke(() =>
        {
            InstallKeepAllDismissHandler();
            ApplyV8CorrectionPass();
        }, DispatcherPriority.Loaded);
    }

    private void ApplyV8CorrectionPass()
    {
        if (_v8RefreshInProgress) return;
        _v8RefreshInProgress = true;
        try
        {
            NormalizeLoadedActionBehaviors();
            RefreshPrimaryActionChoices();
            ApplyCorrectedStatePickers();
            DeduplicateActionTree();
            InstallKeepAllDismissHandler();
            BuildCorrectedKeyboardControlTree();
            RefreshCorrectedDeviceArtwork();
            RefreshCorrectedDeviceThumbnails();
        }
        finally
        {
            _v8RefreshInProgress = false;
        }
    }

    private void NormalizeLoadedActionBehaviors()
    {
        if (_profile is null || _v8ActionNormalizationInProgress) return;
        var token = $"{_profile.FilePath}|{File.GetLastWriteTimeUtc(_profile.FilePath).Ticks}|{_profile.Actions.Count}";
        if (token.Equals(_v8NormalizedProfileToken, StringComparison.Ordinal)) return;

        _v8ActionNormalizationInProgress = true;
        try
        {
            var selectedIdentity = _selectedAction?.Identity;
            var corrected = _profile.Actions
                .Select(action =>
                {
                    var behavior = CorrectedBehavior(action);
                    return behavior.Equals(action.Behavior, StringComparison.OrdinalIgnoreCase)
                        ? action
                        : action with { Behavior = behavior };
                })
                .ToList();

            _profile = new StarbindProfile
            {
                FilePath = _profile.FilePath,
                ProfileName = _profile.ProfileName,
                Channel = _profile.Channel,
                Devices = _profile.Devices,
                Actions = corrected,
                AxisOptions = _profile.AxisOptions
            };
            _selectedAction = selectedIdentity is null ? null : corrected.FirstOrDefault(action => action.Identity == selectedIdentity);
            _pendingPlans.Clear();
            BuildActionBrowser();
            if (_selectedControl is not null)
            {
                BuildStateRows();
                BuildWarnings();
                BuildStateOverview();
            }
            _v8NormalizedProfileToken = $"{_profile.FilePath}|{File.GetLastWriteTimeUtc(_profile.FilePath).Ticks}|{_profile.Actions.Count}";
        }
        finally
        {
            _v8ActionNormalizationInProgress = false;
        }
    }

    private static string CorrectedBehavior(StarbindAction action)
    {
        var text = $"{action.ActionName} {action.DisplayName}".ToLowerInvariant();
        if (text.Contains("toggle", StringComparison.Ordinal)) return "Toggle";
        if (text.Contains("cycle", StringComparison.Ordinal)
            || text.Contains("next", StringComparison.Ordinal)
            || text.Contains("previous", StringComparison.Ordinal)
            || text.Contains("prev", StringComparison.Ordinal)) return "Cycle";
        if (text.Contains("hold", StringComparison.Ordinal)
            || text.Contains("push_to_talk", StringComparison.Ordinal)
            || text.Contains("pushtotalk", StringComparison.Ordinal)) return "Hold";
        if (action.Behavior.Equals("Axis", StringComparison.OrdinalIgnoreCase)
            || text.Contains("axis", StringComparison.Ordinal)
            || text.Contains("_abs", StringComparison.Ordinal)
            || text.Contains("strafe", StringComparison.Ordinal)
            || text.Contains("throttle", StringComparison.Ordinal)
            || text.Contains("pitch", StringComparison.Ordinal)
            || text.Contains("yaw", StringComparison.Ordinal)
            || text.Contains("roll", StringComparison.Ordinal)
            || text.Contains("elevation", StringComparison.Ordinal)
            || text.Contains("azimuth", StringComparison.Ordinal)) return "Axis";
        return action.Behavior;
    }

    private IReadOnlyList<StarbindAction> CanonicalActions()
    {
        if (_profile is null) return [];
        return _profile.Actions
            .GroupBy(action => $"{action.Context}|{action.ActionName}|{CorrectedBehavior(action)}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderByDescending(action => action.IsBound)
                .ThenBy(action => action.ActionMap)
                .First())
            .OrderBy(action => ContextIndex(action.Context))
            .ThenBy(action => action.Category)
            .ThenBy(action => action.DisplayName)
            .ToList();
    }

    private IReadOnlyList<ActionChoice> BuildCorrectedActionChoices(bool includeContext)
    {
        var actions = CanonicalActions();
        var collisions = actions
            .GroupBy(action => $"{action.DisplayName}|{action.Context}|{CorrectedBehavior(action)}", StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        return actions.Select(action =>
        {
            var behavior = CorrectedBehavior(action);
            var key = $"{action.DisplayName}|{action.Context}|{behavior}";
            var label = includeContext
                ? $"{action.DisplayName} ({action.Context}) [{behavior}]"
                : $"{action.DisplayName} [{behavior}]";
            if (collisions[key] > 1) label += $" - {StarbindText.Humanize(action.ActionMap)}";
            return new ActionChoice(action, label);
        }).ToList();
    }

    private void RefreshPrimaryActionChoices()
    {
        if (_profile is null) return;
        var selected = _selectedAction;
        var choices = BuildCorrectedActionChoices(true);
        _suppressUi = true;
        try
        {
            _primaryActionPicker.ItemsSource = choices;
            _primaryActionPicker.SelectedItem = selected is null
                ? null
                : choices.FirstOrDefault(choice => SameAction(choice.Action, selected));
        }
        finally
        {
            _suppressUi = false;
        }
    }

    private void ApplyCorrectedStatePickers()
    {
        if (_profile is null || _selectedControl is null) return;
        var choices = BuildCorrectedActionChoices(true);
        foreach (var pair in _statePickers.ToList())
        {
            var picker = pair.Value;
            if (picker.Tag is not PlannedStateBinding state) continue;
            if (_correctedStatePickers.Add(picker))
            {
                picker.SelectionChanged -= StatePickerChanged;
                picker.SelectionChanged += CorrectedStatePickerChanged;
                picker.ToolTip = "All profile actions are available. Actions are saved in their real Star Citizen game state.";
            }

            _suppressUi = true;
            try
            {
                picker.ItemsSource = choices;
                picker.SelectedItem = state.Action is null
                    ? null
                    : choices.FirstOrDefault(choice => SameAction(choice.Action, state.Action));
            }
            finally
            {
                _suppressUi = false;
            }
        }
    }

    private void CorrectedStatePickerChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressUi || _selectedControl is null || sender is not ComboBox { Tag: PlannedStateBinding source } picker) return;
        if (picker.SelectedItem is not ActionChoice choice) return;

        _assistantAcknowledgedInputs.Remove(_selectedControl.Input);
        var action = choice.Action;
        var plan = GetOrCreatePlan(_selectedControl);
        if (!plan.States.TryGetValue(action.Context, out var destination))
        {
            var existing = CurrentAssignments(_selectedControl).Where(item => item.Context.Equals(action.Context, StringComparison.OrdinalIgnoreCase)).ToList();
            destination = new PlannedStateBinding
            {
                Context = action.Context,
                Choices = CanonicalActions(),
                Existing = existing,
                Enabled = false,
                Action = null
            };
            plan.States[action.Context] = destination;
        }

        destination.Action = action;
        destination.Enabled = true;
        UpdateStateStatus(destination);
        plan.IsDirty = true;
        MarkDirty();

        BuildStateRows();
        BuildWarnings();
        BuildStateOverview();
        BuildDeviceVisual();
        SetStatus(action.Context.Equals(source.Context, StringComparison.OrdinalIgnoreCase)
            ? $"Assigned {action.DisplayName} to {_selectedControl.DisplayName}."
            : $"{action.DisplayName} belongs to {action.Context}, so it was assigned there for {_selectedControl.DisplayName}. Every action remains available from every control.");
    }

    private void DeduplicateActionTree()
    {
        if (_actionTree.Items.Count == 0) return;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in _actionTree.Items.OfType<TreeViewItem>().ToList())
            DeduplicateActionTreeNode(root, seen);
    }

    private static void DeduplicateActionTreeNode(TreeViewItem parent, HashSet<string> seen)
    {
        foreach (var child in parent.Items.OfType<TreeViewItem>().ToList())
        {
            if (child.Tag is StarbindAction action)
            {
                var key = $"{action.Context}|{action.ActionName}|{CorrectedBehavior(action)}";
                if (!seen.Add(key)) parent.Items.Remove(child);
                continue;
            }
            DeduplicateActionTreeNode(child, seen);
        }
    }

    private void BeginActionDrag(object sender, MouseButtonEventArgs e)
    {
        _actionDragStart = e.GetPosition(_actionTree);
        _draggedAction = FindTaggedAncestor<StarbindAction>(e.OriginalSource as DependencyObject);
    }

    private void ContinueActionDrag(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _draggedAction is null) return;
        var current = e.GetPosition(_actionTree);
        if (Math.Abs(current.X - _actionDragStart.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(current.Y - _actionDragStart.Y) < SystemParameters.MinimumVerticalDragDistance) return;

        var action = _draggedAction;
        _draggedAction = null;
        DragDrop.DoDragDrop(_actionTree, new DataObject(typeof(StarbindAction), action), DragDropEffects.Copy);
    }

    private void MappingTargetDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(StarbindAction)) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void MappingTargetDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(StarbindAction)) is not StarbindAction action) return;
        var control = FindTaggedAncestor<StarbindControl>(e.OriginalSource as DependencyObject) ?? _selectedControl;
        if (control is null)
        {
            SetStatus("Choose a physical control, then drag the action onto it.");
            return;
        }

        SelectControl(control);
        _assistantAcknowledgedInputs.Remove(control.Input);
        var plan = GetOrCreatePlan(control);
        if (!plan.States.TryGetValue(action.Context, out var state))
        {
            state = new PlannedStateBinding
            {
                Context = action.Context,
                Choices = CanonicalActions(),
                Existing = CurrentAssignments(control).Where(item => item.Context.Equals(action.Context, StringComparison.OrdinalIgnoreCase)).ToList(),
                Enabled = false,
                Action = null
            };
            plan.States[action.Context] = state;
        }
        state.Action = action;
        state.Enabled = true;
        UpdateStateStatus(state);
        plan.IsDirty = true;
        MarkDirty();
        BuildStateRows();
        BuildWarnings();
        BuildStateOverview();
        BuildDeviceVisual();
        SetStatus($"Assigned {action.DisplayName} to {control.DisplayName} in {action.Context}. Dragging maps only the action you chose; it does not add automatic companion binds.");
        e.Handled = true;
    }

    private static T? FindTaggedAncestor<T>(DependencyObject? source) where T : class
    {
        var current = source;
        while (current is not null)
        {
            if (current is FrameworkElement { Tag: T tagged }) return tagged;
            current = current is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(current)
                : LogicalTreeHelper.GetParent(current);
        }
        return null;
    }

    private void InstallKeepAllDismissHandler()
    {
        var button = VisualDescendants<Button>(_mappingAssistantHost)
            .FirstOrDefault(item => string.Equals(item.Content?.ToString(), "YES, KEEP ALL", StringComparison.OrdinalIgnoreCase));
        if (button is null || Equals(button.Tag, "V8_KEEP_ALL_FIXED")) return;
        button.Tag = "V8_KEEP_ALL_FIXED";
        button.Click += (_, _) => AcknowledgeAndDismissMappingAssistant();
    }

    private void AcknowledgeAndDismissMappingAssistant()
    {
        if (_selectedControl is not null) _assistantAcknowledgedInputs.Add(_selectedControl.Input);
        _mappingAssistantChooser.Visibility = Visibility.Collapsed;
        _mappingAssistantHost.Visibility = Visibility.Collapsed;
    }

    private static IEnumerable<T> VisualDescendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match) yield return match;
            foreach (var descendant in VisualDescendants<T>(child)) yield return descendant;
        }
    }
}
