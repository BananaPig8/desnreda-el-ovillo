# 1. Visión y pilares

> **Desenreda el Ovillo** · Estrategia por turnos · PC (Steam) · **Unity 3D + URP** · desarrollo en solitario, presupuesto reducido.

## La frase

> **"Cada hilo cuenta. Cada camino es una trampa."**
>
> (Lema de apoyo para la tienda y la feria: *"No defiendas el camino. Constrúyelo para que se convierta en una trampa."*)

## El juego en 30 segundos

Las polillas invaden un taller de costura para comerse el carrete que protege **Ovillo**, un pequeño ser de lana con bufanda roja. Ovillo no combate: **el jugador teje con su lana el camino que las polillas están obligadas a recorrer** y cose encima herramientas de costura (alfileres, velcro, bobinas, tijeras, botones) que funcionan como trampas. Luego suelta a las polillas y mira, turno a turno, cómo su diseño funciona… o no.

Es un **roguelike corto de salas diseñadas a mano**: el carrete tiene 4 puntos de vida para toda la partida, el costurero (las herramientas) crece sala a sala y entre salas se elige una mejora.

## La decisión 3D, con números

La dirección **3D artesanal adorable** es viable para una persona si se controla el alcance. Lo que encarece el 3D no son los polígonos, sino el **número de personajes con rig y animación**. Por eso:

- **Solo 5 personajes** con rig (Ovillo + 4 polillas) y rigs mínimos (12–20 huesos), reutilizando un único esqueleto para las tres polillas normales.
- **Animación "stop motion" a 12 imágenes por segundo**: además de ser la estética buscada, perdona imperfecciones y reduce el pulido de curvas.
- **Cámara fija de maqueta** (inclinación ~55°, giro limitado ±30°): no hay que modelar lo que nunca se ve, y el decorado puede ser de baja resolución y desenfocado.
- **El aspecto textil sale del material, no de la geometría**: texturas de lana y fieltro con mapa de normales y un shader "Textil" (pelusa por Fresnel). Nada de pelo simulado.
- **Todo lo demás son objetos rígidos** sencillos (botones, dedales, bobinas, tijeras), baratos de modelar o de comprar en paquetes de bajo coste y retocar.

Ya existe un **prototipo 3D completo con geometría procedural** (sin modelos externos) para probar la jugabilidad desde hoy mientras llegan los modelos definitivos.

## Por qué el título encaja con la mecánica

El protagonista **es** un ovillo y el verbo principal es **desenredar lana para tejer caminos**. Propuesta narrativa (barata y coherente): la lana del recorrido es la de Ovillo; el contador de hilo es "cuánto puede desenredarse Ovillo en esta sala". Por eso, además, la lana es **crema** (el color de Ovillo) y no roja: se lee mejor sobre el fieltro verde y es accesible para el daltonismo (ver `04_Direccion_de_arte.md`).

## Pilares de diseño

Cada decisión se comprueba contra estos cuatro pilares. Si una idea no sirve a ninguno, no entra en la demo.

| # | Pilar | Qué significa en la práctica |
|---|---|---|
| 1 | **El camino es el arma** | La forma del recorrido (longitud, curvas, por dónde pasa) cambia el resultado. Las polillas veloces solo tocan el suelo en las curvas y al final de su vuelo; la lámpara premia pasar a su lado; el botón y el velcro dependen del orden de las casillas. |
| 2 | **Se entiende de un vistazo** | Todo ocurre "cuando una polilla pisa la casilla". Una herramienta = un icono = un efecto. Las huellas rosas enseñan dónde aterrizará la veloz. |
| 3 | **Determinista y justo** | Mismo recorrido + mismas herramientas = mismo resultado, siempre. Sin azar dentro de la oleada: si pierdes, puedes entender por qué y corregirlo. |
| 4 | **Diorama 3D hecho a mano** | Un pequeño taller tridimensional de lana, fieltro, madera y botones, con volumen real, luz cálida y animación con aire de *stop motion*. Las derrotas son simpáticas, no frustrantes. |

## Género (revisado)

La propuesta inicial mezclaba seis etiquetas. Con las mecánicas ya definidas, la clasificación honesta es:

- **Principal:** *estrategia por turnos de construcción de rutas* ("puzle táctico de trampas").
- **Estructura:** *roguelike ligero* (partida corta, vida persistente, costurero que crece, mejoras elegidas).
- **Etiquetas de Steam recomendadas:** Strategy, Turn-Based Tactics, Puzzle, Roguelite, Tower Defense, Cute, Cozy, Hand-drawn (o "Arts & Crafts" si existe), Singleplayer.

No es un tower defense convencional: no hay torres que disparan ni camino fijo. El camino lo diseña el jugador y las trampas solo actúan al pisarlas.

## Público y referencias de posicionamiento

- Jugadores de puzles tácticos cortos y deterministas (estilo *Into the Breach*, *Dungeon Warfare*, *Mini Motorways* por lo táctil de dibujar rutas), con estética *cozy* hecha a mano.
- Sesiones de 10–15 minutos; ideal para ferias, Steam Next Fest y vídeos cortos (dibujar la lana es muy "gifeable").

## Objetivo de la versión actual

**Vertical slice para feria**: 5 salas, 8–13 minutos, muy pulida, que enseñe la idea en 30 segundos, recoja opiniones y lleve a la lista de deseados de Steam.

## Estado (noviembre de 2026)

| Pieza | Estado |
|---|---|
| Reglas completas y equilibradas con laboratorio de balance | ✅ |
| Núcleo de simulación en C# puro con 57 pruebas automáticas | ✅ |
| 5 salas diseñadas, cada una con solución de referencia verificada | ✅ |
| Prototipo jugable en Unity **3D**: diorama, lana tubular, personajes y herramientas con geometría procedural, luz cálida y postproceso URP opcional | ✅ compila contra UnityEngine; **pendiente de probar en el editor** (este entorno no tiene Unity) |
| Vista previa 3D del prototipo renderizada fuera de Unity con la misma geometría (`Docs/Arte/vista_previa_3d_*.jpg`) | ✅ |
| Modelos 3D finales, rigs, animaciones, música, UI definitiva | ⏳ siguiente hito (ver `07_Produccion_Steam_y_feria.md`) |
