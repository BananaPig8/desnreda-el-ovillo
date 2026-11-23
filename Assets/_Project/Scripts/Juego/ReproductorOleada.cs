using System;
using System.Collections;
using System.Collections.Generic;
using DesenredaElOvillo.Nucleo;
using UnityEngine;

namespace DesenredaElOvillo.Juego
{
    /// <summary>Texto que sube y se desvanece sobre el tablero ("-2", "¡Pegada!"...). Lo dibuja la Interfaz.</summary>
    public sealed class TextoFlotante
    {
        public Vector3 Mundo;
        public string Texto;
        public Color Color;
        public float Nacimiento;
        public float Vida = 1.1f;
        public float Tamano = 30;
    }

    /// <summary>
    /// Reproduce una oleada: ejecuta la simulación turno a turno y traduce cada evento en animación, sonido y texto.
    /// La lógica ya está decidida por el núcleo; aquí solo se cuenta lo que pasó, con ritmo y claridad.
    /// </summary>
    public sealed class ReproductorOleada : MonoBehaviour
    {
        private readonly Dictionary<int, VistaPolilla> _polillas = new Dictionary<int, VistaPolilla>();
        private readonly List<Evento> _turno = new List<Evento>();
        private SimuladorOleada _sim;
        private Recorrido _recorrido;
        private VistaTablero _tablero;
        private VistaHerramientas _herramientas;
        private Action<ResultadoOleada> _alTerminar;
        private Transform _contenedor;

        public readonly List<TextoFlotante> Textos = new List<TextoFlotante>();
        public bool EnCurso { get; private set; }
        public int TurnoActual => _sim == null ? 0 : _sim.Turno;
        public int VidaCarreteMostrada { get; private set; }
        public float Velocidad => Ajustes.Velocidad;
        public int PolillasEnTablero
        {
            get
            {
                int n = 0;
                foreach (var v in _polillas.Values) if (v != null && v.gameObject.activeSelf) n++;
                return n;
            }
        }

        public void Iniciar(SimuladorOleada sim, VistaTablero tablero, VistaHerramientas herramientas, int vidaMaxima, Action<ResultadoOleada> alTerminar)
        {
            Limpiar();
            _sim = sim;
            _recorrido = sim.Recorrido;
            _tablero = tablero;
            _herramientas = herramientas;
            _alTerminar = alTerminar;
            _vidaMaxima = vidaMaxima;
            VidaCarreteMostrada = sim.VidaCarrete;
            _contenedor = new GameObject("Polillas").transform;
            _contenedor.SetParent(transform, false);
            EnCurso = true;
            StartCoroutine(Reproducir());
        }

        private int _vidaMaxima;

        private IEnumerator Reproducir()
        {
            yield return Esperar(0.3f);
            while (!_sim.Terminada)
            {
                _turno.Clear();
                _turno.AddRange(_sim.EjecutarTurno());
                foreach (var e in _turno) yield return Animar(e);
                yield return Esperar(0.12f);
            }
            var resultado = _sim.Resultado();
            if (!resultado.Victoria) yield return Devorar();
            else
            {
                _tablero.Saltar(1.6f);
                Sonidos.Tocar(Sonido.Victoria);
                Flotar(_tablero.PosicionCarrete + Vector3.up * 0.8f, "¡Sala superada!", Paleta.Crema, 1.8f, 44);
                yield return Esperar(1.4f);
            }
            EnCurso = false;
            _alTerminar?.Invoke(resultado);
        }

        private IEnumerator Esperar(float segundos)
        {
            float t = segundos / Mathf.Max(1, Velocidad);
            while (t > 0)
            {
                t -= Time.deltaTime;
                yield return null;
            }
        }

        private float D(float segundos) => segundos / Mathf.Max(1, Velocidad);

        /// <summary>Posición de los pies de una polilla en la casilla (encima de la lana).</summary>
        private Vector3 Pos(int indice)
        {
            indice = Mathf.Clamp(indice, 0, _recorrido.Cantidad - 1);
            var c = _recorrido.Celdas[indice];
            return new Vector3(c.X + 0.5f, Tres.Medidas.AlturaPolilla, c.Y + 0.5f);
        }

        private static Vector3 Arriba(VistaPolilla v, float altura) => v.transform.position + Vector3.up * altura;

        private VistaPolilla Vista(Evento e)
        {
            _polillas.TryGetValue(e.PolillaId, out var v);
            return v;
        }

