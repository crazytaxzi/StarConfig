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
    private void DrawResponseGraph(double liveValue = 0.65)
    {
        _responseGraph.Children.Clear();
        var width = Math.Max(160, _responseGraph.ActualWidth > 1 ? _responseGraph.ActualWidth : 180);
        var height = 95d;
        for (var i = 1; i < 4; i++)
        {
            _responseGraph.Children.Add(new ShapeLine { X1 = 0, Y1 = i * height / 4, X2 = width, Y2 = i * height / 4, Stroke = Border, StrokeThickness = 0.7 });
            _responseGraph.Children.Add(new ShapeLine { X1 = i * width / 4, Y1 = 0, X2 = i * width / 4, Y2 = height, Stroke = Border, StrokeThickness = 0.7 });
        }
        var deadzone = ParsePercent(_deadzonePicker.SelectedItem as string);
        var curve = _curvePicker.SelectedItem as string ?? "Linear";
        var points = new PointCollection();
        for (var i = 0; i <= 40; i++)
        {
            var x = i / 40d;
            var normalized = x <= deadzone ? 0 : (x - deadzone) / (1 - deadzone);
            var y = curve switch { "Gentle" => Math.Pow(normalized, 1.35), "Aggressive" => Math.Pow(normalized, 0.72), _ => normalized };
            points.Add(new Point(x * width, height - y * height));
        }
        _responseGraph.Children.Add(new ShapePolyline { Points = points, Stroke = Blue, StrokeThickness = 2 });
        var markerX = Math.Clamp(liveValue, 0, 1) * width;
        _responseGraph.Children.Add(new ShapeLine { X1 = markerX, Y1 = 0, X2 = markerX, Y2 = height, Stroke = Green, StrokeThickness = 1.5 });
    }

    private void LoadAxisSettings(StarbindControl control)
    {
        _deadzonePicker.IsEnabled = control.IsAxis;
        _curvePicker.IsEnabled = control.IsAxis;
        if (!control.IsAxis || _profile is null || _selectedDevice is null) return;
        var (_, suffix) = StarbindInput.Split(control.Input);
        var option = _profile.AxisOptions.FirstOrDefault(x => x.DeviceInstance == control.DeviceInstance && x.Axis.Equals(suffix, StringComparison.OrdinalIgnoreCase));
        var percent = (int)Math.Round((option?.Deadzone ?? 0) * 100);
        _deadzonePicker.SelectedItem = new[] { 0, 2, 5, 7, 10, 15, 20 }.OrderBy(x => Math.Abs(x - percent)).First() + "%";
    }

    private void ToggleListen(object sender, RoutedEventArgs e)
    {
        if (_capturing) { StopListening(); SetStatus("Input listening canceled."); return; }
        _captureBaselines.Clear();
        foreach (var device in _joysticks.GetConnectedDevices())
        {
            var snapshot = _joysticks.GetSnapshot(device.Id);
            if (snapshot is not null) _captureBaselines[device.Id] = snapshot;
        }
        _capturing = true;
        _listenButton.Content = "■  Stop Listening";
        _listenButton.Background = Brush("#5B2830");
        _inputTimer.Start();
        Focus();
        SetStatus("Listening for a key, mouse button, joystick button, hat, or decisive axis movement. Escape cancels.");
    }

    private void StopListening()
    {
        _capturing = false;
        _captureBaselines.Clear();
        _inputTimer.Stop();
        _listenButton.Content = "◎  Test / Listen";
        _listenButton.Background = BlueDim;
    }

    private void InputTimerTick(object? sender, EventArgs e)
    {
        if (_selectedControl is not null && _selectedControl.DeviceInstance > 0 && _selectedControl.Kind == StarbindControlKind.Axis)
        {
            var snapshot = _joysticks.GetSnapshot(_selectedControl.DeviceInstance);
            if (snapshot is not null) DrawResponseGraph(ReadAxisValue(snapshot, StarbindInput.Split(_selectedControl.Input).Suffix));
        }
        if (!_capturing) return;
        foreach (var baseline in _captureBaselines)
        {
            var activity = _joysticks.DetectActivity(baseline.Key, baseline.Value);
            if (activity is null) continue;
            StopListening();
            var device = _profile?.Devices.FirstOrDefault(x => x.Kind == StarbindDeviceKind.Joystick && x.Instance == baseline.Key);
            if (device is not null) _selectedDevice = device;
            SelectControl(ControlFromInput(activity.Input));
            SetStatus($"Captured {activity.Description}. Its existing and suggested assignments are shown.");
            return;
        }
    }

}
