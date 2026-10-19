using UnityEngine;

namespace UltimaJugada
{
    public static class Ballistics
    {
        public static int PrecisionScore(Vector2 localPoint, float radius)
            => radius <= 0 ? 0 : Mathf.RoundToInt(Mathf.Clamp01(1f - localPoint.magnitude / radius) * 100f);

        public static void ApplyPrecision(Vector3 plannedDirection, float plannedSpeed, float score, float sign,
            StaminaSystem stamina, MatchSettings settings, out Vector3 direction, out float speed)
        {
            float effective = stamina.EffectivePrecision(score);
            float error = 1f - effective / 100f;
            float angle = settings.maxPrecisionErrorAngle * error * (sign < 0f ? -1f : 1f);
            direction = Quaternion.Euler(0f, angle, 0f) * plannedDirection.normalized;
            speed = plannedSpeed * (1f - settings.maxPrecisionSpeedLoss * error) * stamina.PowerMultiplier;
        }

        /// <summary>First contact along a segment on XZ, including initial overlap.</summary>
        public static bool SegmentCircle(Vector3 from, Vector3 to, Vector3 center, float radius, out float fraction)
        {
            Vector2 d = new Vector2(to.x - from.x, to.z - from.z);
            Vector2 f = new Vector2(from.x - center.x, from.z - center.z);
            float c = Vector2.Dot(f, f) - radius * radius;
            if (c <= 0f) { fraction = 0f; return true; }
            float a = Vector2.Dot(d, d);
            if (a <= 0.000001f) { fraction = 0f; return false; }
            float b = 2f * Vector2.Dot(f, d);
            float discriminant = b * b - 4f * a * c;
            if (discriminant < 0f) { fraction = 0f; return false; }
            fraction = (-b - Mathf.Sqrt(discriminant)) / (2f * a);
            return fraction >= 0f && fraction <= 1f;
        }

        public static bool GoalCrossing(Vector3 from, Vector3 to, out float fraction)
        {
            fraction = 0f;
            if (from.z >= FieldView.GoalLine || to.z < FieldView.GoalLine) return false;
            fraction = (FieldView.GoalLine - from.z) / (to.z - from.z);
            float x = Mathf.Lerp(from.x, to.x, fraction);
            return Mathf.Abs(x) <= FieldView.GoalHalfWidth - 0.23f;
        }
    }
}
