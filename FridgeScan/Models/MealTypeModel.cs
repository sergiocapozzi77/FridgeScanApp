namespace FridgeScan.Models;

public class MealTypeModel
{
    public string Label { get; set; } // Quello che appare nel chip (es. "Main Course")
    public string Value { get; set; } // Quello che passi alla ricerca (es. "main-course")
}
