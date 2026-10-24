using System;
using System.Collections.Generic;
using System.Linq;

namespace TomatoGrow;

public readonly record struct FarmPlotVisualSnapshot(string CropDefinitionId, int DaysGrown, bool IsWatered)
{
    public bool IsEmpty => string.IsNullOrWhiteSpace(CropDefinitionId);

    public static FarmPlotVisualSnapshot FromPlot(FarmPlot plot)
    {
        if (plot is null || plot.PlantedCrop is null)
            return new FarmPlotVisualSnapshot(string.Empty, 0, false);

        return new FarmPlotVisualSnapshot(plot.PlantedCrop.CropDefinitionId, plot.PlantedCrop.DaysGrown, plot.IsWatered);
    }

    public FarmPlot ToPlot(int x, int y)
    {
        FarmPlot plot = new(x, y);
        if (!IsEmpty)
            plot.Restore(CropDefinitionId, DaysGrown, IsWatered);

        return plot;
    }
}

public readonly record struct BotActivityStep(
    BotRole Role,
    int TileX,
    int TileY,
    FarmPlotVisualSnapshot Before,
    FarmPlotVisualSnapshot After,
    string StatusLine);

public sealed class BotActivityReplay
{
    public BotActivityReplay(IEnumerable<BotActivityStep> steps)
    {
        Steps = (steps ?? []).ToArray();
    }

    public IReadOnlyList<BotActivityStep> Steps { get; }
    public bool HasSteps => Steps.Count > 0;
}

public sealed class BotActivityPlaybackState
{
    private const double ManualStepDurationSeconds = 0.12;
    private const double AutoPlayStepDurationSeconds = 0.02;

    private BotActivityReplay _activeReplay;
    private double _stepTimerSeconds;
    private bool _isFastMode;

    public bool IsPlayingBotActivity => _activeReplay?.HasSteps == true;
    public int ActivityStepIndex { get; private set; }
    public double StepTimerSeconds => _stepTimerSeconds;

    public BotRole? ActiveBotRole => IsPlayingBotActivity
        ? _activeReplay.Steps[ActivityStepIndex].Role
        : null;

    public int ActiveTileX => IsPlayingBotActivity ? _activeReplay.Steps[ActivityStepIndex].TileX : -1;
    public int ActiveTileY => IsPlayingBotActivity ? _activeReplay.Steps[ActivityStepIndex].TileY : -1;
    public string ActiveStatusLine => IsPlayingBotActivity ? _activeReplay.Steps[ActivityStepIndex].StatusLine : string.Empty;

    public void Start(BotActivityReplay replay, bool isFastMode)
    {
        if (replay?.HasSteps != true)
        {
            Clear();
            return;
        }

        _activeReplay = replay;
        _isFastMode = isFastMode;
        ActivityStepIndex = 0;
        _stepTimerSeconds = 0;
    }

    public void Advance(TimeSpan elapsedTime, bool preferFastMode)
    {
        if (!IsPlayingBotActivity)
            return;

        _stepTimerSeconds += Math.Max(0, elapsedTime.TotalSeconds);
        double stepDurationSeconds = preferFastMode || _isFastMode
            ? AutoPlayStepDurationSeconds
            : ManualStepDurationSeconds;

        while (_stepTimerSeconds >= stepDurationSeconds)
        {
            _stepTimerSeconds -= stepDurationSeconds;

            if (ActivityStepIndex >= _activeReplay.Steps.Count - 1)
            {
                Clear();
                return;
            }

            ActivityStepIndex++;
        }
    }

    public bool TryGetTileVisualOverride(int tileX, int tileY, out FarmPlotVisualSnapshot snapshot)
    {
        if (!IsPlayingBotActivity)
        {
            snapshot = default;
            return false;
        }

        for (int index = ActivityStepIndex + 1; index < _activeReplay.Steps.Count; index++)
        {
            BotActivityStep step = _activeReplay.Steps[index];
            if (step.TileX == tileX && step.TileY == tileY)
            {
                snapshot = step.Before;
                return true;
            }
        }

        snapshot = default;
        return false;
    }

    public void Clear()
    {
        _activeReplay = null;
        ActivityStepIndex = 0;
        _stepTimerSeconds = 0;
        _isFastMode = false;
    }
}