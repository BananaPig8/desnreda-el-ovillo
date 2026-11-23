# 2. Reglas del juego

Este documento es la **fuente de verdad de las reglas** (independientes de la presentación 2D o 3D). El código las implementa en `Assets/_Project/Scripts/Nucleo/` y cada regla tiene una prueba automática en `Assets/_Project/Tests/EditMode/`. Los números viven en `Reglas.cs` (un solo sitio).

---

## 2.1 Tablero

- Cuadrícula de casillas. Por defecto **9×9**; la sala tutorial usa **7×7**. El sistema admite cualquier tamaño.
- Tipos de casilla:

| Símbolo en el mapa | Casilla | Regla |
|---|---|---|
| `.` | Fieltro | Se puede tender lana encima. |
| `E` | Entrada | Agujero del rodapié. Aquí empieza la lana y aparecen las polillas. |
| `S` | Carrete (salida) | Lo que Ovillo protege. Aquí termina la lana. |
| `L` | Lámpara | Obstáculo especial: deslumbra a las polillas que pasan a su lado (ver 2.5). |
| cualquier otro (`#` caja, `o` botón gigante, `u` dedal, `c` carrete pequeño, `a` acerico, `m` metro, `t` tijeras) | Objeto del taller | No se puede atravesar. El símbolo solo elige el dibujo. |

- Las salas son **archivos de texto** en `Assets/_Project/Resources/Salas/` (mapa, oleada, hilo, ofertas, textos del tutorial y solución de referencia). Añadir una sala = escribir un archivo.

## 2.2 Construir el camino (la mecánica principal)

**Gesto:** mantener el clic izquierdo en el agujero (o en el final de la lana) y arrastrar por casillas vecinas. Si el ratón va rápido, el juego rellena las casillas intermedias. Arrastrar hacia atrás deshace; pulsar sobre cualquier tramo de lana la recorta desde ahí; Retroceso/Ctrl+Z deshace el último tramo.

**Reglas** (todas se comprueban al tejer y antes de soltar a las polillas):
1. Solo casillas vecinas en horizontal o vertical (no diagonales).
2. No se atraviesan objetos.
3. No se repiten casillas: por tanto no hay bucles ni cruces.
4. Debe empezar en el agujero y terminar en el carrete.
5. **Hilo:** cada casilla de fieltro cuesta 1 unidad. El agujero y el carrete no cuestan.
6. **Longitud mínima:** 10 casillas (6 en el tutorial). Evita caminos triviales y obliga a rodear objetos.
7. **Hilo disponible:** 18 por sala (14 en el tutorial, 16 en la sala 4) **+ la reserva**.
8. **Reserva de hilo (cambio respecto a la propuesta):** el hilo que no se use en una sala pasa a la siguiente, hasta un máximo de 6.

**Por qué el camino no es una decisión trivial** (lo pedía la propuesta y lo hemos comprobado con el laboratorio):
- **Curvas:** la polilla veloz no puede girar en el aire; en cada curva aterriza. Las curvas deciden dónde pisa y, por tanto, qué trampas la tocan.
- **Orden de casillas:** el velcro y el botón dependen de qué hay justo antes y después; la bobina potencia los alfileres que vienen detrás.
- **Escenario:** pasar junto a la lámpara da un control gratis a cada polilla; los objetos obligan a rodear.
- **Longitud contra futuro:** más lana da más sitio y más curvas, pero el hilo que ahorres se guarda para salas más duras (la Reina).

Con la propuesta original (18 de hilo, sin reserva y con trampas que solo actúan al pisarlas) la mejor estrategia era siempre gastar todo el hilo y la forma daba igual: **la velocidad de las polillas no importaba**, porque todas pisaban todas las casillas. Las reglas de vuelo, de curvas y de "polillas atrapadas" corrigen eso.

## 2.3 Herramientas

Se colocan **sobre casillas de lana** (no en el agujero ni en el carrete). Cada casilla admite **una herramienta encima** y, además, **un velcro cosido debajo**.

### Definición de "usos" (decisión clave)

> **Usos = cuántas polillas distintas puede atrapar la herramienta en la sala.**
> La primera vez que una herramienta afecta a una polilla gasta un uso y la "atrapa".
> A una polilla ya atrapada le sigue afectando **cada vez que vuelve a pisarla o se queda quieta encima**, sin gastar usos.
> Excepción: el **botón** lanza **una sola vez** a cada polilla (si no, rebotaría para siempre).
> Los usos se reponen al empezar cada sala.

