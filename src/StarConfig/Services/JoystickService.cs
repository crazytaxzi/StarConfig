using System.Runtime.InteropServices;
using StarConfig.Models;

namespace StarConfig.Services;

public sealed class JoystickService
{
    private const uint JoyNoError = 0;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct JoyCaps
    {
        public ushort ManufacturerId;
        public ushort ProductId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string ProductName;
        public uint XMin, XMax, YMin, YMax, ZMin, ZMax;
        public uint NumberOfButtons, PeriodMin, PeriodMax;
        public uint RMin, RMax, UMin, UMax, VMin, VMax;
        public uint Capabilities, MaxAxes, NumberOfAxes, MaxButtons;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string RegistryKey;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string OemVxd;
    }

    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    private static extern uint joyGetDevCaps(uint id, ref JoyCaps caps, uint size);

    [DllImport("winmm.dll")]
    private static extern uint joyGetNumDevs();

    public IReadOnlyList<InputDevice> GetConnectedDevices()
    {
        var devices = new List<InputDevice>
        {
            new(0, "Keyboard", 104, 0),
            new(-1, "Mouse", 5, 2)
        };

        var count = joyGetNumDevs();
        for (uint id = 0; id < count; id++)
        {
            var caps = new JoyCaps();
            if (joyGetDevCaps(id, ref caps, (uint)Marshal.SizeOf<JoyCaps>()) != JoyNoError) continue;
            var name = string.IsNullOrWhiteSpace(caps.ProductName) ? $"Joystick {id + 1}" : caps.ProductName.Trim();
            devices.Add(new InputDevice((int)id + 1, name, (int)caps.NumberOfButtons, (int)caps.NumberOfAxes));
        }

        return devices;
    }
}
