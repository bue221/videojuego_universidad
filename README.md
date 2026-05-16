# Light Chase Prototype

Prototipo 3D en Unity centrado en una idea simple: **la luz del jugador atrae al enemigo**.

Recolectar estrellas te acerca a la salida, pero también aumenta tu brillo, deja un rastro más visible y hace que el perseguidor te detecte desde más lejos. El loop no trata de pelear; trata de administrar riesgo, ruta y exposición mientras administras vidas, tiempo y presión creciente.

## Resumen rápido

- Motor: Unity `6000.4.3f1`
- Render Pipeline: `URP`
- Input: `Unity Input System`
- Navegación enemiga: `Unity AI Navigation` con `NavMeshAgent`
- Base del personaje: `Starter Assets Third Person Controller`
- Escena versionada actual: `Assets/Scenes/LightChasePrototype.unity`
- Escena opcional generable por builder: `Assets/Scenes/MainMenu.unity`

## Fantasía del prototipo

Este proyecto valida una tensión central:

1. Exploras el nivel.
2. Recolectas estrellas.
3. Tu brillo aumenta.
4. El enemigo se vuelve más sensible a tu presencia.
5. Tomas decisiones entre progreso rápido y exposición.
6. Desbloqueas la salida.
7. Escapas.

La pregunta que debe sostener cada cambio es: **¿más progreso también implica más peligro?**

## Estado actual del gameplay

### Jugador

- Cada estrella aumenta el brillo del jugador.
- El brillo modifica una `Light` y un `TrailRenderer`.
- El jugador puede usar uno de dos avatares seleccionables desde menú: `Humano` o `Capsula`.
- `PrototypeLevelManager` administra vidas, score, tiempo límite y estado de la partida.
- El `HUD` comunica objetivo, timer, vidas y estado de tensión.

Valores actuales en código:

- `baseBrightness = 0.45`
- `brightnessPerStar = 0.45`
- `maximumBrightness = 3.6`
- `startingLives = 3`
- `levelTimeSeconds = 180`
- `scorePerStar = 100`

### Enemigo

- Persigue al jugador cuando su firma de luz entra dentro de su rango de detección.
- Aumenta su velocidad durante la persecución según el brillo actual del jugador.
- Emite audio de advertencia antes y durante la detección.
- Cambia de color para indicar estado pasivo, alerta o persecución.
- Hace daño por contacto si alcanza al jugador.

Valores actuales en código:

- `lightSignatureMultiplier = 1.85`
- `maximumDetectionRange = 20`
- `baseMoveSpeed = 1.35`
- `chaseMoveSpeed = 2.25`
- `contactDamageRange = 1.65`
- `damageInterval = 1.2`
- `preDetectionWarningPadding = 5.5`

La firma de luz actual crece con esta fórmula:

`signatureRange = brightness * brightness * multiplier`

### Objetivo del nivel

- Hay `7` estrellas colocadas por el builder.
- La salida se desbloquea al recolectar `5`.
- Si se acaban las vidas o el tiempo, la partida termina.
- El portal cambia de color entre bloqueado y desbloqueado.

### Presentación y atmósfera

- `LightChaseAtmosphere` fuerza una lectura nocturna con niebla, ambiente oscuro y luz direccional mínima.
- El prototipo asegura un overlay de menú principal y un `HUD` en runtime.
- El menú principal refuerza el framing académico del proyecto y permite:
  - jugar
  - leer instrucciones
  - elegir avatar
  - salir

## Cómo abrir el proyecto

1. Abre Unity Hub.
2. Usa la versión `6000.4.3f1`.
3. Agrega esta carpeta como proyecto.
4. Abre la escena `Assets/Scenes/LightChasePrototype.unity`.

## Cómo correrlo

1. Abre `Assets/Scenes/LightChasePrototype.unity`.
2. Presiona `Play` en Unity.
3. El proyecto asegura el overlay de menú principal también desde runtime, así que puedes elegir avatar y pulsar `Jugar` sin salir de la escena jugable.
4. Si prefieres una escena dedicada de menú, primero genera `Assets/Scenes/MainMenu.unity` con `Tools > Prototype > Build Main Menu`.
5. Al comenzar la partida, el mouse queda tomado por el controlador third-person, así que la cámara se maneja directamente con movimiento del mouse.

## Controles del jugador

- `WASD`: moverte
- `Mouse`: girar cámara y orientación
- `Shift izquierdo`: correr
- `Espacio`: saltar

Estos inputs vienen del `Starter Assets Third Person Controller` conectado al `Unity Input System`.

## Cómo se juega

1. Explora el escenario.
2. Recoge estrellas para acercarte a la meta.
3. Observa cómo sube tu brillo y tu rastro visual.
4. Evalúa si seguir recolectando o escapar antes de quedar demasiado expuesto.
5. Activa el portal juntando `5` estrellas.
6. Llega a la salida antes de perder todas tus vidas o de quedarte sin tiempo.

## Qué hace el enemigo

- El enemigo responde a tu luz, no a combate ni a score.
- Entre más brillo lleves, desde más lejos puede detectarte.
- Cuando entras en su rango, cambia a persecución y acelera.
- Si te alcanza, hace daño por contacto y te baja vidas.
- También tiene señales de alerta audiovisuales antes de la persecución completa, así que el jugador puede leer el peligro y reaccionar.

