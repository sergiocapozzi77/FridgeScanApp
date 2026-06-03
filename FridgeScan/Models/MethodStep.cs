namespace FridgeScan.Models;

public class MethodStep
{
    public int Number { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? StepDuration { get; set; }
    public bool HasDuration => !string.IsNullOrEmpty(StepDuration);
    public bool IsSectionHeader { get; set; }
    public bool IsStep => !IsSectionHeader;
}
