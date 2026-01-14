
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste;
using System.Reflection;
using static Celeste.Mod.CelesteRLAgentBridge.CelesteRLAgentGlobals;
using Microsoft.Xna.Framework.Graphics;

namespace Celeste.Mod.CelesteRLAgentBridge;

public class CelesteRLAgentBridgeModule : EverestModule
{
    #region ModBoilerplate
    public static CelesteRLAgentBridgeModule Instance { get; private set; }

    public CelesteRLAgentBridgeModule()
    {
        Instance = this;
    }
    #endregion

    public CelesteRLNetworkServer Server;
    public CelesteRLStateController StateController;
    private static bool isWarping = false;

    private List<string> debugLines = new List<string>();
    private List<string> gridDebugLines = new List<string>();
    private static readonly Color BackgroundColour = Color.Black * 0.6f;

    public override void Load()
    {

        isWarping = false;

        int port = 5000;
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--port" && i + 1 < args.Length)
            {
                int.TryParse(args[i + 1], out port);
            }
        }

        Logger.Log(LogLevel.Info, "CelesteRL", $"Mod Loading... ({IPAddress.Loopback}:{port})");

        Server = new CelesteRLNetworkServer();
        StateController = new CelesteRLStateController();
        Server.Start(port);

        On.Monocle.Engine.Update += OnEngineUpdate;
        On.Celeste.Player.Update += OnPlayerUpdate;
        On.Celeste.Player.Die += OnPlayerDie;
        //On.Celeste.Level.LoadLevel += OnLoadLevel;
        On.Celeste.Level.Render += OnLevelRender;
    }

    public override void Unload()
    {
        isWarping = false;

        Server?.ShutDown();
        On.Monocle.Engine.Update -= OnEngineUpdate;
        On.Celeste.Player.Update -= OnPlayerUpdate;
        On.Celeste.Player.Die -= OnPlayerDie;
        //On.Celeste.Level.LoadLevel -= OnLoadLevel;
        On.Celeste.Level.Render -= OnLevelRender;
    }

    private static void OnLevelRender(On.Celeste.Level.orig_Render orig, Level self)
    {
        orig(self);

        if (Engine.Scene is not Level level || level.Paused)
        {
            return;
        }

        Vector2 mouseWorldPos = Vector2.Transform(MInput.Mouse.Position, Matrix.Invert(level.Camera.Matrix));
        string blockAtCursor = CelesteRLAgentUtil.GetTileAt(level, mouseWorldPos);

        Instance.debugLines.Clear();
        Instance.debugLines.Add("=== CELESTE RL BRIDGE ===");
        Instance.debugLines.Add($"Connected: {Instance.Server?.IsConnected}");
        Instance.debugLines.Add($"Cursor World: {(int)mouseWorldPos.X}, {(int)mouseWorldPos.Y}");
        Instance.debugLines.Add($"Block at cursor: {blockAtCursor}");

        if (level.Tracker.GetEntity<Player>() is Player player)
        {
            Instance.debugLines.Add($"Player Pos: {(int)player.X}, {(int)player.Y}");
            Instance.debugLines.Add($"Player State: {player.StateMachine.State}");
        }

        Instance.gridDebugLines.Clear();
        Instance.gridDebugLines.Add("--- WORLD GRID ---");
        for (int i = 0; i < DebugGrid.GetLength(0); i++)
        {
            // Ensure the row isn't null so the grid doesn't flicker
            string row = DebugGrid[i] ?? new string('.', GridSize);
            Instance.gridDebugLines.Add(row);
        }
        Instance.gridDebugLines.Add("------------------");

        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Engine.ScreenMatrix);

        // Settings for Draw.DefaultFont (Fixed Width)
        float lineHeight = 20.0f; // DefaultFont is smaller than ActiveFont
        float boxPadding = 50.0f;
        float fontScale = 2.0f; // Scale up the default font for readability
        Color textColor = Color.White;
        Color gridColor = Color.Cyan;

        // Render Left Panel (Stats)
        float leftWidth = 400.0f;
        float leftHeight = Instance.debugLines.Count * (lineHeight * fontScale) + 20.0f;
        Draw.Rect(boxPadding, boxPadding, leftWidth, leftHeight, BackgroundColour);

        for (int i = 0; i < Instance.debugLines.Count; i++)
        {
            Vector2 pos = new Vector2(20, 20 + (i * lineHeight * fontScale));
            Draw.SpriteBatch.DrawString(Draw.DefaultFont, Instance.debugLines[i], pos, textColor, 0.0f, Vector2.Zero, fontScale, SpriteEffects.None, 0.0f);
        }

        // Render Right Panel (The Grid)
        // Using a fixed-width font here ensures all '.' and '#' align perfectly in columns
        float rightWidth = 500.0f;
        float rightHeight = Instance.gridDebugLines.Count * (lineHeight * fontScale) + 20.0f;
        float rightX = Engine.Width - rightWidth - boxPadding;

        Draw.Rect(rightX, boxPadding, rightWidth, rightHeight, BackgroundColour);

        for (int i = 0; i < Instance.gridDebugLines.Count; i++)
        {
            Vector2 pos = new Vector2(rightX + 10, 20 + (i * lineHeight * fontScale));
            Draw.SpriteBatch.DrawString(Draw.DefaultFont, Instance.gridDebugLines[i], pos, gridColor, 0.0f, Vector2.Zero, fontScale, SpriteEffects.None, 0.0f);
        }

        Draw.SpriteBatch.End();
    }

    private static void OnEngineUpdate(On.Monocle.Engine.orig_Update orig, Engine self, GameTime time)
    {
        orig(self, time);

        if (Instance.Server == null || !Instance.Server.IsConnected)
        {
            return;
        }

        string cmd = Instance.Server.Receive();

        int.TryParse(cmd, out int cmdResetSignal);

        if (cmdResetSignal == -1)
        {
            return;
        }

        if (!string.IsNullOrEmpty(cmd) && cmd.Contains("LOAD_LEVEL,"))
        {
            Logger.Log(LogLevel.Info, "CelesteRL", "Received LOAD_LEVEL!");
            if (self.scene is Level level)
            {
                string levelId = cmd.Replace("LOAD_LEVEL,", "").Trim();
                
                Entity coroutineHost = new Entity();
                coroutineHost.Add(new Coroutine(WarpRoutine(levelId)));
                Engine.Scene.Add(coroutineHost);
            }
        }

        if (!string.IsNullOrEmpty(cmd) && cmd.Contains("START_LEVEL"))
        {
            Logger.Log(LogLevel.Info, "CelesteRL", $"GOT COMMAND {cmd}");
            if (!isWarping && Engine.Scene.GetType().Name != "Level")
            {
                Entity coroutineHost = new Entity();
                coroutineHost.Add(new Coroutine(WarpRoutine()));
                Engine.Scene.Add(coroutineHost);
            }
        }
    }

    private static IEnumerator WarpRoutine(string levelId = null)
    {
        isWarping = true;

        Logger.Log(LogLevel.Info, "CelesteRL", "WarpRoutine: Starting...");

        while (Engine.Scene == null || AreaData.Areas == null || AreaData.Areas.Count == 0)
        {
            yield return null;
        }

        Logger.Log(LogLevel.Info, "CelesteRL", "WarpRoutine: Game Loaded!...");

        if (global::Celeste.SaveData.Instance == null)
        {
            global::Celeste.SaveData.InitializeDebugMode();
            yield return null;
        }

        Logger.Log(LogLevel.Info, "CelesteRL", "WarpRoutine: Save Data confirmed...");

        try
        {
            AreaKey area = new AreaKey(1, AreaMode.Normal);
            Session session = new Session(area);
            
            MapData mapData = AreaData.Get(area).Mode[0].MapData;

            if (string.IsNullOrEmpty(levelId) || mapData.Get(levelId) == null)
            {
              levelId = mapData.StartLevel().Name;
              Logger.Log(LogLevel.Info, "CelesteRL", $"Level ID = {levelId}");
            }
            
            session.Level = levelId;
            session.FirstLevel = false;
            session.InArea = true;
            session.StartedFromBeginning = false;


            Logger.Log(LogLevel.Info, "CelesteRL", "Warping now...");
            global::Celeste.LevelEnter.Go(session, fromSaveData: false);
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, "CelesteRL", "Warp Error: " + ex.ToString());
        }

        yield return 2.0f;
        isWarping = false;
    }

    // private static void OnLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader)
    // {
    //     if (Instance.Server.IsConnected) playerIntro = Player.IntroTypes.None;
    //     orig(self, playerIntro, isFromLoader);
    //     Instance.StateController.Save();
    // }

    private static void OnPlayerUpdate(On.Celeste.Player.orig_Update orig, Player self)
    {
        if (Instance.Server == null || !Instance.Server.IsConnected)
        {
            orig(self);
            return;
        }

        Instance.Server.Send(CelesteRLStateEncoder.Encode(self));

        string response = Instance.Server.Receive();

        if (response == "GET_SPAWNS")
        {

            Level level = self.SceneAs<Level>();

            if (level != null)
            {
                var allSpawns = level.Session.MapData.Levels
                    .SelectMany(room => room.Spawns)
                    .Select(s => $"{s.X},{s.Y}")
                    .ToList();

                string spawnData = $"SPAWNS:{string.Join(";", allSpawns)}";
                Instance.Server.Send(spawnData);

                Logger.Log(LogLevel.Info, "CelesteRL", $"Sent {allSpawns.Count} spawns from the whole map.");
            }

            response = Instance.Server.Receive();
        }

        if (string.IsNullOrEmpty(response))
        {
            return;
        }

        int[] actions = ParseActions(response);
        CelesteRLInputController.Apply(actions);

        orig(self);
    }

    private static int[] ParseActions(string response)
    {
        try
        {
            Logger.Log(LogLevel.Info, "CelesteRL", $"Received action: {response}");

            if (response.Contains("START_LEVEL") || response.Contains("GET_SPAWNS"))
            {
                return null;
            }

            string clean = response.Replace("[", "").Replace("]", "").Replace(" ", "").Trim();
            string[] parts = clean.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            int[] actions = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                actions[i] = int.Parse(parts[i], System.Globalization.CultureInfo.InvariantCulture);
            }
            return actions;
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Warn, "CelesteRL", $"Action Parse Error on string '{response}': {ex.Message}");
            return new int[] { 1, 1, 0, 0, 0 };
        }
    }

    private static PlayerDeadBody OnPlayerDie(On.Celeste.Player.orig_Die orig, Player self, Vector2 direction, bool evenIfInvincible, bool registerDeathInStats)
    {
        if (Instance.Server.IsConnected) Instance.Server.Send("DEAD\n");
        return orig(self, direction, evenIfInvincible, registerDeathInStats);
    }

    public override void Initialize()
    {
        base.Initialize();
        CelesteRLInputController.Setup();
        inputInitialized = true;
    }
}

