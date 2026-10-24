using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TomatoGrow.GameContent;
using TomatoGrow.GameContent.Definitions;

namespace TomatoGrow;

public partial class Game1 : Game
{
    private static readonly Color PanelBorderColor = new(28, 44, 28, 220);
    private static readonly Color PanelFillColor = new(224, 232, 206);
    private static readonly Color PanelHeaderColor = new(190, 205, 174);
    private static readonly Color TextColor = new(38, 52, 38);
    private static readonly Color StatBoxColor = new(207, 219, 188);

    private const int Margin = 20;
    private const int PanelGap = 14;
    private const int FarmPanelWidth = 432;
    private const int FarmPanelHeight = 390;
    private const int ControlsPanelHeight = 152;
    private const int StatsPanelHeight = 176;
    private const int LastActionPanelHeight = 170;
    private const int RightPanelX = Margin + FarmPanelWidth + PanelGap;
    private const int DashboardY = Margin;
    private const int PanelWidth = 538;
    private const int DashboardPanelHeight = 190;
    private const int SelectedPlotPanelHeight = 126;
    private const int InventoryPanelHeight = 198;
    private const int BotPanelHeight = 170;
    private const int MachinePanelHeight = 138;
    private const int FarmGridX = Margin + 14;
    private const int FarmGridY = Margin + 36;
    private const int LineHeight = 14;
    private const int WindowWidth = RightPanelX + PanelWidth + Margin;
    private const int WindowHeight = Margin + FarmPanelHeight + PanelGap + ControlsPanelHeight + PanelGap + StatsPanelHeight + PanelGap + LastActionPanelHeight + Margin;

    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private Texture2D _pixel = null!;

    private static readonly string ContentModulesPath = Path.Combine(AppContext.BaseDirectory, "ContentModules");
    private static readonly string SaveGamePath = Path.Combine(AppContext.BaseDirectory, "savegame.json");

    private readonly ContentRegistry _contentRegistry;
    private readonly TurnipTestState _turnipTestState;
    private readonly DebugUiState _debugUiState = new();
    private readonly AutoPlayTestController _autoPlayController;
    private readonly GameSaveController _gameSaveController;
    private readonly ProgressionSimulationRunner _progressionSimulationRunner;

    private KeyboardState _previousKeyboard;
    private string _lastActionMessage = "Ready.";
    private double _autoPlayElapsedSeconds;
    private ProgressionSimulationResult _progressionSimulationResult;

