using Microsoft.Win32;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace StarConfig;

public sealed partial class StarbindV5Window
{
    private void ToggleListen(object sender, RoutedEventArgs e)
    {
        if (_capturing)
        {
            StopListening();
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
        _listenButton.Content = "■  STOP LISTENING";
        _listenButton.Background = Paint("#5B2830");
        _inputTimer.Start();
        Focus();
        SetStatus("Listening for a key, mouse button, joystick button, hat, or decisive axis movement. Escape cancels.");
    }

    private void StopListening()
    {
        _capturing = false;
        _captureBaselines.Clear();
        _inputTimer.Stop();
        _listenButton.Content = "◎  LISTEN FOR INPUT";
        _listenButton.Background = BlueDim;
    }

    private void CaptureKeyboard(object sender, KeyEventArgs e)
    {
        if (!_capturing) return;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape)
        {
            StopListening();
            SetStatus("Input listening canceled.");
            e.Handled = true;
            return;
        }
        var name = key switch
        {
            Key.Space => "space",
            Key.LeftCtrl => "lctrl",
            Key.RightCtrl => "rctrl",
            Key.LeftShift => "lshift",
            Key.RightShift => "rshift",
            Key.LeftAlt => "lalt",
            Key.RightAlt => "ralt",
            Key.Return => "enter",
            Key.Back => "backspace",
            _ => key.ToString().ToLowerInvariant()
        };
        StopListening();
        SelectCapturedControl($"kb1_{name}", $"Keyboard {key}");
        e.Handled = true;
    }

    private void CaptureMouse(object sender, MouseButtonEventArgs e)
    {
        if (!_capturing) return;
        var number = e.ChangedButton switch
        {
            MouseButton.Left => 1,
            MouseButton.Right => 2,
            MouseButton.Middle => 3,
            MouseButton.XButton1 => 4,
            MouseButton.XButton2 => 5,
            _ => 0
        };
        if (number == 0) return;
        StopListening();
        SelectCapturedControl($"mouse1_button{number}", $"Mouse Button {number}");
        e.Handled = true;
    }

