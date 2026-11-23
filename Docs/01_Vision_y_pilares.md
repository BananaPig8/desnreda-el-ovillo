# 1. Visión y pilares

> **Desenreda el Ovillo** · Estrategia por turnos · PC (Steam) · Unity 2D · desarrollo en solitario, presupuesto reducido.

## La frase

> **"No defiendas el camino. Constrúyelo para que se convierta en una trampa."**

## El juego en 30 segundos

Las polillas invaden un taller de costura para comerse el carrete que protege **Ovillo**, un pequeño ser de lana con bufanda roja. Ovillo no combate: **el jugador teje con su lana el camino que las polillas están obligadas a recorrer** y cose encima herramientas de costura (alfileres, velcro, bobinas, tijeras, botones) que funcionan como trampas. Luego suelta a las polillas y mira, turno a turno, cómo su diseño funciona… o no.

Es un **roguelike corto de salas diseñadas a mano**: el carrete tiene 4 puntos de vida para toda la partida, el costurero (las herramientas) crece sala a sala y entre salas se elige una mejora.

## Por qué el título encaja con la mecánica

El protagonista **es** un ovillo y el verbo principal es **desenredar lana para tejer caminos**. Propuesta narrativa (barata y coherente): la lana del recorrido es la de Ovillo; el contador de hilo es "cuánto puede desenredarse Ovillo en esta sala". Por eso, además, la lana es **crema** (el color de Ovillo) y no roja: se lee mejor sobre el fieltro verde y es accesible para el daltonismo (ver `04_Direccion_de_arte.md`).

## Pilares de diseño

Cada decisión se comprueba contra estos cuatro pilares. Si una idea no sirve a ninguno, no entra en la demo.

| # | Pilar | Qué significa en la práctica |
|---|---|---|
| 1 | **El camino es el arma** | La forma del recorrido (longitud, curvas, por dónde pasa) cambia el resultado. Las polillas veloces solo tocan el suelo en las curvas y al final de su vuelo; la lámpara premia pasar a su lado; el botón y el velcro dependen del orden de las casillas. |
| 2 | **Se entiende de un vistazo** | Todo ocurre "cuando una polilla pisa la casilla". Una herramienta = un icono = un efecto. Las huellas rosas enseñan dónde aterrizará la veloz. |
| 3 | **Determinista y justo** | Mismo recorrido + mismas herramientas = mismo resultado, siempre. Sin azar dentro de la oleada: si pierdes, puedes entender por qué y corregirlo. |
| 4 | **Acogedor y hecho a mano** | Fieltro, lana, puntadas, colores cálidos, animaciones elásticas y sonidos de taller. Las derrotas son simpáticas, no frustrantes. |

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
| Prototipo jugable en Unity (arte provisional generado por código + Ovillo y retratos reales) | ✅ compila; pendiente de probar en el editor |
| Arte final, música, animaciones finales, UI definitiva | ⏳ siguiente hito (ver `07_Produccion_Steam_y_feria.md`) |
