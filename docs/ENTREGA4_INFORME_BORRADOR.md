# Informe de avance — Entrega 4

**Grupo 2 · Ultima Jugada · ICC0021**  
**Estado:** borrador técnico para revisión del equipo; no es el PDF final.

## 1. Funcionalidades implementadas

La alfa representa un partido completo entre Ultima Jugada y Deportivo Cordillera. Su flujo integra menú inicial, dos tiempos de 45 minutos simulados, descanso, seis intervenciones y pantalla final con estadísticas y revancha. La simulación avanza con reloj acelerado y se pausa durante las jugadas del usuario. El estado persiste al cambiar escenas.

El regate tiene movimiento WASD/flechas en ocho direcciones, diagonal normalizada y tres defensores con presión directa, anticipación y cobertura. El objetivo es cruzar una línea visible sin ser alcanzado. Tanto ganar como perder devuelve el control a la simulación.

Pases y remates se controlan con clic y arrastre inverso sobre el balón. La longitud selecciona potencia, y una segunda fase de clic en un balón circular determina precisión. Esa precisión modifica realmente el ángulo y la velocidad. El balón se desacelera mediante un modelo arcade; su recorrido detecta receptores, defensores, arquero, límites y línea de gol.

La afinidad comienza en 50, cambia con los resultados y determina el radio de recepción del compañero. La resistencia baja por tiempo y esfuerzo, recupera parcialmente en el descanso y activa fatiga extrema al llegar a 20. Ese estado reduce velocidad, precisión y potencia, con aviso visible.

El relato en pantalla explica recuperaciones, pérdidas, faltas, remates, atajadas, goles y resultados. El marcador procede del mismo estado que la narración. La cancha tiene césped a franjas, marcas, arco y red con recursos procedurales propios.

El detalle que vincula cada uno de los ocho requisitos de la pauta con scripts, escenas y comprobaciones está en `FEATURE_CHECKLIST.md`.

## 2. Decisiones de diseño y justificación

Se eligió un partido de intervenciones inspirado en el flujo de simulación deportiva: el usuario participa en momentos decisivos y observa el resto mediante relato y representación táctica. No se implementan torneo ni progresión, para concentrar la alfa en demostrar una partida completa.

Se programaron seis oportunidades en minutos fijos, incluyendo dos de cada mecánica. Así la evaluación siempre permite ver regate, pase y tiro sin depender del azar. Los eventos entre oportunidades sí tienen variación controlada por seed.

Los prototipos descritos en el handoff no estaban en este proyecto. Se implementó una arquitectura integrada nueva conservando los controles, los tres roles defensivos permanentes y las matemáticas de precisión y potencia. El remate a portería es una ampliación respecto de la referencia de pase.

Se separaron estado, simulación, input y presentación para poder probar el partido sin escenas, y probar las jugadas con los mismos controladores usados en producción. Se prefirió colisión geométrica continua y desaceleración analítica sobre un sistema físico complejo.

“Resolución textural” se interpreta como narración visible de resultados y una representación material básica del terreno. Esta es una interpretación operativa adoptada en la implementación, sin atribuir al profesor una aclaración no recibida.

Los recursos geométricos y la interfaz son originales. Se usa Unity 6000.5.10f1, URP, Input System y uGUI ya presentes en la plantilla. No se añadieron paquetes de terceros.

## 3. Problemas observados y soluciones

- **Diferencia entre handoff y repositorio:** el handoff describía scripts y escenas ubicados en otro proyecto. La inspección confirmó que aquí solo existía la plantilla; se construyeron los sistemas integrados desde cero.
- **No perder el estado al cambiar escenas:** se centralizó en una sesión persistente y se impide aplicar dos veces el resultado de una intervención.
- **Riesgo de atravesar receptores o rivales:** se implementó contacto por segmento y se ordena el primer impacto, incluyendo las salidas del campo y la línea de gol.
- **Generación inicial en batch:** Unity rechazó crear una escena aditiva junto a la escena vacía sin título del arranque. El generador ahora reemplaza solo ese escenario vacío, preservando las escenas existentes del usuario.
- **Compatibilidad de APIs:** la compilación real detectó obsolescencia de `FindFirstObjectByType` en un test. Se cambió por `FindAnyObjectByType`; el bucle del juego no usa búsquedas de objetos.
- **Firma del ejecutable macOS:** el build con nombre de archivo acentuado compiló, pero `codesign` rechazó el sello de recursos. Una copia con ejecutable ASCII verificó correctamente. El nombre técnico se cambió a `Ultima Jugada`, usando también el título sin tilde en la UI, y el comando de build verifica la firma.
- **Nombre interno de los materiales:** renombrar los archivos de césped no había cambiado sus nombres serializados, que Unity escribió con el escape `\xE9`. Se corrigieron ambos materiales y la reparación automática ahora sincroniza archivo y nombre interno, conservando GUID y referencias. Dos pruebas de regresión comprueban el caso.

Los resultados de compilación y tests se registran en `TEST_PLAN.md`. Antes de entregar el PDF, el equipo debe revisar ese registro y agregar únicamente observaciones de sus pruebas humanas reales.

## 4. Funcionalidades deseadas para la versión final

Propuestas sujetas a feedback y tiempo del equipo:

- Ajustar dificultad de regates y portero a partir de pruebas con compañeros.
- Mejorar desmarque del receptor, sin eliminar la influencia de afinidad.
- Ampliar variedad de situaciones iniciales de las tres mecánicas.
- Mejorar feedback audiovisual y accesibilidad del minijuego de precisión.
- Incorporar una introducción interactiva breve y opciones de duración/dificultad.

Estas propuestas no se presentan como funcionalidades ya implementadas.

## 5. Alcance esperado del entregable final

Un juego de fútbol para un jugador en macOS, con un partido completo de 90 minutos simulados por sesión, menú, resultado y revancha. El núcleo sigue siendo regate, pase y remate en oportunidades, con fatiga y afinidad influyendo en las decisiones. Se espera iterar sobre la variedad y claridad de esas mecánicas; no se comprometen torneos, temporadas, fichajes, economía ni multijugador.

## Pendientes de entrega del equipo

Revisar nombres de integrantes y texto; añadir observaciones reales de pruebas; exportar este informe a PDF; generar y comprobar el ZIP de Asset Package, ProjectSettings y Packages; subirlo al canal del curso. Preparar la presentación oral respetando la indicación del enunciado de no usar IA generativa para el material de apoyo. El feedback del otro grupo se entrega después de la actividad indicada por el profesor.
