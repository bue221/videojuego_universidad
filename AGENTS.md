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
- Presentación adicional: `HUD`, menú principal y selección de avatar en runtime

## Fantasía principal
La idea central del juego es:

> La luz atrae al enemigo.

Las estrellas no solo dan progreso. También hacen al jugador más visible y más riesgoso.

Eso crea una decisión constante:
- recoger estrellas rápido para desbloquear la salida
- o jugar más seguro para no atraer al enemigo demasiado pronto

La tensión buscada no viene de pelear, sino de administrar visibilidad, ruta, riesgo y tiempo bajo presión.

## Loop principal
El loop actual del prototipo es:

1. Explorar el nivel.
2. Recolectar estrellas.
3. Aumentar brillo y rastro del jugador.
4. Hacer que el enemigo detecte al jugador desde más lejos.
5. Escuchar y leer señales de warning antes de ser perseguido.
6. Escapar o reposicionarse.
7. Reunir suficientes estrellas para desbloquear la salida.
8. Llegar al portal de salida antes de perder todas las vidas o el tiempo.

## Mecánicas implementadas hoy

### 1. Brillo del jugador
Archivo clave: [PlayerLightState.cs](/Users/bue221/Documents/Estudio/videojuego_universidad/Assets/Scripts/Gameplay/PlayerLightState.cs)

- El jugador tiene un brillo base.
- Cada estrella recolectada aumenta el brillo.
- El brillo tiene límite máximo.
- El brillo modifica una `Light` del jugador.
- El brillo también modifica un `TrailRenderer`.
- A mayor brillo, mayor rango visual y rastro más notorio.
- El brillo expone al jugador ante el enemigo mediante una firma de luz no lineal.

Valores actuales en código:
- `baseBrightness = 0.45`
- `brightnessPerStar = 0.45`
- `maximumBrightness = 3.6`

Complementos relevantes:
- [PlayerAvatarSetup.cs](/Users/bue221/Documents/Estudio/videojuego_universidad/Assets/Scripts/Gameplay/PlayerAvatarSetup.cs) asegura `GlowLight`, `GlowTrail` y binding de cámara.
- [PlayerAvatarSelection.cs](/Users/bue221/Documents/Estudio/videojuego_universidad/Assets/Scripts/Gameplay/PlayerAvatarSelection.cs) permite elegir entre `Humano` y `Capsula`.

### 2. Estrellas coleccionables
Archivo clave: [StarPickup.cs](/Users/bue221/Documents/Estudio/videojuego_universidad/Assets/Scripts/Gameplay/StarPickup.cs)

- Las estrellas rotan y flotan.
- Al tocar una estrella, el jugador gana brillo.
- Cada estrella recolectada también incrementa progreso y `score` del nivel.
- La estrella desaparece después de recolectarse.
- Cada estrella tiene una `Light` propia con pulsación suave.

### 3. Enemigo atraído por la luz
Archivo clave: [EnemyLightSeeker.cs](/Users/bue221/Documents/Estudio/videojuego_universidad/Assets/Scripts/Gameplay/EnemyLightSeeker.cs)

- El enemigo mide distancia al jugador.
- El rango de detección depende de la firma de luz actual del jugador.
- Si el jugador entra en ese rango, el enemigo entra en persecución.
- Antes de detectar, el enemigo ya puede emitir warning audiovisual por cercanía al umbral.
- Mientras persigue, aumenta su velocidad según el brillo normalizado del jugador.
- Hace daño por contacto en intervalos.
- Usa `NavMeshAgent` para navegar hacia el jugador.
- Cambia de color para indicar estado pasivo, alerta o persecución.

Valores actuales en código:
- `lightSignatureMultiplier = 1.85`
- `maximumDetectionRange = 20`
- `baseMoveSpeed = 1.35`
- `chaseMoveSpeed = 2.25`
- `contactDamageRange = 1.65`
- `damageInterval = 1.2`
- `preDetectionWarningPadding = 5.5`

La fórmula actual vive en [LightChaseMath.cs](/Users/bue221/Documents/Estudio/videojuego_universidad/Assets/Scripts/Gameplay/LightChaseMath.cs):

`signatureRange = brightness * brightness * multiplier`

### 4. Salida bloqueada por progreso
Archivos clave:
- [PrototypeLevelManager.cs](/Users/bue221/Documents/Estudio/videojuego_universidad/Assets/Scripts/Gameplay/PrototypeLevelManager.cs)
- [ExitPortal.cs](/Users/bue221/Documents/Estudio/videojuego_universidad/Assets/Scripts/Gameplay/ExitPortal.cs)

- La salida se desbloquea al reunir suficientes estrellas.
- El portal cambia visualmente entre estado bloqueado y desbloqueado.
- Hoy el requisito está configurado en `5` estrellas.
- Cuando el jugador entra al portal con el requisito cumplido, el sistema marca finalización del nivel.

### 5. Estado de partida y UI
Archivos clave:
- [PrototypeLevelManager.cs](/Users/bue221/Documents/Estudio/videojuego_universidad/Assets/Scripts/Gameplay/PrototypeLevelManager.cs)
- [GameHudController.cs](/Users/bue221/Documents/Estudio/videojuego_universidad/Assets/Scripts/UI/GameHudController.cs)
- [MainMenuController.cs](/Users/bue221/Documents/Estudio/videojuego_universidad/Assets/Scripts/UI/MainMenuController.cs)

