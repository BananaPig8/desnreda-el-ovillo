using System.Collections.Generic;

namespace DesenredaElOvillo.Nucleo
{
    public enum ResultadoPaso
    {
        Avanzado,
        Retrocedido,
        Completado,
        SinCambios,
        NoAdyacente,
        Ocupada,
        Bloqueada,
        SinHilo,
        YaCompleto
    }

    /// <summary>
    /// La hebra de lana que el jugador tiende desde la entrada hasta el carrete.
    /// Siempre empieza en la entrada. Cada casilla de suelo cuesta 1 unidad de hilo;
    /// la entrada y el carrete no cuestan.
    /// Reglas: solo casillas adyacentes, sin repetir casillas (por tanto sin bucles ni cruces),
    /// sin atravesar obstáculos, y debe terminar en el carrete.
    /// </summary>
    public sealed class Recorrido
    {
        private readonly List<Celda> _celdas = new List<Celda>();

        public Tablero Tablero { get; }

        public Recorrido(Tablero tablero)
        {
            Tablero = tablero;
            _celdas.Add(tablero.Entrada);
        }

        public IReadOnlyList<Celda> Celdas => _celdas;
        public int Cantidad => _celdas.Count;
        public Celda Ultima => _celdas[_celdas.Count - 1];
        public bool EstaCompleto => _celdas.Count > 1 && Ultima == Tablero.Salida;

        /// <summary>Unidades de hilo gastadas (casillas de suelo del recorrido).</summary>
        public int HiloUsado => _celdas.Count - 1 - (EstaCompleto ? 1 : 0);

        /// <summary>Índice de la casilla del carrete dentro del recorrido, o -1 si aún no se ha llegado.</summary>
        public int IndiceSalida => EstaCompleto ? _celdas.Count - 1 : -1;

        public int IndiceDe(Celda c)
        {
            for (int i = 0; i < _celdas.Count; i++)
            {
                if (_celdas[i] == c) return i;
            }
            return -1;
        }

        public bool Contiene(Celda c) => IndiceDe(c) >= 0;

        /// <summary>
        /// Intenta añadir la celda al final de la hebra. Si la celda es la penúltima, deshace el último tramo
        /// (permite arrastrar hacia atrás para corregir).
        /// </summary>
        public ResultadoPaso IntentarAvanzar(Celda c, int hiloDisponible)
        {
            if (c == Ultima) return ResultadoPaso.SinCambios;
            if (_celdas.Count >= 2 && c == _celdas[_celdas.Count - 2])
            {
                _celdas.RemoveAt(_celdas.Count - 1);
                return ResultadoPaso.Retrocedido;
            }
            if (EstaCompleto) return ResultadoPaso.YaCompleto;
            if (!c.EsAdyacente(Ultima)) return ResultadoPaso.NoAdyacente;
            if (Contiene(c)) return ResultadoPaso.Ocupada;

            var tipo = Tablero.Tipo(c);
            if (tipo == TipoCasilla.Salida)
            {
                _celdas.Add(c);
                return ResultadoPaso.Completado;
            }
            if (tipo != TipoCasilla.Suelo) return ResultadoPaso.Bloqueada;
            if (HiloUsado + 1 > hiloDisponible) return ResultadoPaso.SinHilo;
            _celdas.Add(c);
            return ResultadoPaso.Avanzado;
        }

        /// <summary>Deja la hebra hasta el índice indicado (incluido). El índice 0 deja solo la entrada.</summary>
        public void RecortarHasta(int indice)
        {
            if (indice < 0) indice = 0;
            if (indice >= _celdas.Count - 1) return;
            _celdas.RemoveRange(indice + 1, _celdas.Count - indice - 1);
        }

        public void Reiniciar() => RecortarHasta(0);

        /// <summary>
        /// Una casilla es curva si la hebra cambia de dirección en ella.
        /// La entrada y el carrete nunca son curvas.
        /// </summary>
        public bool EsCurva(int indice)
        {
            if (indice <= 0 || indice >= _celdas.Count - 1) return false;
            var antes = _celdas[indice].Menos(_celdas[indice - 1]);
            var despues = _celdas[indice + 1].Menos(_celdas[indice]);
            return antes != despues;
        }

        public int ContarCurvas()
        {
            int n = 0;
            for (int i = 1; i < _celdas.Count - 1; i++) if (EsCurva(i)) n++;
            return n;
        }

