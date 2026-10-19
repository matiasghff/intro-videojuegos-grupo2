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
                case MatchPhase.Simulating: Engine.Tick(dt); break;
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
            field.ClearActors();
            HUD.HideOverlay();
            CurrentAction = kind == InterventionKind.Dribble
                ? (IInterventionController)new DribbleController(field, Engine, HUD, Resolve)
                : new PassShotController(field, Engine, HUD, kind, Resolve);
        }

        private void Resolve(ActionResult result)
        {
            if (!Engine.Resolve(result)) return;
            pauseTimer = 3.5f;
            var s = Engine.State;
            string body = s.Events[s.Events.Count - 1] + "\nLa simulación continúa en unos segundos.";
            HUD.ShowOverlay(s.Status, body);
        }

        public void Continue()
        {
            if (Engine.State.Phase != MatchPhase.HalfTime && Engine.State.Phase != MatchPhase.Resolution) return;
            CurrentAction?.Dispose();
            CurrentAction = null;
            field.ClearActors();
            Engine.Continue();
            HUD.HideOverlay();
            ShowSimulationActors();
        }
        private void ShowSimulationActors()
        {
            for (int i = 0; i < 5; i++)
            {
                field.Player($"Local {i}", new Vector3(-3.5f + i * 1.7f, 0.6f, -3f + i % 2));
                field.Player($"Visitante {i}", new Vector3(-3.5f + i * 1.7f, 0.6f, 2f + i % 2), true);
            }
            field.Ball(new Vector3(0, 0.25f, 0));
        }
        private void OnDestroy()
        {
            if (Engine != null) Engine.InterventionStarted -= BeginIntervention;
            CurrentAction?.Dispose();
        }
    }
}
