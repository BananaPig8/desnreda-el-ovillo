using System;
using System.Collections.Generic;
using DesenredaElOvillo.Juego.Tres;
using DesenredaElOvillo.Nucleo;
using UnityEngine;

namespace DesenredaElOvillo.Juego
{
    public enum Pantalla
    {
        Menu,
        ComoJugar,
        Ajustes,
        Creditos,
        Juego
    }

    /// <summary>
    /// Director del juego: une el núcleo (Partida, Recorrido, PlanColocacion) con las vistas y la interfaz.
    /// Se crea solo al pulsar Play (ver Arranque) o desde la escena Juego.
    /// </summary>
    public sealed class ControladorJuego : MonoBehaviour
    {
        /// <summary>Zona central de la pantalla (referencia 1920x1080) donde se dibuja el tablero.</summary>
        public static readonly Rect ZonaTablero = new Rect(372, 140, 1176, 806);

        public static ControladorJuego Instancia { get; private set; }

        public Reglas ReglasBase { get; private set; }
        public List<Sala> Salas { get; } = new List<Sala>();
        public Partida Partida { get; private set; }
        public Recorrido Recorrido { get; private set; }
        public PlanColocacion Plan { get; private set; }

        public Pantalla Pantalla { get; private set; } = Pantalla.Menu;
        public bool Pausado { get; private set; }
        public bool EnOleada => Reproductor != null && Reproductor.EnCurso;
        public TipoHerramienta? Seleccionada { get; private set; }
        public float TiempoPartida { get; private set; }
        public string Mensaje { get; private set; }
        public float MensajeHasta { get; private set; }
        public string ErrorCarga { get; private set; }

        public CamaraJuego Camara { get; private set; }
        public VistaTablero Tablero { get; private set; }
        public VistaLana Lana { get; private set; }
        public VistaHerramientas Herramientas { get; private set; }
        public ReproductorOleada Reproductor { get; private set; }
        public Interfaz Interfaz { get; private set; }
        public Iluminacion Iluminacion { get; private set; }

        private Pantalla _volverA = Pantalla.Menu;
        private bool _tejiendo;
        private Celda? _ultimaCeldaArrastre;

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;
            Application.targetFrameRate = 60;

            var camara = Camera.main;
            if (camara == null)
            {
                var goCam = new GameObject("Cámara");
                camara = goCam.AddComponent<Camera>();
                goCam.tag = "MainCamera";
            }
            Camara = camara.GetComponent<CamaraJuego>();
            if (Camara == null) Camara = camara.gameObject.AddComponent<CamaraJuego>();
            Iluminacion = Iluminacion.Crear(transform);
            Iluminacion.PrepararCamara(camara);

            Tablero = Hijo<VistaTablero>("Tablero");
            Lana = Hijo<VistaLana>("Lana");
            Herramientas = Hijo<VistaHerramientas>("Herramientas");
            Reproductor = Hijo<ReproductorOleada>("Oleada");
            Hijo<Sonidos>("Sonidos");
            Interfaz = gameObject.AddComponent<Interfaz>();

            ReglasBase = Reglas.PorDefecto();
            // el modelo OBJ de Ovillo entregado (copia de lectura en Resources/Modelos)
            OvilloImportado.Fuente = () => TextoDe("Modelos/Ovillo_obj");
            OvilloImportado.FuenteMtl = () => TextoDe("Modelos/Ovillo_mtl");
            CargarSalas();
            Ajustes.Aplicar();
        }

        private void Start()
        {
            MostrarFondoMenu();
        }

        private static string TextoDe(string recurso)
        {
            var t = Resources.Load<TextAsset>(recurso);
            return t != null ? t.text : null;
        }

