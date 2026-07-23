using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using StarConfig.Models;
using StarConfig.Services;

namespace StarConfig;

public partial class MainWindow : Window
{
    private readonly JoystickService _joysticks = new();
    private readonly StarCitizenProfileService _profiles = new();
    private readonly ObservableCollection<BindingEntry> _allBindings = [];
    private string? _mappingsFolder;
    private string? _activeProfile;

    public MainWindow()
    {
        InitializeComponent();
        RefreshDevices();
        _mappingsFolder = _profiles.FindDefaultMappingsFolder();
        if (_mappingsFolder is not null) LoadProfiles();
    }

    private void RefreshDevices_Click(object sender, RoutedEventArgs e) => RefreshDevices();

    private void RefreshDevices()
    {
        var devices = _joysticks.GetConnectedDevices();
        DevicesList.ItemsSource = devices;
        StatusText.Text = $"{devices.Count} connected input device(s) detected.";
    }

    private void ChooseFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Choose Star Citizen Controls\\Mappings folder" };
        if (dialog.ShowDialog() == true)
        {
            _mappingsFolder = dialog.FolderName;
            LoadProfiles();
        }
    }

    private void LoadProfiles()
    {
        var files = _profiles.GetProfiles(_mappingsFolder!);
        ProfilesList.ItemsSource = files.Select(path => new FileInfo(path)).ToList();
        StatusText.Text = $"Found {files.Count} XML profile(s) in {_mappingsFolder}";
    }

    private void LoadProfile_Click(object sender, RoutedEventArgs e)
    {
        if (ProfilesList.SelectedItem is not FileInfo selected)
        {
            MessageBox.Show("Select a profile first.", "StarConfig", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        LoadProfile(selected.FullName);
    }

    private void LoadProfile(string file)
    {
        try
        {
            _activeProfile = file;
            _allBindings.Clear();
            foreach (var binding in _profiles.LoadBindings(file)) _allBindings.Add(binding);
            ApplyFilter();
            StatusText.Text = $"Loaded {Path.GetFileName(file)} with {_allBindings.Count} actions.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Could not load profile", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    private void ApplyFilter()
    {
        var term = SearchBox.Text?.Trim() ?? string.Empty;
        BindingsList.ItemsSource = string.IsNullOrWhiteSpace(term)
            ? _allBindings
            : _allBindings.Where(x => x.ActionName.Contains(term, StringComparison.OrdinalIgnoreCase)
                                   || x.Context.Contains(term, StringComparison.OrdinalIgnoreCase)
                                   || x.Input.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private void BindingsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BindingsList.SelectedItem is not BindingEntry binding) return;
        ActionNameText.Text = binding.ActionName;
        ActionContextText.Text = binding.Context;
        ActionDescriptionText.Text = binding.Description;
        InputTextBox.Text = binding.Input;
        ApplyBindingButton.IsEnabled = _activeProfile is not null;
    }

    private void ApplyBinding_Click(object sender, RoutedEventArgs e)
    {
        if (_activeProfile is null || BindingsList.SelectedItem is not BindingEntry binding) return;
        try
        {
            var input = InputTextBox.Text.Trim();
            var selected = new List<BindingEntry> { binding };
            if (!string.IsNullOrWhiteSpace(binding.IntentKey))
            {
                var peers = _allBindings.Where(x => x.Identity != binding.Identity && x.IntentKey == binding.IntentKey && x.Context != binding.Context).ToList();
                if (peers.Count > 0)
                {
                    var dialog = new IntentSuggestionWindow(binding, peers, input) { Owner = this };
                    if (dialog.ShowDialog() != true) return;
                    selected = dialog.SelectedBindings.ToList();
                    if (selected.Count == 0) return;
                }
            }

            _profiles.SaveBindings(_activeProfile, selected.Select(x => new BindingUpdate(x.ActionMap, x.ActionName, input)).ToList());
            LoadProfile(_activeProfile);
            StatusText.Text = $"Saved {selected.Count} binding(s). Timestamped backup created.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Could not save binding", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LaunchRsi_Click(object sender, RoutedEventArgs e)
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Roberts Space Industries", "RSI Launcher", "RSI Launcher.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "RSI Launcher", "RSI Launcher.exe")
        };
        var launcher = candidates.FirstOrDefault(File.Exists);
        if (launcher is null)
        {
            MessageBox.Show("RSI Launcher was not found in the common Windows locations.", "StarConfig", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        Process.Start(new ProcessStartInfo(launcher) { UseShellExecute = true });
    }
}
