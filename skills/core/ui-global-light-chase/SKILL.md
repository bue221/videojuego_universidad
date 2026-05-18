---
name: ui-global-light-chase
description: Canon visual obligatorio para HUD y menu principal de Light Chase Prototype.
---

# Skill: UI Global Light Chase

Esta skill define como debe verse y organizarse la UI global del proyecto.

Se usa cuando haya cambios en:

- HUD
- menu principal
- score, vidas, tiempo
- mensajes de estado
- jerarquia visual de overlays
- layout entre niveles

## Regla madre

La UI nunca debe tapar informacion importante del nivel ni competir con la lectura del espacio.

## HUD canonico

- `VIDAS`, `SCORE` y `TIEMPO` viven arriba
- `VIDAS` arriba a la izquierda
- `SCORE` arriba al centro
- `TIEMPO` arriba a la derecha
- el mensaje de estado vive abajo, centrado
- el mensaje de estado no debe cruzar el centro de pantalla salvo casos excepcionales
- el HUD se oculta cuando el menu principal visible esta abierto

## Menu principal canonico

En el estado inicial solo deben verse:

- seleccion de nivel
- boton `Jugar`
- boton `Salir`

No deben aparecer de entrada:

- HUD del gameplay
- overlays duplicados
- avatar selection abierta
- panel de instrucciones abierto

## Jerarquia institucional obligatoria

El menu principal debe mostrar de forma visible:

- `UNIVERSIDAD CENTRAL`
- `MODELADO 3D Y VIDEOJUEGOS 2026`

Estas lineas hacen parte del encabezado institucional y no deben desaparecer por cambios cosmeticos.

## Regla de limpieza tecnica

- nunca deben coexistir `GameplayHUD` legacy de escena y `GlobalUIRoot`
- nunca deben coexistir `MainMenuOverlay` legacy de escena y `GlobalUIRoot`
- los builders de niveles no deben guardar HUD o menu runtime en las escenas

## Validacion minima

Antes de cerrar cambios de UI debes comprobar:

1. el HUD no tapa el centro del gameplay
2. el mensaje de estado queda abajo
3. el menu inicial muestra solo nivel + jugar + salir
4. el encabezado institucional sigue visible
5. no hay solapamiento entre HUD y menu
6. la UI funciona igual en los 3 niveles
