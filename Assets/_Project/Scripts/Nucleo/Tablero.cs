using System;
using System.Collections.Generic;

namespace DesenredaElOvillo.Nucleo
{
    public enum TipoCasilla
    {
        /// <summary>Fieltro libre: se puede tender lana encima.</summary>
        Suelo,
        /// <summary>Objeto del taller (botón gigante, dedal, carrete, caja...). No se puede atravesar.</summary>
        Obstaculo,
        /// <summary>Agujero del rodapié por el que entran las polillas.</summary>
        Entrada,
        /// <summary>El carrete que protege Ovillo.</summary>
        Salida,
        /// <summary>Obstáculo especial: deslumbra a las polillas que pasan a su lado.</summary>
        Lampara
    }

    /// <summary>
    /// Cuadrícula de una sala. Inmutable una vez creada.
    /// Formato de texto (filas de arriba abajo, los espacios se ignoran):
    ///   .  suelo        E  entrada      S  salida (carrete)
    ///   L  lámpara      cualquier otro símbolo = obstáculo (el símbolo elige el dibujo)
    ///      #  caja de costura   o  botón gigante   u  dedal   c  carrete pequeño
    ///      a  acerico           t  tijeras cerradas   m  metro de costura
    /// </summary>
    public sealed class Tablero
    {
        private readonly TipoCasilla[] _tipos;
        private readonly char[] _simbolos;
        private readonly List<Celda> _lamparas = new List<Celda>();

        public int Ancho { get; }
        public int Alto { get; }
        public Celda Entrada { get; }
        public Celda Salida { get; }
        public IReadOnlyList<Celda> Lamparas => _lamparas;

        private Tablero(int ancho, int alto, TipoCasilla[] tipos, char[] simbolos, Celda entrada, Celda salida)
        {
            Ancho = ancho;
            Alto = alto;
            _tipos = tipos;
            _simbolos = simbolos;
            Entrada = entrada;
            Salida = salida;
            for (int y = 0; y < alto; y++)
            {
                for (int x = 0; x < ancho; x++)
                {
                    if (tipos[y * ancho + x] == TipoCasilla.Lampara) _lamparas.Add(new Celda(x, y));
                }
            }
        }

        public bool Dentro(Celda c) => c.X >= 0 && c.Y >= 0 && c.X < Ancho && c.Y < Alto;

        public TipoCasilla Tipo(Celda c)
        {
            if (!Dentro(c)) return TipoCasilla.Obstaculo;
            return _tipos[c.Y * Ancho + c.X];
        }

        /// <summary>Símbolo original del mapa; la capa visual lo usa para elegir el objeto que dibuja.</summary>
        public char Simbolo(Celda c) => Dentro(c) ? _simbolos[c.Y * Ancho + c.X] : '#';

        /// <summary>Casillas donde se puede tender lana (sin contar entrada y salida).</summary>
        public bool EsConstruible(Celda c) => Tipo(c) == TipoCasilla.Suelo;

        /// <summary>Índices (en <see cref="Lamparas"/>) de las lámparas ortogonalmente adyacentes a la celda.</summary>
        public List<int> LamparasAdyacentes(Celda c)
        {
            var resultado = new List<int>();
            for (int i = 0; i < _lamparas.Count; i++)
            {
                if (_lamparas[i].EsAdyacente(c)) resultado.Add(i);
            }
            return resultado;
        }

        public static Tablero DesdeTexto(IReadOnlyList<string> filasTexto)
        {
            var filas = new List<string>();
            foreach (var bruta in filasTexto)
            {
                if (bruta == null) continue;
                var limpia = bruta.Replace(" ", string.Empty).Replace("\t", string.Empty).TrimEnd('\r');
                if (limpia.Length > 0) filas.Add(limpia);
            }

            if (filas.Count == 0) throw new FormatException("El mapa de la sala está vacío.");
            int ancho = filas[0].Length;
            int alto = filas.Count;
            var tipos = new TipoCasilla[ancho * alto];
            var simbolos = new char[ancho * alto];
            Celda entrada = default, salida = default;
            int entradas = 0, salidas = 0;

            for (int fila = 0; fila < alto; fila++)
            {
                if (filas[fila].Length != ancho)
                    throw new FormatException("Todas las filas del mapa deben tener " + ancho + " casillas (la fila " + (fila + 1) + " tiene " + filas[fila].Length + ").");
                int y = alto - 1 - fila;
                for (int x = 0; x < ancho; x++)
                {
                    char s = filas[fila][x];
                    TipoCasilla tipo;
                    switch (s)
                    {
                        case '.': tipo = TipoCasilla.Suelo; break;
                        case 'E': tipo = TipoCasilla.Entrada; entrada = new Celda(x, y); entradas++; break;
                        case 'S': tipo = TipoCasilla.Salida; salida = new Celda(x, y); salidas++; break;
                        case 'L': tipo = TipoCasilla.Lampara; break;
                        default: tipo = TipoCasilla.Obstaculo; break;
                    }
                    tipos[y * ancho + x] = tipo;
                    simbolos[y * ancho + x] = s;
                }
            }

            if (entradas != 1) throw new FormatException("El mapa necesita exactamente una entrada 'E' (tiene " + entradas + ").");
            if (salidas != 1) throw new FormatException("El mapa necesita exactamente una salida 'S' (tiene " + salidas + ").");
            return new Tablero(ancho, alto, tipos, simbolos, entrada, salida);
        }

        /// <summary>Vuelve a escribir el mapa en el mismo formato de texto (útil para depurar e informes).</summary>
        public string ATexto(IReadOnlyCollection<Celda> resaltadas = null, char simboloResaltado = '*')
        {
            var sb = new System.Text.StringBuilder();
            for (int y = Alto - 1; y >= 0; y--)
            {
                for (int x = 0; x < Ancho; x++)
                {
                    var c = new Celda(x, y);
                    char s = Simbolo(c);
                    if (resaltadas != null && s == '.' && Contiene(resaltadas, c)) s = simboloResaltado;
                    sb.Append(s);
                    if (x < Ancho - 1) sb.Append(' ');
                }
                if (y > 0) sb.Append('\n');
            }
            return sb.ToString();
        }

        private static bool Contiene(IReadOnlyCollection<Celda> celdas, Celda c)
        {
            foreach (var otra in celdas) if (otra == c) return true;
            return false;
        }
    }
}
