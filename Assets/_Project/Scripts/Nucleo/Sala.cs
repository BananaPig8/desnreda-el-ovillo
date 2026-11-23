using System;
using System.Collections.Generic;

namespace DesenredaElOvillo.Nucleo
{
    /// <summary>Solución de referencia guardada en el archivo de la sala. Las pruebas automáticas comprueban que sigue ganando.</summary>
    public sealed class SolucionReferencia
    {
        public string Movimientos;
        public List<TipoHerramienta> Costurero = new List<TipoHerramienta>();
        public List<Colocacion> Colocaciones = new List<Colocacion>();
        public List<TipoMejora> Mejoras = new List<TipoMejora>();
        /// <summary>Vida del carrete con la que se juega la solución (por defecto la inicial).</summary>
        public int VidaCarrete = -1;
    }

    /// <summary>Una sala diseñada a mano: tablero, oleada, hilo disponible y herramientas que se ofrecen.</summary>
    public sealed class Sala
    {
        public string Id = "";
        public string Nombre = "";
        public string Subtitulo = "";
        public string Objetivo = "";
        public Tablero Tablero;
        public Oleada Oleada;
        public int Hilo = 18;
        public int LongitudMinima = 10;
        /// <summary>Si no está vacía, se ofrecen exactamente estas herramientas (p. ej., en el tutorial).</summary>
        public List<TipoHerramienta> OfertaFija = new List<TipoHerramienta>();
        /// <summary>Herramientas que siempre aparecen entre las tres ofrecidas (presentación de herramientas nuevas).</summary>
        public List<TipoHerramienta> OfertaGarantizada = new List<TipoHerramienta>();
        public int CantidadOfrecida = 3;
        public int CantidadAElegir = 2;
        public List<string> Tutorial = new List<string>();
        public SolucionReferencia Solucion;

        /// <summary>
        /// Lee una sala desde texto. Claves "clave: valor"; "mapa:" debe ir al final, seguido de las filas.
        /// Las líneas que empiezan por # son comentarios (salvo dentro del mapa, donde # es una caja).
        /// </summary>
        public static Sala DesdeTexto(string texto)
        {
            var sala = new Sala();
            var filasMapa = new List<string>();
            bool leyendoMapa = false;
            var lineas = texto.Replace("\r\n", "\n").Split('\n');
            foreach (var lineaBruta in lineas)
            {
                var linea = lineaBruta.TrimEnd();
                if (leyendoMapa)
                {
                    if (linea.Trim().Length == 0) continue;
                    filasMapa.Add(linea);
                    continue;
                }
                var limpia = linea.Trim();
                if (limpia.Length == 0 || limpia.StartsWith("#")) continue;
                int dosPuntos = limpia.IndexOf(':');
                if (dosPuntos < 0) throw new FormatException("Línea no reconocida en la sala: '" + limpia + "'.");
                var clave = limpia.Substring(0, dosPuntos).Trim().ToLowerInvariant();
                var valor = limpia.Substring(dosPuntos + 1).Trim();
                switch (clave)
                {
                    case "id": sala.Id = valor; break;
                    case "nombre": sala.Nombre = valor; break;
                    case "subtitulo": sala.Subtitulo = valor; break;
                    case "objetivo": sala.Objetivo = valor; break;
                    case "hilo": sala.Hilo = int.Parse(valor); break;
                    case "longitud_minima": sala.LongitudMinima = int.Parse(valor); break;
                    case "oferta_fija": sala.OfertaFija = ParsearHerramientas(valor); break;
                    case "oferta_garantizada": sala.OfertaGarantizada = ParsearHerramientas(valor); break;
                    case "ofrecidas": sala.CantidadOfrecida = int.Parse(valor); break;
                    case "elegir": sala.CantidadAElegir = int.Parse(valor); break;
                    case "oleada": sala.Oleada = Oleada.DesdeTexto(valor); break;
                    case "tutorial": sala.Tutorial.Add(valor); break;
                    case "solucion_recorrido": AsegurarSolucion(sala).Movimientos = valor; break;
                    case "solucion_costurero": AsegurarSolucion(sala).Costurero = ParsearHerramientas(valor); break;
                    case "solucion_colocacion": AsegurarSolucion(sala).Colocaciones = ParsearColocaciones(valor); break;
                    case "solucion_mejoras": AsegurarSolucion(sala).Mejoras = ParsearMejoras(valor); break;
                    case "solucion_vida": AsegurarSolucion(sala).VidaCarrete = int.Parse(valor); break;
                    case "mapa": leyendoMapa = true; break;
                    default: throw new FormatException("Clave desconocida en la sala: '" + clave + "'.");
                }
            }
            if (filasMapa.Count == 0) throw new FormatException("La sala '" + sala.Id + "' no tiene mapa.");
            if (sala.Oleada == null) throw new FormatException("La sala '" + sala.Id + "' no tiene oleada.");
            sala.Tablero = Tablero.DesdeTexto(filasMapa);
            return sala;
        }

        private static SolucionReferencia AsegurarSolucion(Sala sala) => sala.Solucion ?? (sala.Solucion = new SolucionReferencia());

        public static List<TipoHerramienta> ParsearHerramientas(string valor)
        {
            var lista = new List<TipoHerramienta>();
            foreach (var trozo in valor.Split(','))
            {
                var t = trozo.Trim();
                if (t.Length == 0) continue;
                lista.Add(ParsearHerramienta(t));
            }
            return lista;
        }

        public static TipoHerramienta ParsearHerramienta(string s)
        {
            switch (s.Trim().ToLowerInvariant())
            {
                case "alfiler": return TipoHerramienta.Alfiler;
                case "velcro": return TipoHerramienta.Velcro;
                case "bobina": return TipoHerramienta.Bobina;
                case "tijeras": return TipoHerramienta.Tijeras;
                case "boton": case "botón": return TipoHerramienta.Boton;
                default: throw new FormatException("Herramienta desconocida: '" + s + "'.");
            }
        }

        public static List<Colocacion> ParsearColocaciones(string valor)
        {
            var lista = new List<Colocacion>();
            foreach (var trozo in valor.Split(','))
            {
                var t = trozo.Trim();
                if (t.Length == 0) continue;
                var partes = t.Split('@');
                if (partes.Length != 2) throw new FormatException("Colocación mal escrita: '" + t + "' (usa Herramienta@indice).");
                lista.Add(new Colocacion(ParsearHerramienta(partes[0]), int.Parse(partes[1].Trim())));
            }
            return lista;
        }

        public static List<TipoMejora> ParsearMejoras(string valor)
        {
            var lista = new List<TipoMejora>();
            foreach (var trozo in valor.Split(','))
            {
                var t = trozo.Trim();
                if (t.Length == 0) continue;
                lista.Add((TipoMejora)Enum.Parse(typeof(TipoMejora), t, true));
            }
            return lista;
        }
    }
}
