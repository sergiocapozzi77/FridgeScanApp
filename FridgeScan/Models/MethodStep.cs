namespace FridgeScan.Models;

public class MethodStep
{
    public int Number { get; set; }
    public string Text { get; set; } = string.Empty;
    
    public string? StepDuration { get; set; }  // e.g. "5 min"
    public bool HasDuration => !string.IsNullOrEmpty(StepDuration);
}
