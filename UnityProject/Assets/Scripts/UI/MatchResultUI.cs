using UnityEngine;

namespace UltimaJugada
{
    public sealed class MatchResultUI : MonoBehaviour
    {
        private void Start()
        {
            var state = GameManager.Instance.EnsureMatch().State;
            var stats = state.Stats;
            var canvas = UIFactory.Canvas(transform);
            UIFactory.Panel(canvas.transform, "Fondo", Vector2.zero, Vector2.one, UIFactory.Background);
            UIFactory.Label(canvas.transform, "90′   /   FINAL DEL PARTIDO", new Vector2(0.10f, 0.83f), new Vector2(0.90f, 0.93f), 31, UIFactory.Accent, TextAnchor.MiddleCenter);
            UIFactory.Label(canvas.transform, $"{state.HomeGoals}  –  {state.AwayGoals}", new Vector2(0.26f, 0.63f), new Vector2(0.74f, 0.83f), 112, null, TextAnchor.MiddleCenter);
            UIFactory.Label(canvas.transform, "ULTIMA JUGADA     /     DEPORTIVO CORDILLERA", new Vector2(0.12f, 0.56f), new Vector2(0.88f, 0.64f), 29, UIFactory.Muted, TextAnchor.MiddleCenter);
            UIFactory.Label(canvas.transform, $"RESULTADO: {state.Verdict}", new Vector2(0.20f, 0.47f), new Vector2(0.80f, 0.56f), 42, UIFactory.Accent, TextAnchor.MiddleCenter);
            var panel = UIFactory.Panel(canvas.transform, "Estadísticas", new Vector2(0.15f, 0.24f), new Vector2(0.85f, 0.45f), UIFactory.Surface);
            UIFactory.Label(panel.transform, $"Regates exitosos: {stats.SuccessfulDribbles}/{stats.DribbleAttempts}\nPases completados: {stats.CompletedPasses}/{stats.PassAttempts}\nDisparos: {stats.Shots}    Goles del jugador: {stats.PlayerGoals}", new Vector2(0.04f, 0.05f), new Vector2(0.53f, 0.95f), 28);
            UIFactory.Label(panel.transform, $"Afinidad final: {state.Affinity.Value:0}/100\nResistencia final: {state.Stamina.Value:0}/100\nIntercepciones sufridas: {stats.Interceptions}\nPrecisión media: {stats.AveragePrecision}%", new Vector2(0.55f, 0.05f), new Vector2(0.97f, 0.95f), 26);
            UIFactory.Button(canvas.transform, "JUGAR OTRA VEZ", new Vector2(0.20f, 0.10f), new Vector2(0.46f, 0.19f), SceneFlowController.PlayNewMatch);
            UIFactory.Button(canvas.transform, "CERRAR", new Vector2(0.49f, 0.10f), new Vector2(0.68f, 0.19f), SceneFlowController.Quit, false);
            UIFactory.Button(canvas.transform, "MENÚ", new Vector2(0.70f, 0.10f), new Vector2(0.81f, 0.19f), SceneFlowController.MainMenu, false);
        }
    }
}
