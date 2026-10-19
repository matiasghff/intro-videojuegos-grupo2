using System;
using UnityEngine;

namespace UltimaJugada
{
    /// <summary>Match rules independent of scenes, input, frame rate and presentation.</summary>
    public sealed class MatchEngine
    {
        public static readonly int[] OpportunityMinutes = { 6, 13, 21, 29, 36, 42, 50, 58, 65, 73, 80, 87 };
        private static readonly InterventionKind[] Kinds =
        {
            InterventionKind.Dribble, InterventionKind.Pass, InterventionKind.Shot,
            InterventionKind.Pass, InterventionKind.Dribble, InterventionKind.Shot,
            InterventionKind.Dribble, InterventionKind.Pass, InterventionKind.Shot,
            InterventionKind.Pass, InterventionKind.Dribble, InterventionKind.Shot
        };
        public GameState State { get; } = new GameState();
        public MatchSettings Settings { get; }
        public float SimulationSpeed { get; private set; } = 1f;
        public FollowUpKind PendingFollowUp { get; private set; }
        public float TeammateAimX { get; private set; }
        public event Action<InterventionKind> InterventionStarted;
        public event Action Changed;
        private readonly MatchEventGenerator events;
        private readonly System.Random attackRandom;
        private int nextOpportunity;
        private float nextEvent = 4f;
        private bool halftimePlayed;

        public MatchEngine(MatchSettings settings, int seed)
        {
            Settings = settings;
            events = new MatchEventGenerator(seed);
            attackRandom = new System.Random(seed ^ 0x4F17);
            Log("¡Comienza el primer tiempo! Juegas para Ultima Jugada.");
        }

        public void Tick(float realSeconds)
        {
            if (State.Phase != MatchPhase.Simulating || realSeconds <= 0f) return;
            float budget = realSeconds * SimulationSpeed * Mathf.Max(0.05f, Settings.simulatedMinutesPerSecond);
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
                    StartIntervention(Kinds[nextOpportunity++], false);
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
            PendingFollowUp = FollowUpKind.None;
            if (result.Kind != InterventionKind.TeammateShot) State.Stats.Interventions++;
            if (result.Precision >= 0 && result.Kind != InterventionKind.TeammateShot)
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
                        PendingFollowUp = FollowUpKind.ChoosePassOrShot;
                        Log("¡Superas a la defensa! Elige dar un pase o rematar ahora. Afinidad +3.");
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
                    if (result.Succeeded && attackRandom.NextDouble() < Mathf.Clamp01(Settings.passFollowUpChance))
                    {
                        PendingFollowUp = attackRandom.NextDouble() < 0.55 ? FollowUpKind.PlayerShot : FollowUpKind.TeammateShot;
                        TeammateAimX = attackRandom.NextDouble() < 0.5 ? 1.5f : -0.5f;
                        Log(PendingFollowUp == FollowUpKind.PlayerShot
                            ? "¡Pared! Tu compañero te devuelve el balón. Remata al arco."
                            : "¡Asistencia! Tu compañero queda frente al arquero y prepara el remate.");
                    }
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
                case InterventionKind.TeammateShot:
                    State.Stats.TeammateShots++;
                    State.Possession = "Deportivo Cordillera";
                    if (result.Succeeded)
                    {
                        State.HomeGoals++;
                        State.Stats.TeammateGoals++;
                        State.Stats.Assists++;
                        State.Affinity.Change(5f);
                        State.Status = "¡GOL DEL COMPAÑERO!";
                        Log($"¡Tu pase termina en gol! Asistencia para ti. {State.HomeGoals}–{State.AwayGoals}.");
                    }
                    else
                    {
                        State.Status = result.Outcome == ActionOutcome.Saved ? "¡EL ARQUERO ATAJA!" : "REMATE DEL COMPAÑERO";
                        Log(result.Outcome == ActionOutcome.Saved ? "El arquero tapa el remate de tu compañero tras tu pase." : "Tu compañero no logra convertir la ocasión.");
                    }
                    break;
            }
            Changed?.Invoke();
            return true;
        }

        public void Continue()
        {
            if (State.Phase != MatchPhase.HalfTime && State.Phase != MatchPhase.Resolution) return;
            if (PendingFollowUp == FollowUpKind.ChoosePassOrShot) return;
            if (PendingFollowUp != FollowUpKind.None)
            {
                var kind = PendingFollowUp == FollowUpKind.PlayerShot ? InterventionKind.Shot : InterventionKind.TeammateShot;
                PendingFollowUp = FollowUpKind.None;
                StartIntervention(kind, true);
                Changed?.Invoke();
                return;
            }
            if (State.Phase == MatchPhase.HalfTime) Log("¡Comienza el segundo tiempo!");
            State.IsChainedIntervention = false;
            State.Phase = MatchPhase.Simulating;
            State.Status = "El partido continúa…";
            Changed?.Invoke();
        }
        public void CycleSimulationSpeed()
        {
            SimulationSpeed = SimulationSpeed == 1f ? 1.5f : SimulationSpeed == 1.5f ? 2f : 1f;
            Changed?.Invoke();
        }

        public bool ChooseFollowUp(InterventionKind kind)
        {
            if (State.Phase != MatchPhase.Resolution || PendingFollowUp != FollowUpKind.ChoosePassOrShot
                || (kind != InterventionKind.Pass && kind != InterventionKind.Shot)) return false;
            PendingFollowUp = FollowUpKind.None;
            StartIntervention(kind, true);
            Changed?.Invoke();
            return true;
        }

        private void StartIntervention(InterventionKind kind, bool chained)
        {
            State.CurrentIntervention = kind;
            State.IsChainedIntervention = chained;
            State.Phase = MatchPhase.Intervention;
            State.Possession = "Ultima Jugada";
            State.Status = kind == InterventionKind.TeammateShot ? "REMATA TU COMPAÑERO" : chained ? "¡SIGUE LA JUGADA!" : "¡TU MOMENTO!";
            Log(chained ? $"La jugada sigue con {KindName(kind)}. ¡El reloj continúa pausado!"
                : $"¡Oportunidad de {KindName(kind)}! El reloj se pausa.");
            InterventionStarted?.Invoke(kind);
        }
        public void Log(string message) => State.Events.Add($"{MatchClock.Format(State.Minute)}  {message}");
        public static string KindName(InterventionKind kind) => kind == InterventionKind.Dribble ? "regate" : kind == InterventionKind.Pass ? "pase" : kind == InterventionKind.TeammateShot ? "remate del compañero" : "disparo";
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
