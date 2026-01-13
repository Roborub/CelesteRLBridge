using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using System.Linq;
using System.Text;
using static Celeste.Mod.CelesteRLAgentBridge.CelesteRLAgentGlobals;

namespace Celeste.Mod.CelesteRLAgentBridge
{
    public class CelesteRLStateEncoder
    {
        private static StringBuilder _observationBuilder = new StringBuilder(16384);
        private static Dictionary<Vector2, int[]> _gridBuffer = new Dictionary<Vector2, int[]>();

        public static string Encode(Player player)
        {
            if (player == null || player.Scene is not Level level)
            {
                return string.Empty;
            }

            _observationBuilder.Clear();
            _gridBuffer.Clear();

            // 1. Core State (Kinematics)
            _observationBuilder.Append(player.X.ToString("F2")).Append(",")
                               .Append(player.Y.ToString("F2")).Append(",")
                               .Append(player.Speed.X.ToString("F2")).Append(",")
                               .Append(player.Speed.Y.ToString("F2")).Append(",")
                               .Append(player.Dashes).Append(",")
                               .Append(player.Stamina.ToString("F2")).Append(",")
                               .Append(player.OnGround() ? "1," : "0,")
                               .Append(player.StateMachine.State == Player.StClimb ? "1," : "0,")
                               .Append(player.Facing == Facings.Right ? "1" : "-1");

            // 2. Grid Observation
            if (GridSize > 0)
            {
                UpdateObservationGrid(player, level);
            }

            return _observationBuilder.ToString() + "\n";
        }

        private static void UpdateObservationGrid(Player player, Level level)
        {
            int half = GridSize / 2;
            int pTileX = (int)Math.Floor(player.X / TileSize);
            int pTileY = (int)Math.Floor(player.Y / TileSize);

            // IMPORTANT: The room's physical limits
            Rectangle bounds = level.Bounds;

            // 1. First, fill the buffer with all dynamic objects (Berries, Platforms, etc.)
            PopulateBuffer(level, player, pTileX, pTileY);

            var solidTiles = level.SolidTiles;
            var tileGrid = solidTiles.Grid;
            int gridOffX = (int)Math.Floor(solidTiles.X / TileSize);
            int gridOffY = (int)Math.Floor(solidTiles.Y / TileSize);

            for (int y = -half; y <= half; y++)
            {
                int gridRow = y + half;
                DebugGrid[gridRow] = "";

                for (int x = -half; x <= half; x++)
                {
                    Vector2 key = new Vector2(x + half, y + half);
                    int worldX = pTileX + x;
                    int worldY = pTileY + y;

                    // Check tile center against level bounds
                    int pixelX = (worldX * TileSize) + (TileSize / 2);
                    int pixelY = (worldY * TileSize) + (TileSize / 2);
                    bool isInsideLevel = bounds.Contains(pixelX, pixelY);

                    int[] cellData;

                    // 2. CHECK PRIORITY:

                    // A. Is there an entity here? (Player, Berry, Spring, Hazard, Platform)
                    if (_gridBuffer.TryGetValue(key, out int[] entityData))
                    {
                        cellData = entityData;
                    }
                    // B. Is this outside the room? (Level Boundary check)
                    else if (!isInsideLevel)
                    {
                        cellData = BoundaryOhv; // Marks the "Void"
                    }
                    // C. Is it a static wall?
                    else
                    {
                        int tx = worldX - gridOffX;
                        int ty = worldY - gridOffY;
                        bool isWall = (tx >= 0 && ty >= 0 && tx < tileGrid.CellsX && ty < tileGrid.CellsY && tileGrid[tx, ty]);
                        cellData = isWall ? SolidOhv : AirOhv;
                    }

                    // 3. ENCODE:
                    _observationBuilder.Append(",");
                    for (int i = 0; i < CategoryCount; i++)
                    {
                        _observationBuilder.Append(cellData[i]);
                        if (i < CategoryCount - 1) { _observationBuilder.Append(","); }
                    }

                    UpdateDebugString(gridRow, cellData);
                }
            }
        }
        private static void PopulateBuffer(Level level, Player player, int pTileX, int pTileY)
        {
            // 1. Hazards (Spikes, Spinners, etc.)
            List<Entity> hazards = level.Entities.Where(e =>
                e is Spikes || e is TriggerSpikes || e is CrystalStaticSpinner ||
                e.GetType().Name.Contains("Spikes") || e.GetType().Name.Contains("Spinner")
            ).ToList();
            AddGroupToBuffer(hazards, pTileX, pTileY, HazardOhv);

            // 2. Interactables (Springs, Refills)
            List<Entity> interactables = level.Entities.Where(e =>
                e is Spring || e is Refill ||
                e.GetType().Name.Contains("Spring") || e.GetType().Name.Contains("Refill")
            ).ToList();
            AddGroupToBuffer(interactables, pTileX, pTileY, MiscEntityOhv);

            // 3. NEW: Platforms (Jumpthrus and Moving Solids like Kevins/CrushBlocks)
            List<Entity> platforms = level.Entities.Where(e =>
                e is JumpThru || e is JumpthruPlatform || (e is Solid && e is not SolidTiles)
            ).ToList();
            AddGroupToBuffer(platforms, pTileX, pTileY, PlatformOhv); // Use your new Platform OHV

            // 4. NEW: Strawberries
            List<Entity> berries = level.Entities.Where(e =>
                e is Strawberry || e.GetType().Name.Contains("Strawberry")
            ).ToList();
            AddGroupToBuffer(berries, pTileX, pTileY, StrawberryOhv); // Use your new Strawberry OHV

            // 5. Player
            AddGroupToBuffer(new List<Entity> { player }, pTileX, pTileY, PlayerOhv);
        }

