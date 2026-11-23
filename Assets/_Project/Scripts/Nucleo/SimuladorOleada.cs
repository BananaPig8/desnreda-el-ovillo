using System;
using System.Collections.Generic;

namespace DesenredaElOvillo.Nucleo
{
    /// <summary>Estado de una polilla durante la oleada.</summary>
    public sealed class PolillaEnJuego
    {
        public int Id;
        public DefinicionPolilla Def;
        public TipoPolilla Tipo => Def.Tipo;
        public int Vida;
        /// <summary>Posición: índice de casilla dentro del recorrido (0 = entrada).</summary>
        public int Indice;
        /// <summary>Medios pasos acumulados mientras está enredada (para la media velocidad).</summary>
        public int Acumulador;
        public int TurnosRetenida;
        public int TurnosEnredada;
        public int Placas;
        public bool HaComido;
        public bool Viva = true;
        /// <summary>Ha llegado al carrete y ya no está en el tablero.</summary>
        public bool Fuera;
        public bool Invocada;
        public int TurnoAparicion;
        public int UmbralesDisparados;
        public int Vueltas;
        /// <summary>Lámparas que ya la han deslumbrado (bit por índice de lámpara).</summary>
        public ulong LamparasVistas;
    }

    /// <summary>
    /// Herramienta colocada durante la oleada.
    /// Los usos son las polillas DISTINTAS que puede atrapar. A una polilla ya atrapada le sigue afectando
    /// cada vez que la pisa o se queda quieta encima, sin gastar usos (salvo el botón: una vez por polilla).
    /// </summary>
    public sealed class HerramientaEnJuego
    {
        public TipoHerramienta Tipo;
        public DefinicionHerramienta Def;
        public int Indice;
        /// <summary>Polillas nuevas que todavía puede atrapar.</summary>
        public int Usos;
        public bool Destruida;
        /// <summary>Ids de las polillas que ya ha atrapado.</summary>
        public readonly List<int> Atrapadas = new List<int>(2);

        /// <summary>Le quedan usos (puede atrapar polillas nuevas).</summary>
        public bool Disponible => !Destruida && Usos > 0;

        public bool HaAtrapado(int polillaId) => Atrapadas.Contains(polillaId);

        public bool PuedeAfectar(int polillaId)
        {
            if (Destruida) return false;
            if (Atrapadas.Contains(polillaId)) return !Def.UnaVezPorPolilla;
            return Usos > 0;
        }
    }

    public sealed class ResultadoOleada
    {
        public bool Victoria;
        public bool CorteDeSeguridad;
        public int VidaCarreteInicial;
        public int VidaCarreteFinal;
        public int Derrotadas;
        public int Escapadas;
        public int Turnos;
        public List<Evento> Eventos;
    }

