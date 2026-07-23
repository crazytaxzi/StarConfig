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

public static class CockpitProgram
{
    [STAThread]
    public static void Main()
    {
        var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
        app.Run(new CockpitWindow());
    }
}

public sealed partial class CockpitWindow : Window
{
    private static readonly Brush WindowBg = Paint("#070B11");
    private static readonly Brush RailBg = Paint("#09111B");
    private static readonly Brush PanelBg = Paint("#101821");
    private static readonly Brush PanelAlt = Paint("#151E28");
    private static readonly Brush FieldBg = Paint("#0B121A");
    private static readonly Brush Border = Paint("#2B3949");
    private static readonly Brush BorderStrong = Paint("#3D5269");
    private static readonly Brush Blue = Paint("#3A8DFF");
    private static readonly Brush BlueDim = Paint("#173A68");
    private static readonly Brush Cyan = Paint("#55B7FF");
    private static readonly Brush Green = Paint("#49D66D");
    private static readonly Brush Amber = Paint("#FFB64A");
    private static readonly Brush Red = Paint("#FF6868");
    private static readonly Brush Text = Paint("#F1F5FA");
    private static readonly Brush Muted = Paint("#9EACBC");
    private static readonly Brush Faint = Paint("#657587");

    private static readonly string[] ContextOrder =
    [
        "Flight", "Vehicle", "On Foot", "EVA", "Turret", "Mining", "Salvage", "General"
    ];

    private readonly ProfileService _profiles = new();
    private readonly JoystickService _joysticks = new();
    private readonly ObservableCollection<BindingEntry> _allBindings = [];
    private readonly ObservableCollection<BindingEntry> _actionResults = [];
    private readonly HashSet<string> _mappingFolders = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, JoystickSnapshot> _captureBaselines = [];
    private readonly Dictionary<string, StateBindingRow> _stateRows = new(StringComparer.OrdinalIgnoreCase);
    private readonly DispatcherTimer _captureTimer;

    private readonly StackPanel _deviceStrip = new() { Orientation = Orientation.Horizontal };
    private readonly ComboBox _profilePicker = new();
    private readonly TextBlock _channelText = new();
    private readonly TextBlock _deviceCountText = new();
    private readonly TreeView _controlTree = new();
    private readonly Grid _deviceVisualHost = new();
    private readonly TextBlock _deviceHeading = new();
    private readonly TextBlock _deviceSubheading = new();
    private readonly TextBlock _selectedControlName = new();
    private readonly TextBlock _selectedControlInput = new();
    private readonly TextBlock _selectedControlType = new();
    private readonly TextBlock _selectedControlUsage = new();
    private readonly Border _responseTrack = new();
    private readonly Border _responseFill = new();
    private readonly StackPanel _stateBindingPanel = new();
    private readonly TextBlock _aboutActionName = new();
    private readonly TextBlock _aboutActionBody = new();
    private readonly StackPanel _similarActionsPanel = new();
    private readonly ListBox _warningList = new();
    private readonly TextBox _actionSearch = new();
    private readonly TreeView _actionTree = new();
    private readonly StackPanel _stateOverview = new() { Orientation = Orientation.Horizontal };
    private readonly TextBlock _statusText = new();
    private readonly TextBlock _saveStateText = new();
    private readonly Border _emptyProfileBanner = new();
    private readonly TextBlock _emptyProfileText = new();
    private readonly Button _listenButton = new();
    private readonly Button _applyButton = new();
    private readonly Button _validateButton = new();
    private readonly Button _backupButton = new();
    private readonly Button _saveLaunchButton = new();

    private readonly UserSettingsStore _settings = new();
    private List<InputDevice> _devices = [];
    private InputDevice? _selectedDevice;
    private PhysicalControl? _selectedControl;
    private BindingEntry? _selectedAction;
    private string? _activeProfile;
    private bool _capturing;
    private bool _loadingProfile;
    private DateTime? _lastSavedAt;
    private string _selectedDeviceCardId = "KEYBOARD";

    public CockpitWindow()
    {
        Title = "StarConfig - Star Citizen Control Mapper";
        Width = 1680;
        Height = 1000;
        MinWidth = 1280;
        MinHeight = 780;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = WindowBg;
        Foreground = Text;
        FontFamily = new FontFamily("Segoe UI");
        Content = BuildInterface();

        PreviewKeyDown += CaptureKeyboard;
        PreviewMouseDown += CaptureMouse;
        _captureTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(35) };
        _captureTimer.Tick += CaptureTimerTick;
        Loaded += (_, _) => InitializeApplication();
    }
}
