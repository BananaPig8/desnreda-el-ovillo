using System;
using System.Collections.Generic;
using UnityEngine;

namespace DesenredaElOvillo.Juego.Tres
{
    /// <summary>Geometría pura (sin UnityEngine.Mesh) para poder generarla y verificarla también fuera de Unity.</summary>
    public sealed class DatosMalla
    {
        public readonly List<Vector3> Vertices = new List<Vector3>();
        public readonly List<Vector3> Normales = new List<Vector3>();
        public readonly List<Vector2> Uvs = new List<Vector2>();
        public readonly List<int> Triangulos = new List<int>();

        public int Agregar(Vector3 v, Vector3 n, Vector2 uv)
        {
            Vertices.Add(v);
            Normales.Add(n);
            Uvs.Add(uv);
            return Vertices.Count - 1;
        }

        /// <summary>
        /// Añade un triángulo orientado hacia "fuera" (la normal indicada). En Unity un triángulo es frontal cuando
        /// Cross(b - a, c - a) apunta hacia quien lo mira; si no coincide con la normal deseada, se invierte.
        /// </summary>
        public void Tri(int a, int b, int c, Vector3 fuera)
        {
            var n = Vector3.Cross(Vertices[b] - Vertices[a], Vertices[c] - Vertices[a]);
            if (Vector3.Dot(n, fuera) < 0) { int t = b; b = c; c = t; }
            Triangulos.Add(a);
            Triangulos.Add(b);
            Triangulos.Add(c);
        }

        public void Quad(int a, int b, int c, int d, Vector3 fuera)
        {
            Tri(a, b, c, fuera);
            Tri(a, c, d, fuera);
        }
    }

    /// <summary>
    /// Mallas procedurales del prototipo: formas suaves y redondeadas (aspecto de juguete de fieltro),
    /// con normales y UV pensadas para texturas tileables de lana, fieltro y madera.
    /// </summary>
    public static class Mallas
    {
        /// <summary>Superficie paramétrica (u, v en [0,1]) con orientación automática de triángulos.</summary>
        public static DatosMalla Superficie(int nu, int nv, Func<float, float, Vector3> posicion, Func<float, float, Vector3> normal,
            Func<float, float, Vector2> uv = null, DatosMalla m = null)
        {
            m = m ?? new DatosMalla();
            int baseIndice = m.Vertices.Count;
            for (int j = 0; j <= nv; j++)
            {
                for (int i = 0; i <= nu; i++)
                {
                    float u = i / (float)nu, v = j / (float)nv;
                    m.Agregar(posicion(u, v), normal(u, v), uv != null ? uv(u, v) : new Vector2(u, v));
                }
            }
            for (int j = 0; j < nv; j++)
            {
                for (int i = 0; i < nu; i++)
                {
                    int a = baseIndice + j * (nu + 1) + i;
                    int b = a + 1, d = a + nu + 1, c = d + 1;
                    var fuera = m.Normales[a] + m.Normales[b] + m.Normales[c] + m.Normales[d];
                    if (fuera.sqrMagnitude < 1e-8f) fuera = m.Normales[a];
                    m.Quad(a, b, c, d, fuera);
                }
            }
            return m;
        }

        public static DatosMalla Esfera(float radio = 0.5f, int segmentos = 20)
        {
            return Superficie(segmentos * 2, segmentos,
                (u, v) => PuntoEsfera(u, v) * radio,
                (u, v) => PuntoEsfera(u, v),
                (u, v) => new Vector2(u * 2f, v));
        }

