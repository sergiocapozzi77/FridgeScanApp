namespace FridgeScan.Models;

public class MetadataChip
{
    public string Icon { get; set; }   // Material icon codepoint, e.g. "&#xe425;" for clock
    public string Value { get; set; }  // "45 min"
    public string Label { get; set; }  // "Total time"
}