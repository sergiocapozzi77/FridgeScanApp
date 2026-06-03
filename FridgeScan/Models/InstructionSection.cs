using System.Text.Json.Serialization;

namespace FridgeScan.Models;

public class InstructionSection
{
    public string? Name { get; set; }
    public List<string> Steps { get; set; } = new();
}
