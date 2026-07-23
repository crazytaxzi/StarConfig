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
    private bool SaveStateBindings(bool launchAfter)
    {
        if (_activeProfile is null || _selectedControl is null)
        {
            SetStatus("Open a profile and select a physical control first.");
            return false;
        }
        var changeMap = new Dictionary<string, (BindingEntry Binding, string Input)>(StringComparer.OrdinalIgnoreCase);
        var summary = new List<string>();
        foreach (var row in _stateRows.Values)
        {
            var keep = row.Include.IsChecked == true;
            var selected = row.SelectedBinding;
            foreach (var existing in row.InitialBindings)
                if (!keep || selected is null || existing.Identity != selected.Identity) changeMap[existing.Identity] = (existing, "Unbound");
            if (keep && selected is not null)
            {
                changeMap[selected.Identity] = (selected, _selectedControl.Input);
                summary.Add($"{row.Context}: {Humanize(selected.ActionName)}");
            }
        }
        if (changeMap.Count == 0)
        {
            SetStatus("No state-binding changes are pending.");
            if (launchAfter) LaunchRsi(null, new RoutedEventArgs());
            return true;
        }
        var confirm = MessageBox.Show(
            $"Apply {_selectedControl.Name} ({_selectedControl.Input}) to these checked states?\n\n{string.Join(Environment.NewLine, summary.Select(line => "• " + line))}\n\nUnchecked states currently using this control will be unbound. A timestamped backup will be created first.",
            "Confirm state bindings", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return false;
        try
        {
            var backup = _profiles.SaveBindings(_activeProfile, changeMap.Values);
            _lastSavedAt = DateTime.Now;
            LoadProfile(_activeProfile);
            SetStatus($"Saved {changeMap.Count} change(s). Backup: {System.IO.Path.GetFileName(backup)}");
            _saveStateText.Text = $"Profile saved {_lastSavedAt:t}";
            if (launchAfter) LaunchRsi(null, new RoutedEventArgs());
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Could not save bindings", MessageBoxButton.OK, MessageBoxImage.Error);
            SetStatus("Save failed. The original profile was not replaced.");
            return false;
        }
    }

    private void ValidateProfile(object sender, RoutedEventArgs e)
    {
        if (_activeProfile is null) { SetStatus("Open a profile before validating."); return; }
        var duplicateGroups = _allBindings.Where(binding => !binding.Input.Equals("Unbound", StringComparison.OrdinalIgnoreCase))
            .GroupBy(binding => $"{binding.Context}|{binding.Input}", StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1).ToList();
        var unbound = _allBindings.Count(binding => binding.Input.Equals("Unbound", StringComparison.OrdinalIgnoreCase));
        var message = duplicateGroups.Count == 0
            ? $"Profile XML is readable. No same-state duplicate inputs were found. {unbound:N0} actions are unbound."
            : $"Profile XML is readable. Found {duplicateGroups.Count} same-state duplicate input group(s). Review the Conflicts & Warnings panel. {unbound:N0} actions are unbound.";
        MessageBox.Show(message, "Profile validation", MessageBoxButton.OK, duplicateGroups.Count == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
        RebuildWarnings();
    }

    private void BackupProfile(object sender, RoutedEventArgs e)
    {
        if (_activeProfile is null) { SetStatus("Open a profile before creating a backup."); return; }
        try
        {
            var folder = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_activeProfile)!, "StarConfig Backups");
            Directory.CreateDirectory(folder);
            var backup = System.IO.Path.Combine(folder, $"{System.IO.Path.GetFileNameWithoutExtension(_activeProfile)}-{DateTime.Now:yyyyMMdd-HHmmssfff}.xml");
            File.Copy(_activeProfile, backup, false);
            SetStatus($"Backup created: {System.IO.Path.GetFileName(backup)}");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Backup failed", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void SaveAndLaunch(object sender, RoutedEventArgs e) => SaveStateBindings(true);

    private void ListenForInput(object sender, RoutedEventArgs e)
    {
        if (_capturing)
        {
            StopCapture();
            SetStatus("Input listening canceled.");
            return;
        }
        _captureBaselines.Clear();
        foreach (var device in _joysticks.GetConnectedDevices())
        {
            var snapshot = _joysticks.GetSnapshot(device.Id);
            if (snapshot is not null) _captureBaselines[device.Id] = snapshot;
        }
        _capturing = true;
        _listenButton.Content = "CANCEL LISTENING";
        _listenButton.Background = Paint("#5B2730");
        _captureTimer.Start();
        Focus();
        SetStatus("Listening: press a key, mouse button, joystick button or hat, or move one axis decisively. Escape cancels.");
    }

    private void CaptureTimerTick(object? sender, EventArgs e)
    {
        if (!_capturing) return;
        foreach (var baseline in _captureBaselines)
        {
            var activity = _joysticks.DetectActivity(baseline.Key, baseline.Value);
            if (activity is null) continue;
            AcceptCapturedInput(activity.Input, activity.Description);
            return;
        }
    }

    private void CaptureKeyboard(object sender, KeyEventArgs e)
    {
        if (!_capturing) return;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape)
        {
            StopCapture();
            SetStatus("Input listening canceled.");
            e.Handled = true;
            return;
        }
        var name = key switch
        {
            Key.Space => "space", Key.LeftCtrl => "lctrl", Key.RightCtrl => "rctrl", Key.LeftShift => "lshift", Key.RightShift => "rshift",
            Key.LeftAlt => "lalt", Key.RightAlt => "ralt", Key.Return => "enter", Key.Back => "backspace", _ => key.ToString().ToLowerInvariant()
        };
        AcceptCapturedInput($"kb1_{name}", $"Keyboard {key}");
        e.Handled = true;
    }

    private void CaptureMouse(object sender, MouseButtonEventArgs e)
    {
        if (!_capturing) return;
        var number = e.ChangedButton switch { MouseButton.Left => 1, MouseButton.Right => 2, MouseButton.Middle => 3, MouseButton.XButton1 => 4, MouseButton.XButton2 => 5, _ => 0 };
        if (number == 0) return;
        AcceptCapturedInput($"mouse1_button{number}", $"Mouse Button {number}");
        e.Handled = true;
    }

    private void AcceptCapturedInput(string input, string description)
    {
        StopCapture();
        SelectPhysicalControl(new PhysicalControl(input, description, InferControlType(input), "Captured"));
        SetStatus($"Captured {description}: {input}. Existing and suggested state bindings are shown on the right.");
    }

    private void StopCapture()
    {
        _captureTimer.Stop();
        _captureBaselines.Clear();
        _capturing = false;
        _listenButton.Content = "LISTEN FOR INPUT";
        _listenButton.Background = BlueDim;
    }

    private void LaunchRsi(object? sender, RoutedEventArgs e)
    {
        var candidates = new[]
        {
            System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Roberts Space Industries", "RSI Launcher", "RSI Launcher.exe"),
            System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "RSI Launcher", "RSI Launcher.exe")
        };
        var launcher = candidates.FirstOrDefault(File.Exists);
        if (launcher is null)
        {
            MessageBox.Show("RSI Launcher was not found in the common Windows locations.", "StarConfig", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        Process.Start(new ProcessStartInfo(launcher) { UseShellExecute = true });
    }

    private void UpdateEnabledState()
    {
        var hasProfile = _activeProfile is not null;
        _validateButton.IsEnabled = hasProfile;
        _backupButton.IsEnabled = hasProfile;
        _applyButton.IsEnabled = hasProfile && _selectedControl is not null;
        _saveLaunchButton.IsEnabled = hasProfile;
    }

    private void SetStatus(string text) => _statusText.Text = text;

    private static string DetectChannel(string file)
    {
        var segments = file.Split(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
        return segments.FirstOrDefault(segment => segment.Equals("LIVE", StringComparison.OrdinalIgnoreCase)
            || segment.Equals("PTU", StringComparison.OrdinalIgnoreCase)
            || segment.Equals("EPTU", StringComparison.OrdinalIgnoreCase)
            || segment.Equals("TECH-PREVIEW", StringComparison.OrdinalIgnoreCase))?.ToUpperInvariant() ?? "CUSTOM";
    }

    private static string InferControlType(string input)
    {
        if (input.Contains("_hat", StringComparison.OrdinalIgnoreCase)) return "Hat";
        if (input.Contains("_button", StringComparison.OrdinalIgnoreCase)) return "Button";
        if (input.StartsWith("kb", StringComparison.OrdinalIgnoreCase)) return "Key";
        if (input.StartsWith("mouse", StringComparison.OrdinalIgnoreCase)) return "Mouse";
        return "Axis";
    }

    private static string ContextIcon(string context) => context switch
    {
        "Flight" => "✈", "Vehicle" => "▣", "On Foot" => "♟", "EVA" => "◎", "Turret" => "⌖", "Mining" => "◆", "Salvage" => "◇", _ => "•"
    };

    private static string TitleCase(string value) => string.IsNullOrWhiteSpace(value) ? value : char.ToUpperInvariant(value[0]) + value[1..];
    private static string Humanize(string value) => System.Text.RegularExpressions.Regex.Replace(System.Text.RegularExpressions.Regex.Replace(value.Replace("v_", "", StringComparison.OrdinalIgnoreCase).Replace('_', ' '), "([a-z0-9])([A-Z])", "$1 $2"), "\\s+", " ").Trim();
}