        private T Hijo<T>(string nombre) where T : Component
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(transform, false);
            return go.AddComponent<T>();
        }

        private void CargarSalas()
        {
            Salas.Clear();
            for (int i = 1; i <= 9; i++)
            {
                var texto = Resources.Load<TextAsset>("Salas/sala" + i);
                if (texto == null) break;
                try
                {
                    Salas.Add(Sala.DesdeTexto(texto.text));
                }
                catch (Exception ex)
                {
                    ErrorCarga = "No se pudo leer la sala " + i + ": " + ex.Message;
                    Debug.LogError(ErrorCarga);
                    break;
                }
            }
            if (Salas.Count == 0 && ErrorCarga == null) ErrorCarga = "No hay salas en Resources/Salas.";
        }

        private void MostrarFondoMenu()
        {
            Reproductor.Limpiar();
            Herramientas.Limpiar();
            Lana.Limpiar();
            if (Salas.Count == 0) return;
            Tablero.Construir(Salas[0].Tablero, ReglasBase.VidaCarrete, ReglasBase.VidaCarreteMaxima);
            Camara.Encuadrar(Tablero.Encuadre, new Rect(860, 120, 1020, 880));
            Camara.ReiniciarVista();
        }

        // ================================================================== navegación

        public void Jugar()
        {
            if (Salas.Count == 0) return;
            Partida = new Partida(ReglasBase, Salas, Environment.TickCount & 0x7FFFFFFF) { ReintentarDesdeSala = Ajustes.ModoFeria };
            TiempoPartida = 0;
            Pantalla = Pantalla.Juego;
            Pausar(false);
            PrepararSala(false);
            Sonidos.Tocar(Sonido.Clic);
        }

        public void IrAlMenu()
        {
            Pausar(false);
            Partida = null;
            Pantalla = Pantalla.Menu;
            MostrarFondoMenu();
        }

        public void Abrir(Pantalla p)
        {
            _volverA = Pantalla == Pantalla.Juego ? Pantalla.Juego : Pantalla.Menu;
            Pantalla = p;
            Sonidos.Tocar(Sonido.Clic);
        }

        public void Volver()
        {
            Pantalla = _volverA;
            Sonidos.Tocar(Sonido.Clic);
        }

        public void Salir()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void Pausar(bool pausa)
        {
            Pausado = pausa;
            Time.timeScale = pausa ? 0f : 1f;
        }

        public void AvisarConMensaje(string texto, float segundos = 2.5f)
        {
            Mensaje = texto;
            MensajeHasta = Time.unscaledTime + segundos;
        }

        // ================================================================== salas

        private void PrepararSala(bool conservarLana)
        {
            var sala = Partida.SalaActual;
            Reproductor.Limpiar();
            Tablero.Construir(sala.Tablero, Partida.VidaCarrete, ReglasBase.VidaCarreteMaxima);
            Camara.Encuadrar(Tablero.Encuadre, ZonaTablero);
            Camara.ReiniciarVista();
            // Ovillo llega andando a su puesto; en las salas pares (la primera vez) tropieza con su propia hebra
            if (Tablero.Ovillo != null) Tablero.Ovillo.Entrar(Tablero.FueraSalida, !conservarLana && Partida.NumeroSala % 2 == 0);
            if (!conservarLana || Recorrido == null || Recorrido.Tablero != sala.Tablero)
            {
                Recorrido = new Recorrido(sala.Tablero);
                Plan = new PlanColocacion();
            }
            AjustarPlanAlCosturero();
            Seleccionada = null;
            _tejiendo = false;
            RedibujarLana();
            Herramientas.Dibujar(Recorrido, Plan, Partida.Reglas);
        }

        public void ElegirHerramientas(IList<TipoHerramienta> elegidas)
        {
            Partida.ElegirHerramientas(elegidas);
            Sonidos.Tocar(Sonido.Colocar);
            AvisarConMensaje(Partida.Costurero.Count + " herramientas en tu costurero.");
        }

        public void ElegirMejora(TipoMejora mejora)
        {
            Partida.ElegirMejora(mejora);
            Sonidos.Tocar(Sonido.Colocar);
            PrepararSala(false);
        }

        public void Reintentar()
        {
            if (Partida == null) return;
            if (Partida.ReintentarSala()) PrepararSala(true);
            else Jugar();
        }

        /// <summary>Durante la preparación: vuelve a empezar la lana y quita las herramientas.</summary>
        public void ReiniciarLana()
        {
            if (Partida == null || EnOleada) return;
            Recorrido.Reiniciar();
            Plan.Limpiar();
            Seleccionada = null;
            RedibujarLana();
            Herramientas.Dibujar(Recorrido, Plan, Partida.Reglas);
            Sonidos.Tocar(Sonido.Quitar);
        }

        public void QuitarHerramientas()
        {
            if (Partida == null || EnOleada) return;
            Plan.Limpiar();
            Herramientas.Dibujar(Recorrido, Plan, Partida.Reglas);
            Sonidos.Tocar(Sonido.Quitar);
        }

        public int Disponibles(TipoHerramienta t)
        {
            if (Partida == null) return 0;
            int n = 0;
            foreach (var k in Partida.Costurero) if (k == t) n++;
            return n - (Plan == null ? 0 : Plan.Contar(t));
        }

        public int HerramientasSinColocar()
        {
            int n = 0;
            foreach (TipoHerramienta t in Enum.GetValues(typeof(TipoHerramienta))) n += Math.Max(0, Disponibles(t));
            return n;
        }

        public void Seleccionar(TipoHerramienta? t)
        {
            if (t.HasValue && Disponibles(t.Value) <= 0)
            {
                AvisarConMensaje("Ya has colocado todas las herramientas de ese tipo.");
                return;
            }
            Seleccionada = t;
            if (t.HasValue && !Recorrido.EstaCompleto) AvisarConMensaje("Primero lleva la lana hasta el carrete.");
            Sonidos.Tocar(Sonido.Clic);
        }

        public List<string> ProblemasParaEmpezar()
        {
            if (Partida == null || Recorrido == null) return new List<string>();
            return Partida.ProblemasParaEmpezar(Recorrido, Plan);
        }

        public void EmpezarOleada()
        {
            if (Partida == null || Partida.Fase != FasePartida.Preparacion || EnOleada) return;
            var problemas = ProblemasParaEmpezar();
            if (problemas.Count > 0)
            {
                AvisarConMensaje(problemas[0]);
                Sonidos.Tocar(Sonido.Placa);
                return;
            }
            Seleccionada = null;
            _tejiendo = false;
            Tablero.Resaltar(null, Color.clear);
            Lana.MostrarHuellas(Recorrido, null);
            Herramientas.Dibujar(Recorrido, Plan, Partida.Reglas);
            var sim = Partida.CrearSimulador(Recorrido, Plan, true);
            Reproductor.Iniciar(sim, Tablero, Herramientas, ReglasBase.VidaCarreteMaxima, AlTerminarOleada);
        }

        private void AlTerminarOleada(ResultadoOleada resultado)
        {
            Partida.AplicarResultado(resultado, Recorrido.HiloUsado);
            if (Tablero.Ovillo == null) return;
            if (Partida.Fase == FasePartida.Victoria) Tablero.Ovillo.Celebrar();
            else if (Partida.Fase == FasePartida.Derrota) Tablero.Ovillo.Entristecer();
            else Tablero.Ovillo.Saltar(1.5f);
        }

        // ================================================================== bucle

        private void Update()
        {
            if (EntradaJugador.Escape)
            {
                if (Pantalla == Pantalla.Juego) Pausar(!Pausado);
                else if (Pantalla != Pantalla.Menu) Volver();
            }
            if (Pantalla != Pantalla.Juego || Pausado || Partida == null) return;

            if (!Interfaz.RatonSobreInterfaz(EntradaJugador.PosicionRaton))
            {
                float rueda = EntradaJugador.Rueda;
                if (Mathf.Abs(rueda) > 0.01f) Camara.Zoom(rueda);
            }
            int giro = EntradaJugador.Giro;
            if (giro != 0) Camara.Girar(giro * 15f);

            if (Partida.Fase != FasePartida.Victoria && Partida.Fase != FasePartida.Derrota)
                TiempoPartida += Time.unscaledDeltaTime;

            if (EnOleada)
            {
                if (EntradaJugador.Espacio) Ajustes.Velocidad = Ajustes.Velocidad >= 4 ? 1 : Ajustes.Velocidad * 2;
                return;
            }
            if (Partida.Fase == FasePartida.Preparacion) ManejarPreparacion();
            else Tablero.Resaltar(null, Color.clear);
        }

        private void ManejarPreparacion()
        {
            int numero = EntradaJugador.NumeroPulsado;
            if (numero > 0)
            {
                var t = (TipoHerramienta)(numero - 1);
                Seleccionar(Seleccionada == t ? (TipoHerramienta?)null : t);
            }
            if (EntradaJugador.Deshacer && Recorrido.Cantidad > 1)
            {
                Recorrido.RecortarHasta(Recorrido.Cantidad - 2);
                TrasCambiarLana();
                Sonidos.Tocar(Sonido.LanaAtras);
            }
            if (EntradaJugador.Espacio)
            {
                EmpezarOleada();
                return;
            }

            if (EntradaJugador.IzquierdoSoltado) _tejiendo = false;

            var raton = EntradaJugador.PosicionRaton;
            if (Interfaz.RatonSobreInterfaz(raton) && !_tejiendo)
            {
                Tablero.Resaltar(null, Color.clear);
                return;
            }

            bool dentro = Tablero.CeldaBajo(Camara.RayoDesdePantalla(raton), out var celda);
            ActualizarResalte(dentro, celda);

            if (EntradaJugador.DerechoPulsado)
            {
                int idx = dentro ? Recorrido.IndiceDe(celda) : -1;
                if (idx > 0 && Plan.QuitarEn(idx) != null)
                {
                    Herramientas.Dibujar(Recorrido, Plan, Partida.Reglas);
                    Sonidos.Tocar(Sonido.Quitar);
                }
                else if (Seleccionada.HasValue) Seleccionada = null;
            }

            if (EntradaJugador.IzquierdoPulsado && dentro)
            {
                if (Seleccionada.HasValue) Colocar(celda);
                else EmpezarATejer(celda);
            }
            else if (_tejiendo && EntradaJugador.IzquierdoMantenido && dentro && _ultimaCeldaArrastre != celda)
            {
                AvanzarHacia(celda);
            }
        }

        private void ActualizarResalte(bool dentro, Celda celda)
        {
            if (!dentro)
            {
                Tablero.Resaltar(null, Color.clear);
                return;
            }
            Color color;
            if (Seleccionada.HasValue)
            {
                int idx = Recorrido.IndiceDe(celda);
                bool ok = idx > 0 && Plan.PuedeColocar(Recorrido, Seleccionada.Value, idx);
                color = ok ? new Color(1f, 0.95f, 0.6f, 0.55f) : new Color(1f, 0.35f, 0.3f, 0.35f);
            }
            else
            {
                bool siguiente = !Recorrido.EstaCompleto && celda.EsAdyacente(Recorrido.Ultima) && !Recorrido.Contiene(celda) &&
                                 (Recorrido.Tablero.EsConstruible(celda) || celda == Recorrido.Tablero.Salida);
                bool enLana = Recorrido.Contiene(celda);
                color = siguiente || enLana ? new Color(1f, 1f, 1f, 0.35f) : new Color(0f, 0f, 0f, 0.18f);
            }
            Tablero.Resaltar(celda, color);
        }

        private void EmpezarATejer(Celda celda)
        {
            int idx = Recorrido.IndiceDe(celda);
            if (idx >= 0)
            {
                if (idx < Recorrido.Cantidad - 1)
                {
                    Recorrido.RecortarHasta(idx);
                    TrasCambiarLana();
                    Sonidos.Tocar(Sonido.LanaAtras);
                }
                _tejiendo = true;
                _ultimaCeldaArrastre = celda;
                return;
            }
            if (!Recorrido.EstaCompleto && celda.EsAdyacente(Recorrido.Ultima))
            {
                _tejiendo = true;
                AvanzarHacia(celda);
                return;
            }
            AvisarConMensaje(Recorrido.Cantidad <= 1
                ? "Empieza a tejer desde el agujero de las polillas."
                : "Sigue tejiendo desde el final de tu lana (o pulsa sobre la lana para recortarla).");
        }

        /// <summary>Avanza la hebra hacia la celda bajo el ratón, rellenando los huecos si el ratón va rápido.</summary>
        private void AvanzarHacia(Celda objetivo)
        {
            _ultimaCeldaArrastre = objetivo;
            for (int guarda = 0; guarda < 40; guarda++)
            {
                if (Recorrido.Ultima == objetivo) break;
                int idx = Recorrido.IndiceDe(objetivo);
                if (idx >= 0)
                {
                    Recorrido.RecortarHasta(idx);
                    TrasCambiarLana();
                    Sonidos.Tocar(Sonido.LanaAtras);
                    break;
                }
                if (Recorrido.EstaCompleto) break;
                var ultima = Recorrido.Ultima;
                int dx = objetivo.X - ultima.X, dy = objetivo.Y - ultima.Y;
                var pasoX = new Celda(Math.Sign(dx), 0);
                var pasoY = new Celda(0, Math.Sign(dy));
                var primero = Math.Abs(dx) >= Math.Abs(dy) ? pasoX : pasoY;
                var segundo = Math.Abs(dx) >= Math.Abs(dy) ? pasoY : pasoX;
                var res = Intentar(ultima, primero);
                if (!Exito(res) && (segundo.X != 0 || segundo.Y != 0)) res = Intentar(ultima, segundo);
                if (!Exito(res))
                {
                    if (res == ResultadoPaso.SinHilo) AvisarConMensaje("¡Se acabó el hilo! Recorta la lana o busca un camino más corto.");
                    break;
                }
                Sonidos.Tocar(Sonido.Lana, 0.8f, 0.9f + Recorrido.HiloUsado * 0.02f);
                if (res == ResultadoPaso.Completado)
                {
                    Sonidos.Tocar(Sonido.Colocar);
                    if (Tablero.Ovillo != null) Tablero.Ovillo.Guinar(1.4f);
                    _tejiendo = false;
                    AvisarConMensaje("¡La lana llega al carrete! Ahora coloca tus herramientas.");
                    break;
                }
            }
            TrasCambiarLana();
        }

        private ResultadoPaso Intentar(Celda desde, Celda paso)
        {
            if (paso.X == 0 && paso.Y == 0) return ResultadoPaso.SinCambios;
            return Recorrido.IntentarAvanzar(desde.Mas(paso), Partida.HiloDisponible);
        }

        private static bool Exito(ResultadoPaso r) => r == ResultadoPaso.Avanzado || r == ResultadoPaso.Completado || r == ResultadoPaso.Retrocedido;

        private void Colocar(Celda celda)
        {
            int idx = Recorrido.IndiceDe(celda);
            var t = Seleccionada.Value;
            if (!Recorrido.EstaCompleto)
            {
                AvisarConMensaje("Primero lleva la lana hasta el carrete.");
                return;
            }
            if (idx <= 0 || !Plan.Colocar(Recorrido, t, idx))
            {
                AvisarConMensaje(idx <= 0 ? "Las herramientas van encima de la lana." :
                    PlanColocacion.VaDebajo(t) ? "Ahí ya hay un velcro." : "Ahí ya hay una herramienta (el velcro sí puede ir debajo).");
                Sonidos.Tocar(Sonido.Placa, 0.6f);
                return;
            }
            Sonidos.Tocar(Sonido.Colocar);
            Herramientas.Dibujar(Recorrido, Plan, Partida.Reglas);
            if (Disponibles(t) <= 0) Seleccionada = null;
        }

        private void TrasCambiarLana()
        {
            var quitadas = Plan.PodarFueraDe(Recorrido);
            if (quitadas.Count > 0) Herramientas.Dibujar(Recorrido, Plan, Partida.Reglas);
            RedibujarLana();
        }

        /// <summary>Si el costurero cambió (reintento), quita colocaciones que ya no se pueden pagar.</summary>
        private void AjustarPlanAlCosturero()
        {
            if (Plan == null) return;
            foreach (TipoHerramienta t in Enum.GetValues(typeof(TipoHerramienta)))
            {
                while (Disponibles(t) < 0)
                {
                    int indice = -1;
                    foreach (var c in Plan.Colocaciones) if (c.Tipo == t) indice = c.Indice;
                    if (indice < 0 || Plan.QuitarEn(indice) == null) break;
                }
            }
        }

        private void RedibujarLana()
        {
            float restante = Partida == null || Partida.HiloDisponible <= 0 ? 1f : 1f - (float)Recorrido.HiloUsado / Partida.HiloDisponible;
            Lana.Dibujar(Recorrido, restante);
            bool hayVeloces = Partida != null && Partida.SalaActual.Oleada.Contar(TipoPolilla.Veloz) > 0;
            Lana.MostrarHuellas(Recorrido, hayVeloces && Recorrido.EstaCompleto
                ? SimuladorOleada.PrevisualizarAterrizajes(Recorrido, Partida.Reglas)
                : null);
        }
    }
}
