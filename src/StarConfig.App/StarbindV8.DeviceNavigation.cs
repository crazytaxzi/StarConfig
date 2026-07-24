using System.Windows.Controls;
using System.Windows.Threading;

namespace StarConfig;

public sealed partial class StarbindV5Window
{
    private bool _v8DeviceNavigationHooked;
    private Button? _v8LastVisibleDeviceCard;

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
        if (card is null || ReferenceEquals(card, _v8LastVisibleDeviceCard)) return;
        _v8LastVisibleDeviceCard = card;
        Dispatcher.BeginInvoke(card.BringIntoView, DispatcherPriority.Background);
    }
}