        private static void AddGroupToBuffer(List<Entity> entities, int pTileX, int pTileY, int[] ohv)
        {
            int half = GridSize / 2;
            foreach (var e in entities)
            {
                // Convert world hitboxes to grid-relative tile indices
                int x1 = (int)Math.Floor(e.Left / TileSize) - pTileX + half;
                int x2 = (int)Math.Floor((e.Right - 1) / TileSize) - pTileX + half;
                int y1 = (int)Math.Floor(e.Top / TileSize) - pTileY + half;
                int y2 = (int)Math.Floor((e.Bottom - 1) / TileSize) - pTileY + half;

                // Fill buffer only for tiles visible in the observation window
                for (int ix = Math.Max(0, x1); ix <= Math.Min(GridSize - 1, x2); ix++)
                {
                    for (int iy = Math.Max(0, y1); iy <= Math.Min(GridSize - 1, y2); iy++)
                    {
                        Vector2 key = new Vector2(ix, iy);
                        // Priority: Player (4) and Hazards (2) take precedence
                        if (!_gridBuffer.ContainsKey(key) || ohv[4] == 1 || ohv[2] == 1)
                        {
                            _gridBuffer[key] = ohv;
                        }
                    }
                }
            }
        }

        private static void UpdateDebugString(int row, int[] ohv)
        {
            if (ohv == PlayerOhv)
            {
                DebugGrid[row] += PlayerAscii;
            }
            else if (ohv == SolidOhv)
            {
                DebugGrid[row] += SolidAscii;
            }
            else if (ohv == AirOhv)
            {
                DebugGrid[row] += AirAscii;
            }
            else if (ohv == PlatformOhv)
            {
                DebugGrid[row] += PlatformAscii;
            }
            else if (ohv == HazardOhv)
            {
                DebugGrid[row] += HazardAscii;
            }
            else if (ohv == StrawberryOhv)
            {
                DebugGrid[row] += StrawberryAscii;
            }
            else if (ohv == BoundaryOhv)
            {
                DebugGrid[row] += BoundaryAscii;
            }
            else if (ohv == MiscEntityOhv)
            {
                DebugGrid[row] += MiscEntityAscii;
            }
        }

        private static List<Entity> SafeGetEntities<T>(Level level) where T : Entity
        {
            if (level.Tracker.Entities.ContainsKey(typeof(T)))
            {
                return level.Tracker.GetEntities<T>();
            }
            return new List<Entity>();
        }
    }
}
