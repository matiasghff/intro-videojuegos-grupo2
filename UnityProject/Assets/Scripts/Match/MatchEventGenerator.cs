using System;

namespace UltimaJugada
{
    public sealed class MatchEventGenerator
    {
        private readonly Random random;
        public MatchEventGenerator(int seed) => random = new Random(seed);
        public int NextInterval() => random.Next(3, 6);
        public string Generate(GameState state)
        {
            switch (random.Next(12))
            {
                case 0: state.Possession = "Ultima Jugada"; return "Gran recuperación en mediocampo. Recuperamos el balón.";
                case 1: state.Possession = "Deportivo Cordillera"; return "Pérdida de balón. El rival prepara la contra.";
                case 2: state.Possession = "Deportivo Cordillera"; return "Deportivo Cordillera ataca por la banda.";
                case 3: state.Possession = "Ultima Jugada"; return "¡Atajada! Nuestro arquero evita el gol.";
                case 4: state.Possession = "Ultima Jugada"; return "Tiro desviado del rival. Saque de arco.";
                case 5: state.AwayGoals++; state.Possession = "Ultima Jugada"; return $"Gol de Cordillera. Marcador {state.HomeGoals}–{state.AwayGoals}.";
                case 6: state.HomeGoals++; state.Possession = "Deportivo Cordillera"; return $"¡GOL de nuestro equipo en una jugada colectiva! {state.HomeGoals}–{state.AwayGoals}.";
                case 7: return "Falta en el mediocampo. El árbitro cobra tiro libre.";
                case 8: state.Possession = "Ultima Jugada"; return "Pase completado. El equipo combina por dentro.";
                case 9: state.Possession = "Deportivo Cordillera"; return "Ocasión ofensiva: el remate sale junto al poste.";
                case 10: state.Possession = "Deportivo Cordillera"; return "El rival recupera; nuestra defensa cierra los espacios.";
                default: state.Possession = "Ultima Jugada"; return "Ultima Jugada avanza por la derecha. La tribuna alienta.";
            }
        }
    }
}