- El prototipo ya no valida solo estrellas y portal.
- También administra `vidas`, `score`, `timer`, `game over`, `level completed` y mensajes de estado.
- El `HUD` se asegura en runtime y comunica objetivo, peligro y controles.
- El menú principal puede pausar gameplay, mostrar instrucciones, lanzar la escena jugable y seleccionar avatar.

Valores actuales en código:
- `startingLives = 3`
- `scorePerStar = 100`
- `levelTimeSeconds = 180`

## Escena y generación del prototipo

### Escena principal
- [LightChasePrototype.unity](/Users/bue221/Documents/Estudio/videojuego_universidad/Assets/Scenes/LightChasePrototype.unity)

### Escena de menú opcional
- `MainMenu.unity` puede generarse con [LightChaseMainMenuBuilder.cs](/Users/bue221/Documents/Estudio/videojuego_universidad/Assets/Editor/LightChaseMainMenuBuilder.cs), pero no está versionada hoy en el repo.

### Builder/editor tooling
- [LightChasePrototypeBuilder.cs](/Users/bue221/Documents/Estudio/videojuego_universidad/Assets/Editor/LightChasePrototypeBuilder.cs)
- [LightChaseMainMenuBuilder.cs](/Users/bue221/Documents/Estudio/videojuego_universidad/Assets/Editor/LightChaseMainMenuBuilder.cs)

Este builder:
- crea o abre la escena prototipo
- configura jugador, luz y trail
- aplica la atmósfera nocturna
- asegura `PrototypeLevelManager`
- crea `7` estrellas en posiciones fijas
- crea enemigo
- crea portal de salida
- crea HUD
- construye el `NavMesh`

Esto significa que, antes de rehacer contenido manualmente, un modelo debe revisar si el cambio conviene hacerse en el builder para mantener consistencia del prototipo.

El builder de menú:
- crea una escena vacía de `MainMenu`
- arma overlay, instrucciones y botones
- permite seleccionar avatar antes de jugar

## Intención de diseño
Los cambios futuros deben reforzar estas ideas:

- Más progreso debe implicar más peligro.
- La información visual del jugador debe comunicar riesgo.
- La recolección nunca debe sentirse gratis.
- La persecución debe ser legible y entendible por el jugador.
- El warning previo a la persecución debe ayudar a anticipar el riesgo.
- La salida debe representar alivio después de una fase de tensión.
- Vidas y tiempo deben sumar presión, no reemplazar la fantasía principal.

## Lo que un modelo NO debe asumir
- No asumir que es un juego de combate.
- No asumir que las estrellas son solo score.
- No asumir que “más poder” significa una ventaja pura.
- No asumir que el objetivo es eliminar al enemigo.
- No asumir que vidas, timer o HUD son el loop central; son soportes de tensión.
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
- [Assets/Scripts/UI](/Users/bue221/Documents/Estudio/videojuego_universidad/Assets/Scripts/UI)
- [Assets/Editor/LightChasePrototypeBuilder.cs](/Users/bue221/Documents/Estudio/videojuego_universidad/Assets/Editor/LightChasePrototypeBuilder.cs)
- [Assets/Editor/LightChaseMainMenuBuilder.cs](/Users/bue221/Documents/Estudio/videojuego_universidad/Assets/Editor/LightChaseMainMenuBuilder.cs)
- [Packages/manifest.json](/Users/bue221/Documents/Estudio/videojuego_universidad/Packages/manifest.json)
- [ProjectSettings/ProjectVersion.txt](/Users/bue221/Documents/Estudio/videojuego_universidad/ProjectSettings/ProjectVersion.txt)

### Convenciones recomendadas
- Mantener gameplay code dentro de `Assets/Scripts/Gameplay`.
- Mantener UI runtime en `Assets/Scripts/UI`.
- Mantener editor tooling en `Assets/Editor`.
- Preferir clases pequeñas con una sola responsabilidad.
- Exponer tuning con `SerializeField` en lugar de hardcodear valores cuando tenga sentido de diseño.
- Si se cambia balance, documentar el efecto esperado sobre tensión, persecución y pacing.

### Validación mínima
Después de cambios de gameplay, un modelo debería validar:
- que el jugador aún puede recolectar estrellas
- que el brillo cambia visualmente
- que el enemigo detecta más rápido con mayor brillo
- que el warning audiovisual del enemigo responde a la cercanía
- que el `HUD` refleja vidas, score, tiempo y estado
- que el portal se desbloquea con el número correcto de estrellas
- que el menú principal permite elegir avatar y entrar al gameplay
- que la escena principal sigue funcional en Play Mode

## Oportunidades claras de evolución
Estas ideas son compatibles con la dirección actual:
- esconderse temporalmente en zonas oscuras
- estrellas con distinto valor y distinto castigo de visibilidad
- enemigos con comportamientos escalonados según brillo
- rutas alternativas con más recompensa y más exposición
- feedback UI/FX que anticipe “estás brillando demasiado”
- variantes de avatar puramente de presentación sin romper legibilidad ni balance

## Riesgos de diseño
Un modelo debe tener cuidado con:
- subir demasiado el brillo por estrella y volver la partida injusta
- hacer al enemigo demasiado rápido y cancelar margen de decisión
- volver el warning tan ruidoso que opaque la claridad del espacio
- llenar el nivel de estrellas sin crear rutas y tradeoffs
- dejar desalineados builder, escena manual y UI runtime
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
- cómo impacta la legibilidad del warning, la presión del tiempo y la claridad del estado de partida
