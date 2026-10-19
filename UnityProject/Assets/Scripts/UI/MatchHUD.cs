using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace UltimaJugada
{
    public sealed class MatchHUD : MonoBehaviour
    {
        public Canvas Canvas { get; private set; }
        public Text Instructions { get; private set; }
        private Text score, minute, phase, stamina, affinity, commentary, warning, detail, next;
        private Image staminaBar, affinityBar;
        private RectTransform overlay;
        private Text overlayTitle, overlayBody;
        private Button continueButton;
        private Button speedButton;
        private RectTransform choice;
        private Image powerBar;
        private Text powerLabel;
        private MatchEngine engine;
        private int lastEventCount = -1;

        public void Initialize(MatchEngine match, System.Action onContinue)
        {
            engine = match;
            Canvas = UIFactory.Canvas(transform);
            var root = Canvas.transform;
            // A full backdrop is behind the viewport only if a second camera clears it; use edge panels.
            var header = UIFactory.Card(root, "Marcador", new Vector2(0, 0.86f), Vector2.one);
            UIFactory.Label(header.transform, "ULTIMA JUGADA", new Vector2(0.025f, 0.50f), new Vector2(0.30f, 0.92f), 33, UIFactory.Accent);
            UIFactory.Label(header.transform, "LOCAL  /  ULTIMA JUGADA     ·     VISITA  /  CORDILLERA", new Vector2(0.025f, 0.10f), new Vector2(0.55f, 0.47f), 22, UIFactory.Muted);
            score = UIFactory.Label(header.transform, "0 – 0", new Vector2(0.53f, 0.12f), new Vector2(0.73f, 0.90f), 62, null, TextAnchor.MiddleCenter);
            speedButton = UIFactory.Button(header.transform, "x1", new Vector2(0.75f, 0.25f), new Vector2(0.855f, 0.74f), engine.CycleSimulationSpeed, false);
            UIFactory.Label(header.transform, "SIMULACIÓN", new Vector2(0.75f, 0.05f), new Vector2(0.855f, 0.23f), 15, UIFactory.Muted, TextAnchor.MiddleCenter);
            minute = UIFactory.Label(header.transform, "00′", new Vector2(0.87f, 0.16f), new Vector2(0.975f, 0.94f), 62, UIFactory.Accent, TextAnchor.MiddleRight);

            var sidebar = UIFactory.Card(root, "Partido y jugador", new Vector2(0.73f, 0.15f), new Vector2(0.985f, 0.84f));
            phase = UIFactory.Label(sidebar.transform, "EN JUEGO", new Vector2(0.07f, 0.91f), new Vector2(0.94f, 0.99f), 30, UIFactory.Accent);
            stamina = UIFactory.Label(sidebar.transform, "", new Vector2(0.07f, 0.82f), new Vector2(0.94f, 0.90f), 26);
            staminaBar = UIFactory.Meter(sidebar.transform, "Resistencia", new Vector2(0.07f, 0.79f), new Vector2(0.93f, 0.81f), UIFactory.Accent);
            affinity = UIFactory.Label(sidebar.transform, "", new Vector2(0.07f, 0.70f), new Vector2(0.94f, 0.78f), 26);
            affinityBar = UIFactory.Meter(sidebar.transform, "Afinidad", new Vector2(0.07f, 0.67f), new Vector2(0.93f, 0.69f), new Color(0.30f, 0.65f, 1f));
            warning = UIFactory.Label(sidebar.transform, "", new Vector2(0.07f, 0.55f), new Vector2(0.94f, 0.65f), 23, UIFactory.Warning);
            UIFactory.Label(sidebar.transform, "RELATO DEL PARTIDO", new Vector2(0.07f, 0.48f), new Vector2(0.94f, 0.54f), 23, UIFactory.Muted);
            commentary = UIFactory.Label(sidebar.transform, "", new Vector2(0.07f, 0.035f), new Vector2(0.94f, 0.47f), 22, null, TextAnchor.UpperLeft);
            detail = UIFactory.Label(root, "", new Vector2(0.035f, 0.75f), new Vector2(0.28f, 0.83f), 25, UIFactory.Accent, TextAnchor.UpperLeft);
            next = UIFactory.Label(root, "", new Vector2(0.035f, 0.26f), new Vector2(0.23f, 0.44f), 23, UIFactory.Muted, TextAnchor.LowerLeft);
            powerLabel = UIFactory.Label(root, "POTENCIA", new Vector2(0.035f, 0.205f), new Vector2(0.20f, 0.25f), 21, UIFactory.Accent);
            powerBar = UIFactory.Meter(root, "Potencia de remate", new Vector2(0.035f, 0.18f), new Vector2(0.20f, 0.198f), UIFactory.Accent);

            var bottom = UIFactory.Panel(root, "Instrucciones", new Vector2(0.015f, 0.02f), new Vector2(0.985f, 0.13f), UIFactory.Surface);
            Instructions = UIFactory.Label(bottom.transform, "", new Vector2(0.025f, 0.10f), new Vector2(0.975f, 0.90f), 28);

            overlay = UIFactory.Card(root, "Pausa de partido", new Vector2(0.20f, 0.32f), new Vector2(0.64f, 0.69f)).rectTransform;
            overlayTitle = UIFactory.Label(overlay, "", new Vector2(0.06f, 0.65f), new Vector2(0.94f, 0.91f), 42, UIFactory.Accent, TextAnchor.MiddleCenter);
            overlayBody = UIFactory.Label(overlay, "", new Vector2(0.07f, 0.29f), new Vector2(0.93f, 0.65f), 27, null, TextAnchor.MiddleCenter);
            continueButton = UIFactory.Button(overlay, "CONTINUAR", new Vector2(0.20f, 0.07f), new Vector2(0.80f, 0.27f), onContinue);
            overlay.gameObject.SetActive(false);
            choice = UIFactory.Card(root, "Decisión tras el regate", new Vector2(0.18f, 0.32f), new Vector2(0.68f, 0.69f)).rectTransform;
            UIFactory.Label(choice, "¡SUPERASTE A LA DEFENSA!", new Vector2(0.05f, 0.67f), new Vector2(0.95f, 0.94f), 36, UIFactory.Accent, TextAnchor.MiddleCenter);
            UIFactory.Label(choice, "Sigue el ataque: elige cómo terminar la jugada.", new Vector2(0.06f, 0.39f), new Vector2(0.94f, 0.65f), 27, null, TextAnchor.MiddleCenter);
            UIFactory.Button(choice, "DAR UN PASE", new Vector2(0.06f, 0.11f), new Vector2(0.48f, 0.34f), () => engine.ChooseFollowUp(InterventionKind.Pass), false);
            UIFactory.Button(choice, "TIRO AL ARCO", new Vector2(0.52f, 0.11f), new Vector2(0.94f, 0.34f), () => engine.ChooseFollowUp(InterventionKind.Shot));
            choice.gameObject.SetActive(false);
            engine.Changed += Refresh;
            Refresh();
        }

        public void Refresh()
        {
            var s = engine.State;
            score.text = $"{s.HomeGoals} – {s.AwayGoals}";
            minute.text = MatchClock.Format(s.Minute);
            speedButton.GetComponentInChildren<Text>().text = "x" + engine.SimulationSpeed.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
            speedButton.interactable = s.Phase == MatchPhase.Simulating;
            phase.text = s.Phase == MatchPhase.Intervention ? "TU MOMENTO · PAUSA" : s.Phase == MatchPhase.HalfTime ? "DESCANSO" : s.Phase == MatchPhase.Resolution ? "JUGADA RESUELTA" : "EN JUEGO";
            stamina.text = $"Resistencia  {s.Stamina.Value:0}/100";
            affinity.text = $"Afinidad  {s.Affinity.Value:0}/100";
            UIFactory.SetMeter(staminaBar, s.Stamina.Value, s.Stamina.Extreme ? UIFactory.Warning : UIFactory.Accent);
            UIFactory.SetMeter(affinityBar, s.Affinity.Value, new Color(0.30f, 0.65f, 1f));
            warning.text = s.Stamina.Extreme ? "¡FATIGA EXTREMA!\nVelocidad y precisión reducidas." : $"Fatiga: {100f - s.Stamina.Value:0}/100";
            if (s.Events.Count != lastEventCount)
            {
                lastEventCount = s.Events.Count;
                var sb = new StringBuilder();
                for (int i = Mathf.Max(0, s.Events.Count - 3); i < s.Events.Count; i++) sb.Append(s.Events[i]).Append("\n\n");
                commentary.text = sb.ToString();
            }
            if (s.Phase == MatchPhase.Simulating)
            {
                Instructions.text = $"{s.Status}   ·   Posesión: {s.Possession}";
                detail.text = "PARTIDO EN CURSO\nSimulación acelerada";
                next.text = $"{MatchEngine.OpportunityMinutes.Length} oportunidades + combinaciones\n{ s.Stats.Interventions} intervenciones jugadas\nEl reloj se pausa al intervenir.";
                SetPower(0f, false);
            }
        }
        public void SetPower(float value, bool visible)
        {
            powerLabel.gameObject.SetActive(visible);
            powerBar.transform.parent.gameObject.SetActive(visible);
            powerLabel.text = $"POTENCIA  {value * 100f:0}%";
            UIFactory.SetMeter(powerBar, value * 100f, UIFactory.Accent);
        }
        public void ShowFollowUpChoice()
        {
            HideOverlay();
            SetPower(0f, false);
            choice.gameObject.SetActive(true);
            choice.SetAsLastSibling();
        }
        public void SetActionInfo(string title, string status, string instruction)
        { detail.text = title; next.text = status; Instructions.text = instruction; }
        public void ShowOverlay(string title, string body, bool allowContinue = true)
        {
            overlayTitle.text = title;
            overlayBody.text = body;
            continueButton.gameObject.SetActive(allowContinue);
            overlay.gameObject.SetActive(true);
            overlay.SetAsLastSibling();
        }
        public void HideOverlay()
        {
            overlay.gameObject.SetActive(false);
            if (choice != null) choice.gameObject.SetActive(false);
        }
        private void OnDestroy() { if (engine != null) engine.Changed -= Refresh; }
    }
}
