using UnityEngine;

namespace UltimaJugada
{
    public enum DefenderRole { Pressure, Anticipation, Cover }
    public sealed class DefenderAI
    {
        public const float Speed = 2.1f;
        public Vector3 Position { get; private set; }
        public DefenderRole Role { get; }
        public DefenderAI(Vector3 start, DefenderRole role) { Position = start; Role = role; }
        public void Tick(Vector3 player, Vector3 moveDirection, float deltaTime)
        {
            Vector3 target = player;
            if (Role == DefenderRole.Anticipation) target += moveDirection * 2.2f;
            if (Role == DefenderRole.Cover) target = new Vector3(player.x, player.y, Mathf.Min(7.4f, player.z + 2.5f));
            target.x = Mathf.Clamp(target.x, -5.1f, 5.1f);
            target.z = Mathf.Clamp(target.z, -8.4f, 8f);
            Position = Vector3.MoveTowards(Position, target, Speed * Mathf.Max(0f, deltaTime));
        }
    }
}
