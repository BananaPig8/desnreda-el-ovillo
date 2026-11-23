using System;
using System.Collections.Generic;

namespace DesenredaElOvillo.Nucleo
{
    public enum FasePartida
    {
        /// <summary>Se ofrecen herramientas y el jugador elige las que se guarda en el costurero.</summary>
        EleccionHerramientas,
        /// <summary>Tender la lana y colocar herramientas.</summary>
        Preparacion,
        /// <summary>Elegir una mejora tras superar una sala.</summary>
        Recompensa,
        Victoria,
        Derrota
    }

    public sealed class EstadisticasPartida
    {
        public int PolillasDerrotadas;
        public int PolillasEscapadas;
        public int HiloUsado;
        public int SalasSuperadas;
        public int Turnos;
        public int Reintentos;

        public EstadisticasPartida Clonar() => (EstadisticasPartida)MemberwiseClone();
    }

    /// <summary>
    /// Una partida de la demo: cinco salas seguidas, un carrete con 4 puntos de vida para toda la partida,
    /// un costurero que crece y mejoras elegidas por el camino. Todo el azar sale de la semilla.
    /// </summary>
    public sealed class Partida
    {
        private readonly List<TipoHerramienta> _costurero = new List<TipoHerramienta>();
        private readonly List<TipoMejora> _mejoras = new List<TipoMejora>();
        private Reglas _reglasCache;
        private Instantanea _instantanea;

        public Reglas ReglasBase { get; }
        public IReadOnlyList<Sala> Salas { get; }
        public int Semilla { get; }
        public bool ReintentarDesdeSala { get; set; } = true;

        public int IndiceSala { get; private set; }
        public Sala SalaActual => Salas[IndiceSala];
        public int NumeroSala => IndiceSala + 1;
        public FasePartida Fase { get; private set; }
        public int VidaCarrete { get; private set; }
        public int HiloReserva { get; private set; }
        public IReadOnlyList<TipoHerramienta> Costurero => _costurero;
        public IReadOnlyList<TipoMejora> Mejoras => _mejoras;
        public List<TipoHerramienta> OfertaHerramientas { get; private set; } = new List<TipoHerramienta>();
        public List<TipoMejora> OfertaMejoras { get; private set; } = new List<TipoMejora>();
        public EstadisticasPartida Estadisticas { get; private set; } = new EstadisticasPartida();
        public ResultadoOleada UltimoResultado { get; private set; }

        public Reglas Reglas => _reglasCache ?? (_reglasCache = ReglasBase.ConMejoras(_mejoras));

        /// <summary>Hilo de esta sala: el de la sala + mejoras + lo que sobró en la anterior.</summary>
        public int HiloDisponible => SalaActual.Hilo + Reglas.HiloExtraPorSala + HiloReserva;

        public Partida(Reglas reglasBase, IReadOnlyList<Sala> salas, int semilla)
        {
            if (salas == null || salas.Count == 0) throw new ArgumentException("La partida necesita al menos una sala.");
            ReglasBase = reglasBase;
            Salas = salas;
            Semilla = semilla;
            VidaCarrete = reglasBase.VidaCarrete;
            EmpezarSala(0);
        }

        private void EmpezarSala(int indice)
        {
            IndiceSala = indice;
            OfertaMejoras = new List<TipoMejora>();
            OfertaHerramientas = GenerarOfertaHerramientas();
            var sala = SalaActual;
            if (OfertaHerramientas.Count == 0)
            {
                EntrarEnPreparacion();
            }
            else if (sala.OfertaFija.Count > 0 && OfertaHerramientas.Count <= sala.CantidadAElegir)
            {
                _costurero.AddRange(OfertaHerramientas);
                OfertaHerramientas = new List<TipoHerramienta>();
                EntrarEnPreparacion();
            }
            else
            {
                Fase = FasePartida.EleccionHerramientas;
            }
        }

        /// <summary>Herramientas desbloqueadas hasta la sala actual (las presentadas en salas anteriores o en esta).</summary>
        public List<TipoHerramienta> HerramientasDesbloqueadas()
        {
            var lista = new List<TipoHerramienta>();
            for (int i = 0; i <= IndiceSala; i++)
            {
                foreach (var t in Salas[i].OfertaFija) if (!lista.Contains(t)) lista.Add(t);
                foreach (var t in Salas[i].OfertaGarantizada) if (!lista.Contains(t)) lista.Add(t);
            }
            if (lista.Count == 0)
            {
                foreach (TipoHerramienta t in Enum.GetValues(typeof(TipoHerramienta))) lista.Add(t);
            }
            return lista;
        }

