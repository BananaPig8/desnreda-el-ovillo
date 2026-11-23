using System.Collections.Generic;
using DesenredaElOvillo.Nucleo;
using UnityEngine;

namespace DesenredaElOvillo.Juego
{
    /// <summary>
    /// Iconos 2D de la interfaz (herramientas, carrete, lana, retratos, paneles de fieltro). Primero busca arte
    /// final en Resources/Arte/... con el mismo nombre; si no existe, lo genera con el Pintor.
    /// El mundo del juego es 3D (carpeta Tres/); esto solo dibuja la interfaz.
    /// </summary>
    public static class Iconos
    {
        public const int PixelesPorUnidad = 128;
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Texture2D> CacheTex = new Dictionary<string, Texture2D>();

        public static void Vaciar()
        {
            Cache.Clear();
            CacheTex.Clear();
        }

        private static Sprite Obtener(string clave, System.Func<Pintor> dibujar, float ppu = PixelesPorUnidad, Vector2? pivote = null)
        {
            if (Cache.TryGetValue(clave, out var s) && s != null) return s;
            var final = Resources.Load<Texture2D>("Arte/" + clave);
            Texture2D tex = final != null ? final : dibujar().ATextura();
            if (final == null) tex.name = clave;
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivote ?? new Vector2(0.5f, 0.5f),
                final != null ? tex.width : ppu, 0, SpriteMeshType.FullRect);
            s.name = clave;
            Cache[clave] = s;
            return s;
        }

        public static Texture2D TexturaDe(Sprite s) => s != null ? s.texture : null;

        // ================================================================== Personajes

