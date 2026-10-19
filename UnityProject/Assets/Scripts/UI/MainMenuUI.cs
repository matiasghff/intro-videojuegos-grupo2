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
            var pitch = UIFactory.Panel(canvas.transform, "Cancha grafica", new Vector2(0.60f, -0.20f), new Vector2(1.08f, 1.20f), new Color(0.04f, 0.10f, 0.14f));
            pitch.transform.localRotation = Quaternion.Euler(0f, 0f, -14f);
            for (int i = 0; i < 12; i++)
                UIFactory.Panel(pitch.transform, "Franja", new Vector2(0f, i / 12f), new Vector2(1f, (i + 0.5f) / 12f), new Color(0.05f, 0.13f, 0.17f));
            UIFactory.Panel(pitch.transform, "Mitad", new Vector2(0f, 0.498f), new Vector2(1f, 0.502f), new Color(0.12f, 0.24f, 0.28f));
            UIFactory.Panel(canvas.transform, "Banda", new Vector2(0.065f, 0.15f), new Vector2(0.074f, 0.86f), UIFactory.Accent);
            UIFactory.Label(canvas.transform, "GRUPO 2   /   VERSIÓN ALFA", new Vector2(0.11f, 0.78f), new Vector2(0.87f, 0.86f), 25, UIFactory.Accent);
            UIFactory.Label(canvas.transform, "ULTIMA\nJUGADA", new Vector2(0.105f, 0.48f), new Vector2(0.70f, 0.79f), 112);
            UIFactory.Label(canvas.transform, "90 minutos. Doce oportunidades y ataques encadenados.\nEl partido cambia cuando intervienes.", new Vector2(0.11f, 0.32f), new Vector2(0.65f, 0.46f), 30, UIFactory.Muted);
            UIFactory.Button(canvas.transform, "JUGAR PARTIDO", new Vector2(0.11f, 0.19f), new Vector2(0.38f, 0.28f), SceneFlowController.PlayNewMatch);
            UIFactory.Button(canvas.transform, "SALIR", new Vector2(0.40f, 0.19f), new Vector2(0.56f, 0.28f), SceneFlowController.Quit, false);
            var card = UIFactory.Card(canvas.transform, "Controles", new Vector2(0.71f, 0.17f), new Vector2(0.95f, 0.78f));
            UIFactory.Label(card.transform, "CÓMO JUGAR", new Vector2(0.08f, 0.81f), new Vector2(0.92f, 0.94f), 28, UIFactory.Accent);
            UIFactory.Label(card.transform, "REGATE\nWASD o flechas. Si superas a la defensa, elige pase o tiro.\n\nPASE Y TIRO\nArrastra el balón hacia atrás, suelta y pulsa el centro del objetivo en movimiento.\n\nSIMULACIÓN\nBotón x1 / x1.5 / x2.\nLas intervenciones mantienen su velocidad.", new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.79f), 25);
            UIFactory.Label(canvas.transform, "ULTIMA JUGADA  vs  DEPORTIVO CORDILLERA", new Vector2(0.11f, 0.07f), new Vector2(0.94f, 0.13f), 23, UIFactory.Muted);
        }
    }
}
