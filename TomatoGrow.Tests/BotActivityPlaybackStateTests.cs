using System;
using Xunit;

namespace TomatoGrow.Tests;

public sealed class BotActivityPlaybackStateTests
{
    [Fact]
    public void PlaybackState_StepsThroughReplayAndClearsWhenFinished()
    {
        BotActivityReplay replay = new(
        [
            new BotActivityStep(
                BotRole.Starter,
                0,
                0,
                new FarmPlotVisualSnapshot(string.Empty, 0, false),
                new FarmPlotVisualSnapshot("turnip", 0, false),
                "Planter Bot planting tile 0,0"),
            new BotActivityStep(
                BotRole.Maintainer,
                1,
                0,
                new FarmPlotVisualSnapshot("turnip", 0, false),
                new FarmPlotVisualSnapshot("turnip", 0, true),
                "Watering Bot watering tile 1,0")
        ]);

        BotActivityPlaybackState playback = new();

        playback.Start(replay, isFastMode: false);

        Assert.True(playback.IsPlayingBotActivity);
        Assert.Equal(BotRole.Starter, playback.ActiveBotRole);
        Assert.Equal(0, playback.ActivityStepIndex);
        Assert.Equal("Planter Bot planting tile 0,0", playback.ActiveStatusLine);
        Assert.True(playback.TryGetTileVisualOverride(1, 0, out FarmPlotVisualSnapshot initialOverride));
        Assert.Equal("turnip", initialOverride.CropDefinitionId);
        Assert.False(initialOverride.IsWatered);

        playback.Advance(TimeSpan.FromSeconds(0.12), preferFastMode: false);

        Assert.True(playback.IsPlayingBotActivity);
        Assert.Equal(BotRole.Maintainer, playback.ActiveBotRole);
        Assert.Equal(1, playback.ActivityStepIndex);
        Assert.False(playback.TryGetTileVisualOverride(1, 0, out _));

        playback.Advance(TimeSpan.FromSeconds(0.12), preferFastMode: false);

        Assert.False(playback.IsPlayingBotActivity);
        Assert.Equal(string.Empty, playback.ActiveStatusLine);
    }
}