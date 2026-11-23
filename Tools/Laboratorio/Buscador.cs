using System;
using System.Collections.Generic;
using System.Linq;
using DesenredaElOvillo.Nucleo;

namespace DesenredaElOvillo.Laboratorio
{
    /// <summary>Una propuesta completa para una sala: lana + herramientas, con su resultado.</summary>
    public sealed class Propuesta
    {
        public Recorrido Recorrido;
        public List<Colocacion> Colocaciones;
        public ResultadoOleada Resultado;
        public double Puntuacion;

        public string ColocacionesTexto() => string.Join(", ", Colocaciones.OrderBy(c => c.Indice).ThenBy(c => c.Tipo).Select(c => c.Tipo + "@" + c.Indice));
    }

    /// <summary>
    /// Busca buenas soluciones para una sala con un costurero concreto:
    ///  1. Genera muchos recorridos válidos al azar (DFS aleatorio con poda por distancia al carrete).
    ///  2. Para cada recorrido optimiza la colocación con búsqueda local (mover y reordenar herramientas).
    ///  3. Refina los mejores candidatos con más iteraciones.
    /// También mide al "jugador ingenuo": recorridos y colocaciones al azar.
    /// </summary>
    public sealed class Buscador
    {
        private readonly Sala _sala;
        private readonly Reglas _reglas;
        private readonly List<TipoHerramienta> _costurero;
        private readonly int _hilo;
        private readonly int _vida;
        private readonly Random _azar;

        public Buscador(Sala sala, Reglas reglas, IEnumerable<TipoHerramienta> costurero, int semilla, int hiloExtra = 0, int vida = -1)
        {
            _sala = sala;
            _reglas = reglas;
            _costurero = costurero.ToList();
            _hilo = sala.Hilo + reglas.HiloExtraPorSala + hiloExtra;
            _vida = vida > 0 ? vida : reglas.VidaCarrete;
            _azar = new Random(semilla);
        }

        public int Hilo => _hilo;

        public static double Puntuar(ResultadoOleada r)
        {
            // Primero sobrevivir, luego no recibir daño, luego derrotar polillas; a igualdad, menos turnos.
            return (r.Victoria ? 10000 : 0) + r.VidaCarreteFinal * 1000 + r.Derrotadas * 10 - r.Turnos * 0.01;
        }

        // ---------------------------------------------------------------- Recorridos

        public List<Recorrido> GenerarRecorridos(int cantidad, int intentosMaximos = 200000)
        {
            var t = _sala.Tablero;
            var vistos = new HashSet<string>();
            var lista = new List<Recorrido>();
            int intentos = 0;
            while (lista.Count < cantidad && intentos++ < intentosMaximos)
            {
                int objetivo = _azar.Next(_sala.LongitudMinima, _hilo + 1);
                var r = RecorridoAleatorio(t, objetivo);
                if (r == null) continue;
                var clave = r.AMovimientos();
                if (vistos.Add(clave)) lista.Add(r);
            }
            return lista;
        }