        private List<TipoHerramienta> GenerarOfertaHerramientas()
        {
            var sala = SalaActual;
            if (sala.OfertaFija.Count > 0) return new List<TipoHerramienta>(sala.OfertaFija);
            if (sala.CantidadOfrecida <= 0) return new List<TipoHerramienta>();

            var rng = Rng.Derivado(Semilla, 100 + IndiceSala);
            var oferta = new List<TipoHerramienta>();
            foreach (var t in sala.OfertaGarantizada)
            {
                if (oferta.Count < sala.CantidadOfrecida) oferta.Add(t);
            }
            var bolsa = HerramientasDesbloqueadas();
            int intentos = 0;
            while (oferta.Count < sala.CantidadOfrecida && intentos++ < 100)
            {
                var candidata = rng.Elegir(bolsa);
                // Nunca tres iguales: siempre hay una elección real.
                int iguales = 0;
                foreach (var t in oferta) if (t == candidata) iguales++;
                if (iguales >= 2) continue;
                oferta.Add(candidata);
            }

            // Red de seguridad: toda oferta incluye al menos una herramienta de daño.
            // Sin ella, una mala racha de azar podía dejar una sala imposible de ganar sin perder vida.
            var deDanio = new List<TipoHerramienta>();
            foreach (var t in bolsa) if (ReglasBase.Herramienta(t).Categoria == CategoriaHerramienta.Danio) deDanio.Add(t);
            bool hayDanio = false;
            foreach (var t in oferta) if (ReglasBase.Herramienta(t).Categoria == CategoriaHerramienta.Danio) hayDanio = true;
            if (!hayDanio && deDanio.Count > 0 && oferta.Count > sala.OfertaGarantizada.Count)
                oferta[oferta.Count - 1] = rng.Elegir(deDanio);

            rng.Barajar(oferta);
            return oferta;
        }

        public void ElegirHerramientas(IList<TipoHerramienta> elegidas)
        {
            if (Fase != FasePartida.EleccionHerramientas) throw new InvalidOperationException("Ahora no toca elegir herramientas.");
            int cantidad = Math.Min(SalaActual.CantidadAElegir, OfertaHerramientas.Count);
            if (elegidas.Count != cantidad) throw new ArgumentException("Hay que elegir " + cantidad + " herramientas.");
            var restantes = new List<TipoHerramienta>(OfertaHerramientas);
            foreach (var t in elegidas)
            {
                if (!restantes.Remove(t)) throw new ArgumentException(t + " no está en la oferta.");
            }
            _costurero.AddRange(elegidas);
            OfertaHerramientas = new List<TipoHerramienta>();
            EntrarEnPreparacion();
        }

        private void EntrarEnPreparacion()
        {
            Fase = FasePartida.Preparacion;
            _instantanea = new Instantanea
            {
                VidaCarrete = VidaCarrete,
                HiloReserva = HiloReserva,
                Costurero = new List<TipoHerramienta>(_costurero),
                Mejoras = new List<TipoMejora>(_mejoras),
                Estadisticas = Estadisticas.Clonar()
            };
        }

        /// <summary>Problemas que impiden empezar la oleada con este recorrido y estas herramientas.</summary>
        public List<string> ProblemasParaEmpezar(Recorrido recorrido, PlanColocacion plan)
        {
            var problemas = ValidadorRecorrido.Problemas(recorrido, HiloDisponible, SalaActual.LongitudMinima);
            if (problemas.Count == 0 && plan != null) problemas.AddRange(plan.Problemas(recorrido, _costurero));
            return problemas;
        }

        /// <summary>Crea el simulador de la oleada actual (la capa visual lo ejecuta turno a turno).</summary>
        public SimuladorOleada CrearSimulador(Recorrido recorrido, PlanColocacion plan, bool registrarEventos = true)
        {
            if (Fase != FasePartida.Preparacion) throw new InvalidOperationException("La oleada solo empieza desde la preparación.");
            var problemas = ProblemasParaEmpezar(recorrido, plan);
            if (problemas.Count > 0) throw new InvalidOperationException(problemas[0]);
            return new SimuladorOleada(Reglas, recorrido, plan, SalaActual.Oleada, VidaCarrete, registrarEventos);
        }

        /// <summary>Resuelve la oleada de golpe y aplica el resultado (útil para pruebas y para el laboratorio).</summary>
        public ResultadoOleada JugarOleada(Recorrido recorrido, PlanColocacion plan)
        {
            var sim = CrearSimulador(recorrido, plan, false);
            while (!sim.Terminada) sim.EjecutarTurno();
            var resultado = sim.Resultado();
            AplicarResultado(resultado, recorrido.HiloUsado);
            return resultado;
        }

