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
    private void CaptureKeyboard(object sender, KeyEventArgs e)
    {
        if (!_capturing) return;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape) { StopListening(); SetStatus("Input listening canceled."); e.Handled = true; return; }
        var name = key switch { Key.Space => "space", Key.LeftCtrl => "lctrl", Key.RightCtrl => "rctrl", Key.LeftShift => "lshift", Key.RightShift => "rshift", Key.LeftAlt => "lalt", Key.RightAlt => "ralt", Key.Return => "enter", _ => key.ToString().ToLowerInvariant() };
        StopListening();
        SelectControl(ControlFromInput($"kb1_{name}"));
        e.Handled = true;
    }

    private void CaptureMouse(object sender, MouseButtonEventArgs e)
    {
        if (!_capturing) return;
        var number = e.ChangedButton switch { MouseButton.Left => 1, MouseButton.Right => 2, MouseButton.Middle => 3, MouseButton.XButton1 => 4, MouseButton.XButton2 => 5, _ => 0 };
        if (number == 0) return;
        StopListening();
        SelectControl(ControlFromInput($"mouse1_button{number}"));
        e.Handled = true;
    }

    private void ApplyAssignments(object sender, RoutedEventArgs e)
    {
        if (_profile is null || _selectedControl is null) { SetStatus("Open a profile and select a control first."); return; }
        var dialog = new StateConfirmationWindow(_selectedControl, _stateAssignments.Values.ToList()) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        foreach (var result in dialog.Results)
        {
            _stateAssignments[result.Context].Enabled = result.Enabled;
            _stateAssignments[result.Context].SelectedAction = result.Action;
        }
        SaveCurrentAssignments(false);
    }

    private bool SaveCurrentAssignments(bool launchAfter)
    {
        if (_profile is null || _selectedControl is null) return false;
        var mutations = new List<BindingMutation>();
        foreach (var assignment in _stateAssignments.Values)
        {
            mutations.Add(new BindingMutation(BindingMutationKind.RemoveInput, assignment.Context, string.Empty, -1, string.Empty, _selectedControl.Input));
            if (assignment.Enabled && assignment.SelectedAction is not null)
            {
                var action = assignment.SelectedAction;
                mutations.Add(new BindingMutation(BindingMutationKind.AddInput, assignment.Context, action.ActionMap, action.ActionOrdinal, action.ActionName, _selectedControl.Input));
            }
        }
        var axisOptions = new List<StarbindDeviceAxisOption>();
        if (_selectedControl.IsAxis && _selectedDevice is not null)
        {
            var (_, suffix) = StarbindInput.Split(_selectedControl.Input);
            axisOptions.Add(new StarbindDeviceAxisOption(_selectedDevice.ProductName, _selectedDevice.Instance, suffix, ParsePercent(_deadzonePicker.SelectedItem as string), false));
        }
        try
        {
            var backup = _profiles.SaveAssignments(_profile, mutations, axisOptions);
            _lastSaved = DateTime.Now;
            var path = _profile.FilePath;
            LoadProfile(path);
            _savedText.Text = $"Profile saved {_lastSaved:t}";
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

    private void SaveAndLaunch(object sender, RoutedEventArgs e)
    {
        if (_profile is null) { SetStatus("Open a profile first."); return; }
        if (_selectedControl is null || _stateAssignments.Count == 0) LaunchRsi();
        else SaveCurrentAssignments(true);
    }

    private void ValidateProfile(object sender, RoutedEventArgs e)
    {
        if (_profile is null) { SetStatus("Open a profile first."); return; }
        var malformed = _profile.Actions.Count(x => x.Input != StarbindInput.Unbound && !x.Input.Contains('_'));
        var duplicateGroups = _profile.Actions.Where(x => x.IsBound).GroupBy(x => $"{x.Context}|{x.Input}", StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).ToList();
        var unbound = _profile.Actions.Count(x => !x.IsBound);
        var text = $"Profile: {_profile.ProfileName}\nActions: {_profile.Actions.Count:N0}\nUnbound slots: {unbound:N0}\nSame-state duplicate inputs: {duplicateGroups.Count:N0}\nMalformed inputs: {malformed:N0}";
        MessageBox.Show(text, "Starbind profile validation", MessageBoxButton.OK, malformed == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
        BuildWarnings();
    }

    private void BackupProfile(object sender, RoutedEventArgs e)
    {
        if (_profile is null) { SetStatus("Open a profile first."); return; }
        try { var backup = _profiles.CreateBackup(_profile.FilePath); SetStatus($"Backup created: {Path.GetFileName(backup)}"); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Backup failed", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

}
