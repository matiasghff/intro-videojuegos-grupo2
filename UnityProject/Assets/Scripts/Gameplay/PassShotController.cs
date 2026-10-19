using System;
using UnityEngine;

namespace UltimaJugada
{
    public enum BallActionPhase { Aiming, Dragging, Precision, Flight, Finished }

    public sealed class PassShotController : IInterventionController
    {
        public bool Finished => Phase == BallActionPhase.Finished;
        public BallActionPhase Phase { get; private set; }
        public Transform BallView { get; }
        public Transform ReceiverView { get; }
        public PrecisionController Precision { get; }
        public int RawPrecision { get; private set; }
        public float EffectivePrecision { get; private set; }
        public float Power { get; private set; }
        public LineRenderer AimArrow => aimLine;
        public LineRenderer DragIndicator => dragLine;
        private readonly FieldView field;
        private readonly MatchEngine engine;
        private readonly MatchHUD hud;
        private readonly InterventionKind kind;
        private readonly Action<ActionResult> complete;
        private readonly Transform defender, keeper;
        private readonly LineRenderer dragLine, aimLine;
        private BallController ball;
        private Vector3 plannedDirection;
        private float plannedSpeed, remaining, flightTime;
        private readonly float initialStamina;
        private float autoShotDelay = 0.85f;
        private bool Automated => kind == InterventionKind.TeammateShot;

        public PassShotController(FieldView field, MatchEngine engine, MatchHUD hud, InterventionKind kind, Action<ActionResult> complete)
        {
            this.field = field;
            this.engine = engine;
            this.hud = hud;
            this.kind = kind;
            this.complete = complete;
            initialStamina = engine.State.Stamina.Value;
            remaining = engine.Settings.aimingTimeLimit;
            Phase = BallActionPhase.Aiming;
            float startZ = Automated ? 4.4f : engine.State.IsChainedIntervention ? (kind == InterventionKind.Pass ? 1f : 1.8f) : kind == InterventionKind.Shot ? -2.8f : -5.5f;
            float startX = Automated ? 2.6f : 0f;
            BallView = field.Ball(new Vector3(startX, 0.25f, startZ));
            field.Player(Automated ? "Compañero · rematador" : "Tú · rematador", new Vector3(startX - 0.65f, 0.6f, startZ - 0.7f));
            if (kind == InterventionKind.Pass)
            {
                float receiverZ = engine.State.IsChainedIntervention ? 4.4f : 3.2f;
                ReceiverView = field.Player("Compañero receptor", new Vector3(2.6f, 0.6f, receiverZ));
                field.ReceptionRing(new Vector3(2.6f, 0, receiverZ), engine.State.Affinity.ReceptionRadius);
                defender = field.Player("Rival · intercepta pases", new Vector3(-0.35f, 0.6f, engine.State.IsChainedIntervention ? 2.8f : -0.8f), true);
            }
            else
            {
                defender = field.Player("Rival · bloquea remates", new Vector3(-1.3f, 0.6f, 0.2f), true);
                keeper = field.Player("Portero · rival", new Vector3(0.15f, 0.6f, 6.9f), true);
            }
            dragLine = field.AimLine("Arrastre · hacia atrás", new Color(1f, 0.79f, 0.25f));
            aimLine = field.AimLine("Trayectoria prevista", new Color(0.67f, 0.94f, 0.39f));
            aimLine.positionCount = 5;
            Precision = new PrecisionController(hud.Canvas);
            hud.SetPower(0f, !Automated);
            UpdateHUD();
        }

