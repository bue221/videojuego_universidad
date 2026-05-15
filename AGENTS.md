# AGENTS.md

## Propósito
Este archivo sirve como base de contexto para modelos de AI que trabajen sobre este repositorio. La prioridad es preservar la fantasía principal del prototipo, entender qué ya existe en código y evitar cambios que desalineen la dirección del juego.

## Resumen del juego
- Nombre de trabajo: `Light Chase Prototype`
- Género actual: prototipo 3D de exploración y evasión
- Motor: Unity `6000.4.3f1`
- Render pipeline: `URP`
- Input: `Unity Input System`
- Navegación enemiga: `Unity AI Navigation` con `NavMeshAgent`
- Base de movimiento del jugador: `Starter Assets Third Person Controller`

## Fantasía principal
La idea central del juego es:

> La luz atrae al enemigo.

Las estrellas no solo dan progreso. También hacen al jugador más visible y más riesgoso.

Eso crea una decisión constante:
- recoger estrellas rápido para desbloquear la salida
- o jugar más seguro para no atraer al enemigo demasiado pronto

La tensión buscada no viene de pelear, sino de administrar visibilidad, ruta y riesgo.

## Loop principal
El loop actual del prototipo es:

1. Explorar el nivel.
2. Recolectar estrellas.
3. Aumentar brillo y rastro del jugador.
4. Hacer que el enemigo detecte al jugador desde más lejos.
5. Escapar o reposicionarse.
6. Reunir suficientes estrellas para desbloquear la salida.
7. Llegar al portal de salida.

## Mecánicas implementadas hoy

### 1. Brillo del jugador
Archivo clave: [PlayerLightState.cs](/Users/bue221/Documents/Estudio/videojuego_universidad/Assets/Scripts/Gameplay/PlayerLightState.cs)

- El jugador tiene un brillo base.
- Cada estrella recolectada aumenta el brillo.
- El brillo tiene límite máximo.
- El brillo modifica una `Light` del jugador.
- El brillo también modifica un `TrailRenderer`.
- A mayor brillo, mayor rango visual y rastro más notorio.

Valores actuales en código:
- `baseBrightness = 0.75`
- `brightnessPerStar = 0.35`
- `maximumBrightness = 3.0`

### 2. Estrellas coleccionables
Archivo clave: [StarPickup.cs](/Users/bue221/Documents/Estudio/videojuego_universidad/Assets/Scripts/Gameplay/StarPickup.cs)

- Las estrellas rotan y flotan.
- Al tocar una estrella, el jugador gana brillo.
- Cada estrella recolectada también incrementa el contador del nivel.
- La estrella desaparece después de recolectarse.

### 3. Enemigo atraído por la luz
Archivo clave: [EnemyLightSeeker.cs](/Users/bue221/Documents/Estudio/videojuego_universidad/Assets/Scripts/Gameplay/EnemyLightSeeker.cs)

- El enemigo mide distancia al jugador.
- El rango de detección depende del brillo actual del jugador.
- Si el jugador entra en ese rango, el enemigo entra en persecución.
- Mientras persigue, aumenta su velocidad.
- Usa `NavMeshAgent` para navegar hacia el jugador.

Valores actuales en código:
- `baseDetectionRange = 4.0`
- `brightnessRangeMultiplier = 4.0`
- `baseMoveSpeed = 2.75`
- `chaseMoveSpeed = 5.5`

La fórmula actual vive en [LightChaseMath.cs](/Users/bue221/Documents/Estudio/videojuego_universidad/Assets/Scripts/Gameplay/LightChaseMath.cs):

`detectionRange = baseRange + (brightness * brightnessMultiplier)`

### 4. Salida bloqueada por progreso
Archivos clave:
- [PrototypeLevelManager.cs](/Users/bue221/Documents/Estudio/videojuego_universidad/Assets/Scripts/Gameplay/PrototypeLevelManager.cs)
- [ExitPortal.cs](/Users/bue221/Documents/Estudio/videojuego_universidad/Assets/Scripts/Gameplay/ExitPortal.cs)

- La salida se desbloquea al reunir suficientes estrellas.
- El portal cambia visualmente entre estado bloqueado y desbloqueado.
- Hoy el requisito está configurado en `5` estrellas.
- Cuando el jugador entra al portal con el requisito cumplido, el sistema registra finalización del nivel por `Debug.Log`.

## Escena y generación del prototipo

### Escena principal
- [LightChasePrototype.unity](/Users/bue221/Documents/Estudio/videojuego_universidad/Assets/Scenes/LightChasePrototype.unity)