    /// <summary>
    /// Resuelve una oleada turno a turno de forma totalmente determinista (sin azar, sin coma flotante).
    ///
    /// ORDEN DE RESOLUCIÓN DE CADA TURNO
    ///  1. Entradas: aparecen las polillas programadas para este turno y las llamadas por la Reina
    ///     en el turno anterior. Las recién llegadas no actúan este turno.
    ///  2. Actúan las polillas de una en una: primero la más avanzada en la lana; a igualdad, la que llegó antes.
    ///     Para cada polilla:
    ///     a. Si está enredada, este turno cuenta como turno de enredo (media velocidad, sin vuelo).
    ///     b. Si está pegada (velcro/lámpara), pierde el turno; si está sobre un alfiler, vuelve a pincharse
    ///        (los pinchazos a una polilla quieta no gastan usos).
    ///     c. Calcula cuántas casillas avanza. Si no avanza (enredo), también se pincha si está sobre un alfiler.
    ///     d. Avanza casilla a casilla. En cada casilla:
    ///        - Si llega al carrete: muerde y sale (la Reina da otra vuelta).
    ///        - Si vuela y no aterriza aquí, solo la sobrevuela (nada se activa).
    ///          Aterriza al final del movimiento, en las curvas, sobre una bobina y junto a una lámpara nueva;
    ///          al aterrizar termina su vuelo de ese turno.
    ///        - La Reina se come la primera casilla con herramientas que pisa (sin sufrir su efecto).
    ///        - Herramienta de encima: daño (alfiler, tijeras) o control (bobina, botón).
    ///        - Velcro de debajo, si la polilla sigue en la casilla.
    ///        (Cada herramienta gasta un uso la primera vez que atrapa a una polilla; a esa polilla le sigue
    ///         afectando gratis cada vez que vuelve a pisarla. El botón solo lanza una vez a cada polilla.)
    ///        - Lámparas adyacentes que aún no la hayan deslumbrado.
    ///        - Muerte: en cuanto la vida llega a 0 la polilla desaparece; el uso de la trampa ya está gastado.
    ///        - Umbrales de la Reina: la llamada se resuelve al principio del turno siguiente.
    ///        - Si quedó pegada, enredada o fue empujada, su movimiento termina aquí.
    ///  3. Fin del turno: derrota si el carrete llega a 0; victoria si no queda ninguna polilla ni ninguna por llegar.
    /// </summary>
    public sealed class SimuladorOleada
    {
        private readonly Reglas _reglas;
        private readonly Oleada _oleada;
        private readonly int _indiceSalida;
        private readonly bool[] _curva;
        private readonly int[][] _lamparas;
        private readonly HerramientaEnJuego[] _encima;
        private readonly HerramientaEnJuego[] _debajo;
        private readonly List<HerramientaEnJuego> _herramientas = new List<HerramientaEnJuego>();
        private readonly List<PolillaEnJuego> _todas = new List<PolillaEnJuego>();
        private readonly List<PolillaEnJuego> _activas = new List<PolillaEnJuego>();
        private readonly List<PolillaEnJuego> _ordenTurno = new List<PolillaEnJuego>();
        private readonly List<TipoPolilla> _invocaciones = new List<TipoPolilla>();
        private readonly List<Evento> _eventosTurno = new List<Evento>();
        private readonly List<Evento> _registro;
        private readonly bool _registrar;
        private int _siguienteAparicion;
        private bool _dedalDisponible;

        public Recorrido Recorrido { get; }
        public int Turno { get; private set; }
        public int VidaCarreteInicial { get; }
        public int VidaCarrete { get; private set; }
        public bool Terminada { get; private set; }
        public bool Victoria { get; private set; }
        public bool CorteDeSeguridad { get; private set; }
        public int Derrotadas { get; private set; }
        public int Escapadas { get; private set; }
        public IReadOnlyList<PolillaEnJuego> Polillas => _todas;
        public IReadOnlyList<HerramientaEnJuego> Herramientas => _herramientas;
        public IReadOnlyList<Evento> Registro => _registro;

        public SimuladorOleada(Reglas reglas, Recorrido recorrido, PlanColocacion plan, Oleada oleada, int vidaCarrete, bool registrarEventos = true)
        {
            if (!recorrido.EstaCompleto) throw new InvalidOperationException("El recorrido debe llegar al carrete antes de simular.");
            _reglas = reglas;
            Recorrido = recorrido;
            _oleada = oleada;
            _indiceSalida = recorrido.IndiceSalida;
            VidaCarreteInicial = vidaCarrete;
            VidaCarrete = vidaCarrete;
            _dedalDisponible = reglas.DedalActivo;
            _registrar = registrarEventos;
            _registro = registrarEventos ? new List<Evento>() : null;

            int n = recorrido.Cantidad;
            _curva = new bool[n];
            _lamparas = new int[n][];
            _encima = new HerramientaEnJuego[n];
            _debajo = new HerramientaEnJuego[n];
            for (int i = 0; i < n; i++)
            {
                _curva[i] = recorrido.EsCurva(i);
                var lista = recorrido.Tablero.LamparasAdyacentes(recorrido.Celdas[i]);
                _lamparas[i] = lista.Count == 0 ? Array.Empty<int>() : lista.ToArray();
            }

            if (plan != null)
            {
                foreach (var c in plan.Colocaciones)
                {
                    if (c.Indice <= 0 || c.Indice >= _indiceSalida)
                        throw new ArgumentException("Herramienta fuera de la lana: " + c);
                    var def = reglas.Herramienta(c.Tipo);
                    var h = new HerramientaEnJuego { Tipo = c.Tipo, Def = def, Indice = c.Indice, Usos = def.Usos };
                    if (def.VaDebajo)
                    {
                        if (_debajo[c.Indice] != null) throw new ArgumentException("Dos herramientas de debajo en la casilla " + c.Indice);
                        _debajo[c.Indice] = h;
                    }
                    else
                    {
                        if (_encima[c.Indice] != null) throw new ArgumentException("Dos herramientas en la casilla " + c.Indice);
                        _encima[c.Indice] = h;
                    }
                    _herramientas.Add(h);
                }
            }
        }

