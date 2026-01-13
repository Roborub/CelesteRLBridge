using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste;

namespace Celeste.Mod.CelesteRLAgentBridge
{
    public class CelesteRLAgentUtil
    {
        public static float Normalize(float value, float max, bool symmetric = false)
        {
            if (max == 0)
            {
                return 0;
            }

            float normalized = value / max;

            return symmetric ? Math.Clamp(normalized, -1.0f, 1.0f) : Math.Clamp(normalized, 0.0f, 1.0f);
        }

        public static string GetTileAt(Level level, Vector2 worldPos)
        {
            if (level.CollideCheck<Solid>(worldPos))
            {
                return "Solid / Wall";
            }

            if (IsTypeInTracker<Spikes>(level) && level.CollideCheck<Spikes>(worldPos))
            {
                return "Spikes";
            }

            if (IsTypeInTracker<Spring>(level) && level.CollideCheck<Spring>(worldPos))
            {
                return "Spring";
            }

            if (IsTypeInTracker<Refill>(level) && level.CollideCheck<Refill>(worldPos))
            {
                return "Refill";
            }

            Entity entity = level.Entities.FirstOrDefault(e =>
                e is not Player &&
                e.Collidable &&
                e.CollidePoint(worldPos));

            if (entity != null)
            {
                return entity.GetType().Name;
            }

            return "Air";
        }

        public static char GetEntityCharFromOhv(int[] bits)
        {
            string ohv = string.Concat(bits).Trim();

            switch (ohv)
            {
                case "10000":
                    return ' ';
                case "01000":
                    return '#';
                case "00100":
                    return '^';
                case "00010":
                    return 'z';
                case "00001":
                    return 'M';
                default:
                    return '?';
            }
        }

        private static bool IsTypeInTracker<T>(Level level) where T : Entity
        {
            if (level.Tracker.Entities.ContainsKey(typeof(T)))
            {
                return true;
            }
            return false;
        }
    }
}
