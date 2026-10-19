using System.Collections.Generic;

namespace UltimaJugada
{
    public enum MatchPhase { Simulating, HalfTime, Intervention, Resolution, FullTime }
    public enum InterventionKind { Dribble, Pass, Shot, TeammateShot }
    public enum FollowUpKind { None, ChoosePassOrShot, PlayerShot, TeammateShot }
    public enum ActionOutcome { Success, Intercepted, Missed, Short, Saved, Timeout }

    public readonly struct ActionResult
    {
        public readonly InterventionKind Kind;
        public readonly ActionOutcome Outcome;
        public readonly int Precision;
        public bool Succeeded => Outcome == ActionOutcome.Success;
        public ActionResult(InterventionKind kind, ActionOutcome outcome, int precision = -1)
        { Kind = kind; Outcome = outcome; Precision = precision; }
    }

    public sealed class GameState
    {
        public float Minute { get; internal set; }
        public int HomeGoals { get; internal set; }
        public int AwayGoals { get; internal set; }
        public MatchPhase Phase { get; internal set; } = MatchPhase.Simulating;
        public InterventionKind CurrentIntervention { get; internal set; }
        public bool IsChainedIntervention { get; internal set; }
        public string Possession { get; internal set; } = "Ultima Jugada";
        public string Status { get; internal set; } = "El partido continúa…";
        public readonly StaminaSystem Stamina = new StaminaSystem();
        public readonly AffinitySystem Affinity = new AffinitySystem();
        public readonly MatchStats Stats = new MatchStats();
        public readonly List<string> Events = new List<string>();
        public string Verdict => HomeGoals > AwayGoals ? "VICTORIA" : HomeGoals < AwayGoals ? "DERROTA" : "EMPATE";
    }
}