        public static ResultadoOleada Simular(Reglas reglas, Recorrido recorrido, PlanColocacion plan, Oleada oleada, int vidaCarrete, bool registrarEventos = false)
        {
            var sim = new SimuladorOleada(reglas, recorrido, plan, oleada, vidaCarrete, registrarEventos);
            while (!sim.Terminada) sim.EjecutarTurno();
            return sim.Resultado();
        }

        public ResultadoOleada Resultado() => new ResultadoOleada
        {
            Victoria = Victoria,
            CorteDeSeguridad = CorteDeSeguridad,
            VidaCarreteInicial = VidaCarreteInicial,
            VidaCarreteFinal = VidaCarrete,
            Derrotadas = Derrotadas,
            Escapadas = Escapadas,
            Turnos = Turno,
            Eventos = _registro
        };

        public HerramientaEnJuego HerramientaEncima(int indice) => indice >= 0 && indice < _encima.Length ? _encima[indice] : null;
        public HerramientaEnJuego HerramientaDebajo(int indice) => indice >= 0 && indice < _debajo.Length ? _debajo[indice] : null;

        /// <summary>Ejecuta un turno completo y devuelve sus eventos (la lista se reutiliza en el turno siguiente).</summary>
        public IReadOnlyList<Evento> EjecutarTurno()
        {
            _eventosTurno.Clear();
            if (Terminada) return _eventosTurno;
            Turno++;
            Emitir(TipoEvento.InicioTurno, null);

            // 1. Entradas
            var apariciones = _oleada.Apariciones;
            while (_siguienteAparicion < apariciones.Count && apariciones[_siguienteAparicion].Turno <= Turno)
            {
                Crear(apariciones[_siguienteAparicion].Tipo, false);
                _siguienteAparicion++;
            }
            if (_invocaciones.Count > 0)
            {
                foreach (var tipo in _invocaciones) Crear(tipo, true);
                _invocaciones.Clear();
            }

            // 2. Actuaciones, de la más adelantada a la más rezagada
            _ordenTurno.Clear();
            foreach (var p in _activas) if (p.TurnoAparicion < Turno) _ordenTurno.Add(p);
            _ordenTurno.Sort((a, b) =>
            {
                int c = b.Indice.CompareTo(a.Indice);
                return c != 0 ? c : a.Id.CompareTo(b.Id);
            });
            foreach (var p in _ordenTurno)
            {
                if (Terminada) break;
                if (!p.Viva || p.Fuera) continue;
                Actuar(p);
            }

            // 3. Fin de turno
            if (!Terminada)
            {
                if (VidaCarrete <= 0) Terminar(false);
                else if (_activas.Count == 0 && _siguienteAparicion >= apariciones.Count && _invocaciones.Count == 0) Terminar(true);
                else if (Turno >= _reglas.MaximoTurnos)
                {
                    CorteDeSeguridad = true;
                    Terminar(false);
                }
            }
            return _eventosTurno;
        }

        private void Crear(TipoPolilla tipo, bool invocada)
        {
            var def = _reglas.Polilla(tipo);
            var p = new PolillaEnJuego
            {
                Id = _todas.Count,
                Def = def,
                Vida = def.Vida,
                Placas = def.Placas,
                Indice = 0,
                Invocada = invocada,
                TurnoAparicion = Turno
            };
            _todas.Add(p);
            _activas.Add(p);
            Emitir(invocada ? TipoEvento.Invocada : TipoEvento.Aparece, p, 0, 0);
        }

