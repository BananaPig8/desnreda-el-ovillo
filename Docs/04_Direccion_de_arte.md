# 4. Dirección de arte — 3D artesanal adorable

> **Decisión:** un pequeño **diorama 3D** de un taller de costura, construido con lana, fieltro, madera, botones y tela, que parece una maqueta hecha a mano que ha cobrado vida. Nada de 2D con sombras: volumen real, materiales que responden a la luz y animación con aire de *stop motion*.

![Vista previa del prototipo 3D, sala 2](Arte/vista_previa_3d_sala2.jpg)

*Vista previa del prototipo actual (geometría procedural, sin modelos externos), renderizada fuera de Unity con la misma geometría y materiales que usa el juego. Es un placeholder jugable, no el arte final.*

## 4.1 Referencias oficiales

| Referencia | Archivo | Uso |
|---|---|---|
| Ovillo (render de referencia) | `Assets/_Project/Resources/Arte/Personajes/ovillo.png` | Modelo de Ovillo, retrato de la interfaz y pantalla de victoria. |
| Hoja de personajes | `Docs/Arte/referencias/hoja_personajes.png` | Silueta, colores y expresiones de las cuatro polillas; estilo de etiquetas de fieltro de la interfaz. |
| Retratos recortados | `Assets/_Project/Resources/Arte/Retratos/*.png` | Fichas de "Conoce a las polillas", vista de la oleada. |
| Placeholders 3D | `Docs/Arte/vista_previa_3d_*.jpg`, `Docs/Arte/giros_personajes_provisionales.jpg` | Proporciones y escala de juego para el modelado final. |

![Hoja de giros de los placeholders](Arte/giros_personajes_provisionales.jpg)

## 4.2 Paleta

Colores medidos sobre las referencias (cuentagotas sobre la imagen de Ovillo y la hoja de personajes) y fijados en `Paleta.cs`. La paleta global es cálida y poco saturada.

| Uso | Color | Hex |
|---|---|---|
| Lana de Ovillo (luz / medio / sombra) | crema melocotón | `#FBE0B5` / `#F7C187` / `#D58249` |
| Bufanda (luz / sombra) | rojo coral | `#F24432` / `#A71108` |
| Botas | marrón fieltro | `#7A3416` |
| Mejillas | rosa | `#FB7F72` |
| Ojos | casi negro cálido | `#3A140C` |
| Fieltro del tablero (oscuro / base / claro) | verde musgo | `#3F5525` / `#5E7A36` / `#7A9849` |
| Polilla común | lila / alas crema | `#9467B8` / `#F3DEC0` |
| Polilla veloz | rosa / alas rosa claro | `#EE8C9E` / `#F7BCC7` |
| Polilla acorazada | gris pizarra / faldones | `#7D7896` / `#5E5A78` |
| Reina | lila rosado / alas morado / capa magenta / oro | `#D7A2C8` / `#7B3F94` / `#B8395E` / `#E8BE4A` |
| Madera | miel | `#B98352` → `#9A6538` |
| Interfaz: títulos | ciruela (como el logo de la hoja) | `#6E2F4C` |

**Reglas de lectura:**
- **La lana del recorrido es crema** (la de Ovillo) y no roja: el rojo sobre verde es la combinación que peor distinguen las personas con daltonismo (contraste de luminancia ≈ 1,4:1). Crema sobre musgo da ≈ 3,8:1. En Ajustes se puede elegir crema, coral o azul.
- **El rojo se reserva** para Ovillo (bufanda), el hilo del carrete (la vida) y avisos.
- Cada polilla se distingue **por silueta y tamaño**, no solo por color: común redonda mediana; veloz pequeña con alas grandes y antenas largas; acorazada grande y ancha con faldones; Reina enorme con capa, cuello y corona.

## 4.3 Materiales

El aspecto textil debe salir del **material**, no de la geometría. Ninguna superficie debe parecer plástico.

| Material | Prototipo (ya implementado) | Versión final (vertical slice) |
|---|---|---|
| **Lana** | URP Lit + textura procedural de **3 cabos en hélice** y mapa de normales (`TexturasTres.Lana`); suavidad 0,12 | Shader Graph *Textil*: albedo + normal de hebras + **pelusa por Fresnel** (borde claro y suave) + iluminación envolvente (*wrap*) para que la luz "entre" en la lana. Opcional para primeros planos: 4–6 capas de *shell fur* solo en Ovillo. |
| **Fieltro** | Ruido de fibras + normal; suavidad 0,06 | Igual + *Textil* con menos pelusa; costuras como textura (no geometría). |
| **Tela** | Trama/urdimbre procedural | Textura de tejido + normal; pliegues modelados solo en la capa de la Reina. |
| **Madera** | Veta suave de bajo contraste | Textura tileable con veta y poro; bordes redondeados en el modelo. |
| **Metal** (alfileres, tijeras, dedal) | Metálico 0,85, suavidad 0,6 | Igual + algo de desgaste en la rugosidad; necesita una *reflection probe* en la escena. |
| **Botones** | Suavidad 0,42 con fibra sutil | Resina/madera con brillo medio; agujeros modelados. |

