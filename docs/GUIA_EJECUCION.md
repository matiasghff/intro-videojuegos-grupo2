# Ultima Jugada — guía de la alfa

Esta guía amplía el `README.md` de la raíz, que explica cómo navegar el repositorio y dónde encontrar el informe. `handoff.md` conserva la referencia histórica de los prototipos anteriores. La implementación actual integra sus ideas en un proyecto nuevo; los prototipos antiguos no estaban en este repositorio.

## Abrir y jugar

1. En Unity Hub, agregar la carpeta **UnityProject**, no la raíz del repositorio.
2. Usar **Unity Editor 6000.5.10f1** con soporte de compilación para macOS. Esperar la importación y compilación.
3. Abrir `Assets/Scenes/MainMenu.unity` y presionar **Play**. El bootstrap también configura esta escena como inicio de Play en el Editor.
4. Elegir **JUGAR PARTIDO**. El partido comienza desde cero y llega a 90 minutos simulados. Al terminar se puede jugar otra vez, volver al menú o cerrar.

Las tres escenas están incluidas en el proyecto. Si faltan tras una importación, ejecutar **Tools > Ultima Jugada > Generate or Repair Project**. La generación crea solamente escenas y assets ausentes, conserva los existentes y sus GUID, y coloca MainMenu, Match y MatchResult primero en las escenas de compilación. No requiere arrastrar referencias en el Inspector. Un escenario sin título con trabajo aún no guardado debe guardarse antes de generar escenas adicionales, por una restricción del Editor.

## Controles

| Momento | Acción |
|---|---|
| Regate | WASD o flechas; movimiento diagonal normalizado |
| Pase / tiro | Clic sobre el balón blanco, mantener y arrastrar hacia atrás |
| Dirección | El balón sale en sentido opuesto al arrastre; línea verde muestra dirección y alcance sin penalizaciones |
| Potencia | Distancia de arrastre, hasta 4 unidades; porcentaje visible |
| Precisión | Después de soltar, hacer clic en el centro del balón circular antes de 2 segundos |
| Descanso / resultado de jugada | CONTINUAR o esperar el avance automático |
| Resultado final | JUGAR OTRA VEZ / MENÚ / CERRAR |

No existe una tecla que conceda goles ni botones para decidir el resultado de las jugadas. Un arrastre casi nulo permite volver a apuntar. Los clics fuera del círculo de precisión no puntúan; al agotarse el tiempo la precisión es cero.

## Partido y mecánicas

- Ultima Jugada juega de local frente a Deportivo Cordillera. Los compañeros son azules; los rivales, rojos; el balón, blanco; los objetivos, dorados.
- Se simulan dos tiempos de 45 minutos. El reloj se detiene exactamente en cada intervención y durante el descanso. El relato y el marcador permanecen visibles.
- Hay seis intervenciones garantizadas: **12′ regate, 26′ pase, 39′ tiro, 57′ pase, 72′ regate, 84′ tiro**.
- Los tres defensores del regate mantienen presión, anticipación y cobertura. Se gana cruzando la línea dorada; un contacto pierde la posesión y vuelve al partido.
- El pase puede llegar al compañero, ser interceptado, desviarse o quedarse corto. El círculo dorado muestra el radio de recepción real.
- El tiro se resuelve mediante trayectoria, desaceleración, contacto con un defensor o portero y cruce de la línea de gol entre los postes. El gol actualiza el marcador.
- La **afinidad** comienza en 50. Un pase completado suma 8; fallarlo resta 4; un regate exitoso suma 3; un gol suma 4; un remate fallido resta 2. El radio de recepción crece de **0,55 a 1,20 unidades** según la afinidad.
- La **resistencia** comienza en 100 y baja por minutos, carrera y acciones. El descanso recupera 10. Con resistencia ≤20 se activa fatiga extrema: velocidad de regate al **62%**, precisión efectiva **−22 puntos** y potencia al **82%**, además de la penalización por precisión. El regate de 72′ permite observar este estado incluso con poco esfuerzo previo.
- La precisión efectiva genera un error angular de hasta 12° y una pérdida de velocidad de hasta 25%. El indicador distingue precisión del clic y precisión efectiva después de la fatiga.

La simulación pura dura aproximadamente **160 segundos**, más intervenciones, descanso y resoluciones. El objetivo es 3–5 minutos por partido, según la rapidez del jugador. Los límites de tiempo impiden que una intervención quede bloqueada sin input.

