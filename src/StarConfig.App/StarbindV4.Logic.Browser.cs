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
    private void BuildActionBrowser()
    {
        _actionTree.Items.Clear();
        if (_profile is null) return;
        var term = _actionSearch.Text.Trim();
        var actions = _profile.Actions.GroupBy(a => $"{a.ActionMap}|{a.ActionOrdinal}|{a.ActionName}").Select(g => g.First())
            .Where(a => string.IsNullOrWhiteSpace(term) || new[] { a.DisplayName, a.ActionName, a.Context, a.Category, a.Intent, a.Behavior }.Any(value => value.Contains(term, StringComparison.OrdinalIgnoreCase))).ToList();
        foreach (var context in Contexts.Where(c => actions.Any(a => a.Context.Equals(c, StringComparison.OrdinalIgnoreCase))))
        {
            var contextItem = TreeGroup(context.ToUpperInvariant());
            foreach (var categoryGroup in actions.Where(a => a.Context.Equals(context, StringComparison.OrdinalIgnoreCase)).GroupBy(a => a.Category).OrderBy(g => g.Key))
            {
                var category = TreeGroup(categoryGroup.Key);
                foreach (var action in categoryGroup.OrderBy(a => a.DisplayName)) category.Items.Add(new TreeViewItem { Header = action.DisplayName, Tag = action, Foreground = Text, Padding = new Thickness(2) });
                category.IsExpanded = !string.IsNullOrWhiteSpace(term) || categoryGroup.Key == "Movement";
                contextItem.Items.Add(category);
            }
            contextItem.IsExpanded = !string.IsNullOrWhiteSpace(term) || context == "Flight";
            _actionTree.Items.Add(contextItem);
        }
    }

    private void BrowserActionChanged()
    {
        if (_actionTree.SelectedItem is TreeViewItem item && item.Tag is StarbindAction action)
        {
            SelectAction(action);
            _primaryActionPicker.SelectedItem = FindPickerAction(_primaryActionPicker, action);
        }
    }

    private void BuildWarnings()
    {
        var warnings = new List<StarbindWarning>();
        if (_profile is null) warnings.Add(new("!", "No profile loaded.", Amber));
        else if (_selectedControl is null) warnings.Add(new("i", "Select a physical control.", Cyan));
        else
        {
            var current = CurrentAssignments(_selectedControl).ToList();
            foreach (var state in current.GroupBy(x => x.Context).Where(g => g.Count() > 1)) warnings.Add(new("!", $"{state.Key}: {_selectedControl.DisplayName} is assigned to {state.Count()} actions.", Amber));
            if (current.Select(x => x.Intent).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1) warnings.Add(new("!", "This control performs unrelated intents across game states.", Amber));
            foreach (var assignment in _stateAssignments.Values.Where(x => x.Enabled && x.SelectedAction is null)) warnings.Add(new("!", $"{assignment.Context} is enabled without an action.", Red));
            var sameStateDuplicates = _profile.Actions.Where(x => x.IsBound).GroupBy(x => $"{x.Context}|{x.Input}", StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).ToList();
            foreach (var duplicate in sameStateDuplicates.Take(4))
            {
                var items = duplicate.ToList();
                warnings.Add(new("!", $"{items[0].Context}: {items[0].Input} is assigned to {items.Count} actions.", Amber));
            }
            if (warnings.Count == 0) warnings.Add(new("✓", "No critical conflicts for this control.", Green));
        }
        _warnings.ItemsSource = warnings;
        _warnings.ItemTemplate = WarningTemplate();
    }

    private void BuildStateOverview()
    {
        _stateOverview.Children.Clear();
        if (_selectedControl is null) return;
        foreach (var assignment in _stateAssignments.Values)
        {
            var card = new Border { Width = 118, Height = 175, Margin = new Thickness(0, 0, 6, 0), Padding = new Thickness(8), Background = Field, BorderBrush = assignment.Enabled ? Blue : Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4) };
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = assignment.Context.ToUpperInvariant(), FontWeight = FontWeights.Bold, Foreground = assignment.Enabled ? Cyan : Muted });
            stack.Children.Add(new TextBlock { Text = assignment.Enabled ? assignment.SelectedAction?.DisplayName ?? "Missing action" : "Not assigned", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 0), FontSize = 11 });
            stack.Children.Add(new TextBlock { Text = $"Type: {assignment.SelectedAction?.Behavior ?? "-"}", Foreground = Muted, Margin = new Thickness(0, 8, 0, 0), FontSize = 10 });
            stack.Children.Add(new TextBlock { Text = $"Input: {_selectedControl.Input}", Foreground = Muted, TextWrapping = TextWrapping.Wrap, FontSize = 10 });
            card.Child = stack;
            _stateOverview.Children.Add(card);
        }
    }

    private void BuildDeviceVisual()
    {
        _deviceCanvasHost.Children.Clear();
        if (_selectedDevice is null) return;
        if (_selectedDevice.Kind != StarbindDeviceKind.Joystick)
        {
            _deviceCanvasHost.Children.Add(new TextBlock { Text = _selectedDevice.Kind == StarbindDeviceKind.Keyboard ? "KEYBOARD\nPress Test / Listen to capture any key." : "MOUSE\nPress Test / Listen to capture a button.", Foreground = Muted, TextAlignment = TextAlignment.Center, FontSize = 18, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
            return;
        }
        var canvas = new Canvas { Width = 760, Height = 420 };
        var viewbox = new Viewbox { Stretch = Stretch.Uniform, Margin = new Thickness(6), Child = canvas };
        _deviceCanvasHost.Children.Add(viewbox);
        var image = new Image { Source = StarbindArtwork.LoadJoystick(), Width = 220, Height = 360, Stretch = Stretch.Uniform };
        Canvas.SetLeft(image, 270); Canvas.SetTop(image, 20); canvas.Children.Add(image);
        var controls = BuildControls(_selectedDevice).Where(x => x.Kind is StarbindControlKind.Button or StarbindControlKind.Hat or StarbindControlKind.Axis).ToList();
        var featured = controls.Where(x => CurrentAssignments(x).Any()).Take(10).Concat(controls.Where(x => !CurrentAssignments(x).Any()).Take(10)).DistinctBy(x => x.Input).Take(10).ToList();
        for (var index = 0; index < featured.Count; index++)
        {
            var leftSide = index % 2 == 0;
            var row = index / 2;
            var control = featured[index];
            var x = leftSide ? 28d : 590d;
            var y = 45d + row * 62d;
            var label = ControlCalloutLabel(control);
            var button = new Button { Content = label, Width = 140, Height = 40, Background = control.Input.Equals(_selectedControl?.Input, StringComparison.OrdinalIgnoreCase) ? BlueDim : Panel2, Foreground = Text, BorderBrush = control.Input.Equals(_selectedControl?.Input, StringComparison.OrdinalIgnoreCase) ? Blue : Border2, FontSize = 10, Cursor = Cursors.Hand, Tag = control };
            button.Click += (_, _) => SelectControl(control);
            Canvas.SetLeft(button, x); Canvas.SetTop(button, y); canvas.Children.Add(button);
            var line = new ShapeLine { Stroke = Border2, StrokeThickness = 1.2, X1 = leftSide ? x + 140 : x, Y1 = y + 20, X2 = leftSide ? 292 : 472, Y2 = 70 + row * 48 };
            canvas.Children.Add(line);
        }
        if (_selectedControl?.IsAxis == true)
        {
            var axisTag = new Border { Width = 150, Height = 48, Background = BlueDim, BorderBrush = Blue, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(3), Child = new TextBlock { Text = $"{_selectedControl.DisplayName}\n{_selectedControl.Input}", Foreground = Text, Margin = new Thickness(8), TextWrapping = TextWrapping.Wrap } };
            Canvas.SetLeft(axisTag, 505); Canvas.SetTop(axisTag, 350); canvas.Children.Add(axisTag);
        }
    }

    private string ControlCalloutLabel(StarbindControl control)
    {
        var action = CurrentAssignments(control).FirstOrDefault();
        return action is null ? control.DisplayName : $"{control.DisplayName}\n{action.DisplayName}";
    }

}
