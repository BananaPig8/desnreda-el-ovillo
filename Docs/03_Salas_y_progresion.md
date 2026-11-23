# 3. Salas de la demo y progresión

Las cinco salas están en `Assets/_Project/Resources/Salas/sala1.txt … sala5.txt`. Cada una guarda una **solución de referencia que gana sin perder vida**; las pruebas automáticas fallan si un cambio de reglas o de mapa la rompe. Las cifras de dificultad salen del laboratorio (`Docs/Balance/informe_balance.md`).

Leyenda de mapas: `E` agujero, `S` carrete, `.` fieltro, `#` caja de costura, `o` botón gigante, `u` dedal, `c` carrete pequeño, `L` lámpara.

## Estructura de una partida (8–13 minutos)

```
Sala 1 ──► mejora ──► Sala 2 ──► mejora ──► Sala 3 ──► mejora ──► Sala 4 ──► mejora ──► Sala 5 (Reina) ──► Victoria
  │             (en cada sala 2-5: se ofrecen 3 herramientas, eliges 2, y se quedan en el costurero)
  └─ El carrete tiene 4 de vida para TODA la partida. Hilo sobrante → reserva (máx. 6).
```

| Sala | Nombre | Tablero | Hilo / mínimo | Oleada | Herramientas nuevas | Qué enseña | Tiempo |
|---|---|---|---|---|---|---|---|
| 1 | Primeros hilos | 7×7, 2 objetos | 14 / 6 | 2 comunes | Alfiler + Velcro (regalados) | Tejer, colocar, **concentrar el daño** (velcro debajo del alfiler) | ~2:30 |
| 2 | El primer problema | 9×9, 6 objetos | 18 / 10 | 3 comunes + 1 veloz | **Bobina** (siempre ofrecida) | La veloz **vuela**: curvas y bobina. Zona de trampas al principio | ~2:00 |
| 3 | El taller se complica | 9×9, columna central | 18 / 10 | 1 común + 2 veloces + 1 acorazada | **Tijeras + Botón** (siempre ofrecidos) | Quitar la **placa** con algo barato y luego el golpe fuerte; el botón hace repetir trampas | ~2:15 |
| 4 | La última costura | 9×9 con pasillos | **16** / 10 | 3 comunes + 2 veloces + 1 acorazada | (azar) | **Aprovechar el escenario**: la lámpara deslumbra gratis a todas | ~2:15 |
| 5 | La Reina | 9×9 abierto, 2 lámparas | 18 / 10 | Reina + 2 comunes (+2 que llama) | (azar) | **Señuelo** (se come lo primero), refuerzos y no dejarla dar vueltas | ~3:00 |

### Sala 1 — Primeros hilos (tutorial interactivo)

```
. . . . . . .
. . . . . . .
. . . u . . .
E . . o . . S
. . . . . . .
. . . . . . .
. . . . . . .
```
- **Objetivo:** llevar la lana hasta el carrete y pegar a las polillas encima del alfiler.
- **Guion del tutorial** (el consejo cambia según lo que haya hecho el jugador): 1) arrastra desde el agujero; 2) coloca el alfiler; 3) cose el velcro en la misma casilla; 4) pulsa Espacio.
- **Lección:** el alfiler solo hace 2 de daño a cada polilla que pasa (4 de vida): sin velcro, las dos escapan. Con velcro, cada una se queda quieta encima y se pincha dos veces.
- **Datos:** con todo al azar solo el 10 % de las partidas sale perfecto; aplicando un poco de cabeza, el 88 %.

### Sala 2 — El primer problema

```
. . . . . . . . .
. . . . . . . . .
. . # # . . . . .
. . . . . . o . .
E . . . . . . . S
. . o . . . . . .
. . . . . . # # .
. . . . . . . . .
. . . . . . . . .
```
- **Oleada:** común (T1), común (T3), **veloz (T4)**, común (T6).
- **Lección:** la veloz vuela y solo toca el suelo donde aterriza; aterriza en las **curvas**. Las huellas rosas muestran dónde. La bobina la atrapa en el aire.
- **Solución de referencia:** bobina en la casilla 1, alfiler en la 2, alfiler + velcro en la 4, con un tramo en zigzag al principio: las comunes enredadas se pinchan dos veces y la veloz aterriza encima del velcro.
- **Datos:** con el costurero típico (2 alfileres, velcro, bobina) se puede ganar sin daño, pero al azar solo un 5 %. Si el jugador ignora los alfileres y se queda con "bobina + velcro", lo mejor posible es perder 2: por eso toda oferta incluye al menos una herramienta de daño.

### Sala 3 — El taller se complica

```
. . . . . . . . .
. # . . . . . # .
. # . . c . . # .
. . . . c . . . .
E . . . . . . . S
. . . . c . . . .
. # . . c . . # .
. # . . . . . # .
. . . . . . . . .
```
- **Oleada:** común (T1), veloz (T2), veloz (T4), **acorazada (T5)**.
- **Lección:** la placa de la acorazada anula el primer control. Se quita con algo barato (velcro, bobina) y después se remata (tijeras; el botón la hace volver a pasar por ellas).
- **Datos:** con 5 polillas la sala solo era ganable sin daño con una mejora de daño concreta; con esta oleada se puede ganar perfecto con los costureros típicos ("tijeras + alfiler" es el más cómodo).

### Sala 4 — La última costura

