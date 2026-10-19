# Verificación de la alfa

## Cómo repetir las pruebas

Abrir **Window > General > Test Runner**. Ejecutar EditMode y luego PlayMode. Los tests están separados por assembly y no entran al build final.

Por CLI, cerrar primero el proyecto abierto en el Editor o usar una copia independiente. Desde la raíz:

```sh
UNITY_EDITOR='/Applications/Unity/Hub/Editor/6000.5.10f1/Unity.app/Contents/MacOS/Unity'
"$UNITY_EDITOR" -batchmode -nographics -projectPath "$PWD/UnityProject" \
  -runTests -testPlatform EditMode -testResults /tmp/ultima-jugada-editmode.xml \
  -logFile /tmp/ultima-jugada-editmode.log
"$UNITY_EDITOR" -batchmode -projectPath "$PWD/UnityProject" \
  -runTests -testPlatform PlayMode -testResults /tmp/ultima-jugada-playmode.xml \
  -logFile /tmp/ultima-jugada-playmode.log
```

No agregar `-quit` a las llamadas con `-runTests`: el runner termina al completar las pruebas.

## Cobertura automatizada

`MatchRulesTests` verifica inicio limpio; partido completo con tres seeds; seis oportunidades y tres tipos; minutos monótonos; pausa de reloj en intervención/resolución; descanso único; final inmutable; resolución única y tipo correcto; estadísticas; goles reales en el estado; afinidad; fatiga extrema alcanzable; penalizaciones y límites; precisión radial; error angular y pérdida de potencia; desviación a ambos lados; desaceleración independiente del frame; colisión continua; límites del arco; movimiento diagonal y roles defensivos; victoria, empate y derrota.

`GameplayIntegrationTests` usa dispositivos del Input System y los controladores de producción. Comprueba selección del balón por raycast, arrastre y espera de precisión; clic central; pase completado, corto e interceptado; tiro convertido, desviado y atajado; clic fuera del círculo y timeout de precisión; regate superando defensores y regate interceptado; lectura real de WASD/flechas; navegación desde MainMenu y dos partidos completos consecutivos pasando por MatchResult, con reinicio del estado.

La prueba de flujo acelera el motor mediante segundos inyectados y resuelve las oportunidades por los timeouts de los controladores reales. Las pruebas específicas de jugada ejecutan input de mouse y trayectorias. Esto separa la cobertura funcional del ensayo humano de dificultad y duración.

`VisualSmokeTests` renderiza el campo y el Canvas reales a texturas de 1920×1080 para revisar menú, regate, arrastre, precisión y resultado. El modo batch no presenta una Game View, por lo que estas son capturas de render fuera de pantalla, no capturas de una partida manual. Los eventos de Input System de un `UnityTest` se dejan procesar en el siguiente fotograma antes de leerlos.

## Evidencia de esta sesión

Ejecutado con Unity **6000.5.10f1**, el **19 de octubre de 2026**, en una copia aislada del proyecto para conservar la sesión del Editor abierta. La copia final usa los Assets y GUID generados en el proyecto real.

| Comprobación | Resultado observado |
|---|---|
| EditMode | **30/30 aprobadas**, incluidas dos regresiones de nombres de materiales, sin omitidas ni fallidas |
| PlayMode | **12/12 aprobadas**, incluida la prueba de cinco capturas |
| Partidas consecutivas | Dos partidos completos a 90′, con seis intervenciones cada uno y revancha limpia |
| Compilación macOS | Build universal **x86_64 + arm64**, completado correctamente |
| Firma local | `codesign --verify --deep --strict` termina con código 0 |
| Inicio del ejecutable | Unity, Input System y Metal inicializan sin excepciones de runtime en la prueba de arranque |
| Asset Package / ZIP | Exportación correcta; las tres escenas, ProjectSettings y Packages/manifest.json están presentes; integridad ZIP comprobada |
| Código del juego | Sin errores ni warnings de C# en la compilación final |