## 4.4 Personajes — fichas de producción

Escala de juego: **1 casilla = 1 unidad de Unity**. Medidas para el modelo final (alto sin antenas).

### Ovillo — protagonista

| | |
|---|---|
| Silueta | Bola de lana con **bufanda roja** (con cola que ondea) y dos botitas. Se reconoce a 40 px de alto. |
| Medidas | Ø cuerpo **0,88** (en el tablero se ve junto al carrete, fuera de las casillas). |
| Materiales | Lana crema (con 3–4 vueltas de hebra que se ven por encima de la bola), bufanda de lana roja de punto grueso, botas de fieltro marrón con pespunte naranja, mejillas de fieltro rosa. |
| Expresiones (blendshapes o texturas) | Contento (base), sorpresa, susto, alegría máxima, tristeza, parpadeo. |
| Animaciones (por prioridad) | **Imprescindibles:** idle/respirar, parpadear, susto (carrete mordido), saltito (polilla derrotada), celebrar (victoria), tristeza (derrota). **Deseables:** caminar, correr, tropezar, mirar a la polilla más cercana. |
| Presupuesto | 3.000–5.000 triángulos · texturas 1024² (color, normal, máscara) · rig de 14–18 huesos (raíz, cuerpo con *squash*, 2 brazos, 2 pies, 3 de bufanda, 2 ojos/párpados). |

### Polilla común

| | |
|---|---|
| Silueta | Bola de lana lila con dos pares de **alas de fieltro crema detrás, en V**, con pespunte y manchas lilas; antenas curvas con bolita; 6 patitas marrones; **cejas enfadadas**. |
| Medidas | Cuerpo Ø 0,56; envergadura ~0,9. |
| Animaciones | Idle (rebote + aleteo), trotar por la lana, recibir daño, quedar pegada, enredada, deslumbrada, morir (bolitas de fieltro), escapar hacia el carrete. |
| Presupuesto | 2.000–3.000 triángulos · 512² · **esqueleto compartido** con la veloz y la acorazada (raíz, cuerpo, 4 alas, 2 antenas de 2 huesos, 3 pares de patas = ~16 huesos). |

### Polilla veloz

Más pequeña (Ø 0,46), **alas más grandes y erguidas**, antenas largas, rosa. Animación propia: **vuelo** (alas rápidas, cuerpo levantado, sombra que se encoge) y aterrizaje con aplastamiento. Mismo esqueleto que la común.

### Polilla acorazada

Grande y ancha (Ø 0,64), gris pizarra, **faldones y peto de fieltro cosido** (piezas separadas para que se caigan al romperse la placa), mejillas color mostaza, cejas más inclinadas. Animaciones propias: caminar pesado, **perder la placa**.

### Reina de las polillas

| | |
|---|---|
| Silueta | La más grande (Ø 0,82): cuerpo lila rosado, **alas moradas enormes** con manchas magenta y ribete rosa, **cuello de piel crema**, **capa magenta** de tela con botones dorados, **corona** con gema roja, sombra de ojos y pestañas. |
| Animaciones | Presentación (entrada de 2 s), idle majestuoso, caminar, **comerse una herramienta**, **llamar a su corte**, recibir daño, dar otra vuelta, derrota. |
| Presupuesto | 6.000–8.000 triángulos · 1024² · rig propio (~24 huesos: capa con 3, corona como hueso fijo). |

### Coherencia entre personajes

Mismos materiales, mismos ojos (esferas negras brillantes con dos reflejos blancos), mismas cejas de fieltro, misma proporción cabeza-cuerpo (son "bolas con cara"), mismo grosor de pespunte. **Un solo artista o un solo kit de piezas** para todos.

## 4.5 Tablero, objetos y escenario

- **Casillas:** cojines de fieltro 0,92 × 0,16 × 0,92 con bordes redondeados y pespunte claro, ligeramente girados al azar (±1,5°) para que parezcan cosidos a mano; separadas para que la cuadrícula se lea sin líneas dibujadas.
- **Tapete** verde oscuro acolchado bajo las casillas; **mesa de madera** debajo.
- **Obstáculos** (bajos, ≤ 0,6 de alto, para no tapar casillas): caja de costura, botón gigante, dedal, carrete pequeño, acerico, metro.
- **Agujero del rodapié** en el borde (entrada) y **carrete** con hilo rojo (salida): su grosor de hilo **es la vida**.
- **Lámpara** con pantalla de fieltro mostaza, bombilla emisiva y luz cálida que hace un charco de luz sobre las casillas vecinas.
- **Decorado** alrededor (bobinas gigantes, ovillos, botones sueltos, rollo de tela, tijeras): **solo detrás y a los lados**, nunca en el lado de la cámara. El postproceso los desenfoca.

