using System;
using UnityEngine;

namespace UltimaJugada
{
    /// <summary>Match rules independent of scenes, input, frame rate and presentation.</summary>
    public sealed class MatchEngine
    {
        public static readonly int[] OpportunityMinutes = { 12, 26, 39, 57, 72, 84 };
        private static readonly InterventionKind[] Kinds =
        {
            InterventionKind.Dribble, InterventionKind.Pass, InterventionKind.Shot,
            InterventionKind.Pass, InterventionKind.Dribble, InterventionKind.Shot
        };
        public GameState State { get; } = new GameState();
        public MatchSettings Settings { get; }
        public event Action<InterventionKind> InterventionStarted;
        public event Action Changed;
        private readonly MatchEventGenerator events;
        private int nextOpportunity;
        private float nextEvent = 4f;
        private bool halftimePlayed;

        public MatchEngine(MatchSettings settings, int seed)
        {
            Settings = settings;
            events = new MatchEventGenerator(seed);
            Log("¡Comienza el primer tiempo! Juegas para Ultima Jugada.");
        }

        public void Tick(float realSeconds)
        {
            if (State.Phase != MatchPhase.Simulating || realSeconds <= 0f) return;
            float budget = realSeconds * Mathf.Max(0.05f, Settings.simulatedMinutesPerSecond);
            while (budget > 0f && State.Phase == MatchPhase.Simulating)
            {
                float opportunity = nextOpportunity < OpportunityMinutes.Length ? OpportunityMinutes[nextOpportunity] : 90f;
                float boundary = Mathf.Min(opportunity, nextEvent, halftimePlayed ? 90f : 45f, 90f);
                float delta = Mathf.Min(budget, Mathf.Max(0f, boundary - State.Minute));
                State.Minute += delta;
                State.Stamina.Spend(delta * Settings.staminaLossPerMinute);
                budget -= delta;
                if (State.Minute < boundary) break;
                if (State.Minute >= 90f)
                {
                    State.Minute = 90f;
                    State.Phase = MatchPhase.FullTime;
                    State.Status = "FINAL DEL PARTIDO";
                    Log($"¡Pitazo final! {State.HomeGoals}–{State.AwayGoals}. {State.Verdict}.");
                }
                else if (!halftimePlayed && State.Minute >= 45f)
                {
                    halftimePlayed = true;
                    State.Phase = MatchPhase.HalfTime;
                    State.Stamina.Recover(Settings.halftimeRecovery);
                    State.Status = "DESCANSO";
                    Log($"FINAL DEL PRIMER TIEMPO. Resistencia +{Settings.halftimeRecovery:0}.");
                }
                else if (nextOpportunity < OpportunityMinutes.Length && State.Minute >= opportunity)
                {
                    State.CurrentIntervention = Kinds[nextOpportunity++];
                    State.Phase = MatchPhase.Intervention;
                    State.Possession = "Ultima Jugada";
                    State.Status = "¡TU MOMENTO!";
                    Log($"¡Oportunidad de {KindName(State.CurrentIntervention)}! El reloj se pausa.");
                    InterventionStarted?.Invoke(State.CurrentIntervention);
                }
                if (State.Minute >= nextEvent && State.Phase == MatchPhase.Simulating)
                {
                    Log(events.Generate(State));
                    nextEvent += events.NextInterval();
                }
            }
            Changed?.Invoke();
        }

        public bool Resolve(ActionResult result)
        {
            if (State.Phase != MatchPhase.Intervention || State.CurrentIntervention != result.Kind) return false;
            State.Phase = MatchPhase.Resolution; // Guard before callbacks: exactly once.
            State.Stats.Interventions++;
            if (result.Precision >= 0)
            {
                State.Stats.PrecisionAttempts++;
                State.Stats.PrecisionTotal += Mathf.Clamp(result.Precision, 0, 100);
            }
            if (result.Outcome == ActionOutcome.Intercepted) State.Stats.Interceptions++;
            State.Possession = result.Succeeded ? "Ultima Jugada" : "Deportivo Cordillera";
            switch (result.Kind)
            {
                case InterventionKind.Dribble:
                    State.Stats.DribbleAttempts++;
                    State.Stamina.Spend(4f);
                    if (result.Succeeded)
                    {
                        State.Stats.SuccessfulDribbles++;
                        State.Affinity.Change(3f);
                        State.Status = "¡REGATE EXITOSO!";
                        Log("Superas a la defensa y generas una ocasión ofensiva. Afinidad +3.");
                    }
                    else
                    {
                        State.Status = result.Outcome == ActionOutcome.Intercepted ? "REGATE INTERCEPTADO" : "SE ESCAPÓ LA OCASIÓN";
                        Log(result.Outcome == ActionOutcome.Intercepted ? "El defensor te alcanza e intercepta el balón." : "Se agotó el tiempo para superar la defensa.");
                    }
                    break;
                case InterventionKind.Pass:
                    if (result.Precision >= 0 || result.Outcome != ActionOutcome.Timeout) State.Stats.PassAttempts++;
                    State.Stamina.Spend(3f);
                    State.Affinity.Change(result.Succeeded ? 8f : -4f);
                    if (result.Succeeded) State.Stats.CompletedPasses++;
                    State.Status = result.Succeeded ? "¡PASE COMPLETADO!" : "PASE FALLIDO";
                    Log(result.Succeeded ? "Buena combinación con tu compañero. Afinidad +8." : $"{FailureText(result.Outcome)} Afinidad −4.");
                    break;
                case InterventionKind.Shot:
                    if (result.Precision >= 0 || result.Outcome != ActionOutcome.Timeout) State.Stats.Shots++;
                    State.Stamina.Spend(5f);
                    if (result.Succeeded)
                    {
                        State.HomeGoals++;
                        State.Stats.PlayerGoals++;
                        State.Affinity.Change(4f);
                        State.Possession = "Deportivo Cordillera";
                        State.Status = "¡GOOOOL!";
                        Log($"¡Tu remate entra! Ultima Jugada {State.HomeGoals}–{State.AwayGoals}. Afinidad +4.");
                    }
                    else
                    {
                        State.Affinity.Change(-2f);
                        State.Status = result.Outcome == ActionOutcome.Saved ? "¡ATAJADA!" : "OCASIÓN PERDIDA";
                        Log($"{FailureText(result.Outcome)} El remate no termina en gol. Afinidad −2.");
                    }
                    break;
            }
            Changed?.Invoke();
            return true;
        }

        public void Continue()
        {
            if (State.Phase != MatchPhase.HalfTime && State.Phase != MatchPhase.Resolution) return;
            if (State.Phase == MatchPhase.HalfTime) Log("¡Comienza el segundo tiempo!");
            State.Phase = MatchPhase.Simulating;
            State.Status = "El partido continúa…";
            Changed?.Invoke();
        }
        public void Log(string message) => State.Events.Add($"{MatchClock.Format(State.Minute)}  {message}");
        public static string KindName(InterventionKind kind) => kind == InterventionKind.Dribble ? "regate" : kind == InterventionKind.Pass ? "pase" : "disparo";
        private static string FailureText(ActionOutcome outcome)
        {
            switch (outcome)
            {
                case ActionOutcome.Intercepted: return "El rival intercepta el balón.";
                case ActionOutcome.Short: return "El balón se queda corto.";
                case ActionOutcome.Saved: return "El portero detiene el remate.";
                case ActionOutcome.Timeout: return "Se agota el tiempo de la jugada.";
                default: return "El balón sale desviado del campo.";
            }
        }
    }
}
