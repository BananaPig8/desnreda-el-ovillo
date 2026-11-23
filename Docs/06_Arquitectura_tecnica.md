# 6. Arquitectura técnica

*(La sección 17 del primer prompt llegó cortada; este documento recoge la propuesta del equipo, ya implementada.)*

## 6.1 Principio rector

> **La simulación decide; la presentación cuenta lo que pasó.**

Las reglas viven en un núcleo de **C# puro** (sin `UnityEngine`) que resuelve cada oleada de forma instantánea y determinista y produce una **lista de eventos**. La capa de Unity solo reproduce esos eventos con animaciones, sonidos y textos. Ventajas para un desarrollador en solitario:

- Las reglas se **prueban en milisegundos** fuera del editor (57 pruebas) y en CI.
- El **laboratorio de balance** usa exactamente el mismo código que el juego.
- La presentación se puede rehacer (ya pasó: de 2D a 3D) **sin tocar una sola regla**.
- Nada depende de los fotogramas, la física ni el orden de los `Update`.

## 6.2 Estructura del repositorio

```
Assets/_Project/
  Scripts/Nucleo/        ← reglas (asmdef DesenredaElOvillo.Nucleo, noEngineReferences)
     Tablero, Recorrido (+validador), Definiciones, Reglas, Mejoras, Oleada (+PlanColocacion),
     SimuladorOleada (orden de resolución), Eventos, Partida (draft, mejoras, vida, reintento), Sala (formato), Rng
  Scripts/Juego/         ← presentación Unity (asmdef DesenredaElOvillo.Juego)
     ControladorJuego (estados y entrada), ReproductorOleada (eventos → animación),
     VistaTablero, VistaLana, VistaHerramientas, VistaPolilla, VistaOvillo, CamaraJuego,
     Interfaz (IMGUI), Iconos, Pintor, Paleta, Sonidos, Ajustes, EntradaJugador, Arranque
     Tres/  ← 3D: Mallas (geometría pura), TexturasTres, Modelos (piezas como datos), Decorado,
              Fabrica (Mesh, materiales URP/Standard, instanciado), Iluminacion, PostprocesoURP
  Scripts/Editor/        ← escena automática, plantillas de material, validar salas, importador de arte
  Tests/EditMode/        ← pruebas NUnit (Unity Test Runner y dotnet test)
  Resources/Salas/       ← sala1..sala5.txt
  Resources/Arte/        ← Ovillo, retratos (y en el futuro el arte final por nombre)
Tools/
  Nucleo/                ← compila el núcleo como .NET Standard 2.1 (el perfil de Unity)
  Pruebas/               ← dotnet test de las mismas pruebas
  Laboratorio/           ← balance: buscador de soluciones, informe, variantes de reglas
  VerificacionUnity/     ← compila los scripts de Unity contra UnityEngine sin abrir el editor
  VistaPreviaArte/       ← exporta la escena 3D y la renderiza con three.js (Chromium)
Docs/                    ← diseño, arte, producción, balance
```

## 6.3 Flujo de una sala

```
Partida ──(sala, costurero, reglas con mejoras)──► Preparación
   ▲                                                  │ el jugador teje (Recorrido) y coloca (PlanColocacion)
   │                                                  ▼
   │                                    Partida.CrearSimulador(recorrido, plan)
   │                                                  │
   │                     ┌────────── SimuladorOleada.EjecutarTurno() ──────────┐
   │                     │  devuelve los eventos del turno (Aparece, Mueve,    │
   │                     │  Sobrevuela, ActivaHerramienta, Danio, Retenida...) │
   │                     └──────────────────────────┬──────────────────────────┘
   │                                                ▼
   │                       ReproductorOleada: anima cada evento en orden
   │                       (VistaPolilla, VistaHerramientas, VistaTablero, Sonidos, textos)
   │                                                │ al terminar
   └──────────── Partida.AplicarResultado(resultado, hiloUsado) ◄──────────┘
                     → Recompensa / siguiente sala / Victoria / Derrota
```

