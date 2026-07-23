using System.Xml.Linq;
using StarConfig.Models;

namespace StarConfig.Services;

public sealed class StarCitizenProfileService
{
    private readonly ActionKnowledgeService _knowledge = new();

    public string? FindDefaultMappingsFolder()
    {
        var roots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
        }.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var root in roots)
        foreach (var channel in new[] { "LIVE", "PTU", "EPTU", "TECH-PREVIEW" })
        {
            var candidate = Path.Combine(root, "Roberts Space Industries", "StarCitizen", channel, "USER", "Client", "0", "Controls", "Mappings");
            if (Directory.Exists(candidate)) return candidate;
        }
        return null;
    }

    public IReadOnlyList<string> GetProfiles(string folder) => Directory.Exists(folder)
        ? Directory.EnumerateFiles(folder, "*.xml").OrderBy(Path.GetFileName).ToList()
        : [];

    public IReadOnlyList<BindingEntry> LoadBindings(string file)
    {
        var document = XDocument.Load(file, LoadOptions.PreserveWhitespace);
        var results = new List<BindingEntry>();
        foreach (var map in document.Descendants().Where(IsActionMap))
        {
            var mapName = (string?)map.Attribute("name") ?? "Unknown";
            foreach (var action in map.Elements().Where(IsAction))
            {
                var actionName = (string?)action.Attribute("name") ?? "Unknown";
                var inputs = action.Descendants().Where(IsRebind).Select(x => (string?)x.Attribute("input")).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                var input = inputs.Count == 0 ? "Unbound" : string.Join(" | ", inputs);
                var context = GuessContext(mapName, actionName);
                results.Add(new BindingEntry
                {
                    ActionMap = mapName,
                    ActionName = actionName,
                    Input = input,
                    Context = context,
                    IntentKey = _knowledge.InferIntent(actionName),
                    Description = _knowledge.Describe(actionName, context)
                });
            }
        }
        return results.OrderBy(x => x.Context).ThenBy(x => x.ActionName).ToList();
    }

    public void SaveBindings(string file, IReadOnlyCollection<BindingUpdate> updates)
    {
        if (updates.Count == 0) return;
        File.Copy(file, $"{file}.{DateTime.Now:yyyyMMdd-HHmmss}.bak", false);
        var document = XDocument.Load(file, LoadOptions.PreserveWhitespace);
        foreach (var update in updates)
        {
            var map = document.Descendants().FirstOrDefault(x => IsActionMap(x) && string.Equals((string?)x.Attribute("name"), update.ActionMap, StringComparison.OrdinalIgnoreCase));
            var action = map?.Elements().FirstOrDefault(x => IsAction(x) && string.Equals((string?)x.Attribute("name"), update.ActionName, StringComparison.OrdinalIgnoreCase));
            if (action is null) throw new InvalidOperationException($"Action '{update.ActionMap}/{update.ActionName}' was not found.");
            foreach (var existing in action.Descendants().Where(IsRebind).ToList()) existing.Remove();
            if (!string.IsNullOrWhiteSpace(update.Input) && !update.Input.Equals("Unbound", StringComparison.OrdinalIgnoreCase))
                action.Add(new XElement("rebind", new XAttribute("input", update.Input)));
        }
        var temp = file + ".tmp";
        document.Save(temp, SaveOptions.DisableFormatting);
        File.Move(temp, file, true);
    }

    private static bool IsActionMap(XElement x) => x.Name.LocalName.Equals("actionmap", StringComparison.OrdinalIgnoreCase);
    private static bool IsAction(XElement x) => x.Name.LocalName.Equals("action", StringComparison.OrdinalIgnoreCase);
    private static bool IsRebind(XElement x) => x.Name.LocalName.Equals("rebind", StringComparison.OrdinalIgnoreCase);

    private static string GuessContext(string map, string action)
    {
        var text = (map + " " + action).ToLowerInvariant();
        if (text.Contains("eva")) return "EVA";
        if (text.Contains("turret")) return "Turret";
        if (text.Contains("mining")) return "Mining";
        if (text.Contains("salvage")) return "Salvage";
        if (text.Contains("vehicle") || text.Contains("ground")) return "Vehicle";
        if (text.Contains("spaceship") || text.Contains("flight") || text.Contains("ifcs") || text.StartsWith("v_")) return "Flight";
        if (text.Contains("player") || text.Contains("foot") || text.Contains("movement")) return "On Foot";
        return "General";
    }
}

public sealed record BindingUpdate(string ActionMap, string ActionName, string Input);
