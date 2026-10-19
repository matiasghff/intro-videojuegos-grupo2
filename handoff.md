# HANDOFF TÉCNICO — ÚLTIMA JUGADA
## Proyecto Unity 6 — Videojuego de fútbol

### 1. Contexto general

**Nombre del proyecto:** Última Jugada  
**Motor:** Unity 6  
**Versión del Editor utilizada:** 6000.5.10f1  
**Plataforma de desarrollo:** macOS  
**Lenguaje de programación:** C#  
**Render Pipeline:** Universal Render Pipeline (URP)  
**Editor de código:** VS Code / Terminal  
**Herramientas disponibles:** Unity Hub y Unity CLI

Estamos desarrollando un videojuego de fútbol por mecánicas independientes. El objetivo actual no es integrar un juego completo, sino desarrollar, probar y perfeccionar cada sistema de forma aislada.

Hasta ahora hemos trabajado en dos prototipos:

1. **DribblePrototype:** mecánica de regate contra defensores controlados por IA.
2. **PassShotPrototype:** mecánica de pase/disparo mediante arrastre, control de potencia y una segunda fase de precisión.

Ambos prototipos han sido implementados y validados manualmente en Unity, con las salvedades indicadas más adelante.

**IMPORTANTE:** Los prototipos deben mantenerse separados hasta que se solicite explícitamente su integración.

---

### 2. Ubicación y estructura del proyecto

Ruta local del proyecto:

```text
/Users/matiasghf/Setup Guide In-Editor Tutorial
```

Estructura relevante:

```text
Setup Guide In-Editor Tutorial/
│
├── Assets/
│   ├── Scenes/
│   │   ├── DribblePrototype.unity
│   │   └── PassShotPrototype.unity
│   │
│   ├── Scripts/
│   │   ├── DribblePrototype.cs
│   │   └── PassShotPrototype.cs
│   │
│   ├── Materials/
│   ├── Prefabs/
│   ├── Tutorials/
│   └── ...
│
├── Packages/
├── ProjectSettings/
├── Library/
├── Logs/
├── Temp/
└── UserSettings/
```

Los archivos `.meta` deben preservarse para mantener sus identificadores y referencias dentro de Unity.

La escena `PassShotPrototype.unity` se creó inicialmente en `Assets/`, pero se movió correctamente a `Assets/Scenes/` junto con su `.meta`.

#### Arquitectura actual

El proyecto utiliza una arquitectura sencilla basada en controladores C# que generan los objetos de juego en tiempo de ejecución.

Cada prototipo utiliza:

```csharp
[RuntimeInitializeOnLoadMethod(
    RuntimeInitializeLoadType.AfterSceneLoad
)]
```

para crear automáticamente su controlador.

**Regate:**

```text
__DRIBBLE_GAME__
```

**Pase/disparo:**

```text
__PASS_SHOT_GAME__
```

Cada controlador comprueba el nombre de la escena activa antes de inicializarse.

Esto permite ejecutar cada mecánica sin configurar manualmente todos los objetos en el Inspector.

Actualmente, gran parte del escenario, materiales y lógica visual se construye con código mediante `GameObject.CreatePrimitive()`.

---

# 3. MECÁNICA 1 — REGATE

## 3.1 Estado actual

**Estado:** funcional, validado y congelado.

El jugador controla una esfera azul que representa al futbolista con balón.

Debe avanzar por la cancha, esquivar tres defensores controlados por IA y alcanzar una línea de meta.

Condiciones:

- Si el jugador cruza la línea amarilla, gana.
- Si cualquier defensor lo alcanza, pierde.
- Presionando `R` se reinicia la ronda.

## 3.2 Controles

| Acción | Control |
|---|---|
| Mover arriba | W / ↑ |
| Mover abajo | S / ↓ |
| Mover izquierda | A / ← |
| Mover derecha | D / → |
| Movimiento diagonal | Combinaciones |
| Reiniciar ronda | R |

El movimiento diagonal utiliza `Vector3.ClampMagnitude` para evitar que sea más rápido que el movimiento en línea recta.

## 3.3 Escenario

Vista superior mediante cámara ortográfica.

Parámetros principales:

```csharp
playerSpeed = 5f;

fieldHalfWidth = 5.5f;
fieldBottom = -9f;
fieldTop = 9f;

finishLineZ = 7f;

catchDistance = 0.75f;
```

