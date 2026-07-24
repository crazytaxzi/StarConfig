using System.Globalization;
using System.Xml.Linq;

namespace StarConfig;

public sealed partial class StarbindProfileService
{
    public string SaveWorkspace(
        StarbindProfile profile,
        IEnumerable<ControlBindingPlan> requestedPlans,
        IEnumerable<AxisTuningChange> requestedTunings)
    {
        var plans = requestedPlans.Where(plan => plan.IsDirty).ToList();
        var tunings = requestedTunings.ToList();
        if (plans.Count == 0 && tunings.Count == 0) throw new InvalidOperationException("There are no pending Starbind changes to save.");

        var backupPath = CreateBackup(profile.FilePath);
        var document = XDocument.Load(profile.FilePath, LoadOptions.PreserveWhitespace);
        var profileElement = document.Descendants().FirstOrDefault(element => element.Name.LocalName.Equals("ActionProfiles", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidDataException("This XML does not contain a Star Citizen ActionProfiles section.");

        foreach (var plan in plans)
        {
            foreach (var state in plan.States.Values)
            {
                ApplyStatePlan(profileElement, plan.Input, state);
            }
        }

        foreach (var tuning in tunings)
        {
            ApplyAxisTuning(profileElement, tuning);
        }

        var tempPath = profile.FilePath + ".starbind-v5.tmp";
        document.Save(tempPath, SaveOptions.DisableFormatting);
        _ = XDocument.Load(tempPath, LoadOptions.PreserveWhitespace);
        File.Move(tempPath, profile.FilePath, true);
        return backupPath;
    }

    public string SaveConflictResolutions(StarbindProfile profile, IEnumerable<ConflictResolution> resolutions)
    {
        var groups = resolutions.ToList();
        if (groups.Count == 0) throw new InvalidOperationException("No conflict resolutions were selected.");
        var backupPath = CreateBackup(profile.FilePath);
        var document = XDocument.Load(profile.FilePath, LoadOptions.PreserveWhitespace);
        var profileElement = document.Descendants().FirstOrDefault(element => element.Name.LocalName.Equals("ActionProfiles", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidDataException("This XML does not contain a Star Citizen ActionProfiles section.");

        foreach (var resolution in groups)
        {
            foreach (var actionMap in profileElement.Elements().Where(element => element.Name.LocalName.Equals("actionmap", StringComparison.OrdinalIgnoreCase)))
            {
                var mapName = (string?)actionMap.Attribute("name") ?? string.Empty;
                var actions = actionMap.Elements().Where(element => element.Name.LocalName.Equals("action", StringComparison.OrdinalIgnoreCase)).ToList();
                for (var index = 0; index < actions.Count; index++)
                {
                    var action = actions[index];
                    var actionName = (string?)action.Attribute("name") ?? string.Empty;
                    if (!ClassifyContext(mapName, actionName).Equals(resolution.Context, StringComparison.OrdinalIgnoreCase)) continue;
                    var keep = index == resolution.KeepAction.ActionOrdinal
                        && mapName.Equals(resolution.KeepAction.ActionMap, StringComparison.OrdinalIgnoreCase)
                        && actionName.Equals(resolution.KeepAction.ActionName, StringComparison.OrdinalIgnoreCase);
                    if (keep) continue;
                    foreach (var rebind in action.Elements().Where(element => element.Name.LocalName.Equals("rebind", StringComparison.OrdinalIgnoreCase)).ToList())
                    {
                        if (StarbindInput.Normalize((string?)rebind.Attribute("input")).Equals(resolution.Input, StringComparison.OrdinalIgnoreCase)) rebind.Remove();
                    }
                }
            }
        }

        var tempPath = profile.FilePath + ".starbind-conflicts.tmp";
        document.Save(tempPath, SaveOptions.DisableFormatting);
        _ = XDocument.Load(tempPath, LoadOptions.PreserveWhitespace);
        File.Move(tempPath, profile.FilePath, true);
        return backupPath;
    }

    private static void ApplyStatePlan(XElement profileElement, string physicalInput, PlannedStateBinding state)
    {
        XElement? selectedActionElement = null;
        XElement? existingSelectedRebind = null;

        foreach (var actionMap in profileElement.Elements().Where(element => element.Name.LocalName.Equals("actionmap", StringComparison.OrdinalIgnoreCase)))
        {
            var mapName = (string?)actionMap.Attribute("name") ?? string.Empty;
            var actions = actionMap.Elements().Where(element => element.Name.LocalName.Equals("action", StringComparison.OrdinalIgnoreCase)).ToList();
            for (var index = 0; index < actions.Count; index++)
            {
                var action = actions[index];
                var actionName = (string?)action.Attribute("name") ?? string.Empty;
                if (!ClassifyContext(mapName, actionName).Equals(state.Context, StringComparison.OrdinalIgnoreCase)) continue;

                var isSelected = state.Enabled && state.Action is not null
                    && state.Action.ActionOrdinal == index
                    && state.Action.ActionMap.Equals(mapName, StringComparison.OrdinalIgnoreCase)
                    && state.Action.ActionName.Equals(actionName, StringComparison.OrdinalIgnoreCase);
                if (isSelected) selectedActionElement = action;

                foreach (var rebind in action.Elements().Where(element => element.Name.LocalName.Equals("rebind", StringComparison.OrdinalIgnoreCase)).ToList())
                {
                    if (!StarbindInput.Normalize((string?)rebind.Attribute("input")).Equals(physicalInput, StringComparison.OrdinalIgnoreCase)) continue;
                    if (isSelected && existingSelectedRebind is null)
                    {
                        existingSelectedRebind = rebind;
                        continue;
                    }
                    rebind.Remove();
                }
            }
        }

        if (!state.Enabled || state.Action is null) return;
        if (selectedActionElement is null)
        {
            var actionMap = profileElement.Elements().FirstOrDefault(element => element.Name.LocalName.Equals("actionmap", StringComparison.OrdinalIgnoreCase)
                && string.Equals((string?)element.Attribute("name"), state.Action.ActionMap, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Action map '{state.Action.ActionMap}' was not found.");
            var actions = actionMap.Elements().Where(element => element.Name.LocalName.Equals("action", StringComparison.OrdinalIgnoreCase)).ToList();
            selectedActionElement = state.Action.ActionOrdinal >= 0 && state.Action.ActionOrdinal < actions.Count ? actions[state.Action.ActionOrdinal] : null;
            if (selectedActionElement is null || !string.Equals((string?)selectedActionElement.Attribute("name"), state.Action.ActionName, StringComparison.OrdinalIgnoreCase))
                selectedActionElement = actions.FirstOrDefault(element => string.Equals((string?)element.Attribute("name"), state.Action.ActionName, StringComparison.OrdinalIgnoreCase));
        }
        if (selectedActionElement is null) throw new InvalidOperationException($"Action '{state.Action.ActionName}' was not found in '{state.Action.ActionMap}'.");
        if (existingSelectedRebind is not null) return;

        var placeholder = selectedActionElement.Elements().FirstOrDefault(element => element.Name.LocalName.Equals("rebind", StringComparison.OrdinalIgnoreCase)
            && StarbindInput.IsUnbound((string?)element.Attribute("input")));
        if (placeholder is not null)
        {
            placeholder.SetAttributeValue("input", physicalInput);
            return;
        }
        selectedActionElement.Add(new XElement("rebind", new XAttribute("input", physicalInput)));
    }

    private static void ApplyAxisTuning(XElement profileElement, AxisTuningChange tuning)
    {
        var deviceOptions = profileElement.Elements().FirstOrDefault(element => element.Name.LocalName.Equals("deviceoptions", StringComparison.OrdinalIgnoreCase)
            && StarbindText.TrimProductGuid((string?)element.Attribute("name")).Equals(tuning.DeviceProduct, StringComparison.OrdinalIgnoreCase));
        if (deviceOptions is null)
        {
            deviceOptions = new XElement("deviceoptions", new XAttribute("name", tuning.DeviceProduct));
            profileElement.AddFirst(deviceOptions);
        }

        var option = deviceOptions.Elements().FirstOrDefault(element => element.Name.LocalName.Equals("option", StringComparison.OrdinalIgnoreCase)
            && string.Equals((string?)element.Attribute("input"), tuning.Axis, StringComparison.OrdinalIgnoreCase));
        if (option is null)
        {
            option = new XElement("option", new XAttribute("input", tuning.Axis));
            deviceOptions.Add(option);
        }
        option.SetAttributeValue("deadzone", Math.Clamp(tuning.Deadzone, 0, 0.95).ToString("0.000000", CultureInfo.InvariantCulture));
        option.SetAttributeValue("exponent", Math.Clamp(tuning.Exponent, 0.1, 4.0).ToString("0.000000", CultureInfo.InvariantCulture));
    }
}