        private void Actuar(PolillaEnJuego p)
        {
            bool enredadaAlEmpezar = p.TurnosEnredada > 0;
            if (enredadaAlEmpezar) p.TurnosEnredada--;

            if (p.TurnosRetenida > 0)
            {
                p.TurnosRetenida--;
                Emitir(TipoEvento.SigueQuieta, p, p.Indice, p.Indice);
                PincharQuieta(p);
                if (p.Viva) ComprobarFinEnredo(p, enredadaAlEmpezar);
                return;
            }

            int medios = p.Def.MediosPasosPorTurno;
            if (enredadaAlEmpezar) medios /= 2;
            p.Acumulador += medios;
            int pasos = p.Acumulador / 2;
            p.Acumulador -= pasos * 2;
            if (!enredadaAlEmpezar) p.Acumulador = 0;

            if (pasos == 0)
            {
                Emitir(TipoEvento.QuietaEnredada, p, p.Indice, p.Indice);
                PincharQuieta(p);
                if (p.Viva) ComprobarFinEnredo(p, enredadaAlEmpezar);
                return;
            }

            bool puedeVolar = p.Def.Vuela && !enredadaAlEmpezar;
            for (int paso = 1; paso <= pasos; paso++)
            {
                int desde = p.Indice;
                int destino = desde + 1;
                p.Indice = destino;

                if (destino >= _indiceSalida)
                {
                    Emitir(TipoEvento.Mueve, p, desde, destino);
                    LlegarAlCarrete(p);
                    return;
                }

                bool aterriza = !puedeVolar
                                || paso == pasos
                                || _curva[destino]
                                || BobinaQueAtrapa(p, destino)
                                || LamparaPendiente(p, destino);
                if (!aterriza)
                {
                    Emitir(TipoEvento.Sobrevuela, p, desde, destino);
                    continue;
                }

                Emitir(TipoEvento.Mueve, p, desde, destino);
                bool detener = ResolverCasilla(p, destino);
                if (!p.Viva || p.Fuera || Terminada) return;
                // Una voladora que toca el suelo termina su vuelo de este turno.
                if (detener || puedeVolar) break;
            }
            ComprobarFinEnredo(p, enredadaAlEmpezar);
        }

        private void ComprobarFinEnredo(PolillaEnJuego p, bool enredadaAlEmpezar)
        {
            if (enredadaAlEmpezar && p.TurnosEnredada == 0)
            {
                p.Acumulador = 0;
                Emitir(TipoEvento.SeDesenreda, p, p.Indice, p.Indice);
            }
        }

        private bool BobinaQueAtrapa(PolillaEnJuego p, int i)
        {
            var h = _encima[i];
            return h != null && h.Def.AtrapaEnVuelo && h.PuedeAfectar(p.Id);
        }

        private bool LamparaPendiente(PolillaEnJuego p, int i)
        {
            foreach (int l in _lamparas[i])
            {
                if ((p.LamparasVistas & (1UL << l)) == 0) return true;
            }
            return false;
        }

        /// <summary>Resuelve la casilla en la que la polilla acaba de entrar. Devuelve true si su movimiento termina aquí.</summary>
        private bool ResolverCasilla(PolillaEnJuego p, int i)
        {
            var encima = _encima[i];
            var debajo = _debajo[i];

            if (p.Def.ComeHerramientas && !p.HaComido && ((encima != null && encima.Disponible) || (debajo != null && debajo.Disponible)))
            {
                p.HaComido = true;
                if (encima != null && encima.Disponible) Devorar(p, encima);
                if (debajo != null && debajo.Disponible) Devorar(p, debajo);
                return false;
            }

            bool detener = false;
            if (encima != null && Afectar(encima, p))
            {
                switch (encima.Tipo)
                {
                    case TipoHerramienta.Alfiler:
                        Daniar(p, encima.Def.Danio + (_curva[i] ? _reglas.BonoAlfilerEnCurva : 0));
                        break;
                    case TipoHerramienta.Tijeras:
                        Daniar(p, encima.Def.Danio);
                        break;
                    case TipoHerramienta.Bobina:
                        if (SuperaPlaca(p))
                        {
                            p.TurnosEnredada = Math.Max(p.TurnosEnredada, encima.Def.Turnos);
                            p.Acumulador = 0;
                            Emitir(TipoEvento.Enredada, p, i, i, encima.Def.Turnos);
                            detener = true;
                        }
                        break;
                    case TipoHerramienta.Boton:
                        if (SuperaPlaca(p))
                        {
                            int destino = Math.Max(0, i - encima.Def.Distancia);
                            p.Indice = destino;
                            Emitir(TipoEvento.Empujada, p, i, destino);
                            return true;
                        }
                        break;
                }
                if (!p.Viva || Terminada) return true;
            }

            if (debajo != null && Afectar(debajo, p))
            {
                if (SuperaPlaca(p))
                {
                    p.TurnosRetenida = Math.Max(p.TurnosRetenida, debajo.Def.Turnos);
                    Emitir(TipoEvento.Retenida, p, i, i, debajo.Def.Turnos);
                    detener = true;
                }
            }

            foreach (int l in _lamparas[i])
            {
                ulong bit = 1UL << l;
                if ((p.LamparasVistas & bit) != 0) continue;
                p.LamparasVistas |= bit;
                if (SuperaPlaca(p))
                {
                    p.TurnosRetenida = Math.Max(p.TurnosRetenida, 1);
                    Emitir(TipoEvento.Deslumbrada, p, i, i, 1);
                    detener = true;
                }
            }
            return detener;
        }

