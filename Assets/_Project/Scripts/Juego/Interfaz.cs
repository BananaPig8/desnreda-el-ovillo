using System;
using System.Collections.Generic;
using DesenredaElOvillo.Nucleo;
using UnityEngine;

namespace DesenredaElOvillo.Juego
{
    /// <summary>Ajustes de publicación. Rellenar cuando exista la página de Steam.</summary>
    public static class Configuracion
    {
        /// <summary>URL de la página de Steam (lista de deseados). Vacía = se muestra "Próximamente en Steam".</summary>
        public const string UrlSteam = "";
        public const string Version = "Prototipo 0.1";
    }

    /// <summary>
    /// Interfaz del prototipo con IMGUI, diseñada a 1920x1080 y escalada con bandas a cualquier resolución.
    /// Todo lo visual sale de texturas de fieltro cosido generadas por código. Para la vertical slice está
    /// previsto migrar a UI Toolkit o uGUI con el arte definitivo (ver Docs/08_Arquitectura_tecnica.md).
    /// </summary>
    public sealed class Interfaz : MonoBehaviour
    {
        private const float W = CamaraJuego.AnchoReferencia;
        private const float H = CamaraJuego.AltoReferencia;

        private readonly List<Rect> _zonas = new List<Rect>();
        private readonly List<int> _marcadas = new List<int>();
        private Estilos _e;
        private List<TipoHerramienta> _ofertaVista;
        private int _paginaAyuda;
        private string _ayudaEmergente;

        private static ControladorJuego C => ControladorJuego.Instancia;

        public bool RatonSobreInterfaz(Vector2 ratonPantalla)
        {
            var p = CamaraJuego.PantallaAReferencia(ratonPantalla);
            foreach (var r in _zonas) if (r.Contains(p)) return true;
            return false;
        }

        private void OnGUI()
        {
            if (C == null) return;
            if (_e == null) _e = new Estilos();
            float s = CamaraJuego.EscalaInterfaz;
            var d = CamaraJuego.DesplazamientoInterfaz;
            GUI.matrix = Matrix4x4.TRS(new Vector3(d.x, d.y, 0), Quaternion.identity, new Vector3(s, s, 1));
            if (Event.current.type == EventType.Repaint) _zonas.Clear();
            _ayudaEmergente = null;

            if (C.ErrorCarga != null)
            {
                Panel(new Rect(460, 380, 1000, 320), _e.Panel);
                GUI.Label(new Rect(500, 420, 920, 240), "No se han podido cargar las salas.\n\n" + C.ErrorCarga, _e.Texto);
                return;
            }

            switch (C.Pantalla)
            {
                case Pantalla.Menu: DibujarMenu(); break;
                case Pantalla.ComoJugar: DibujarAyuda(); break;
                case Pantalla.Ajustes: DibujarAjustes(); break;
                case Pantalla.Creditos: DibujarCreditos(); break;
                case Pantalla.Juego: DibujarJuego(); break;
            }

            if (_ayudaEmergente != null)
            {
                var m = Event.current.mousePosition;
                float alto = _e.TextoPequeno.CalcHeight(new GUIContent(_ayudaEmergente), 380) + 36;
                var r = new Rect(Mathf.Min(m.x + 18, W - 440), Mathf.Min(m.y + 18, H - alto - 10), 420, alto);
                GUI.Box(r, GUIContent.none, _e.PanelOscuro);
                GUI.Label(new Rect(r.x + 20, r.y + 18, 380, alto - 30), _ayudaEmergente, _e.TextoPequenoClaro);
            }
        }

        // ================================================================== utilidades

        private void Panel(Rect r, GUIStyle estilo)
        {
            GUI.Box(r, GUIContent.none, estilo);
            if (Event.current.type == EventType.Repaint) _zonas.Add(r);
        }

        private void Bloquear(Rect r)
        {
            if (Event.current.type == EventType.Repaint) _zonas.Add(r);
        }

        private bool Boton(Rect r, string texto, GUIStyle estilo = null, bool activo = true)
        {
            bool antes = GUI.enabled;
            GUI.enabled = activo;
            bool pulsado = GUI.Button(r, texto, estilo ?? _e.Boton);
            GUI.enabled = antes;
            if (pulsado) Sonidos.Tocar(Sonido.Clic);
            return pulsado;
        }

        private static bool Sobre(Rect r) => r.Contains(Event.current.mousePosition);

        private void Fundido(float alfa = 0.55f)
        {
            var antes = GUI.color;
            GUI.color = new Color(0.12f, 0.07f, 0.04f, alfa);
            GUI.DrawTexture(new Rect(-200, -200, W + 400, H + 400), Texture2D.whiteTexture);
            GUI.color = antes;
            Bloquear(new Rect(0, 0, W, H));
        }

        private static Texture2D Tex(Sprite s) => s != null ? s.texture : null;

        private static string Categoria(CategoriaHerramienta c)
        {
            switch (c)
            {
                case CategoriaHerramienta.Danio: return "Daño";
                case CategoriaHerramienta.Inmovilizacion: return "Inmovilización";
                case CategoriaHerramienta.Ralentizacion: return "Ralentización";
                default: return "Desplazamiento";
            }
        }

        private static string Ficha(DefinicionHerramienta d)
        {
            var partes = new List<string>();
            if (d.Danio > 0) partes.Add("Daño " + d.Danio);
            if (d.Turnos > 0) partes.Add(d.Turnos + (d.Turnos == 1 ? " turno" : " turnos"));
            if (d.Distancia > 0) partes.Add("Lanza " + d.Distancia + " casillas");
            partes.Add("Atrapa a " + d.Usos + (d.Usos == 1 ? " polilla" : " polillas"));
            return string.Join(" · ", partes);
        }

        private static string FichaPolilla(DefinicionPolilla d)
        {
            return d.Nombre + "\n" + d.Descripcion + "\n\nVida " + d.Vida + " · " + d.CasillasPorTurno +
                   (d.CasillasPorTurno == 1 ? " casilla" : " casillas") + " por turno · Muerde " + d.DanioAlCarrete;
        }

        // ================================================================== menú

