using UnityEngine;

namespace UltimaJugada
{
    public sealed class AffinitySystem
    {
        public float Value { get; private set; } = 50f;
        // A deterministic, visible reception radius, from 0.55 to 1.20 world units.
        public float ReceptionRadius => Mathf.Lerp(0.55f, 1.20f, Value / 100f);
        public void Change(float amount) => Value = Mathf.Clamp(Value + amount, 0f, 100f);
    }
}
