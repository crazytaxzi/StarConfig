using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ShapeEllipse = System.Windows.Shapes.Ellipse;
using ShapeLine = System.Windows.Shapes.Line;
using ShapePath = System.Windows.Shapes.Path;
using ShapePolygon = System.Windows.Shapes.Polygon;
using ShapePolyline = System.Windows.Shapes.Polyline;
using ShapeRectangle = System.Windows.Shapes.Rectangle;

namespace StarConfig;

public sealed partial class StarbindV5Window
{
    private void BuildDeviceVisual()
    {
        _deviceCanvasHost.Children.Clear();
        if (_selectedDevice is null || _selectedTemplate is null)
        {
            _deviceCanvasHost.Children.Add(CenteredMessage("Select a connected device."));
            return;
        }

        if (_selectedTemplate.Family is HardwareFamily.Keyboard or HardwareFamily.Mouse or HardwareFamily.Gamepad)
        {
            var host = new Grid { LayoutTransform = new ScaleTransform(_zoom, _zoom) };
            host.Children.Add(DeviceArtworkFactory.BuildLarge(_selectedTemplate.Family, 820, 455, Panel2, Border2, Cyan, Muted));
            AddSimpleControlOverlay(host);
            _deviceCanvasHost.Children.Add(new Viewbox { Stretch = Stretch.Uniform, Margin = new Thickness(8), Child = host });
            return;
        }

        var canvas = new Canvas { Width = 940, Height = 520, LayoutTransform = new ScaleTransform(_zoom, _zoom) };
        _deviceCanvasHost.Children.Add(new Viewbox { Stretch = Stretch.Uniform, Margin = new Thickness(5), Child = canvas });

        var artwork = BuildHardwareArtwork(_selectedTemplate, 420, 475);
        Canvas.SetLeft(artwork, 260);
        Canvas.SetTop(artwork, 10);
        canvas.Children.Add(artwork);

        var controls = FilteredControls();
        var definitions = _selectedTemplate.Controls.ToDictionary(item => item.InputSuffix, StringComparer.OrdinalIgnoreCase);
        var candidates = controls
            .Where(control => definitions.TryGetValue(StarbindInput.Split(control.Input).Suffix, out var definition) && definition.HotspotX > 0 && definition.HotspotY > 0)
            .ToList();
        var display = candidates
            .OrderByDescending(control => control.Input.Equals(_selectedControl?.Input, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(control => EffectiveAssignments(control).Any())
            .ThenBy(control => NaturalOrder(control.Input))
            .Take(12)
            .ToList();

        var left = display.Where(control => definitions[StarbindInput.Split(control.Input).Suffix].LabelX < .5).OrderBy(control => definitions[StarbindInput.Split(control.Input).Suffix].HotspotY).ToList();
        var right = display.Where(control => definitions[StarbindInput.Split(control.Input).Suffix].LabelX >= .5).OrderBy(control => definitions[StarbindInput.Split(control.Input).Suffix].HotspotY).ToList();
        for (var index = 0; index < left.Count; index++) AddControlCallout(canvas, left[index], definitions[StarbindInput.Split(left[index].Input).Suffix], true, index, left.Count);
        for (var index = 0; index < right.Count; index++) AddControlCallout(canvas, right[index], definitions[StarbindInput.Split(right[index].Input).Suffix], false, index, right.Count);

        if (display.Count == 0)
        {
            var note = new TextBlock
            {
                Text = "No named hotspots are defined for this hardware yet. Use the control tree or Test Device to select a physical input.",
                Foreground = Muted,
                TextWrapping = TextWrapping.Wrap,
                Width = 470,
                TextAlignment = TextAlignment.Center
            };
            Canvas.SetLeft(note, 235);
            Canvas.SetTop(note, 470);
            canvas.Children.Add(note);
        }
    }

    private UIElement BuildHardwareArtwork(HardwareTemplate template, double width, double height)
    {
        UIElement artwork = template.ArtworkKey == "joystick"
            ? new Image { Source = StarbindArtwork.LoadJoystick(), Width = width, Height = height, Stretch = Stretch.Uniform }
            : DeviceArtworkFactory.BuildLarge(template.Family, width, height, Panel2, Border2, Cyan, Muted);
        if (template.Family == HardwareFamily.Joystick && _selectedDevice is not null
            && (_selectedDevice.ProductName.Contains(" EVO L", StringComparison.OrdinalIgnoreCase)
                || _selectedDevice.ProductName.Contains(" LEFT", StringComparison.OrdinalIgnoreCase)))
        {
            artwork.RenderTransformOrigin = new Point(.5, .5);
            artwork.RenderTransform = new ScaleTransform(-1, 1);
        }
        return artwork;
    }

    private void AddControlCallout(Canvas canvas, StarbindControl control, HardwareControlDefinition definition, bool leftSide, int laneIndex, int laneCount)
    {
        var selected = control.Input.Equals(_selectedControl?.Input, StringComparison.OrdinalIgnoreCase);
        var assignment = EffectiveAssignments(control).FirstOrDefault();
        var labelWidth = 185d;
        var labelHeight = assignment is null ? 38d : 54d;
        var laneStep = laneCount <= 1 ? 0 : Math.Min(72, 410d / (laneCount - 1));
        var top = laneCount <= 1 ? 215d : 34d + laneIndex * laneStep;
        var left = leftSide ? 16d : 739d;
        var hotspotX = 260 + definition.HotspotX * 420;
        var hotspotY = 10 + definition.HotspotY * 475;

        var label = new Button
        {
            Width = labelWidth,
            Height = labelHeight,
            Background = selected ? BlueDim : Paint("#1B232D"),
            Foreground = Text,
            BorderBrush = selected ? Blue : Border2,
            Padding = new Thickness(8, 4, 8, 4),
            Cursor = System.Windows.Input.Cursors.Hand,
            Tag = control,
            ToolTip = definition.Description ?? control.DisplayName,
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
        var words = new StackPanel();
        words.Children.Add(new TextBlock { Text = control.DisplayName, FontSize = 10, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
        if (assignment is not null) words.Children.Add(new TextBlock { Text = assignment.DisplayName, Foreground = selected ? Cyan : Muted, FontSize = 8.5, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 2, 0, 0) });
        label.Content = words;
        label.Click += (_, _) => SelectControl(control);
        Canvas.SetLeft(label, left);
        Canvas.SetTop(label, top);
        canvas.Children.Add(label);

        var startX = leftSide ? left + labelWidth : left;
        var startY = top + labelHeight / 2;
        var elbowX = leftSide ? 230d : 710d;
        canvas.Children.Add(new ShapePolyline
        {
            Points = new PointCollection { new(startX, startY), new(elbowX, startY), new(hotspotX, hotspotY) },
            Stroke = selected ? Blue : Border2,
            StrokeThickness = selected ? 1.9 : 1.1
        });
        var hotspotSize = selected ? 13d : 8d;
        var hotspot = new ShapeEllipse { Width = hotspotSize, Height = hotspotSize, Fill = selected ? Blue : Cyan, Stroke = Field, StrokeThickness = 1 };
        Canvas.SetLeft(hotspot, hotspotX - hotspotSize / 2);
        Canvas.SetTop(hotspot, hotspotY - hotspotSize / 2);
        canvas.Children.Add(hotspot);
    }

    private void AddSimpleControlOverlay(Grid host)
    {
        if (_selectedDevice is null) return;
        var panel = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(30), MaxWidth = 700 };
        foreach (var control in FilteredControls().OrderByDescending(control => EffectiveAssignments(control).Any()).Take(14))
        {
            var button = new Button
            {
                Content = control.DisplayName,
                Tag = control,
                Background = control.Input.Equals(_selectedControl?.Input, StringComparison.OrdinalIgnoreCase) ? BlueDim : Panel2,
                BorderBrush = control.Input.Equals(_selectedControl?.Input, StringComparison.OrdinalIgnoreCase) ? Blue : Border2,
                Foreground = Text,
                Margin = new Thickness(3),
                Padding = new Thickness(8, 5, 8, 5),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            button.Click += (_, _) => SelectControl(control);
            panel.Children.Add(button);
        }
        host.Children.Add(panel);
    }

    private void ZoomChanged()
    {
        if (_zoomPicker.SelectedItem is not string value) return;
        var clean = value.Replace("%", string.Empty, StringComparison.Ordinal).Trim();
        _zoom = double.TryParse(clean, NumberStyles.Float, CultureInfo.InvariantCulture, out var percent) ? Math.Clamp(percent / 100d, 0.5, 2.0) : 1.0;
        BuildDeviceVisual();
    }

    private void LoadAxisSettings(StarbindControl control)
    {
        _suppressUi = true;
        try
        {
            _deadzonePicker.IsEnabled = control.IsAxis;
            _curvePicker.IsEnabled = control.IsAxis;
            if (!control.IsAxis || _profile is null || _selectedDevice is null)
            {
                _deadzonePicker.SelectedItem = "0%";
                _curvePicker.SelectedItem = "Linear";
                return;
            }

            if (_axisTunings.TryGetValue(control.Input, out var pending))
            {
                _deadzonePicker.SelectedItem = PercentLabel(pending.Deadzone);
                _curvePicker.SelectedItem = pending.CurveName;
                return;
            }

            var (_, suffix) = StarbindInput.Split(control.Input);
            var option = _profile.AxisOptions.FirstOrDefault(item => item.DeviceInstance == control.DeviceInstance && item.Axis.Equals(suffix, StringComparison.OrdinalIgnoreCase));
            _deadzonePicker.SelectedItem = PercentLabel(option?.Deadzone ?? 0);
            _curvePicker.SelectedItem = CurveName(option?.Exponent ?? 1.0);
        }
        finally { _suppressUi = false; }
    }

    private void AxisSettingChanged()
    {
        DrawResponseGraph();
        if (_suppressUi || _selectedControl?.IsAxis != true || _selectedDevice is null) return;
        var (_, suffix) = StarbindInput.Split(_selectedControl.Input);
        var curveName = _curvePicker.SelectedItem as string ?? "Linear";
        var tuning = new AxisTuningChange(
            _selectedControl.Input,
            _selectedDevice.ProductName,
            _selectedDevice.Instance,
            suffix,
            ParsePercent(_deadzonePicker.SelectedItem as string),
            CurveExponent(curveName),
            curveName);
        _axisTunings[_selectedControl.Input] = tuning;
        MarkDirty();
    }

    private void DrawResponseGraph(double liveValue = 0.65)
    {
        _responseGraph.Children.Clear();
        var width = Math.Max(160, _responseGraph.ActualWidth > 1 ? _responseGraph.ActualWidth : 185);
        var height = Math.Max(98, _responseGraph.ActualHeight > 1 ? _responseGraph.ActualHeight : 142);
        for (var i = 1; i < 4; i++)
        {
            _responseGraph.Children.Add(new ShapeLine { X1 = 0, Y1 = i * height / 4, X2 = width, Y2 = i * height / 4, Stroke = Border, StrokeThickness = 0.7 });
            _responseGraph.Children.Add(new ShapeLine { X1 = i * width / 4, Y1 = 0, X2 = i * width / 4, Y2 = height, Stroke = Border, StrokeThickness = 0.7 });
        }
        var deadzone = ParsePercent(_deadzonePicker.SelectedItem as string);
        var exponent = CurveExponent(_curvePicker.SelectedItem as string ?? "Linear");
        var points = new PointCollection();
        for (var i = 0; i <= 50; i++)
        {
            var x = i / 50d;
            var normalized = x <= deadzone ? 0 : (x - deadzone) / (1 - deadzone);
            var y = Math.Pow(normalized, exponent);
            points.Add(new Point(x * width, height - y * height));
        }
        _responseGraph.Children.Add(new ShapePolyline { Points = points, Stroke = Blue, StrokeThickness = 2 });
        var markerX = Math.Clamp(liveValue, 0, 1) * width;
        _responseGraph.Children.Add(new ShapeLine { X1 = markerX, Y1 = 0, X2 = markerX, Y2 = height, Stroke = Green, StrokeThickness = 1.5 });
    }

    private static string PercentLabel(double value)
    {
        var percent = (int)Math.Round(value * 100);
        var allowed = new[] { 0, 2, 5, 7, 10, 15, 20 };
        return allowed.OrderBy(item => Math.Abs(item - percent)).First() + "%";
    }

    private static double CurveExponent(string curve) => curve switch
    {
        "Gentle" => 1.35,
        "Aggressive" => 0.72,
        "Precision" => 1.8,
        _ => 1.0
    };

    private static string CurveName(double exponent)
    {
        var curves = new[]
        {
            (Name: "Linear", Exponent: 1.0),
            (Name: "Gentle", Exponent: 1.35),
            (Name: "Aggressive", Exponent: 0.72),
            (Name: "Precision", Exponent: 1.8)
        };
        return curves.OrderBy(curve => Math.Abs(curve.Exponent - exponent)).First().Name;
    }

    private static double ParsePercent(string? value)
    {
        var clean = (value ?? "0").Replace("%", string.Empty, StringComparison.Ordinal).Trim();
        return double.TryParse(clean, NumberStyles.Float, CultureInfo.InvariantCulture, out var percent) ? Math.Clamp(percent / 100d, 0, 0.95) : 0;
    }

    private static TextBlock CenteredMessage(string text) => new()
    {
        Text = text,
        Foreground = Muted,
        FontSize = 16,
        TextAlignment = TextAlignment.Center,
        TextWrapping = TextWrapping.Wrap,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        MaxWidth = 520
    };
}

internal static class DeviceArtworkFactory
{
    public static UIElement BuildThumbnail(HardwareFamily family, double width, double height, Brush background, Brush accent, Brush muted)
    {
        var view = BuildLarge(family, width, height, background, accent, accent, muted);
        return new Viewbox { Width = width, Height = height, Stretch = Stretch.Uniform, Child = view };
    }

    public static UIElement BuildLarge(HardwareFamily family, double width, double height, Brush fill, Brush stroke, Brush accent, Brush muted)
    {
        var canvas = new Canvas { Width = width, Height = height };
        switch (family)
        {
            case HardwareFamily.Throttle:
                DrawThrottle(canvas, width, height, fill, stroke, accent);
                break;
            case HardwareFamily.Pedals:
                DrawPedals(canvas, width, height, fill, stroke, accent);
                break;
            case HardwareFamily.Keyboard:
                DrawKeyboard(canvas, width, height, fill, stroke, accent);
                break;
            case HardwareFamily.Mouse:
                DrawMouse(canvas, width, height, fill, stroke, accent);
                break;
            case HardwareFamily.Gamepad:
                DrawGamepad(canvas, width, height, fill, stroke, accent);
                break;
            default:
                DrawGenericStick(canvas, width, height, fill, stroke, accent);
                break;
        }
        return canvas;
    }

    private static void DrawThrottle(Canvas canvas, double width, double height, Brush fill, Brush stroke, Brush accent)
    {
        AddRounded(canvas, width * .20, height * .54, width * .60, height * .28, fill, stroke, 16);
        AddRounded(canvas, width * .36, height * .17, width * .28, height * .44, fill, stroke, 20);
        AddRounded(canvas, width * .42, height * .08, width * .18, height * .18, fill, stroke, 14);
        AddCircle(canvas, width * .47, height * .14, Math.Min(width, height) * .045, accent);
        for (var i = 0; i < 4; i++) AddCircle(canvas, width * (.30 + i * .12), height * .68, Math.Min(width, height) * .026, accent);
    }

    private static void DrawPedals(Canvas canvas, double width, double height, Brush fill, Brush stroke, Brush accent)
    {
        AddRounded(canvas, width * .18, height * .60, width * .64, height * .18, fill, stroke, 12);
        AddRounded(canvas, width * .17, height * .20, width * .25, height * .42, fill, stroke, 10, -10);
        AddRounded(canvas, width * .58, height * .20, width * .25, height * .42, fill, stroke, 10, 10);
        canvas.Children.Add(new ShapeLine { X1 = width * .30, Y1 = height * .61, X2 = width * .70, Y2 = height * .61, Stroke = accent, StrokeThickness = Math.Max(2, width * .012) });
    }

    private static void DrawKeyboard(Canvas canvas, double width, double height, Brush fill, Brush stroke, Brush accent)
    {
        AddRounded(canvas, width * .08, height * .25, width * .84, height * .50, fill, stroke, 10);
        for (var row = 0; row < 4; row++)
        for (var column = 0; column < 12; column++)
        {
            var key = new ShapeRectangle { Width = width * .055, Height = height * .065, RadiusX = 2, RadiusY = 2, Fill = column is 3 or 7 ? accent : stroke, Opacity = .7 };
            Canvas.SetLeft(key, width * .12 + column * width * .063);
            Canvas.SetTop(key, height * .31 + row * height * .085);
            canvas.Children.Add(key);
        }
    }

    private static void DrawMouse(Canvas canvas, double width, double height, Brush fill, Brush stroke, Brush accent)
    {
        AddRounded(canvas, width * .31, height * .13, width * .38, height * .74, fill, stroke, Math.Min(width, height) * .18);
        canvas.Children.Add(new ShapeLine { X1 = width * .50, Y1 = height * .14, X2 = width * .50, Y2 = height * .43, Stroke = stroke, StrokeThickness = 2 });
        AddRounded(canvas, width * .47, height * .27, width * .06, height * .16, accent, stroke, 4);
    }

    private static void DrawGamepad(Canvas canvas, double width, double height, Brush fill, Brush stroke, Brush accent)
    {
        var body = new ShapePath
        {
            Data = Geometry.Parse($"M {width*.18},{height*.40} C {width*.20},{height*.18} {width*.38},{height*.18} {width*.50},{height*.32} C {width*.62},{height*.18} {width*.80},{height*.18} {width*.82},{height*.40} L {width*.75},{height*.78} C {width*.70},{height*.92} {width*.60},{height*.78} {width*.55},{height*.62} L {width*.45},{height*.62} C {width*.40},{height*.78} {width*.30},{height*.92} {width*.25},{height*.78} Z"),
            Fill = fill,
            Stroke = stroke,
            StrokeThickness = 2
        };
        canvas.Children.Add(body);
        AddCircle(canvas, width * .34, height * .47, Math.Min(width, height) * .055, accent);
        AddCircle(canvas, width * .66, height * .47, Math.Min(width, height) * .055, accent);
        AddCircle(canvas, width * .72, height * .36, Math.Min(width, height) * .025, accent);
        AddCircle(canvas, width * .78, height * .43, Math.Min(width, height) * .025, accent);
    }

    private static void DrawGenericStick(Canvas canvas, double width, double height, Brush fill, Brush stroke, Brush accent)
    {
        AddRounded(canvas, width * .25, height * .72, width * .50, height * .20, fill, stroke, 14);
        AddRounded(canvas, width * .44, height * .35, width * .12, height * .42, fill, stroke, 18);
        AddRounded(canvas, width * .35, height * .08, width * .30, height * .34, fill, stroke, 24);
        AddCircle(canvas, width * .50, height * .20, Math.Min(width, height) * .05, accent);
    }

    private static void AddRounded(Canvas canvas, double left, double top, double width, double height, Brush fill, Brush stroke, double radius, double angle = 0)
    {
        var rectangle = new ShapeRectangle { Width = width, Height = height, RadiusX = radius, RadiusY = radius, Fill = fill, Stroke = stroke, StrokeThickness = 2, RenderTransform = new RotateTransform(angle, width / 2, height / 2) };
        Canvas.SetLeft(rectangle, left);
        Canvas.SetTop(rectangle, top);
        canvas.Children.Add(rectangle);
    }

    private static void AddCircle(Canvas canvas, double centerX, double centerY, double radius, Brush fill)
    {
        var circle = new ShapeEllipse { Width = radius * 2, Height = radius * 2, Fill = fill };
        Canvas.SetLeft(circle, centerX - radius);
        Canvas.SetTop(circle, centerY - radius);
        canvas.Children.Add(circle);
    }
}