El campo tiene aproximadamente:

```csharp
new Vector3(11f, 0.2f, 18f)
```

Posición inicial del jugador:

```csharp
new Vector3(0f, 0.6f, -7f)
```

La línea de meta es un cubo muy plano de color amarillo situado cerca del extremo superior.

El jugador se mantiene dentro de los límites mediante `Mathf.Clamp`.

## 3.4 Defensores

Existen tres defensores.

Posiciones iniciales:

```csharp
Defender_1:
new Vector3(0f, 0.6f, -1f);

Defender_2:
new Vector3(-3.5f, 0.6f, 1.5f);

Defender_3:
new Vector3(3.5f, 0.6f, 1.5f);
```

Se representan con cápsulas rojas.

**Velocidad definitiva seleccionada:**

```csharp
enemySpeed = 2.1f;
```

Inicialmente era `1.8f`, pero la dificultad se consideraba algo fácil y se aumentó a `2.1f`.

## 3.5 IA defensiva seleccionada

Este punto es especialmente importante.

Después de varias iteraciones, el comportamiento preferido fue mantener **tres roles fijos**.

**Defender_1 — Presión directa**

Persigue constantemente la posición actual del balón:

```csharp
targetPosition = player.transform.position;
```

**Defender_2 — Anticipación**

Calcula la dirección en que el usuario está intentando moverse y apunta hacia una posición adelantada:

```csharp
targetPosition =
    player.transform.position
    + playerMoveDirection * predictionDistance;
```

Parámetro:

```csharp
predictionDistance = 2.2f;
```

**Defender_3 — Cobertura**

Intenta mantenerse delante del jugador y bloquear su ruta hacia la meta:

```csharp
targetZ =
    player.transform.position.z
    + coverDistance;
```

Parámetro:

```csharp
coverDistance = 2.5f;
```

El objetivo de cobertura se limita para que no sobrepase demasiado la zona de meta.

Los defensores utilizan `Vector3.MoveTowards` para desplazarse hacia sus objetivos.

### Decisión de diseño importante

Se experimentó con una versión de IA de **roles dinámicos**, donde el defensor más cercano siempre presionaba y los demás alternaban entre anticipación y cobertura.

Esa versión fue probada y descartada porque el comportamiento se sentía peor que el sistema anterior.

**No restaurar automáticamente el sistema de roles dinámicos.**

El comportamiento elegido es:

```text
Defender_1 → PRESIÓN
Defender_2 → ANTICIPACIÓN
Defender_3 → COBERTURA
```

con:

```csharp
enemySpeed = 2.1f;
```

Preservar esta versión salvo que el usuario solicite rediseñarla.

## 3.6 Condiciones de victoria y derrota

Victoria:

```csharp
player.transform.position.z >= finishLineZ
```

Derrota:

```csharp
distanceToDefender <= catchDistance
```

La distancia se calcula en el plano horizontal XZ, ignorando la altura Y.

Ambos resultados detienen la simulación de la ronda.

`OnGUI()` muestra mensajes de victoria o derrota.

`R` restaura las posiciones iniciales y los estados booleanos.

## 3.7 Validaciones realizadas

Todas estas pruebas fueron completadas exitosamente:

- Movimiento en cuatro direcciones y diagonal.
- Restricciones de los límites del campo.
- Línea de meta y victoria.
- Persecución de los defensores.
- Derrota al ser alcanzado.
- Tres defensores simultáneos.
- Diferenciación de roles.
- Anticipación y cobertura.
- Reinicio después de ganar o perder.

**Esta mecánica está congelada como checkpoint funcional.**

No modificarla mientras se desarrolla pase/disparo.

---

# 4. MECÁNICA 2 — PASE / DISPARO

## 4.1 Estado actual

**Estado:** mecánica de pase funcional y validada, con ajustes visuales recientes pendientes de validación final.

La escena es:

```text
Assets/Scenes/PassShotPrototype.unity
```

El script principal es:

```text
Assets/Scripts/PassShotPrototype.cs
```

El prototipo contiene:

- Campo verde.
- Cámara ortográfica superior.
- Balón blanco.
- Compañero receptor azul.
- Defensor rojo.
- Línea amarilla de arrastre.
- Fase de elección de potencia/dirección.
- Fase adicional de precisión.
- Movimiento del balón con desaceleración.
- Recepción del pase.
- Intercepción.
- Pase fallido.
- Reinicio.

Por ahora solo está implementado el caso jugable de **pase a un compañero**. El remate a portería todavía no se ha construido como variante independiente.

## 4.2 Fase 1 — Selección, dirección y potencia

El usuario hace clic directamente sobre el balón.

La selección se detecta mediante:

```csharp
Camera.ScreenPointToRay()
Physics.Raycast()
```

Se utiliza el collider de la esfera para identificar correctamente el balón.

Cuando comienza el arrastre:

- El balón pasa temporalmente a amarillo.
- Aparece una línea amarilla desde el balón hacia el cursor.
- Se calcula el vector de arrastre.
- Se calcula una potencia entre 0% y 100%.

Parámetros:

```csharp
maxDragDistance = 4f;

minShotSpeed = 4f;
maxShotSpeed = 12f;
```

La potencia depende de la distancia de arrastre:

```csharp
dragPower =
    Mathf.Clamp01(
        dragVector.magnitude / maxDragDistance
    );
```

La dirección del pase se invierte respecto al arrastre:

```csharp
pendingBallDirection =
    -dragVector.normalized;
```

Es decir:

```text
Arrastrar hacia abajo
           ↓
Pase dirigido hacia arriba
```

La velocidad inicial planeada utiliza interpolación:

```csharp
pendingBallSpeed =
    Mathf.Lerp(
        minShotSpeed,
        maxShotSpeed,
        dragPower
    );
```

Un arrastre prácticamente nulo no ejecuta el pase.

## 4.3 Fase 2 — Minijuego de precisión

Después de soltar el mouse en la primera fase, el balón todavía no se mueve.

Se activa un segundo minijuego:

- Aparece un objetivo circular de gran tamaño en una posición aleatoria de la interfaz.
- El usuario tiene un tiempo limitado para hacer clic.
- Mientras más cerca del centro haga clic, mayor será la precisión.
- Si se acaba el tiempo, la precisión es 0%.

Parámetros:

```csharp
precisionTimeLimit = 2f;

precisionTargetSize = 180f;
```

El objetivo se genera programáticamente mediante una textura `Texture2D`.

Se dibuja mediante:

```csharp
GUI.DrawTexture()
```

La posición se determina con `Random.Range`.

La puntuación se calcula según la distancia normalizada al centro:

```csharp
precisionScore =
    Mathf.RoundToInt(
        (1f - normalizedDistance) * 100f
    );
```

Resultado:

```text
Centro exacto → 100%

Cerca del centro → puntuación alta

Cerca del borde → puntuación baja

Se termina el tiempo → 0%
```

Se validó que el objetivo aparece en distintas ubicaciones, el clic es detectado y el temporizador funciona.

## 4.4 La precisión afecta la dirección

La segunda fase no es solamente visual.

El resultado modifica realmente la dirección ejecutada.

Parámetro:

```csharp
maxPrecisionErrorAngle = 12f;
```

La desviación depende de:

```csharp
float errorFactor =
    1f - precision01;
```

Porcentajes aproximados:

| Precisión | Error angular |
|---|---|
| 100% | 0° |
| 75% | 3° |
| 50% | 6° |
| 25% | 9° |
| 0% | 12° |

El signo del ángulo se selecciona aleatoriamente hacia izquierda o derecha.

Se aplica la desviación utilizando:

```csharp
ballMoveDirection =
    Quaternion.Euler(
        0f,
        appliedDeviationAngle,
        0f
    ) * pendingBallDirection;
```

La dirección resultante se normaliza.

Esto fue probado exitosamente con puntuaciones de precisión altas, intermedias y bajas.

## 4.5 La precisión también afecta la velocidad

La última versión funcional del sistema incorpora una penalización suave de potencia.

Parámetro:

```csharp
maxPrecisionSpeedLoss = 0.25f;
```

El multiplicador de velocidad es:

```csharp
float speedMultiplier =
    1f - maxPrecisionSpeedLoss * errorFactor;
```

La velocidad ejecutada:

```csharp
appliedBallSpeed =
    pendingBallSpeed * speedMultiplier;
```

