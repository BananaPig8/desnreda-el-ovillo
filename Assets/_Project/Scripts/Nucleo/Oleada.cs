using System;
using System.Collections.Generic;

namespace DesenredaElOvillo.Nucleo
{
    public struct Aparicion
    {
        public int Turno;
        public TipoPolilla Tipo;

        public Aparicion(int turno, TipoPolilla tipo)
        {
            Turno = turno;
            Tipo = tipo;
        }

        public override string ToString() => Tipo + "@" + Turno;
    }

    /// <summary>Guion de polillas de una sala: qué tipo aparece por la entrada y en qué turno.</summary>
    public sealed class Oleada
    {
        private readonly List<Aparicion> _apariciones = new List<Aparicion>();

        public IReadOnlyList<Aparicion> Apariciones => _apariciones;

        public Oleada(IEnumerable<Aparicion> apariciones)
        {
            _apariciones.AddRange(apariciones);
            // Orden estable: por turno y, a igual turno, por orden de declaración.
            var indexadas = new List<KeyValuePair<int, Aparicion>>();
            for (int i = 0; i < _apariciones.Count; i++) indexadas.Add(new KeyValuePair<int, Aparicion>(i, _apariciones[i]));
            indexadas.Sort((a, b) =>
            {
                int c = a.Value.Turno.CompareTo(b.Value.Turno);
                return c != 0 ? c : a.Key.CompareTo(b.Key);
            });
            _apariciones.Clear();
            foreach (var kv in indexadas) _apariciones.Add(kv.Value);
        }

        public int Contar(TipoPolilla tipo)
        {
            int n = 0;
            foreach (var a in _apariciones) if (a.Tipo == tipo) n++;
            return n;
        }

        /// <summary>Formato: "Comun@1, Comun@3, Veloz@4". También acepta "Comun x3 cada 2 desde 1".</summary>
        public static Oleada DesdeTexto(string texto)
        {
            var lista = new List<Aparicion>();
            foreach (var trozoBruto in texto.Split(','))
            {
                var trozo = trozoBruto.Trim();
                if (trozo.Length == 0) continue;
                if (trozo.Contains("@"))
                {
                    var partes = trozo.Split('@');
                    lista.Add(new Aparicion(int.Parse(partes[1].Trim()), ParsearTipo(partes[0])));
                }
                else
                {
                    // "Comun x3 cada 2 desde 1"
                    var palabras = trozo.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    var tipo = ParsearTipo(palabras[0]);
                    int cantidad = 1, cada = 1, desde = 1;
                    for (int i = 1; i < palabras.Length; i++)
                    {
                        var p = palabras[i].ToLowerInvariant();
                        if (p.StartsWith("x")) cantidad = int.Parse(p.Substring(1));
                        else if (p == "cada" && i + 1 < palabras.Length) cada = int.Parse(palabras[++i]);
                        else if (p == "desde" && i + 1 < palabras.Length) desde = int.Parse(palabras[++i]);
                    }
                    for (int k = 0; k < cantidad; k++) lista.Add(new Aparicion(desde + k * cada, tipo));
                }
            }
            return new Oleada(lista);
        }

        public static TipoPolilla ParsearTipo(string s)
        {
            switch (s.Trim().ToLowerInvariant())
            {
                case "comun": case "común": return TipoPolilla.Comun;
                case "veloz": return TipoPolilla.Veloz;
                case "acorazada": return TipoPolilla.Acorazada;
                case "reina": return TipoPolilla.Reina;
                default: throw new FormatException("Tipo de polilla desconocido: '" + s + "'.");
            }
        }

        public override string ToString() => string.Join(", ", _apariciones);
    }

    public struct Colocacion
    {
        public TipoHerramienta Tipo;
        /// <summary>Índice de la casilla dentro del recorrido (1 .. salida-1).</summary>
        public int Indice;

        public Colocacion(TipoHerramienta tipo, int indice)
        {
            Tipo = tipo;
            Indice = indice;
        }

        public override string ToString() => Tipo + "#" + Indice;
    }