### Builder/editor tooling
- [LightChasePrototypeBuilder.cs](/Users/bue221/Documents/Estudio/videojuego_universidad/Assets/Editor/LightChasePrototypeBuilder.cs)

Este builder:
- crea o abre la escena prototipo
- configura jugador, luz y trail
- crea estrellas en posiciones fijas
- crea enemigo
- crea portal de salida
- construye el `NavMesh`

Esto significa que, antes de rehacer contenido manualmente, un modelo debe revisar si el cambio conviene hacerse en el builder para mantener consistencia del prototipo.

## Intención de diseño
Los cambios futuros deben reforzar estas ideas:

- Más progreso debe implicar más peligro.
- La información visual del jugador debe comunicar riesgo.
- La recolección nunca debe sentirse gratis.
- La persecución debe ser legible y entendible por el jugador.
- La salida debe representar alivio después de una fase de tensión.

## Lo que un modelo NO debe asumir
- No asumir que es un juego de combate.
- No asumir que las estrellas son solo score.
- No asumir que “más poder” significa una ventaja pura.
- No asumir que el objetivo es eliminar al enemigo.
- No reemplazar la lógica de riesgo/recompensa por power-ups que anulen la tensión central.

## Reglas para contribuir con AI

### Prioridades
1. Preservar la mecánica central: la luz atrae al enemigo.
2. Mantener coherencia entre diseño, escena y scripts.
3. Preferir cambios pequeños e iterables sobre reescrituras grandes.
4. Si una mecánica nueva altera riesgo o visibilidad, explicitar su impacto en el loop.

### Antes de hacer cambios
Un modelo debe revisar mínimo:
- [Assets/Scripts/Gameplay](/Users/bue221/Documents/Estudio/videojuego_universidad/Assets/Scripts/Gameplay)
- [Assets/Editor/LightChasePrototypeBuilder.cs](/Users/bue221/Documents/Estudio/videojuego_universidad/Assets/Editor/LightChasePrototypeBuilder.cs)
- [Packages/manifest.json](/Users/bue221/Documents/Estudio/videojuego_universidad/Packages/manifest.json)
- [ProjectSettings/ProjectVersion.txt](/Users/bue221/Documents/Estudio/videojuego_universidad/ProjectSettings/ProjectVersion.txt)

### Convenciones recomendadas
- Mantener gameplay code dentro de `Assets/Scripts/Gameplay`.
- Mantener editor tooling en `Assets/Editor`.
- Preferir clases pequeñas con una sola responsabilidad.
- Exponer tuning con `SerializeField` en lugar de hardcodear valores cuando tenga sentido de diseño.
- Si se cambia balance, documentar el efecto esperado sobre tensión, persecución y pacing.

### Validación mínima
Después de cambios de gameplay, un modelo debería validar:
- que el jugador aún puede recolectar estrellas
- que el brillo cambia visualmente
- que el enemigo detecta más rápido con mayor brillo
- que el portal se desbloquea con el número correcto de estrellas
- que la escena principal sigue funcional en Play Mode

## Oportunidades claras de evolución
Estas ideas son compatibles con la dirección actual:
- esconderse temporalmente en zonas oscuras
- estrellas con distinto valor y distinto castigo de visibilidad
- enemigos con comportamientos escalonados según brillo
- rutas alternativas con más recompensa y más exposición
- feedback UI/FX que anticipe “estás brillando demasiado”

## Riesgos de diseño
Un modelo debe tener cuidado con:
- subir demasiado el brillo por estrella y volver la partida injusta
- hacer al enemigo demasiado rápido y cancelar margen de decisión
- llenar el nivel de estrellas sin crear rutas y tradeoffs
- introducir sistemas que opaquen la claridad del loop principal

## Estado actual del proyecto
Este repositorio parece ser un prototipo funcional centrado en validar una mecánica principal, no un juego completo todavía. La meta de corto plazo debería ser profundizar la tensión del loop antes de expandir demasiado contenido o complejidad sistémica.

## Instrucción para futuros modelos
Si vas a trabajar aquí, parte de esta premisa:

> Este proyecto no trata sobre “coleccionar por coleccionar”. Trata sobre cómo el progreso vuelve al jugador más visible, más potente visualmente y más perseguible.

Toda propuesta debe responder:
- qué añade al loop principal
- cómo afecta el balance riesgo/recompensa
- cómo se refleja en la fantasía de luz, persecución y escape