El orden exacto de resolución de un turno está documentado en `02_Reglas_del_juego.md` y en la cabecera de `SimuladorOleada.cs`.

## 6.4 Determinismo

- Solo enteros en la simulación (la velocidad se cuenta en medios pasos para la media velocidad).
- Orden de actuación explícito (más avanzada primero; a igualdad, la que llegó antes).
- Azar solo fuera de la oleada, con un generador propio (`Rng`, SplitMix64) que da la misma secuencia en cualquier plataforma; la semilla identifica la partida.
- Prueba automática: dos simulaciones iguales producen exactamente los mismos eventos.

## 6.5 Presentación 3D

- **Prototipo sin modelos externos:** toda la geometría es procedural (`Mallas`: esferas, semiesferas, toros, cilindros, cajas acolchadas, el tubo de lana con transporte paralelo y remates, los **ovillos de hebras** —haces de cuatro vueltas de tubo que se cruzan— y los tubos de la bufanda). Los modelos se describen como **árboles de piezas** (`Modelos`) con malla, material, color, transformación, **grupo de animación**, capas de pelusa, sombra de contacto y si empiezan ocultas (expresiones alternativas). `Constructor` los convierte en GameObjects; el mismo árbol se exporta a la vista previa.
- **Mallas por clave:** `CatalogoMallas` genera y cachea cada malla a partir de una clave de texto (`"caja:0.92,0.16,0.92,0.05"`, `"hebras:24,0.3,0.045,7"`…). La pelusa es otra clave compuesta (`"hebras:…|pelusa:2"`): la misma malla inflada a lo largo de sus normales con UV de fibras. Así Unity y la vista previa usan exactamente la misma geometría.
- **Pelusa y sombras de contacto:** capas con recorte alfa (material `pelusa`, umbral por capa con `MaterialPropertyBlock`) y manchas de sombra con el shader de sprites (`SombraDinamica` las mantiene en el suelo cuando el personaje salta o vuela). Las plantillas `Pelusa` y `Sombra` de `Resources/Materiales` meten esos shaders y variantes en la build.
- **Expresiones como datos:** `Modelos.Rasgos(Expresion)` dice qué grupos se ven (ojo abierto, párpado, "^", "&gt;", bocas) y a qué escala; `VistaOvillo` lo aplica en el juego y la vista previa lo aplica a la descripción del modelo para dibujar la hoja de expresiones.
- **Sustitución por arte final:** basta con instanciar un FBX en lugar del árbol de piezas, manteniendo los nombres de grupo (ver `04_Direccion_de_arte.md`, 4.9).
- **Materiales:** `Materiales` usa **URP/Lit** si el proyecto tiene URP y **Standard** si no; texturas procedurales con mapa de normales; color por objeto con `MaterialPropertyBlock` (una textura sirve para todo el fieltro). Para las builds, el editor crea plantillas en `Resources/Materiales` que garantizan la inclusión del shader y sus variantes.
- **Cámara:** proyección perspectiva descentrada que coloca el tablero exactamente en la zona libre de la interfaz; la entrada del ratón se resuelve con un rayo contra el plano de las casillas (sin colliders).
- **URP opcional por compilación:** el postproceso está en `PostprocesoURP.cs` bajo `#if OVILLO_URP`, que el asmdef define solo si el paquete URP está instalado. Sin URP, el juego funciona con el pipeline integrado.

## 6.6 Interfaz

IMGUI escalado desde 1920×1080 para el prototipo (robusto, sin dependencias de paquetes ni de EventSystem, funciona con cualquier sistema de entrada). **Para la vertical slice:** migrar a **UI Toolkit** (UXML/USS) con el arte final de etiquetas de fieltro; la lógica ya está separada (`Interfaz` solo lee el estado de `ControladorJuego` y llama a sus métodos).