| Precisión | Velocidad conservada |
|---|---|
| 100% | 100% |
| 75% | 93.75% |
| 50% | 87.5% |
| 25% | 81.25% |
| 0% | 75% |

De esta manera, una mala precisión provoca tanto desviación lateral como una ejecución menos potente.

**Esta modificación fue probada exitosamente.**

## 4.6 Movimiento y desaceleración

No se está utilizando un Rigidbody para simular el desplazamiento del balón.

El movimiento actual es determinista y se realiza modificando su `Transform` en cada frame.

La desaceleración se simula con:

```csharp
ballDeceleration = 4f;
```

Durante el movimiento:

```csharp
ball.transform.position +=
    ballMoveDirection
    * ballCurrentSpeed
    * Time.deltaTime;
```

Luego se reduce `ballCurrentSpeed` hasta llegar a cero.

La idea es conservar un comportamiento arcade simple.

No introducir física compleja salvo que se solicite explícitamente.

## 4.7 Compañero receptor

Existe un compañero estático representado por una cápsula azul.

Posición:

```csharp
new Vector3(2.5f, 0.6f, 3.5f)
```

La recepción se detecta mediante proximidad en el plano XZ:

```csharp
receptionDistance = 0.9f;
```

Cuando el balón llega suficientemente cerca:

- Se detiene.
- El compañero cambia de azul a verde.
- Aparece `¡PASE EXITOSO!`.
- Se guarda el resultado de precisión.
- La ronda queda terminada.

## 4.8 Defensor e intercepción

Existe un defensor estático representado por una cápsula roja.

Posición:

```csharp
new Vector3(0.6f, 0.6f, -1.2f)
```

Distancia de intercepción:

```csharp
interceptionDistance = 0.85f;
```

Cuando el balón pasa suficientemente cerca:

- Se detiene.
- El defensor cambia de rojo a naranja.
- Aparece `¡PASE INTERCEPTADO!`.
- Se termina la ronda.

Si el balón se detiene sin ser recibido ni interceptado:

```text
PASE FALLIDO
```

No existe todavía una IA de movimiento para este defensor.

## 4.9 Reinicio

Tecla:

```text
R
```

El sistema restaura:

- Posición inicial del balón.
- Dirección y velocidad.
- Estado de arrastre.
- Estado de precisión.
- Temporizador.
- Resultado del pase.
- Colores originales del compañero y defensor.
- Visibilidad de la línea de arrastre.

Se ha validado que reinicia correctamente durante las distintas fases.

---

# 5. ÚLTIMOS CAMBIOS VISUALES

Después de validar todas las mecánicas anteriores, se solicitó mejorar las proporciones visuales.

El problema era que el balón parecía aproximadamente tan grande como los jugadores y las sombras resultaban excesivamente largas.

Se aplicaron estos cambios al script `PassShotPrototype.cs`.

### 5.1 Balón más pequeño

Antes:

```csharp
ball.transform.localScale =
    new Vector3(0.9f, 0.9f, 0.9f);
```

Ahora:

```csharp
ball.transform.localScale =
    new Vector3(0.55f, 0.55f, 0.55f);
```

También se modificó la altura inicial:

```csharp
ballStartPosition =
    new Vector3(0f, 0.4f, -6.5f);
```

Los jugadores mantienen su tamaño anterior.

Este cambio busca que la pelota tenga una escala proporcionalmente menor.

### 5.2 Sombras más pequeñas

Se agregó una función:

```csharp
ConfigureLighting();
```

que modifica la luz direccional de la escena.

Configuración:

```csharp
directionalLight.transform.rotation =
    Quaternion.Euler(82f, -30f, 0f);

directionalLight.shadows =
    LightShadows.Soft;

directionalLight.shadowStrength =
    0.55f;
```

La intención es dirigir la luz más verticalmente para acortar las sombras y suavizarlas.

### 5.3 Warning de Unity 6 — Situación pendiente

La primera implementación usaba:

```csharp
FindObjectsOfType<Light>();
```

Esto generó un warning `CS0618` por utilizar una API obsoleta.

Se reemplazó por:

```csharp
FindObjectsByType<Light>(
    FindObjectsSortMode.None
);
```

Pero Unity 6000.5.10f1 produjo otros dos warnings indicando que tanto el enumerado como ese overload están obsoletos.