        public void Tick(float deltaTime)
        {
            if (Finished) return;
            if (Automated && Phase == BallActionPhase.Aiming)
            {
                autoShotDelay -= deltaTime;
                if (autoShotDelay <= 0f)
                {
                    plannedDirection = (new Vector3(engine.TeammateAimX, 0.25f, FieldView.GoalLine) - BallView.position).normalized;
                    plannedSpeed = 10f;
                    Power = 0.75f;
                    ball = new BallController(BallView.position, plannedDirection, plannedSpeed, engine.Settings.ballDeceleration);
                    Phase = BallActionPhase.Flight;
                }
                UpdateHUD();
                return;
            }
            if (Phase == BallActionPhase.Aiming || Phase == BallActionPhase.Dragging)
            {
                remaining -= deltaTime;
                if (remaining <= 0f) { Finish(ActionOutcome.Timeout); return; }
                if (Phase == BallActionPhase.Aiming && GameInput.Clicked && field.FieldCamera.pixelRect.Contains(GameInput.MousePosition))
                {
                    Ray ray = field.FieldCamera.ScreenPointToRay(GameInput.MousePosition);
                    if (Physics.Raycast(ray, out var hit, 100f) && hit.transform == BallView) Phase = BallActionPhase.Dragging;
                }
                if (Phase == BallActionPhase.Dragging)
                {
                    Ray ray = field.FieldCamera.ScreenPointToRay(GameInput.MousePosition);
                    var plane = new Plane(Vector3.up, BallView.position);
                    if (plane.Raycast(ray, out float distance))
                    {
                        Vector3 drag = ray.GetPoint(distance) - BallView.position;
                        drag.y = 0f;
                        drag = Vector3.ClampMagnitude(drag, engine.Settings.maxDragDistance);
                        Power = Mathf.Clamp01(drag.magnitude / engine.Settings.maxDragDistance);
                        plannedDirection = drag.sqrMagnitude > 0.0001f ? -drag.normalized : Vector3.forward;
                        plannedSpeed = Mathf.Lerp(engine.Settings.minShotSpeed, engine.Settings.maxShotSpeed, Power);
                        dragLine.enabled = aimLine.enabled = true;
                        float length = Mathf.Max(0.1f, engine.Settings.maxAimArrowLength) * Power;
                        dragLine.SetPosition(0, BallView.position); dragLine.SetPosition(1, BallView.position - plannedDirection * length);
                        // Compact arrow: power is shown by its bounded length and the HUD meter.
                        aimLine.SetPosition(0, BallView.position);
                        Vector3 tip = BallView.position + plannedDirection * length;
                        Vector3 side = Vector3.Cross(Vector3.up, plannedDirection) * Mathf.Min(0.28f, length * 0.25f);
                        Vector3 back = tip - plannedDirection * Mathf.Min(0.4f, length * 0.3f);
                        aimLine.SetPosition(1, tip);
                        aimLine.SetPosition(2, back + side);
                        aimLine.SetPosition(3, tip);
                        aimLine.SetPosition(4, back - side);
                        hud.SetPower(Power, true);
                    }
                    // Releasing outside the camera plane still commits the last valid drag.
                    if (GameInput.Released)
                    {
                        dragLine.enabled = aimLine.enabled = false;
                        if (Power * engine.Settings.maxDragDistance < 0.12f) Phase = BallActionPhase.Aiming;
                        else { Phase = BallActionPhase.Precision; Precision.Begin(engine.Settings.precisionTimeLimit, engine.Settings.precisionTargetSpeed); }
                    }
                }
            }
            else if (Phase == BallActionPhase.Precision)
            {
                int? score = Precision.Tick(deltaTime);
                if (score.HasValue) Launch(score.Value);
            }
            else if (Phase == BallActionPhase.Flight) AdvanceFlight(deltaTime);
            UpdateHUD();
        }

        private void Launch(int score)
        {
            RawPrecision = score;
            EffectivePrecision = engine.State.Stamina.EffectivePrecision(score);
            Ballistics.ApplyPrecision(plannedDirection, plannedSpeed, score, UnityEngine.Random.value < 0.5f ? -1f : 1f,
                engine.State.Stamina, engine.Settings, out var direction, out float speed);
            ball = new BallController(BallView.position, direction, speed, engine.Settings.ballDeceleration);
            Phase = BallActionPhase.Flight;
        }

