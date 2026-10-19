using UnityEngine;

namespace UltimaJugada
{
    public sealed class StaminaSystem
    {
        public float Value { get; private set; } = 100f;
        public bool Extreme => Value <= 20f;
        public float SpeedMultiplier => Extreme ? 0.62f : Mathf.Lerp(0.78f, 1f, (Value - 20f) / 80f);
        public float PrecisionPenalty => Extreme ? 22f : Mathf.Lerp(10f, 0f, (Value - 20f) / 80f);
        public float PowerMultiplier => Extreme ? 0.82f : Mathf.Lerp(0.92f, 1f, (Value - 20f) / 80f);
        public void Spend(float amount) => Value = Mathf.Clamp(Value - Mathf.Max(0f, amount), 0f, 100f);
        public void Recover(float amount) => Value = Mathf.Clamp(Value + Mathf.Max(0f, amount), 0f, 100f);
        public float EffectivePrecision(float rawScore) => Mathf.Clamp(rawScore - PrecisionPenalty, 0f, 100f);
    }
}
