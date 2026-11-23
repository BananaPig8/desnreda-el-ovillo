# 5. Interfaz, controles, audio y animación

## 5.1 Disposición de la partida (1920×1080)

La interfaz se diseña a 1920×1080 y se escala con bandas a cualquier resolución (también 16:10 y 1366×768). El tablero siempre ocupa el hueco central y la cámara se ajusta sola a él.

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ [Ovillo] Carrete ●●●○      Sala 2 de 5 · El primer problema        Hilo 9/18 │  barra superior
├──────────┬────────────────────────────────────────────────────┬──────────────┤
│COSTURERO │                                                    │   OLEADA     │
│ 1 Alfiler│                                                    │ [común] T1   │
│   2 libres                                                    │ [común] T3   │
│ 2 Velcro │              TABLERO 3D (diorama)                  │ [veloz] T4   │
│ 3 Bobina │                                                    │ [común] T6   │
│          │                                                    │ CONSEJO      │
│ Mejoras  │                                                    │ ANTES DE     │
│ • ...    │                                                    │ EMPEZAR      │
├──────────┴────────────────────────────────────────────────────┴──────────────┤
│ [Reiniciar lana] [Quitar herramientas]  Modo: tejer   [¡Soltar polillas! ⎵]  │  barra inferior
└──────────────────────────────────────────────────────────────────────────────┘
```

- La interfaz responde a lo que pedía la propuesta: dónde empieza y termina el recorrido (agujero y carrete en el propio tablero), cuánto hilo queda, qué trampas hay (en el tablero, con cuentas de usos), qué polillas vienen (panel de oleada con retratos y turno de entrada; al pasar el ratón, ficha con vida, velocidad y mordisco).
- **Consejos contextuales:** en la sala 1 el consejo cambia según el paso del tutorial; en las demás muestra el objetivo de la sala.
- **"Antes de empezar":** explica en una frase por qué aún no se puede soltar a las polillas (lana incompleta, demasiado corta, sin hilo…).
- **Durante la oleada:** botones de velocidad ×1/×2/×4 (Espacio también cambia), turno actual y polillas en el taller; textos flotantes sobre el tablero ("-2", "¡Pegada!", "¡Enredada!", "¡Placa rota!", "¡Ñam!", "¡Boing!").
- **Presentación del jefe:** cuando entra la Reina, la cámara se acerca al agujero, la Reina crece con un rebote enorme y despliega las alas, entra desde la izquierda un **rótulo de fieltro** con su retrato ("La Reina Polilla — Se come tus trampas · Llama a su corte · Da otra vuelta si muerde"), ruge ("¡Este taller es mío!"), la maqueta tiembla y Ovillo se asusta. Dura unos 3 s y a ×4 se acorta, pero nunca por debajo de lo necesario para leer el rótulo.
- **Sala superada:** confeti de fieltro desde el carrete. **Victoria final:** Ovillo celebra con ojos "&gt; &lt;" y una lluvia de confeti.

## 5.2 Pantallas

| Pantalla | Contenido |
|---|---|
| Menú principal | Logo en etiqueta de fieltro, lema, Jugar, Cómo jugar, Ajustes, Créditos, Salir; el tablero de la sala 1 con Ovillo de fondo. |
| Cómo jugar | 5 páginas: el taller en peligro, tejer la lana, herramientas (icono, categoría, ficha), **conoce a las polillas** (retratos de la hoja de personajes), controles. |
| Ajustes | Volumen, velocidad de oleada, color de la lana (accesibilidad), modo feria, consejos, pantalla completa. |
| Elección de herramientas | Tres tarjetas cosidas; se marcan dos; "¡Al costurero!". |
| Recompensa | "¡Sala superada!" y tres mejoras. |
| Victoria | Ovillo celebrando, estadísticas e **invitación a la lista de deseados** (la URL se pone en `Configuracion.UrlSteam`). |
| Derrota | Las polillas devoran el carrete a mordiscos (el hilo adelgaza en cada uno) mientras Ovillo corre asustado; estadísticas, "Volver a intentarlo" y "Menú principal". |
| Pausa | Continuar, Cómo jugar, Ajustes, Abandonar partida. |

No hay tienda, perfiles, multijugador ni monedas, como pedía la propuesta.

## 5.3 Controles

| Acción | Control |
|---|---|
| Tejer la lana | Clic izquierdo y arrastrar (desde el agujero o el final de la lana) |
| Recortar la lana | Clic sobre cualquier tramo; arrastrar hacia atrás; Retroceso / Ctrl+Z |
| Elegir herramienta | Clic en el costurero o teclas 1–5 |
| Colocar / quitar | Clic izquierdo sobre la lana / clic derecho |
| Cancelar selección | Clic derecho |
| Soltar a las polillas | Espacio (durante la oleada: cambiar velocidad) |
| Zum / girar la maqueta | Rueda del ratón / Q y E |
| Pausa | Escape |

Toda la entrada pasa por `EntradaJugador.cs` (Input System nuevo o clásico), así que añadir mando más adelante es tocar un solo archivo.

## 5.4 Accesibilidad (base)

Lana crema por defecto y selector de color; textos grandes (mínimo 21 px a 1080p); nada depende solo del color (formas e iconos distintos); velocidades de oleada; sin límite de tiempo al preparar; consejos desactivables.

## 5.5 Audio

**Música:** acústica y juguetona (ukelele o guitarra de nailon, glockenspiel, contrabajo pizzicato, percusión de cajas de madera y botones). Dos temas para la demo: *preparación* (tranquilo, 80–90 bpm) y *oleada* (el mismo tema con percusión, 100–110 bpm), más un *jingle* de victoria y otro de derrota. Opción barata: música con licencia de un compositor indie o de una librería sin regalías.

**Efectos:** cada objeto tiene su sonido reconocible. El prototipo ya los **sintetiza en tiempo real** (sin archivos) y se sustituyen poniendo un `AudioClip` con el mismo nombre en `Resources/Audio/`:

| Evento | Nombre del efecto | Idea para la grabación final (foley casero) |
|---|---|---|
| Tejer / deshacer lana | `Lana`, `LanaAtras` | Lana frotando y estirándose |
| Colocar / quitar herramienta | `Colocar`, `Quitar` | Alfiler clavándose en fieltro / tirón |
| Alfiler | `Alfiler` | "Tin" metálico corto |
| Tijeras | `Tijeras` | Corte de tijeras real |
| Velcro | `Velcro` | Velcro despegándose |
| Bobina | `Bobina` | Carrete girando |
| Botón saltarín | `Boton` | "Boing" de muelle |
| Placa rota | `Placa` | Tela gruesa rasgándose |
| Daño / derrota de polilla | `Dano`, `Muere` | Golpecito de peluche / "pof" de pelusa |
| Mordisco al carrete | `Mordisco` | Mordisquito + hilo tensándose |
| Reina | `Reina` | Zumbido grave de alas + voz de fieltro |
| Deslumbrada | `Deslumbra` | Campanitas |
| Victoria / derrota | `Victoria`, `Derrota` | Jingles |

## 5.6 Animación

Principio: **pocas animaciones, muy expresivas**, con *squash and stretch* y aire de *stop motion* (12 poses por segundo).

| Personaje | Imprescindibles para la feria | Deseables |
|---|---|---|
| Ovillo | Idle/respirar, parpadeo, llegar andando, susto, dolor (mordisco), saltito, guiño, celebrar, tristeza | Correr asustado, tropezar, mirar a las polillas, preocuparse si se acercan (**todo ya está en el prototipo**, con las 8 expresiones de su hoja) |
| Común | Idle, trotar, daño (ojos apretados), pegada (ojos entornados), enredada, morir (ojos en X) | Escapar con el botín |
| Veloz | Vuelo, aterrizaje, daño, morir | Atrapada en el aire |
| Acorazada | Caminar pesado, daño, **perder la placa**, morir | Resistir con orgullo |
| Reina | **Presentación**, caminar, **comer herramienta**, **llamar a su corte**, daño, derrota | Dar otra vuelta con enfado |
| Herramientas | Alfiler pincha, tijeras cortan, botón rebota, bobina gira, velcro se aplasta | — |

El prototipo ya tiene versiones procedurales de casi todas (ver `VistaPolilla.cs`, `VistaOvillo.cs`, `VistaHerramientas.cs`, `ReproductorOleada.cs`); los clips finales las sustituirán una a una.

### Ovillo en el prototipo (qué hace y cuándo)

| Momento | Animación | Cara |
|---|---|---|
| Empieza una sala | Llega andando desde fuera del tablero (pasos alternos, balanceo, bracitos); en las salas pares **tropieza** con su hebra, cae de morros y se levanta | Normal → Sorprendido → Herido → Normal |
| Preparación | Respira, parpadea, la punta de la bufanda se mece | Normal |
| La lana llega al carrete | Saltito | **Guiño** |
| Polilla cerca del carrete | La mira y tiembla un poco | Preocupado |
| Cae una polilla | Saltito con los bracitos arriba | Feliz |
| Muerden el carrete | Se encoge hacia atrás, se pone colorado un instante | **Herido** ("&gt; &lt;") |
| Entra la Reina | Respingo | Sorprendido |
| Derrota | **Corre** de un lado a otro con los brazos en alto (la bufanda ondea) y luego se queda abatido | Sorprendido → Triste |
| Victoria final | Saltos con vueltas, bracitos arriba y confeti | **¡Euforia!** |

Todo se anima a **12 poses por segundo** (aire de *stop motion*). Las polillas parpadean a su ritmo, aprietan los ojos con cada golpe y ponen ojos en X al caer; su sombra de contacto se queda en la casilla cuando vuelan.
