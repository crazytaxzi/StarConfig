using System.IO;
using System.Globalization;
using System.Xml.Linq;

namespace StarConfig;

public sealed partial class StarbindProfileService
{
    private readonly StarbindActionKnowledge _knowledge = new();

    public StarbindProfile Load(string filePath, IReadOnlyList<InputDevice> detectedDevices)
    {
        var document = XDocument.Load(filePath, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        var profileElement = document.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals("ActionProfiles", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidDataException("This XML does not contain a Star Citizen ActionProfiles section.");

        var profileName = (string?)profileElement.Attribute("profileName") ?? Path.GetFileNameWithoutExtension(filePath);
        var devices = ParseDevices(profileElement, detectedDevices);
        var actions = ParseActions(profileElement);
        var axisOptions = ParseAxisOptions(profileElement, devices);

        return new StarbindProfile
        {
            FilePath = filePath,
            ProfileName = profileName,
            Channel = DetectChannel(filePath),
            Devices = devices,
            Actions = actions,
            AxisOptions = axisOptions
        };
    }

    public string SaveAssignments(StarbindProfile profile, IEnumerable<BindingMutation> requestedMutations, IEnumerable<StarbindDeviceAxisOption>? axisOptions = null)
    {
        var mutations = requestedMutations.ToList();
        var axisChanges = axisOptions?.ToList() ?? [];
        if (mutations.Count == 0 && axisChanges.Count == 0) throw new InvalidOperationException("There are no profile changes to save.");

        var backupFolder = Path.Combine(Path.GetDirectoryName(profile.FilePath)!, "Starbind Backups");
        Directory.CreateDirectory(backupFolder);
        var backupPath = Path.Combine(backupFolder, $"{Path.GetFileNameWithoutExtension(profile.FilePath)}-{DateTime.Now:yyyyMMdd-HHmmssfff}.xml");
        File.Copy(profile.FilePath, backupPath, false);

        var document = XDocument.Load(profile.FilePath, LoadOptions.PreserveWhitespace);
        var profileElement = document.Descendants().First(x => x.Name.LocalName.Equals("ActionProfiles", StringComparison.OrdinalIgnoreCase));

        var additions = mutations.Where(m => m.Kind == BindingMutationKind.AddInput).ToList();
        foreach (var removalGroup in mutations.Where(m => m.Kind == BindingMutationKind.RemoveInput).GroupBy(m => (m.Context, m.Input), new ContextInputComparer()))
        {
            foreach (var actionMap in profileElement.Elements().Where(IsActionMap))
            {
                var mapName = (string?)actionMap.Attribute("name") ?? string.Empty;
                var actions = actionMap.Elements().Where(IsAction).ToList();
                for (var actionIndex = 0; actionIndex < actions.Count; actionIndex++)
                {
                    var action = actions[actionIndex];
                    var actionName = (string?)action.Attribute("name") ?? string.Empty;
                    if (!ClassifyContext(mapName, actionName).Equals(removalGroup.Key.Context, StringComparison.OrdinalIgnoreCase)) continue;
                    var preserveExistingRebind = additions.Any(addition =>
                        addition.Context.Equals(removalGroup.Key.Context, StringComparison.OrdinalIgnoreCase)
                        && addition.Input.Equals(removalGroup.Key.Input, StringComparison.OrdinalIgnoreCase)
                        && addition.ActionMap.Equals(mapName, StringComparison.OrdinalIgnoreCase)
                        && addition.ActionOrdinal == actionIndex
                        && addition.ActionName.Equals(actionName, StringComparison.OrdinalIgnoreCase));
                    if (preserveExistingRebind) continue;
                    foreach (var rebind in action.Elements().Where(IsRebind).ToList())
                    {
                        if (StarbindInput.Normalize((string?)rebind.Attribute("input")).Equals(removalGroup.Key.Input, StringComparison.OrdinalIgnoreCase)) rebind.Remove();
                    }
                }
            }
        }

        foreach (var mutation in additions)
        {
            var actionMap = profileElement.Elements().Where(IsActionMap)
                .FirstOrDefault(x => string.Equals((string?)x.Attribute("name"), mutation.ActionMap, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Action map '{mutation.ActionMap}' was not found.");
            var actions = actionMap.Elements().Where(IsAction).ToList();
            XElement? action = mutation.ActionOrdinal >= 0 && mutation.ActionOrdinal < actions.Count ? actions[mutation.ActionOrdinal] : null;
            if (action is null || !string.Equals((string?)action.Attribute("name"), mutation.ActionName, StringComparison.OrdinalIgnoreCase))
                action = actions.FirstOrDefault(x => string.Equals((string?)x.Attribute("name"), mutation.ActionName, StringComparison.OrdinalIgnoreCase));
            if (action is null) throw new InvalidOperationException($"Action '{mutation.ActionName}' was not found in '{mutation.ActionMap}'.");
            if (action.Elements().Where(IsRebind).Any(x => StarbindInput.Normalize((string?)x.Attribute("input")).Equals(mutation.Input, StringComparison.OrdinalIgnoreCase))) continue;
            var placeholder = action.Elements().Where(IsRebind).FirstOrDefault(x => StarbindInput.IsUnbound((string?)x.Attribute("input")));
            if (placeholder is not null) placeholder.SetAttributeValue("input", mutation.Input);
            else action.Add(new XElement("rebind", new XAttribute("input", mutation.Input)));
        }

        foreach (var option in axisChanges)
        {
            var deviceOptions = profileElement.Elements().Where(x => x.Name.LocalName.Equals("deviceoptions", StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault(x => StarbindText.TrimProductGuid((string?)x.Attribute("name")).Equals(option.DeviceProduct, StringComparison.OrdinalIgnoreCase));
            if (deviceOptions is null)
            {
                deviceOptions = new XElement("deviceoptions", new XAttribute("name", option.DeviceProduct));
                profileElement.AddFirst(deviceOptions);
            }
            var axis = deviceOptions.Elements().FirstOrDefault(x => x.Name.LocalName.Equals("option", StringComparison.OrdinalIgnoreCase)
                && string.Equals((string?)x.Attribute("input"), option.Axis, StringComparison.OrdinalIgnoreCase));
            if (axis is null)
            {
                axis = new XElement("option", new XAttribute("input", option.Axis));
                deviceOptions.Add(axis);
            }
            axis.SetAttributeValue("deadzone", option.Deadzone.ToString("0.000000", CultureInfo.InvariantCulture));
        }

        var temp = profile.FilePath + ".starbind.tmp";
        document.Save(temp, SaveOptions.DisableFormatting);
        _ = XDocument.Load(temp);
        File.Move(temp, profile.FilePath, true);
        return backupPath;
    }

    public string CreateBackup(string filePath)
    {
        var backupFolder = Path.Combine(Path.GetDirectoryName(filePath)!, "Starbind Backups");
        Directory.CreateDirectory(backupFolder);
        var backupPath = Path.Combine(backupFolder, $"{Path.GetFileNameWithoutExtension(filePath)}-{DateTime.Now:yyyyMMdd-HHmmssfff}.xml");
        File.Copy(filePath, backupPath, false);
        return backupPath;
    }

    public IReadOnlyList<string> FindProfiles()
    {
        var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var drive in DriveInfo.GetDrives().Where(x => x.IsReady && x.DriveType == DriveType.Fixed))
        {
            foreach (var root in new[]
            {
                Path.Combine(drive.RootDirectory.FullName, "Program Files", "Roberts Space Industries", "StarCitizen"),
                Path.Combine(drive.RootDirectory.FullName, "Roberts Space Industries", "StarCitizen"),
                Path.Combine(drive.RootDirectory.FullName, "Games", "Roberts Space Industries", "StarCitizen")
            }.Where(Directory.Exists))
            {
                IEnumerable<string> channels;
                try { channels = Directory.EnumerateDirectories(root); }
                catch { continue; }
                foreach (var channel in channels)
                {
                    foreach (var mappingFolder in new[]
                    {
                        Path.Combine(channel, "USER", "Client", "0", "Controls", "Mappings"),
                        Path.Combine(channel, "USER", "Controls", "Mappings")
                    }.Where(Directory.Exists))
                    {
                        try { foreach (var file in Directory.EnumerateFiles(mappingFolder, "*.xml")) results.Add(file); }
                        catch { }
                    }
                }
            }
        }
        return results.OrderBy(x => x).ToList();
    }

}
