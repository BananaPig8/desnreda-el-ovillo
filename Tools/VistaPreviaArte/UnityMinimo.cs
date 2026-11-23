// Imitación mínima de UnityEngine: solo lo que necesitan Pintor, Paleta y Texturas para dibujar fuera del editor.
using System;
using System.Globalization;

namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public float magnitude => MathF.Sqrt(x * x + y * y);
        public float sqrMagnitude => x * x + y * y;
        public Vector2 normalized { get { float m = magnitude; return m > 1e-6f ? new Vector2(x / m, y / m) : new Vector2(0, 0); } }
        public static float Dot(Vector2 a, Vector2 b) => a.x * b.x + a.y * b.y;
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 operator -(Vector2 a) => new Vector2(-a.x, -a.y);
        public static Vector2 operator *(Vector2 a, float k) => new Vector2(a.x * k, a.y * k);
        public static Vector2 operator *(float k, Vector2 a) => new Vector2(a.x * k, a.y * k);
        public static Vector2 operator /(Vector2 a, float k) => new Vector2(a.x / k, a.y / k);
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new Color(1, 1, 1, 1);
        public static Color black => new Color(0, 0, 0, 1);
        public static Color clear => new Color(0, 0, 0, 0);
        public static Color Lerp(Color x, Color y, float t)
        {
            t = Mathf.Clamp01(t);
            return new Color(x.r + (y.r - x.r) * t, x.g + (y.g - x.g) * t, x.b + (y.b - x.b) * t, x.a + (y.a - x.a) * t);
        }
    }

    public static class Mathf
    {
        public const float PI = MathF.PI;
        public const float Deg2Rad = MathF.PI / 180f;
        public static float Clamp01(float v) => v < 0 ? 0 : v > 1 ? 1 : v;
        public static float Clamp(float v, float a, float b) => v < a ? a : v > b ? b : v;
        public static int Clamp(int v, int a, int b) => v < a ? a : v > b ? b : v;
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float SmoothStep(float a, float b, float t) { t = Clamp01(t); t = t * t * (3 - 2 * t); return a + (b - a) * t; }
        public static float Sqrt(float v) => MathF.Sqrt(v);
        public static float Abs(float v) => MathF.Abs(v);
        public static float Min(float a, float b) => MathF.Min(a, b);
        public static float Max(float a, float b) => MathF.Max(a, b);
        public static int Min(int a, int b) => Math.Min(a, b);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Sin(float v) => MathF.Sin(v);
        public static float Cos(float v) => MathF.Cos(v);
        public static float Atan2(float y, float x) => MathF.Atan2(y, x);
        public static float Pow(float a, float b) => MathF.Pow(a, b);
        public static float Exp(float v) => MathF.Exp(v);
        public static int FloorToInt(float v) => (int)MathF.Floor(v);
        public static int CeilToInt(float v) => (int)MathF.Ceiling(v);
        public static int RoundToInt(float v) => (int)MathF.Round(v);
        public static float Repeat(float t, float l) => Clamp(t - MathF.Floor(t / l) * l, 0, l);
    }

    public struct RectInt
    {
        public int xMin, yMin, xMax, yMax;
        public RectInt(int x, int y, int w, int h) { xMin = x; yMin = y; xMax = x + w; yMax = y + h; }
    }

    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float w, float h) { this.x = x; this.y = y; width = w; height = h; }
    }

    public enum TextureFormat { RGBA32 }
    public enum TextureWrapMode { Repeat, Clamp }
    public enum FilterMode { Point, Bilinear }
    public enum SpriteMeshType { FullRect, Tight }

    public class Object { public string name; }

    public class Texture2D : Object
    {
        public readonly int width, height;
        public Color[] Pixeles;
        public TextureWrapMode wrapMode;
        public FilterMode filterMode;
        public Texture2D(int w, int h, TextureFormat f, bool mip) { width = w; height = h; Pixeles = new Color[w * h]; }
        public void SetPixels(Color[] px) => Array.Copy(px, Pixeles, px.Length);
        public void SetPixel(int x, int y, Color c) => Pixeles[y * width + x] = c;
        public void Apply(bool a = true, bool b = false) { }
        public void Apply() { }
    }

    public class Sprite : Object
    {
        public Texture2D texture;
        public float PixelesPorUnidad;
        public Vector2 Pivote;
        public static Sprite Create(Texture2D t, Rect r, Vector2 pivote, float ppu, uint extrude, SpriteMeshType m)
            => new Sprite { texture = t, PixelesPorUnidad = ppu, Pivote = pivote };
    }

    public class Shader : Object { public static Shader Find(string nombre) => null; }
    public class Material : Object { public Material(Shader s) { } }
    public class SpriteRenderer : Object { public Material sharedMaterial; }

    public static class Resources
    {
        public static T Load<T>(string ruta) where T : class => null;
    }

    public static class ColorUtility
    {
        public static bool TryParseHtmlString(string s, out Color c)
        {
            s = s.TrimStart('#');
            int v = int.Parse(s.Substring(0, 6), NumberStyles.HexNumber);
            c = new Color(((v >> 16) & 255) / 255f, ((v >> 8) & 255) / 255f, (v & 255) / 255f, 1f);
            return true;
        }
    }
}
