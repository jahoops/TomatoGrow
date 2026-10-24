namespace TomatoGrow;

public sealed class PlantedCrop
{
    public PlantedCrop(string cropDefinitionId, int daysGrown = 0)
    {
        CropDefinitionId = cropDefinitionId;
        DaysGrown = daysGrown < 0 ? 0 : daysGrown;
    }

    public string CropDefinitionId { get; }
    public int DaysGrown { get; private set; }

    public void AdvanceDay()
    {
        DaysGrown++;
    }
}