La corrección indicada fue:

```csharp
FindObjectsByType<Light>();
```

**Estado exacto:** esta última corrección fue indicada al usuario, pero no existe una validación posterior confirmada en la conversación.

Por lo tanto, la primera tarea de Codex debe ser inspeccionar la implementación actual y comprobar si el archivo contiene la llamada correcta.

No asumir que el warning quedó solucionado.

Los warnings anteriores no fueron errores de compilación.

---

# 6. VALIDACIONES COMPLETADAS

## DribblePrototype

Funciona:

- Movimiento.
- Movimiento diagonal.
- Límites.
- Tres defensores.
- IA de presión.
- IA de anticipación.
- IA de cobertura.
- Victoria.
- Derrota.
- Reinicio.

## PassShotPrototype

Funciona, según las pruebas completadas antes del último ajuste visual:

- Creación de cancha, balón y personajes.
- Selección del balón mediante raycast.
- Arrastre y visualización de la línea.
- Cálculo de dirección.
- Potencia variable.
- Movimiento y desaceleración.
- Compañero receptor.
- Intercepción.
- Pase fallido.
- Fase de precisión.
- Objetivo en posición aleatoria.
- Temporizador.
- Puntaje entre 0% y 100%.
- Desviación angular según precisión.
- Penalización de velocidad según precisión.
- Reinicio.

Pendiente de validación final:

- Corrección de warnings de iluminación.
- Tamaño nuevo del balón.
- Tamaño y suavidad de las sombras.
- Prueba de regresión de las mecánicas después del último cambio visual.

---

# 7. DECISIONES DE ARQUITECTURA

Por ahora mantenemos una estructura muy simple.

**No estamos utilizando todavía:**

- Arquitectura compleja de componentes de gameplay.
- ScriptableObjects de configuración.
- Máquina de estados externa.
- Sistema avanzado de animaciones.
- Prefabs de personajes finales.
- Cinemachine.
- NavMesh.
- Física realista del balón.
- UI Toolkit para la interfaz de juego.

Actualmente predominan `GameObject.CreatePrimitive`, `Transform`, `Raycast`, `OnGUI` y variables internas de los scripts.

Esto fue deliberado para conseguir prototipos jugables rápidamente.

Codex puede proponer mejoras arquitectónicas, pero no debe reescribir todo el proyecto sin autorización.

Cuando el proyecto crezca será razonable separar:

```text
GameManager
PlayerController
DefenderAI
BallController
InputHandler
PassSystem
PrecisionSystem
UIManager
```

Sin embargo, esto es una **propuesta futura**, no la arquitectura que ya tenemos implementada.

---

# 8. CONTROL DE VERSIONES Y COLABORACIÓN

El proyecto está pensado para ser compartido con dos integrantes adicionales.

Se conversó sobre utilizar Git y GitHub, pero no se completó en esta conversación la configuración de un repositorio remoto compartido.

**No asumir que existe un repositorio Git inicializado ni que GitHub ya está configurado.**

Antes de realizar cambios relacionados con Git, inspeccionar:

```bash
git status
git remote -v
```

Si se configura Git, usar un `.gitignore` adecuado para Unity, evitando subir carpetas generadas como:

```text
Library/
Temp/
Logs/
Obj/
```

Preservar y versionar los archivos `.meta`, las escenas, los scripts, los assets relevantes y la configuración del proyecto.

No modificar el historial ni hacer `force push` sin autorización.

---

# 9. CÓMO DEBE TRABAJAR CODEX EN ESTE PROYECTO

Este apartado es especialmente importante.

El usuario ha desarrollado el proyecto paso a paso con asistencia y está aprendiendo Unity desde cero.

Por lo tanto, Codex debe seguir estas reglas:

1. **Trabajar incrementalmente.** Implementar un paso y esperar a que el usuario valide antes de continuar.
2. **Preferir código.** Realizar la mayor cantidad posible de cambios mediante C# y Terminal.
3. **Dar instrucciones exactas** cuando sea necesario interactuar manualmente con Unity.
4. **No modificar ambos prototipos innecesariamente.** Mantener las mecánicas independientes.
5. **Preservar el gameplay validado.** No cambiar velocidades, controles, IA o parámetros sin explicarlo.
6. **Verificar la compilación.** Distinguir errores de warnings.
7. **Mantener compatibilidad con Unity 6000.5.10f1.**
8. **No utilizar APIs obsoletas** cuando exista una alternativa compatible.
9. **Respetar los archivos existentes.** Inspeccionar el código real antes de modificarlo y evitar sobrescribir trabajo no revisado.
10. **Entregar criterios claros de validación**, explicando qué debería observarse en pantalla y en Console.

