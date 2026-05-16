# Light Chase Prototype

Prototipo 3D en Unity centrado en una idea simple: **la luz del jugador atrae al enemigo**.

Recolectar estrellas te acerca a la salida, pero también aumenta tu brillo, deja un rastro más visible y hace que el perseguidor te detecte desde más lejos. El loop no trata de pelear; trata de administrar riesgo, ruta y exposición.

## Resumen rápido

- Motor: Unity `6000.4.3f1`
- Render Pipeline: `URP`
- Input: `Unity Input System`
- Navegación enemiga: `Unity AI Navigation` con `NavMeshAgent`
- Base del personaje: `Starter Assets Third Person Controller`
- Escena principal: `Assets/Scenes/LightChasePrototype.unity`

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
- El jugador empieza con vidas, score y tiempo límite administrados por `PrototypeLevelManager`.

Valores actuales en código:

- `baseBrightness = 0.45`
- `brightnessPerStar = 0.45`
- `maximumBrightness = 3.6`
- `startingLives = 3`
- `levelTimeSeconds = 180`

### Enemigo

- Persigue al jugador cuando su firma de luz entra dentro de su rango de detección.
- Aumenta su velocidad durante la persecución.
- Emite audio de advertencia por proximidad.
- Hace daño por contacto si alcanza al jugador.

Valores actuales en código:

- `lightSignatureMultiplier = 1.85`
- `maximumDetectionRange = 20`
- `baseMoveSpeed = 2`
- `chaseMoveSpeed = 3.75`
- `contactDamageRange = 1.65`

La firma de luz actual crece con esta idea:

`signatureRange = brightness * brightness * multiplier`

### Objetivo del nivel

- Hay `7` estrellas colocadas por el builder.
- La salida se desbloquea al recolectar `5`.
- Si se acaban las vidas o el tiempo, la partida termina.

## Cómo abrir el proyecto

1. Abre Unity Hub.
2. Usa la versión `6000.4.3f1`.
3. Agrega esta carpeta como proyecto.
4. Abre la escena `Assets/Scenes/LightChasePrototype.unity`.

## Cómo reconstruir la escena prototipo

El proyecto incluye un builder para regenerar la escena y sus piezas base:

- Archivo: `Assets/Editor/LightChasePrototypeBuilder.cs`
- Menú de Unity: `Tools > Prototype > Build Light Chase Level`

Ese builder:

- crea o abre la escena prototipo
- configura jugador, luz y trail
- crea estrellas
- crea enemigo
- crea portal de salida
- crea HUD
- construye el `NavMesh`

Si vas a cambiar layout, actores o tuning base del prototipo, revisa primero el builder para no dejar la escena y la automatización desalineadas.

## Scripts clave

### Gameplay

- `Assets/Scripts/Gameplay/PlayerLightState.cs`: brillo del jugador y feedback visual.
- `Assets/Scripts/Gameplay/StarPickup.cs`: recolección de estrellas.
- `Assets/Scripts/Gameplay/EnemyLightSeeker.cs`: detección, persecución, warning y daño del enemigo.
- `Assets/Scripts/Gameplay/LightChaseMath.cs`: fórmulas de brillo y detección.
- `Assets/Scripts/Gameplay/PrototypeLevelManager.cs`: progreso, vidas, score, timer y estado de partida.
- `Assets/Scripts/Gameplay/ExitPortal.cs`: desbloqueo y finalización del nivel.

### UI

- `Assets/Scripts/UI/GameHudController.cs`: HUD de vidas, score, tiempo y estado.
- `Assets/Scripts/UI/MainMenuController.cs`: lógica básica de menú/instrucciones.

## Validación manual recomendada

Después de cualquier cambio de gameplay, valida mínimo esto en Play Mode:

1. El jugador puede recolectar estrellas.
2. El brillo cambia visualmente en la luz y el trail.
3. El enemigo detecta antes cuando el jugador brilla más.
4. El HUD refleja vidas, score, tiempo y estado.
5. El portal se desbloquea al llegar al requisito de estrellas.
6. El nivel termina correctamente por salida, daño o tiempo.

## Tests

Hay pruebas ya creadas en `Assets/Tests`:

- `Assets/Tests/Editor/LightChaseMathTests.cs`
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