        private void DibujarMenu()
        {
            var logo = new Rect(70, 70, 760, 250);
            Panel(logo, _e.PanelLogo);
            GUI.Label(new Rect(110, 100, 680, 120), "Desenreda\nel Ovillo", _e.Logo);
            GUI.Label(new Rect(110, 232, 680, 60), "No defiendas el camino. Constrúyelo para que se convierta en una trampa.", _e.LemaLogo);

            var col = new Rect(170, 380, 560, 610);
            Panel(col, _e.Panel);
            float y = col.y + 44;
            if (Boton(new Rect(col.x + 60, y, 440, 86), "Jugar")) C.Jugar();
            y += 106;
            if (Boton(new Rect(col.x + 60, y, 440, 76), "Cómo jugar", _e.BotonSecundario)) { _paginaAyuda = 0; C.Abrir(Pantalla.ComoJugar); }
            y += 94;
            if (Boton(new Rect(col.x + 60, y, 440, 76), "Ajustes", _e.BotonSecundario)) C.Abrir(Pantalla.Ajustes);
            y += 94;
            if (Boton(new Rect(col.x + 60, y, 440, 76), "Créditos", _e.BotonSecundario)) C.Abrir(Pantalla.Creditos);
            y += 94;
            if (Boton(new Rect(col.x + 60, y, 440, 76), "Salir", _e.BotonSecundario)) C.Salir();
            GUI.Label(new Rect(20, H - 44, 600, 30), Configuracion.Version, _e.TextoPequenoClaro);
        }

        private void DibujarAyuda()
        {
            Fundido(0.35f);
            var r = new Rect(210, 90, 1500, 900);
            Panel(r, _e.Panel);
            string[] titulos = { "El taller está en peligro", "Teje la lana", "Coloca tus herramientas", "Conoce a las polillas", "Controles" };
            GUI.Label(new Rect(r.x, r.y + 36, r.width, 70), titulos[_paginaAyuda], _e.Titulo);
            var cuerpo = new Rect(r.x + 70, r.y + 130, r.width - 140, 620);
            var reglas = C.ReglasBase;
            switch (_paginaAyuda)
            {
                case 0:
                    GUI.DrawTexture(new Rect(cuerpo.x, cuerpo.y + 20, 360, 360), Tex(Iconos.Ovillo()), ScaleMode.ScaleToFit);
                    GUI.Label(new Rect(cuerpo.x + 400, cuerpo.y, cuerpo.width - 400, 600),
                        "Las polillas han invadido el taller de costura y quieren comerse el carrete que Ovillo protege.\n\n" +
                        "Ovillo no lucha: TÚ tejes con su lana el camino que las polillas están obligadas a recorrer, " +
                        "y lo llenas de trampas de costura.\n\n" +
                        "Cada polilla que llega al carrete se come parte del hilo. El carrete aguanta " + reglas.VidaCarrete +
                        " mordiscos en toda la partida.\n\nSupera las cinco salas y derrota a la Reina de las polillas.", _e.Texto);
                    break;
                case 1:
                    GUI.Label(cuerpo,
                        "• Arrastra con el botón izquierdo desde el agujero del rodapié hasta el carrete.\n\n" +
                        "• Cada casilla de lana gasta 1 unidad de hilo. El hilo que no uses se guarda para la sala siguiente (hasta " + reglas.ReservaMaximaDeHilo + ").\n\n" +
                        "• No se puede atravesar objetos ni pasar dos veces por la misma casilla.\n\n" +
                        "• Arrastra hacia atrás para deshacer, o pulsa sobre la lana para recortarla desde ahí. Retroceso deshace el último tramo.\n\n" +
                        "• La forma importa: la polilla veloz vuela y solo toca el suelo donde aterriza… y en las curvas siempre tiene que aterrizar. " +
                        "Las huellas rosas te enseñan dónde.", _e.Texto);
                    break;
                case 2:
                {
                    GUI.Label(new Rect(cuerpo.x, cuerpo.y, cuerpo.width, 110),
                        "Elige una herramienta del costurero (o pulsa 1-5) y haz clic sobre la lana. Clic derecho para quitarla. " +
                        "Cada herramienta atrapa a tantas polillas distintas como usos tenga; a esas les vuelve a afectar cada vez que la pisan.", _e.Texto);
                    float y = cuerpo.y + 130;
                    foreach (TipoHerramienta t in Enum.GetValues(typeof(TipoHerramienta)))
                    {
                        var def = reglas.Herramienta(t);
                        GUI.DrawTexture(new Rect(cuerpo.x, y, 80, 80), Tex(Iconos.Herramienta(t)), ScaleMode.ScaleToFit);
                        GUI.Label(new Rect(cuerpo.x + 100, y + 2, 400, 40), def.Nombre + "  ·  " + Categoria(def.Categoria), _e.Subtitulo);
                        GUI.Label(new Rect(cuerpo.x + 100, y + 42, cuerpo.width - 100, 60), def.Descripcion + "  (" + Ficha(def) + ")", _e.TextoPequeno);
                        y += 96;
                    }
                    break;
                }
                case 3:
                {
                    var tipos = new[] { TipoPolilla.Comun, TipoPolilla.Veloz, TipoPolilla.Acorazada, TipoPolilla.Reina };
                    float ancho = (cuerpo.width - 60) / 4f;
                    for (int i = 0; i < tipos.Length; i++)
                    {
                        var def = reglas.Polilla(tipos[i]);
                        var x = cuerpo.x + i * (ancho + 20);
                        Panel(new Rect(x, cuerpo.y, ancho, 600), _e.Tarjeta);
                        var retrato = Iconos.Retrato(tipos[i]);
                        if (retrato != null) GUI.DrawTexture(new Rect(x + 20, cuerpo.y + 20, ancho - 40, 250), retrato, ScaleMode.ScaleToFit);
                        GUI.Label(new Rect(x + 16, cuerpo.y + 280, ancho - 32, 300), FichaPolilla(def), _e.TextoPequeno);
                    }
                    break;
                }
                default:
                    GUI.Label(cuerpo,
                        "Clic izquierdo y arrastrar: tejer la lana\n" +
                        "Clic izquierdo: elegir y colocar herramientas\n" +
                        "Clic derecho: quitar una herramienta o cancelar la selección\n" +
                        "1 - 5: elegir herramienta del costurero\n" +
                        "Retroceso o Ctrl+Z: deshacer el último tramo de lana\n" +
                        "Espacio: soltar a las polillas (y, durante la oleada, cambiar la velocidad)\n" +
                        "Escape: pausa", _e.Texto);
                    break;
            }
            if (_paginaAyuda > 0 && Boton(new Rect(r.x + 60, r.yMax - 120, 260, 76), "Anterior", _e.BotonSecundario)) _paginaAyuda--;
            if (_paginaAyuda < titulos.Length - 1 && Boton(new Rect(r.xMax - 320, r.yMax - 120, 260, 76), "Siguiente")) _paginaAyuda++;
            if (Boton(new Rect(r.center.x - 130, r.yMax - 120, 260, 76), "Volver", _e.BotonSecundario)) C.Volver();
            GUI.Label(new Rect(r.x, r.yMax - 40, r.width, 30), (_paginaAyuda + 1) + " / " + titulos.Length, _e.TextoCentrado);
        }

