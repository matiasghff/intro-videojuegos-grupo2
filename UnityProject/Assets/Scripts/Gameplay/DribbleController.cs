using System;
using UnityEngine;

namespace UltimaJugada
{
    public sealed class DribbleController : IInterventionController
    {
        public bool Finished { get; private set; }
        public PlayerController Player { get; }
        private readonly DefenderAI[] defenders;
        private readonly Transform playerView, ballView;
        private readonly Transform[] defenderViews;
        private readonly MatchEngine engine;
        private readonly MatchHUD hud;
        private readonly Action<ActionResult> complete;
        private float remaining;

        public DribbleController(FieldView field, MatchEngine match, MatchHUD hud, Action<ActionResult> complete)
        {
            engine = match;
            this.hud = hud;
            this.complete = complete;
            hud.SetPower(0f, false);
            remaining = match.Settings.dribbleTimeLimit;
            Player = new PlayerController(new Vector3(0, 0.6f, -7));
            playerView = field.Player("Tú · dorsal 10", Player.Position);
            ballView = field.Ball(new Vector3(0, 0.25f, -6.45f));
            field.FinishLine(7f);
            defenders = new[]
            {
                new DefenderAI(new Vector3(0, 0.6f, -1), DefenderRole.Pressure),
                new DefenderAI(new Vector3(-3.5f, 0.6f, 1.5f), DefenderRole.Anticipation),
                new DefenderAI(new Vector3(3.5f, 0.6f, 1.5f), DefenderRole.Cover)
            };
            defenderViews = new Transform[3];
            for (int i = 0; i < 3; i++) defenderViews[i] = field.Player($"Defensor · {defenders[i].Role}", defenders[i].Position, true);
            UpdateHUD();
        }

        public void Tick(float deltaTime) => TickInput(GameInput.Move, deltaTime);
        public void TickInput(Vector2 input, float deltaTime)
        {
            if (Finished) return;
            // Substeps keep defenders and contact stable even during a slow frame.
            float budget = Mathf.Min(deltaTime, 0.25f);
            while (budget > 0f && !Finished)
            {
                float dt = Mathf.Min(budget, 1f / 60f);
                budget -= dt;
                remaining -= dt;
                Vector3 oldPlayer = Player.Position;
                Player.Tick(input, dt, engine.State.Stamina);
                float earliestContact = float.PositiveInfinity;
                for (int i = 0; i < defenders.Length; i++)
                {
                    Vector3 oldDefender = defenders[i].Position;
                    defenders[i].Tick(Player.Position, Player.Direction, dt);
                    defenderViews[i].position = defenders[i].Position;
                    if (Ballistics.SegmentCircle(oldPlayer - oldDefender, Player.Position - defenders[i].Position, Vector3.zero, 0.75f, out float t))
                        earliestContact = Mathf.Min(earliestContact, t);
                }
                playerView.position = Player.Position;
                Vector3 dir = Player.Direction.sqrMagnitude > 0f ? Player.Direction : Vector3.forward;
                ballView.position = Player.Position + dir * 0.5f + Vector3.down * 0.35f;
                float finishT = Player.Position.z >= 7f && oldPlayer.z < 7f ? (7f - oldPlayer.z) / (Player.Position.z - oldPlayer.z) : float.PositiveInfinity;
                if (earliestContact <= finishT && !float.IsPositiveInfinity(earliestContact)) Finish(ActionOutcome.Intercepted);
                else if (!float.IsPositiveInfinity(finishT)) Finish(ActionOutcome.Success);
                else if (remaining <= 0f) Finish(ActionOutcome.Timeout);
            }
            UpdateHUD();
            hud.Refresh();
        }
        private void UpdateHUD() => hud.SetActionInfo("REGATE\nCruza la línea dorada", $"{Mathf.CeilToInt(remaining)} s restantes\nVelocidad: {PlayerController.BaseSpeed * engine.State.Stamina.SpeedMultiplier:0.0}\n3 defensores · roles fijos", "WASD / FLECHAS   ·   Esquiva a los rivales rojos y alcanza la línea dorada al fondo.");
        private void Finish(ActionOutcome outcome)
        {
            Finished = true;
            complete(new ActionResult(InterventionKind.Dribble, outcome));
        }
        public void Dispose() { }
    }
}