        private static Vector3 PuntoEsfera(float u, float v)
        {
            float lon = u * Mathf.PI * 2f, lat = (v - 0.5f) * Mathf.PI;
            return new Vector3(Mathf.Cos(lat) * Mathf.Cos(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Sin(lon));
        }

        public static DatosMalla Toro(float radioMayor, float radioMenor, int segmentosMayor = 28, int segmentosMenor = 12)
        {
            return Superficie(segmentosMayor, segmentosMenor,
                (u, v) =>
                {
                    float a = u * Mathf.PI * 2f, b = v * Mathf.PI * 2f;
                    return new Vector3((radioMayor + radioMenor * Mathf.Cos(b)) * Mathf.Cos(a), radioMenor * Mathf.Sin(b), (radioMayor + radioMenor * Mathf.Cos(b)) * Mathf.Sin(a));
                },
                (u, v) =>
                {
                    float a = u * Mathf.PI * 2f, b = v * Mathf.PI * 2f;
                    return new Vector3(Mathf.Cos(b) * Mathf.Cos(a), Mathf.Sin(b), Mathf.Cos(b) * Mathf.Sin(a));
                },
                (u, v) => new Vector2(u * 6f, v));
        }

        /// <summary>Cilindro (o cono, o tronco) con tapas. Centrado en el origen, eje Y.</summary>
        public static DatosMalla Cilindro(float radioAbajo, float radioArriba, float alto, int segmentos = 24, bool tapas = true)
        {
            var m = new DatosMalla();
            float h = alto / 2f;
            float pendiente = (radioAbajo - radioArriba) / Mathf.Max(0.0001f, alto);
            Superficie(segmentos, 1, (u, v) =>
                {
                    float a = u * Mathf.PI * 2f, r = Mathf.Lerp(radioAbajo, radioArriba, v);
                    return new Vector3(Mathf.Cos(a) * r, Mathf.Lerp(-h, h, v), Mathf.Sin(a) * r);
                },
                (u, v) =>
                {
                    float a = u * Mathf.PI * 2f;
                    return new Vector3(Mathf.Cos(a), pendiente, Mathf.Sin(a)).normalized;
                },
                (u, v) => new Vector2(u * 3f, v * alto * 2f), m);
            if (tapas)
            {
                Tapa(m, radioArriba, h, Vector3.up, segmentos);
                Tapa(m, radioAbajo, -h, Vector3.down, segmentos);
            }
            return m;
        }

        private static void Tapa(DatosMalla m, float radio, float y, Vector3 normal, int segmentos)
        {
            if (radio <= 0.0001f) return;
            int centro = m.Agregar(new Vector3(0, y, 0), normal, new Vector2(0.5f, 0.5f));
            int primero = m.Vertices.Count;
            for (int i = 0; i <= segmentos; i++)
            {
                float a = i / (float)segmentos * Mathf.PI * 2f;
                m.Agregar(new Vector3(Mathf.Cos(a) * radio, y, Mathf.Sin(a) * radio), normal, new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f));
            }
            for (int i = 0; i < segmentos; i++) m.Tri(centro, primero + i, primero + i + 1, normal);
        }

        /// <summary>
        /// Capa de pelusa: la malla desplazada a lo largo de sus normales. En las esferas las UV se rehacen con una
        /// proyección octaédrica de la normal (sin polos pellizcados e igual en todas las capas, así cada fibra
        /// atraviesa varias capas y forma un mechón) y se descartan los triángulos que cruzan un pliegue de la
        /// proyección (solo hay en la mitad de abajo, que no se ve). En tubos y cajas se usan sus propias UV
        /// escaladas ("escalaUvPropia"). Si "despejar" tiene valor, se quitan los triángulos que miran hacia esa
        /// dirección: así la pelusa no tapa la cara.
        /// </summary>
        public static DatosMalla Capa(DatosMalla origen, float desplazamiento, float repeticionUv, Vector3? despejar = null, float cosenoDespejado = 0.7f, Vector2? escalaUvPropia = null)
        {
            var m = new DatosMalla();
            var uvBase = new Vector2[origen.Vertices.Count];
            for (int i = 0; i < origen.Vertices.Count; i++)
            {
                var n = origen.Normales[i];
                Vector2 uv;
                if (escalaUvPropia.HasValue)
                {
                    uvBase[i] = Vector2.zero;
                    uv = new Vector2(origen.Uvs[i].x * escalaUvPropia.Value.x, origen.Uvs[i].y * escalaUvPropia.Value.y);
                }
                else
                {
                    uvBase[i] = Octaedro(n);
                    uv = uvBase[i] * repeticionUv;
                }
                m.Agregar(origen.Vertices[i] + n * desplazamiento, n, uv);
            }
            for (int t = 0; t + 2 < origen.Triangulos.Count; t += 3)
            {
                int a = origen.Triangulos[t], b = origen.Triangulos[t + 1], c = origen.Triangulos[t + 2];
                float du = Mathf.Max(Mathf.Abs(uvBase[a].x - uvBase[b].x), Mathf.Max(Mathf.Abs(uvBase[a].x - uvBase[c].x), Mathf.Abs(uvBase[b].x - uvBase[c].x)));
                float dv = Mathf.Max(Mathf.Abs(uvBase[a].y - uvBase[b].y), Mathf.Max(Mathf.Abs(uvBase[a].y - uvBase[c].y), Mathf.Abs(uvBase[b].y - uvBase[c].y)));
                if (du > 0.25f || dv > 0.25f) continue;
                if (despejar.HasValue)
                {
                    var n = origen.Normales[a] + origen.Normales[b] + origen.Normales[c];
                    if (n.sqrMagnitude > 1e-8f && Vector3.Dot(n.normalized, despejar.Value) > cosenoDespejado) continue;
                }
                m.Triangulos.Add(a);
                m.Triangulos.Add(b);
                m.Triangulos.Add(c);
            }
            return m;
        }