        private void DibujarAjustes()
        {
            Fundido(0.35f);
            var r = new Rect(460, 150, 1000, 780);
            Panel(r, _e.Panel);
            GUI.Label(new Rect(r.x, r.y + 36, r.width, 70), "Ajustes", _e.Titulo);
            float x = r.x + 80, y = r.y + 150;

            GUI.Label(new Rect(x, y, 400, 44), "Volumen", _e.Subtitulo);
            float vol = GUI.HorizontalSlider(new Rect(x + 420, y + 14, 420, 30), Ajustes.Volumen, 0f, 1f);
            if (Math.Abs(vol - Ajustes.Volumen) > 0.001f) Ajustes.Volumen = vol;
            y += 90;

            GUI.Label(new Rect(x, y, 420, 44), "Velocidad de las oleadas", _e.Subtitulo);
            int[] velocidades = { 1, 2, 4 };
            for (int i = 0; i < velocidades.Length; i++)
            {
                bool actual = Ajustes.Velocidad == velocidades[i];
                if (Boton(new Rect(x + 420 + i * 140, y - 6, 120, 60), "x" + velocidades[i], actual ? _e.Boton : _e.BotonSecundario)) Ajustes.Velocidad = velocidades[i];
            }
            y += 90;

            GUI.Label(new Rect(x, y, 420, 44), "Color de la lana", _e.Subtitulo);
            for (int i = 0; i < Paleta.ColoresLana.Length; i++)
            {
                var b = new Rect(x + 420 + i * 140, y - 6, 120, 60);
                bool actual = Ajustes.ColorLana == i;
                if (Boton(b, "", actual ? _e.Boton : _e.BotonSecundario)) Ajustes.ColorLana = i;
                var antes = GUI.color;
                GUI.color = Paleta.ColoresLana[i];
                GUI.DrawTexture(new Rect(b.x + 36, b.y + 12, 48, 36), Tex(Iconos.PiezaLana(Iconos.Derecha | Iconos.Izquierda)), ScaleMode.StretchToFill);
                GUI.color = antes;
                if (Sobre(b)) _ayudaEmergente = Paleta.NombresColoresLana[i] + ". La crema es la que mejor se distingue del tablero para todo tipo de visión del color.";
            }
            y += 90;

            Ajustes.ModoFeria = GUI.Toggle(new Rect(x, y, 840, 50), Ajustes.ModoFeria, "  Modo feria: tras perder, repetir la sala (no toda la partida)", _e.Interruptor);
            y += 70;
            Ajustes.MostrarTutorial = GUI.Toggle(new Rect(x, y, 840, 50), Ajustes.MostrarTutorial, "  Mostrar consejos durante la partida", _e.Interruptor);
            y += 70;
            bool completa = GUI.Toggle(new Rect(x, y, 840, 50), Screen.fullScreen, "  Pantalla completa", _e.Interruptor);
            if (completa != Screen.fullScreen) Screen.fullScreen = completa;

            if (Boton(new Rect(r.center.x - 150, r.yMax - 120, 300, 80), "Volver")) C.Volver();
        }

        private void DibujarCreditos()
        {
            Fundido(0.35f);
            var r = new Rect(460, 170, 1000, 740);
            Panel(r, _e.Panel);
            GUI.Label(new Rect(r.x, r.y + 36, r.width, 70), "Créditos", _e.Titulo);
            GUI.Label(new Rect(r.x + 80, r.y + 140, r.width - 160, 420),
                "Desenreda el Ovillo\nUn juego indie hecho a mano, puntada a puntada.\n\n" +
                "Diseño, programación y arte: (tu nombre aquí)\n" +
                "Música y sonido: provisionales, sintetizados en el propio juego\n\n" +
                "Hecho con Unity.\n" + Configuracion.Version, _e.TextoCentrado);
            if (Boton(new Rect(r.center.x - 150, r.yMax - 120, 300, 80), "Volver")) C.Volver();
        }

        // ================================================================== partida

        private void DibujarJuego()
        {
            var p = C.Partida;
            if (p == null) return;
            DibujarTextosFlotantes();
            DibujarPresentacion();
            DibujarBarraSuperior(p);
            DibujarCosturero(p);
            DibujarPanelOleada(p);
            DibujarBarraInferior(p);
            DibujarMensaje();

            if (!C.EnOleada)
            {
                switch (p.Fase)
                {
                    case FasePartida.EleccionHerramientas: DibujarEleccion(p); break;
                    case FasePartida.Recompensa: DibujarRecompensa(p); break;
                    case FasePartida.Victoria: DibujarVictoria(p); break;
                    case FasePartida.Derrota: DibujarDerrota(p); break;
                }
            }
            if (C.Pausado) DibujarPausa();
        }

