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
    private void LoadProfilePicker(string? preferredProfile)
    {
        _loadingProfile = true;
        try
        {
            var files = _profiles.GetProfiles(_mappingFolders).Select(path => new FileInfo(path)).ToList();
            if (!string.IsNullOrWhiteSpace(preferredProfile) && File.Exists(preferredProfile) && files.All(f => !f.FullName.Equals(preferredProfile, StringComparison.OrdinalIgnoreCase))) files.Add(new FileInfo(preferredProfile));
            files = files.OrderByDescending(f => f.FullName.Equals(preferredProfile, StringComparison.OrdinalIgnoreCase)).ThenBy(f => f.Name).ToList();
            _profilePicker.ItemsSource = files;
            _profilePicker.DisplayMemberPath = nameof(FileInfo.Name);
            var preferred = files.FirstOrDefault(f => f.FullName.Equals(preferredProfile, StringComparison.OrdinalIgnoreCase));
            _profilePicker.SelectedItem = preferred ?? files.FirstOrDefault();
            if (files.Count == 0)
            {
                _activeProfile = null;
                _allBindings.Clear();
                _channelText.Text = "NO PROFILE";
                _channelText.Foreground = Amber;
                _emptyProfileBanner.Visibility = Visibility.Visible;
                _actionTree.Visibility = Visibility.Collapsed;
                UpdateEnabledState();
            }
        }
        finally { _loadingProfile = false; }
        if (_profilePicker.SelectedItem is FileInfo selected) LoadProfile(selected.FullName);
    }

    private void ProfileSelectionChanged()
    {
        if (_loadingProfile || _profilePicker.SelectedItem is not FileInfo file) return;
        LoadProfile(file.FullName);
    }

    private void LoadProfile(string file)
    {
        try
        {
            StopCapture();
            _activeProfile = file;
            _allBindings.Clear();
            foreach (var binding in _profiles.LoadBindings(file)) _allBindings.Add(binding);
            _mappingFolders.Add(System.IO.Path.GetDirectoryName(file)!);
            _settings.Save(_mappingFolders, file);
            _channelText.Text = DetectChannel(file);
            _channelText.Foreground = Green;
            _emptyProfileBanner.Visibility = Visibility.Collapsed;
            _actionTree.Visibility = Visibility.Visible;
            RebuildActionBrowser();
            RebuildStateAssignments();
            RebuildWarnings();
            RebuildStateOverview();
            UpdateEnabledState();
            SetStatus($"Loaded {System.IO.Path.GetFileName(file)} with {_allBindings.Count:N0} actions.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Could not load profile", MessageBoxButton.OK, MessageBoxImage.Error);
            SetStatus("Profile load failed.");
        }
    }

    private void BrowseForProfile(object? sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open an exported Star Citizen control profile",
            Filter = "Star Citizen profiles (layout_*_exported.xml)|layout_*_exported.xml|XML profiles (*.xml)|*.xml|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog(this) != true) return;
        var folder = System.IO.Path.GetDirectoryName(dialog.FileName)!;
        _mappingFolders.Add(folder);
        _settings.Save(_mappingFolders, dialog.FileName);
        LoadProfilePicker(dialog.FileName);
    }

    private void RebuildActionBrowser()
    {
        _actionTree.Items.Clear();
        if (_activeProfile is null) return;
        var term = _actionSearch.Text.Trim();
        var filtered = _allBindings.Where(binding => MatchesActionSearch(binding, term)).ToList();
        _actionResults.Clear();
        foreach (var binding in filtered) _actionResults.Add(binding);
        foreach (var context in ContextOrder.Where(context => filtered.Any(binding => binding.Context.Equals(context, StringComparison.OrdinalIgnoreCase))))
        {
            var root = TreeGroup(context.ToUpperInvariant());
            var contextBindings = filtered.Where(binding => binding.Context.Equals(context, StringComparison.OrdinalIgnoreCase)).OrderBy(binding => Humanize(binding.ActionName));
            foreach (var binding in contextBindings)
            {
                var item = new TreeViewItem { Header = BuildActionTreeHeader(binding), Tag = binding, Foreground = Text, Padding = new Thickness(3) };
                root.Items.Add(item);
            }
            root.IsExpanded = !string.IsNullOrWhiteSpace(term) || context == "Flight";
            _actionTree.Items.Add(root);
        }
    }

    private static bool MatchesActionSearch(BindingEntry binding, string term)
    {
        if (string.IsNullOrWhiteSpace(term)) return true;
        return binding.ActionName.Contains(term, StringComparison.OrdinalIgnoreCase)
            || Humanize(binding.ActionName).Contains(term, StringComparison.OrdinalIgnoreCase)
            || binding.ActionMap.Contains(term, StringComparison.OrdinalIgnoreCase)
            || binding.Context.Contains(term, StringComparison.OrdinalIgnoreCase)
            || binding.Intent.Contains(term, StringComparison.OrdinalIgnoreCase)
            || binding.Behavior.Contains(term, StringComparison.OrdinalIgnoreCase)
            || binding.Input.Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    private UIElement BuildActionTreeHeader(BindingEntry binding)
    {
        var panel = new DockPanel { LastChildFill = true };
        var behavior = new Border
        {
            Background = binding.Behavior switch { "Axis" => BlueDim, "Toggle" => Paint("#4A2C68"), "Cycle" => Paint("#5B4320"), "Hold" => Paint("#244C3A"), _ => FieldBg },
            BorderBrush = Border,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(5, 1, 5, 1),
            Margin = new Thickness(7, 0, 0, 0),
            Child = new TextBlock { Text = binding.Behavior.ToUpperInvariant(), FontSize = 9, Foreground = Muted }
        };
        DockPanel.SetDock(behavior, Dock.Right);
        panel.Children.Add(behavior);
        panel.Children.Add(new TextBlock { Text = Humanize(binding.ActionName), TextTrimming = TextTrimming.CharacterEllipsis });
        return panel;
    }

    private void ActionTreeSelectionChanged()
    {
        if (_actionTree.SelectedItem is not TreeViewItem item || item.Tag is not BindingEntry action) return;
        _selectedAction = action;
        ShowActionDetails(action);
        SuggestActionAcrossStates(action);
    }

    private void ShowActionDetails(BindingEntry action)
    {
        _aboutActionName.Text = $"{Humanize(action.ActionName)}  [{action.Behavior}]";
        _aboutActionBody.Text = action.Description;
        _similarActionsPanel.Children.Clear();
        var similar = _allBindings.Where(candidate => candidate.Identity != action.Identity)
            .Where(candidate => !string.IsNullOrWhiteSpace(action.Intent) && candidate.Intent.Equals(action.Intent, StringComparison.OrdinalIgnoreCase))
            .OrderBy(candidate => candidate.Context).ThenBy(candidate => candidate.Behavior).Take(8).ToList();
        if (similar.Count == 0)
        {
            _similarActionsPanel.Children.Add(new TextBlock { Text = "No known semantic peers. This action stays independent.", Foreground = Faint, TextWrapping = TextWrapping.Wrap });
            return;
        }
        foreach (var peer in similar)
        {
            var sameBehavior = peer.Behavior.Equals(action.Behavior, StringComparison.OrdinalIgnoreCase);
            _similarActionsPanel.Children.Add(new TextBlock { Text = $"{peer.Context}: {Humanize(peer.ActionName)} [{peer.Behavior}]", Foreground = sameBehavior ? Muted : Amber, Margin = new Thickness(0, 2, 0, 2), TextWrapping = TextWrapping.Wrap });
        }
    }

    private void SuggestActionAcrossStates(BindingEntry action)
    {
        if (_selectedControl is null)
        {
            SetStatus("Select a physical control first, then choose the action it should perform.");
            return;
        }
        if (_stateRows.TryGetValue(action.Context, out var ownRow))
        {
            ownRow.SetSelectedAction(action);
            ownRow.Include.IsChecked = true;
            ownRow.SetStatus("SELECTED", Blue);
        }
        if (!string.IsNullOrWhiteSpace(action.Intent))
        {
            foreach (var row in _stateRows.Values.Where(row => !row.Context.Equals(action.Context, StringComparison.OrdinalIgnoreCase)))
            {
                if (row.HasExistingAssignments) continue;
                var peer = _allBindings.FirstOrDefault(candidate => candidate.Context.Equals(row.Context, StringComparison.OrdinalIgnoreCase)
                    && candidate.Intent.Equals(action.Intent, StringComparison.OrdinalIgnoreCase)
                    && candidate.Behavior.Equals(action.Behavior, StringComparison.OrdinalIgnoreCase));
                if (peer is null) continue;
                row.SetSelectedAction(peer);
                row.Include.IsChecked = true;
                row.SetStatus("SUGGESTED", Green);
            }
        }
        RebuildStateOverview();
        RebuildWarnings();
        SetStatus($"Suggested compatible '{action.Intent}' actions. Uncheck any state that should not use {_selectedControl.Name}.");
    }

    private void RebuildStateAssignments()
    {
        _stateRows.Clear();
        _stateBindingPanel.Children.Clear();
        if (_activeProfile is null || _selectedControl is null)
        {
            _stateBindingPanel.Children.Add(new TextBlock { Text = _activeProfile is null ? "Open a profile to load its game-state actions." : "Select a physical control to see every state that uses it.", Foreground = Muted, TextWrapping = TextWrapping.Wrap });
            _selectedControlUsage.Text = "0";
            return;
        }
        var current = _allBindings.Where(binding => binding.Input.Equals(_selectedControl.Input, StringComparison.OrdinalIgnoreCase)).ToList();
        _selectedControlUsage.Text = current.Count.ToString();
        foreach (var context in ContextOrder.Where(context => _allBindings.Any(binding => binding.Context.Equals(context, StringComparison.OrdinalIgnoreCase))))
        {
            var choices = _allBindings.Where(binding => binding.Context.Equals(context, StringComparison.OrdinalIgnoreCase)).OrderBy(binding => Humanize(binding.ActionName)).ToList();
            var existing = current.Where(binding => binding.Context.Equals(context, StringComparison.OrdinalIgnoreCase)).ToList();
            var row = new StateBindingRow(context, choices, existing, Humanize, Text, Muted, FieldBg, Border, Green, Amber);
            row.Include.Checked += (_, _) => RebuildStateOverview();
            row.Include.Unchecked += (_, _) => RebuildStateOverview();
            row.ActionPicker.SelectionChanged += (_, _) =>
            {
                if (row.SelectedBinding is BindingEntry selected) ShowActionDetails(selected);
                RebuildWarnings();
                RebuildStateOverview();
            };
            _stateRows[context] = row;
            _stateBindingPanel.Children.Add(row.Root);
        }
    }

    private void RebuildWarnings()
    {
        var warnings = new List<WarningItem>();
        if (_activeProfile is null) warnings.Add(new WarningItem("!", "No profile loaded", Amber));
        else if (_selectedControl is null) warnings.Add(new WarningItem("i", "Select a physical control to inspect its assignments.", Cyan));
        else
        {
            var existing = _allBindings.Where(binding => binding.Input.Equals(_selectedControl.Input, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var group in existing.GroupBy(binding => binding.Context).Where(group => group.Count() > 1)) warnings.Add(new WarningItem("!", $"{group.Key}: {_selectedControl.Name} is bound to {group.Count()} actions.", Amber));
            var intentGroups = existing.Where(binding => !string.IsNullOrWhiteSpace(binding.Intent)).GroupBy(binding => binding.Intent).ToList();
            var unknown = existing.Where(binding => string.IsNullOrWhiteSpace(binding.Intent)).ToList();
            if (intentGroups.Count > 1 || (intentGroups.Count > 0 && unknown.Count > 0)) warnings.Add(new WarningItem("!", "This control performs unrelated intents across states. Review before saving.", Amber));
            foreach (var row in _stateRows.Values)
            {
                if (row.Include.IsChecked == true && row.ActionPicker.SelectedItem is null) warnings.Add(new WarningItem("!", $"{row.Context} is checked but has no action selected.", Red));
                if (row.SelectedBinding is BindingEntry selected && row.Include.IsChecked == true)
                {
                    var duplicates = _allBindings.Where(binding => binding.Identity != selected.Identity)
                        .Where(binding => binding.Context.Equals(row.Context, StringComparison.OrdinalIgnoreCase))
                        .Where(binding => binding.Input.Equals(_selectedControl.Input, StringComparison.OrdinalIgnoreCase)).ToList();
                    if (duplicates.Count > 0) warnings.Add(new WarningItem("!", $"{row.Context}: applying {Humanize(selected.ActionName)} will replace {duplicates.Count} existing assignment(s) on this control.", Amber));
                }
            }
            if (warnings.Count == 0) warnings.Add(new WarningItem("✓", "No critical conflicts for the selected control.", Green));
        }
        _warningList.ItemsSource = warnings;
        _warningList.ItemTemplate = WarningTemplate();
    }

    private void RebuildStateOverview()
    {
        _stateOverview.Children.Clear();
        if (_selectedControl is null)
        {
            _stateOverview.Children.Add(new TextBlock { Text = "Select a control", Foreground = Muted });
            return;
        }
        foreach (var row in _stateRows.Values)
        {
            var selected = row.SelectedBinding;
            var card = new Border { Width = 150, Height = 205, Margin = new Thickness(0, 0, 7, 0), Padding = new Thickness(10), Background = FieldBg, BorderBrush = row.Include.IsChecked == true ? Blue : Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5) };
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = ContextIcon(row.Context) + "  " + row.Context.ToUpperInvariant(), FontWeight = FontWeights.Bold, Foreground = row.Include.IsChecked == true ? Cyan : Muted });
            stack.Children.Add(new TextBlock { Text = row.Include.IsChecked == true && selected is not null ? Humanize(selected.ActionName) : "Not assigned", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 0), Foreground = Text, FontSize = 12 });
            stack.Children.Add(new TextBlock { Text = $"Type: {selected?.Behavior ?? "-"}", Foreground = Muted, Margin = new Thickness(0, 10, 0, 0) });
            stack.Children.Add(new TextBlock { Text = $"Input: {_selectedControl.Input}", Foreground = Muted, TextWrapping = TextWrapping.Wrap });
            card.Child = stack;
            _stateOverview.Children.Add(card);
        }
    }

    private void ApplyStateBindings(object sender, RoutedEventArgs e) => SaveStateBindings(false);
}