        /// <summary>
        /// La polilla pasa el turno quieta encima de su casilla: el alfiler que ya la había atrapado
        /// vuelve a pincharla sin gastar usos (aunque esté doblado: solo deja de atrapar polillas nuevas).
        /// </summary>
        private void PincharQuieta(PolillaEnJuego p)
        {
            int i = p.Indice;
            if (i <= 0 || i >= _indiceSalida) return;
            var h = _encima[i];
            if (h == null || h.Destruida || !h.Def.PinchaQuietas || !h.HaAtrapado(p.Id)) return;
            var e = NuevoEvento(TipoEvento.PinchazoQuieta, p, i, i, h.Usos);
            e.Herramienta = h.Tipo;
            e.IndiceHerramienta = h.Indice;
            Registrar(e);
            Daniar(p, h.Def.Danio + (_curva[i] ? _reglas.BonoAlfilerEnCurva : 0));
        }

        /// <summary>Si la polilla tiene placas, la placa absorbe el efecto de control y se rompe.</summary>
        private bool SuperaPlaca(PolillaEnJuego p)
        {
            if (p.Placas <= 0) return true;
            p.Placas--;
            Emitir(TipoEvento.PlacaRota, p, p.Indice, p.Indice, p.Placas);
            return false;
        }

        /// <summary>
        /// Intenta que la herramienta afecte a la polilla. La primera vez gasta un uso y la "atrapa";
        /// después le afecta gratis (salvo herramientas de una vez por polilla). Devuelve si hubo efecto.
        /// </summary>
        private bool Afectar(HerramientaEnJuego h, PolillaEnJuego p)
        {
            if (!h.PuedeAfectar(p.Id)) return false;
            bool repite = h.HaAtrapado(p.Id);
            if (!repite)
            {
                h.Usos--;
                h.Atrapadas.Add(p.Id);
            }
            var e = NuevoEvento(TipoEvento.ActivaHerramienta, p, p.Indice, p.Indice, h.Usos, repite ? 1 : 0);
            e.Herramienta = h.Tipo;
            e.IndiceHerramienta = h.Indice;
            Registrar(e);
            if (!repite && h.Usos <= 0)
            {
                var g = NuevoEvento(TipoEvento.HerramientaGastada, p, p.Indice, p.Indice, 0);
                g.Herramienta = h.Tipo;
                g.IndiceHerramienta = h.Indice;
                Registrar(g);
            }
            return true;
        }

        private void Devorar(PolillaEnJuego p, HerramientaEnJuego h)
        {
            h.Destruida = true;
            var e = NuevoEvento(TipoEvento.Devora, p, p.Indice, p.Indice, h.Usos);
            e.Herramienta = h.Tipo;
            e.IndiceHerramienta = h.Indice;
            Registrar(e);
        }

