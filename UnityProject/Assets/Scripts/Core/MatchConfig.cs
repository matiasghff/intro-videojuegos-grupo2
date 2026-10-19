using UnityEngine;

namespace UltimaJugada
{
    [CreateAssetMenu(menuName = "Ultima Jugada/Configuración de partido")]
    public sealed class MatchConfig : ScriptableObject
    {
        public MatchSettings settings = new MatchSettings();
    }

    [System.Serializable]
    public sealed class MatchSettings
    {
        [Min(0.05f)] public float simulatedMinutesPerSecond = 0.5625f;
        [Min(0f)] public float staminaLossPerMinute = 1.05f;
        [Range(0f, 100f)] public float halftimeRecovery = 10f;
        [Min(1f)] public float dribbleTimeLimit = 16f;
        [Min(1f)] public float aimingTimeLimit = 22f;
        [Min(0.2f)] public float precisionTimeLimit = 2f;
        [Min(0.1f)] public float maxDragDistance = 4f;
        [Min(0.1f)] public float minShotSpeed = 4f;
        [Min(1f)] public float maxShotSpeed = 12f;
        [Min(0.1f)] public float ballDeceleration = 4f;
        [Range(0f, 30f)] public float maxPrecisionErrorAngle = 12f;
        [Range(0f, 0.9f)] public float maxPrecisionSpeedLoss = 0.25f;
    }
}