        /// <summary>Proyección octaédrica de una dirección a [0,1]² (el hemisferio superior ocupa el rombo central, sin costuras).</summary>
        public static Vector2 Octaedro(Vector3 n)
        {
            float s = Mathf.Abs(n.x) + Mathf.Abs(n.y) + Mathf.Abs(n.z);
            if (s < 1e-6f) return new Vector2(0.5f, 0.5f);
            float x = n.x / s, z = n.z / s;
            if (n.y < 0f)
            {
                float ox = (1f - Mathf.Abs(z)) * (x >= 0f ? 1f : -1f);
                float oz = (1f - Mathf.Abs(x)) * (z >= 0f ? 1f : -1f);
                x = ox;
                z = oz;
            }
            return new Vector2(x * 0.5f + 0.5f, z * 0.5f + 0.5f);
        }

        /// <summary>Semiesfera inferior (y ≤ 0) con tapa plana arriba: vista de frente es una "D" tumbada (boca abierta).</summary>
        public static DatosMalla Semiesfera(float radio = 0.5f, int segmentos = 16)
        {
            var m = Superficie(segmentos * 2, segmentos / 2,
                (u, v) => PuntoEsfera(u, v * 0.5f) * radio,
                (u, v) => PuntoEsfera(u, v * 0.5f),
                (u, v) => new Vector2(u * 2f, v * 0.5f));
            Tapa(m, radio, 0f, Vector3.up, segmentos * 2);
            return m;
        }

        /// <summary>Hebras por haz en los ovillos: van de cuatro en cuatro, paralelas, como al enrollar un ovillo de verdad.</summary>
        public const int HebrasPorHaz = 4;