**Por qué:** con "cada activación gasta un uso" el laboratorio demostró dos problemas graves: (1) dos alfileres seguidos lo resolvían todo y el velcro y la bobina no aportaban nada; (2) el botón saltarín era casi inútil, porque al volver a pasar los alfileres ya no tenían usos. Con esta definición **el control multiplica el daño** (pegar una polilla encima de un alfiler la pincha dos veces) y las combinaciones nacen de cómo construyes el camino.

### Tabla de herramientas (valores actuales)

| Herramienta | Categoría | Efecto al pisarla | Atrapa | Detalle |
|---|---|---|---|---|
| **Alfiler** | Daño | 2 de daño | 2 polillas | Si la polilla se queda quieta encima (pegada, enredada, deslumbrada), la pincha **cada turno**. Sin usos se "dobla": ya no atrapa polillas nuevas, pero sigue pinchando a las suyas. |
| **Tijeras** | Daño fuerte | 5 de daño | 1 polilla | Si la misma polilla vuelve a pasar (botón), vuelve a cortarla. |
| **Velcro** | Inmovilización | La polilla no se mueve en su siguiente turno | 2 polillas | **Se cose debajo**: comparte casilla con otra herramienta. Velcro + alfiler = la lección del tutorial. |
| **Bobina** | Ralentización | Enredada 4 turnos: **media velocidad** y **no puede volar** | 2 polillas | Única herramienta que **atrapa en pleno vuelo** (una veloz que pasa por encima cae en ella). Una común enredada avanza una casilla cada dos turnos: en los turnos quieta, los alfileres la pinchan. |
| **Botón saltarín** | Desplazamiento | Lanza a la polilla **2 casillas atrás** por la lana | 2 polillas | **Al volar y al caer no activa nada.** Al volver a avanzar, sí: vuelve a pisar las trampas anteriores. Una vez por polilla. |

**Diferencias claras entre categorías** (lo pedía la propuesta):
- *Daño* quita vida. *Inmovilización* deja a la polilla **en su casilla** (sirve para clavarla encima de un alfiler). *Ralentización* afecta a **las casillas siguientes** durante varios turnos y **anula el vuelo**. *Desplazamiento* cambia **dónde** está y hace repetir un tramo.

### Bobina contra Nudo: análisis y decisión

| | Bobina (propuesta) | Nudo (propuesta) |
|---|---|---|
| Efecto | Reduce la velocidad 2 turnos | Detiene 1 turno + daño 2 si vuelve a pasar |
| Problema | Con velocidades de 1–2 casillas, "ralentizar" una común = detenerla: idéntico al velcro | Solapa con el velcro (detener) y con el combo botón + alfiler (dañar al volver a pasar) |

**Decisión:** la demo tiene **5 herramientas**. El **Nudo sale de la demo** y la **Bobina se redefine** con una identidad propia (media velocidad durante varios turnos + anula el vuelo + atrapa en el aire). Para el juego completo queda propuesto el **Nudo corredizo**: ata dos tramos de lana que se tocan y "teletransporta" a la polilla al tramo anterior. Es una herramienta 100 % de construcción de camino (requiere doblar la lana sobre sí misma) y por eso encaja con el pilar 1, pero es demasiado compleja para una feria.

### Preguntas que pedía resolver la propuesta

| Pregunta | Respuesta |
|---|---|
| ¿Cuándo se consume un uso? | La primera vez que la herramienta afecta a una polilla nueva. |
| ¿Qué pasa si una polilla muere encima? | Muere en ese instante; el uso ya está gastado. La herramienta sigue en su sitio. |
| ¿Y si varias polillas activan la misma trampa? | Cada una se resuelve por separado, en orden de actuación (ver 2.6). Cada polilla nueva gasta un uso; cuando no quedan, las siguientes pasan sin efecto. |
| ¿Puede afectar a polillas distintas? | Sí: a tantas como usos tenga. |
| ¿Qué pasa si una trampa es destruida? | (La Reina se las come.) Desaparece al momento con sus usos y deja de afectar incluso a sus polillas atrapadas. La Reina no sufre el efecto de lo que se come. |
| ¿Cómo se comportan los controles? | Retenida: pierde su siguiente turno. Enredada: N turnos de media velocidad y sin vuelo. Si se acumulan efectos del mismo tipo, se queda el mayor (no se suman). Retención y enredo terminan el movimiento de ese turno; el empujón también. |
| ¿El retroceso activa trampas? | No al volar ni al caer. Al volver a avanzar, sí (es el combo). Para evitar bucles, cada botón lanza una sola vez a cada polilla. |

