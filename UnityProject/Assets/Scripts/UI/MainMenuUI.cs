using UnityEngine;

namespace UltimaJugada
{
    public sealed class MainMenuUI : MonoBehaviour
    {
        private void Start()
        {
            _ = GameManager.Instance;
            Application.targetFrameRate = 60;
            var canvas = UIFactory.Canvas(transform);
            UIFactory.Panel(canvas.transform, "Fondo", Vector2.zero, Vector2.one, UIFactory.Background);
            UIFactory.Panel(canvas.transform, "Banda", new Vector2(0.065f, 0.15f), new Vector2(0.074f, 0.86f), UIFactory.Accent);
            UIFactory.Label(canvas.transform, "GRUPO 2   /   VERSIÓN ALFA", new Vector2(0.11f, 0.78f), new Vector2(0.87f, 0.86f), 25, UIFactory.Accent);
            UIFactory.Label(canvas.transform, "ÚLTIMA\nJUGADA", new Vector2(0.105f, 0.48f), new Vector2(0.70f, 0.79f), 112);
            UIFactory.Label(canvas.transform, "90 minutos. Seis oportunidades.\nEl partido cambia cuando intervienes.", new Vector2(0.11f, 0.32f), new Vector2(0.66f, 0.46f), 32, UIFactory.Muted);
            UIFactory.Button(canvas.transform, "JUGAR PARTIDO", new Vector2(0.11f, 0.19f), new Vector2(0.38f, 0.28f), SceneFlowController.PlayNewMatch);
            UIFactory.Button(canvas.transform, "SALIR", new Vector2(0.40f, 0.19f), new Vector2(0.56f, 0.28f), SceneFlowController.Quit, false);
            var card = UIFactory.Panel(canvas.transform, "Controles", new Vector2(0.71f, 0.21f), new Vector2(0.95f, 0.75f), UIFactory.Surface);
            UIFactory.Label(card.transform, "CÓMO JUGAR", new Vector2(0.08f, 0.81f), new Vector2(0.92f, 0.94f), 28, UIFactory.Accent);
            UIFactory.Label(card.transform, "REGATE\nWASD o flechas para moverte.\n\nPASE Y TIRO\nClic en el balón y arrastra hacia atrás. Suelta y haz clic en el centro del balón de precisión.\n\nDuración aproximada: 3–5 min.", new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.79f), 26);
            UIFactory.Label(canvas.transform, "ULTIMA JUGADA  vs  DEPORTIVO CORDILLERA", new Vector2(0.11f, 0.07f), new Vector2(0.94f, 0.13f), 23, UIFactory.Muted);
        }
    }
}
