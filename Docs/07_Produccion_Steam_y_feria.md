# 7. Producción, Steam y feria

## 7.1 Hitos (una persona, ritmo de ~25 h/semana)

| Hito | Contenido | Duración estimada | Estado |
|---|---|---|---|
| **M0 · Prototipo jugable** | Reglas, 5 salas equilibradas, prototipo 3D procedural (Ovillo según su hoja, con 8 expresiones y sus animaciones; polillas de hebras; pelusa; presentación de la Reina), pruebas, laboratorio | — | ✅ hecho (pendiente de la primera prueba en el editor) |
| **M1 · Primera partida en Unity** | Abrir con la plantilla URP, jugar las 5 salas, corregir lo que solo se ve dentro del editor, primeras 5 pruebas con jugadores | 1–2 semanas | siguiente |
| **M2 · Arte núcleo** | Shader *Textil*, Ovillo y polilla común finales, tablero final | 4–5 semanas | |
| **M3 · Vertical slice** | Resto de personajes, herramientas y decorado finales; UI Toolkit; música y efectos; clips finales de la presentación de la Reina | 6–8 semanas | |
| **M4 · Build de feria** | Pulido, 15–20 pruebas con jugadores, ajustes de balance, modo feria, página de Steam con lista de deseados | 2–3 semanas | |
| **M5 · Juego completo** | 3–4 biomas del taller (25–35 salas), 8–10 herramientas, 8 polillas, desbloqueos ligeros entre partidas | 9–15 meses | |

**De hoy a la feria: unas 14–19 semanas.** El camino crítico es el arte 3D de personajes.

## 7.2 Presupuesto orientativo (demo)

| Partida | Opción barata | Opción recomendada |
|---|---|---|
| Modelado de los 5 personajes | Hecho en casa (muy lento si no hay experiencia) | Encargo a un artista 3D *freelance* (orientativo: 150–400 € por personaje según detalle) |
| Objetos de costura y decorado | Packs de bajo coste + retoque de materiales | Igual |
| Música | Librería sin regalías | Encargo de 2 temas + 2 *jingles* |
| Efectos de sonido | Grabación casera (foley con lana, tijeras, velcro, botones) | Igual — es barato y muy auténtico |
| Steam | Tasa de Steam Direct (100 USD por juego) | — |
| Feria | Inscripción + transporte + material del stand | Pantalla grande y lana real en la mesa |

(Las cifras son orientativas para planificar; confirma precios actuales antes de comprometerte.)

## 7.3 Riesgos

| Riesgo | Impacto | Mitigación |
|---|---|---|
| El arte 3D se come el calendario | Alto | Rigs mínimos y esqueleto compartido; animación a 12 poses/s; encargar el modelado; el prototipo procedural es jugable mientras tanto |
| La zona de trampas junto a la entrada domina | Medio | Palancas de diseño listas (ver `02_Reglas_del_juego.md`, 2.10) y medición en las pruebas con jugadores |
| No se entiende que la veloz "aterriza en las curvas" | Medio | Huellas rosas, tutorial contextual en la sala 2, prueba con 5 jugadores en M1 |
| Dificultad mal calibrada para la feria | Medio | Modo feria (reintentar sala), laboratorio, registro de partidas en la build de feria |
| Nombre "Desenreda el Ovillo" no disponible o difícil fuera de España | Medio | Buscar en Steam, marcas (EUIPO/OEPM) y dominios antes de la página de Steam; preparar un título en inglés (p. ej. *Unravel the Yarn*: comprobar disponibilidad y confusión con otros títulos) |
| Rendimiento en portátiles de feria | Bajo | Presupuesto de 6.9 en `06_Arquitectura_tecnica.md`; probar pronto en el equipo real del stand |

## 7.4 Steam

- **Página cuanto antes** (en M3): la lista de deseados se acumula con tiempo.
- **Etiquetas:** Strategy, Turn-Based Tactics, Puzzle, Roguelite, Tower Defense, Cute, Cozy, Singleplayer (y la de manualidades si está disponible).
- **Cápsulas:** Ovillo grande con su bufanda, una polilla enfadada y la lana formando el título.
- **Tráiler (45–60 s):** 0–5 s, un dedo tejiendo la lana en el diorama; 5–20 s, polillas cayendo en trampas encadenadas (velcro + alfiler, botón + tijeras); 20–35 s, la Reina comiéndose una trampa; 35–50 s, Ovillo celebrando; final con el lema *"Cada hilo cuenta. Cada camino es una trampa."* y "Añádelo a tu lista de deseados".
- **Demo en Steam Next Fest** cuando la vertical slice esté pulida.
- **Redes:** clips cortos de tejer la lana y de combos (los GIF de 6–10 s son el mejor formato para este juego).

## 7.5 Plan de feria

- **Stand:** el juego en una pantalla grande, **lana y botones reales** en la mesa, un Ovillo de lana de verdad si es posible.
- **Build de feria:** modo feria activado, reinicio automático al menú tras 60 s sin actividad (pendiente), QR a la página de Steam en la pantalla de victoria.
- **Qué medir** (pendiente: registro local en la build de feria): tiempo por sala, reintentos, sala donde se abandona, herramientas elegidas, si usaron la reserva de hilo.
- **Preguntas cortas al terminar:** ¿qué hace la polilla rosa?, ¿qué combo te gustó más?, ¿qué te frustró?, ¿te lo comprarías?, ¿lo añades a tu lista de deseados?

## 7.6 Pruebas con jugadores (desde M1)

1. Cinco personas sin explicación: observar si entienden tejer, colocar y soltar sin ayuda (objetivo: el 80 % supera la sala 1 sin preguntar).
2. Medir dónde pierden vida; contrastar con el laboratorio (`Docs/Balance`).
3. Ajustar números en `Reglas.cs`, regenerar el informe y comprobar que las 5 salas siguen teniendo solución sin daño (`dotnet test`).
