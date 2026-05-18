---
name: logica-global-light-chase
description: Canon obligatorio del gameplay base para que cualquier cambio preserve la logica de progreso, persecucion, derrota y reinicio.
---

# Skill: Logica Global Light Chase

Esta skill define la logica base obligatoria de `Light Chase Prototype`.

No importa si el nivel es bosque, agua, playa, ruinas o cualquier otro terreno:

> las reglas centrales del juego no cambian.

Si una propuesta rompe estas reglas, debe corregirse o rechazarse.

## Cuando usar esta skill

Activa esta skill cuando el usuario pida o implique cambios en:

- reglas del juego
- gameplay base
- estrellas o luces
- portal de salida
- vidas
- tiempo limite
- derrota o reinicio
- respawn
- enemigo
- audio de alerta o persecucion
- HUD de partida
- flow de ganar o perder
- logica comun entre niveles

## Objetivo

Asegurar que todos los modelos de AI traten esta logica como canon del proyecto y no inventen reglas incompatibles entre niveles.

## Archivos que SIEMPRE debes revisar antes de cambiar esta logica

1. `AGENTS.md`
2. `Assets/Project/LightChasePrototype/Scripts/Gameplay/PrototypeLevelManager.cs`
3. `Assets/Project/LightChasePrototype/Scripts/Gameplay/GameSessionManager.cs`
4. `Assets/Project/LightChasePrototype/Scripts/Gameplay/PlayerLightState.cs`
5. `Assets/Project/LightChasePrototype/Scripts/Gameplay/StarPickup.cs`
6. `Assets/Project/LightChasePrototype/Scripts/Gameplay/EnemyLightSeeker.cs`
7. `Assets/Project/LightChasePrototype/Scripts/Gameplay/ExitPortal.cs`
8. `Assets/Project/LightChasePrototype/Scripts/UI/GameHudController.cs`
9. `Assets/Project/LightChasePrototype/Scripts/UI/MainMenuController.cs`
10. `Assets/Project/LightChasePrototype/Scripts/UI/GlobalUiController.cs`
11. `Assets/Project/LightChasePrototype/Scripts/Gameplay/LightChaseLevelCatalog.cs`
12. `Assets/Project/LightChasePrototype/Editor/LightChasePrototypeBuilder.cs`
13. `Assets/Project/LightChasePrototype/Editor/LightChaseNatureLevelBuilder.cs`
14. `Assets/Project/LightChasePrototype/Editor/LightChaseWaterLevelBuilder.cs`

## Regla madre

La fantasia central del proyecto siempre es esta:

> progresar te hace mas visible, mas tenso y mas perseguible.

Ningun sistema nuevo debe borrar, suavizar o contradecir esa idea.

## Reglas invariantes del juego

Estas reglas son obligatorias en todos los niveles actuales y futuros:

- el jugador siempre debe poder recorrer el nivel por rutas jugables validas
- el terreno puede cambiar movilidad, ritmo o riesgo, pero no puede volver imposible completar el nivel
- el objetivo principal siempre es recolectar `X` estrellas o luces para desbloquear el portal
- `X` puede variar por nivel, pero la regla de desbloqueo nunca cambia
- el portal siempre empieza bloqueado
- el jugador solo gana si entra al portal despues de cumplir el requisito
- el portal usa el mismo modelo Meshy de `Assets/MeshyImports/Portal` en todos los niveles
- el portal siempre es atravesable: el contacto se resuelve por un `Collider` con `isTrigger=true` y nunca debe bloquear fisicamente al jugador
- el juego no es de combate; el enemigo existe para perseguir, alertar y castigar la exposicion
- recoger progreso nunca debe sentirse gratis; mas progreso debe implicar mas peligro o mas presion

## Reglas de progreso

- estrellas y luces cuentan como el recurso principal de progreso del nivel
- el progreso recolectado debe ser legible para el jugador en todo momento
- el portal no se puede activar por shortcuts, triggers ocultos o condiciones distintas a la recoleccion requerida
- si un nivel agrega variantes de coleccionables, debe seguir siendo obvio cuales cuentan para abrir el portal

## Reglas de derrota por enemigo

- si el enemigo alcanza al jugador, el jugador pierde 1 vida
- debe aparecer un mensaje claro indicando que fue atrapado
- si aun quedan vidas, el jugador reaparece en el punto inicial del nivel
- al reaparecer, pierde todas las estrellas o luces recolectadas en esa corrida
- al reaparecer, tambien se reinician brillo, score de la corrida y estado de portal desbloqueado
- las estrellas o luces recolectadas deben reaparecer fisicamente en el nivel
- el tiempo restante no se reinicia al perder una vida
- si las vidas llegan a 0, la partida termina de inmediato

## Reglas de derrota por tiempo

- si el tiempo llega a `0`, el jugador pierde automaticamente
- debe aparecer un mensaje claro indicando que la derrota fue por tiempo agotado
- el tiempo es presion global de la partida, no un reemplazo de la fantasia de luz y persecucion

## Reglas de UI obligatoria

El HUD o la UI runtime deben mostrar como minimo:

- vidas restantes
- progreso de estrellas o luces
- tiempo restante
- estado general de la partida

Cuando el jugador pierde un nivel, la pantalla de derrota debe incluir siempre:

- un boton de `Reintentar`
- un boton de `Salir al menu principal`

Reglas del flow de derrota:

- `Reintentar` reinicia la corrida del mismo nivel con estado limpio
- `Salir al menu principal` vuelve al menu sin arrastrar progreso roto de la partida perdida
- la UI de derrota debe diferenciar si la causa fue enemigo o tiempo agotado

## Reglas de audio del enemigo

El audio del enemigo no es decorativo. Es una herramienta de lectura del peligro.

Debe existir una fase de percepcion:

- cuando el enemigo empieza a percibir la luz del jugador, debe sonar una alerta
- esa alerta debe escalar segun cercania al umbral de deteccion, brillo del jugador o ambos
- si el enemigo aun no persigue, el sonido debe advertir claramente que el riesgo esta subiendo

Debe existir una fase de persecucion:

- cuando el enemigo ya detecta al jugador y empieza la persecucion, debe sonar un audio constante
- el audio de persecucion debe sentirse mas urgente, intenso y alarmante que la fase de percepcion
- el cambio entre percepcion y persecucion debe ser claro para el jugador

Reglas de calidad para esa alerta:

- el warning debe ayudar antes de morir, no solo confirmar que ya es tarde
- el sonido puede cambiar de estilo por nivel, pero la logica de escalado y persecucion constante debe mantenerse
- el audio no debe ser tan ruidoso que opaque la lectura del espacio o canse demasiado rapido

## Reglas para el terreno y variaciones por nivel

Esto SI puede variar por nivel:

- cantidad total de estrellas o luces
- cantidad requerida para abrir portal
- layout del mapa
- densidad de cobertura
- zonas de agua, barro, pendientes o superficies especiales
- tuning de presion, warning y dificultad
- estilo visual del enemigo y de la alerta sonora

Esto NO puede variar:

- el modelo visual del portal (siempre el asset Meshy compartido)
- el portal atravesable por trigger (nunca bloquear al jugador)
- la logica de portal bloqueado por progreso
- la derrota por tiempo agotado
- la perdida de vida por captura del enemigo
- el respawn en el inicio cuando aun quedan vidas
- la perdida del progreso recolectado en esa corrida
- la existencia de opciones de `Reintentar` y `Salir al menu principal` al perder
- la presencia de warning sonoro antes y durante la persecucion

## Reglas de diseno que los modelos NO deben violar

- no asumir que es un juego de combate
- no asumir que las estrellas son solo score
- no convertir el progreso en una ventaja gratuita sin costo de exposicion
- no quitar presion del enemigo al punto de volver irrelevante la fantasia principal
- no proponer sistemas que opaquen la claridad de riesgo, warning, captura y escape

## Checklist de implementacion cuando se toque esta logica

1. Revisar scripts base, HUD y flujo de menu.
2. Identificar claramente estados de partida: jugando, alerta, persecucion, atrapado, game over, victoria.
3. Verificar que la derrota por enemigo y por tiempo tengan mensajes diferenciados.
4. Verificar que el reinicio limpie progreso de corrida.
5. Verificar que el portal vuelva a bloqueado tras perder una vida o reintentar.
6. Verificar que las estrellas reaparezcan si la corrida se reinicia.
7. Verificar que el audio de percepcion escale antes de la persecucion.
8. Verificar que el audio de persecucion se mantenga constante mientras el enemigo persigue.
9. Verificar que el menu principal y el reintento carguen el estado correcto.

## Validacion minima obligatoria

Antes de cerrar cualquier cambio relacionado con esta skill, debes comprobar:

1. El jugador puede recorrer el nivel por rutas jugables validas.
2. El jugador recolecta estrellas o luces y el progreso se refleja en UI.
3. El portal sigue bloqueado hasta cumplir el requisito.
4. Al ser atrapado por el enemigo, el jugador pierde una vida y reaparece al inicio.
5. Al reaparecer, el progreso recolectado de esa corrida se pierde y reaparece en el mapa.
6. Si el tiempo llega a cero, la partida termina automaticamente.
7. La derrota muestra `Reintentar` y `Salir al menu principal`.
8. El warning sonoro del enemigo escala antes de detectar.
9. El audio de persecucion se mantiene claro y urgente mientras persigue.
10. El flow completo sigue funcionando en todos los niveles afectados.

## Criterios de rechazo

Debes rechazar o rehacer cualquier propuesta si:

- rompe la regla de progreso para abrir portal
- conserva estrellas tras morir cuando no deberia
- reinicia tiempo al perder una vida sin justificacion explicita del usuario
- elimina el warning o deja al enemigo sin lectura sonora clara
- deja una derrota sin opcion de reintentar o volver al menu
- rompe el HUD base o vuelve ilegible el estado de la partida
- convierte el terreno en un bloqueo injusto o imposible

## Respuesta esperada al usar esta skill

Cuando apliques esta skill, responde con:

1. Que entendiste del cambio pedido
2. Que regla global del juego esta involucrada
3. Que partes del flujo de gameplay, UI o audio vas a tocar
4. Como validaras que sigue cumpliendo el canon

## Frase guia del proyecto

No estamos creando reglas sueltas, mka.
Estamos protegiendo un loop donde avanzar te expone, te delata y te obliga a escapar bajo presion.