La intención del loop es que el enemigo convierta el progreso en presión, no que sea un obstáculo aleatorio.

## Cómo reconstruir la escena prototipo

El proyecto incluye un builder para regenerar la escena y sus piezas base:

- Archivo: `Assets/Editor/LightChasePrototypeBuilder.cs`
- Menú de Unity: `Tools > Prototype > Build Light Chase Level`
- Menú auxiliar de atmósfera: `Tools > Prototype > Apply Suspense Atmosphere`

Ese builder:

- crea o abre la escena prototipo
- configura jugador, luz y trail
- aplica atmósfera nocturna
- asegura `PrototypeLevelManager`
- crea `7` estrellas
- crea enemigo
- crea portal de salida
- crea HUD
- construye el `NavMesh`

Si vas a cambiar layout, actores o tuning base del prototipo, revisa primero el builder para no dejar la escena y la automatización desalineadas.

## Cómo reconstruir el menú principal

El proyecto también incluye un builder para el menú:

- Archivo: `Assets/Editor/LightChaseMainMenuBuilder.cs`
- Menú de Unity: `Tools > Prototype > Build Main Menu`

Ese builder crea una escena de menú con:

- cámara ortográfica base
- overlay UI principal
- selección de avatar
- acceso a instrucciones
- transición a la escena `LightChasePrototype`

## Scripts clave

### Gameplay

- `Assets/Scripts/Gameplay/PlayerLightState.cs`: brillo del jugador y feedback visual.
- `Assets/Scripts/Gameplay/PlayerAvatarSetup.cs`: presentación jugable del avatar, trail, glow y binding de cámara.
- `Assets/Scripts/Gameplay/PlayerAvatarSelection.cs`: catálogo y persistencia del avatar seleccionado.
- `Assets/Scripts/Gameplay/StarPickup.cs`: recolección de estrellas.
- `Assets/Scripts/Gameplay/EnemyLightSeeker.cs`: detección, warning, persecución y daño por contacto.
- `Assets/Scripts/Gameplay/LightChaseMath.cs`: fórmulas de brillo, firma de luz y warning.
- `Assets/Scripts/Gameplay/PrototypeLevelManager.cs`: progreso, vidas, score, timer, HUD y estado de partida.
- `Assets/Scripts/Gameplay/ExitPortal.cs`: desbloqueo y finalización del nivel.
- `Assets/Scripts/Gameplay/LightChaseAtmosphere.cs`: ambientación visual nocturna para escena y cámaras.

### UI

- `Assets/Scripts/UI/GameHudController.cs`: HUD de vidas, score, tiempo y estado.
- `Assets/Scripts/UI/MainMenuController.cs`: menú principal, instrucciones, pausa y selección de avatar.

## Validación manual recomendada

Después de cualquier cambio de gameplay, valida mínimo esto en Play Mode:

1. El jugador puede recolectar estrellas.
2. El brillo cambia visualmente en la luz y el trail.
3. El enemigo detecta antes cuando el jugador brilla más.
4. El warning audiovisual del enemigo aumenta antes de la persecución.
5. El HUD refleja vidas, score, tiempo y estado.
6. El HUD muestra claramente los controles y el objetivo durante la partida.
7. El portal se desbloquea al llegar al requisito de estrellas.
8. El nivel termina correctamente por salida, daño o tiempo.
9. El menú permite elegir avatar y entrar al gameplay sin romper cámara o inputs.

## Tests

Hay pruebas ya creadas en `Assets/Tests`:

- `Assets/Tests/Editor/LightChaseMathTests.cs`
- `Assets/Tests/EditMode/GameHudControllerTests.cs`
- `Assets/Tests/EditMode/LightChaseAtmosphereTests.cs`
- `Assets/Tests/EditMode/PlayerAvatarSelectionTests.cs`
- `Assets/Tests/EditMode/PrototypeLevelManagerTests.cs`
- `Assets/Tests/EditMode/MainMenuControllerTests.cs`

Puedes correrlas desde el `Test Runner` de Unity en modos `EditMode` y `Editor`.

## Convenciones del repo

- Gameplay en `Assets/Scripts/Gameplay`
- UI en `Assets/Scripts/UI`
- Tooling/editor en `Assets/Editor`
- Prefiere exponer tuning con `SerializeField`
- Evita cambios que conviertan la progresión en ventaja gratuita

## Dirección de diseño

Este prototipo no trata sobre combate ni sobre coleccionar por score solamente.

La dirección correcta es reforzar:

- riesgo/recompensa
- legibilidad de la persecución
- feedback visual de exposición
- alivio al desbloquear y alcanzar la salida

Y evitar:

- power-ups que neutralicen la tensión central
- buffs que hagan trivial el enemigo
- cambios que separen progreso de visibilidad

## Nota para contribuir

Si vas a tocar balance o añadir una mecánica, deja explícito:

- qué aporta al loop principal
- cómo cambia el riesgo/recompensa
- cómo impacta la fantasía de luz, persecución y escape
