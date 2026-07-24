using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace StarConfig;

public sealed partial class StarbindV5Window
{
    private bool _v8DeviceNavigationHooked;
    private string? _v8LastVisibleDeviceKey;

    private void InstallDeviceNavigationFix()
    {
        if (_v8DeviceNavigationHooked) return;
        _v8DeviceNavigationHooked = true;
        _deviceCards.LayoutUpdated += (_, _) => BringSelectedDeviceCardIntoView();
        _deviceCanvasHost.LayoutUpdated += (_, _) => RefreshUnplacedControlTray();
    }

    private void BringSelectedDeviceCardIntoView()
    {
        if (_selectedDevice is null) return;
        var card = _deviceCards.Children.OfType<Button>().FirstOrDefault(button =>
            button.Tag is StarbindDevice device
            && device.Kind == _selectedDevice.Kind
            && device.Instance == _selectedDevice.Instance
            && device.ProductName.Equals(_selectedDevice.ProductName, StringComparison.OrdinalIgnoreCase));
        if (card is null) return;

        var deviceKey = $"{_selectedDevice.Kind}|{_selectedDevice.Instance}|{_selectedDevice.ProductName}";
        if (deviceKey.Equals(_v8LastVisibleDeviceKey, StringComparison.OrdinalIgnoreCase)) return;
        _v8LastVisibleDeviceKey = deviceKey;

        Dispatcher.BeginInvoke(() =>
        {
            var scroll = FindVisualAncestor<ScrollViewer>(_deviceCards);
            if (scroll is null)
            {
                card.BringIntoView();
                return;
            }
            var position = card.TranslatePoint(new Point(0, 0), _deviceCards).X;
            var target = Math.Max(0, position - 8);
            scroll.ScrollToHorizontalOffset(target);
            card.BringIntoView(new Rect(0, 0, card.ActualWidth, card.ActualHeight));
        }, DispatcherPriority.Background);
    }

    private static T? FindVisualAncestor<T>(DependencyObject child) where T : DependencyObject
    {
        var current = child;
        while (current is not null)
        {
            if (current is T match) return match;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}