        private void AdvanceFlight(float deltaTime)
        {
            // Small steps also bound keeper movement. Swept contacts still handle arbitrarily fast balls.
            float budget = Mathf.Min(deltaTime, 0.25f);
            while (budget > 0 && !Finished)
            {
                float dt = Mathf.Min(budget, 1f / 120f);
                budget -= dt;
                flightTime += dt;
                Vector3 from = ball.Position;
                Vector3 oldKeeper = keeper != null ? keeper.position : Vector3.zero;
                if (keeper != null)
                {
                    Vector3 target = new Vector3(Mathf.Clamp(from.x, -1.65f, 1.65f), 0.6f, 6.9f);
                    keeper.position = Vector3.MoveTowards(keeper.position, target, 0.4f * dt);
                }
                Vector3 to = ball.Step(dt);
                float first = float.PositiveInfinity;
                ActionOutcome? outcome = null;
                ConsiderCircle(from, to, defender.position, 0.68f, ActionOutcome.Intercepted, ref first, ref outcome);
                if (ReceiverView != null) ConsiderCircle(from, to, ReceiverView.position, engine.State.Affinity.ReceptionRadius, ActionOutcome.Success, ref first, ref outcome);
                if (keeper != null && Ballistics.SegmentCircle(from - oldKeeper, to - keeper.position, Vector3.zero, 0.48f, out float keeperT) && keeperT < first)
                { first = keeperT; outcome = ActionOutcome.Saved; }
                if (kind != InterventionKind.Pass && Ballistics.GoalCrossing(from, to, out float goalT) && goalT < first)
                { first = goalT; outcome = ActionOutcome.Success; }
                // Boundary tests also take part in time-of-impact ordering, so an out-of-bounds pass cannot score a later reception.
                ConsiderBoundary(from.x, to.x, FieldView.HalfWidth, ref first, ref outcome);
                ConsiderBoundary(-from.x, -to.x, FieldView.HalfWidth, ref first, ref outcome);
                ConsiderBoundary(from.z, to.z, FieldView.Top, ref first, ref outcome);
                ConsiderBoundary(-from.z, -to.z, -FieldView.Bottom, ref first, ref outcome);
                if (kind != InterventionKind.Pass && from.z < FieldView.GoalLine && to.z >= FieldView.GoalLine && !Ballistics.GoalCrossing(from, to, out _))
                {
                    float missT = (FieldView.GoalLine - from.z) / (to.z - from.z);
                    if (missT < first) { first = missT; outcome = ActionOutcome.Missed; }
                }
                BallView.position = outcome.HasValue ? Vector3.Lerp(from, to, first) : to;
                if (outcome.HasValue) Finish(outcome.Value);
                else if (ball.Stopped) Finish(ActionOutcome.Short);
                else if (flightTime > 8f) Finish(ActionOutcome.Timeout);
            }
        }

        private static void ConsiderCircle(Vector3 from, Vector3 to, Vector3 center, float radius, ActionOutcome hit, ref float first, ref ActionOutcome? outcome)
        {
            if (Ballistics.SegmentCircle(from, to, center, radius, out float t) && t < first) { first = t; outcome = hit; }
        }
        private static void ConsiderBoundary(float from, float to, float limit, ref float first, ref ActionOutcome? outcome)
        {
            if (to <= limit || to <= from) return;
            float t = Mathf.Max(0f, (limit - from) / (to - from));
            if (t < first) { first = t; outcome = ActionOutcome.Missed; }
        }
        private void UpdateHUD()
        {
            if (Automated)
            {
                hud.SetActionInfo("REMATE DEL COMPAÑERO", "Tu pase creó esta ocasión.\nObserva la definición.", "Tu compañero remata al arco. El arquero intentará tapar: el vuelo del balón decide si es gol.");
                return;
            }
            string title = kind == InterventionKind.Pass ? "PASE\nBusca al compañero azul" : "DISPARO\nBusca un lado del arco";
            string status = Phase == BallActionPhase.Flight ? $"Precisión: {RawPrecision}%\nEfectiva: {EffectivePrecision:0}%\nResistencia: {initialStamina:0}/100" : $"Potencia: {Power * 100f:0}%\n{Mathf.CeilToInt(remaining)} s para apuntar";
            string instruction;
            if (Phase == BallActionPhase.Precision) instruction = $"Sigue el objetivo en movimiento y haz clic en su CENTRO. Tienes {engine.Settings.precisionTimeLimit:0.0} segundos.";
            else if (Phase == BallActionPhase.Flight || Finished) instruction = "Balón en juego… La dirección, potencia, precisión y resistencia deciden la jugada.";
            else instruction = "Clic sobre el balón → arrastra hacia ATRÁS → suelta → precisión. Flecha = dirección; barra = potencia.";
            if (kind == InterventionKind.Pass) status += $"\nRecepción: radio {engine.State.Affinity.ReceptionRadius:0.00}";
            hud.SetActionInfo(title, status, instruction);
        }
        private void Finish(ActionOutcome outcome)
        {
            if (Finished) return;
            bool usedPrecision = Phase == BallActionPhase.Flight && !Automated;
            Phase = BallActionPhase.Finished;
            complete(new ActionResult(kind, outcome, usedPrecision ? RawPrecision : -1));
        }
        public void Dispose() => Precision.Dispose();
    }
}
