using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StarConfig;

public sealed partial class StarbindV5Window
{
    private bool _v8SystemDeviceCardsRefreshing;

    private void EnsureSystemDeviceCards()
    {
        if (_v8SystemDeviceCardsRefreshing) return;
        var systemDevices = CurrentDevices()
            .Where(device => device.Kind is StarbindDeviceKind.Keyboard or StarbindDeviceKind.Mouse)
            .OrderBy(device => device.Kind)
            .ToList();
        if (systemDevices.Count == 0) return;

        var existingKinds = _deviceCards.Children.OfType<Button>()
            .Select(button => button.Tag)
            .OfType<StarbindDevice>()
            .Select(device => device.Kind)
            .ToHashSet();
        if (systemDevices.All(device => existingKinds.Contains(device.Kind))) return;

        _v8SystemDeviceCardsRefreshing = true;
        try
        {
            var insertionIndex = 0;
            foreach (var device in systemDevices)
            {
                if (existingKinds.Contains(device.Kind)) continue;
                _deviceCards.Children.Insert(insertionIndex++, BuildSystemDeviceCard(device));
            }
        }
        finally
        {
            _v8SystemDeviceCardsRefreshing = false;
        }
    }

    private Button BuildSystemDeviceCard(StarbindDevice device)
    {
        var template = _hardware.Resolve(device, _settings);
        var selected = _selectedDevice is not null && SameDevice(_selectedDevice, device);
        var card = new Button
        {
            Width = 174,
            Height = 64,
            Margin = new Thickness(0, 0, 8, 0),
            Padding = new Thickness(8),
            Background = selected ? BlueDim : Panel2,
            BorderBrush = selected ? Cyan : Border,
            BorderThickness = new Thickness(selected ? 2 : 1),
            Foreground = Text,
            Cursor = Cursors.Hand,
            Tag = device,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            ToolTip = device.Kind == StarbindDeviceKind.Keyboard
                ? "Open the complete interactive keyboard and assign actions to individual keys."
                : "Open mouse axes, wheel and button controls."
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        var art = StarbindDeviceArtworkV8.Build(template, device, 150, 150, Field, Border2, Cyan, Muted);
        grid.Children.Add(new Viewbox { Width = 44, Height = 44, Stretch = System.Windows.Media.Stretch.Uniform, Child = art });
        var words = new StackPanel { Margin = new Thickness(7, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        words.Children.Add(new TextBlock { Text = device.ProductName, FontSize = 10.5, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
        words.Children.Add(new TextBlock
        {
            Text = device.Kind == StarbindDeviceKind.Keyboard ? "KEYBOARD  •  LIVE" : "MOUSE  •  LIVE",
            Foreground = Green,
            FontSize = 9,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        Grid.SetColumn(words, 1);
        grid.Children.Add(words);
        card.Content = grid;
        card.Click += (_, _) => SelectDevice(device);
        return card;
    }
}
