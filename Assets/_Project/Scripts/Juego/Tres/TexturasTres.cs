using UnityEngine;

namespace DesenredaElOvillo.Juego.Tres
{
    /// <summary>Par de texturas de un material: color (sRGB) y mapa de normales (lineal) generado desde una altura.</summary>
    public sealed class TexturaMaterial
    {
        public string Nombre;
        public Pintor Color;
        public Pintor Normal;
    }

    /// <summary>
    /// Texturas procedurales de los materiales 3D: fieltro, lana trenzada (torsión helicoidal), madera,
    /// casilla de fieltro con pespunte, tela tejida y velcro. Todas tileables salvo la casilla.
    /// Son grises/claras: el color final lo pone el material o cada objeto, así una textura sirve para todos.
    /// </summary>
    public static class TexturasTres
    {
        // ------------------------------------------------------------------ ruido periódico (tileable)

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

        private static int Envolver(int v, int periodo) => ((v % periodo) + periodo) % periodo;

        /// <summary>Ruido de valor que se repite cada "periodo" unidades en x e y.</summary>
        public static float RuidoPeriodico(float x, float y, int periodo, int semilla)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            float sx = fx * fx * (3 - 2 * fx), sy = fy * fy * (3 - 2 * fy);
            int ax = Envolver(x0, periodo), bx = Envolver(x0 + 1, periodo), ay = Envolver(y0, periodo), by = Envolver(y0 + 1, periodo);
            float a = Hash(ax, ay, semilla), b = Hash(bx, ay, semilla), c = Hash(ax, by, semilla), d = Hash(bx, by, semilla);
            return Mathf.Lerp(Mathf.Lerp(a, b, sx), Mathf.Lerp(c, d, sx), sy);
        }

        /// <summary>Fieltro: varias octavas + fibras cortas en direcciones al azar. u,v en [0,1).</summary>
        private static float AlturaFieltro(float u, float v, int semilla)
        {
            float h = RuidoPeriodico(u * 8, v * 8, 8, semilla) * 0.45f
                      + RuidoPeriodico(u * 24, v * 24, 24, semilla + 1) * 0.3f
                      + RuidoPeriodico(u * 64, v * 64, 64, semilla + 2) * 0.25f;
            float fibraA = RuidoPeriodico(u * 96 + v * 24, v * 12, 96, semilla + 3);
            float fibraB = RuidoPeriodico(u * 10, v * 96 + u * 30, 96, semilla + 4);
            return Mathf.Clamp01(h * 0.8f + Mathf.Max(fibraA, fibraB) * 0.2f);
        }