        private IEnumerator Animar(Evento e)
        {
            var v = Vista(e);
            switch (e.Tipo)
            {
                case TipoEvento.Aparece:
                case TipoEvento.Invocada:
                {
                    var nueva = VistaPolilla.Crear(_contenedor, e.PolillaId, e.TipoPolilla, Pos(0));
                    _polillas[e.PolillaId] = nueva;
                    Sonidos.Tocar(e.TipoPolilla == TipoPolilla.Reina ? Sonido.Reina : Sonido.Aparece);
                    if (e.TipoPolilla == TipoPolilla.Reina) Flotar(Pos(0) + Vector3.up * 1.1f, "¡La Reina!", Paleta.Dorado, 1.6f, 40);
                    if (e.Tipo == TipoEvento.Invocada) Flotar(Pos(0) + Vector3.up * 0.9f, "¡Refuerzos!", Paleta.RosaPastel);
                    yield return nueva.Aparecer(D(e.TipoPolilla == TipoPolilla.Reina ? 0.6f : 0.25f));
                    break;
                }
                case TipoEvento.Sobrevuela:
                    if (v == null) break;
                    v.Volando = true;
                    yield return v.Mover(Pos(e.Hasta), D(0.13f), false);
                    break;
                case TipoEvento.Mueve:
                    if (v == null) break;
                    bool veniaVolando = v.Volando;
                    v.Volando = false;
                    yield return v.Mover(Pos(e.Hasta), D(veniaVolando ? 0.13f : 0.22f), !veniaVolando);
                    if (veniaVolando) yield return v.Aplastar(D(0.08f));
                    break;
                case TipoEvento.ActivaHerramienta:
                    _herramientas.Activar(e.IndiceHerramienta, e.Herramienta, e.Valor);
                    Sonidos.Tocar(SonidoDe(e.Herramienta));
                    yield return Esperar(0.06f);
                    break;
                case TipoEvento.PinchazoQuieta:
                    _herramientas.Activar(e.IndiceHerramienta, e.Herramienta, e.Valor);
                    Sonidos.Tocar(Sonido.Alfiler, 0.8f, 1.15f);
                    yield return Esperar(0.08f);
                    break;
                case TipoEvento.HerramientaGastada:
                    _herramientas.Gastar(e.IndiceHerramienta, e.Herramienta);
                    break;
                case TipoEvento.Danio:
                    if (v == null) break;
                    Flotar(Arriba(v, 0.75f), "-" + e.Valor, Paleta.Coral, 1f, 34);
                    Sonidos.Tocar(Sonido.Dano);
                    yield return v.Golpe(D(0.16f));
                    break;
                case TipoEvento.Muere:
                    if (v == null) break;
                    Sonidos.Tocar(Sonido.Muere);
                    Flotar(Arriba(v, 0.7f), "¡Pof!", Paleta.Crema);
                    _tablero.Saltar(0.5f);
                    yield return v.Morir(D(0.3f));
                    break;
                case TipoEvento.Retenida:
                    if (v == null) break;
                    v.Pegada(true);
                    Flotar(Arriba(v, 0.85f), "¡Pegada!", Paleta.AzulSuave);
                    yield return v.Aplastar(D(0.15f));
                    break;
                case TipoEvento.SigueQuieta:
                    if (v == null) break;
                    v.Temblar(D(0.2f));
                    yield return Esperar(0.18f);
                    if (v != null)
                    {
                        v.Pegada(false);
                        v.Deslumbrada(false);
                    }
                    break;
                case TipoEvento.Enredada:
                    if (v == null) break;
                    v.Enredada(true);
                    Flotar(Arriba(v, 0.85f), "¡Enredada!", Paleta.Mostaza);
                    yield return v.Aplastar(D(0.18f));
                    break;
                case TipoEvento.QuietaEnredada:
                    if (v == null) break;
                    v.Temblar(D(0.18f));
                    yield return Esperar(0.16f);
                    break;
                case TipoEvento.SeDesenreda:
                    if (v != null) v.Enredada(false);
                    break;
                case TipoEvento.Empujada:
                    if (v == null) break;
                    Flotar(Arriba(v, 0.85f), "¡Boing!", Paleta.Mostaza);
                    yield return v.Empujada(Pos(e.Hasta), D(0.38f));
                    break;
                case TipoEvento.PlacaRota:
                    if (v == null) break;
                    Sonidos.Tocar(Sonido.Placa);
                    Flotar(Arriba(v, 0.9f), "¡Placa rota!", Paleta.Beige);
                    yield return v.RomperPlaca(D(0.3f));
                    break;
                case TipoEvento.Deslumbrada:
                    if (v == null) break;
                    v.Volando = false;
                    v.Deslumbrada(true);
                    Sonidos.Tocar(Sonido.Deslumbra);
                    Flotar(Arriba(v, 0.9f), "¡Qué luz!", Paleta.Dorado);
                    yield return v.Aplastar(D(0.2f));
                    break;
                case TipoEvento.Devora:
                    Sonidos.Tocar(Sonido.Mordisco);
                    _herramientas.Devorar(e.IndiceHerramienta, e.Herramienta);
                    if (v != null) Flotar(Arriba(v, 1.0f), "¡Ñam!", Paleta.RosaPastel, 1.2f, 38);
                    yield return Esperar(0.35f);
                    break;
                case TipoEvento.LlamaAyuda:
                    Sonidos.Tocar(Sonido.Reina);
                    if (v != null) Flotar(Arriba(v, 1.15f), "¡A mí, mi corte!", Paleta.Dorado, 1.4f, 36);
                    yield return Esperar(0.45f);
                    break;
                case TipoEvento.LlegaAlCarrete:
                    if (v == null) break;
                    yield return v.Escapar(_tablero.PosicionCarrete, D(0.2f));
                    break;
                case TipoEvento.DedalProtege:
                    Flotar(_tablero.PosicionCarrete + Vector3.up * 0.8f, "¡El dedal amortigua!", Paleta.Plata);
                    yield return Esperar(0.25f);
                    break;
                case TipoEvento.CarreteDaniado:
                    VidaCarreteMostrada = e.Extra;
                    _tablero.ActualizarCarrete(e.Extra, _vidaMaxima, true);
                    Sonidos.Tocar(Sonido.Mordisco);
                    Flotar(_tablero.PosicionCarrete + Vector3.up * 0.9f, "-" + e.Valor + " hilo", Paleta.Bufanda, 1.3f, 36);
                    CamaraJuego.Sacudir(0.25f);
                    yield return Esperar(0.35f);
                    break;
                case TipoEvento.DaLaVuelta:
                    if (v == null) break;
                    Flotar(_tablero.PosicionCarrete + Vector3.up * 0.8f, "¡Otra vuelta!", Paleta.Dorado);
                    v.Teletransportar(Pos(0));
                    yield return v.Aparecer(D(0.4f));
                    break;
            }
        }