## 2.4 Polillas

| | Común | Veloz | Acorazada | Reina |
|---|---|---|---|---|
| Vida | 4 | 3 | 9 | **34** (propuesta: 30) |
| Velocidad | 1 casilla/turno | 2 casillas/turno, **vuela** | 1 | 1 |
| Muerde al carrete | 1 | 1 | 2 | 2 |
| Rasgo | — | Solo toca el suelo donde **aterriza**: al final de su vuelo, en cada **curva**, sobre una **bobina** con usos y junto a una **lámpara** nueva | **Placa de fieltro**: anula el **primer efecto de control** que recibe (velcro, bobina, botón o lámpara) | **Glotona**: se come la primera casilla con herramientas que pisa. **Corte**: al bajar a 22 y a 11 de vida llama a una común. **Tozuda**: si llega al carrete, muerde y **da otra vuelta** |

### ¿Tiene cada enemigo personalidad jugable? (análisis)

- **Común:** la unidad de medida. 4 de vida = exactamente dos pinchazos: enseña a **concentrar** el daño (un alfiler suelto la deja a medias).
- **Veloz:** en la propuesta original su velocidad **no importaba** (todas pisaban todas las casillas). Ahora **vuela**: obliga a diseñar **curvas** donde están las trampas o a llevar **bobina**. Las huellas rosas enseñan dónde aterrizará. Es el enemigo que hace que la forma del camino importe.
- **Acorazada:** propuesta original: "resistencia al primer efecto de ralentización". La ampliamos a **cualquier control**, porque así tiene una identidad clara ("hay que quitarle la placa con algo barato antes de usar lo bueno") y enseña **orden** en las trampas. 9 de vida = tijeras + dos pinchazos.
- **Reina:** tres reglas legibles que cambian la estrategia: **poner un señuelo** (se come lo primero), **gestionar refuerzos** y **matarla antes de que dé dos vueltas**. Que dé vueltas convierte en amenaza real cualquier trampa que ya la haya atrapado (en la segunda vuelta le vuelven a afectar gratis). Subimos su vida a 34 porque con 30 y tres mejoras era demasiado fácil (laboratorio).

## 2.5 Lámpara (casilla especial)

Las polillas no resisten la luz: **la primera vez** que cada polilla entra en una casilla **junto** a una lámpara, queda deslumbrada (pierde su siguiente turno) y, si volaba, **aterriza**. Cuenta como control (gasta la placa de la acorazada). Es la herramienta para que "aprovechar los obstáculos" sea una decisión real. La propuesta dejaba las casillas especiales para más adelante; la incluimos solo en las salas 4 y 5, porque la sala 4 necesitaba una idea propia.

## 2.6 Orden exacto de resolución de un turno

Implementado en `SimuladorOleada.cs` (cabecera del archivo) y cubierto por pruebas:

1. **Entradas.** Aparecen las polillas programadas para este turno y las que la Reina llamó el turno anterior. Las recién llegadas **no actúan** este turno.
2. **Actuaciones, una polilla cada vez:** primero la **más avanzada** en la lana; a igualdad, la que llegó antes. Para cada polilla:
   1. Si está enredada, este turno cuenta como turno de enredo (media velocidad, sin vuelo).
   2. Si está **pegada/deslumbrada**, pierde el turno. Si está sobre un alfiler que la había atrapado, **se pincha**.
   3. Calcula cuántas casillas avanza (velocidad en medios pasos: una enredada de velocidad 1 avanza cada dos turnos). Si no avanza, se pincha igual que en el punto anterior.
   4. **Avanza casilla a casilla.** En cada casilla:
      - Si es el carrete: **muerde** (el dedal resta 1 al primer mordisco de la sala). La Reina vuelve a la entrada; las demás salen del tablero.
      - Si vuela y no aterriza aquí, solo **sobrevuela**: no se activa nada.
      - La Reina se come la primera casilla con herramientas que pisa (y no sufre su efecto).
      - **Herramienta de encima:** daño (alfiler, tijeras) o control (bobina, botón).
      - **Velcro de debajo**, si la polilla sigue en la casilla.
      - **Lámparas** vecinas que aún no la hayan deslumbrado.
      - **Muerte:** si su vida llega a 0, desaparece en ese momento.
      - **Umbrales de la Reina:** la llamada se resuelve al principio del turno siguiente.
      - Si quedó **pegada, enredada, empujada o aterrizó**, su movimiento termina aquí.