        private Recorrido RecorridoAleatorio(Tablero t, int objetivo)
        {
            var r = new Recorrido(t);
            var pasosDisponibles = new List<Celda>(4);
            int pasos = 0;
            while (!r.EstaCompleto && pasos++ < 400)
            {
                var actual = r.Ultima;
                int restante = _hilo - r.HiloUsado;
                var dist = DistanciasAlCarrete(t, r);
                pasosDisponibles.Clear();
                bool salidaAlLado = false;
                foreach (var d in Celda.Direcciones)
                {
                    var c = actual.Mas(d);
                    if (c == t.Salida) { salidaAlLado = true; continue; }
                    if (!t.EsConstruible(c) || r.Contiene(c)) continue;
                    int dc = dist[c.Y * t.Ancho + c.X];
                    if (dc < 0 || dc - 1 > restante - 1) continue; // tras entrar en c quedan restante-1 unidades
                    pasosDisponibles.Add(c);
                }
                if (salidaAlLado && (r.HiloUsado >= objetivo || pasosDisponibles.Count == 0))
                {
                    if (r.HiloUsado < _sala.LongitudMinima) return null;
                    r.IntentarAvanzar(t.Salida, _hilo);
                    break;
                }
                if (pasosDisponibles.Count == 0) return null;
                // Sesgo suave: si aún queda mucho por recorrer, prefiere alejarse del carrete.
                Celda elegida;
                if (r.HiloUsado < objetivo - 2 && _azar.NextDouble() < 0.55)
                    elegida = pasosDisponibles.OrderByDescending(c => dist[c.Y * t.Ancho + c.X]).ThenBy(_ => _azar.Next()).First();
                else
                    elegida = pasosDisponibles[_azar.Next(pasosDisponibles.Count)];
                if (r.IntentarAvanzar(elegida, _hilo) != ResultadoPaso.Avanzado) return null;
            }
            if (!r.EstaCompleto) return null;
            return ValidadorRecorrido.EsValido(r, _hilo, _sala.LongitudMinima) ? r : null;
        }

        /// <summary>Distancia (en pasos) desde cada casilla libre hasta el carrete, sin pisar la hebra actual.</summary>
        private static int[] DistanciasAlCarrete(Tablero t, Recorrido r)
        {
            var dist = new int[t.Ancho * t.Alto];
            for (int i = 0; i < dist.Length; i++) dist[i] = -1;
            var cola = new Queue<Celda>();
            dist[t.Salida.Y * t.Ancho + t.Salida.X] = 0;
            cola.Enqueue(t.Salida);
            while (cola.Count > 0)
            {
                var c = cola.Dequeue();
                int dc = dist[c.Y * t.Ancho + c.X];
                foreach (var d in Celda.Direcciones)
                {
                    var n = c.Mas(d);
                    if (!t.Dentro(n) || !t.EsConstruible(n) || r.Contiene(n)) continue;
                    int k = n.Y * t.Ancho + n.X;
                    if (dist[k] >= 0) continue;
                    dist[k] = dc + 1;
                    cola.Enqueue(n);
                }
            }
            return dist;
        }

        // ---------------------------------------------------------------- Colocaciones

        public Propuesta Evaluar(Recorrido r, List<Colocacion> colocaciones)
        {
            var plan = new PlanColocacion(colocaciones);
            var res = SimuladorOleada.Simular(_reglas, r, plan, _sala.Oleada, _vida, false);
            return new Propuesta { Recorrido = r, Colocaciones = colocaciones, Resultado = res, Puntuacion = Puntuar(res) };
        }

        public List<Colocacion> ColocacionAleatoria(Recorrido r)
        {
            int casillas = r.IndiceSalida - 1;
            var lista = new List<Colocacion>();
            var plan = new PlanColocacion();
            foreach (var tipo in _costurero)
            {
                for (int intento = 0; intento < 50; intento++)
                {
                    int i = 1 + _azar.Next(casillas);
                    if (plan.Colocar(r, tipo, i))
                    {
                        lista.Add(new Colocacion(tipo, i));
                        break;
                    }
                }
            }
            return lista;
        }

        private bool EsValida(Recorrido r, List<Colocacion> lista)
        {
            var plan = new PlanColocacion();
            foreach (var c in lista) if (!plan.Colocar(r, c.Tipo, c.Indice)) return false;
            return true;
        }

