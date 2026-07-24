using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace StarConfig;

public sealed partial class StarbindV5Window
{
    private bool _v8ControlTrayRefreshing;

    private void RefreshUnplacedControlTray()
    {
        if (_v8ControlTrayRefreshing || _selectedDevice is null || _selectedTemplate is null) return;
        if (_selectedTemplate.Family is HardwareFamily.Keyboard or HardwareFamily.Mouse or HardwareFamily.Gamepad) return;

        var viewbox = VisualDescendants<Viewbox>(_deviceCanvasHost).FirstOrDefault();
        if (viewbox?.Child is not Canvas canvas) return;

        _v8ControlTrayRefreshing = true;
        try
        {
            var controls = FilteredControls();
            var definitions = _selectedTemplate.Controls.ToDictionary(definition => definition.InputSuffix, StringComparer.OrdinalIgnoreCase);
            var unplaced = controls
                .Where(control =>
                {
                    var suffix = StarbindInput.Split(control.Input).Suffix;
                    return !definitions.TryGetValue(suffix, out var definition)
                        || definition.HotspotX <= 0
                        || definition.HotspotY <= 0;
                })
                .OrderByDescending(control => control.Input.Equals(_selectedControl?.Input, StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(control => EffectiveAssignments(control).Any())
                .ThenBy(control => NaturalOrder(control.Input))
                .ToList();

            var existing = canvas.Children.OfType<FrameworkElement>().FirstOrDefault(element => element.Uid.StartsWith("V8_EXTRA_CONTROLS:", StringComparison.Ordinal));
            if (unplaced.Count == 0)
            {
                if (existing is not null) canvas.Children.Remove(existing);
                return;
            }

            var assignmentSignature = string.Join(',', unplaced.Select(control => $"{control.Input}:{EffectiveAssignments(control).Count()}"));
            var marker = $"V8_EXTRA_CONTROLS:{_selectedDevice.InputPrefix}:{_selectedControl?.Input}:{_showUnassigned.IsChecked}:{assignmentSignature}";
            if (existing?.Uid == marker) return;
            if (existing is not null) canvas.Children.Remove(existing);

            var tray = new Border
            {
                Uid = marker,
                Width = 500,
                Height = 112,
                Background = Paint("#E90A1018"),
                BorderBrush = Border2,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8)
            };
            var trayGrid = new Grid();
            trayGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            trayGrid.RowDefinitions.Add(new RowDefinition());
            tray.Child = trayGrid;

            var heading = new Grid();
            heading.ColumnDefinitions.Add(new ColumnDefinition());
            heading.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            heading.Children.Add(new TextBlock
            {
                Text = "EXTRA PHYSICAL CONTROLS",
                Foreground = Cyan,
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            });
            var instruction = new TextBlock
            {
                Text = "Drag any action onto any button",
                Foreground = Muted,
                FontSize = 8.5,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(instruction, 1);
            heading.Children.Add(instruction);
            trayGrid.Children.Add(heading);

            var wrap = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                ItemWidth = 54,
                ItemHeight = 29,
                Margin = new Thickness(0, 5, 0, 0)
            };
            foreach (var control in unplaced)
            {
                var assignments = EffectiveAssignments(control).ToList();
                var selected = control.Input.Equals(_selectedControl?.Input, StringComparison.OrdinalIgnoreCase);
                var suffix = StarbindInput.Split(control.Input).Suffix;
                var digits = new string(suffix.Where(char.IsDigit).ToArray());
                var shortName = string.IsNullOrWhiteSpace(digits)
                    ? control.ShortCode
                    : control.Kind == StarbindControlKind.Button ? $"B{digits}" : control.ShortCode;
                var button = new Button
                {
                    Width = 50,
                    Height = 25,
                    Margin = new Thickness(2),
                    Content = shortName,
                    Tag = control,
                    AllowDrop = true,
                    Cursor = Cursors.Hand,
                    Background = selected ? BlueDim : assignments.Count > 0 ? Paint("#193425") : Panel2,
                    BorderBrush = selected ? Blue : assignments.Count > 0 ? Green : Border2,
                    Foreground = selected ? Text : assignments.Count > 0 ? Green : Muted,
                    Padding = new Thickness(3, 1, 3, 1),
                    FontSize = 9,
                    ToolTip = assignments.Count == 0
                        ? $"{control.DisplayName}\n{control.Input}\nUnassigned. Drop any action here."
                        : $"{control.DisplayName}\n{control.Input}\n{string.Join(Environment.NewLine, assignments.Select(action => $"{action.Context}: {action.DisplayName}"))}\nDrop any action here to replace or add a state assignment."
                };
                button.Click += (_, _) => SelectControl(control);
                button.PreviewDragOver += MappingTargetDragOver;
                button.Drop += MappingTargetDrop;
                wrap.Children.Add(button);
            }

            var scroll = new ScrollViewer
            {
                Content = wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
            Grid.SetRow(scroll, 1);
            trayGrid.Children.Add(scroll);
            Canvas.SetLeft(tray, 220);
            Canvas.SetTop(tray, 398);
            canvas.Children.Add(tray);
        }
        finally
        {
            _v8ControlTrayRefreshing = false;
        }
    }
}
