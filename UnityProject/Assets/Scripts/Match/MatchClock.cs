using UnityEngine;

namespace UltimaJugada
{
    public static class MatchClock
    {
        public static float Advance(float minute, float realSeconds, float rate, float boundary)
            => Mathf.Min(boundary, minute + Mathf.Max(0f, realSeconds) * Mathf.Max(0.05f, rate));
        public static string Format(float minute) => $"{Mathf.FloorToInt(Mathf.Clamp(minute, 0f, 90f)):00}′";
    }
}
