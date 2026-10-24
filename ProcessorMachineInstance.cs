using System;

namespace TomatoGrow;

public sealed class ProcessorMachineInstance
{
    public ProcessorMachineInstance(string machineId)
    {
        MachineId = machineId;
    }

    public string MachineId { get; }
    public string CurrentRecipeId { get; private set; }
    public string InputItemId { get; private set; }
    public int InputQuantity { get; private set; }
    public string OutputItemId { get; private set; }
    public int OutputQuantity { get; private set; }
    public int DaysRemaining { get; private set; }
    public bool HasOutputReady { get; private set; }
    public bool IsWorking => !string.IsNullOrWhiteSpace(CurrentRecipeId) && !HasOutputReady && DaysRemaining > 0;

    public void Reset()
    {
        CurrentRecipeId = null;
        InputItemId = null;
        InputQuantity = 0;
        OutputItemId = null;
        OutputQuantity = 0;
        DaysRemaining = 0;
        HasOutputReady = false;
    }

    public void Start(string recipeId, string inputItemId, int inputQuantity, string outputItemId, int outputQuantity, int processingDays)
    {
        CurrentRecipeId = recipeId;
        InputItemId = inputItemId;
        InputQuantity = inputQuantity;
        OutputItemId = outputItemId;
        OutputQuantity = outputQuantity;
        DaysRemaining = processingDays;
        HasOutputReady = false;
    }

    public void AdvanceDay()
    {
        if (!IsWorking)
            return;

        DaysRemaining--;
        if (DaysRemaining <= 0)
        {
            DaysRemaining = 0;
            HasOutputReady = true;
        }
    }

    public void Restore(
        string currentRecipeId,
        string inputItemId,
        int inputQuantity,
        string outputItemId,
        int outputQuantity,
        int daysRemaining,
        bool hasOutputReady)
    {
        if (string.IsNullOrWhiteSpace(currentRecipeId))
        {
            Reset();
            return;
        }

        CurrentRecipeId = currentRecipeId;
        InputItemId = inputItemId;
        InputQuantity = inputQuantity < 0 ? 0 : inputQuantity;
        OutputItemId = outputItemId;
        OutputQuantity = outputQuantity < 0 ? 0 : outputQuantity;
        DaysRemaining = hasOutputReady ? 0 : Math.Max(0, daysRemaining);
        HasOutputReady = hasOutputReady;
    }
}