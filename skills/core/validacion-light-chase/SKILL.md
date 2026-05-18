---
name: validacion-light-chase
description: Estandar obligatorio para validar cambios en gameplay, UI, audio, niveles, builders y flow de partida.
---

# Skill: Validacion Light Chase

Esta skill define como validar cambios en `Light Chase Prototype` para que ningun modelo cierre trabajo con una implementacion rota, incompleta o desalineada del loop principal.

## Cuando usar esta skill

Activa esta skill cuando el usuario pida o implique:

- cambios de gameplay
- cambios de HUD o UI de partida
- cambios de menu principal
- cambios de derrota, game over o reintento
- cambios de portal, estrellas, luces o tiempo
- cambios del enemigo
- cambios de audio de warning o persecucion
- cambios de builders de niveles
- cambios de escenas jugables
- validacion final antes de entregar

## Objetivo

Forzar a cualquier modelo a validar el juego como experiencia jugable, no solo como codigo que compila.

## Regla principal

Ningun cambio se considera completo solo porque "el script quedo bonito".

Debe validarse el flujo completo afectado:

- gameplay
- UI
- audio
- derrota
- reintento
- menu
- escena o builder relacionado

## Archivos que SIEMPRE debes revisar

1. `AGENTS.md`
2. `skills/core/logica-global-light-chase/SKILL.md`
3. `Assets/Project/LightChasePrototype/Scripts/Gameplay/PrototypeLevelManager.cs`
4. `Assets/Project/LightChasePrototype/Scripts/Gameplay/GameSessionManager.cs`
5. `Assets/Project/LightChasePrototype/Scripts/Gameplay/EnemyLightSeeker.cs`
6. `Assets/Project/LightChasePrototype/Scripts/Gameplay/ExitPortal.cs`
7. `Assets/Project/LightChasePrototype/Scripts/Gameplay/StarPickup.cs`
8. `Assets/Project/LightChasePrototype/Scripts/UI/GameHudController.cs`
9. `Assets/Project/LightChasePrototype/Scripts/UI/MainMenuController.cs`
10. `Assets/Project/LightChasePrototype/Scripts/UI/GlobalUiController.cs`
11. `Assets/Project/LightChasePrototype/Tests`

## Tipos de validacion obligatoria

### 1. Validacion de logica

Debes comprobar que:

- la regla central del cambio si quedo aplicada
- no se rompio la logica global del juego
- no aparecieron atajos o estados inconsistentes

### 2. Validacion de UI

Debes comprobar que:

- el HUD sigue mostrando vidas, progreso, tiempo y estado
- la causa de derrota se comunica con claridad
- la UI de perder muestra `Reintentar` y `Salir al menu principal` cuando aplique
- la UI no tapa informacion critica del gameplay

### 3. Validacion de audio

Debes comprobar que:

- el warning del enemigo aparece antes de la persecucion
- el warning escala con el riesgo
- el audio de persecucion se mantiene durante la persecucion
- el audio sigue siendo claro sin volverse insoportable

### 4. Validacion de escena y builder

Debes comprobar que:

- la escena afectada sigue cargando
- el builder afectado sigue regenerando contenido correcto si aplica
- menu, nivel y catalogo siguen alineados
- no quedaron dependencias manuales fragiles

### 5. Validacion de pruebas

Debes comprobar que:

- las pruebas existentes siguen siendo validas
- agregaste o actualizaste tests si cambiaste comportamiento
- si no pudiste correr tests, lo debes declarar explicitamente

## Matriz minima de validacion manual

Si el cambio toca gameplay o UI, valida como minimo:

1. Entrar al nivel desde el menu.
2. Mover al jugador por rutas jugables.
3. Recolectar estrellas o luces.
4. Ver reflejado el progreso en el HUD.
5. Confirmar que el portal siga bloqueado antes del requisito.
6. Confirmar que el portal se desbloquee al cumplir el requisito.
7. Escuchar o verificar warning del enemigo al acercarse al umbral de deteccion.
8. Confirmar persecucion y audio constante al detectar.
9. Ser atrapado por el enemigo y validar perdida de vida.
10. Validar mensaje de atrapado y respawn correcto.
11. Validar perdida del progreso de la corrida si esa regla aplica.
12. Agotar el tiempo para confirmar derrota automatica.
13. Validar botones de `Reintentar` y `Salir al menu principal`.

## Matriz minima por tipo de cambio

### Si cambias `EnemyLightSeeker`

Debes validar:

- warning
- persecucion
- dano o captura
- audio de alerta
- audio de persecucion
- impacto en HUD o estado

### Si cambias `PrototypeLevelManager` o `GameSessionManager`

Debes validar:

- vidas
- timer
- score
- portal
- derrota
- reintento
- respawn

### Si cambias `MainMenuController` o `GlobalUiController`

Debes validar:

- entrada al gameplay
- volver al menu principal
- reintento
- persistencia del nivel seleccionado
- persistencia del avatar seleccionado si aplica

### Si cambias builders o escenas

Debes validar:

- escena generada
- contenido obligatorio
- HUD runtime
- portal
- enemigo
- estrellas
- NavMesh

## Regla de honestidad

Si no pudiste validar algo, no lo escondas.

Debes decir exactamente:

- que validaste
- que no validaste
- por que no pudiste validarlo
- que riesgo deja abierto

## Criterios de rechazo

Debes rechazar la entrega como incompleta si:

- el cambio no tiene validacion proporcional a su impacto
- se modifico comportamiento sin actualizar tests ni declarar el gap
- se rompio menu, HUD, derrota o builder y no se menciono
- se cambio una regla global del juego por accidente
- se hizo solo validacion de codigo, pero no de flujo jugable

## Formato de salida al usar esta skill

Cuando cierres una tarea validada con esta skill, reporta:

1. Que cambiaste
2. Que validaste manualmente
3. Que validaste con tests
4. Que no lograste validar
5. Que riesgos quedan abiertos

## Frase guia

En este proyecto no basta con que compile, parce.
Tiene que seguir jugando como `Light Chase`.
