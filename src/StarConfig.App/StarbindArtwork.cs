using System.Windows.Media.Imaging;

namespace StarConfig;

internal static class StarbindArtwork
{
    public static BitmapImage LoadJoystick()
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri("pack://application:,,,/Assets/joystick.jpg", UriKind.Absolute);
        image.EndInit();
        image.Freeze();
        return image;
    }
}