## 4.6 Cámara e iluminación

*(La sección 14 del prompt llegó cortada; esta es la propuesta del equipo, ya implementada en el prototipo.)*

- **Cámara de maqueta:** perspectiva con **30° de campo** (sensación de miniatura, poca deformación), **inclinación 55°** (se ven todas las casillas y las caras de los personajes), fija. Rueda = zum ±25 %; **Q/E = girar la maqueta ±30°** para lucir el 3D sin perder la lectura. El encuadre se calcula solo para que el tablero ocupe el hueco central de la interfaz a cualquier resolución.
- **Luz:** "ventana" cálida (#FFE6C4) desde arriba a la izquierda con **sombras suaves**; relleno frío muy suave desde el lado contrario; ambiente de tres tonos (techo crema, paredes beige, suelo madera); luces puntuales cálidas en las lámparas.
- **Postproceso (URP):** desenfoque gaussiano del fondo a partir de ~17 unidades (efecto *tilt-shift*), bloom suave para bombillas, viñeta, gradación cálida y mapeado de tonos neutro. Niebla lineal oscura al fondo.

## 4.7 La lana del recorrido

Un **tubo de 0,22 de grosor** que sigue el recorrido con curvas de radio media casilla, ligeramente hundido en el fieltro (contacto) y que proyecta sombra. La textura tiene **tres cabos en hélice** (la torsión se ve al girar el tubo) y mapa de normales. Al tejer, **el último tramo crece** en vez de aparecer de golpe. Extremos redondeados.

## 4.8 Interfaz

Etiquetas y paneles de **fieltro con pespunte**, botones como parches cosidos, iconos de los propios objetos de costura, títulos en ciruela como el logo de la hoja de personajes. En el prototipo se dibuja con IMGUI y texturas generadas; la versión final se hará con UI Toolkit y piezas pintadas (ver `05_Interfaz_audio_animacion.md`).

## 4.9 Pipeline de producción (Blender → Unity)

1. **Modelado** en Blender a escala real de juego (1 casilla = 1 m), pivote en los pies, frente mirando a −Y de Blender (−Z de Unity).
2. **Nombres de grupo** iguales a los del placeholder (`cuerpo`, `ala_izq`, `ala_der`, `patas`, `ojos`, `cara`, `placas`, `capa`, `corona`, `bufanda`, `cola_bufanda`, `brazo_izq`, `brazo_der`, `pie_izq`, `pie_der`) para que el código siga encontrando las piezas mientras se sustituyen las animaciones procedurales por clips.
3. **Texturas** a 1024² (personajes principales) o 512² (polillas normales, herramientas); tileables de 512² para lana, fieltro, madera y tela. Exportar color (sRGB), normal (OpenGL, +Y) y máscara (rugosidad/pelusa).
4. **FBX** con *Apply Transform*, escala 1, solo deformadores en el rig. Animaciones como acciones separadas.
5. **Animación stop motion:** claves cada 2 fotogramas a 24 fps (12 poses/s), con *squash and stretch* exagerado. El placeholder de Ovillo ya se actualiza a 12 poses por segundo.
6. En Unity: modelo en `Assets/_Project/Arte/Modelos`, *Animation Type: Generic*, materiales con el shader *Textil*.

## 4.10 Lista de arte para la vertical slice (por prioridad)

| Prioridad | Pieza | Estimación (una persona) |
|---|---|---|
| 1 | Shader Graph *Textil* + texturas finales de lana, fieltro, madera, tela | 4–6 días |
| 1 | Ovillo: modelo, rig, 6 animaciones imprescindibles, expresiones | 6–8 días |
| 1 | Esqueleto compartido de polilla + polilla común (modelo + 8 animaciones) | 6–8 días |
| 2 | Veloz y acorazada (variantes del mismo esqueleto) | 5–6 días |
| 2 | Reina (modelo, rig propio, 8 animaciones) | 7–9 días |
| 2 | Cinco herramientas + cinco obstáculos + carrete + agujero + lámpara | 5–7 días |
| 3 | Decorado del taller (10–12 objetos de baja resolución) | 3–4 días |
| 3 | Interfaz final (etiquetas, paneles, iconos, logo) | 4–5 días |

**Total orientativo: 40–53 días de arte.** Para recortar: comprar un pack de objetos de costura de bajo coste y adaptar materiales; encargar solo el modelado de los 5 personajes y hacer rigs y animación en casa.
