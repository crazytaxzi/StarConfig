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
    private void BrowseProfile(object? sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Open exported Star Citizen controls", Filter = "Exported Star Citizen profiles (layout_*_exported.xml)|layout_*_exported.xml|XML files (*.xml)|*.xml|All files (*.*)|*.*", CheckFileExists = true };
        if (dialog.ShowDialog(this) != true) return;
        var known = (_profilePicker.ItemsSource as IEnumerable<ProfileChoice> ?? []).Select(x => x.Path).ToList();
        if (!known.Contains(dialog.FileName, StringComparer.OrdinalIgnoreCase)) known.Insert(0, dialog.FileName);
        PopulateProfilePicker(known, dialog.FileName);
    }

    private void OpenProfilesFolder(object sender, RoutedEventArgs e)
    {
        if (_profile is null) { BrowseProfile(sender, e); return; }
        Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{_profile.FilePath}\"") { UseShellExecute = true });
    }

    private void OpenSettings(object sender, RoutedEventArgs e)
    {
        var message = _profile is null ? "No profile is loaded." : $"Profile: {_profile.FilePath}\nBackups: {Path.Combine(Path.GetDirectoryName(_profile.FilePath)!, "Starbind Backups")}\n\nAxis deadzone changes are written into the profile's deviceoptions section. Curves are preview-only unless Star Citizen exposes a safe matching exponent option.";
        MessageBox.Show(message, "Starbind settings", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OpenHelp(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("1. Open an exported layout_*_exported.xml profile.\n2. Select or capture a physical control.\n3. Review every state that uses it.\n4. Choose actions and uncheck states that should not share the control.\n5. Apply. Starbind creates a backup before writing the XML.", "Starbind help", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void LaunchRsi()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Roberts Space Industries", "RSI Launcher", "RSI Launcher.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "RSI Launcher", "RSI Launcher.exe")
        };
        var launcher = candidates.FirstOrDefault(File.Exists);
        if (launcher is null) { MessageBox.Show("RSI Launcher was not found in its common locations.", "Starbind", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        Process.Start(new ProcessStartInfo(launcher) { UseShellExecute = true });
    }

    private static StarbindControl ControlFromInput(string input)
    {
        var normalized = StarbindInput.Normalize(input);
        var (prefix, suffix) = StarbindInput.Split(normalized);
        var kind = StarbindInput.KindOf(normalized);
        var name = kind switch
        {
            StarbindControlKind.Axis => suffix.ToLowerInvariant() switch { "x" => "X Axis", "y" => "Y Axis", "z" => "Z Axis", "rotx" => "RX Axis", "roty" => "RY Axis", "rotz" => "RZ Axis", "slider1" => "Slider 1", "slider2" => "Slider 2", _ => StarbindText.Humanize(suffix) },
            StarbindControlKind.Button => "Button " + new string(suffix.Where(char.IsDigit).ToArray()),
            StarbindControlKind.Hat => StarbindText.Humanize(suffix),
            StarbindControlKind.Key => "Key " + StarbindText.Humanize(suffix),
            StarbindControlKind.MouseButton => "Mouse Button " + new string(suffix.Where(char.IsDigit).ToArray()),
            _ => StarbindText.Humanize(suffix)
        };
        return new StarbindControl(normalized, name, kind, StarbindInput.DeviceInstance(normalized), prefix, suffix.ToUpperInvariant());
    }

    private static bool SameAction(StarbindAction left, StarbindAction right) => left.ActionOrdinal == right.ActionOrdinal
        && left.ActionMap.Equals(right.ActionMap, StringComparison.OrdinalIgnoreCase)
        && left.ActionName.Equals(right.ActionName, StringComparison.OrdinalIgnoreCase);

    private static StarbindAction? FindPickerAction(ComboBox picker, StarbindAction action) => (picker.ItemsSource as IEnumerable<StarbindAction>)?.FirstOrDefault(candidate => SameAction(candidate, action));

    private static int NaturalControlOrder(string input)
    {
        var digits = new string(input.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var result) ? result : input.GetHashCode(StringComparison.OrdinalIgnoreCase);
    }

    private static double ReadAxisValue(JoystickSnapshot snapshot, string axis) => axis.ToLowerInvariant() switch
    {
        "x" => snapshot.X / 65535d, "y" => snapshot.Y / 65535d, "z" => snapshot.Z / 65535d, "rotx" => snapshot.R / 65535d,
        "slider1" => snapshot.U / 65535d, "slider2" => snapshot.V / 65535d, _ => 0.5
    };

    private static double ParsePercent(string? value)
    {
        var clean = (value ?? "0").Replace("%", string.Empty).Trim();
        return double.TryParse(clean, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? Math.Clamp(result / 100d, 0, 0.95) : 0;
    }

    private void SetStatus(string text) => _status.Text = text;
    private sealed record ProfileChoice(string Path, string Name);
}