```
. . # . . . # . .
. . # . . . # . .
. . . . L . . . .
# # . # # # . # #
E . . . . . . . S
# # . # # # . # #
. . . . . . . . .
. . # . . . # . .
. . # . . . # . .
```
- **Oleada:** común (T1), veloz (T2), común (T3), acorazada (T5), veloz (T6), común (T8).
- **Lección:** el pasillo central es el camino corto pero no tiene curvas; la lámpara, arriba, **deslumbra gratis a cada polilla** (y hace aterrizar a las veloces). La solución de referencia sube por el pasillo izquierdo, pasa junto a la lámpara y baja por el derecho.
- **Menos hilo (16)**: obliga a elegir entre pasar por la lámpara o guardar hilo para la Reina.
- **Datos:** sin mejoras se puede ganar perfecto pero cuesta (4 % aplicando al azar); con dos mejoras de daño sube al 62 %. Un costurero de "mucho control y poco daño" pierde 2.

### Sala 5 — La Reina

```
. . . . . . . . .
. . . L . . . . .
. . . . . . o . .
. . u . . . . . .
E . . . . . . . S
. . . . . . u . .
. . o . . . . . .
. . . . . L . . .
. . . . . . . . .
```
- **Oleada:** común (T1), **Reina (T3)**, común (T5). La Reina llama a una común al bajar a 22 y otra a 11 de vida.
- **Presentación:** en el prototipo, aparición elástica + rótulo "¡La Reina!" + sonido grave. Para la vertical slice está previsto un plano de cámara de 2 s con su animación de entrada (ver `05_Interfaz_audio_animacion.md`).
- **Lecciones:** (1) **señuelo**: se come la primera casilla con herramientas; (2) los refuerzos entran por el agujero cuando ya has gastado usos; (3) si llega al carrete muerde 2 y **da otra vuelta**: las herramientas que ya la atraparon le vuelven a afectar gratis en la segunda vuelta.
- **Datos:** sin mejoras se gana perdiendo 2; con cualquier trío de mejoras se puede ganar sin daño (entre un 0 % y un 43 % de soluciones al azar perfectas según el trío). Es la sala donde se nota el hilo ahorrado.

## Elección de herramientas (draft)

- Sala 1: el tutorial regala **Alfiler + Velcro**.
- Salas 2–5: se ofrecen **3**, eliges **2**. Se ofrecen solo herramientas ya presentadas; las nuevas de la sala aparecen siempre (Bobina en la 2; Tijeras y Botón en la 3).
- Nunca tres iguales y siempre al menos una de daño.
- El azar sale de la semilla de la partida (mismo número = mismas ofertas: útil para reproducir errores y para un futuro "reto diario").
- Al final de la demo el costurero tiene 10 herramientas.

## Mejoras (una de tres tras las salas 1–4)

| Mejora | Efecto | Tipo | Comentario de diseño |
|---|---|---|---|
| Alfileres afilados | Alfiler +1 de daño | Daño | Simple y fuerte: hace que una común caiga con dos pinchazos de cualquier forma. |
| Acerico lleno | Cada alfiler atrapa a 1 polilla más | Daño/escala | Era +2 y rompía el jefe; con +1 queda bien. |
| Hilo grueso | +1 de daño al alfiler clavado en una **curva** | Construcción | La mejor para el pilar 1: hace que diseñar curvas compense. |
| Velcro reforzado | El velcro retiene un turno más | Control | Un pinchazo gratis más por polilla pegada. |
| Bobina pesada | Enredo +2 turnos y +1 uso | Control | Para quien juega a "zona lenta". |
| Tijeras de sastre | Las tijeras atrapan a una polilla más | Daño | Brilla contra acorazada y Reina. |
| Muelle tenso | El botón lanza 1 casilla más lejos | Desplazamiento | Combos más largos. |
| Ovillo grande | +3 de hilo por sala | Hilo | Más sitio y más reserva para la Reina. |
| Remiendo | El carrete recupera 2 (máx. 4) | Resistencia | No se ofrece con el carrete lleno (sería una opción trampa). |
| Dedal | El primer mordisco de cada sala hace 1 menos | Resistencia | Era "anula el mordisco" y trivializaba a la Reina. |

**Variedad estratégica:** las que más cambian la forma de jugar son *Hilo grueso* (premia diseñar curvas), *Bobina pesada* (zonas lentas largas), *Muelle tenso* (combos de rebote) y *Ovillo grande* (caminos largos frente a reserva). Las de daño puro son las "seguras" para quien empieza. Solo se ofrecen mejoras de herramientas que el jugador ya tiene o que se acaban de presentar.

## Reserva de hilo

El hilo que no se usa pasa a la siguiente sala (máximo 6). Crea una decisión real: **"¿Necesito todo el hilo aquí o lo guardo para la Reina?"**, y da sentido a los caminos cortos (que antes nunca eran mejores). En la pantalla de victoria se muestra el **hilo total utilizado**, útil como puntuación en la feria.

## Derrota y reintentos

- **Modo feria (activado por defecto):** al perder, se repite la sala con el costurero, las mejoras y la vida con las que se empezó, **conservando la lana y las herramientas colocadas** para que el jugador corrija en vez de rehacer.
- **Modo roguelike (desactivable en ajustes):** perder empieza una partida nueva. Es el previsto para el juego completo.

## Pantalla de victoria

Ovillo celebrando, polillas derrotadas, hilo utilizado, vida del carrete, tiempo, reintentos e invitación a añadir el juego a la lista de deseados de Steam.