El flujo habitual ha sido:

```text
1. Detener Play Mode.
2. Modificar código.
3. Esperar compilación.
4. Verificar Console.
5. Ejecutar Play.
6. Realizar pruebas manuales.
7. Confirmar resultado.
8. Avanzar al siguiente paso.
```

Se han usado scripts generados desde Terminal mediante `cat > archivo <<'EOF'`, aunque Codex puede editar archivos directamente si tiene acceso al workspace.

---

# 10. LIMITACIONES Y MEJORAS FUTURAS

Estas son posibles mejoras identificadas, no tareas que deban ejecutarse automáticamente.

### Jugabilidad

- Desarrollar la variante de remate al arco.
- Agregar portería y portero cuando corresponda.
- Considerar múltiples receptores para pases.
- Incorporar defensores adicionales.
- Eventualmente añadir movimiento de compañeros.
- Integrar las mecánicas en el juego completo.

### Sistemas técnicos

- Mejorar la comprobación de colisiones del balón para evitar que atraviese objetivos a velocidades altas.
- Incorporar reglas para determinar cuándo el balón sale de la cancha.
- Revisar la precisión de la interfaz circular, porque actualmente la detección inicial utiliza el rectángulo que contiene al círculo.
- Separar responsabilidades del script cuando el proyecto crezca.
- Mejorar la presentación visual y HUD.

### Visuales

- Proporciones realistas de los personajes y el balón.
- Sombras suaves y cortas.
- Cancha con marcas y líneas.
- Modelos más representativos de futbolistas.
- Animaciones y feedback visual.
- UI mejor diseñada.

Evitar que mejoras estéticas rompan la lógica de las mecánicas.

---

# 11. PUNTO EXACTO DE CONTINUACIÓN

La situación actual es:

**DribblePrototype**

- Funcional.
- Congelado.
- No requiere intervención inmediata.

**PassShotPrototype**

- Lógica principal de pases funcional.
- Arrastre, potencia y precisión implementados.
- Recepción, intercepción y resultados implementados.
- Última modificación: reducir tamaño del balón y acortar las sombras.
- Pendiente: confirmar corrección de warnings de `FindObjectsByType` y validar los últimos ajustes visuales.

### Primera tarea solicitada a Codex

1. Abrir e inspeccionar el proyecto existente.
2. Leer `Assets/Scripts/PassShotPrototype.cs` y `Assets/Scripts/DribblePrototype.cs`.
3. Confirmar que ambas escenas existen.
4. Revisar específicamente `ConfigureLighting()`.
5. Verificar el uso correcto de `FindObjectsByType<Light>()` en esta versión de Unity.
6. Identificar posibles errores o warnings sin alterar las mecánicas.
7. Informar del estado actual antes de realizar cambios significativos.

**No comenzar integrando los prototipos ni desarrollando nuevas mecánicas.**

Primero debemos recuperar el estado limpio y validar los ajustes visuales actuales.

---

## 12. Instrucción final para Codex

Actúa como desarrollador senior de videojuegos especializado en Unity 6 y C#, con especial atención a la calidad del código, el diseño de gameplay y la experiencia de usuario.

Este proyecto ya tiene un trabajo considerable implementado y probado. Tu responsabilidad inicial es comprender y preservar ese trabajo.

Prioriza soluciones simples, mantenibles y compatibles con la versión instalada de Unity.

No supongas que el contenido de este handoff coincide necesariamente con todos los archivos actuales: inspecciona el workspace para comprobarlo.

Distingue entre funcionalidades verificadas, decisiones de diseño descartadas y tareas pendientes.

Explica las modificaciones claramente, mantén los prototipos independientes y trabaja de manera incremental, esperando confirmación después de cada paso relevante.

**Comienza por inspeccionar el estado actual de Última Jugada y presentar un diagnóstico breve de los archivos, las escenas y los warnings pendientes, sin modificar código todavía.**