3. **Fin del turno:** derrota si el carrete llega a 0; victoria si no queda ninguna polilla en el tablero ni por llegar.

**Polillas de 2 casillas por turno:** cada casilla recorrida se resuelve con el bloque 2.4 completo. Nunca se saltan el carrete, una curva ni una bobina: hay pruebas específicas para ello.

## 2.7 El carrete

- **4 puntos de vida para toda la partida** (no se reponen entre salas; la mejora *Remiendo* cura 2).
- Una sala se supera aunque alguna polilla muerda, mientras el carrete aguante. En la sala de la Reina, además, hay que derrotarla (da vueltas).
- La vida se ve en el propio carrete: **el hilo rojo del carrete se va gastando** (interfaz diegética), además del contador.

## 2.8 Determinismo

- Sin coma flotante, sin azar y sin depender de los fotogramas dentro de la oleada: la simulación es instantánea y produce una **lista de eventos** que la capa visual reproduce con animaciones.
- El azar fuera de la oleada (herramientas y mejoras ofrecidas) sale de una **semilla** con un generador propio (`Rng.cs`, SplitMix64), idéntico en Unity, .NET y cualquier plataforma.
- Prueba `Mismos_datos_mismos_eventos`: dos simulaciones iguales producen exactamente los mismos eventos.

## 2.9 Registro de decisiones (cambios respecto a la propuesta)

| Propuesta | Decisión | Motivo (datos del laboratorio cuando aplica) |
|---|---|---|
| 2 herramientas por oleada | El **costurero acumula** 2 herramientas por sala | Con solo 2 herramientas el daño máximo (≈9) no alcanzaba la vida de la sala 2 (15). |
| Usos = activaciones | **Usos = polillas distintas** | Ver 2.3: dos alfileres lo resolvían todo y el botón era inútil. |
| Alfiler 2 usos | Se mantiene (2), con pinchazos gratis a polillas quietas | Con 4 usos el tutorial se ganaba sin aprender el combo. |
| 6 herramientas | **5** (Nudo fuera) | Solapamiento Bobina/Nudo/Velcro. |
| Bobina = ralentizar | Media velocidad, sin vuelo, atrapa en el aire | Darle identidad frente al velcro. |
| Veloz = rápida | Además **vuela** y aterriza en curvas | Sin esto su velocidad no cambiaba nada. |
| Acorazada resiste la 1.ª ralentización | Resiste el **1.er control** de cualquier tipo | Identidad más clara y enseña el orden. |
| Reina 30 de vida | **34**, llamadas a 22 y 11, da vueltas | Con 30 y tres mejoras de daño se ganaba sin despeinarse (90 % de soluciones aleatorias perfectas). |
| Hilo se repone por sala | Se repone **y** lo sobrante se guarda (máx. 6) | Da valor a los caminos cortos y a planificar para la Reina. |
| Sala 3 con varias comunes y veloces | 1 común + 2 veloces + 1 acorazada | Con 5 polillas y 6 herramientas solo se podía ganar sin daño con una mejora de daño concreta. |
| Ovillo sin función clara | Mascota, narrador y **la lana es la suya** | Sin mecánica extra (como pedía la propuesta). |
| — | Mejora **Dedal** resta 1 al primer mordisco (antes lo anulaba) | Anulado, la Reina se volvía trivial (100 % perfectas). |
| — | Toda oferta incluye al menos 1 herramienta de daño | Evita costureros imposibles por mala suerte. |

## 2.10 Riesgos de diseño y preguntas abiertas

1. **La "zona de muerte" junto a la entrada domina.** En las soluciones óptimas casi todas las trampas se concentran en las 5–6 primeras casillas; el resto del camino importa menos. Ya lo matizan la longitud mínima, las curvas de la veloz, la lámpara y la reserva de hilo. **Palancas para probar** si en la feria se nota: (a) mapas con la zona de la entrada estrecha u ocupada; (b) una polilla "saltadora" que entra 3 casillas más allá; (c) lámparas y casillas especiales lejos de la entrada; (d) puntuación por hilo ahorrado.
2. **Lectura de la veloz:** que "aterriza en las curvas" debe verse sin leer. Las huellas rosas lo anticipan; hay que confirmarlo con jugadores.
3. **Dificultad real:** el laboratorio mide óptimos y jugadores aleatorios; la curva real solo la darán las pruebas con personas (plan en `07_Produccion_Steam_y_feria.md`).
4. **Previsualización:** ¿mostrar la oleada simulada antes de soltarla? Lo dejamos fuera (quitaría el "¡ajá!"). Las huellas son el término medio.
