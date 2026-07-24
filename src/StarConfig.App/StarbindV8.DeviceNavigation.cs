using System.Windows;
using System.Windows.Controls;
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
        _deviceCards.LayoutUpdated += (_, _) =>
        {
            EnsureSystemDeviceCards();
            BringSelectedDeviceCardIntoView();
        };
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
        var scroll = VisualDescendants<ScrollViewer>(this)
            .FirstOrDefault(candidate => ReferenceEquals(candidate.Content, _deviceCards));
        if (scroll is null)
        {
            Dispatcher.BeginInvoke(card.BringIntoView, DispatcherPriority.ContextIdle);
            return;
        }

        var position = card.TranslatePoint(new Point(0, 0), _deviceCards).X;
        var maximumOffset = Math.Max(0, scroll.ExtentWidth - scroll.ViewportWidth);
        var target = Math.Clamp(position - 8, 0, maximumOffset);
        var cardRight = position + Math.Max(card.ActualWidth, card.Width);
        var visibleLeft = scroll.HorizontalOffset;
        var visibleRight = visibleLeft + scroll.ViewportWidth;
        var alreadyVisible = position >= visibleLeft - 1 && cardRight <= visibleRight + 1;

        if (alreadyVisible && deviceKey.Equals(_v8LastVisibleDeviceKey, StringComparison.OrdinalIgnoreCase)) return;
        if (alreadyVisible)
        {
            _v8LastVisibleDeviceKey = deviceKey;
            return;
        }

        _v8LastVisibleDeviceKey = null;
        Dispatcher.BeginInvoke(() =>
        {
            scroll.ScrollToHorizontalOffset(target);
            scroll.UpdateLayout();
            card.BringIntoView(new Rect(0, 0, card.ActualWidth, card.ActualHeight));
            if (Math.Abs(scroll.HorizontalOffset - target) <= 2) _v8LastVisibleDeviceKey = deviceKey;
        }, DispatcherPriority.ContextIdle);
    }
}