        /// <summary>
        /// Ovillo de hebras: haces de cuatro vueltas paralelas de lana gruesa (tubos retorcidos) enrollados alrededor
        /// de una esfera. Cada haz cruza a los demás en otra dirección (espiral de Fibonacci con algo de azar) y va
        /// un poco más alto que el anterior, así se montan unos sobre otros como en un ovillo bien enrollado.
        /// Una ondulación suave evita que se vea demasiado perfecto. Todo en una sola malla.
        /// </summary>
        public static DatosMalla OvilloDeHebras(int hebras, float radio, float grosor, int semilla, int muestras = 44, int segmentos = 8)
        {
            var m = new DatosMalla();
            var azar = new System.Random(semilla);
            float R(float a, float b) => a + (float)azar.NextDouble() * (b - a);
            // torsión de los cabos: una vuelta de los tres cabos cada ~6 grosores (cuerda gruesa, no espiga)
            float uvPorUnidad = 0.16f / grosor;
            int haces = Mathf.Max(1, hebras / HebrasPorHaz);
            var puntos = new List<Vector3>(muestras + 1);
            for (int k = 0; k < haces; k++)
            {
                float t = (k + 0.5f) / haces;
                float polar = Mathf.Acos(1f - 2f * t), acimut = k * 2.39996f + 0.6f;
                var eje = new Vector3(Mathf.Sin(polar) * Mathf.Cos(acimut), Mathf.Cos(polar), Mathf.Sin(polar) * Mathf.Sin(acimut));
                eje = (eje + new Vector3(R(-0.2f, 0.2f), R(-0.2f, 0.2f), R(-0.2f, 0.2f))).normalized;
                var a = Vector3.Cross(eje, Mathf.Abs(eje.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
                var b = Vector3.Cross(eje, a).normalized;
                float r = radio + grosor * (0.3f + 0.9f * k / Mathf.Max(1, haces - 1));
                int ondas = 2 + azar.Next(2);
                float amplitud = R(0.02f, 0.05f), fase = R(0f, Mathf.PI * 2f);
                for (int j = 0; j < HebrasPorHaz; j++)
                {
                    // vueltas paralelas, una junto a otra: círculos menores de la esfera a distinta altura sobre el eje
                    float h = (j - (HebrasPorHaz - 1) * 0.5f) * grosor * 2.05f;
                    float anillo = Mathf.Sqrt(Mathf.Max(0.01f, r * r - h * h));
                    puntos.Clear();
                    for (int i = 0; i <= muestras; i++)
                    {
                        float ang = i * Mathf.PI * 2f / muestras;
                        float onda = amplitud * r * Mathf.Sin(ondas * ang + fase);
                        var p = (a * Mathf.Cos(ang) + b * Mathf.Sin(ang)) * anillo + eje * (h + onda);
                        puntos.Add(p.normalized * r);
                    }
                    Tubo(puntos, grosor, segmentos, uvPorUnidad, false, m);
                }
            }
            return m;
        }

        /// <summary>Anillo de tubo (la bufanda alrededor del cuello), con UV de tubo: U alrededor, V a lo largo.</summary>
        public static DatosMalla Anillo(float radioMayor, float radioMenor, float uvPorUnidad = 3.6f, int muestras = 64, int segmentos = 14)
        {
            var puntos = new List<Vector3>(muestras + 1);
            for (int i = 0; i <= muestras; i++)
            {
                float a = i * Mathf.PI * 2f / muestras;
                puntos.Add(new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radioMayor);
            }
            return Tubo(puntos, radioMenor, segmentos, uvPorUnidad, false);
        }

        /// <summary>Tira colgante (punta de la bufanda): un tubo que baja curvándose un poco hacia atrás.</summary>
        public static DatosMalla Colgante(float largo, float radio, float uvPorUnidad = 3.6f, int muestras = 10, int segmentos = 12)
        {
            var puntos = new List<Vector3>(muestras + 1);
            for (int i = 0; i <= muestras; i++)
            {
                float t = i / (float)muestras;
                puntos.Add(new Vector3(0f, -t * largo, Mathf.Sin(t * Mathf.PI * 0.5f) * largo * 0.18f));
            }
            return Tubo(puntos, radio, segmentos, uvPorUnidad, true);
        }

        /// <summary>Plano horizontal (XZ) centrado, mirando hacia arriba (sombras de contacto).</summary>
        public static DatosMalla Plano(float ancho, float fondo)
        {
            var m = new DatosMalla();
            float a = ancho / 2f, f = fondo / 2f;
            int v0 = m.Agregar(new Vector3(-a, 0, -f), Vector3.up, new Vector2(0, 0));
            int v1 = m.Agregar(new Vector3(a, 0, -f), Vector3.up, new Vector2(1, 0));
            int v2 = m.Agregar(new Vector3(a, 0, f), Vector3.up, new Vector2(1, 1));
            int v3 = m.Agregar(new Vector3(-a, 0, f), Vector3.up, new Vector2(0, 1));
            m.Quad(v0, v1, v2, v3, Vector3.up);
            return m;
        }

        /// <summary>
        /// Caja con aristas redondeadas (aspecto acolchado). Las muestras se concentran en los bordes para que
        /// el redondeo sea suave con pocos vértices. UV 0..1 en cada cara.
        /// </summary>
        public static DatosMalla CajaRedondeada(float ancho, float alto, float fondo, float radio, int pasosBorde = 4)
        {
            var m = new DatosMalla();
            var semi = new Vector3(ancho / 2f, alto / 2f, fondo / 2f);
            radio = Mathf.Min(radio, Mathf.Min(semi.x, Mathf.Min(semi.y, semi.z)) * 0.999f);
            var interior = semi - Vector3.one * radio;
            var ejes = new[] { Vector3.right, Vector3.up, Vector3.forward };
            for (int eje = 0; eje < 3; eje++)
            {
                for (int signo = -1; signo <= 1; signo += 2)
                {
                    var n = ejes[eje] * signo;
                    var a = ejes[(eje + 1) % 3];
                    var b = ejes[(eje + 2) % 3];
                    float sa = Componente(semi, (eje + 1) % 3), sb = Componente(semi, (eje + 2) % 3);
                    var muestrasA = Muestras(sa, radio, pasosBorde);
                    var muestrasB = Muestras(sb, radio, pasosBorde);
                    int filas = muestrasB.Count, columnas = muestrasA.Count;
                    int baseIndice = m.Vertices.Count;
                    for (int j = 0; j < filas; j++)
                    {
                        for (int i = 0; i < columnas; i++)
                        {
                            var p = n * Componente(semi, eje) + a * muestrasA[i] + b * muestrasB[j];
                            var dentro = new Vector3(Mathf.Clamp(p.x, -interior.x, interior.x), Mathf.Clamp(p.y, -interior.y, interior.y), Mathf.Clamp(p.z, -interior.z, interior.z));
                            var normal = (p - dentro).sqrMagnitude > 1e-10f ? (p - dentro).normalized : n;
                            var pos = dentro + normal * radio;
                            var uv = new Vector2((muestrasA[i] + sa) / (2f * sa), (muestrasB[j] + sb) / (2f * sb));
                            m.Agregar(pos, normal, uv);
                        }
                    }
                    for (int j = 0; j < filas - 1; j++)
                    {
                        for (int i = 0; i < columnas - 1; i++)
                        {
                            int v0 = baseIndice + j * columnas + i, v1 = v0 + 1, v3 = v0 + columnas, v2 = v3 + 1;
                            var fuera = m.Normales[v0] + m.Normales[v1] + m.Normales[v2] + m.Normales[v3];
                            m.Quad(v0, v1, v2, v3, fuera);
                        }
                    }
                }
            }
            return m;
        }

        private static float Componente(Vector3 v, int i) => i == 0 ? v.x : i == 1 ? v.y : v.z;

        /// <summary>Muestras de un eje: densas en las dos bandas redondeadas y una sola división en la parte plana.</summary>
        private static List<float> Muestras(float semi, float radio, int pasos)
        {
            var lista = new List<float>();
            for (int k = 0; k <= pasos; k++)
            {
                float ang = k / (float)pasos * Mathf.PI / 2f;
                lista.Add(-semi + radio * (1f - Mathf.Cos(ang)));
            }
            for (int k = pasos; k >= 0; k--)
            {
                float ang = k / (float)pasos * Mathf.PI / 2f;
                float x = semi - radio * (1f - Mathf.Cos(ang));
                if (x > lista[lista.Count - 1] + 1e-5f) lista.Add(x);
            }
            return lista;
        }

        /// <summary>
        /// Tubo a lo largo de una polilínea (la hebra de lana). Marcos por transporte paralelo para que no gire
        /// de golpe en las curvas; V avanza con la longitud (la textura helicoidal da la torsión) y U rodea el tubo.
        /// Extremos redondeados con media esfera.
        /// </summary>
        public static DatosMalla Tubo(IList<Vector3> puntos, float radio, int segmentos = 14, float uvPorUnidad = 2.5f, bool extremos = true, DatosMalla destino = null)
        {
            var m = destino ?? new DatosMalla();
            int n = puntos.Count;
            if (n < 2) return m;
            var tangentes = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                var t = i == 0 ? puntos[1] - puntos[0] : i == n - 1 ? puntos[n - 1] - puntos[n - 2] : puntos[i + 1] - puntos[i - 1];
                tangentes[i] = t.sqrMagnitude > 1e-10f ? t.normalized : Vector3.forward;
            }
            var normal = Mathf.Abs(Vector3.Dot(tangentes[0], Vector3.up)) < 0.9f ? Vector3.Cross(tangentes[0], Vector3.up).normalized : Vector3.Cross(tangentes[0], Vector3.right).normalized;
            float longitud = 0;
            var anillos = new int[n];
            for (int i = 0; i < n; i++)
            {
                if (i > 0)
                {
                    longitud += (puntos[i] - puntos[i - 1]).magnitude;
                    var eje = Vector3.Cross(tangentes[i - 1], tangentes[i]);
                    if (eje.sqrMagnitude > 1e-10f)
                    {
                        float ang = Mathf.Acos(Mathf.Clamp(Vector3.Dot(tangentes[i - 1], tangentes[i]), -1f, 1f)) * Mathf.Rad2Deg;
                        normal = Quaternion.AngleAxis(ang, eje.normalized) * normal;
                    }
                }
                var binormal = Vector3.Cross(tangentes[i], normal).normalized;
                anillos[i] = m.Vertices.Count;
                for (int s = 0; s <= segmentos; s++)
                {
                    float a = s / (float)segmentos * Mathf.PI * 2f;
                    var dir = normal * Mathf.Cos(a) + binormal * Mathf.Sin(a);
                    m.Agregar(puntos[i] + dir * radio, dir, new Vector2(s / (float)segmentos, longitud * uvPorUnidad));
                }
            }
            for (int i = 0; i < n - 1; i++)
            {
                for (int s = 0; s < segmentos; s++)
                {
                    int a = anillos[i] + s, b = a + 1, d = anillos[i + 1] + s, c = d + 1;
                    var fuera = m.Normales[a] + m.Normales[b] + m.Normales[c] + m.Normales[d];
                    m.Quad(a, b, c, d, fuera);
                }
            }
            if (extremos)
            {
                Remate(m, puntos[0], -tangentes[0], radio, segmentos, 0f);
                Remate(m, puntos[n - 1], tangentes[n - 1], radio, segmentos, longitud * uvPorUnidad);
            }
            return m;
        }

        private static void Remate(DatosMalla m, Vector3 centro, Vector3 hacia, float radio, int segmentos, float v0)
        {
            var normal = Mathf.Abs(Vector3.Dot(hacia, Vector3.up)) < 0.9f ? Vector3.Cross(hacia, Vector3.up).normalized : Vector3.Cross(hacia, Vector3.right).normalized;
            var binormal = Vector3.Cross(hacia, normal).normalized;
            int pisos = 4;
            Superficie(segmentos, pisos, (u, v) =>
                {
                    float a = u * Mathf.PI * 2f, lat = v * Mathf.PI / 2f;
                    var dir = (normal * Mathf.Cos(a) + binormal * Mathf.Sin(a)) * Mathf.Cos(lat) + hacia * Mathf.Sin(lat);
                    return centro + dir * radio;
                },
                (u, v) =>
                {
                    float a = u * Mathf.PI * 2f, lat = v * Mathf.PI / 2f;
                    return (normal * Mathf.Cos(a) + binormal * Mathf.Sin(a)) * Mathf.Cos(lat) + hacia * Mathf.Sin(lat);
                },
                (u, v) => new Vector2(u, v0 + v * 0.2f), m);
        }

        /// <summary>
        /// Camino suave de la lana a partir de los centros de casilla: tramos rectos y cuartos de circunferencia
        /// en las curvas (radio de media casilla), a la altura indicada.
        /// </summary>
        public static List<Vector3> CaminoSuave(IList<Vector3> centros, float muestrasPorCurva = 7)
        {
            var puntos = new List<Vector3>();
            void Poner(Vector3 p)
            {
                if (puntos.Count == 0 || (puntos[puntos.Count - 1] - p).sqrMagnitude > 1e-6f) puntos.Add(p);
            }
            int n = centros.Count;
            if (n == 0) return puntos;
            if (n == 1)
            {
                puntos.Add(centros[0]);
                return puntos;
            }
            Poner(centros[0]);
            for (int i = 1; i < n - 1; i++)
            {
                var entrada = (centros[i] - centros[i - 1]).normalized;
                var salida = (centros[i + 1] - centros[i]).normalized;
                var bordeEntrada = centros[i] - entrada * 0.5f;
                var bordeSalida = centros[i] + salida * 0.5f;
                if (Vector3.Dot(entrada, salida) > 0.99f)
                {
                    Poner(bordeEntrada);
                    Poner(centros[i]);
                    continue;
                }
                var esquina = centros[i] - entrada * 0.5f + salida * 0.5f;
                var desdeEsquinaA = bordeEntrada - esquina;
                var desdeEsquinaB = bordeSalida - esquina;
                for (int k = 0; k <= muestrasPorCurva; k++)
                {
                    float t = k / muestrasPorCurva * Mathf.PI / 2f;
                    Poner(esquina + desdeEsquinaA * Mathf.Cos(t) + desdeEsquinaB * Mathf.Sin(t));
                }
            }
            Poner(centros[n - 1] - (centros[n - 1] - centros[n - 2]).normalized * 0.5f);
            Poner(centros[n - 1]);
            return puntos;
        }
    }
}
