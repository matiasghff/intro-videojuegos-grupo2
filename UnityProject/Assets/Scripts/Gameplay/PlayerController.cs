using UnityEngine;

namespace UltimaJugada
{
    public sealed class PlayerController
    {
        public const float BaseSpeed = 5f;
        public Vector3 Position { get; private set; }
        public Vector3 Direction { get; private set; }
        public PlayerController(Vector3 start) => Position = start;
        public void Tick(Vector2 input, float deltaTime, StaminaSystem stamina)
        {
            input = Vector2.ClampMagnitude(input, 1f);
            Direction = new Vector3(input.x, 0, input.y);
            Position += Direction * (BaseSpeed * stamina.SpeedMultiplier * Mathf.Max(0f, deltaTime));
            Position = new Vector3(Mathf.Clamp(Position.x, -5.1f, 5.1f), Position.y, Mathf.Clamp(Position.z, -8.4f, 8f));
            stamina.Spend(input.magnitude * Mathf.Max(0f, deltaTime) * 0.9f);
        }
    }
}