        private void DibujarBarraSuperior(Partida p)
        {
            var r = new Rect(20, 14, W - 40, 112);
            Panel(r, _e.PanelOscuro);
            GUI.DrawTexture(new Rect(r.x + 24, r.y + 8, 96, 96), Tex(Iconos.Ovillo()), ScaleMode.ScaleToFit);
            GUI.Label(new Rect(r.x + 134, r.y + 14, 260, 40), "Carrete", _e.SubtituloClaro);
            int vida = C.EnOleada ? C.Reproductor.VidaCarreteMostrada : p.VidaCarrete;
            int maximo = C.ReglasBase.VidaCarreteMaxima;
            for (int i = 0; i < maximo; i++)
            {
                var spr = Iconos.Carrete(i < vida ? 1 : 0, 1);
                var antes = GUI.color;
                if (i >= vida) GUI.color = new Color(1, 1, 1, 0.45f);
                GUI.DrawTexture(new Rect(r.x + 134 + i * 52, r.y + 52, 50, 50), Tex(spr), ScaleMode.ScaleToFit);
                GUI.color = antes;
            }
            if (Sobre(new Rect(r.x + 20, r.y, 360, r.height)))
                _ayudaEmergente = "Vida del carrete: " + vida + " de " + maximo + ". Es la misma para toda la partida. Común y veloz muerden 1; acorazada y Reina, 2.";

            var sala = p.SalaActual;
            GUI.Label(new Rect(r.center.x - 450, r.y + 10, 900, 50), "Sala " + p.NumeroSala + " de " + p.Salas.Count + "  ·  " + sala.Nombre, _e.TituloClaro);
            GUI.Label(new Rect(r.center.x - 450, r.y + 62, 900, 40), sala.Subtitulo, _e.TextoCentradoClaro);

            int usado = C.Recorrido == null ? 0 : C.Recorrido.HiloUsado;
            int total = p.HiloDisponible;
            var hilo = new Rect(r.xMax - 420, r.y + 10, 400, 92);
            var colorAntes = GUI.color;
            GUI.color = Paleta.ColoresLana[Ajustes.ColorLana];
            GUI.DrawTexture(new Rect(hilo.x, hilo.y + 4, 84, 84), Tex(Iconos.PiezaLana(Iconos.Arriba)), ScaleMode.ScaleToFit);
            GUI.color = colorAntes;
            GUI.Label(new Rect(hilo.x + 96, hilo.y, 300, 52), "Hilo  " + (total - usado) + " / " + total, _e.Contador);
            GUI.Label(new Rect(hilo.x + 96, hilo.y + 52, 300, 36),
                "Mínimo " + sala.LongitudMinima + (p.HiloReserva > 0 ? "  ·  +" + p.HiloReserva + " de reserva" : ""), _e.TextoPequenoClaro);
            if (Sobre(hilo))
                _ayudaEmergente = "Cada casilla de lana gasta 1 de hilo. El camino debe medir al menos " + sala.LongitudMinima +
                                  ". Lo que sobre se guarda para la próxima sala (hasta " + C.ReglasBase.ReservaMaximaDeHilo + ").";
        }

        private void DibujarCosturero(Partida p)
        {
            var r = new Rect(20, 140, 336, 806);
            Panel(r, _e.Panel);
            GUI.Label(new Rect(r.x, r.y + 22, r.width, 50), "Costurero", _e.Subtitulo2);
            float y = r.y + 82;
            int tecla = 0;
            foreach (TipoHerramienta t in Enum.GetValues(typeof(TipoHerramienta)))
            {
                tecla++;
                int total = 0;
                foreach (var k in p.Costurero) if (k == t) total++;
                if (total == 0) continue;
                int libres = C.Disponibles(t);
                var fila = new Rect(r.x + 18, y, r.width - 36, 92);
                bool seleccionada = C.Seleccionada == t;
                bool activo = p.Fase == FasePartida.Preparacion && !C.EnOleada;
                if (GUI.Button(fila, GUIContent.none, seleccionada ? _e.TarjetaMarcada : _e.Tarjeta) && activo)
                    C.Seleccionar(seleccionada ? (TipoHerramienta?)null : t);
                GUI.DrawTexture(new Rect(fila.x + 10, fila.y + 8, 76, 76), Tex(Iconos.Herramienta(t)), ScaleMode.ScaleToFit);
                var def = p.Reglas.Herramienta(t);
                GUI.Label(new Rect(fila.x + 94, fila.y + 10, 190, 36), def.Nombre, _e.TextoNegrita);
                GUI.Label(new Rect(fila.x + 94, fila.y + 46, 190, 34), libres + " de " + total + " libres", _e.TextoPequeno);
                GUI.Label(new Rect(fila.xMax - 44, fila.y + 8, 34, 30), tecla.ToString(), _e.Tecla);
                if (Sobre(fila)) _ayudaEmergente = def.Nombre + " (" + Categoria(def.Categoria) + ")\n" + def.Descripcion + "\n" + Ficha(def);
                y += 100;
            }

            if (p.Mejoras.Count > 0)
            {
                GUI.Label(new Rect(r.x + 24, r.yMax - 210, r.width - 48, 36), "Mejoras", _e.TextoNegrita);
                string lista = "";
                foreach (var m in p.Mejoras) lista += "• " + CatalogoMejoras.Obtener(m).Nombre + "\n";
                GUI.Label(new Rect(r.x + 24, r.yMax - 172, r.width - 48, 160), lista, _e.TextoPequeno);
            }
        }

