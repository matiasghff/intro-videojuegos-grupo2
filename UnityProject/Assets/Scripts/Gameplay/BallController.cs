using UnityEngine;

namespace UltimaJugada
{
    /// <summary>Analytical deceleration makes travel distance independent of frame rate.</summary>
    public sealed class BallController
    {
        public Vector3 Position { get; private set; }
        public float Speed { get; private set; }
        public bool Stopped => Speed <= 0.001f;
        private readonly Vector3 direction;
        private readonly float deceleration;
        public BallController(Vector3 start, Vector3 direction, float speed, float deceleration)
        {
            Position = start;
            this.direction = direction.normalized;
            Speed = Mathf.Max(0f, speed);
            this.deceleration = Mathf.Max(0.01f, deceleration);
        }
        public Vector3 Step(float deltaTime)
        {
            float dt = Mathf.Min(Mathf.Max(0f, deltaTime), Speed / deceleration);
            Position += direction * (Speed * dt - 0.5f * deceleration * dt * dt);
            Speed = Mathf.Max(0f, Speed - deceleration * dt);
            return Position;
        }
    }
}