        private void Daniar(PolillaEnJuego p, int cantidad)
        {
            p.Vida -= cantidad;
            Emitir(TipoEvento.Danio, p, p.Indice, p.Indice, cantidad, Math.Max(0, p.Vida));
            if (p.Vida <= 0)
            {
                p.Viva = false;
                _activas.Remove(p);
                Derrotadas++;
                Emitir(TipoEvento.Muere, p, p.Indice, p.Indice);
                return;
            }
            var umbrales = p.Def.UmbralesInvocacion;
            while (p.UmbralesDisparados < umbrales.Length && p.Vida <= umbrales[p.UmbralesDisparados])
            {
                Emitir(TipoEvento.LlamaAyuda, p, p.Indice, p.Indice, umbrales[p.UmbralesDisparados]);
                _invocaciones.Add(p.Def.TipoInvocado);
                p.UmbralesDisparados++;
            }
        }

        private void LlegarAlCarrete(PolillaEnJuego p)
        {
            Emitir(TipoEvento.LlegaAlCarrete, p, p.Indice, p.Indice);
            int danio = p.Def.DanioAlCarrete;
            if (_dedalDisponible && danio > 0)
            {
                _dedalDisponible = false;
                danio -= 1;
                Emitir(TipoEvento.DedalProtege, p, p.Indice, p.Indice, 1);
            }
            if (danio > 0)
            {
                VidaCarrete -= danio;
                Emitir(TipoEvento.CarreteDaniado, p, p.Indice, p.Indice, danio, Math.Max(0, VidaCarrete));
            }

            if (VidaCarrete > 0 && p.Def.DaVueltas)
            {
                p.Indice = 0;
                p.TurnosRetenida = 0;
                p.TurnosEnredada = 0;
                p.Acumulador = 0;
                p.Vueltas++;
                Emitir(TipoEvento.DaLaVuelta, p, _indiceSalida, 0);
                return;
            }

            p.Fuera = true;
            _activas.Remove(p);
            Escapadas++;
            if (VidaCarrete <= 0) Terminar(false);
        }

        private void Terminar(bool victoria)
        {
            if (Terminada) return;
            Terminada = true;
            Victoria = victoria;
            Emitir(victoria ? TipoEvento.Victoria : TipoEvento.Derrota, null);
        }

        private Evento NuevoEvento(TipoEvento tipo, PolillaEnJuego p, int desde, int hasta, int valor = 0, int extra = 0)
        {
            return new Evento
            {
                Tipo = tipo,
                Turno = Turno,
                PolillaId = p != null ? p.Id : -1,
                TipoPolilla = p != null ? p.Tipo : TipoPolilla.Comun,
                Desde = desde,
                Hasta = hasta,
                Valor = valor,
                Extra = extra,
                IndiceHerramienta = -1
            };
        }

        private void Emitir(TipoEvento tipo, PolillaEnJuego p, int desde = 0, int hasta = 0, int valor = 0, int extra = 0)
        {
            Registrar(NuevoEvento(tipo, p, desde, hasta, valor, extra));
        }

        private void Registrar(Evento e)
        {
            _eventosTurno.Add(e);
            if (_registrar) _registro.Add(e);
        }

        /// <summary>
        /// Casillas en las que aterrizaría una polilla veloz que recorre la lana sola, sin herramientas que la frenen
        /// (solo cuentan las curvas y las lámparas). La interfaz las marca con huellas para que el jugador
        /// vea dónde tocará el suelo.
        /// </summary>
        public static List<int> PrevisualizarAterrizajes(Recorrido recorrido, Reglas reglas)
        {
            var aterrizajes = new List<int>();
            if (!recorrido.EstaCompleto) return aterrizajes;
            var def = reglas.Polilla(TipoPolilla.Veloz);
            int pasosPorTurno = Math.Max(1, def.CasillasPorTurno);
            int salida = recorrido.IndiceSalida;
            var tablero = recorrido.Tablero;
            ulong lamparasVistas = 0;
            int indice = 0;
            while (indice < salida)
            {
                for (int paso = 1; paso <= pasosPorTurno; paso++)
                {
                    indice++;
                    if (indice >= salida) break;
                    bool lampara = false;
                    foreach (int l in tablero.LamparasAdyacentes(recorrido.Celdas[indice]))
                    {
                        if ((lamparasVistas & (1UL << l)) == 0)
                        {
                            lampara = true;
                            lamparasVistas |= 1UL << l;
                        }
                    }
                    if (paso == pasosPorTurno || recorrido.EsCurva(indice) || lampara)
                    {
                        aterrizajes.Add(indice);
                        break;
                    }
                }
            }
            return aterrizajes;
        }
    }
}