    private void InputTimerTick(object? sender, EventArgs e)
    {
        if (_selectedControl?.IsAxis == true)
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
            SelectCapturedControl(activity.Input, activity.Description);
            return;
        }
    }

    private void SelectCapturedControl(string input, string description)
    {
        var instance = StarbindInput.DeviceInstance(input);
        var prefix = StarbindInput.Split(input).Prefix;
        var device = CurrentDevices().FirstOrDefault(candidate => candidate.InputPrefix.Equals(prefix, StringComparison.OrdinalIgnoreCase))
            ?? CurrentDevices().FirstOrDefault(candidate => candidate.Instance == instance && candidate.Kind == StarbindDeviceKind.Joystick);
        if (device is not null && (_selectedDevice is null || !SameDevice(device, _selectedDevice))) SelectDevice(device);
        var control = _selectedDevice is null
            ? ControlFromInput(input, description)
            : _hardware.BuildControls(_selectedDevice, _profile, _settings).FirstOrDefault(candidate => candidate.Input.Equals(input, StringComparison.OrdinalIgnoreCase)) ?? ControlFromInput(input, description);
        SelectControl(control);
        SetStatus($"Captured {description}. Its existing and suggested state bindings are ready.");
    }

    private void OpenStateBindingEditor(object sender, RoutedEventArgs e)
    {
        if (_selectedControl is null || _visibleStates.Count == 0)
        {
            SetStatus("Select a physical control first.");
            return;
        }
        var editor = new StateBindingDecisionWindow(_selectedControl, _visibleStates.Values.Select(state => state.Clone()).ToList()) { Owner = this };
        if (editor.ShowDialog() != true) return;
        var plan = GetOrCreatePlan(_selectedControl);
        foreach (var result in editor.Results)
        {
            if (!plan.States.TryGetValue(result.Context, out var state)) continue;
            state.Enabled = result.Enabled;
            state.Action = result.Action;
            UpdateStateStatus(state);
        }
        plan.IsDirty = true;
        MarkDirty();
        BuildStateRows();
        BuildWarnings();
        BuildStateOverview();
        SetStatus("State choices staged. Use Save when the complete profile is ready.");
    }

    private void OpenActionOptions(object sender, RoutedEventArgs e)
    {
        if (_selectedControl is null) return;
        var menu = new ContextMenu { Background = Panel, Foreground = Text, BorderBrush = Border };
        var assignCompatible = new MenuItem { Header = "Assign compatible actions to empty states" };
        assignCompatible.Click += (_, _) =>
        {
            if (_selectedAction is null) return;
            SuggestCompatibleStates(_selectedAction, includeExisting: false);
        };
        var clearState = new MenuItem { Header = "Clear selected action state" };
        clearState.Click += (_, _) =>
        {
            if (_selectedAction is null || !_visibleStates.TryGetValue(_selectedAction.Context, out var state)) return;
            state.Enabled = false;
            UpdateStateStatus(state);
            MarkCurrentPlanDirty();
            BuildStateRows();
            BuildWarnings();
            BuildStateOverview();
        };
        var clearAll = new MenuItem { Header = "Clear this control from every state" };
        clearAll.Click += (_, _) =>
        {
            foreach (var state in _visibleStates.Values) { state.Enabled = false; UpdateStateStatus(state); }
            MarkCurrentPlanDirty();
            BuildStateRows();
            BuildWarnings();
            BuildStateOverview();
        };
        var copy = new MenuItem { Header = "Copy physical input name" };
        copy.Click += (_, _) => Clipboard.SetText(_selectedControl.Input);
        menu.Items.Add(assignCompatible);
        menu.Items.Add(clearState);
        menu.Items.Add(clearAll);
        menu.Items.Add(new Separator());
        menu.Items.Add(copy);
        menu.IsOpen = true;
    }

    private void SuggestCompatibleStates(StarbindAction action, bool includeExisting)
    {
        foreach (var state in _visibleStates.Values)
        {
            if (state.Context.Equals(action.Context, StringComparison.OrdinalIgnoreCase)) continue;
            if (!includeExisting && state.Existing.Count > 0) continue;
            var peer = state.Choices.FirstOrDefault(candidate => candidate.Intent.Equals(action.Intent, StringComparison.OrdinalIgnoreCase)
                && candidate.Behavior.Equals(action.Behavior, StringComparison.OrdinalIgnoreCase));
            if (peer is null) continue;
            state.Action = peer;
            state.Enabled = true;
            state.Status = "SUGGESTED";
        }
        MarkCurrentPlanDirty();
        BuildStateRows();
        BuildWarnings();
        BuildStateOverview();
    }

    private void OpenSaveMenu(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu { Background = Panel, Foreground = Text, BorderBrush = Border };
        var save = new MenuItem { Header = "Save Profile" };
        save.Click += (_, _) => SaveWorkspace(false);
        var saveCopy = new MenuItem { Header = "Save Profile As Copy..." };
        saveCopy.Click += SaveProfileCopy;
        var launchOnly = new MenuItem { Header = "Launch Star Citizen without saving" };
        launchOnly.Click += (_, _) => LaunchRsi();
        menu.Items.Add(save);
        menu.Items.Add(saveCopy);
        menu.Items.Add(new Separator());
        menu.Items.Add(launchOnly);
        menu.PlacementTarget = _saveMenuButton;
        menu.Placement = PlacementMode.Top;
        menu.IsOpen = true;
    }

    private void SaveAndLaunch(object sender, RoutedEventArgs e)
    {
        if (HasPendingChanges()) SaveWorkspace(true);
        else LaunchRsi();
    }

    private bool SaveWorkspace(bool launchAfter)
    {
        if (_profile is null)
        {
            SetStatus("Open a profile first.");
            return false;
        }
        var dirtyPlans = _pendingPlans.Values.Where(plan => plan.IsDirty).ToList();
        var tunings = _axisTunings.Values.ToList();
        if (dirtyPlans.Count == 0 && tunings.Count == 0)
        {
            SetStatus("No unsaved changes.");
            if (launchAfter) LaunchRsi();
            return true;
        }

        if (_settings.ConfirmBeforeWrite)
        {
            var summary = $"Save {dirtyPlans.Count} changed physical control(s) and {tunings.Count} axis tuning change(s)?\n\nA timestamped backup will be created and the new XML will be parsed before replacing the original.";
            if (MessageBox.Show(summary, "Save Starbind profile", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return false;
        }

        try
        {
            var backup = _profiles.SaveWorkspace(_profile, dirtyPlans, tunings);
            _lastSaved = DateTime.Now;
            var path = _profile.FilePath;
            LoadProfile(path);
            _savedText.Text = $"Profile saved {_lastSaved:t}";
            _savedText.Foreground = Muted;
            SetStatus($"Saved safely. Backup: {Path.GetFileName(backup)}");
            if (launchAfter) LaunchRsi();
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Save failed", MessageBoxButton.OK, MessageBoxImage.Error);
            SetStatus("Save failed. The original profile was not replaced.");
            return false;
        }
    }

    private void SaveProfileCopy(object? sender, RoutedEventArgs e)
    {
        if (_profile is null) return;
        var dialog = new SaveFileDialog
        {
            Title = "Save Star Citizen profile copy",
            Filter = "Star Citizen profile (*.xml)|*.xml",
            FileName = Path.GetFileNameWithoutExtension(_profile.FilePath) + "_copy.xml",
            InitialDirectory = Path.GetDirectoryName(_profile.FilePath)
        };
        if (dialog.ShowDialog(this) != true) return;
        File.Copy(_profile.FilePath, dialog.FileName, true);
        SetStatus($"Profile copy saved: {dialog.FileName}");
    }

    private void ValidateProfile(object sender, RoutedEventArgs e)
    {
        if (_profile is null)
        {
            SetStatus("Open a profile first.");
            return;
        }
        try
        {
            _ = System.Xml.Linq.XDocument.Load(_profile.FilePath);
            var conflicts = FindConflictGroups();
            var malformed = _profile.Actions.Count(action => action.Input != StarbindInput.Unbound && !action.Input.Contains('_'));
            var unbound = _profile.Actions.Count(action => !action.IsBound);
            var pending = _pendingPlans.Values.Count(plan => plan.IsDirty) + _axisTunings.Count;
            var text = $"Profile: {_profile.ProfileName}\nActions: {_profile.Actions.Count:N0}\nUnbound slots: {unbound:N0}\nDuplicate input groups: {conflicts.Count:N0}\nMalformed inputs: {malformed:N0}\nPending edits: {pending:N0}";
            MessageBox.Show(text, "Starbind profile validation", MessageBoxButton.OK, malformed == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
            BuildWarnings();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Profile validation failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BackupProfile(object sender, RoutedEventArgs e)
    {
        if (_profile is null)
        {
            SetStatus("Open a profile first.");
            return;
        }
        try
        {
            var backup = _profiles.CreateBackup(_profile.FilePath);
            SetStatus($"Backup created: {Path.GetFileName(backup)}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Backup failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LaunchRsi()
    {
        var launcher = _settingsStore.FindLauncher(_settings);
        if (launcher is null)
        {
            MessageBox.Show("RSI Launcher was not found. Open Settings and choose RSI Launcher.exe.", "Starbind", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        _settings.LauncherPath = launcher;
        _settingsStore.Save(_settings);
        Process.Start(new ProcessStartInfo(launcher) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(launcher) });
    }

    private void OpenGameManual(object sender, RoutedEventArgs e)
    {
        var query = Uri.EscapeDataString((_selectedAction?.DisplayName ?? "keybindings") + " Star Citizen controls");
        Process.Start(new ProcessStartInfo($"https://support.robertsspaceindustries.com/hc/en-us/search?query={query}") { UseShellExecute = true });
    }

    private bool HasPendingChanges() => _pendingPlans.Values.Any(plan => plan.IsDirty) || _axisTunings.Count > 0;

    private static StarbindControl ControlFromInput(string input, string? friendlyName = null)
    {
        var normalized = StarbindInput.Normalize(input);
        var (prefix, suffix) = StarbindInput.Split(normalized);
        var kind = StarbindInput.KindOf(normalized);
        var name = friendlyName ?? kind switch
        {
            StarbindControlKind.Axis => suffix.ToLowerInvariant() switch
            {
                "x" => "X Axis",
                "y" => "Y Axis",
                "z" => "Z Axis",
                "rotx" => "RX Axis",
                "roty" => "RY Axis",
                "rotz" => "RZ Axis",
                "slider1" => "Slider 1",
                "slider2" => "Slider 2",
                _ => StarbindText.Humanize(suffix)
            },
            StarbindControlKind.Button => "Button " + new string(suffix.Where(char.IsDigit).ToArray()),
            StarbindControlKind.Hat => StarbindText.Humanize(suffix),
            StarbindControlKind.Key => "Key " + StarbindText.Humanize(suffix),
            StarbindControlKind.MouseButton => "Mouse Button " + new string(suffix.Where(char.IsDigit).ToArray()),
            _ => StarbindText.Humanize(suffix)
        };
        return new StarbindControl(normalized, name, kind, StarbindInput.DeviceInstance(normalized), prefix, suffix.ToUpperInvariant());
    }

    private static double ReadAxisValue(JoystickSnapshot snapshot, string axis) => axis.ToLowerInvariant() switch
    {
        "x" => snapshot.X / 65535d,
        "y" => snapshot.Y / 65535d,
        "z" => snapshot.Z / 65535d,
        "rotx" => snapshot.R / 65535d,
        "roty" => snapshot.U / 65535d,
        "rotz" => snapshot.V / 65535d,
        "slider1" => snapshot.U / 65535d,
        "slider2" => snapshot.V / 65535d,
        _ => 0.5
    };

    private void SetStatus(string text) => _status.Text = text;
}