## 6.7 Datos

- **Salas** en texto plano editable (mapa ASCII, oleada, hilo, ofertas, tutorial, solución de referencia). Menú del editor *Desenreda el Ovillo → Validar salas*.
- **Números de equilibrio** en `Reglas.cs` (un solo sitio). El laboratorio permite probar cambios sin tocar el código: `--set "Alfiler.Usos=3;Reina.Vida=30"`.
- **Preferencias** en `PlayerPrefs` (volumen, velocidad, color de lana, modo feria). La demo no guarda partidas.

## 6.8 Calidad

| Herramienta | Qué comprueba | Cómo |
|---|---|---|
| Pruebas NUnit (57) | Cada regla, el orden de resolución, determinismo, partida, y que **las 5 salas siguen ganándose sin daño** | Unity Test Runner (EditMode) o `dotnet test Tools/Pruebas` |
| Verificación de compilación | Los scripts de Unity compilan (con avisos a cero) | `dotnet build Tools/VerificacionUnity` |
| CI (GitHub Actions) | Las dos anteriores en cada push | `.github/workflows/pruebas.yml` |
| Laboratorio | Dificultad de cada sala y escenario | `dotnet run -c Release --project Tools/Laboratorio -- informe` |
| Vista previa 3D | Geometría, materiales y composición sin abrir Unity | `Tools/VistaPreviaArte` (ver README) |
| Verificación de mallas | Las ~100 mallas procedurales (con sus capas de pelusa): valores finitos, normales unitarias, índices válidos y triángulos orientados como sus normales | `dotnet run --project Tools/VistaPreviaArte -- verificar` (también en CI) |
| Modelos de bloqueo | Exportación a glTF de los placeholders para Blender | `dotnet run --project Tools/VistaPreviaArte -- glb` |

## 6.9 Rendimiento (objetivo: 60 fps en un portátil de feria con gráfica integrada reciente)

- Escena pequeña: ~90 casillas (malla compartida, cada una con una capa de pelusa), < 500 renderers en el peor momento, 1 luz con sombras + 2 lámparas sin sombras.
- Prototipo procedural: Ovillo ~15.000 triángulos de hebras + 2 capas de pelusa (~46.000 en total); cada polilla ~6.000 + 2 capas (~17.000). En el peor momento (Reina + 6 polillas) son unos 300.000 triángulos: holgado para cualquier gráfica de los últimos años, pero **el arte final debe bajar** a ≤ 8.000 triángulos por personaje con la pelusa en el shader *Textil*.
- Personajes finales ≤ 8.000 triángulos; texturas ≤ 1024².
- Presupuesto: < 400 draw calls (sin SRP Batcher por los `MaterialPropertyBlock`; si hiciera falta, pasar a materiales por color o a GPU instancing).
- La lana se regenera solo al tejer (unos pocos miles de vértices).

## 6.10 Cómo ampliar

- **Nueva herramienta:** valor en `TipoHerramienta` + definición en `Reglas.PorDefecto` + efecto en `SimuladorOleada.ResolverCasilla` + prueba + modelo en `Modelos.Herramienta` + icono en `Iconos` + sonido.
- **Nueva polilla:** `TipoPolilla` + `DefinicionPolilla` (rasgos por datos: vuela, placas, come herramientas, umbrales, da vueltas) + modelo + retrato.
- **Nueva sala:** archivo de texto + `dotnet run -- fijar salaN --costurero ...` en el laboratorio para generar y guardar su solución de referencia.

## 6.11 Camino a Steam

Unity 6 LTS + URP, build de Windows x64 (Mac/Linux a valorar por coste de pruebas). Steamworks.NET para logros y página de la demo cuando toque. Versionado semántico en `Configuracion.Version`. Git LFS para el arte binario cuando crezca (`.gitattributes` ya lo deja preparado).