        /// <summary>Ovillo (imagen de referencia del proyecto). Pivote en los pies.</summary>
        public static Sprite Ovillo()
        {
            const string clave = "Personajes/ovillo";
            if (Cache.TryGetValue(clave, out var s) && s != null) return s;
            var tex = Resources.Load<Texture2D>("Arte/" + clave);
            if (tex == null)
            {
                var p = new Pintor(128, 128);
                p.Forma(q => Pintor.Circulo(q, new Vector2(64, 66), 50), Paleta.LanaOvillo, 0.35f, 7);
                p.Forma(q => Pintor.Caja(q, new Vector2(64, 50), 52, 9, 8), Paleta.Bufanda, 0.3f, 8);
                p.Forma(q => Pintor.Elipse(q, new Vector2(80, 78), 6, 9), Paleta.Ojos);
                p.Forma(q => Pintor.Elipse(q, new Vector2(100, 78), 6, 9), Paleta.Ojos);
                tex = p.ATextura();
            }
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.04f), tex.height / 1.35f, 0, SpriteMeshType.FullRect);
            s.name = clave;
            Cache[clave] = s;
            return s;
        }

        /// <summary>Retrato recortado de la hoja de personajes (Resources/Arte/Retratos), para las fichas de la interfaz.</summary>
        public static Texture2D Retrato(string nombre)
        {
            string clave = "Retratos/" + nombre;
            if (CacheTex.TryGetValue(clave, out var t)) return t;
            t = Resources.Load<Texture2D>("Arte/" + clave);
            CacheTex[clave] = t;
            return t;
        }

        public static Texture2D Retrato(TipoPolilla tipo)
        {
            switch (tipo)
            {
                case TipoPolilla.Veloz: return Retrato("veloz");
                case TipoPolilla.Acorazada: return Retrato("acorazada");
                case TipoPolilla.Reina: return Retrato("reina");
                default: return Retrato("comun");
            }
        }

        // ================================================================== Tablero

        private static void DibujarCarrete(Pintor p, Vector2 c, Color hilo, float escala, float cantidad)
        {
            float w = 34 * escala, h = 42 * escala;
            p.Sombra(q => Pintor.Caja(q, c + new Vector2(5, -7), w + 8, h + 4, 8), 6, 0.35f);
            p.Forma(q => Pintor.Caja(q, c, w * 0.55f, h, 4), Paleta.Beige, 0.2f, 70);
            float anchoHilo = w * (0.55f + 0.45f * Mathf.Clamp01(cantidad));
            if (cantidad > 0.01f)
            {
                p.Forma(q => Pintor.Caja(q, c, anchoHilo, h - 8 * escala, 8), hilo, 0.25f, 71,
                    q => 0.8f + 0.3f * Mathf.Clamp01(1f - Mathf.Abs(q.x - (c.x - anchoHilo * 0.3f)) / (anchoHilo * 1.4f)));
                for (float y = -h + 12 * escala; y < h - 10 * escala; y += 7 * escala)
                {
                    float yy = y;
                    p.Forma(q => Mathf.Max(Pintor.Segmento(q, c + new Vector2(-anchoHilo, yy), c + new Vector2(anchoHilo, yy + 3 * escala), 1.6f),
                        Pintor.Caja(q, c, anchoHilo, h - 8 * escala, 8)), Paleta.ConAlfa(Paleta.Oscurecer(hilo, 0.25f), 0.7f));
                }
            }
            p.Forma(q => Pintor.Caja(q, c + new Vector2(0, h), w + 8, 7 * escala, 5), Paleta.Madera, 0.2f, 72);
            p.Forma(q => Pintor.Caja(q, c + new Vector2(0, -h), w + 8, 7 * escala, 5), Paleta.Madera, 0.2f, 73);
        }

        /// <summary>El carrete de la salida: el hilo que le queda refleja la vida (diegético).</summary>
        public static Sprite Carrete(int vida, int vidaMaxima) => Obtener("Tablero/carrete_" + vida + "_" + vidaMaxima, () =>
        {
            var p = new Pintor(160, 160);
            DibujarCarrete(p, new Vector2(80, 78), Paleta.Bufanda, 1.5f, vidaMaxima <= 0 ? 0 : (float)vida / vidaMaxima);
            return p;
        }, 128);

        // ================================================================== Lana

        public const int Derecha = 1, Arriba = 2, Izquierda = 4, Abajo = 8;

        /// <summary>
        /// Pieza de lana gruesa para una casilla según sus conexiones (bits Derecha/Arriba/Izquierda/Abajo).
        /// Se pinta en gris claro con volumen (sombreado cilíndrico), torsión de las hebras y pelusa;
        /// el color final lo pone el SpriteRenderer, así el jugador puede elegir el color de la lana.
        /// </summary>
        public static Sprite PiezaLana(int mascara) => Obtener("Lana/pieza_" + mascara, () =>
        {
            const int n = 128;
            var p = new Pintor(n, n);
            float medio = n / 2f, semi = n * 0.17f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    var q = new Vector2(x + 0.5f, y + 0.5f);
                    if (!Parametros(mascara, q, n, medio, out float t, out float s)) continue;
                    float borde = semi + (Pintor.Ruido(x * 0.35f, y * 0.35f, 5) - 0.5f) * 3.5f;
                    float a = Mathf.Clamp01((borde - Mathf.Abs(t)) / 1.6f);
                    if (a <= 0) continue;
                    float k = Mathf.Clamp01(Mathf.Abs(t) / semi);
                    float cilindro = 0.55f + 0.45f * Mathf.Sqrt(1f - k * k);
                    float luz = Mathf.Clamp01(1f - Mathf.Abs(t / semi + 0.35f)) * 0.12f;
                    float hebra = 0.5f + 0.5f * Mathf.Sin((s / 21f + t / 26f) * Mathf.PI * 2f);
                    float surco = 0.8f + 0.2f * Mathf.SmoothStep(0f, 1f, hebra);
                    float fibra = 0.93f + 0.14f * Pintor.Ruido(s * 0.9f, t * 0.9f, 6);
                    float b = Mathf.Clamp01(cilindro * surco * fibra + luz);
                    p.Escribir(x, y, new Color(b, b, b, a));
                }
            }
            return p;
        });

        private static bool Parametros(int m, Vector2 q, int n, float medio, out float t, out float s)
        {
            t = 0;
            s = 0;
            bool d = (m & Derecha) != 0, a = (m & Arriba) != 0, i = (m & Izquierda) != 0, b = (m & Abajo) != 0;
            int cuenta = (d ? 1 : 0) + (a ? 1 : 0) + (i ? 1 : 0) + (b ? 1 : 0);
            if (cuenta == 2 && d && i) { t = q.y - medio; s = q.x; return true; }
            if (cuenta == 2 && a && b) { t = q.x - medio; s = q.y; return true; }
            if (cuenta == 2)
            {
                var centro = new Vector2(d ? n : 0, a ? n : 0);
                var v = q - centro;
                t = v.magnitude - medio;
                s = Mathf.Atan2(Mathf.Abs(v.y), Mathf.Abs(v.x)) * medio;
                return true;
            }
            if (cuenta == 1)
            {
                Vector2 borde = d ? new Vector2(n, medio) : a ? new Vector2(medio, n) : i ? new Vector2(0, medio) : new Vector2(medio, 0);
                var centro = new Vector2(medio, medio);
                var eje = (borde - centro).normalized;
                var v = q - centro;
                float along = Vector2.Dot(v, eje);
                float perp = Vector2.Dot(v, new Vector2(-eje.y, eje.x));
                if (along >= 0) { t = perp; s = along; }
                else { t = v.magnitude * (perp >= 0 ? 1 : -1); s = along; }
                return true;
            }
            return false;
        }

        // ================================================================== Herramientas

        public static Sprite Herramienta(TipoHerramienta tipo, bool doblada = false) =>
            Obtener("Herramientas/" + tipo + (doblada ? "_doblada" : ""), () => DibujarHerramienta(tipo, doblada));

        private static Pintor DibujarHerramienta(TipoHerramienta tipo, bool doblada)
        {
            var p = new Pintor(128, 128);
            var c = new Vector2(64, 64);
            switch (tipo)
            {
                case TipoHerramienta.Alfiler:
                {
                    var a = new Vector2(38, 26);
                    var b = doblada ? new Vector2(70, 70) : new Vector2(88, 96);
                    var b2 = doblada ? new Vector2(98, 78) : b;
                    p.Sombra(q => Pintor.Segmento(q, a + new Vector2(5, -5), b + new Vector2(5, -5), 7), 4, 0.35f);
                    p.Forma(q => Pintor.Segmento(q, a, b, 6), Paleta.Plata, 0, 0, q => 0.8f + 0.3f * Mathf.Clamp01((q.x - q.y + 60) / 60f));
                    if (doblada) p.Forma(q => Pintor.Segmento(q, b, b2, 6), Paleta.Plata);
                    var cabeza = doblada ? b2 : b;
                    p.Sombra(q => Pintor.Circulo(q, cabeza + new Vector2(4, -5), 15), 5, 0.35f);
                    p.Forma(q => Pintor.Circulo(q, cabeza, 15), doblada ? Paleta.GrisFieltro : Paleta.Coral, 0.15f, 101,
                        q => 0.75f + 0.45f * Mathf.Clamp01(1f - (q - (cabeza + new Vector2(-5, 6))).magnitude / 22f));
                    p.Forma(q => Pintor.Circulo(q, cabeza + new Vector2(-5, 6), 4), Paleta.ConAlfa(Color.white, 0.8f));
                    break;
                }
                case TipoHerramienta.Velcro:
                {
                    var col = Paleta.Hex("#6C8FB5");
                    p.Forma(q => Pintor.Caja(q, c, 54, 38, 10), col, 0.3f, 102);
                    for (int y = 0; y < 6; y++)
                        for (int x = 0; x < 9; x++)
                        {
                            var h = c + new Vector2(-44 + x * 11 + (y % 2) * 5, -27 + y * 11);
                            p.Forma(q => Pintor.Anillo(q, h, 3.2f, 1.6f), Paleta.Oscurecer(col, 0.3f));
                        }
                    p.Puntadas(q => Mathf.Abs(Pintor.Caja(q, c, 47, 31, 7)) - 1.3f, q => q.x + q.y, 6, 5, Paleta.Crema);
                    break;
                }
                case TipoHerramienta.Bobina:
                {
                    p.Sombra(q => Pintor.Caja(q, c + new Vector2(5, -7), 40, 46, 10), 6, 0.35f);
                    p.Forma(q => Pintor.Caja(q, c, 22, 44, 4), Paleta.Beige, 0.2f, 103);
                    p.Forma(q => Pintor.Caja(q, c, 34, 34, 10), Paleta.Mostaza, 0.3f, 104,
                        q => 0.8f + 0.3f * Mathf.Clamp01(1f - Mathf.Abs(q.x - 54f) / 50f));
                    for (int k = -4; k <= 4; k++)
                    {
                        float yy = k * 7f;
                        p.Forma(q => Mathf.Max(Pintor.Segmento(q, c + new Vector2(-34, yy), c + new Vector2(34, yy + 3), 1.8f), Pintor.Caja(q, c, 34, 34, 10)),
                            Paleta.ConAlfa(Paleta.Oscurecer(Paleta.Mostaza, 0.3f), 0.7f));
                    }
                    p.Forma(q => Pintor.Caja(q, c + new Vector2(0, 40), 42, 7, 5), Paleta.Madera, 0.2f, 105);
                    p.Forma(q => Pintor.Caja(q, c + new Vector2(0, -40), 42, 7, 5), Paleta.Madera, 0.2f, 106);
                    p.Forma(q => Pintor.Segmento(q, c + new Vector2(30, 10), c + new Vector2(56, -30), 3), Paleta.Mostaza);
                    break;
                }
                case TipoHerramienta.Tijeras:
                {
                    p.Sombra(q => Pintor.Caja(q, c + new Vector2(5, 0), 14, 50, 6, 20), 6, 0.3f);
                    p.Forma(q => Pintor.Caja(q, c + new Vector2(-8, 22), 8, 40, 6, -16), Paleta.Plata, 0.1f, 107, q => 0.85f + 0.25f * Mathf.Clamp01((q.x - 40) / 40f));
                    p.Forma(q => Pintor.Caja(q, c + new Vector2(8, 22), 8, 40, 6, 16), Paleta.Hex("#B5BAC2"), 0.1f, 108);
                    p.Forma(q => Pintor.Anillo(q, c + new Vector2(-20, -30), 15, 8), Paleta.Coral, 0.2f, 109);
                    p.Forma(q => Pintor.Anillo(q, c + new Vector2(20, -30), 15, 8), Paleta.Coral, 0.2f, 110);
                    p.Forma(q => Pintor.Circulo(q, c + new Vector2(0, -4), 5), Paleta.Tinta);
                    break;
                }
                case TipoHerramienta.Boton:
                {
                    for (int k = 0; k < 4; k++)
                    {
                        float y0 = 20 + k * 7;
                        p.Forma(q => Pintor.Segmento(q, new Vector2(46, y0), new Vector2(82, y0 + 4), 3), Paleta.Plata);
                    }
                    var cc = c + new Vector2(0, 14);
                    p.Sombra(q => Pintor.Circulo(q, cc + new Vector2(5, -6), 42), 6, 0.35f);
                    p.Forma(q => Pintor.Circulo(q, cc, 42), Paleta.Mostaza, 0.25f, 111, q => 0.82f + 0.3f * Mathf.Clamp01((q.y - 40) / 80f));
                    p.Forma(q => Pintor.Anillo(q, cc, 32, 5), Paleta.Oscurecer(Paleta.Mostaza, 0.2f));
                    for (int i = 0; i < 4; i++)
                    {
                        var h = cc + Pintor.Rotar(new Vector2(11, 0), 45 + 90 * i);
                        p.Forma(q => Pintor.Circulo(q, h, 6), Paleta.Oscurecer(Paleta.Mostaza, 0.5f));
                    }
                    break;
                }
            }
            return p;
        }

        // ================================================================== Interfaz

        public static Texture2D Panel(Color fondo, Color puntada, string clave, int tam = 96, float radio = 22f)
        {
            if (CacheTex.TryGetValue(clave, out var t) && t != null) return t;
            var p = new Pintor(tam, tam);
            float m = tam / 2f;
            p.Sombra(q => Pintor.Caja(q, new Vector2(m + 2, m - 3), m - 4, m - 4, radio), 4, 0.35f);
            p.Forma(q => Pintor.Caja(q, new Vector2(m, m), m - 4, m - 4, radio), fondo, 0.3f, clave.GetHashCode() & 0xFF,
                q => 0.9f + 0.12f * Mathf.Clamp01(q.y / tam));
            p.Puntadas(q => Mathf.Abs(Pintor.Caja(q, new Vector2(m, m), m - 12, m - 12, radio - 8)) - 1.2f, q => q.x + q.y, 6, 5, puntada);
            t = p.ATextura();
            t.name = clave;
            CacheTex[clave] = t;
            return t;
        }

        public static Texture2D Plano(Color color, string clave)
        {
            if (CacheTex.TryGetValue(clave, out var t) && t != null) return t;
            t = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            t.SetPixel(0, 0, color);
            t.Apply();
            CacheTex[clave] = t;
            return t;
        }
    }
}