## Configuración y compilación

Los parámetros de tiempo y pelota se editan en `Assets/Resources/MatchConfig.asset`. El proyecto usa URP 17.5.0, Input System 1.20.0 y uGUI 2.5.0, presentes en el manifest original; no se agregaron dependencias externas. El Canvas escala desde 1920×1080 en formato horizontal. Se usa la fuente integrada de Unity, sin importar recursos de TextMeshPro.

Para generar la aplicación universal (Intel y Apple Silicon): **Tools > Ultima Jugada > Build macOS**. El resultado queda en `UnityProject/Builds/macOS/UltimaJugada.app`. El nombre técnico del ejecutable usa `Ultima Jugada` sin tilde para evitar problemas de normalización Unicode en la firma local de macOS; la UI también usa “ULTIMA JUGADA” sin tilde. El comando verifica la firma con `codesign`. En el Editor, SALIR/CERRAR detiene Play; en la aplicación llama a `Application.Quit()`.

Con el proyecto cerrado en Unity, también se pueden ejecutar estos comandos desde la raíz del repositorio:

```sh
UNITY_EDITOR='/Applications/Unity/Hub/Editor/6000.5.10f1/Unity.app/Contents/MacOS/Unity'
"$UNITY_EDITOR" -batchmode -projectPath "$PWD/UnityProject" \
  -executeMethod UltimaJugada.Editor.ProjectBootstrap.Generate -quit -logFile /tmp/ultima-jugada-setup.log
"$UNITY_EDITOR" -batchmode -projectPath "$PWD/UnityProject" \
  -executeMethod UltimaJugada.Editor.ProjectBootstrap.BuildMacOS -quit -logFile /tmp/ultima-jugada-build.log
```

## Empaquetar la entrega

1. Ejecutar **Tools > Ultima Jugada > Export Entrega 4**.
2. Se genera `Builds/Entrega4/UltimaJugada.unitypackage` con los Assets, además de copias de `ProjectSettings` y los JSON de `Packages`, incluido `manifest.json`. La carpeta también incluye `LEEME_ENTREGA.md` y copia el informe actualizado a `docs/Entrega4_Informe.pdf` cuando se exporta desde el repositorio completo. Las guías y la evidencia técnica permanecen en `docs/` del repositorio.
3. Comprimir `Builds/Entrega4` en ZIP. También puede ejecutarse, desde UnityProject:

   ```sh
   ditto -c -k --keepParent Builds/Entrega4 Builds/UltimaJugada-Entrega4.zip
   ```

4. Probar la importación en un proyecto de la misma versión de Unity; conservar `ProjectSettings` y `Packages` junto al package. Las escenas de inicio se configuran con Generate or Repair Project.
5. Subir el ZIP al Drive del grupo y entregar el informe PDF en el buzón indicado por el curso. Los archivos en `Builds/` son salidas locales ignoradas por Git.

La entrega exige informe PDF, repositorio y Asset Package, presentación oral y feedback a otro grupo. El informe actualizado es [Entrega4_Informe.pdf](Entrega4_Informe.pdf); el borrador Markdown y el respaldo original no sustituyen ese PDF final. La pauta indica que el material de apoyo de la presentación debe realizarse **sin IA generativa**; ese material queda a cargo del equipo.

## Alcance y limitaciones

Alfa arcade para teclado y mouse en macOS: un solo partido, dos equipos y seis oportunidades. La simulación entre jugadas se presenta como campo táctico y relato; no reproduce continuamente a los 22 futbolistas. Los compañeros receptores son estáticos y el portero tiene movimiento lateral sencillo. No hay torneos, economía, multijugador, guardado ni progresión entre partidos. La portería usa una comprobación geométrica de cruce, sin altura ni física aérea del balón. El arte es procedural y no incluye assets de terceros.

La expresión “resolución textural” de la pauta se interpreta operativamente como relato visible de acontecimientos y resultados, más césped y marcas con materiales sencillos. Es una decisión del equipo para esta implementación; no se atribuye una aclaración al profesor.

Consultar [TEST_PLAN.md](TEST_PLAN.md) para las pruebas y su evidencia, y [FEATURE_CHECKLIST.md](FEATURE_CHECKLIST.md) para el mapeo de los ocho requisitos.
