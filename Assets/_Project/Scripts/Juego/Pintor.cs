using System;
using UnityEngine;

namespace DesenredaElOvillo.Juego
{
    /// <summary>
    /// Pequeño rasterizador de formas por campos de distancia (SDF) con antialiasing.
    /// Se usa para generar todo el arte provisional del prototipo (fieltro, lana, herramientas, polillas)
    /// sin depender de archivos de imagen. Cuando llegue el arte final, Texturas busca primero en Resources.
    /// Coordenadas en píxeles, origen abajo a la izquierda.
    /// </summary>
    public sealed class Pintor
    {
        public readonly int Ancho;
        public readonly int Alto;
        private readonly Color[] _px;

        public Pintor(int ancho, int alto)
        {
            Ancho = ancho;
            Alto = alto;
            _px = new Color[ancho * alto];
        }

        // ------------------------------------------------------------------ ruido

        private static float Hash(int x, int y, int semilla)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + semilla * 982451653);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777215f;
            }
        }

        public static float Ruido(float x, float y, int semilla)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            float sx = fx * fx * (3 - 2 * fx), sy = fy * fy * (3 - 2 * fy);
            float a = Hash(x0, y0, semilla), b = Hash(x0 + 1, y0, semilla);
            float c = Hash(x0, y0 + 1, semilla), d = Hash(x0 + 1, y0 + 1, semilla);
            return Mathf.Lerp(Mathf.Lerp(a, b, sx), Mathf.Lerp(c, d, sx), sy);
        }

        /// <summary>Ruido de fieltro: varias octavas más fibras finas alargadas.</summary>
        public static float Fieltro(float x, float y, int semilla)
        {
            float n = Ruido(x * 0.08f, y * 0.08f, semilla) * 0.5f
                      + Ruido(x * 0.2f, y * 0.2f, semilla + 1) * 0.3f
                      + Ruido(x * 0.55f, y * 0.55f, semilla + 2) * 0.2f;
            float fibra = Ruido(x * 0.9f + y * 0.15f, y * 0.12f, semilla + 3);
            return Mathf.Clamp01(n * 0.85f + fibra * 0.15f);
        }

        // ------------------------------------------------------------------ SDF

        public static float Circulo(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;

        public static float Elipse(Vector2 p, Vector2 c, float rx, float ry, float anguloGrados = 0)
        {
            var q = Rotar(p - c, -anguloGrados);
            float k = new Vector2(q.x / rx, q.y / ry).magnitude;
            return (k - 1f) * Mathf.Min(rx, ry);
        }

        public static float Caja(Vector2 p, Vector2 c, float semiAncho, float semiAlto, float radio, float anguloGrados = 0)
        {
            var q = Rotar(p - c, -anguloGrados);
            var d = new Vector2(Mathf.Abs(q.x) - semiAncho + radio, Mathf.Abs(q.y) - semiAlto + radio);
            float fuera = new Vector2(Mathf.Max(d.x, 0), Mathf.Max(d.y, 0)).magnitude;
            return fuera + Mathf.Min(Mathf.Max(d.x, d.y), 0) - radio;
        }

        public static float Segmento(Vector2 p, Vector2 a, Vector2 b, float grosor)
        {
            var pa = p - a;
            var ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Mathf.Max(0.0001f, ba.sqrMagnitude));
            return (pa - ba * h).magnitude - grosor * 0.5f;
        }

        public static float Anillo(Vector2 p, Vector2 c, float r, float grosor) => Mathf.Abs((p - c).magnitude - r) - grosor * 0.5f;

        public static Vector2 Rotar(Vector2 v, float grados)
        {
            float a = grados * Mathf.Deg2Rad;
            float cs = Mathf.Cos(a), sn = Mathf.Sin(a);
            return new Vector2(v.x * cs - v.y * sn, v.x * sn + v.y * cs);
        }

        // ------------------------------------------------------------------ pintar

        /// <summary>
        /// Rellena una forma. "sombreado" devuelve un factor de brillo por píxel (1 = color tal cual)
        /// para simular volumen; "ruido" añade textura de fieltro.
        /// </summary>
        public void Forma(Func<Vector2, float> sdf, Color color, float ruido = 0f, int semilla = 0,
            Func<Vector2, float> sombreado = null, RectInt? limites = null, float suavizado = 1f)
        {
            var r = limites ?? new RectInt(0, 0, Ancho, Alto);
            int x0 = Mathf.Max(0, r.xMin), x1 = Mathf.Min(Ancho, r.xMax);
            int y0 = Mathf.Max(0, r.yMin), y1 = Mathf.Min(Alto, r.yMax);
            for (int y = y0; y < y1; y++)
            {
                for (int x = x0; x < x1; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = sdf(p);
                    float a = Mathf.Clamp01(0.5f - d / suavizado);
                    if (a <= 0f) continue;
                    var c = color;
                    float brillo = 1f;
                    if (ruido > 0f) brillo *= 1f + (Fieltro(x, y, semilla) - 0.5f) * ruido;
                    if (sombreado != null) brillo *= sombreado(p);
                    c = new Color(Mathf.Clamp01(c.r * brillo), Mathf.Clamp01(c.g * brillo), Mathf.Clamp01(c.b * brillo), c.a * a);
                    Mezclar(x, y, c);
                }
            }
        }

        /// <summary>Puntada discontinua a lo largo de una forma cerrada (anillo o borde de caja).</summary>
        public void Puntadas(Func<Vector2, float> sdfLinea, Func<Vector2, float> parametro, float largo, float hueco, Color color)
        {
            Forma(p =>
            {
                float t = parametro(p);
                float fase = Mathf.Repeat(t, largo + hueco);
                return fase < largo ? sdfLinea(p) : 10f;
            }, color);
        }

        public void Mezclar(int x, int y, Color c)
        {
            int i = y * Ancho + x;
            var b = _px[i];
            float a = c.a + b.a * (1f - c.a);
            if (a <= 0f)
            {
                _px[i] = new Color(0, 0, 0, 0);
                return;
            }
            _px[i] = new Color(
                (c.r * c.a + b.r * b.a * (1f - c.a)) / a,
                (c.g * c.a + b.g * b.a * (1f - c.a)) / a,
                (c.b * c.a + b.b * b.a * (1f - c.a)) / a,
                a);
        }

        public void Sombra(Func<Vector2, float> sdf, float desenfoque, float opacidad)
        {
            Forma(p =>
            {
                float d = sdf(p);
                return d;
            }, new Color(0, 0, 0, opacidad), 0, 0, null, null, desenfoque);
        }

        public Color Leer(int x, int y) => _px[y * Ancho + x];
        public void Escribir(int x, int y, Color c) => _px[y * Ancho + x] = c;

        public Texture2D ATextura(bool repetir = false, FilterMode filtro = FilterMode.Bilinear)
        {
            var tex = new Texture2D(Ancho, Alto, TextureFormat.RGBA32, false)
            {
                wrapMode = repetir ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
                filterMode = filtro,
                name = "Procedural"
            };
            tex.SetPixels(_px);
            tex.Apply(false, false);
            return tex;
        }
    }
}
