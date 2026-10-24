namespace TomatoGrow.GameContent.Definitions;

public sealed class ProcessingRecipeDefinition
{
    public string Id { get; set; } = string.Empty;
    public string MachineId { get; set; } = string.Empty;
    public string InputItemId { get; set; } = string.Empty;
    public int InputQuantity { get; set; }
    public string OutputItemId { get; set; } = string.Empty;
    public int OutputQuantity { get; set; }
    public int ProcessingDays { get; set; }
}