        public void AplicarResultado(ResultadoOleada resultado, int hiloUsado)
        {
            if (Fase != FasePartida.Preparacion) throw new InvalidOperationException("No hay ninguna oleada en curso.");
            UltimoResultado = resultado;
            VidaCarrete = resultado.VidaCarreteFinal;
            Estadisticas.PolillasDerrotadas += resultado.Derrotadas;
            Estadisticas.PolillasEscapadas += resultado.Escapadas;
            Estadisticas.Turnos += resultado.Turnos;

            if (!resultado.Victoria || VidaCarrete <= 0)
            {
                Fase = FasePartida.Derrota;
                return;
            }

            Estadisticas.SalasSuperadas++;
            Estadisticas.HiloUsado += hiloUsado;
            int sobrante = Math.Max(0, HiloDisponible - hiloUsado);
            HiloReserva = Math.Min(sobrante, Reglas.ReservaMaximaDeHilo);

            if (IndiceSala >= Salas.Count - 1)
            {
                Fase = FasePartida.Victoria;
                return;
            }
            OfertaMejoras = GenerarOfertaMejoras();
            if (OfertaMejoras.Count == 0) EmpezarSala(IndiceSala + 1);
            else Fase = FasePartida.Recompensa;
        }

        private List<TipoMejora> GenerarOfertaMejoras()
        {
            var rng = Rng.Derivado(Semilla, 200 + IndiceSala);
            var candidatas = new List<TipoMejora>();
            foreach (var m in CatalogoMejoras.Todas)
            {
                if (_mejoras.Contains(m.Tipo)) continue;
                if (m.Tipo == TipoMejora.Remiendo && VidaCarrete >= ReglasBase.VidaCarreteMaxima) continue;
                if (m.Herramienta.HasValue && !TieneOPuedeTener(m.Herramienta.Value)) continue;
                candidatas.Add(m.Tipo);
            }
            rng.Barajar(candidatas);
            if (candidatas.Count > 3) candidatas.RemoveRange(3, candidatas.Count - 3);
            return candidatas;
        }

        /// <summary>Solo se ofrecen mejoras de herramientas que el jugador ya tiene o que ya se han presentado.</summary>
        private bool TieneOPuedeTener(TipoHerramienta t)
        {
            if (_costurero.Contains(t)) return true;
            int siguiente = Math.Min(IndiceSala + 1, Salas.Count - 1);
            for (int i = 0; i <= siguiente; i++)
            {
                if (Salas[i].OfertaFija.Contains(t) || Salas[i].OfertaGarantizada.Contains(t)) return true;
            }
            return false;
        }

        public void ElegirMejora(TipoMejora mejora)
        {
            if (Fase != FasePartida.Recompensa) throw new InvalidOperationException("Ahora no toca elegir mejora.");
            if (!OfertaMejoras.Contains(mejora)) throw new ArgumentException("Esa mejora no está en la oferta.");
            _mejoras.Add(mejora);
            _reglasCache = null;
            if (mejora == TipoMejora.Remiendo)
                VidaCarrete = Math.Min(ReglasBase.VidaCarreteMaxima, VidaCarrete + 2);
            EmpezarSala(IndiceSala + 1);
        }

        /// <summary>
        /// Tras una derrota: en modo feria se repite la sala actual tal y como empezó; si no, la partida acaba.
        /// </summary>
        public bool ReintentarSala()
        {
            if (Fase != FasePartida.Derrota || !ReintentarDesdeSala || _instantanea == null) return false;
            VidaCarrete = _instantanea.VidaCarrete;
            HiloReserva = _instantanea.HiloReserva;
            _costurero.Clear();
            _costurero.AddRange(_instantanea.Costurero);
            _mejoras.Clear();
            _mejoras.AddRange(_instantanea.Mejoras);
            _reglasCache = null;
            int reintentos = Estadisticas.Reintentos + 1;
            Estadisticas = _instantanea.Estadisticas.Clonar();
            Estadisticas.Reintentos = reintentos;
            Fase = FasePartida.Preparacion;
            return true;
        }

        private sealed class Instantanea
        {
            public int VidaCarrete;
            public int HiloReserva;
            public List<TipoHerramienta> Costurero;
            public List<TipoMejora> Mejoras;
            public EstadisticasPartida Estadisticas;
        }
    }
}
