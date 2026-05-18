---
name: crear-niveles-light-chase
description: Estandar para crear niveles nuevos o corregir niveles existentes en Light Chase Prototype.
---

# Skill: Crear Niveles Light Chase

Esta skill define como se deben construir los niveles de `Light Chase Prototype` para que todos conserven la fantasia principal:

> La luz atrae al enemigo.

Si un nivel no refuerza esa idea, esta mal planteado aunque "funcione".

## Cuando usar esta skill

Activa esta skill cuando el usuario pida:

- crear un nivel nuevo
- corregir un nivel existente
- rehacer el `Level 02`
- agregar rutas, props o layout
- usar el `Low-Poly Simple Nature Pack`
- asegurar HUD, portal, enemigo, estrellas o canvas comun
- validar que un nivel cumpla el estandar del proyecto

## Objetivo

Todos los niveles del proyecto deben compartir una base jugable comun, pero variar en:

- distribucion espacial
- densidad de cobertura
- rutas de riesgo
- lectura del enemigo
- tension de exploracion y escape

No se vale hacer un nivel "bonito" pero desconectado del loop.

## Archivos que SIEMPRE debes revisar antes de tocar un nivel

1. `AGENTS.md`
2. `Assets/Project/LightChasePrototype/Scripts/Gameplay/PlayerLightState.cs`
3. `Assets/Project/LightChasePrototype/Scripts/Gameplay/EnemyLightSeeker.cs`
4. `Assets/Project/LightChasePrototype/Scripts/Gameplay/PrototypeLevelManager.cs`
5. `Assets/Project/LightChasePrototype/Scripts/Gameplay/ExitPortal.cs`
6. `Assets/Project/LightChasePrototype/Scripts/Gameplay/StarPickup.cs`
7. `Assets/Project/LightChasePrototype/Scripts/UI/GameHudController.cs`
8. `Assets/Project/LightChasePrototype/Scripts/UI/MainMenuController.cs`
9. `Assets/Project/LightChasePrototype/Scripts/Gameplay/LightChaseLevelCatalog.cs`
10. `Assets/Project/LightChasePrototype/Editor/LightChasePrototypeBuilder.cs`
11. `Assets/Project/LightChasePrototype/Editor/LightChaseNatureLevelBuilder.cs`

## Regla madre

Cada nivel debe responder estas cuatro preguntas antes de implementarse:

1. Que decision de riesgo fuerza al jugador
2. Como aumenta la exposicion por recoger estrellas
3. Como se lee el warning del enemigo en ese espacio
4. Como se siente el alivio o la presion al llegar a la salida

Si no puedes responder eso, no sigas construyendo.

## Estandar obligatorio de todo nivel

Todo nivel jugable debe incluir obligatoriamente:

- `Player` con presentacion configurada por `PlayerAvatarSetup`
- `PlayerLightState` funcional
- estrellas coleccionables con `StarPickup`
- enemigo con `EnemyLightSeeker`
- `PrototypeLevelManager`
- `ExitPortal`
- `GameHudController` o el mismo HUD runtime asegurado
- `EventSystem` cuando aplique UI
- `NavMesh` generado y navegable
- escena agregada al build profile o a `EditorBuildSettings`

## HUD obligatorio

Todos los niveles deben mostrar el mismo estado base de partida:

- vidas
- score
- tiempo
- progreso de estrellas
- estado general del nivel

No se debe crear un canvas alterno que omita esta informacion.

Si un nivel usa otro canvas, debes:

1. justificar por que existe
2. demostrar que mantiene las mismas metricas visibles
3. validar que `GameHudController.EnsureHudExists(...)` siga funcionando

## Reglas especificas para arte y set dressing

### Nivel tipo naturaleza

Si el nivel es de bosque, senderos, ruinas naturales o cobertura organica:

- debe usar assets del `Low-Poly Simple Nature Pack` como base visual principal
- no debe quedarse solo con geometria gris o placeholders del `Playground`
- debe usar arboles, rocas, arbustos, vegetacion y cobertura para construir rutas de riesgo legibles
- la decoracion no puede bloquear la lectura del jugador, estrellas, enemigo o portal