    /// <summary>
    /// Herramientas colocadas sobre la lana antes de la oleada.
    /// Cada casilla admite una herramienta "de encima" y, además, un velcro cosido debajo.
    /// </summary>
    public sealed class PlanColocacion
    {
        private readonly List<Colocacion> _colocaciones = new List<Colocacion>();

        public IReadOnlyList<Colocacion> Colocaciones => _colocaciones;

        public PlanColocacion() { }

        public PlanColocacion(IEnumerable<Colocacion> colocaciones)
        {
            _colocaciones.AddRange(colocaciones);
        }

        public PlanColocacion Clonar() => new PlanColocacion(_colocaciones);

        public static bool VaDebajo(TipoHerramienta t) => t == TipoHerramienta.Velcro;

        /// <summary>¿Se puede poner esta herramienta en esta casilla del recorrido?</summary>
        public bool PuedeColocar(Recorrido recorrido, TipoHerramienta tipo, int indice)
        {
            if (!recorrido.EstaCompleto) return false;
            if (indice <= 0 || indice >= recorrido.IndiceSalida) return false;
            bool debajo = VaDebajo(tipo);
            foreach (var c in _colocaciones)
            {
                if (c.Indice == indice && VaDebajo(c.Tipo) == debajo) return false;
            }
            return true;
        }

        public bool Colocar(Recorrido recorrido, TipoHerramienta tipo, int indice)
        {
            if (!PuedeColocar(recorrido, tipo, indice)) return false;
            _colocaciones.Add(new Colocacion(tipo, indice));
            return true;
        }

        /// <summary>Quita la herramienta de encima (o, si no hay, el velcro) de la casilla. Devuelve lo quitado.</summary>
        public TipoHerramienta? QuitarEn(int indice)
        {
            int elegido = -1;
            for (int i = 0; i < _colocaciones.Count; i++)
            {
                if (_colocaciones[i].Indice != indice) continue;
                if (!VaDebajo(_colocaciones[i].Tipo)) { elegido = i; break; }
                if (elegido < 0) elegido = i;
            }
            if (elegido < 0) return null;
            var tipo = _colocaciones[elegido].Tipo;
            _colocaciones.RemoveAt(elegido);
            return tipo;
        }

        public void Limpiar() => _colocaciones.Clear();

        public int Contar(TipoHerramienta tipo)
        {
            int n = 0;
            foreach (var c in _colocaciones) if (c.Tipo == tipo) n++;
            return n;
        }

        /// <summary>Quita las herramientas que ya no caen sobre la lana (p. ej., tras acortar el recorrido).</summary>
        public List<TipoHerramienta> PodarFueraDe(Recorrido recorrido)
        {
            var quitadas = new List<TipoHerramienta>();
            int salida = recorrido.EstaCompleto ? recorrido.IndiceSalida : recorrido.Cantidad;
            for (int i = _colocaciones.Count - 1; i >= 0; i--)
            {
                if (_colocaciones[i].Indice <= 0 || _colocaciones[i].Indice >= salida)
                {
                    quitadas.Add(_colocaciones[i].Tipo);
                    _colocaciones.RemoveAt(i);
                }
            }
            return quitadas;
        }

        /// <summary>Comprueba que el plan es coherente con el recorrido y con el costurero (herramientas disponibles).</summary>
        public List<string> Problemas(Recorrido recorrido, IReadOnlyList<TipoHerramienta> costurero)
        {
            var problemas = new List<string>();
            var copia = new PlanColocacion();
            foreach (var c in _colocaciones)
            {
                if (!copia.Colocar(recorrido, c.Tipo, c.Indice))
                    problemas.Add("No se puede colocar " + c.Tipo + " en la casilla " + c.Indice + ".");
            }
            if (costurero != null)
            {
                foreach (TipoHerramienta t in Enum.GetValues(typeof(TipoHerramienta)))
                {
                    int disponibles = 0;
                    foreach (var k in costurero) if (k == t) disponibles++;
                    if (Contar(t) > disponibles)
                        problemas.Add("Hay más herramientas de tipo " + t + " colocadas de las que tienes en el costurero.");
                }
            }
            return problemas;
        }

        public override string ToString() => string.Join(", ", _colocaciones);
    }
}