        private static Pintor NormalDesdeAltura(float[] alto, int w, int h, float fuerza, bool repetir)
        {
            var p = new Pintor(w, h);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float l = Muestra(alto, w, h, x - 1, y, repetir), r = Muestra(alto, w, h, x + 1, y, repetir);
                    float d = Muestra(alto, w, h, x, y - 1, repetir), u = Muestra(alto, w, h, x, y + 1, repetir);
                    var n = new Vector3((l - r) * fuerza, (d - u) * fuerza, 1f).normalized;
                    p.Escribir(x, y, new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f));
                }
            }
            return p;
        }

        private static float Muestra(float[] a, int w, int h, int x, int y, bool repetir)
        {
            if (repetir)
            {
                x = Envolver(x, w);
                y = Envolver(y, h);
            }
            else
            {
                x = Mathf.Clamp(x, 0, w - 1);
                y = Mathf.Clamp(y, 0, h - 1);
            }
            return a[y * w + x];
        }

        private static TexturaMaterial Desde(string nombre, int w, int h, System.Func<float, float, float> altura,
            System.Func<float, float, float, Color> color, float fuerzaNormal, bool repetir)
        {
            var alto = new float[w * h];
            var pc = new Pintor(w, h);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)w, v = y / (float)h;
                    float a = altura(u, v);
                    alto[y * w + x] = a;
                    pc.Escribir(x, y, color(u, v, a));
                }
            }
            return new TexturaMaterial { Nombre = nombre, Color = pc, Normal = NormalDesdeAltura(alto, w, h, fuerzaNormal, repetir) };
        }

        private static Color Gris(float g) => new Color(g, g, g, 1f);

        // ------------------------------------------------------------------ materiales

        public static TexturaMaterial Fieltro() => Desde("fieltro", 256, 256,
            (u, v) => AlturaFieltro(u, v, 11),
            (u, v, a) => Gris(0.84f + (a - 0.5f) * 0.28f), 5f, true);

        /// <summary>
        /// Lana: tres cabos que se enroscan en hélice alrededor del tubo (U = alrededor, V = a lo largo).
        /// Los surcos entre cabos se oscurecen y las fibras sueltas dan la pelusa.
        /// </summary>
        public static TexturaMaterial Lana() => Desde("lana", 128, 128,
            (u, v) =>
            {
                float cabo = 0.5f + 0.5f * Mathf.Sin((u * 3f + v * 2f) * Mathf.PI * 2f);
                float redondo = Mathf.Sqrt(Mathf.Clamp01(cabo));
                float fibra = RuidoPeriodico(u * 64 + v * 16, v * 64, 64, 21) * 0.18f;
                return Mathf.Clamp01(redondo * 0.85f + fibra);
            },
            (u, v, a) => Gris(0.62f + a * 0.38f), 7f, true);

        public static TexturaMaterial Madera() => Desde("madera", 256, 256,
            (u, v) =>
            {
                float vetas = 0.5f + 0.5f * Mathf.Sin((v * 5f + RuidoPeriodico(u * 2, v * 4, 2, 31) * 0.8f) * Mathf.PI * 2f);
                float poro = RuidoPeriodico(u * 32, v * 128, 32, 32);
                float fibra = RuidoPeriodico(u * 4, v * 256, 4, 33);
                return Mathf.Clamp01(vetas * 0.45f + poro * 0.3f + fibra * 0.25f);
            },
            (u, v, a) =>
            {
                var claro = Paleta.Hex("#B98352");
                var oscuro = Paleta.Hex("#9A6538");
                var c = Color.Lerp(oscuro, claro, a);
                return new Color(c.r, c.g, c.b, 1f);
            }, 1.5f, true);

        /// <summary>Casilla del tablero: fieltro con un pespunte discontinuo de hilo claro cerca del borde.</summary>
        public static TexturaMaterial Casilla() => Desde("casilla", 256, 256,
            (u, v) =>
            {
                float f = AlturaFieltro(u, v, 41);
                float borde = Mathf.Min(Mathf.Min(u, 1 - u), Mathf.Min(v, 1 - v));
                float costura = Mathf.Abs(borde - 0.09f) < 0.012f && Mathf.Repeat((u + v) * 22f, 1f) < 0.6f ? 1f : 0f;
                float surco = Mathf.Abs(borde - 0.09f) < 0.02f ? -0.25f : 0f;
                return Mathf.Clamp01(f * 0.8f + costura * 0.6f + surco);
            },
            (u, v, a) =>
            {
                float borde = Mathf.Min(Mathf.Min(u, 1 - u), Mathf.Min(v, 1 - v));
                bool costura = Mathf.Abs(borde - 0.09f) < 0.012f && Mathf.Repeat((u + v) * 22f, 1f) < 0.6f;
                return costura ? new Color(1.25f, 1.22f, 1.1f, 1f) * 0.85f : Gris(0.8f + (AlturaFieltro(u, v, 41) - 0.5f) * 0.3f);
            }, 5f, false);

        /// <summary>Tela tejida (capa de la Reina, retales): trama y urdimbre.</summary>
        public static TexturaMaterial Tela() => Desde("tela", 128, 128,
            (u, v) =>
            {
                float urdimbre = 0.5f + 0.5f * Mathf.Sin(u * 16 * Mathf.PI * 2f);
                float trama = 0.5f + 0.5f * Mathf.Sin(v * 16 * Mathf.PI * 2f);
                bool cruce = (Mathf.FloorToInt(u * 16) + Mathf.FloorToInt(v * 16)) % 2 == 0;
                return cruce ? urdimbre : trama;
            },
            (u, v, a) => Gris(0.72f + a * 0.28f), 6f, true);

        public static TexturaMaterial Velcro() => Desde("velcro", 128, 128,
            (u, v) =>
            {
                float gu = Mathf.Repeat(u * 12f, 1f) - 0.5f, gv = Mathf.Repeat(v * 12f + (Mathf.FloorToInt(u * 12f) % 2) * 0.5f, 1f) - 0.5f;
                float gancho = Mathf.Clamp01(1f - Mathf.Abs(Mathf.Sqrt(gu * gu + gv * gv) - 0.25f) * 10f);
                return Mathf.Clamp01(AlturaFieltro(u, v, 51) * 0.4f + gancho * 0.7f);
            },
            (u, v, a) => Gris(0.75f + a * 0.25f), 6f, true);
    }
}