        public Propuesta OptimizarColocacion(Recorrido r, int iteraciones, List<Colocacion> inicio = null)
        {
            var actual = Evaluar(r, inicio ?? ColocacionAleatoria(r));
            var mejor = actual;
            int casillas = r.IndiceSalida - 1;
            if (actual.Colocaciones.Count == 0) return actual;
            for (int it = 0; it < iteraciones; it++)
            {
                var vecino = new List<Colocacion>(actual.Colocaciones);
                int mov = _azar.Next(3);
                if (mov == 0 || vecino.Count < 2)
                {
                    int k = _azar.Next(vecino.Count);
                    vecino[k] = new Colocacion(vecino[k].Tipo, 1 + _azar.Next(casillas));
                }
                else if (mov == 1)
                {
                    int a = _azar.Next(vecino.Count), b = _azar.Next(vecino.Count);
                    if (a == b) continue;
                    var ia = vecino[a].Indice;
                    vecino[a] = new Colocacion(vecino[a].Tipo, vecino[b].Indice);
                    vecino[b] = new Colocacion(vecino[b].Tipo, ia);
                }
                else
                {
                    int k = _azar.Next(vecino.Count);
                    int delta = _azar.Next(2) == 0 ? -1 : 1;
                    int ni = vecino[k].Indice + delta;
                    if (ni < 1 || ni > casillas) continue;
                    vecino[k] = new Colocacion(vecino[k].Tipo, ni);
                }
                if (!EsValida(r, vecino)) continue;
                var p = Evaluar(r, vecino);
                if (p.Puntuacion >= actual.Puntuacion)
                {
                    actual = p;
                    if (p.Puntuacion > mejor.Puntuacion) mejor = p;
                }
            }
            return mejor;
        }

        public Propuesta Buscar(int recorridos, int iteracionesRapidas, int finalistas, int iteracionesFinales)
        {
            var candidatos = GenerarRecorridos(recorridos);
            var rapidas = new List<Propuesta>();
            foreach (var r in candidatos) rapidas.Add(OptimizarColocacion(r, iteracionesRapidas));
            var top = rapidas.OrderByDescending(p => p.Puntuacion).ThenBy(p => p.Recorrido.HiloUsado).Take(finalistas).ToList();
            Propuesta mejor = null;
            foreach (var p in top)
            {
                for (int reinicio = 0; reinicio < 3; reinicio++)
                {
                    var refinada = OptimizarColocacion(p.Recorrido, iteracionesFinales, reinicio == 0 ? p.Colocaciones : null);
                    if (mejor == null || refinada.Puntuacion > mejor.Puntuacion ||
                        (refinada.Puntuacion == mejor.Puntuacion && refinada.Recorrido.HiloUsado < mejor.Recorrido.HiloUsado))
                        mejor = refinada;
                }
            }
            return mejor;
        }

        /// <summary>Estadísticas del jugador ingenuo: recorrido al azar y herramientas al azar.</summary>
        public EstadisticaIngenua JugadorIngenuo(int partidas, int intentosPorRecorrido = 1)
        {
            var est = new EstadisticaIngenua();
            var recorridos = GenerarRecorridos(Math.Max(10, partidas / 4));
            if (recorridos.Count == 0) return est;
            for (int i = 0; i < partidas; i++)
            {
                var r = recorridos[_azar.Next(recorridos.Count)];
                Propuesta mejor = null;
                for (int k = 0; k < intentosPorRecorrido; k++)
                {
                    var p = Evaluar(r, ColocacionAleatoria(r));
                    if (mejor == null || p.Puntuacion > mejor.Puntuacion) mejor = p;
                }
                est.Registrar(mejor.Resultado, _vida);
            }
            return est;
        }
    }

    public sealed class EstadisticaIngenua
    {
        public int Partidas;
        public int Victorias;
        public int Perfectas;
        public int DanioTotal;
        public int[] Histograma = new int[8];

        public void Registrar(ResultadoOleada r, int vida)
        {
            Partidas++;
            if (r.Victoria) Victorias++;
            int danio = Math.Max(0, vida - r.VidaCarreteFinal);
            if (r.Victoria && danio == 0) Perfectas++;
            DanioTotal += danio;
            Histograma[Math.Min(Histograma.Length - 1, danio)]++;
        }

        public double PorcentajeVictorias => Partidas == 0 ? 0 : 100.0 * Victorias / Partidas;
        public double PorcentajePerfectas => Partidas == 0 ? 0 : 100.0 * Perfectas / Partidas;
        public double DanioMedio => Partidas == 0 ? 0 : (double)DanioTotal / Partidas;
    }
}