        private static Sonido SonidoDe(TipoHerramienta t)
        {
            switch (t)
            {
                case TipoHerramienta.Alfiler: return Sonido.Alfiler;
                case TipoHerramienta.Velcro: return Sonido.Velcro;
                case TipoHerramienta.Bobina: return Sonido.Bobina;
                case TipoHerramienta.Tijeras: return Sonido.Tijeras;
                default: return Sonido.Boton;
            }
        }

        /// <summary>Derrota: las polillas que quedan se lanzan a por el carrete y se lo comen.</summary>
        private IEnumerator Devorar()
        {
            Sonidos.Tocar(Sonido.Derrota);
            _tablero.Asustar(2f);
            int k = 0;
            foreach (var v in _polillas.Values)
            {
                if (v == null || !v.gameObject.activeSelf) continue;
                var destino = _tablero.PosicionCarrete + new Vector3(Mathf.Cos(k * 2.1f) * 0.35f, Mathf.Sin(k * 2.1f) * 0.3f, 0);
                v.Volando = true;
                StartCoroutine(v.Mover(destino, 0.6f, false));
                k++;
            }
            Flotar(_tablero.PosicionCarrete + Vector3.up * 1.1f, "¡Ñam, ñam, ñam!", Paleta.RosaPastel, 2f, 40);
            for (int i = VidaCarreteMostrada; i >= 0; i--)
            {
                _tablero.ActualizarCarrete(0, _vidaMaxima, true);
                Sonidos.Tocar(Sonido.Mordisco, 0.7f, 1f + i * 0.05f);
                yield return new WaitForSeconds(0.25f);
            }
            yield return new WaitForSeconds(0.8f);
        }

        public void Flotar(Vector3 mundo, string texto, Color color, float vida = 1.1f, float tamano = 30)
        {
            Textos.Add(new TextoFlotante { Mundo = mundo, Texto = texto, Color = color, Nacimiento = Time.time, Vida = vida, Tamano = tamano });
        }

        private void Update()
        {
            if (EnCurso && _tablero != null && _tablero.Ovillo != null)
            {
                VistaPolilla cercana = null;
                float mejor = float.MaxValue;
                foreach (var v in _polillas.Values)
                {
                    if (v == null || !v.gameObject.activeSelf) continue;
                    float d = (v.transform.position - _tablero.PosicionCarrete).sqrMagnitude;
                    if (d < mejor) { mejor = d; cercana = v; }
                }
                _tablero.Ovillo.MirarA(cercana != null ? cercana.transform.position : (Vector3?)null);
            }
            for (int i = Textos.Count - 1; i >= 0; i--)
                if (Time.time - Textos[i].Nacimiento > Textos[i].Vida) Textos.RemoveAt(i);
        }

        public void Limpiar()
        {
            StopAllCoroutines();
            EnCurso = false;
            _polillas.Clear();
            Textos.Clear();
            if (_contenedor != null) Destroy(_contenedor.gameObject);
            _contenedor = null;
        }
    }
}
