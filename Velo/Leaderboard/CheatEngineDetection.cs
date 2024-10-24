using CEngine.World.Actor;
using Microsoft.Xna.Framework;

namespace Velo
{
    public class CheatEngineDetection
    {
        public struct VecField
        {
            public Vector2 Value;

            public void Update(Vector2 prevValue, Vector2 newValue, CActor actor)
            {
                if (Velo.MainPlayer == null || actor != Velo.MainPlayer.actor)
                    return;
                if (Value != prevValue)
                    Detected = true;
                Value = newValue;
            }
        }

        public struct FloatField
        {
            public float Value;

            public void Update(float prevValue, float newValue, CActor actor)
            {
                if (Velo.MainPlayer == null || actor != Velo.MainPlayer.actor)
                    return;
                if (Value != prevValue)
                    Detected = true;
                Value = newValue;
            }
        }

        public static VecField Position;
        public static VecField Velocity;
        public static FloatField Boost;

        public static bool Detected = false;

        public static void MatchValues()
        {
            Position.Value = Velo.MainPlayer.actor.Position;
            Velocity.Value = Velo.MainPlayer.actor.Velocity;
            Boost.Value = Velo.MainPlayer.boost;
        }
    }
}
