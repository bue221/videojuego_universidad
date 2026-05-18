---
name: cierre-documental-light-chase
description: Workflow obligatorio de cierre para detectar reglas nuevas y convertirlas en documentacion viva del proyecto.
---

# Skill: Cierre Documental Light Chase

Esta skill existe para que una decision importante no se quede enterrada en un chat.

Su trabajo es detectar cuando una tarea produjo una regla nueva, un patron reusable o un canon que deberia vivir dentro del repositorio.

## Cuando usar esta skill

Activa esta skill al final de una tarea cuando ocurra al menos una de estas cosas:

- se creo o cambio una regla global de gameplay
- se definio un canon de UI o layout
- se estandarizo un builder o flujo de escenas
- se agrego una convencion tecnica reusable
- se corrigio un bug que revela una regla importante
- se establecio una validacion que deberia repetirse
- el usuario pide explicitamente que no se olvide una decision

## Regla principal

Si el cambio deja una idea que seria molesto volver a explicar en otro chat, debes ofrecer documentarla.

## Pregunta obligatoria de cierre

Cuando esta skill aplique, antes de cerrar la tarea debes preguntar de forma corta:

`Detecte una regla o decision reusable en este cambio. Quieres que la deje documentada en una skill o en la documentacion del proyecto?`

Si la respuesta ya fue afirmativa durante la conversacion, no repitas la pregunta: documenta de una vez.

## Como decidir donde documentar

### Actualizar una skill existente

Haz esto si la decision pertenece claramente a uno de estos dominios:

- gameplay global
- UI global
- validacion
- niveles y builders

Skills dueñas actuales:

- `skills/core/logica-global-light-chase/SKILL.md`
- `skills/core/ui-global-light-chase/SKILL.md`
- `skills/core/validacion-light-chase/SKILL.md`
- `skills/dev/crear-niveles-light-chase/SKILL.md`

### Crear una skill nueva

Haz esto si la decision:

- no cabe bien en una skill actual
- va a repetirse bastante
- define un sistema o area nueva del proyecto

Al crearla, debes:

1. crear `metadata.json`
2. crear `SKILL.md`
3. actualizar [AGENTS.md](/Users/bue221/Documents/Estudio/videojuego_universidad/AGENTS.md) para referenciarla si cambia la orientacion global

### Actualizar documentacion general

Haz esto si la decision afecta el mapa global del proyecto y no solo una skill puntual.

Los candidatos normales son:

- [AGENTS.md](/Users/bue221/Documents/Estudio/videojuego_universidad/AGENTS.md)
- [README.md](/Users/bue221/Documents/Estudio/videojuego_universidad/README.md)

## Criterio de minima documentacion

Si el usuario acepta documentar, debes dejar por escrito como minimo:

1. que se decidio
2. por que importa
3. donde aplica
4. como validarlo

## Regla de no duplicacion

No copies la misma regla en cinco lugares.

La prioridad es:

1. skill duena del tema
2. `AGENTS.md` como indice o regla global
3. `README.md` solo si mejora onboarding o uso

## Señales de que SI debes proponer documentacion

- el cambio afecta mas de un nivel
- el cambio afecta menu y HUD
- el cambio redefine un comportamiento canonico
- el cambio arregla una confusion recurrente
- el usuario expresa que no quiere volver a explicarlo

## Señales de que NO necesitas crear una skill nueva

- es un ajuste cosmetico aislado
- es un bug puntual sin regla reusable
- ya existe una skill que cubre perfectamente la decision

## Formato esperado al aplicar esta skill

1. detectar la regla reusable
2. preguntar si se quiere documentar, salvo que ya este confirmado
3. si la respuesta es si, actualizar skill o crear nueva skill
4. actualizar `AGENTS.md` solo si hace falta cambiar el mapa global
5. mencionar en el cierre que quedo documentado y donde

## Frase guia

Si una decision vale la pena repetirla, vale la pena dejarla escrita, parce.