### Lo que NO se vale

- props puestos solo para llenar espacio
- decoracion que tape por completo el warning del enemigo
- layout lineal sin opciones de ruta
- cobertura tan cerrada que vuelva injusta la persecucion
- niveles vacios con el HUD roto

## Reglas de layout

Cada nivel debe tener:

- punto de inicio claro
- ruta principal entendible
- al menos una decision de ruta con mas recompensa y mas exposicion
- estrellas distribuidas con riesgo creciente o tradeoff espacial
- salida en una posicion que se sienta como remate de la tension

El layout no debe sentirse como una arena random. Debe enseñar, presionar o castigar una decision.

## Reglas de gameplay para estrellas y enemigo

- recoger estrellas debe seguir aumentando brillo y firma de luz
- el enemigo debe detectar mas facil a un jugador mas brillante
- el warning previo debe poder percibirse dentro del layout nuevo
- no metas tantas estrellas que el progreso deje de importar
- no pongas estrellas gratis pegadas a la salida sin costo de riesgo

## Regla para builders de niveles

Si el nivel se puede regenerar, el cambio debe vivir en un builder editor antes que en retoques manuales dispersos.

Checklist para un builder de nivel:

- asegurar carpeta de escenas
- asegurar existencia de la escena
- abrir escena correcta
- limpiar contenido generado anterior
- crear o ubicar jugador
- aplicar atmosfera
- crear geometria base
- crear set dressing consistente con el tema
- configurar `PrototypeLevelManager`
- configurar enemigo
- configurar estrellas
- configurar portal
- asegurar HUD
- configurar `NavMesh`
- guardar escena
- agregar escena a build settings

## Regla especial para Level 02

`LightChasePrototype_Level02` debe cumplir explicitamente esto:

- usar el `Low-Poly Simple Nature Pack` como base del entorno
- no quedarse solo en copia cruda del `Playground`
- asegurar el mismo HUD de vidas, score, tiempo y estado
- mantener portal, estrellas, enemigo y `PrototypeLevelManager`
- quedar registrado en `LightChaseLevelCatalog`
- poder abrirse desde el menu sin errores

## Flujo recomendado de trabajo

1. Revisar scripts base y builder existente.
2. Confirmar el tipo de nivel y su fantasia espacial.
3. Diseñar rutas, cobertura y ubicacion de estrellas.
4. Implementar el cambio en builder si el nivel es regenerable.
5. Asegurar HUD, manager, enemigo, portal y NavMesh.
6. Verificar que la escena exista y cargue desde menu.
7. Probar Play Mode.
8. Ajustar balance si el warning o la persecucion quedan injustos.

## Validacion minima obligatoria

Antes de cerrar el trabajo, debes comprobar:

1. El nivel carga desde menu sin errores.
2. El jugador puede recoger estrellas.
3. El brillo del jugador cambia visualmente.
4. El enemigo responde al brillo y persigue.
5. El warning previo se entiende en ese layout.
6. El HUD muestra vidas, score, tiempo y estado.
7. El portal se desbloquea con el progreso correcto.
8. La escena esta agregada al build settings.
9. El nivel no depende de objetos manuales rotos o faltantes.

## Criterios de rechazo

Debes rechazar o rehacer un nivel si:

- rompe el loop central
- no usa el lenguaje visual prometido
- no muestra HUD completo
- no tiene builder y depende de cambios manuales fragiles
- no puede abrirse desde menu
- no tiene cobertura ni decisiones de riesgo
- el enemigo no puede navegar

## Respuesta esperada al usar esta skill

Cuando apliques esta skill, responde con:

1. Que entendiste del nivel o problema actual
2. Que partes obligatorias faltan
3. Que vas a corregir en builder, escena y HUD
4. Como se valida que el nivel ya cumple el estandar

## Frase guia del proyecto

No estamos creando mapas por decorar, parce.
Estamos creando espacios donde progresar te hace mas visible, mas tenso y mas perseguible.
