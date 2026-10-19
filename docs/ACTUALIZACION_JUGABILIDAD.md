# Actualización posterior al checkpoint — 19 de octubre de 2026

El informe `Entrega4_Informe.pdf` y los archivos ya subidos corresponden al checkpoint académico. Esta actualización modifica el juego a partir de ese estado; el PDF entregado se conserva como registro.

## Cambios jugables

- Botón del marcador que alterna **x1 → x1.5 → x2 → x1**. Acelera únicamente los minutos y la presentación de la simulación. Los controles, el vuelo del balón, la precisión y las pausas usan tiempo real.
- **Doce oportunidades base**, en 6′, 13′, 21′, 29′, 36′, 42′, 50′, 58′, 65′, 73′, 80′ y 87′. Se mantienen descanso a 45′ y final a 90′.
- **Regate exitoso → elección inmediata de pase o tiro** mediante dos botones, sin avanzar el reloj. Una decisión solo se puede aplicar una vez.
- **Pase completado → 90 % de probabilidad de remate inmediato**. En las continuaciones, 55 % son devolución para un tiro del jugador y 45 % son remate automático del compañero. El breve feedback dura 0,65 s; la siguiente jugada continúa en el mismo minuto. Un pase fallido no genera esta continuación.
- **Remate del compañero visible en cancha** con vuelo real, arquero, gol o atajada. Los tiros/goles del compañero y las asistencias se registran aparte de los tiros, goles e intentos de precisión del jugador.
- **Objetivo de precisión móvil** en ambos ejes, dentro de su panel. Se conserva el plazo de 2 s y la puntuación radial del clic.
- **Flecha de potencia compacta**, limitada a 2,4 unidades. La barra y el porcentaje muestran la carga máxima; arrastrar más lejos no alarga los indicadores. La flecha indica dirección, sin prometer todo el alcance físico.

## Presentación

Estadio con tribunas, vallas, césped, banderines, dos porterías y redes; cámara inclinada, luz direccional y materiales iluminados. Los futbolistas tienen uniforme, cabeza, extremidades, botines y animación de carrera; el balón tiene paneles oscuros. La simulación mueve a los actores y al balón. El menú, HUD, paneles y resultado comparten la nueva paleta; el resultado muestra asistencias y remates de compañeros.

La presentación se genera dentro del proyecto con geometría y UI, sin recursos gráficos ni paquetes externos. Se mantienen las rutas y GUID de las escenas. El comando `Tools > Ultima Jugada > Refresh Match Presentation` permite reconstruir la presentación de Match; el juego también actualiza escenas con la presentación antigua al iniciar.

## Parámetros

En `UnityProject/Assets/Resources/MatchConfig.asset`:

| Parámetro | Valor | Efecto |
|---|---|---|
| `precisionTargetSpeed` | 1.7 | Ritmo del desplazamiento del objetivo |
| `maxAimArrowLength` | 2.4 | Largo máximo de los indicadores de dirección/potencia |
| `maxDragDistance` | 4 | Distancia que satura la potencia |
| `passFollowUpChance` | 0.9 | Probabilidad de generar un remate tras completar un pase |

## Verificación

Validado con Unity 6000.5.10f1 en una copia aislada del proyecto:

- **42/42 pruebas EditMode**: calendario, velocidades, pausa del reloj, elección tras regate, alta probabilidad de continuación, ambos rematadores, estadísticas y asistencias, partidos completos con ataques encadenados y regresiones previas.
- **18/18 pruebas PlayMode**: entrada real de mouse, potencia limitada, objetivo móvil y clic central, pase, tiro, atajada, regate, botones, dos partidos consecutivos y secuencia real regate → pase → remate/gol del compañero.
- **Ocho capturas** del Canvas y la cancha reales renderizados fuera de pantalla y revisados visualmente.
- **Build macOS universal x86_64 + arm64**, firma local válida antes y después de comprimir/extraer el ZIP. Arranque nativo de 6 segundos sin excepciones observadas; terminación mediante SIGTERM (no verifica el botón de cierre).

Evidencia: [EditMode](validation/gameplay-update/editmode-results.xml), [PlayMode](validation/gameplay-update/playmode-results.xml), [resumen y hashes](validation/gameplay-update/results.json), [cancha y flecha](validation/gameplay-update/screenshots/03-pass-aim.png), [elección tras regate](validation/gameplay-update/screenshots/06-dribble-choice.png) y [gol del compañero](validation/gameplay-update/screenshots/08-teammate-goal.png). La evidencia de la raíz de `docs/validation/` corresponde al checkpoint anterior.

La aplicación actual está en `UnityProject/Builds/macOS/UltimaJugada.app`; la vista previa comprimida, en `UnityProject/Builds/UltimaJugada-Actualizacion-macOS.zip`. Una copia del ZIP se dejó en Descargas. Estas salidas son locales y están ignoradas por Git.

Queda por evaluar con una partida humana la dificultad del objetivo móvil, el ritmo de las intervenciones y la sensación de control. Las capturas automatizadas no sustituyen esa prueba.