        private void DibujarPanelOleada(Partida p)
        {
            var r = new Rect(W - 356, 140, 336, 806);
            Panel(r, _e.Panel);
            GUI.Label(new Rect(r.x, r.y + 22, r.width, 50), "Oleada", _e.Subtitulo2);
            var sala = p.SalaActual;
            float x = r.x + 22, y = r.y + 80;
            int i = 0;
            foreach (var a in sala.Oleada.Apariciones)
            {
                var celda = new Rect(x + (i % 4) * 74, y + (i / 4) * 90, 68, 84);
                var retrato = Iconos.Retrato(a.Tipo);
                if (retrato != null) GUI.DrawTexture(new Rect(celda.x, celda.y, 68, 64), retrato, ScaleMode.ScaleAndCrop);
                GUI.Label(new Rect(celda.x, celda.y + 62, 68, 24), "T" + a.Turno, _e.TextoMini);
                if (Sobre(celda)) _ayudaEmergente = FichaPolilla(p.Reglas.Polilla(a.Tipo)) + "\nEntra en el turno " + a.Turno + ".";
                i++;
            }
            y += ((i + 3) / 4) * 90 + 10;

            if (sala.Oleada.Contar(TipoPolilla.Reina) > 0)
            {
                GUI.Label(new Rect(x, y, r.width - 44, 60), "La Reina llamará a 2 polillas más al perder vida.", _e.TextoPequeno);
                y += 62;
            }

            if (Ajustes.MostrarTutorial && p.Fase == FasePartida.Preparacion && !C.EnOleada)
            {
                GUI.Label(new Rect(x, y, r.width - 44, 36), "Consejo", _e.TextoNegrita);
                y += 38;
                string consejo = Consejo(p);
                float alto = _e.TextoPequeno.CalcHeight(new GUIContent(consejo), r.width - 44);
                GUI.Label(new Rect(x, y, r.width - 44, alto), consejo, _e.TextoPequeno);
                y += alto + 16;
            }

            if (p.Fase == FasePartida.Preparacion && !C.EnOleada)
            {
                var problemas = C.ProblemasParaEmpezar();
                float zona = r.yMax - 190;
                GUI.Label(new Rect(x, Mathf.Max(y, zona), r.width - 44, 36), problemas.Count == 0 ? "¡Todo listo!" : "Antes de empezar", _e.TextoNegrita);
                string txt = problemas.Count == 0
                    ? (C.HerramientasSinColocar() > 0 ? "Te quedan herramientas sin colocar. " : "") + "Pulsa ESPACIO para soltar a las polillas."
                    : problemas[0];
                GUI.Label(new Rect(x, Mathf.Max(y, zona) + 38, r.width - 44, 130), txt, _e.TextoPequeno);
            }
            else if (C.EnOleada)
            {
                GUI.Label(new Rect(x, r.yMax - 150, r.width - 44, 40), "Turno " + C.Reproductor.TurnoActual, _e.TextoNegrita);
                GUI.Label(new Rect(x, r.yMax - 108, r.width - 44, 80), "Polillas en el taller: " + C.Reproductor.PolillasEnTablero, _e.TextoPequeno);
            }
        }

        private string Consejo(Partida p)
        {
            var sala = p.SalaActual;
            var tut = sala.Tutorial;
            if (p.IndiceSala == 0 && tut.Count >= 5)
            {
                if (!C.Recorrido.EstaCompleto) return tut[0] + "\n\n" + tut[1];
                if (C.Plan.Contar(TipoHerramienta.Alfiler) == 0) return tut[2];
                if (C.Plan.Contar(TipoHerramienta.Velcro) == 0) return tut[3];
                return tut[4];
            }
            return sala.Objetivo.Length > 0 ? sala.Objetivo : string.Join("\n", tut);
        }

        private void DibujarBarraInferior(Partida p)
        {
            var r = new Rect(ControladorJuego.ZonaTablero.x, 958, ControladorJuego.ZonaTablero.width, 108);
            Panel(r, _e.PanelOscuro);
            if (C.EnOleada)
            {
                GUI.Label(new Rect(r.x + 30, r.y + 30, 500, 50), "¡Las polillas avanzan!", _e.SubtituloClaro);
                int[] velocidades = { 1, 2, 4 };
                for (int i = 0; i < velocidades.Length; i++)
                {
                    bool actual = Ajustes.Velocidad == velocidades[i];
                    if (Boton(new Rect(r.xMax - 470 + i * 150, r.y + 20, 130, 68), "x" + velocidades[i], actual ? _e.Boton : _e.BotonSecundario))
                        Ajustes.Velocidad = velocidades[i];
                }
                return;
            }
            if (p.Fase != FasePartida.Preparacion) return;
            if (Boton(new Rect(r.x + 24, r.y + 18, 250, 72), "Reiniciar lana", _e.BotonSecundario)) C.ReiniciarLana();
            if (Boton(new Rect(r.x + 290, r.y + 18, 290, 72), "Quitar herramientas", _e.BotonSecundario)) C.QuitarHerramientas();
            string modo = C.Seleccionada.HasValue
                ? "Colocando: " + p.Reglas.Herramienta(C.Seleccionada.Value).Nombre + "  (clic derecho: cancelar)"
                : "Modo: tejer lana";
            GUI.Label(new Rect(r.x + 600, r.y + 10, 290, 88), modo, _e.TextoPequenoClaro);
            bool listo = C.ProblemasParaEmpezar().Count == 0;
            if (Boton(new Rect(r.xMax - 300, r.y + 14, 276, 80), "¡Soltar polillas!\n(Espacio)", _e.BotonGrande, listo)) C.EmpezarOleada();
        }

        private void DibujarMensaje()
        {
            if (string.IsNullOrEmpty(C.Mensaje) || Time.unscaledTime > C.MensajeHasta) return;
            float alfa = Mathf.Clamp01((C.MensajeHasta - Time.unscaledTime) * 2f);
            var antes = GUI.color;
            GUI.color = new Color(1, 1, 1, alfa);
            var r = new Rect(W / 2 - 460, 140, 920, 70);
            GUI.Box(r, GUIContent.none, _e.PanelOscuro);
            GUI.Label(new Rect(r.x + 20, r.y + 8, r.width - 40, r.height - 16), C.Mensaje, _e.TextoCentradoClaro);
            GUI.color = antes;
        }

        private void DibujarTextosFlotantes()
        {
            foreach (var t in C.Reproductor.Textos)
            {
                float edad = Time.time - t.Nacimiento;
                float k = Mathf.Clamp01(edad / t.Vida);
                var pos = C.Camara.MundoAReferencia(t.Mundo + Vector3.up * k * 0.6f);
                var estilo = _e.Flotante;
                estilo.fontSize = Mathf.RoundToInt(t.Tamano);
                var r = new Rect(pos.x - 300, pos.y - 30, 600, 60);
                var antes = GUI.color;
                GUI.color = new Color(0.2f, 0.1f, 0.05f, (1 - k) * 0.85f);
                GUI.Label(new Rect(r.x + 3, r.y + 3, r.width, r.height), t.Texto, estilo);
                GUI.color = new Color(t.Color.r, t.Color.g, t.Color.b, 1 - k * k);
                GUI.Label(r, t.Texto, estilo);
                GUI.color = antes;
            }
        }

