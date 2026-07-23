namespace StarConfig.Models;

public sealed class BindingEntry
{
    public string ActionMap { get; init; } = "Unknown";
    public string ActionName { get; init; } = "Unknown";
    public string Input { get; set; } = "Unbound";
    public string Description { get; init; } = "No description is available yet.";
    public string Context { get; init; } = "General";
    public string? IntentKey { get; init; }

    public string Identity => $"{ActionMap}/{ActionName}";
    public string Display => $"[{Context}] {ActionName}  ->  {Input}";
}
