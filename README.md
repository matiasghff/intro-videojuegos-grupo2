# Ultima Jugada - Grupo 2

Versión alfa del proyecto de ICC0021 - Introducción al Desarrollo de Videojuegos. El juego combina un partido de 90 minutos simulados con doce oportunidades base, ataques encadenados de regate, pase y tiro, afinidad con compañeros y fatiga. La simulación permite velocidades x1, x1.5 y x2.

## Informe de la Entrega 4

El informe actualizado está en **[docs/Entrega4_Informe.pdf](docs/Entrega4_Informe.pdf)**. Describe las funcionalidades implementadas, decisiones de diseño, problemas resueltos, pruebas realizadas y propuestas para la versión final. Conserva la portada y el formato del documento original.

El PDF corresponde al checkpoint académico ya entregado. Los cambios posteriores de jugabilidad y gráficos están documentados en [docs/ACTUALIZACION_JUGABILIDAD.md](docs/ACTUALIZACION_JUGABILIDAD.md).

`docs/backups/Entrega4_Informe_original.pdf` es el respaldo anterior a la edición. El PDF de la raíz de `docs/` es el que corresponde entregar.

## Cómo navegar el repositorio

```text
intro-videojuegos-grupo2/
├── README.md                    # Punto de entrada al repositorio
├── handoff.md                   # Referencia histórica de los prototipos anteriores
├── docs/
│   ├── Entrega4_Informe.pdf      # Informe actualizado de la entrega
│   ├── GUIA_EJECUCION.md         # Controles, configuración, build y exportación
│   ├── ACTUALIZACION_JUGABILIDAD.md # Cambios posteriores al checkpoint entregado
│   ├── ARCHITECTURE.md           # Organización de los sistemas y del código
│   ├── FEATURE_CHECKLIST.md      # Los ocho requisitos y cómo comprobarlos
│   ├── TEST_PLAN.md              # Pruebas ejecutadas y validación manual pendiente
│   ├── validation/               # Resultados XML, resumen y capturas de pruebas
│   └── backups/                  # Respaldo del informe original
└── UnityProject/                 # Carpeta que se abre desde Unity Hub
    ├── Assets/
    │   ├── Scenes/               # MainMenu, Match y MatchResult
    │   ├── Scripts/              # Código del juego y herramientas de Editor
    │   ├── Materials/            # Materiales de la cancha
    │   ├── Resources/            # MatchConfig: parámetros del partido
    │   └── Tests/                # Pruebas EditMode y PlayMode
    ├── Packages/                 # Dependencias y sus versiones
    ├── ProjectSettings/          # Configuración compartida de Unity
    └── Builds/                   # Aplicación y paquetes generados; ignorados por Git
```

El `handoff.md` describe un proyecto anterior. Para el funcionamiento de esta alfa, consultar el código de `UnityProject/` y la documentación actual de `docs/`.

## Abrir y jugar

1. Instalar **Unity Editor 6000.5.10f1** desde Unity Hub. Para generar la aplicación, incluir el módulo de soporte macOS.
2. En Unity Hub, agregar la carpeta **UnityProject**, no la raíz del repositorio.
3. Esperar la importación y compilación. El proyecto usa URP e Input System.
4. Abrir `Assets/Scenes/MainMenu.unity` y presionar **Play**.
5. Seleccionar **JUGAR PARTIDO**. El flujo continúa hasta el resultado final y permite jugar otra vez.

Si faltan escenas después de importar el proyecto, ejecutar **Tools > Ultima Jugada > Generate or Repair Project**. Esta herramienta también configura MainMenu como inicio de Play.

| Acción | Control |
|---|---|
| Moverse durante el regate | WASD o flechas |
| Apuntar y elegir potencia | Clic en el balón y arrastrar hacia atrás |
| Ejecutar con precisión | Soltar el arrastre y clicar el centro del objetivo en movimiento antes de 2 segundos |
| Acelerar la simulación | Botón x1 / x1.5 / x2 del marcador; los controles conservan su ritmo |
| Continuar un regate exitoso | Elegir DAR UN PASE o TIRO AL ARCO inmediatamente |
| Avanzar tras una jugada o el descanso | CONTINUAR o esperar el avance automático |
| Reiniciar el partido | JUGAR OTRA VEZ en la pantalla final |

## Dónde está cada sistema

Las rutas de esta tabla son relativas a `UnityProject/Assets/Scripts/`.

| Carpeta | Responsabilidad |
|---|---|
| `Core/` | Estado persistente, configuración y navegación entre escenas |
| `Match/` | Reloj, simulación, eventos, estadísticas y coordinación de intervenciones |
| `Gameplay/` | Movimiento, defensores, pelota, pase, tiro y precisión |
| `Systems/` | Resistencia/fatiga y afinidad |
| `UI/` | Menú, HUD y pantalla de resultado |
| `Presentation/` | Estadio, iluminación, portería, balón y animación de futbolistas |
| `Editor/` | Generación/reparación, compilación y exportación de la entrega |

Las pruebas están en `UnityProject/Assets/Tests/`. Se pueden ejecutar desde **Window > General > Test Runner**. La evidencia y sus límites están explicados en [docs/TEST_PLAN.md](docs/TEST_PLAN.md).

## Compilar y preparar la entrega

- **Tools > Ultima Jugada > Build macOS** genera la aplicación universal Intel/Apple Silicon en `UnityProject/Builds/macOS/UltimaJugada.app` y verifica su firma local.
- **Tools > Ultima Jugada > Export Entrega 4** genera `UnityProject/Builds/Entrega4/` con el Asset Package, `ProjectSettings`, `Packages`, instrucciones de importación y el informe de `docs/` si está disponible junto al proyecto.
- Comprimir esa carpeta produce el ZIP de código para entregar. Desde `UnityProject/`:

  ```sh
  ditto -c -k --keepParent Builds/Entrega4 Builds/UltimaJugada-Entrega4.zip
  ```

El **ZIP de código** corresponde al paquete para el Drive del grupo; el **informe PDF** se entrega además por separado en el buzón del curso. La aplicación macOS es un archivo adicional para ejecutar la demostración.

Los archivos de `Builds/` se generan localmente y no se versionan. El repositorio debe conservar `Assets/` con sus `.meta`, `Packages/`, `ProjectSettings/` y `docs/`.

Más detalles en [la guía de ejecución](docs/GUIA_EJECUCION.md), [la arquitectura](docs/ARCHITECTURE.md) y [el checklist de funcionalidades](docs/FEATURE_CHECKLIST.md).