        /// <summary>Rótulo del jefe: una etiqueta de fieltro que entra desde la izquierda con su retrato.</summary>
        private void DibujarPresentacion()
        {
            var pr = C.Reproductor.Presentacion;
            if (pr == null) return;
            float edad = Time.time - pr.Inicio;
            if (edad < 0f || edad > pr.Duracion) return;
            float entra = Mathf.Clamp01(edad / 0.3f), sale = Mathf.Clamp01((pr.Duracion - edad) / 0.3f);
            float k = Mathf.Min(1f - (1f - entra) * (1f - entra), sale);
            var r = new Rect(Mathf.Lerp(-1100f, W / 2f - 540f, k), 196, 1080, 190);
            var antes = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(k * 1.5f));
            Panel(r, _e.PanelLogo);
            var retrato = Iconos.Retrato(pr.Retrato);
            if (retrato != null) GUI.DrawTexture(new Rect(r.x + 24, r.y + 10, 170, 170), retrato, ScaleMode.ScaleToFit);
            GUI.Label(new Rect(r.x + 210, r.y + 26, r.width - 240, 70), pr.Titulo, _e.Logo2);
            GUI.Label(new Rect(r.x + 214, r.y + 110, r.width - 240, 60), pr.Subtitulo, _e.LemaLogo);
            GUI.color = antes;
        }

        // ================================================================== ventanas

        private void DibujarEleccion(Partida p)
        {
            Fundido(0.45f);
            var oferta = p.OfertaHerramientas;
            if (!ReferenceEquals(_ofertaVista, oferta))
            {
                _ofertaVista = oferta;
                _marcadas.Clear();
            }
            int elegir = Mathf.Min(p.SalaActual.CantidadAElegir, oferta.Count);
            var r = new Rect(310, 150, 1300, 790);
            Panel(r, _e.Panel);
            GUI.Label(new Rect(r.x, r.y + 30, r.width, 60), "Sala " + p.NumeroSala + ": " + p.SalaActual.Nombre, _e.Titulo);
            GUI.Label(new Rect(r.x, r.y + 100, r.width, 44), "Elige " + elegir + " herramientas para tu costurero. Se quedan contigo toda la partida.", _e.TextoCentrado);
            float ancho = 360, alto = 470, hueco = 40;
            float x0 = r.center.x - (oferta.Count * ancho + (oferta.Count - 1) * hueco) / 2f;
            for (int i = 0; i < oferta.Count; i++)
            {
                var t = oferta[i];
                var def = p.Reglas.Herramienta(t);
                var tarjeta = new Rect(x0 + i * (ancho + hueco), r.y + 170, ancho, alto);
                bool marcada = _marcadas.Contains(i);
                if (GUI.Button(tarjeta, GUIContent.none, marcada ? _e.TarjetaMarcada : _e.Tarjeta))
                {
                    if (marcada) _marcadas.Remove(i);
                    else if (_marcadas.Count < elegir) _marcadas.Add(i);
                    Sonidos.Tocar(Sonido.Clic);
                }
                GUI.DrawTexture(new Rect(tarjeta.center.x - 80, tarjeta.y + 30, 160, 160), Tex(Iconos.Herramienta(t)), ScaleMode.ScaleToFit);
                GUI.Label(new Rect(tarjeta.x, tarjeta.y + 200, ancho, 44), def.Nombre, _e.Subtitulo);
                GUI.Label(new Rect(tarjeta.x, tarjeta.y + 244, ancho, 34), Categoria(def.Categoria), _e.TextoCentrado);
                GUI.Label(new Rect(tarjeta.x + 26, tarjeta.y + 286, ancho - 52, 130), def.Descripcion, _e.TextoPequenoCentrado);
                GUI.Label(new Rect(tarjeta.x + 26, tarjeta.y + 410, ancho - 52, 50), Ficha(def), _e.TextoMini);
                if (marcada) GUI.Label(new Rect(tarjeta.x, tarjeta.y + 4, ancho, 30), "ELEGIDA", _e.Marca);
            }
            bool listo = _marcadas.Count == elegir;
            if (Boton(new Rect(r.center.x - 200, r.yMax - 120, 400, 86), listo ? "¡Al costurero!" : "Elige " + (elegir - _marcadas.Count) + " más", _e.Boton, listo))
            {
                var elegidas = new List<TipoHerramienta>();
                foreach (int i in _marcadas) elegidas.Add(oferta[i]);
                _marcadas.Clear();
                C.ElegirHerramientas(elegidas);
            }
        }

        private void DibujarRecompensa(Partida p)
        {
            Fundido(0.45f);
            var r = new Rect(310, 170, 1300, 740);
            Panel(r, _e.Panel);
            GUI.Label(new Rect(r.x, r.y + 30, r.width, 60), "¡Sala superada!", _e.Titulo);
            GUI.Label(new Rect(r.x, r.y + 100, r.width, 44), "Ovillo ha encontrado algo útil en el taller. Elige una mejora:", _e.TextoCentrado);
            var oferta = p.OfertaMejoras;
            float ancho = 360, alto = 440, hueco = 40;
            float x0 = r.center.x - (oferta.Count * ancho + (oferta.Count - 1) * hueco) / 2f;
            for (int i = 0; i < oferta.Count; i++)
            {
                var m = CatalogoMejoras.Obtener(oferta[i]);
                var tarjeta = new Rect(x0 + i * (ancho + hueco), r.y + 170, ancho, alto);
                if (GUI.Button(tarjeta, GUIContent.none, _e.Tarjeta))
                {
                    C.ElegirMejora(m.Tipo);
                    return;
                }
                Texture2D icono = m.Herramienta.HasValue ? Tex(Iconos.Herramienta(m.Herramienta.Value))
                    : m.Tipo == TipoMejora.OvilloGrande ? Tex(Iconos.Ovillo()) : Tex(Iconos.Carrete(1, 1));
                GUI.DrawTexture(new Rect(tarjeta.center.x - 80, tarjeta.y + 34, 160, 160), icono, ScaleMode.ScaleToFit);
                GUI.Label(new Rect(tarjeta.x, tarjeta.y + 214, ancho, 50), m.Nombre, _e.Subtitulo);
                GUI.Label(new Rect(tarjeta.x + 26, tarjeta.y + 274, ancho - 52, 150), m.Descripcion, _e.TextoPequenoCentrado);
            }
        }

        private static string Tiempo(float segundos)
        {
            int s = Mathf.FloorToInt(segundos);
            return (s / 60) + ":" + (s % 60).ToString("00");
        }

