using UnityEngine;

namespace UltimaJugada
{
    public sealed class MatchSimulationManager : MonoBehaviour
    {
        [SerializeField] private FieldView field;
        public MatchEngine Engine { get; private set; }
        public MatchHUD HUD { get; private set; }
        public IInterventionController CurrentAction { get; private set; }
        private float pauseTimer;
        private MatchPhase previousPhase;
        private bool resultLoaded;
        private Transform[] simulationPlayers;
        private Vector3[] simulationOrigins;
        private Transform simulationBall;
        private float simulationTime;

        private void Start()
        {
            Engine = GameManager.Instance.EnsureMatch();
            if (field == null) field = GetComponentInChildren<FieldView>();
            if (field == null) field = FieldView.Build(transform);
            field.Initialize();
            HUD = gameObject.AddComponent<MatchHUD>();
            HUD.Initialize(Engine, Continue);
            Engine.InterventionStarted += BeginIntervention;
            previousPhase = Engine.State.Phase;
            ShowSimulationActors();
            if (Engine.State.Phase == MatchPhase.Intervention) BeginIntervention(Engine.State.CurrentIntervention);
        }

        private void Update()
        {
            if (Engine == null) return;
            float dt = Time.unscaledDeltaTime;
            switch (Engine.State.Phase)
            {
                case MatchPhase.Simulating:
                    AnimateSimulation(dt * Engine.SimulationSpeed);
                    Engine.Tick(dt);
                    break;
                case MatchPhase.Intervention: CurrentAction?.Tick(dt); break;
                case MatchPhase.HalfTime:
                case MatchPhase.Resolution:
                    pauseTimer -= dt;
                    if (pauseTimer <= 0f) Continue();
                    break;
                case MatchPhase.FullTime:
                    if (!resultLoaded) { resultLoaded = true; SceneFlowController.ShowResult(); }
                    break;
            }
            if (Engine.State.Phase != previousPhase)
            {
                if (Engine.State.Phase == MatchPhase.HalfTime)
                {
                    pauseTimer = 7f;
                    HUD.ShowOverlay("DESCANSO · 45′", $"Resistencia +{Engine.Settings.halftimeRecovery:0}.\nPrepárate para el segundo tiempo.\nContinúa automáticamente en 7 segundos.");
                }
                previousPhase = Engine.State.Phase;
            }
        }

        private void BeginIntervention(InterventionKind kind)
        {
            CurrentAction?.Dispose();
            simulationPlayers = null;
            simulationBall = null;
            field.ClearActors();
            HUD.HideOverlay();
            CurrentAction = kind == InterventionKind.Dribble
                ? (IInterventionController)new DribbleController(field, Engine, HUD, Resolve)
                : new PassShotController(field, Engine, HUD, kind, Resolve);
        }

        private void Resolve(ActionResult result)
        {
            if (!Engine.Resolve(result)) return;
            HUD.SetPower(0f, false);
            if (Engine.PendingFollowUp == FollowUpKind.ChoosePassOrShot)
            {
                pauseTimer = float.PositiveInfinity;
                HUD.ShowFollowUpChoice();
                return;
            }
            bool chained = Engine.PendingFollowUp != FollowUpKind.None;
            pauseTimer = chained ? 0.65f : 2.5f;
            var s = Engine.State;
            string body = s.Events[s.Events.Count - 1] + (chained ? "\n¡El ataque continúa ahora!" : "\nLa simulación continúa en unos segundos.");
            HUD.ShowOverlay(s.Status, body);
        }

        public void Continue()
        {
            if (Engine.State.Phase != MatchPhase.HalfTime && Engine.State.Phase != MatchPhase.Resolution) return;
            if (Engine.PendingFollowUp == FollowUpKind.ChoosePassOrShot) return;
            CurrentAction?.Dispose();
            CurrentAction = null;
            field.ClearActors();
            HUD.HideOverlay();
            Engine.Continue();
            if (Engine.State.Phase == MatchPhase.Simulating) ShowSimulationActors();
        }
        private void ShowSimulationActors()
        {
            simulationPlayers = new Transform[10];
            simulationOrigins = new Vector3[10];
            for (int i = 0; i < 5; i++)
            {
                simulationPlayers[i] = field.Player($"Local {i}", new Vector3(-3.5f + i * 1.7f, 0.6f, -3f + i % 2));
                simulationPlayers[i + 5] = field.Player($"Visitante {i}", new Vector3(-3.5f + i * 1.7f, 0.6f, 2f + i % 2), true);
            }
            for (int i = 0; i < simulationPlayers.Length; i++) simulationOrigins[i] = simulationPlayers[i].position;
            simulationBall = field.Ball(new Vector3(0, 0.25f, 0));
        }
        private void AnimateSimulation(float dt)
        {
            if (simulationPlayers == null || simulationBall == null) return;
            simulationTime += dt;
            for (int i = 0; i < simulationPlayers.Length; i++)
                simulationPlayers[i].position = simulationOrigins[i] + new Vector3(Mathf.Sin(simulationTime * 0.7f + i) * 0.7f, 0f, Mathf.Sin(simulationTime * 0.5f + i * 0.8f) * 0.8f);
            int index = (Mathf.FloorToInt(simulationTime / 1.3f) * 3) % simulationPlayers.Length;
            int nextIndex = (index + 3) % simulationPlayers.Length;
            simulationBall.position = Vector3.Lerp(simulationPlayers[index].position, simulationPlayers[nextIndex].position, Mathf.SmoothStep(0f, 1f, (simulationTime % 1.3f) / 1.3f)) + new Vector3(0f, -0.35f, 0.5f);
        }
        private void OnDestroy()
        {
            if (Engine != null) Engine.InterventionStarted -= BeginIntervention;
            CurrentAction?.Dispose();
        }
    }
}
