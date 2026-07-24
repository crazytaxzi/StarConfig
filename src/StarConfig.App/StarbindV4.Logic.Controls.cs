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
    private IReadOnlyList<StarbindControl> BuildControls(StarbindDevice device)
    {
        var result = new Dictionary<string, StarbindControl>(StringComparer.OrdinalIgnoreCase);
        if (_profile is not null)
        {
            foreach (var action in _profile.Actions.Where(x => x.IsBound && x.Input.StartsWith(device.InputPrefix + "_", StringComparison.OrdinalIgnoreCase)))
            {
                var control = ControlFromInput(action.Input);
                result[control.Input] = control;
            }
        }
        if (device.Kind == StarbindDeviceKind.Joystick)
        {
            var axes = new[] { "x", "y", "z", "rotx", "roty", "rotz", "slider1", "slider2" };
            foreach (var axis in axes.Take(Math.Max(0, Math.Min(axes.Length, device.Axes))))
            {
                var input = $"js{device.Instance}_{axis}";
                result.TryAdd(input, ControlFromInput(input));
            }
            for (var button = 1; button <= device.Buttons; button++)
            {
                var input = $"js{device.Instance}_button{button}";
                result.TryAdd(input, ControlFromInput(input));
            }
            foreach (var direction in new[] { "up", "right", "down", "left" })
            {
                var input = $"js{device.Instance}_hat1_{direction}";
                result.TryAdd(input, ControlFromInput(input));
            }
        }
        else if (device.Kind == StarbindDeviceKind.Mouse)
        {
            for (var button = 1; button <= 5; button++)
            {
                var input = $"mouse1_button{button}";
                result.TryAdd(input, ControlFromInput(input));
            }
        }
        return result.Values.OrderBy(x => x.Kind).ThenBy(x => NaturalControlOrder(x.Input)).ToList();
    }

    private void BuildControlTree()
    {
        _controlTree.Items.Clear();
        if (_selectedDevice is null) return;
        var controls = BuildControls(_selectedDevice);
        foreach (var group in controls.GroupBy(x => x.Kind).OrderBy(x => x.Key))
        {
            var parent = TreeGroup(group.Key switch
            {
                StarbindControlKind.Axis => "⌁  AXES", StarbindControlKind.Button => "◉  BUTTONS", StarbindControlKind.Hat => "✥  HATS",
                StarbindControlKind.Key => "⌨  KEYS", StarbindControlKind.MouseButton => "◫  MOUSE BUTTONS", _ => "CONTROLS"
            });
            foreach (var control in group)
            {
                var count = _profile?.Actions.Count(x => x.Input.Equals(control.Input, StringComparison.OrdinalIgnoreCase)) ?? 0;
                var header = new DockPanel();
                var badge = new TextBlock { Text = count > 0 ? count.ToString(CultureInfo.InvariantCulture) : string.Empty, Foreground = count > 0 ? Green : Faint, Margin = new Thickness(8, 0, 0, 0) };
                DockPanel.SetDock(badge, Dock.Right);
                header.Children.Add(badge);
                header.Children.Add(new TextBlock { Text = control.DisplayName, Foreground = Text });
                parent.Items.Add(new TreeViewItem { Header = header, Tag = control, Foreground = Text, Padding = new Thickness(3) });
            }
            parent.IsExpanded = group.Key is StarbindControlKind.Axis or StarbindControlKind.Button;
            _controlTree.Items.Add(parent);
        }
    }

    private void ControlTreeChanged()
    {
        if (_controlTree.SelectedItem is TreeViewItem item && item.Tag is StarbindControl control) SelectControl(control);
    }

    private void SelectControl(StarbindControl control)
    {
        _selectedControl = control;
        _selectedControlName.Text = control.DisplayName;
        _selectedControlInput.Text = $"Physical: {control.Input}";
        _selectedControlType.Text = control.Kind.ToString();
        var assignments = CurrentAssignments(control).ToList();
        _selectedControlAssignmentCount.Text = assignments.Count.ToString(CultureInfo.InvariantCulture);
        LoadAxisSettings(control);
        BuildStateRows();
        BuildDeviceVisual();
        BuildWarnings();
        BuildStateOverview();
        DrawResponseGraph();
        if (assignments.Count > 0)
        {
            SelectAction(assignments[0]);
            _primaryActionPicker.SelectedItem = FindPickerAction(_primaryActionPicker, assignments[0]);
        }
        else
        {
            _selectedAction = null;
            _primaryActionPicker.SelectedItem = null;
            _actionDescription.Text = "This physical control is currently unassigned. Choose an action or use the action browser.";
            _similarActions.Children.Clear();
        }
        SetStatus($"Selected {control.DisplayName}. Existing assignments and state-aware choices are ready.");
    }

    private IEnumerable<StarbindAction> CurrentAssignments(StarbindControl control) => _profile?.Actions.Where(x => x.Input.Equals(control.Input, StringComparison.OrdinalIgnoreCase)) ?? [];

}