        private void DibujarVictoria(Partida p)
        {
            Fundido(0.4f);
            var r = new Rect(260, 110, 1400, 860);
            Panel(r, _e.Panel);
            float bote = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5f)) * 30f;
            GUI.DrawTexture(new Rect(r.x + 60, r.y + 160 - bote, 420, 420), Tex(Iconos.Ovillo()), ScaleMode.ScaleToFit);
            GUI.Label(new Rect(r.x + 500, r.y + 50, r.width - 560, 80), "¡Has salvado el taller!", _e.Titulo);
            var e = p.Estadisticas;
            GUI.Label(new Rect(r.x + 520, r.y + 160, r.width - 580, 300),
                "Polillas derrotadas: " + e.PolillasDerrotadas + "\n" +
                "Hilo utilizado: " + e.HiloUsado + "\n" +
                "Vida del carrete: " + p.VidaCarrete + " / " + C.ReglasBase.VidaCarreteMaxima + "\n" +
                "Tiempo: " + Tiempo(C.TiempoPartida) + "\n" +
                (e.Reintentos > 0 ? "Reintentos: " + e.Reintentos : "¡Sin reintentos!"), _e.TextoGrande);
            var invitacion = new Rect(r.x + 520, r.y + 470, r.width - 580, 170);
            Panel(invitacion, _e.TarjetaMarcada);
            GUI.Label(new Rect(invitacion.x + 30, invitacion.y + 20, invitacion.width - 60, 80),
                "¿Te ha gustado? Añade Desenreda el Ovillo a tu lista de deseados de Steam para no perderte el juego completo.", _e.Texto);
            if (string.IsNullOrEmpty(Configuracion.UrlSteam))
                GUI.Label(new Rect(invitacion.x + 30, invitacion.y + 108, invitacion.width - 60, 44), "Próximamente en Steam", _e.TextoNegrita);
            else if (Boton(new Rect(invitacion.x + 30, invitacion.y + 100, 380, 60), "Abrir la página de Steam"))
                Application.OpenURL(Configuracion.UrlSteam);
            if (Boton(new Rect(r.x + 520, r.yMax - 130, 380, 86), "Jugar otra vez")) C.Jugar();
            if (Boton(new Rect(r.x + 930, r.yMax - 130, 380, 86), "Volver al menú", _e.BotonSecundario)) C.IrAlMenu();
        }

        private void DibujarDerrota(Partida p)
        {
            Fundido(0.5f);
            var r = new Rect(360, 160, 1200, 760);
            Panel(r, _e.Panel);
            GUI.Label(new Rect(r.x, r.y + 40, r.width, 80), "¡Las polillas se han comido el carrete!", _e.Titulo);
            var retrato = Iconos.Retrato("comun");
            if (retrato != null) GUI.DrawTexture(new Rect(r.x + 70, r.y + 170, 330, 330), retrato, ScaleMode.ScaleToFit);
            var e = p.Estadisticas;
            GUI.Label(new Rect(r.x + 440, r.y + 170, r.width - 500, 300),
                "Llegaste a la sala " + p.NumeroSala + ": " + p.SalaActual.Nombre + "\n" +
                "Polillas derrotadas: " + e.PolillasDerrotadas + "\n" +
                "Salas superadas: " + e.SalasSuperadas + "\n" +
                "Tiempo: " + Tiempo(C.TiempoPartida), _e.TextoGrande);
            string pista = p.ReintentarDesdeSala
                ? "Repetirás esta sala con el mismo costurero, y tu lana y tus herramientas tal y como las dejaste."
                : "Empezarás una partida nueva.";
            GUI.Label(new Rect(r.x + 440, r.y + 440, r.width - 500, 90), pista, _e.TextoPequeno);
            if (Boton(new Rect(r.x + 440, r.yMax - 140, 360, 86), "Volver a intentarlo")) C.Reintentar();
            if (Boton(new Rect(r.x + 820, r.yMax - 140, 320, 86), "Menú principal", _e.BotonSecundario)) C.IrAlMenu();
        }

        private void DibujarPausa()
        {
            Fundido(0.55f);
            var r = new Rect(660, 200, 600, 680);
            Panel(r, _e.Panel);
            GUI.Label(new Rect(r.x, r.y + 36, r.width, 70), "Pausa", _e.Titulo);
            float y = r.y + 150;
            if (Boton(new Rect(r.x + 80, y, 440, 80), "Continuar")) C.Pausar(false);
            y += 100;
            if (Boton(new Rect(r.x + 80, y, 440, 76), "Cómo jugar", _e.BotonSecundario)) { _paginaAyuda = 0; C.Abrir(Pantalla.ComoJugar); }
            y += 96;
            if (Boton(new Rect(r.x + 80, y, 440, 76), "Ajustes", _e.BotonSecundario)) C.Abrir(Pantalla.Ajustes);
            y += 96;
            if (Boton(new Rect(r.x + 80, y, 440, 76), "Abandonar partida", _e.BotonSecundario)) C.IrAlMenu();
        }

        // ================================================================== estilos

        /// <summary>Estilos de fieltro y etiquetas de tela. Se crean dentro de OnGUI (GUI.skin solo existe ahí).</summary>
        private sealed class Estilos
        {
            public readonly GUIStyle Panel, PanelOscuro, PanelLogo, Tarjeta, TarjetaMarcada;
            public readonly GUIStyle Boton, BotonSecundario, BotonGrande, Interruptor;
            public readonly GUIStyle Logo, Logo2, LemaLogo, Titulo, TituloClaro, Subtitulo, Subtitulo2, SubtituloClaro;
            public readonly GUIStyle Texto, TextoGrande, TextoNegrita, TextoCentrado, TextoCentradoClaro, TextoPequeno, TextoPequenoCentrado, TextoPequenoClaro, TextoMini;
            public readonly GUIStyle Contador, Tecla, Marca, Flotante;

            public Estilos()
            {
                var ciruela = Paleta.Hex("#6E2F4C");
                Panel = Caja(Iconos.Panel(Paleta.Crema, Paleta.Hex("#C98A5A"), "ui_panel"), 30);
                PanelOscuro = Caja(Iconos.Panel(Paleta.Hex("#5A3A28"), Paleta.Hex("#E9C79E"), "ui_panel_oscuro"), 30);
                PanelLogo = Caja(Iconos.Panel(Paleta.Hex("#F4D3A8"), Paleta.Hex("#B5683D"), "ui_panel_logo"), 30);
                Tarjeta = Caja(Iconos.Panel(Paleta.Hex("#FBEBD2"), Paleta.Hex("#B98AC2"), "ui_tarjeta"), 30);
                Tarjeta.hover.background = Iconos.Panel(Paleta.Hex("#FFF3DF"), Paleta.Hex("#9B6CC0"), "ui_tarjeta_hover");
                TarjetaMarcada = Caja(Iconos.Panel(Paleta.Hex("#FFE3A3"), Paleta.Coral, "ui_tarjeta_marcada"), 30);
                TarjetaMarcada.hover.background = TarjetaMarcada.normal.background;

                Boton = Caja(Iconos.Panel(Paleta.Coral, Paleta.Crema, "ui_boton"), 26);
                Boton.hover.background = Iconos.Panel(Paleta.Aclarar(Paleta.Coral, 0.12f), Paleta.Crema, "ui_boton_hover");
                Boton.active.background = Iconos.Panel(Paleta.Oscurecer(Paleta.Coral, 0.15f), Paleta.Crema, "ui_boton_activo");
                Boton.normal.textColor = Boton.hover.textColor = Boton.active.textColor = Color.white;
                Boton.fontSize = 30;
                Boton.fontStyle = FontStyle.Bold;
                Boton.alignment = TextAnchor.MiddleCenter;

                BotonSecundario = Caja(Iconos.Panel(Paleta.Hex("#F3DDBB"), Paleta.Hex("#B5683D"), "ui_boton2"), 26);
                BotonSecundario.hover.background = Iconos.Panel(Paleta.Hex("#FBEAD0"), Paleta.Hex("#B5683D"), "ui_boton2_hover");
                BotonSecundario.active.background = Iconos.Panel(Paleta.Hex("#E2C59C"), Paleta.Hex("#B5683D"), "ui_boton2_activo");
                BotonSecundario.normal.textColor = BotonSecundario.hover.textColor = BotonSecundario.active.textColor = Paleta.Tinta;
                BotonSecundario.fontSize = 26;
                BotonSecundario.fontStyle = FontStyle.Bold;
                BotonSecundario.alignment = TextAnchor.MiddleCenter;

                BotonGrande = new GUIStyle(Boton) { fontSize = 26 };

                Interruptor = new GUIStyle(GUI.skin.toggle) { fontSize = 26 };
                Interruptor.normal.textColor = Interruptor.onNormal.textColor = Interruptor.hover.textColor = Interruptor.onHover.textColor = Paleta.Tinta;

                Logo = TextoBase(88, ciruela, FontStyle.Bold, TextAnchor.UpperLeft);
                Logo.wordWrap = false;
                Logo2 = TextoBase(62, ciruela, FontStyle.Bold, TextAnchor.UpperLeft);
                Logo2.wordWrap = false;
                LemaLogo = TextoBase(22, Paleta.MarronCalido, FontStyle.Italic, TextAnchor.UpperLeft);
                Titulo = TextoBase(54, ciruela, FontStyle.Bold, TextAnchor.UpperCenter);
                TituloClaro = TextoBase(40, Paleta.Crema, FontStyle.Bold, TextAnchor.UpperCenter);
                Subtitulo = TextoBase(32, ciruela, FontStyle.Bold, TextAnchor.UpperCenter);
                Subtitulo2 = TextoBase(36, ciruela, FontStyle.Bold, TextAnchor.UpperCenter);
                SubtituloClaro = TextoBase(30, Paleta.Crema, FontStyle.Bold, TextAnchor.UpperLeft);
                Texto = TextoBase(27, Paleta.Tinta, FontStyle.Normal, TextAnchor.UpperLeft);
                TextoGrande = TextoBase(34, Paleta.Tinta, FontStyle.Normal, TextAnchor.UpperLeft);
                TextoNegrita = TextoBase(26, Paleta.Tinta, FontStyle.Bold, TextAnchor.UpperLeft);
                TextoCentrado = TextoBase(27, Paleta.Tinta, FontStyle.Normal, TextAnchor.UpperCenter);
                TextoCentradoClaro = TextoBase(26, Paleta.Crema, FontStyle.Normal, TextAnchor.MiddleCenter);
                TextoPequeno = TextoBase(22, Paleta.Tinta, FontStyle.Normal, TextAnchor.UpperLeft);
                TextoPequenoCentrado = TextoBase(22, Paleta.Tinta, FontStyle.Normal, TextAnchor.UpperCenter);
                TextoPequenoClaro = TextoBase(21, Paleta.Crema, FontStyle.Normal, TextAnchor.UpperLeft);
                TextoMini = TextoBase(18, Paleta.MarronCalido, FontStyle.Bold, TextAnchor.UpperCenter);
                Contador = TextoBase(40, Paleta.Crema, FontStyle.Bold, TextAnchor.UpperLeft);
                Tecla = TextoBase(20, Paleta.Hex("#9B6CC0"), FontStyle.Bold, TextAnchor.UpperRight);
                Marca = TextoBase(22, Paleta.Coral, FontStyle.Bold, TextAnchor.MiddleCenter);
                Flotante = TextoBase(30, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
                Flotante.wordWrap = false;
            }

            private static GUIStyle Caja(Texture2D fondo, int borde)
            {
                var s = new GUIStyle(GUI.skin.box);
                s.normal.background = fondo;
                s.hover.background = fondo;
                s.active.background = fondo;
                s.border = new RectOffset(borde, borde, borde, borde);
                s.padding = new RectOffset(20, 20, 16, 16);
                s.normal.textColor = Paleta.Tinta;
                return s;
            }

            private static GUIStyle TextoBase(int tamano, Color color, FontStyle estilo, TextAnchor alineacion)
            {
                var s = new GUIStyle(GUI.skin.label)
                {
                    fontSize = tamano,
                    fontStyle = estilo,
                    alignment = alineacion,
                    wordWrap = true,
                    richText = true
                };
                s.normal.textColor = color;
                return s;
            }
        }
    }
}
