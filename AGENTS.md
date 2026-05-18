# AGENTS.md

## Proposito
Este archivo es la puerta de entrada para cualquier AI que trabaje sobre este repositorio.

Su trabajo es:

1. preservar la fantasia principal del proyecto
2. ubicar rapido la fuente de verdad correcta
3. activar las skills adecuadas en vez de repetir reglas dispersas

## Proyecto
- Nombre de trabajo: `Light Chase Prototype`
- Motor: Unity `6000.4.3f1`
- Pipeline: `URP`
- Input: `Unity Input System`
- Navegacion enemiga: `Unity AI Navigation` con `NavMeshAgent`
- Base de movimiento: `Starter Assets Third Person Controller`

## Fantasia principal

> La luz atrae al enemigo.

Este proyecto no trata sobre coleccionar por coleccionar.
Trata sobre como progresar vuelve al jugador mas visible, mas tenso y mas perseguible.

Toda propuesta debe responder:
- que añade al loop principal
- como afecta el balance riesgo/recompensa
- como impacta la lectura de warning, persecucion, tiempo y escape

## Fuente de verdad por tema

No dupliques reglas si ya viven en una skill. Usa esta tabla como indice:

- `skills/core/logica-global-light-chase/SKILL.md`
  Para reglas base de gameplay, progreso, portal, vidas, tiempo, derrota, respawn, enemigo y flow comun entre niveles.

- `skills/core/ui-global-light-chase/SKILL.md`
  Para HUD, menu principal, layout global, jerarquia visual y reglas de no solapamiento.

- `skills/core/validacion-light-chase/SKILL.md`
  Para validacion final de gameplay, UI, audio, builders, escenas y flows jugables.

- `skills/dev/crear-niveles-light-chase/SKILL.md`
  Para crear, rehacer o corregir niveles y sus builders.

- `skills/core/cierre-documental-light-chase/SKILL.md`
  Para cierre documental despues de cambios que introduzcan reglas, patrones, canon visual o decisiones reutilizables.

## Archivos base que siempre debes revisar

Antes de cambiar codigo o escenas, revisa como minimo:

1. [AGENTS.md](/Users/bue221/Documents/Estudio/videojuego_universidad/AGENTS.md)
2. [Packages/manifest.json](/Users/bue221/Documents/Estudio/videojuego_universidad/Packages/manifest.json)
3. [ProjectSettings/ProjectVersion.txt](/Users/bue221/Documents/Estudio/videojuego_universidad/ProjectSettings/ProjectVersion.txt)
4. [Assets/Project/LightChasePrototype/Scripts/Gameplay](/Users/bue221/Documents/Estudio/videojuego_universidad/Assets/Project/LightChasePrototype/Scripts/Gameplay)
5. [Assets/Project/LightChasePrototype/Scripts/UI](/Users/bue221/Documents/Estudio/videojuego_universidad/Assets/Project/LightChasePrototype/Scripts/UI)
6. [Assets/Project/LightChasePrototype/Editor](/Users/bue221/Documents/Estudio/videojuego_universidad/Assets/Project/LightChasePrototype/Editor)
7. [Assets/Project/LightChasePrototype/Tests](/Users/bue221/Documents/Estudio/videojuego_universidad/Assets/Project/LightChasePrototype/Tests)

## Flujo obligatorio para modelos

1. entender que sistema fue tocado
2. activar la skill correcta segun el tema
3. revisar la fuente de verdad antes de asumir
4. implementar con cambios pequenos y coherentes
5. actualizar o agregar tests cuando cambie comportamiento
6. validar de forma proporcional al impacto
7. pasar por cierre documental si aparecio una nueva regla o decision reusable

## Reglas de contribucion

- No asumas que esto es un juego de combate.
- No rompas la relacion entre progreso y peligro.
- No metas logica critica solo en escena si debe vivir en script o builder.
- Si un nivel es regenerable, prioriza corregir el builder sobre retoques manuales fragiles.
- Si una decision ya esta definida en una skill, actualiza la skill en vez de copiar la regla en varios archivos.
- Si una regla cambia de verdad, actualiza primero la skill dueña y luego este `AGENTS.md` solo si cambia la orientacion global.

## Documentacion viva

Cuando un cambio introduzca una nueva regla, patron reusable, canon visual, convencion de builder, criterio de validacion o decision arquitectonica:

1. activa `skills/core/cierre-documental-light-chase/SKILL.md`
2. pregunta al usuario si quiere documentarlo
3. si responde que si, actualiza la skill adecuada y cualquier documento afectado
4. si no existe una skill dueña, crea una nueva

## Estado actual

Este repositorio sigue siendo un prototipo funcional orientado a validar el loop principal antes de expandir demasiado contenido.

La prioridad no es cantidad de features.
La prioridad es claridad del loop, tension y consistencia entre gameplay, UI y niveles.
