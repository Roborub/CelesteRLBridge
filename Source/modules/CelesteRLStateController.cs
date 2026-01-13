
using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CelesteRLAgentBridge;

public class CelesteRLStateController
{
    private string _savedLevelStateBytes = null;

    public void Load()
    {
        if (_savedLevelStateBytes == null || Engine.Scene is not Level level)
        {
            return;
        }

        try
        {
            string[] parts = _savedLevelStateBytes.Split('|');
            string levelName = parts[0];
            Vector2 spawnPoint = new Vector2(float.Parse(parts[1]), float.Parse(parts[2]));

            // FIX: Use level.Tracker to find the player
            Player player = level.Tracker.GetEntity<Player>();

            if (player != null)
            {
                level.Session.Level = levelName;
                level.Session.RespawnPoint = spawnPoint;

                // Use the found player instance here
                level.TeleportTo(player, levelName, Player.IntroTypes.Respawn, spawnPoint);
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, "RLBridge", $"Load State Failed: {ex.Message}");
        }
    }



    public void Save()
    {
        if (Engine.Scene is Level level)
        {
            string levelName = level.Session.Level;

            Vector2 spawnPoint = level.Session.LevelData.DefaultSpawn.HasValue
                ? level.Session.LevelData.DefaultSpawn.Value
                : level.Session.LevelData.Spawns[0];

            string stateToSave = $"{levelName}|{spawnPoint.X}|{spawnPoint.Y}";

            _savedLevelStateBytes = stateToSave;
        }
    }
}