Evidencia versionada: [EditMode XML](validation/editmode-results.xml), [PlayMode XML](validation/playmode-results.xml), [resumen y hashes](validation/results.json). Capturas revisadas: [menú](validation/screenshots/01-main-menu.png), [regate](validation/screenshots/02-dribble.png), [arrastre](validation/screenshots/03-pass-aim.png), [precisión](validation/screenshots/04-precision.png), [resultado](validation/screenshots/05-result.png).

Salidas locales en `UnityProject/Builds`: `macOS/UltimaJugada.app`, `Entrega4/UltimaJugada.unitypackage` y `UltimaJugada-Entrega4.zip`. Esa carpeta está ignorada por Git y los artefactos se pueden regenerar con los comandos de la guía.

La prueba nativa inicia la aplicación durante unos segundos y la termina mediante SIGTERM desde el proceso de prueba, con salida limpia. **No verifica el botón CERRAR ni constituye una partida manual.** Queda pendiente la prueba humana de sensación de control, dificultad, duración real y cierre mediante los botones, y la legibilidad en otras resoluciones.

Después de detectar los avisos de `Cesped claro` y `Cesped oscuro`, se corrigió también el `Material.name` serializado y se repitieron las 30 pruebas de EditMode. Las dos regresiones guardan deliberadamente el nombre incorrecto, esperan ese aviso y verifican la reparación, el GUID y las referencias de la escena. Una exportación posterior desde un arranque limpio no produjo avisos de discrepancia de nombres. La aplicación macOS, el Asset Package y el ZIP se regeneraron después de los ajustes; el ZIP también incluye el informe final. Las 12 pruebas de PlayMode corresponden a la validación de la alfa y no se repitieron por los cambios de documentación y empaquetado.

## Prueba manual reproducible

1. Abrir MainMenu, presionar Play y comprobar título, controles y botones. SALIR debe detener Play; volver a iniciarlo.
2. JUGAR PARTIDO: comprobar 0′, 0–0, resistencia inicial y afinidad 50. Ver avanzar minutos y relato. Un gol del relato debe cambiar el marcador.
3. A 12′, comprobar que el reloj se congela. Moverse con cada dirección y diagonal. Intentar esquivar y cruzar la línea; en otra partida dejarse interceptar. Ambos resultados deben volver al mismo minuto.
4. A 26′, seleccionar el balón, arrastrar hacia atrás y observar porcentaje, línea amarilla y trayectoria verde. Soltar debe abrir precisión sin lanzar aún. Clicar el centro y apuntar al compañero para completar un pase; comprobar afinidad +8.
5. Repetir apuntando al rival, usando poca potencia o clicando cerca del borde para comprobar intercepción, pase corto y desviación. Dejar transcurrir los 2 segundos de precisión debe producir cero puntos.
6. A 39′, intentar un remate potente hacia un costado dentro del arco. El balón debe cruzar su línea y el marcador subir en uno. Un tiro al centro puede ser atajado; fuera de los postes debe fallar.
7. A 45′, comprobar descanso, resistencia +10 y comienzo del segundo tiempo. Probar tanto CONTINUAR como el avance automático.
8. A 57′, comparar el círculo de recepción con el pase anterior tras subir/bajar afinidad.
9. A 72′, comprobar FATIGA EXTREMA y desplazamiento más lento. A 84′, comparar precisión efectiva con la del clic y potencia reducida. La geometría permite llegar al arco con suficiente potencia incluso cansado.
10. Llegar a 90′ sin nuevas intervenciones. Comprobar resultado, goles del jugador, intentos, pases/regates, intercepciones, afinidad y resistencia finales.
11. JUGAR OTRA VEZ: comprobar estado limpio y seis oportunidades disponibles otra vez. Repetir al menos dos veces sin detener Play.
12. Compilar macOS y abrir la aplicación. Repetir el flujo y usar CERRAR; debe terminar el proceso.
13. Comprobar legibilidad en horizontal a 1920×1080 y 1280×720, clic sobre el balón y ausencia de texto superpuesto.

## Límites de la evidencia

Las pruebas automatizadas pueden verificar flujo, input y geometría; no sustituyen una partida humana para valorar dificultad, sensación de control o legibilidad en cada pantalla. No se afirmará una prueba manual que no haya sido realizada. El cierre dentro del Editor detiene el runner, por lo que se verifica separadamente en una aplicación compilada.