        public Recorrido Clonar()
        {
            var copia = new Recorrido(Tablero);
            for (int i = 1; i < _celdas.Count; i++) copia._celdas.Add(_celdas[i]);
            return copia;
        }

        /// <summary>Construye una hebra a partir de celdas (sin incluir la entrada). Devuelve null si no es válida.</summary>
        public static Recorrido DesdeCeldas(Tablero tablero, IEnumerable<Celda> celdasSinEntrada, int hiloDisponible = int.MaxValue)
        {
            var r = new Recorrido(tablero);
            foreach (var c in celdasSinEntrada)
            {
                var res = r.IntentarAvanzar(c, hiloDisponible);
                if (res != ResultadoPaso.Avanzado && res != ResultadoPaso.Completado) return null;
            }
            return r;
        }

        /// <summary>
        /// Construye una hebra a partir de una cadena de movimientos desde la entrada:
        /// D (derecha), I (izquierda), A (arriba), B (abajo). Ej.: "DDDAAADDD". Devuelve null si no es válida.
        /// </summary>
        public static Recorrido DesdeMovimientos(Tablero tablero, string movimientos, int hiloDisponible = int.MaxValue)
        {
            var r = new Recorrido(tablero);
            foreach (char m in movimientos)
            {
                Celda d;
                switch (char.ToUpperInvariant(m))
                {
                    case 'D': d = new Celda(1, 0); break;
                    case 'I': d = new Celda(-1, 0); break;
                    case 'A': d = new Celda(0, 1); break;
                    case 'B': d = new Celda(0, -1); break;
                    case ' ': continue;
                    default: return null;
                }
                var res = r.IntentarAvanzar(r.Ultima.Mas(d), hiloDisponible);
                if (res != ResultadoPaso.Avanzado && res != ResultadoPaso.Completado) return null;
            }
            return r;
        }

        /// <summary>Convierte la hebra en la cadena de movimientos D/I/A/B (inversa de <see cref="DesdeMovimientos"/>).</summary>
        public string AMovimientos()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 1; i < _celdas.Count; i++)
            {
                var d = _celdas[i].Menos(_celdas[i - 1]);
                if (d.X == 1) sb.Append('D');
                else if (d.X == -1) sb.Append('I');
                else if (d.Y == 1) sb.Append('A');
                else sb.Append('B');
            }
            return sb.ToString();
        }
    }

    public static class ValidadorRecorrido
    {
        /// <summary>
        /// Devuelve la lista de problemas que impiden empezar la oleada (vacía si el recorrido es válido).
        /// Los textos están pensados para mostrarse tal cual en la interfaz.
        /// </summary>
        public static List<string> Problemas(Recorrido recorrido, int hiloDisponible, int longitudMinima)
        {
            var problemas = new List<string>();
            var t = recorrido.Tablero;
            var celdas = recorrido.Celdas;

            if (celdas.Count == 0 || celdas[0] != t.Entrada)
            {
                problemas.Add("La lana tiene que empezar en la entrada de las polillas.");
                return problemas;
            }

            for (int i = 1; i < celdas.Count; i++)
            {
                if (!celdas[i].EsAdyacente(celdas[i - 1]))
                {
                    problemas.Add("La lana tiene un salto: solo se pueden unir casillas vecinas.");
                    break;
                }
            }

            var vistas = new HashSet<Celda>();
            foreach (var c in celdas)
            {
                if (!vistas.Add(c))
                {
                    problemas.Add("La lana no puede pasar dos veces por la misma casilla.");
                    break;
                }
            }

            for (int i = 1; i < celdas.Count - 1; i++)
            {
                if (t.Tipo(celdas[i]) != TipoCasilla.Suelo)
                {
                    problemas.Add("La lana no puede atravesar objetos del taller.");
                    break;
                }
            }

            if (!recorrido.EstaCompleto)
                problemas.Add("La lana todavía no llega al carrete.");

            int usado = recorrido.HiloUsado;
            if (usado > hiloDisponible)
                problemas.Add("No queda hilo suficiente: usas " + usado + " y solo tienes " + hiloDisponible + ".");
            if (recorrido.EstaCompleto && usado < longitudMinima)
                problemas.Add("El camino es demasiado corto: necesita al menos " + longitudMinima + " casillas de lana (tiene " + usado + ").");

            return problemas;
        }

        public static bool EsValido(Recorrido recorrido, int hiloDisponible, int longitudMinima)
            => Problemas(recorrido, hiloDisponible, longitudMinima).Count == 0;
    }
}
