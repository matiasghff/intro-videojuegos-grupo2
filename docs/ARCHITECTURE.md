# Arquitectura — Ultima Jugada

## Estado y flujo

`GameManager` es el único objeto persistente. Conserva una instancia de `MatchEngine`; los cambios de escena no reconstruyen ese estado. `NewMatch` reemplaza toda la instancia, incluyendo reloj, marcador, estadísticas, resistencia, afinidad y calendario de oportunidades. `SceneFlowController` concentra la navegación y el cierre.

`GameState` contiene los datos y `MatchStats` los contadores. `MatchEngine` no depende de GameObjects, UI, Input ni `Time`: recibe segundos mediante `Tick` y un seed para los eventos. Sus fases son:

```text
Simulating → Intervention → Resolution → Simulating
Simulating → HalfTime → Simulating
Simulating → FullTime → escena MatchResult
```

Solo `Simulating` consume tiempo. El reloj se limita al siguiente evento, oportunidad, descanso o final y procesa los límites en orden. Un frame largo no salta oportunidades. `Resolve` acepta únicamente la intervención activa y cambia de fase antes de notificar; evita aplicar dos veces los goles o estadísticas. A 90′ no se generan eventos ni oportunidades nuevos.

`MatchEventGenerator` produce relato y goles de equipo/rival con un generador `System.Random` de seed explícito. Los goles automáticos actualizan el mismo estado que los goles del jugador; únicamente estos últimos aumentan `PlayerGoals`.

## Escenas y presentación

| Escena | Objetos serializados | Construcción al iniciar |
|---|---|---|
| MainMenu | MainMenuUI, cámara | Canvas, controles y botones |
| Match | MatchSimulationManager, FieldView, cámara, césped, marcas, arco y red | HUD y actores de cada intervención |
| MatchResult | MatchResultUI, cámara | Resultado y estadísticas del estado persistente |

`MatchSimulationManager` conecta motor, campo y controlador activo. Se suscribe a `InterventionStarted`, crea una sola jugada y destruye los actores previos. Al resolver, muestra el resultado por 3,5 segundos o hasta CONTINUAR; en el descanso espera 7 segundos o el botón. Cancela sus suscripciones al destruirse.

La UI usa uGUI con CanvasScaler 1920×1080 y `InputSystemUIInputModule`. Las referencias visuales se crean juntas al iniciar cada pantalla, sin búsquedas continuas ni referencias que sobrevivan a su escena. `FieldView` genera primitivas y líneas originales. La cancha y sus materiales están guardados en disco; los actores se generan según la jugada, por lo que no requieren prefabs configurados manualmente.

## Mecánicas

- `GameInput`: teclado y mouse exclusivamente mediante Input System. Sin APIs del Input Manager antiguo.
- `PlayerController`: modelo de posición, dirección y movimiento normalizado; consume resistencia al correr.
- `DefenderAI`: posiciones y tres roles permanentes. Mantiene velocidad base 2,1, predicción 2,2 y cobertura 2,5 del handoff.
- `DribbleController`: representa los modelos, comprueba contacto relativo entre movimientos, línea objetivo y límite de tiempo. Usa pasos de hasta 1/60 s para estabilidad.
- `PassShotController`: fases Aiming, Dragging, Precision, Flight y Finished; conserva dirección y potencia antes del clic de precisión. Solo la trayectoria y los contactos resuelven el resultado.
- `PrecisionController`: balón procedural en UI, círculo real de selección, puntuación radial y timeout a cero. Se destruyen textura, sprite y panel al salir.
- `BallController`: movimiento analítico con desaceleración constante. El alcance final es `v²/(2a)` y no depende de la tasa de frames.
- `Ballistics`: cálculos de precisión, error angular, pérdida de potencia, contactos segmento–círculo y cruce de gol. Los contactos se ordenan por el primer punto del recorrido, evitando atravesar objetivos o recibir después de salir del campo.

El tiro parte más cerca del arco que el pase, para que incluso bajo fatiga extrema una ejecución suficientemente precisa y potente todavía pueda llegar a la portería. El arquero se mueve a 0,4 unidades/s durante el vuelo; un tiro al centro puede ser atajado y uno a un costado puede vencerlo.

## Afinidad y fatiga

`AffinitySystem`: rango 0–100, inicial 50; recepción `Lerp(0.55, 1.20, afinidad/100)`. La circunferencia dorada representa exactamente ese radio. Cambia tras resultados reales y no depende de una tirada de probabilidad.

`StaminaSystem`: rango 0–100, inicial 100. Pierde 1,05/minuto simulado, 0,9/segundo corriendo y 4/3/5 por resolver regate/pase/tiro. El descanso recupera 10. Por encima de 20 hay deterioro gradual; con valor ≤20 la velocidad es ×0,62, la precisión efectiva pierde 22 puntos y la velocidad de salida es ×0,82 antes/después de aplicar los demás factores multiplicativos.

La precisión produce `error = 1 - precisiónEfectiva/100`, desviación de `±12° × error` y velocidad `planeada × (1 - 0.25 × error) × factorFatiga`. El signo aleatorio elige izquierda o derecha; el tamaño del error lo determina el clic y la resistencia.

## Generación y dependencias

`ProjectBootstrap` pertenece a un assembly solo Editor. Crea los assets ausentes, asigna la referencia de FieldView, guarda escenas y materiales y configura el orden de compilación. Preserva el contenido y GUID de las escenas existentes. La escena de inicio de Play es MainMenu incluso si el Editor tiene abierta SampleScene. Los comandos de build/export usan las tres escenas del juego.

Los nombres de archivos generados, el identificador de aplicación y el nombre técnico del producto usan ASCII. También se usa `ULTIMA JUGADA` sin tilde en la interfaz por decisión del equipo. El generador puede migrar los nombres antiguos de materiales de césped mediante `AssetDatabase.MoveAsset`, preservando sus GUID. El build universal verifica la firma local de macOS; no se usa una identidad de distribución externa.

La reparación sincroniza además el nombre interno del material con su archivo y lo guarda con `SaveAssetIfDirty`, incluso cuando el archivo ya tiene el nombre correcto. Se ejecuta al recargar el código del Editor y al generar o exportar, sin reescribir las escenas existentes.

Los assemblies separan Runtime, Editor, EditModeTests y PlayModeTests. Se reutilizan los paquetes Unity que ya estaban instalados. No se modificó `Packages/manifest.json` ni se agregaron librerías externas. No se usa búsqueda de objetos en el bucle de juego.

## Decisiones de alcance

El handoff describe prototipos de otro proyecto que no estaban físicamente presentes. La autorización actual solicita su integración, por lo que reemplaza sus restricciones antiguas de mantenerlos separados y esperar validación entre pasos. Se preservan controles, matemáticas, tres roles fijos y parámetros base; se añade el remate a portería que todavía faltaba.

No se implementan NavMesh, Rigidbody para el balón, sistemas externos de estados ni arquitecturas de inyección. El estado puro y los controladores acotados permiten probar la lógica sin depender del arte.
