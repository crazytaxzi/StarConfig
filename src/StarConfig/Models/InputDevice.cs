namespace StarConfig.Models;

public sealed record InputDevice(int Id, string Name, int ButtonCount, int AxisCount)
{
    public string Summary => $"{Name}  |  {ButtonCount} buttons, {AxisCount} axes";
}
