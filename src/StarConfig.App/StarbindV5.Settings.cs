using System.Text.Json;
using System.Xml.Linq;

namespace StarConfig;

public sealed class StarbindV5SettingsStore
{
    private readonly string _folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Starbind");
    private string SettingsPath => Path.Combine(_folder, "settings-v5.json");

    public StarbindV5Settings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return new StarbindV5Settings();
            var settings = JsonSerializer.Deserialize<StarbindV5Settings>(File.ReadAllText(SettingsPath), JsonOptions()) ?? new StarbindV5Settings();
            settings.ProfileFolders ??= [];
            settings.DeviceRoleOverrides ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            return settings;
        }
        catch
        {
            return new StarbindV5Settings();
        }
    }

    public void Save(StarbindV5Settings settings)
    {
        Directory.CreateDirectory(_folder);
        settings.ProfileFolders = settings.ProfileFolders.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, JsonOptions()));
    }

    public IReadOnlyList<string> DiscoverProfiles(StarbindV5Settings settings)
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(settings.LastProfile) && File.Exists(settings.LastProfile)) candidates.Add(settings.LastProfile);

        foreach (var folder in settings.ProfileFolders.Where(Directory.Exists)) AddXmlFiles(folder, candidates);

        foreach (var drive in DriveInfo.GetDrives().Where(drive => drive.IsReady && drive.DriveType == DriveType.Fixed))
        {
            foreach (var starCitizenRoot in CandidateStarCitizenRoots(drive.RootDirectory.FullName).Where(Directory.Exists))
            {
                IEnumerable<string> channels;
                try { channels = Directory.EnumerateDirectories(starCitizenRoot); }
                catch { continue; }

                foreach (var channel in channels)
                {
                    foreach (var folder in new[]
                    {
                        Path.Combine(channel, "USER", "Client", "0", "Controls", "Mappings"),
                        Path.Combine(channel, "USER", "Controls", "Mappings")
                    }.Where(Directory.Exists))
                    {
                        settings.ProfileFolders.Add(folder);
                        AddXmlFiles(folder, candidates);
                    }
                }
            }
        }

        settings.ProfileFolders = settings.ProfileFolders.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var profiles = candidates.Where(IsUsableProfile)
            .OrderByDescending(File.GetLastWriteTime)
            .ThenBy(Path.GetFileName)
            .ToList();

        if (!string.IsNullOrWhiteSpace(settings.LastProfile) && !profiles.Contains(settings.LastProfile, StringComparer.OrdinalIgnoreCase))
            settings.LastProfile = null;
        return profiles;
    }

    public static bool IsUsableProfile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return false;
        try
        {
            var info = new FileInfo(filePath);
            if (info.Length < 80) return false;
            var document = XDocument.Load(filePath, LoadOptions.None);
            var hasProfile = document.Descendants().Any(element => element.Name.LocalName.Equals("ActionProfiles", StringComparison.OrdinalIgnoreCase));
            var hasActionMap = document.Descendants().Any(element => element.Name.LocalName.Equals("actionmap", StringComparison.OrdinalIgnoreCase));
            return hasProfile && hasActionMap;
        }
        catch
        {
            return false;
        }
    }

    public string? FindLauncher(StarbindV5Settings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.LauncherPath) && File.Exists(settings.LauncherPath)) return settings.LauncherPath;
        var candidates = new List<string>
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Roberts Space Industries", "RSI Launcher", "RSI Launcher.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "RSI Launcher", "RSI Launcher.exe")
        };
        foreach (var drive in DriveInfo.GetDrives().Where(drive => drive.IsReady && drive.DriveType == DriveType.Fixed))
        {
            candidates.Add(Path.Combine(drive.RootDirectory.FullName, "Program Files", "Roberts Space Industries", "RSI Launcher", "RSI Launcher.exe"));
            candidates.Add(Path.Combine(drive.RootDirectory.FullName, "Roberts Space Industries", "RSI Launcher", "RSI Launcher.exe"));
        }
        return candidates.FirstOrDefault(File.Exists);
    }

    public static string DetectChannel(string filePath)
    {
        foreach (var segment in filePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            if (segment.Equals("LIVE", StringComparison.OrdinalIgnoreCase)
                || segment.Equals("PTU", StringComparison.OrdinalIgnoreCase)
                || segment.Equals("EPTU", StringComparison.OrdinalIgnoreCase)
                || segment.Equals("TECH-PREVIEW", StringComparison.OrdinalIgnoreCase)) return segment.ToUpperInvariant();
        }
        return "CUSTOM";
    }

    private static void AddXmlFiles(string folder, HashSet<string> candidates)
    {
        try
        {
            foreach (var file in Directory.EnumerateFiles(folder, "*.xml", SearchOption.TopDirectoryOnly)) candidates.Add(file);
        }
        catch { }
    }

    private static IEnumerable<string> CandidateStarCitizenRoots(string driveRoot)
    {
        yield return Path.Combine(driveRoot, "Program Files", "Roberts Space Industries", "StarCitizen");
        yield return Path.Combine(driveRoot, "Roberts Space Industries", "StarCitizen");
        yield return Path.Combine(driveRoot, "Games", "Roberts Space Industries", "StarCitizen");
        yield return Path.Combine(driveRoot, "RSI", "StarCitizen");
    }

    private static JsonSerializerOptions JsonOptions() => new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };
}
