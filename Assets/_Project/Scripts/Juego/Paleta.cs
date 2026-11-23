using UnityEngine;

namespace DesenredaElOvillo.Juego
{
    /// <summary>
    /// Paleta oficial (Docs/05_Direccion_de_arte.md). Los colores de Ovillo se han medido sobre la imagen de referencia.
    /// Regla de accesibilidad: la lana y el tablero se separan por luminosidad, no solo por tono (rojo/verde es la
    /// combinación más difícil para el daltonismo). Por eso la lana por defecto es crema sobre fieltro musgo.
    /// </summary>
    public static class Paleta
    {
        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        // Ovillo (medidos en la referencia)
        public static readonly Color LanaOvillo = Hex("#F7C187");
        public static readonly Color LanaOvilloLuz = Hex("#FBE0B5");
        public static readonly Color LanaOvilloSombra = Hex("#D58249");
        public static readonly Color Bufanda = Hex("#F24432");
        public static readonly Color BufandaSombra = Hex("#A71108");
        public static readonly Color Botas = Hex("#7A3416");
        public static readonly Color Mejillas = Hex("#FB7F72");
        public static readonly Color Ojos = Hex("#3A140C");
        public static readonly Color Puntada = Hex("#D1854A");

        // Paleta del juego
        public static readonly Color Crema = Hex("#F6E3C1");
        public static readonly Color Beige = Hex("#E3C9A0");
        public static readonly Color MarronCalido = Hex("#8A4B26");
        public static readonly Color Madera = Hex("#B07A45");
        public static readonly Color MaderaOscura = Hex("#6E4426");
        public static readonly Color Coral = Hex("#EF5B45");
        public static readonly Color RosaPastel = Hex("#F2A7B5");
        public static readonly Color Morado = Hex("#7D4F9E");
        public static readonly Color MoradoOscuro = Hex("#4E2D6B");
        public static readonly Color Musgo = Hex("#5E7A36");
        public static readonly Color MusgoClaro = Hex("#7A9849");
        public static readonly Color MusgoOscuro = Hex("#3F5525");
        public static readonly Color AzulSuave = Hex("#8DB2D6");
        public static readonly Color Mostaza = Hex("#E2A631");
        public static readonly Color GrisFieltro = Hex("#8F8A84");
        public static readonly Color Dorado = Hex("#E8BE4A");
        public static readonly Color Tinta = Hex("#3A2015");
        public static readonly Color Plata = Hex("#C9CDD3");

        // Lana del recorrido (opción de accesibilidad en Ajustes)
        public static readonly Color[] ColoresLana = { Hex("#F4DDB3"), Hex("#EF6A55"), Hex("#9CC3E6") };
        public static readonly string[] NombresColoresLana = { "Crema (recomendado)", "Coral", "Azul" };

        public static Color ConAlfa(Color c, float a) => new Color(c.r, c.g, c.b, a);
        public static Color Aclarar(Color c, float t) => Color.Lerp(c, Color.white, t);
        public static Color Oscurecer(Color c, float t) => Color.Lerp(c, Color.black, t);
    }
}
