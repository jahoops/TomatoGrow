using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TomatoGrow.GameContent;
using TomatoGrow.GameContent.Definitions;

namespace TomatoGrow;

public partial class Game1
{
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(76, 118, 78));

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        DrawPanels();

        if (_debugUiState.IsLoadConfirmationPending)
            DrawLoadConfirmationOverlay();

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private bool IsSelectedPlotReady()
    {
        return _turnipTestState.IsPlantedCropHarvestable();
    }

    private string GetItemName(string itemId)
    {
        return _contentRegistry.GetItem(itemId).Name.ToUpperInvariant();
    }

    private int GetItemCount(string itemId)
    {
        return _turnipTestState.GetItemCount(itemId);
    }

    private int GetStorageItemCount(string itemId)
    {
        return _turnipTestState.GetStorageItemCount(itemId);
    }

    private void DrawPanels()
    {
        switch (_debugUiState.CurrentScreen)
        {
            case GameScreen.Farm:
                DrawFarmScreen();
                break;
            case GameScreen.Inventory:
                DrawInventoryScreen();
                break;
            case GameScreen.Storage:
                DrawStorageScreen();
                break;
            case GameScreen.Market:
                DrawMarketScreen();
                break;
            case GameScreen.Computer:
                DrawComputerScreen();
                break;
            case GameScreen.ProgressionReport:
                DrawProgressionReportScreen();
                break;
            case GameScreen.ProductionReport:
                DrawProductionReportScreen();
                break;
            case GameScreen.ActivityLog:
                DrawActivityLogScreen();
                break;
            case GameScreen.Machines:
                DrawMachinesScreen();
                break;
            case GameScreen.Stats:
                DrawStatsScreen();
                break;
            case GameScreen.Help:
                DrawHelpScreen();
                break;
        }
    }

    private void DrawFarmScreen()
    {
        DrawPanel(GetFarmPanelBounds(), DebugUiTextFormatter.GetScreenTitle(GameScreen.Farm, _turnipTestState.ThemeVocabulary));
        DrawFarmGrid(FarmGridX, FarmGridY, GetPlotWidth(), GetPlotHeight(), GetPlotGap());
        DrawDashboardPanel();
        DrawSelectedPlotPanel();
        DrawBotsPanel();
        DrawFarmMachinePanel();
        DrawFarmControlsPanel();
        DrawLastActionPanel();
    }

    private void DrawInventoryScreen()
    {
        DrawFullScreenTextPanel(DebugUiTextFormatter.GetScreenTitle(GameScreen.Inventory, _turnipTestState.ThemeVocabulary), GetInventoryScreenLines());
    }

    private void DrawMachinesScreen()
    {
        DrawFullScreenTextPanel(DebugUiTextFormatter.GetScreenTitle(GameScreen.Machines, _turnipTestState.ThemeVocabulary), GetMachineScreenLines());
    }

    private void DrawStorageScreen()
    {
        DrawFullScreenTextPanel(DebugUiTextFormatter.GetScreenTitle(GameScreen.Storage, _turnipTestState.ThemeVocabulary), GetStorageScreenLines());
    }

    private void DrawMarketScreen()
    {
        DrawFullScreenTextPanel(DebugUiTextFormatter.GetScreenTitle(GameScreen.Market, _turnipTestState.ThemeVocabulary), GetMarketScreenLines());
    }

    private void DrawComputerScreen()
    {
        DrawFullScreenTextPanel(DebugUiTextFormatter.GetScreenTitle(GameScreen.Computer, _turnipTestState.ThemeVocabulary), GetComputerScreenLines());
    }

    private void DrawActivityLogScreen()
    {
        DrawFullScreenTextPanel(DebugUiTextFormatter.GetScreenTitle(GameScreen.ActivityLog, _turnipTestState.ThemeVocabulary), GetActivityLogScreenLines());
    }

    private void DrawProgressionReportScreen()
    {
        DrawFullScreenTextPanel(DebugUiTextFormatter.GetScreenTitle(GameScreen.ProgressionReport, _turnipTestState.ThemeVocabulary), GetProgressionReportScreenLines());
    }

    private void DrawProductionReportScreen()
    {
        DrawFullScreenTextPanel(DebugUiTextFormatter.GetScreenTitle(GameScreen.ProductionReport, _turnipTestState.ThemeVocabulary), GetProductionReportScreenLines());
    }

    private void DrawHelpScreen()
    {
        DrawFullScreenTextPanel(DebugUiTextFormatter.GetScreenTitle(GameScreen.Help, _turnipTestState.ThemeVocabulary), GetHelpScreenLines());
    }

    private void DrawDashboardPanel()
    {
        Rectangle panelBounds = GetDashboardPanelBounds();
        DebugUiTextFormatter.DashboardSummary summary = DebugUiTextFormatter.GetDashboardSummary(
            _turnipTestState.DayNumber,
            _turnipTestState.Money,
            _turnipTestState.FarmGrid.Width,
            _turnipTestState.FarmGrid.Height,
            _contentRegistry,
            _turnipTestState.SelectedSeedItemId,
            GetItemCount,
            _turnipTestState.GetEffectiveSellPrice,
            _turnipTestState.ActiveMarketOpportunity,
            _turnipTestState.GetDaysUntilNextMarketOpportunity());

        DrawPanel(panelBounds, _debugUiState.ShowDebugDetails ? "DASHBOARD DEBUG" : "DASHBOARD PLAY");
        DrawStatBox(panelBounds.X + 12, panelBounds.Y + 28, 88, 34, "DAY", summary.DayNumber.ToString());
        DrawStatBox(panelBounds.X + 106, panelBounds.Y + 28, 136, 34, "MONEY", summary.Money.ToString());
        DrawStatBox(panelBounds.X + 248, panelBounds.Y + 28, 116, 34, _turnipTestState.ThemeVocabulary.ToUpperLabel(_turnipTestState.ThemeVocabulary.WorldName), summary.FarmSize);
        DrawStatBox(panelBounds.X + 370, panelBounds.Y + 28, 156, 34, _turnipTestState.ThemeVocabulary.ToUpperLabel(_turnipTestState.ThemeVocabulary.StarterItemName), summary.SelectedSeedName);
        DrawStatBox(panelBounds.X + 12, panelBounds.Y + 68, 118, 34, $"{_turnipTestState.ThemeVocabulary.ToUpperLabel(_turnipTestState.ThemeVocabulary.StarterItemName)} CNT", summary.SelectedSeedCount.ToString());
        DrawStatBox(panelBounds.X + 136, panelBounds.Y + 68, 164, 34, "SELLABLE", summary.SellableCropCount.ToString());
        DrawStatBox(panelBounds.X + 306, panelBounds.Y + 68, 220, 34, "SELL VALUE", summary.EstimatedSellValue.ToString());
        List<string> dashboardLines = [];
        if (!string.IsNullOrWhiteSpace(summary.MarketStatusLine))
        {
            dashboardLines.Add(summary.MarketStatusLine);
            dashboardLines.Add(string.Empty);
        }

        dashboardLines.AddRange(GetAutoPlayStatusLines());
        DrawPanelLinesInRegion(GetDashboardAutoPlayBounds(panelBounds), 2, LineHeight, dashboardLines, TextColor);
    }

    private void DrawSelectedPlotPanel()
    {
        Rectangle panelBounds = GetSelectedPlotPanelBounds();
        DrawPanel(panelBounds, _turnipTestState.ThemeVocabulary.ToUpperLabel(_turnipTestState.ThemeVocabulary.GetTileSelectionLabel()));

        FarmPlot selectedPlot = _turnipTestState.GetSelectedPlot();
        string[] lines = DebugUiTextFormatter.GetSelectedTileDetailLines(
            selectedPlot,
            _contentRegistry,
            _turnipTestState.IsPlotHarvestable(selectedPlot.X, selectedPlot.Y),
            _turnipTestState.ThemeVocabulary);

        if (_debugUiState.ShowDebugDetails)
        {
            lines = DebugUiTextFormatter.GetSelectedPlotSummaryLines(
                selectedPlot,
                _contentRegistry,
                _turnipTestState.IsPlotHarvestable(selectedPlot.X, selectedPlot.Y),
                includeDebugDetails: true,
                themeVocabulary: _turnipTestState.ThemeVocabulary);
        }

        if (_debugUiState.IsPlayingBotActivity)
            lines = [_debugUiState.ActiveBotActivityStatusLine, .. lines];

        DrawPanelLines(panelBounds, 2, LineHeight, lines, TextColor);
    }

    private void DrawBotsPanel()
    {
        Rectangle panelBounds = GetBotPanelBounds();
        DrawPanel(panelBounds, _debugUiState.ShowDebugDetails ? "BOTS DEBUG" : "BOTS");

        Rectangle contentBounds = GetPanelContentBounds(panelBounds);
        List<(string Text, Color Color)> lines = GetBotLines();
        int maxLines = Math.Max(1, contentBounds.Height / LineHeight);

        for (int index = 0; index < Math.Min(lines.Count, maxLines); index++)
        {
            (string text, Color color) = lines[index];
            DrawText(contentBounds.X, contentBounds.Y + (index * LineHeight), 2, ShortenText(text, GetMaxCharacters(contentBounds.Width, 2)), color);
        }
    }

    private void DrawFarmControlsPanel()
    {
        Rectangle panelBounds = GetControlsPanelBounds();
        DrawPanel(panelBounds, "ACTIONS");

        Rectangle contentBounds = GetPanelContentBounds(panelBounds);
        FarmPlot selectedPlot = _turnipTestState.GetSelectedPlot();
        DebugUiTextFormatter.VisualCommand[] commands = DebugUiTextFormatter.OrderVisualCommands(DebugUiTextFormatter.GetFarmVisualCommands(
            _contentRegistry,
            selectedPlot,
            _turnipTestState.SelectedSeedItemId,
            _turnipTestState.IsPlotHarvestable,
            _debugUiState.IsPaused,
            _turnipTestState.ThemeVocabulary,
            _turnipTestState.ActiveMarketOpportunity));

        DrawFarmVisualCommandSections(contentBounds, commands);
    }

    private void DrawLastActionPanel()
    {
        Rectangle panelBounds = GetLastActionPanelBounds();
        DrawPanel(panelBounds, "LAST ACTION");
        string[] nextHintLines = GetNextHintLines();
        DrawWrappedPanelText(
            panelBounds,
            2,
            LineHeight,
            [
                _lastActionMessage,
                string.Empty,
                "NEXT",
                .. nextHintLines
            ],
            TextColor);
    }

    private void DrawFarmMachinePanel()
    {
        Rectangle panelBounds = GetMachinePanelBounds();
        DrawPanel(panelBounds, _debugUiState.ShowDebugDetails ? "MACHINES DEBUG" : "MACHINES");

        Rectangle contentBounds = GetPanelContentBounds(panelBounds);
        DebugUiTextFormatter.FarmMachineCard[] cards = DebugUiTextFormatter.GetFarmMachineCards(
            _contentRegistry,
            _turnipTestState.ProcessorMachines,
            _turnipTestState.SelectedProcessorMachine.MachineId,
            GetSelectedMachineRecipeId,
            _turnipTestState.GetAccessibleItemCount,
            _turnipTestState.ActiveMarketOpportunity,
            _turnipTestState.GetEffectiveSellPrice,
            maxCards: 2);

        if (cards.Length == 0)
        {
            DrawPanelLinesInRegion(contentBounds, 2, LineHeight, ["NO MACHINES LOADED.", "F3 MACHINES FOR DETAILS"], TextColor);
            return;
        }

        int footerHeight = 12;
        int cardGap = 10;
        int visibleCardCount = Math.Min(2, cards.Length);
        int cardWidth = visibleCardCount == 1
            ? contentBounds.Width
            : (contentBounds.Width - cardGap) / 2;
        int cardHeight = Math.Max(70, contentBounds.Height - footerHeight - 4);

        for (int index = 0; index < visibleCardCount; index++)
        {
            Rectangle cardBounds = new(
                contentBounds.X + (index * (cardWidth + cardGap)),
                contentBounds.Y,
                cardWidth,
                cardHeight);
            DrawFarmMachineCard(cardBounds, cards[index]);
        }

        string footerLine = cards.Length < _turnipTestState.ProcessorMachines.Count
            ? $"F3 MACHINES FOR DETAILS (+{_turnipTestState.ProcessorMachines.Count - cards.Length} MORE)"
            : "F3 MACHINES FOR DETAILS";
        DrawText(contentBounds.X, contentBounds.Bottom - footerHeight, 1, ShortenText(footerLine, GetMaxCharacters(contentBounds.Width, 1)), TextColor);
    }

    private void DrawStatsScreen()
    {
        DrawFullScreenTextPanel(DebugUiTextFormatter.GetScreenTitle(GameScreen.Stats, _turnipTestState.ThemeVocabulary), GetStatsScreenLines());
    }

    private string[] GetNextHintLines()
    {
        return DebugUiTextFormatter.GetNextHintLines(
            _contentRegistry,
            _turnipTestState.FarmGrid,
            _turnipTestState.ProcessorMachines,
            _turnipTestState.Money,
            _turnipTestState.SelectedSeedItemId,
            _turnipTestState.IsPlanterBotUnlocked,
            _turnipTestState.IsWateringBotUnlocked,
            _turnipTestState.IsHarvesterBotUnlocked,
            _turnipTestState.IsFarmFullyExpanded,
            _turnipTestState.NextFarmExpansionCost,
            _turnipTestState.IsFinalFarmExpansionBlockedByCleanup,
            _turnipTestState.CleanupSite,
            _turnipTestState.GetItemCount,
            _turnipTestState.GetStorageItemCount,
            _turnipTestState.GetAccessibleItemCount,
            plot => _turnipTestState.IsPlotHarvestable(plot.X, plot.Y),
            _turnipTestState.ActiveMarketOpportunity,
            _turnipTestState.GetEffectiveSellPrice,
            _debugUiState.IsPaused,
            _debugUiState.IsLoadConfirmationPending,
            maxHints: 3,
            balanceConfig: _turnipTestState.BalanceConfig,
            themeVocabulary: _turnipTestState.ThemeVocabulary);
    }

    private int GetPlotGap()
    {
        return _turnipTestState.FarmGrid.Width >= 5 ? 6 : 8;
    }

    private int GetPlotHeight()
    {
        int plotGap = GetPlotGap();
        int availableHeight = FarmPanelHeight - 56;
        return (availableHeight - ((_turnipTestState.FarmGrid.Height - 1) * plotGap)) / _turnipTestState.FarmGrid.Height;
    }

    private int GetPlotWidth()
    {
        int plotGap = GetPlotGap();
        int availableWidth = FarmPanelWidth - 28;
        return (availableWidth - ((_turnipTestState.FarmGrid.Width - 1) * plotGap)) / _turnipTestState.FarmGrid.Width;
    }

    private Rectangle GetFarmPanelBounds()
    {
        return new Rectangle(Margin, Margin, FarmPanelWidth, FarmPanelHeight);
    }

    private Rectangle GetControlsPanelBounds()
    {
        Rectangle farmPanel = GetFarmPanelBounds();
        return new Rectangle(Margin, farmPanel.Bottom + PanelGap, FarmPanelWidth, ControlsPanelHeight);
    }

    private Rectangle GetLastActionPanelBounds()
    {
        Rectangle anchorPanel = GetControlsPanelBounds();
        return new Rectangle(Margin, anchorPanel.Bottom + PanelGap, FarmPanelWidth, LastActionPanelHeight);
    }

    private Rectangle GetStatsPanelBounds()
    {
        Rectangle controlsPanel = GetControlsPanelBounds();
        return new Rectangle(Margin, controlsPanel.Bottom + PanelGap, FarmPanelWidth, StatsPanelHeight);
    }

    private Rectangle GetDashboardPanelBounds()
    {
        return new Rectangle(RightPanelX, DashboardY, PanelWidth, DashboardPanelHeight);
    }

    private Rectangle GetSelectedPlotPanelBounds()
    {
        Rectangle dashboardPanel = GetDashboardPanelBounds();
        return new Rectangle(RightPanelX, dashboardPanel.Bottom + PanelGap, PanelWidth, SelectedPlotPanelHeight);
    }

    private Rectangle GetInventoryPanelBounds()
    {
        Rectangle selectedPlotPanel = GetSelectedPlotPanelBounds();
        return new Rectangle(RightPanelX, selectedPlotPanel.Bottom + PanelGap, PanelWidth, InventoryPanelHeight);
    }

    private Rectangle GetBotPanelBounds()
    {
        Rectangle inventoryPanel = GetInventoryPanelBounds();
        return new Rectangle(RightPanelX, inventoryPanel.Bottom + PanelGap, PanelWidth, BotPanelHeight);
    }

    private Rectangle GetPanelContentBounds(Rectangle panelBounds)
    {
        return new Rectangle(panelBounds.X + 12, panelBounds.Y + 28, panelBounds.Width - 24, panelBounds.Height - 40);
    }

    private Rectangle GetMachinePanelBounds()
    {
        Rectangle botPanel = GetBotPanelBounds();
        return new Rectangle(RightPanelX, botPanel.Bottom + PanelGap, PanelWidth, MachinePanelHeight);
    }

    private Rectangle GetDashboardAutoPlayBounds(Rectangle panelBounds)
    {
        return new Rectangle(panelBounds.X + 12, panelBounds.Y + 108, panelBounds.Width - 24, panelBounds.Height - 120);
    }

    private Rectangle GetFullScreenPanelBounds()
    {
        return new Rectangle(Margin, Margin, WindowWidth - (Margin * 2), WindowHeight - (Margin * 2));
    }

    private Rectangle GetLoadConfirmationBounds()
    {
        Rectangle fullBounds = GetFullScreenPanelBounds();
        int width = Math.Min(560, fullBounds.Width - 80);
        int height = 96;
        int x = fullBounds.X + ((fullBounds.Width - width) / 2);
        int y = fullBounds.Y + 48;
        return new Rectangle(x, y, width, height);
    }

    private void DrawPanel(Rectangle bounds, string title)
    {
        FillRect(bounds.X, bounds.Y, bounds.Width, bounds.Height, PanelBorderColor);
        FillRect(bounds.X + 4, bounds.Y + 4, bounds.Width - 8, bounds.Height - 8, PanelFillColor);
        FillRect(bounds.X + 8, bounds.Y + 8, bounds.Width - 16, 14, PanelHeaderColor);
        DrawText(bounds.X + 12, bounds.Y + 10, 2, title, TextColor);
    }

    private void DrawStatBox(int x, int y, int width, int height, string label, string value)
    {
        FillRect(x, y, width, height, PanelBorderColor);
        FillRect(x + 2, y + 2, width - 4, height - 4, StatBoxColor);
        DrawText(x + 6, y + 4, 2, label, TextColor);
        DrawText(x + 6, y + 18, 3, value, TextColor);
    }

    private void DrawLines(int x, int y, int scale, int lineHeight, IEnumerable<string> lines, Color color)
    {
        int index = 0;

        foreach (string line in lines)
        {
            DrawText(x, y + (index * lineHeight), scale, line, color);
            index++;
        }
    }

    private void DrawPanelLines(Rectangle panelBounds, int scale, int lineHeight, IEnumerable<string> lines, Color color)
    {
        Rectangle contentBounds = GetPanelContentBounds(panelBounds);
        DrawPanelLinesInRegion(contentBounds, scale, lineHeight, lines, color);
    }

    private void DrawPanelLinesInRegion(Rectangle contentBounds, int scale, int lineHeight, IEnumerable<string> lines, Color color)
    {
        int maxLines = Math.Max(1, contentBounds.Height / lineHeight);
        int maxCharacters = GetMaxCharacters(contentBounds.Width, scale);

        List<string> fittedLines = lines
            .Select(line => ShortenText(line, maxCharacters))
            .ToList();

        if (fittedLines.Count > maxLines)
        {
            fittedLines = fittedLines.Take(maxLines).ToList();
            fittedLines[^1] = ShortenText("...", maxCharacters);
        }

        DrawLines(contentBounds.X, contentBounds.Y, scale, lineHeight, fittedLines, color);
    }

    private void DrawWrappedPanelText(Rectangle panelBounds, int scale, int lineHeight, IEnumerable<string> paragraphs, Color color)
    {
        Rectangle contentBounds = GetPanelContentBounds(panelBounds);
        int maxLines = Math.Max(1, contentBounds.Height / lineHeight);
        int maxCharacters = GetMaxCharacters(contentBounds.Width, scale);
        List<string> wrappedLines = [];

        foreach (string paragraph in paragraphs)
        {
            if (string.IsNullOrEmpty(paragraph))
            {
                wrappedLines.Add(string.Empty);
                continue;
            }

            wrappedLines.AddRange(WrapText(paragraph, maxCharacters));
        }

        if (wrappedLines.Count > maxLines)
        {
            wrappedLines = wrappedLines.Take(maxLines).ToList();
            wrappedLines[^1] = ShortenText(wrappedLines[^1], maxCharacters);
        }

        DrawLines(contentBounds.X, contentBounds.Y, scale, lineHeight, wrappedLines, color);
    }

    private void DrawFullScreenTextPanel(string title, IEnumerable<string> lines)
    {
        Rectangle panelBounds = GetFullScreenPanelBounds();
        DrawPanel(panelBounds, _debugUiState.ShowDebugDetails ? title + " DEBUG" : title);
        DrawPanelLines(panelBounds, 2, LineHeight, lines, TextColor);
    }

    private void DrawLoadConfirmationOverlay()
    {
        Rectangle panelBounds = GetLoadConfirmationBounds();
        DrawPanel(panelBounds, "LOAD CONFIRMATION");
        DrawPanelLines(panelBounds, 2, LineHeight, DebugUiTextFormatter.GetLoadConfirmationLines(_gameSaveController.Status.SavePath), TextColor);
    }

    private int GetMaxCharacters(int width, int scale)
    {
        return Math.Max(8, (width - 8) / (4 * scale));
    }

    private string ShortenText(string text, int maxCharacters)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxCharacters)
            return text;

        if (maxCharacters <= 3)
            return text[..maxCharacters];

        return text[..(maxCharacters - 3)] + "...";
    }

    private IEnumerable<string> WrapText(string text, int maxCharacters)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [string.Empty];

        List<string> lines = [];
        string[] words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string currentLine = string.Empty;

        foreach (string word in words)
        {
            string nextLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
            if (nextLine.Length <= maxCharacters)
            {
                currentLine = nextLine;
                continue;
            }

            if (!string.IsNullOrEmpty(currentLine))
                lines.Add(currentLine);

            currentLine = word;
        }

        if (!string.IsNullOrEmpty(currentLine))
            lines.Add(currentLine);

        return lines;
    }

    private List<string> GetInventoryLines()
    {
        return DebugUiTextFormatter
            .GetInventoryLines(_contentRegistry, GetItemCount, _debugUiState.ShowDebugDetails)
            .ToList();
    }

    private string[] GetInventoryScreenLines()
    {
        return DebugUiTextFormatter.GetInventoryScreenLines(
            _contentRegistry,
            GetItemCount,
            GetStorageItemCount,
            _debugUiState.ShowDebugDetails,
            _autoPlayController.IsEnabled,
            _debugUiState.IsPaused,
            _turnipTestState.GetEffectiveSellPrice,
            _turnipTestState.ThemeVocabulary);
    }

    private string[] GetStorageScreenLines()
    {
        return DebugUiTextFormatter.GetStorageScreenLines(
            _contentRegistry,
            GetStorageItemCount,
            _debugUiState.ShowDebugDetails,
            _autoPlayController.IsEnabled,
            _debugUiState.IsPaused,
            _turnipTestState.GetEffectiveSellPrice,
            _turnipTestState.ThemeVocabulary);
    }

    private string[] GetComputerScreenLines()
    {
        return DebugUiTextFormatter.GetComputerScreenLines(
            _contentRegistry,
            _turnipTestState.FarmGrid,
            _turnipTestState.ProcessorMachines,
            _turnipTestState.ActivityLog,
            _gameSaveController.Status,
            _turnipTestState.Stats,
            _turnipTestState.DayNumber,
            _turnipTestState.Money,
            _turnipTestState.SelectedSeedItemId,
            _turnipTestState.IsPlanterBotUnlocked,
            _turnipTestState.IsWateringBotUnlocked,
            _turnipTestState.IsHarvesterBotUnlocked,
            _turnipTestState.IsFarmFullyExpanded,
            _turnipTestState.NextFarmExpansionCost,
            _turnipTestState.IsFinalFarmExpansionBlockedByCleanup,
            GetItemCount,
            GetStorageItemCount,
            _turnipTestState.GetAccessibleItemCount,
            plot => _turnipTestState.IsPlotHarvestable(plot.X, plot.Y),
            _debugUiState.ShowDebugDetails,
            _autoPlayController.IsEnabled,
            _debugUiState.IsPaused,
            _turnipTestState.ActiveMarketOpportunity,
            _turnipTestState.GetEffectiveSellPrice,
            _turnipTestState.RestorationSites,
            _turnipTestState.BalanceConfig,
            _turnipTestState.ThemeVocabulary,
            _turnipTestState.GetDaysUntilNextMarketOpportunity(),
                _turnipTestState.GetNextMarketOpportunityCandidateItemId(),
                _turnipTestState.GetUnlockedProductionAreas());
    }

    private string[] GetActivityLogScreenLines()
    {
        return DebugUiTextFormatter.GetActivityLogScreenLines(
            _turnipTestState.ActivityLog,
            _autoPlayController.IsEnabled,
            _debugUiState.IsPaused,
            _turnipTestState.ThemeVocabulary);
    }

    private string[] GetProductionReportScreenLines()
    {
        return DebugUiTextFormatter.GetProductionReportScreenLines(
            _contentRegistry,
            _turnipTestState.ActiveMarketOpportunity,
            _turnipTestState.GetEffectiveSellPrice,
            _autoPlayController.IsEnabled,
            _debugUiState.IsPaused);
    }

    private string[] GetProgressionReportScreenLines()
    {
        return DebugUiTextFormatter.GetProgressionReportScreenLines(
            _progressionSimulationResult,
            _autoPlayController.IsEnabled,
            _debugUiState.IsPaused,
            _turnipTestState.ThemeVocabulary);
    }

    private string[] GetMarketScreenLines()
    {
        return DebugUiTextFormatter.GetMarketScreenLines(
            _contentRegistry,
            _turnipTestState.MarketOffers,
            _turnipTestState.Money,
            _debugUiState.ShowDebugDetails,
            _autoPlayController.IsEnabled,
            _debugUiState.IsPaused,
            _turnipTestState.ActiveMarketOpportunity,
            _turnipTestState.GetEffectiveSellPrice);
    }

    private List<(string Text, Color Color)> GetBotLines()
    {
        string planterStatus = DebugUiTextFormatter.GetBotStatusText(
            _turnipTestState.ThemeVocabulary.ToUpperLabel(BotRoleFormatter.GetBotShortDisplayName(BotRole.Starter, _turnipTestState.ThemeVocabulary)),
            _turnipTestState.IsBotUnlocked(BotRole.Starter),
            _turnipTestState.GetBotCost(BotRole.Starter),
            _turnipTestState.Money);
        string watererStatus = DebugUiTextFormatter.GetBotStatusText(
            _turnipTestState.ThemeVocabulary.ToUpperLabel(BotRoleFormatter.GetBotShortDisplayName(BotRole.Maintainer, _turnipTestState.ThemeVocabulary)),
            _turnipTestState.IsBotUnlocked(BotRole.Maintainer),
            _turnipTestState.GetBotCost(BotRole.Maintainer),
            _turnipTestState.Money,
            _turnipTestState.WateringBot.Enabled ? "UNLOCKED AUTO ON" : "UNLOCKED MANUAL");
        string harvesterStatus = DebugUiTextFormatter.GetBotStatusText(
            _turnipTestState.ThemeVocabulary.ToUpperLabel(BotRoleFormatter.GetBotShortDisplayName(BotRole.Collector, _turnipTestState.ThemeVocabulary)),
            _turnipTestState.IsBotUnlocked(BotRole.Collector),
            _turnipTestState.GetBotCost(BotRole.Collector),
            _turnipTestState.Money);

        List<(string Text, Color Color)> lines =
        [
            (planterStatus, GetBotStatusColor(_turnipTestState.IsBotUnlocked(BotRole.Starter), _turnipTestState.GetBotCost(BotRole.Starter))),
            (watererStatus, GetBotStatusColor(_turnipTestState.IsBotUnlocked(BotRole.Maintainer), _turnipTestState.GetBotCost(BotRole.Maintainer))),
            (harvesterStatus, GetBotStatusColor(_turnipTestState.IsBotUnlocked(BotRole.Collector), _turnipTestState.GetBotCost(BotRole.Collector)))
        ];

        if (!_debugUiState.ShowDebugDetails)
            return lines;

        lines.Add(($"PLANTER SEED {GetItemName(_turnipTestState.PlanterBot.AssignedSeedItemId)} ID {_turnipTestState.PlanterBot.AssignedSeedItemId}", TextColor));
        lines.Add(($"PLANTER REGION {FormatRegion(_turnipTestState.PlanterBot.Region)}", TextColor));
        lines.Add(($"WATERER REGION {FormatRegion(_turnipTestState.WateringBot.Region)}", TextColor));
        lines.Add(($"HARVESTER REGION {FormatRegion(_turnipTestState.HarvesterBot.Region)}", TextColor));
        return lines;
    }

    private string FormatRegion(FarmRegion region)
    {
        return $"{region.X},{region.Y} {region.Width}X{region.Height}";
    }

    private string[] GetStatsScreenLines()
    {
        return DebugUiTextFormatter.GetStatsScreenLines(
            _contentRegistry,
            _turnipTestState.Stats,
            _turnipTestState.FarmGrid.Width,
            _turnipTestState.FarmGrid.Height,
            _debugUiState.ShowDebugDetails,
            _autoPlayController.IsEnabled,
            _debugUiState.IsPaused,
            _turnipTestState.ThemeVocabulary);
    }

    private string[] GetMachineScreenLines()
    {
        return DebugUiTextFormatter.GetMachineScreenLines(
            _contentRegistry,
            _turnipTestState.ProcessorMachines,
            _turnipTestState.SelectedProcessorMachine.MachineId,
            GetSelectedMachineRecipeId(_turnipTestState.SelectedProcessorMachine.MachineId),
            _turnipTestState.Stats,
            _turnipTestState.GetAccessibleItemCount,
            _debugUiState.ShowDebugDetails,
            _autoPlayController.IsEnabled,
            _debugUiState.IsPaused,
            _turnipTestState.ActiveMarketOpportunity,
            _turnipTestState.GetEffectiveSellPrice);
    }

    private string[] GetFarmControlLines()
    {
        return DebugUiTextFormatter.GetFarmControlLines(
            _contentRegistry,
            _turnipTestState.GetSelectedPlot(),
            _turnipTestState.SelectedSeedItemId,
            _turnipTestState.IsPlotHarvestable,
            _debugUiState.IsPaused,
            _turnipTestState.InterfacePack,
            _turnipTestState.ThemeVocabulary,
            _turnipTestState.ActiveMarketOpportunity,
            _turnipTestState.GetEffectiveSellPrice,
            _turnipTestState.GetDaysUntilNextMarketOpportunity());
    }

    private string[] GetCompactMachineScreenLines()
    {
        return DebugUiTextFormatter.GetFarmMachinePanelLines(
            _contentRegistry,
            _turnipTestState.ProcessorMachines,
            _turnipTestState.SelectedProcessorMachine.MachineId,
            GetSelectedMachineRecipeId,
            _turnipTestState.GetAccessibleItemCount,
            _turnipTestState.ActiveMarketOpportunity,
            _turnipTestState.GetEffectiveSellPrice,
            maxCards: 2);
    }

    private string[] GetHelpScreenLines()
    {
        return DebugUiTextFormatter.GetHelpScreenLines(
            _contentRegistry,
            _turnipTestState,
            _gameSaveController.Status,
            _autoPlayController.IsEnabled,
            _debugUiState.IsPaused,
            _turnipTestState.ThemeVocabulary);
    }

    private string[] GetAutoPlayStatusLines()
    {
        AutoPlayStats stats = _autoPlayController.Stats;
        string onOff = _autoPlayController.IsEnabled ? "ON" : "OFF";
        string seedSafety = _autoPlayController.IsSeedSafetySatisfied ? "SAFE" : "NOT SAFE";
        List<string> lines =
        [
            DebugUiTextFormatter.GetSimulationStatusLine(_debugUiState.IsPaused, _autoPlayController.IsEnabled),
            $"AUTO {onOff} | TARGET ${_autoPlayController.Settings.TargetMoney} | SEEDS {_autoPlayController.CurrentSeedCount}/{_autoPlayController.RequiredSeedSafetyCount} {seedSafety}",
            $"STEPS {stats.StepsRun} DAYS {stats.DaysSimulated} MONEY {_turnipTestState.Money} BUY {stats.SeedsBought} HARV {stats.CropsHarvested} SOLD {stats.CropsSold} BOTS {stats.BotsUnlocked} EXP {stats.ExpansionsCompleted}",
            $"LAST {stats.LastAutoAction}"
        ];

        string marketOpportunityLine = DebugUiTextFormatter.FormatMarketOpportunityLine(_contentRegistry, _turnipTestState.ActiveMarketOpportunity);
        if (!string.IsNullOrWhiteSpace(marketOpportunityLine))
            lines.Insert(1, $"MARKET {marketOpportunityLine}");

        string cleanupStatusLine = DebugUiTextFormatter.FormatCleanupStatusLine(_turnipTestState.CleanupSite);
        if (!string.IsNullOrWhiteSpace(cleanupStatusLine))
            lines.Insert(Math.Min(lines.Count, 1), cleanupStatusLine);

        if (!string.IsNullOrEmpty(stats.StopReasonText))
            lines.Add($"STOP {stats.StopReasonText}");

        if (_debugUiState.ShowDebugDetails)
        {
            lines.Add($"MANUAL P {stats.ManualPlantActions} W {stats.ManualWaterActions} H {stats.ManualHarvestActions}");
            lines.Add($"BOT P {stats.PlanterBotUses} W {stats.WateringBotUses} H {stats.HarvesterBotUses} IDLE {stats.ConsecutiveNoActionSteps}");
        }

        return lines.ToArray();
    }

    private void DrawFarmGrid(int startX, int startY, int plotWidth, int plotHeight, int gap)
    {
        InterfacePack pack = _turnipTestState.InterfacePack;
        foreach (FarmPlot plot in _turnipTestState.FarmGrid.GetAllPlots())
        {
            int x = startX + (plot.X * (plotWidth + gap));
            int y = startY + (plot.Y * (plotHeight + gap));

            bool isSelected = plot.X == _turnipTestState.SelectedPlotX && plot.Y == _turnipTestState.SelectedPlotY;
            FarmPlot visualPlot = plot;
            if (_debugUiState.TryGetBotActivityTileOverride(plot.X, plot.Y, out FarmPlotVisualSnapshot snapshot))
                visualPlot = snapshot.ToPlot(plot.X, plot.Y);

            bool isReady = IsVisualPlotReady(visualPlot);
            Rectangle tileBounds = new(x, y, plotWidth, plotHeight);

            Color borderColor = isSelected
                ? DebugUiTextFormatter.GetTileSelectedBorderColor(pack)
                : DebugUiTextFormatter.GetTileNormalBorderColor(pack);
            Color fillColor = DebugUiTextFormatter.GetTileFillColor(visualPlot, pack);
            if (isReady)
                fillColor = BlendColor(fillColor, DebugUiTextFormatter.GetTileReadyTextColor(pack), 0.28f);

            FillRect(x, y, plotWidth, plotHeight, borderColor);
            FillRect(x + 2, y + 2, plotWidth - 4, plotHeight - 4, fillColor);

            if (isSelected)
            {
                Color selectedBorderColor = DebugUiTextFormatter.GetTileSelectedBorderColor(pack);
                FillRect(x + 4, y + 4, plotWidth - 8, 5, selectedBorderColor);
                FillRect(x + 4, y + plotHeight - 9, plotWidth - 8, 5, selectedBorderColor);
                FillRect(x + 4, y + 9, 5, plotHeight - 18, selectedBorderColor);
                FillRect(x + plotWidth - 9, y + 9, 5, plotHeight - 18, selectedBorderColor);
            }

            DrawTileVisual(tileBounds, visualPlot, isReady, isSelected, pack);

            if (_debugUiState.IsPlayingBotActivity && plot.X == _debugUiState.ActiveTileX && plot.Y == _debugUiState.ActiveTileY)
                DrawBotActivityToken(tileBounds);
        }
    }

    private bool IsVisualPlotReady(FarmPlot plot)
    {
        if (plot?.PlantedCrop is null)
            return false;

        TileProducerDefinition producer = _contentRegistry.GetTileProducer(plot.PlantedCrop.CropDefinitionId);
        return plot.PlantedCrop.DaysGrown >= producer.ProductionDays;
    }

    private void DrawBotActivityToken(Rectangle tileBounds)
    {
        Color fillColor = GetBotActivityTokenFillColor();
        Color textColor = GetBotActivityTokenTextColor();
        string label = GetBotActivityTokenLabel();
        int tokenSize = Math.Min(tileBounds.Width, tileBounds.Height) / 3;
        Rectangle tokenBounds = new(
            tileBounds.Center.X - (tokenSize / 2),
            tileBounds.Y + 8,
            tokenSize,
            tokenSize);

        FillRect(tokenBounds.X, tokenBounds.Y, tokenBounds.Width, tokenBounds.Height, fillColor);
        FillRect(tokenBounds.X + 2, tokenBounds.Y + 2, tokenBounds.Width - 4, tokenBounds.Height - 4, BlendColor(fillColor, Color.White, 0.18f));
        DrawCenteredText(tokenBounds, tokenBounds.Y + Math.Max(4, (tokenBounds.Height / 2) - 6), tokenBounds.Width, 2, label, textColor);
    }

    private string GetBotActivityTokenLabel()
    {
        return _debugUiState.ActiveBotRole switch
        {
            BotRole.Starter => "P",
            BotRole.Maintainer => "W",
            BotRole.Collector => "H",
            _ => "?"
        };
    }

    private Color GetBotActivityTokenFillColor()
    {
        return _debugUiState.ActiveBotRole switch
        {
            BotRole.Starter => new Color(54, 160, 78),
            BotRole.Maintainer => new Color(48, 118, 212),
            BotRole.Collector => new Color(212, 148, 34),
            _ => new Color(96, 96, 96)
        };
    }

    private static Color GetBotActivityTokenTextColor()
    {
        return new Color(24, 24, 24);
    }

    private void DrawTileVisual(Rectangle tileBounds, FarmPlot plot, bool isReady, bool isSelected, InterfacePack pack)
    {
        DebugUiTextFormatter.TileDisplayState displayState = DebugUiTextFormatter.GetTileDisplayState(plot, isReady);
        string primarySymbol = DebugUiTextFormatter.GetTilePrimarySymbol(plot, _contentRegistry, pack);
        float progressFraction = DebugUiTextFormatter.GetTileProgressFraction(plot, _contentRegistry);
        Rectangle progressBounds = DebugUiTextFormatter.GetTileProgressBarRect(tileBounds, progressFraction);
        Rectangle progressTrack = new(
            progressBounds.X,
            progressBounds.Y,
            Math.Max(12, tileBounds.Width - ((progressBounds.X - tileBounds.X) * 2)),
            progressBounds.Height);
        Color symbolColor = displayState == DebugUiTextFormatter.TileDisplayState.Ready
            ? DebugUiTextFormatter.GetTileReadyTextColor(pack)
            : new Color(28, 28, 28);

        FillRect(progressTrack.X, progressTrack.Y, progressTrack.Width, progressTrack.Height, new Color(36, 48, 36, 180));
        if (progressBounds.Width > 0)
            FillRect(progressBounds.X, progressBounds.Y, progressBounds.Width, progressBounds.Height, symbolColor);

        if (displayState == DebugUiTextFormatter.TileDisplayState.Ready)
            DrawCenteredText(tileBounds, tileBounds.Y + 10, tileBounds.Width, 1, "READY", symbolColor);

        int symbolScale = tileBounds.Width >= 100 ? 6 : tileBounds.Width >= 72 ? 5 : 4;
        int symbolY = tileBounds.Y + (displayState == DebugUiTextFormatter.TileDisplayState.Ready ? 26 : 20);
        DrawCenteredText(tileBounds, symbolY, tileBounds.Width, symbolScale, primarySymbol, symbolColor);

        if (isSelected)
            DrawCenteredText(tileBounds, tileBounds.Bottom - 22, tileBounds.Width, 1, DebugUiTextFormatter.GetTileSelectionMarker(pack), symbolColor);
    }

    private void DrawCenteredText(Rectangle tileBounds, int y, int width, int scale, string text, Color color)
    {
        int textWidth = GetTextWidth(text, scale);
        int drawX = tileBounds.X + Math.Max(6, (width - textWidth) / 2);
        DrawText(drawX, y, scale, text, color);
    }

    private int GetTextWidth(string text, int scale)
    {
        int width = 0;
        foreach (char rawCharacter in text)
        {
            char character = char.ToUpperInvariant(rawCharacter);
            if (!PixelFont.TryGetValue(character, out string[] glyph))
            {
                width += 4 * scale;
                continue;
            }

            width += (glyph[0].Length + 1) * scale;
        }

        return Math.Max(0, width - scale);
    }

    private static Color BlendColor(Color first, Color second, float amount)
    {
        float clamped = Math.Clamp(amount, 0f, 1f);
        return new Color(
            (byte)(first.R + ((second.R - first.R) * clamped)),
            (byte)(first.G + ((second.G - first.G) * clamped)),
            (byte)(first.B + ((second.B - first.B) * clamped)));
    }

    private void DrawVisualCommandGrid(Rectangle bounds, IReadOnlyList<DebugUiTextFormatter.VisualCommand> commands, int columns)
    {
        if (commands.Count == 0)
            return;

        int safeColumns = Math.Max(1, columns);
        int rows = (int)Math.Ceiling(commands.Count / (float)safeColumns);
        int horizontalGap = 8;
        int verticalGap = 6;
        int cellWidth = (bounds.Width - ((safeColumns - 1) * horizontalGap)) / safeColumns;
        int cellHeight = Math.Max(18, (bounds.Height - ((rows - 1) * verticalGap)) / Math.Max(1, rows));

        for (int index = 0; index < commands.Count; index++)
        {
            int column = index % safeColumns;
            int row = index / safeColumns;
            Rectangle cellBounds = new(
                bounds.X + (column * (cellWidth + horizontalGap)),
                bounds.Y + (row * (cellHeight + verticalGap)),
                cellWidth,
                cellHeight);
            DrawVisualCommandButton(cellBounds, commands[index]);
        }
    }

    private void DrawFarmMachineCard(Rectangle bounds, DebugUiTextFormatter.FarmMachineCard card)
    {
        InterfacePack pack = _turnipTestState.InterfacePack;
        Color borderColor = GetMachineCardBorderColor(card, pack);
        Color fillColor = GetMachineCardFillColor(card, borderColor);
        Color accentColor = card.ThemeRole == DebugUiTextFormatter.VisualCommandThemeRole.Ready
            ? DebugUiTextFormatter.GetTileReadyTextColor(pack)
            : borderColor;
        Color detailColor = card.ActionCommand?.IsEnabled == false
            ? BlendColor(TextColor, Color.White, 0.34f)
            : TextColor;

        FillRect(bounds.X, bounds.Y, bounds.Width, bounds.Height, borderColor);
        FillRect(bounds.X + 2, bounds.Y + 2, bounds.Width - 4, bounds.Height - 4, fillColor);

        Rectangle tokenBounds = new(bounds.X + 8, bounds.Y + 8, 30, 20);
        FillRect(tokenBounds.X, tokenBounds.Y, tokenBounds.Width, tokenBounds.Height, borderColor);
        FillRect(tokenBounds.X + 1, tokenBounds.Y + 1, tokenBounds.Width - 2, tokenBounds.Height - 2, BlendColor(borderColor, Color.White, 0.38f));
        DrawCenteredText(tokenBounds, tokenBounds.Y + 6, tokenBounds.Width, 2, "M", TextColor);

        int textStartX = tokenBounds.Right + 8;
        int titleWidth = Math.Max(20, bounds.Right - textStartX - 8);
        DrawText(textStartX, bounds.Y + 10, 2, ShortenText(card.MachineName.ToUpperInvariant(), GetMaxCharacters(titleWidth, 2)), TextColor);
        DrawText(bounds.X + 8, bounds.Y + 34, 2, ShortenText(card.StatusLabel.ToUpperInvariant(), GetMaxCharacters(bounds.Width - 16, 2)), accentColor);

        int lineY = bounds.Y + 50;
        foreach (string detailLine in card.DetailLines.Take(3))
        {
            DrawText(bounds.X + 8, lineY, 1, ShortenText(detailLine.ToUpperInvariant(), GetMaxCharacters(bounds.Width - 16, 1)), detailColor);
            lineY += 10;
        }

        if (card.ActionCommand is not null)
        {
            Color actionColor = card.ActionCommand.Value.IsEnabled ? accentColor : detailColor;
            string actionLine = DebugUiTextFormatter.FormatVisualCommand(card.ActionCommand.Value).ToUpperInvariant();
            DrawText(bounds.X + 8, bounds.Bottom - 14, 1, ShortenText(actionLine, GetMaxCharacters(bounds.Width - 16, 1)), actionColor);
        }
    }

    private Color GetMachineCardBorderColor(DebugUiTextFormatter.FarmMachineCard card, InterfacePack pack)
    {
        if (card.ThemeRole == DebugUiTextFormatter.VisualCommandThemeRole.Ready)
            return DebugUiTextFormatter.GetTileReadyTextColor(pack);

        if (card.ThemeRole == DebugUiTextFormatter.VisualCommandThemeRole.MarketBonus)
            return BlendColor(pack.SelectedBorderColor, new Color(212, 150, 44), 0.45f);

        if (card.IsSelected)
            return pack.SelectedBorderColor;

        if (card.ThemeRole == DebugUiTextFormatter.VisualCommandThemeRole.Disabled)
            return BlendColor(PanelBorderColor, Color.White, 0.2f);

        return pack.NormalBorderColor;
    }

    private Color GetMachineCardFillColor(DebugUiTextFormatter.FarmMachineCard card, Color borderColor)
    {
        Color baseFill = card.IsSelected
            ? BlendColor(PanelFillColor, Color.White, 0.08f)
            : PanelFillColor;

        if (card.ThemeRole == DebugUiTextFormatter.VisualCommandThemeRole.Ready)
            return BlendColor(baseFill, DebugUiTextFormatter.GetTileReadyTextColor(_turnipTestState.InterfacePack), 0.08f);

        if (card.ThemeRole == DebugUiTextFormatter.VisualCommandThemeRole.MarketBonus)
            return BlendColor(baseFill, borderColor, 0.1f);

        return baseFill;
    }

    private void DrawFarmVisualCommandSections(Rectangle contentBounds, IReadOnlyList<DebugUiTextFormatter.VisualCommand> commands)
    {
        DebugUiTextFormatter.VisualCommand? primary = DebugUiTextFormatter.GetPrimaryVisualCommand(commands);
        DebugUiTextFormatter.VisualCommand[] marketCommands = commands
            .Where(command => primary is null || !command.Equals(primary.Value))
            .Where(command => command.Group == DebugUiTextFormatter.VisualCommandGroup.Market)
            .Take(1)
            .ToArray();
        DebugUiTextFormatter.VisualCommand[] secondaryCommands = commands
            .Where(command => primary is null || !command.Equals(primary.Value))
            .Where(command => command.Group != DebugUiTextFormatter.VisualCommandGroup.Market)
            .Take(3)
            .ToArray();

        int y = contentBounds.Y;

        if (primary is not null)
        {
            DrawText(contentBounds.X, y, 1, "PRIMARY", TextColor);
            y += 12;
            DrawVisualCommandButton(new Rectangle(contentBounds.X, y, contentBounds.Width, 34), primary.Value, emphasize: true);
            y += 40;
        }

        if (marketCommands.Length > 0)
        {
            DrawText(contentBounds.X, y, 1, "MARKET", GetVisualCommandBorderColor(marketCommands[0]));
            y += 10;
            foreach (DebugUiTextFormatter.VisualCommand command in marketCommands)
            {
                DrawVisualCommandButton(new Rectangle(contentBounds.X, y, contentBounds.Width, 24), command, emphasize: false);
                y += 28;
            }
        }

        if (secondaryCommands.Length > 0)
        {
            DrawText(contentBounds.X, y, 1, "ALSO", TextColor);
            y += 10;
            foreach (DebugUiTextFormatter.VisualCommand command in secondaryCommands)
            {
                DrawVisualCommandButton(new Rectangle(contentBounds.X, y, contentBounds.Width, 24), command, emphasize: false);
                y += 26;
            }
        }
    }

    private void DrawVisualCommandButton(Rectangle bounds, DebugUiTextFormatter.VisualCommand command, bool emphasize = false)
    {
        Color borderColor = GetVisualCommandBorderColor(command);
        Color fillColor = emphasize
            ? BlendColor(GetVisualCommandFillColor(command), Color.White, 0.16f)
            : GetVisualCommandFillColor(command);
        Color keyFillColor = command.IsEnabled ? BlendColor(borderColor, Color.White, 0.45f) : BlendColor(fillColor, Color.White, 0.2f);
        Color textColor = command.IsEnabled ? TextColor : BlendColor(TextColor, Color.White, 0.35f);
        int keyWidth = emphasize
            ? Math.Min(100, Math.Max(76, bounds.Width / 4))
            : Math.Min(84, Math.Max(60, bounds.Width / 4));
        int inset = emphasize ? 3 : 2;
        Rectangle keyBounds = new(bounds.X + 6, bounds.Y + inset, keyWidth, bounds.Height - (inset * 2));

        FillRect(bounds.X, bounds.Y, bounds.Width, bounds.Height, borderColor);
        FillRect(bounds.X + 1, bounds.Y + 1, bounds.Width - 2, bounds.Height - 2, fillColor);
        FillRect(keyBounds.X, keyBounds.Y, keyBounds.Width, keyBounds.Height, borderColor);
        FillRect(keyBounds.X + 1, keyBounds.Y + 1, keyBounds.Width - 2, keyBounds.Height - 2, keyFillColor);

        string keyText = command.KeyLabel.ToUpperInvariant();
        int keyScale = command.KeyLabel.Length <= 2 ? (emphasize ? 2 : 2) : 1;
        int keyTextHeight = 5 * keyScale;
        int keyTextY = keyBounds.Y + Math.Max(3, ((keyBounds.Height - keyTextHeight) / 2));
        DrawCenteredText(keyBounds, keyTextY, keyBounds.Width, keyScale, keyText, textColor);

        int actionX = keyBounds.Right + 10;
        int actionWidth = Math.Max(16, bounds.Right - actionX - 8);
        string statusLine = command.IsEnabled
            ? command.StatusLabel
            : string.IsNullOrWhiteSpace(command.ReasonDisabled) ? "UNAVAILABLE" : command.ReasonDisabled;
        int actionScale = 1;
        DrawText(actionX, bounds.Y + (emphasize ? 9 : 7), actionScale, ShortenText(command.ActionLabel.ToUpperInvariant(), GetMaxCharacters(actionWidth, actionScale)), textColor);

        if (!string.IsNullOrWhiteSpace(statusLine))
            DrawText(actionX, bounds.Y + (emphasize ? 19 : 15), 1, ShortenText(statusLine.ToUpperInvariant(), GetMaxCharacters(actionWidth, 1)), command.IsEnabled ? borderColor : textColor);
    }

    private Color GetVisualCommandBorderColor(DebugUiTextFormatter.VisualCommand command)
    {
        InterfacePack pack = _turnipTestState.InterfacePack;
        return command.ThemeRole switch
        {
            DebugUiTextFormatter.VisualCommandThemeRole.Primary => pack.SelectedBorderColor,
            DebugUiTextFormatter.VisualCommandThemeRole.Ready => DebugUiTextFormatter.GetTileReadyTextColor(pack),
            DebugUiTextFormatter.VisualCommandThemeRole.MarketBonus => BlendColor(pack.SelectedBorderColor, new Color(212, 150, 44), 0.45f),
            DebugUiTextFormatter.VisualCommandThemeRole.Disabled => new Color(112, 96, 86),
            _ => PanelBorderColor
        };
    }

    private Color GetVisualCommandFillColor(DebugUiTextFormatter.VisualCommand command)
    {
        return command.ThemeRole switch
        {
            DebugUiTextFormatter.VisualCommandThemeRole.Primary => new Color(206, 232, 196),
            DebugUiTextFormatter.VisualCommandThemeRole.Ready => new Color(236, 222, 192),
            DebugUiTextFormatter.VisualCommandThemeRole.MarketBonus => new Color(242, 228, 182),
            DebugUiTextFormatter.VisualCommandThemeRole.Disabled => new Color(212, 205, 196),
            _ => new Color(228, 233, 214)
        };
    }

    private Color GetBotStatusColor(bool isUnlocked, int cost)
    {
        if (isUnlocked)
            return new Color(24, 110, 36);

        return _turnipTestState.Money >= cost
            ? new Color(150, 104, 24)
            : new Color(136, 44, 44);
    }
}
