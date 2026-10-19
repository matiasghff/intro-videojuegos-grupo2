# Entrega 4 — checklist del Grupo 2

Referencia: `docs/Entrega4.pdf`, página 2, Grupo 2. “Implementado” indica existencia de código jugable; la evidencia de ejecución se registra por separado en `TEST_PLAN.md`.

| # | Requisito de la pauta | Implementación / escena | Comprobación |
|---|---|---|---|
| 1 | Moverse en todas las direcciones contra defensores | `GameInput`, `PlayerController`, `DefenderAI`, `DribbleController` / Match a 12′ y 72′ | WASD y flechas; diagonal normalizada; presión, anticipación y cobertura; alcanzar la línea dorada |
| 2 | Apuntar y tirar la pelota con el input asociado | `PassShotController`, `BallController`, `Ballistics` / Match a 26′, 39′, 57′ y 84′ | Clic y arrastre inverso; potencia visible; trayectoria real hacia compañero o portería |
| 3 | Ver la “resolución textural” del partido | `MatchEventGenerator`, `MatchEngine`, `MatchHUD`, `FieldView` / Match y MatchResult | Relato visible con minuto y consecuencias; marcador coherente; césped a franjas, líneas y portería con materiales originales |
| 4 | Mayor precisión según dónde se hace clic en el balón | `PrecisionController`, `Ballistics`, `PassShotController` / Match | Tras soltar, clicar centro/borde del balón circular; mostrar puntuación y precisión efectiva; cambiar ángulo y velocidad; timeout = 0 |
| 5 | Poder ser interceptado | `DribbleController`, `PassShotController`, `Ballistics` / Match | Dejarse alcanzar al regatear y dirigir un pase hacia el rival; registrar intercepción y devolver posesión al rival |
| 6 | Afinidad con compañeros | `AffinitySystem`, `MatchEngine`, `MatchHUD` / Match | Completar/fallar pases y ver +8/−4; comparar el radio de recepción real a baja y alta afinidad |
| 7 | Fatiga extrema que afecta el juego | `StaminaSystem`, `PlayerController`, `Ballistics`, `MatchHUD` / Match | Llegar a 72′; aviso con resistencia ≤20; velocidad al 62%, precisión −22 y potencia al 82% |
| 8 | Completar una partida simple | `GameManager`, `MatchEngine`, `MatchSimulationManager`, `SceneFlowController`, UI / MainMenu → Match → MatchResult | Jugar 0′–45′, descanso, 45′–90′; seis oportunidades; final, estadísticas, revancha limpia y cierre |

Los ocho requisitos están implementados. Para el tercero se adopta una interpretación operativa explícita: narración textual visible y representación material sencilla de la cancha. No existe una aclaración del profesor incorporada al proyecto.

## Flujo integrado

- [x] Menú con JUGAR PARTIDO y SALIR.
- [x] Partido nuevo desde 0′ y 0–0 con estado limpio.
- [x] Reloj acelerado y seis oportunidades garantizadas en orden.
- [x] Primer tiempo, descanso con recuperación y segundo tiempo.
- [x] Reloj pausado durante intervenciones y resolución, sin reiniciarse.
- [x] Regate, pase y tiro con éxito/fallo y contacto con rivales.
- [x] Precisión por clic con consecuencias reales sobre ángulo y velocidad.
- [x] Afinidad y resistencia visibles, limitadas y con consecuencias reales.
- [x] Relato en pantalla y marcador actualizado por los goles.
- [x] Final a 90′; victoria, empate o derrota calculados desde el marcador.
- [x] Revancha, regreso al menú y cierre.
- [x] Escenas, referencias, materiales y configuración generados por Editor.
- [x] Pruebas automatizadas de reglas y de integración con Input System.
- [x] 42 pruebas aprobadas (30 EditMode y 12 PlayMode); build macOS universal y verificación de firma local.
- [x] Nombres técnicos de archivos y producto sin acentos.

## Entregables académicos que completa el equipo

- [x] Informe actualizado y exportado a `docs/Entrega4_Informe.pdf`.
- [ ] Revisión final del informe por el equipo antes de subirlo.
- [ ] Preparar personalmente el material de apoyo de la presentación, respetando la restricción de IA de la pauta.
- [ ] Ensayar una demostración de máximo 10 minutos.
- [ ] Subir Asset Package, ProjectSettings y Packages en ZIP al Drive del grupo.
- [ ] Entregar el feedback del otro grupo tras la sesión indicada por el curso.