    public Game1()
    {
        GameBalanceConfig balanceConfig = GameBalanceConfig.Default;
        _contentRegistry = LoadContentRegistry();
        _turnipTestState = new TurnipTestState(_contentRegistry, balanceConfig: balanceConfig);
        _autoPlayController = new AutoPlayTestController(_turnipTestState, _contentRegistry);
        _gameSaveController = new GameSaveController(_turnipTestState, _debugUiState, _autoPlayController, SaveGamePath);
        _progressionSimulationRunner = new ProgressionSimulationRunner(_contentRegistry, balanceConfig);

        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _graphics.PreferredBackBufferWidth = WindowWidth;
        _graphics.PreferredBackBufferHeight = WindowHeight;

        ResetGame();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    private static ContentRegistry LoadContentRegistry()
    {
        ContentModuleLoader loader = new();
        ContentRegistry registry = loader.LoadFromModules(ContentModulesPath);

        ContentValidator validator = new();
        ContentValidationResult validationResult = validator.Validate(registry);

        foreach (string warning in validationResult.Warnings)
        {
            Console.WriteLine($"Content warning: {warning}");
            Debug.WriteLine($"Content warning: {warning}");
        }

        foreach (string error in validationResult.Errors)
        {
            Console.WriteLine($"Content error: {error}");
            Debug.WriteLine($"Content error: {error}");
        }

        if (!validationResult.IsValid)
            throw new InvalidOperationException(
                "Content validation failed:\n" + string.Join("\n", validationResult.Errors));

        Console.WriteLine($"Loaded {registry.Items.Count} items and {registry.Crops.Count} crops.");
        return registry;
    }

    private void FillRect(int x, int y, int width, int height, Color color)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(x, y, width, height), color);
    }

    private void DrawText(int x, int y, int scale, string text, Color color)
    {
        int cursorX = x;

        foreach (char rawCharacter in text)
        {
            char character = char.ToUpperInvariant(rawCharacter);

            if (!PixelFont.TryGetValue(character, out string[] glyph))
            {
                cursorX += 4 * scale;
                continue;
            }

            for (int row = 0; row < glyph.Length; row++)
            {
                for (int column = 0; column < glyph[row].Length; column++)
                {
                    if (glyph[row][column] == '1')
                    {
                        FillRect(
                            cursorX + (column * scale),
                            y + (row * scale),
                            scale,
                            scale,
                            color);
                    }
                }
            }

            cursorX += (glyph[0].Length + 1) * scale;
        }
    }

    private static readonly Dictionary<char, string[]> PixelFont = new()
    {
        [' '] = ["000", "000", "000", "000", "000"],
        ['!'] = ["010", "010", "010", "000", "010"],
        ['-'] = ["000", "000", "111", "000", "000"],
        [':'] = ["000", "010", "000", "010", "000"],
        ['/'] = ["001", "001", "010", "100", "100"],
        [','] = ["000", "000", "000", "010", "100"],
        ['.'] = ["000", "000", "000", "000", "010"],
        ['+'] = ["000", "010", "111", "010", "000"],
        ['\''] = ["010", "010", "000", "000", "000"],
        ['_'] = ["000", "000", "000", "000", "111"],
        ['|'] = ["010", "010", "010", "010", "010"],
        ['['] = ["110", "100", "100", "100", "110"],
        [']'] = ["011", "001", "001", "001", "011"],
        ['0'] = ["111", "101", "101", "101", "111"],
        ['1'] = ["010", "110", "010", "010", "111"],
        ['2'] = ["111", "001", "111", "100", "111"],
        ['3'] = ["111", "001", "111", "001", "111"],
        ['4'] = ["101", "101", "111", "001", "001"],
        ['5'] = ["111", "100", "111", "001", "111"],
        ['6'] = ["111", "100", "111", "101", "111"],
        ['7'] = ["111", "001", "001", "001", "001"],
        ['8'] = ["111", "101", "111", "101", "111"],
        ['9'] = ["111", "101", "111", "001", "111"],
        ['A'] = ["111", "101", "111", "101", "101"],
        ['B'] = ["110", "101", "110", "101", "110"],
        ['C'] = ["111", "100", "100", "100", "111"],
        ['D'] = ["110", "101", "101", "101", "110"],
        ['E'] = ["111", "100", "110", "100", "111"],
        ['F'] = ["111", "100", "110", "100", "100"],
        ['G'] = ["111", "100", "101", "101", "111"],
        ['H'] = ["101", "101", "111", "101", "101"],
        ['I'] = ["111", "010", "010", "010", "111"],
        ['K'] = ["101", "101", "110", "101", "101"],
        ['L'] = ["100", "100", "100", "100", "111"],
        ['M'] = ["101", "111", "111", "101", "101"],
        ['N'] = ["101", "111", "111", "111", "101"],
        ['O'] = ["111", "101", "101", "101", "111"],
        ['P'] = ["111", "101", "111", "100", "100"],
        ['R'] = ["110", "101", "110", "101", "101"],
        ['S'] = ["111", "100", "111", "001", "111"],
        ['T'] = ["111", "010", "010", "010", "010"],
        ['U'] = ["101", "101", "101", "101", "111"],
        ['V'] = ["101", "101", "101", "101", "010"],
        ['W'] = ["101", "101", "111", "111", "101"],
        ['X'] = ["101", "101", "010", "101", "101"],
        ['Y'] = ["101", "101", "010", "010", "010"]
    };
}