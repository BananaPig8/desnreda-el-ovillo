using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace DesenredaElOvillo.Juego.Tres
{
    /// <summary>
    /// Ovillo a partir del modelo OBJ entregado (Assets/_Project/Arte/Modelos/Ovillo/Ovillo.obj, copia de lectura en
    /// Resources/Modelos/Ovillo_obj.txt). El OBJ es un modelo estático de concepto, sin huesos ni UV, así que aquí:
    ///  1. se pasa a las coordenadas del juego (Z invertida, triángulos al revés, escala 0,45 y los pies en y = 0);
    ///  2. se trocea por material y por piezas sueltas, y cada trozo va al grupo de animación que le corresponde
    ///     (cuerpo, brazo_*, pie_*, ojo_*, boca_feliz, bufanda, cola_bufanda) con su pivote (hombro, centro del ojo,
    ///     arranque de la punta de la bufanda...), así VistaOvillo lo anima igual que al Ovillo de la hoja;
    ///  3. se suelda, se calculan normales suaves y UV, y se viste con los materiales del juego (lana con pelusa,
    ///     punto, fieltro, ojos brillantes) o, para compararlo, con colores lisos como el OBJ original;
    ///  4. se le añaden las piezas de expresión que el OBJ no tiene (párpados, ojos "^" y "&gt; &lt;", boca en "o"
    ///     y boca triste), a la medida de sus ojos y su boca.
    /// Código puro (sin APIs de Unity) para usarlo igual en el juego y en la vista previa.
    /// </summary>
    public static class OvilloImportado
    {
        /// <summary>Escala del OBJ al juego: su cuerpo (Ø ≈ 1,56) queda de ≈ 0,70, como el Ovillo de la hoja.</summary>
        public const float Escala = 0.45f;
        /// <summary>La punta de la bufanda del OBJ llega por debajo de las botas: se acorta para que no atraviese el suelo.</summary>
        public const float AcortarCola = 0.78f;

        /// <summary>Texto del OBJ (el juego lo lee de Resources; la vista previa, del disco). Null = no hay modelo.</summary>
        public static Func<string> Fuente;
        /// <summary>Texto del MTL (opcional): colores Kd de cada material.</summary>
        public static Func<string> FuenteMtl;

        private static Datos _datos;
        private static bool _leido;

        public static bool Disponible => Cargar() != null;

        /// <summary>Claves de catálogo de todas las mallas del modelo (para verificarlas).</summary>
        public static IEnumerable<string> ClavesDeMallas()
        {
            var d = Cargar();
            if (d == null) yield break;
            foreach (var lista in d.Trozos.Values)
                foreach (var t in lista) yield return t.Clave;
        }

        // ================================================================== modelo animable

        /// <param name="vestido">true: materiales textiles del juego con pelusa; false: colores lisos como el OBJ original.</param>
        public static Pieza Modelo(bool vestido = true)
        {
            var d = Cargar();
            if (d == null) return null;
            var raiz = new Pieza { Nombre = "Ovillo (modelo OBJ)" };
            raiz.Sombra = new Vector2(d.AnchoCuerpo * 1.05f, d.AnchoCuerpo * 0.9f);

            var cuerpo = raiz.Hijo(new Pieza { Nombre = "Cuerpo", Posicion = d.Pivote("cuerpo"), Grupo = "cuerpo" });
            ColgarMallas(cuerpo, d, "cuerpo", vestido, 2);

            var cara = cuerpo.Hijo(new Pieza { Nombre = "Cara", Grupo = "cara" });
            foreach (string lado in new[] { "_izq", "_der" })
            {
                if (!d.Trozos.ContainsKey("ojo" + lado)) continue;
                var centro = d.Pivote("ojo" + lado);
                var tam = d.Tamano("ojo" + lado);
                var ancla = cara.Hijo(new Pieza { Nombre = "Ojo" + lado, Posicion = centro - d.Pivote("cuerpo") });
                var globo = ancla.Hijo(new Pieza { Nombre = "Globo", Grupo = "ojo" + lado });
                ColgarMallas(globo, d, "ojo" + lado, vestido, 0);
                // piezas de expresión, orientadas hacia fuera de la cara y a la medida del ojo
                var fuera = (centro - d.Pivote("cuerpo")).normalized;
                var orientado = ancla.Hijo(new Pieza
                {
                    Nombre = "Orientación",
                    Rotacion = new Vector3(Mathf.Asin(Mathf.Clamp(fuera.y, -1f, 1f)) * Mathf.Rad2Deg, Mathf.Atan2(-fuera.x, -fuera.z) * Mathf.Rad2Deg, 0f)
                });
                float w = tam.x, h = tam.y, prof = Mathf.Max(tam.z, 0.02f);
                var parpado = orientado.Hijo(new Pieza { Nombre = "Párpado", Posicion = new Vector3(0, h * 0.55f, -prof * 0.1f), Escala = new Vector3(1f, 0.001f, 1f), Grupo = "parpado" + lado, Oculta = true });
                parpado.Hijo(new Pieza { Nombre = "Piel", Malla = "esfera", Material = vestido ? "lana" : "liso", Color = d.ColorPiel, Posicion = new Vector3(0, -h * 0.55f, -prof * 0.12f), Escala = new Vector3(w * 1.12f, h * 1.12f, prof * 1.25f) });
                float largo = w * 0.52f, grosor = Mathf.Max(0.012f, w * 0.15f), z = -prof * 0.5f;
                var feliz = orientado.Hijo(new Pieza { Nombre = "Ojo feliz", Posicion = new Vector3(0, 0, z), Grupo = "feliz" + lado, Oculta = true });
                Trazo(feliz, -largo * 0.38f, 0f, largo, grosor, 40f);
                Trazo(feliz, largo * 0.38f, 0f, largo, grosor, -40f);
                float s = lado == "_izq" ? -1f : 1f;
                var apretado = orientado.Hijo(new Pieza { Nombre = "Ojo apretado", Posicion = new Vector3(0, 0, z), Grupo = "apretado" + lado, Oculta = true });
                Trazo(apretado, s * largo * 0.15f, largo * 0.25f, largo, grosor, s * 30f);
                Trazo(apretado, s * largo * 0.15f, -largo * 0.25f, largo, grosor, -s * 30f);
            }
            if (d.Trozos.ContainsKey("mofletes")) ColgarMallas(cara, d, "mofletes", vestido, 1, d.Pivote("cuerpo"));
            if (d.Trozos.ContainsKey("boca"))
            {
                var boca = d.Pivote("boca") - d.Pivote("cuerpo");
                var tamBoca = d.Tamano("boca");
                var feliz = cara.Hijo(new Pieza { Nombre = "Boca feliz", Posicion = boca, Grupo = "boca_feliz" });
                ColgarMallas(feliz, d, "boca", vestido, 0);
                var fuera = (d.Pivote("boca") - d.Pivote("cuerpo")).normalized;
                var giro = new Vector3(Mathf.Asin(Mathf.Clamp(fuera.y, -1f, 1f)) * Mathf.Rad2Deg, Mathf.Atan2(-fuera.x, -fuera.z) * Mathf.Rad2Deg, 0f);
                float a = Mathf.Max(0.04f, tamBoca.x);
                var o = cara.Hijo(new Pieza { Nombre = "Boca de susto", Posicion = boca, Rotacion = giro, Grupo = "boca_o", Oculta = true });
                o.Hijo(new Pieza { Nombre = "Boca", Malla = "esfera", Material = vestido ? "brillante" : "liso", Color = d.ColorOjos, Escala = new Vector3(a * 0.42f, a * 0.5f, a * 0.25f) });
                var triste = cara.Hijo(new Pieza { Nombre = "Boca triste", Posicion = boca, Rotacion = giro, Grupo = "boca_triste", Oculta = true });
                Trazo(triste, -a * 0.2f, 0f, a * 0.45f, a * 0.13f, 26f, d.ColorOjos);
                Trazo(triste, a * 0.2f, 0f, a * 0.45f, a * 0.13f, -26f, d.ColorOjos);
            }

            foreach (string lado in new[] { "_izq", "_der" })
            {
                if (!d.Trozos.ContainsKey("brazo" + lado)) continue;
                var brazo = cuerpo.Hijo(new Pieza { Nombre = "Brazo", Posicion = d.Pivote("brazo" + lado) - d.Pivote("cuerpo"), Grupo = "brazo" + lado });
                ColgarMallas(brazo, d, "brazo" + lado, vestido, 1);
            }
            if (d.Trozos.ContainsKey("bufanda"))
            {
                var bufanda = cuerpo.Hijo(new Pieza { Nombre = "Bufanda", Posicion = d.Pivote("bufanda") - d.Pivote("cuerpo"), Grupo = "bufanda" });
                ColgarMallas(bufanda, d, "bufanda", vestido, 1);
                if (d.Trozos.ContainsKey("cola"))
                {
                    var cola = bufanda.Hijo(new Pieza { Nombre = "Cola", Posicion = d.Pivote("cola") - d.Pivote("bufanda"), Escala = new Vector3(1f, AcortarCola, 1f), Grupo = "cola_bufanda" });
                    ColgarMallas(cola, d, "cola", vestido, 1);
                }
            }
            foreach (string lado in new[] { "_izq", "_der" })
            {
                if (!d.Trozos.ContainsKey("pie" + lado)) continue;
                var pie = raiz.Hijo(new Pieza { Nombre = "Bota", Posicion = d.Pivote("pie" + lado), Grupo = "pie" + lado });
                ColgarMallas(pie, d, "pie" + lado, vestido, 1);
            }
            return raiz;
        }

        /// <summary>Cuelga de "padre" las mallas de un grupo (una por material), relativas a "origen" (por defecto, su pivote).</summary>
        private static void ColgarMallas(Pieza padre, Datos d, string grupo, bool vestido, int pelusa, Vector3? origen = null)
        {
            if (!d.Trozos.TryGetValue(grupo, out var lista)) return;
            var desplazamiento = origen.HasValue ? d.Pivote(grupo) - origen.Value : Vector3.zero;
            foreach (var t in lista)
            {
                padre.Hijo(new Pieza
                {
                    Nombre = t.Material,
                    Malla = t.Clave,
                    Material = vestido ? t.MaterialJuego : "liso",
                    Color = t.Color,
                    Posicion = desplazamiento,
                    Pelusa = vestido && t.MaterialJuego != "brillante" ? pelusa : 0,
                    // los ojos del OBJ apenas sobresalen: la pelusa del cuerpo deja libre la cara
                    CaraLibre = grupo == "cuerpo"
                });
            }
        }

        private static void Trazo(Pieza padre, float x, float y, float largo, float grosor, float grados, Color? color = null)
        {
            padre.Hijo(new Pieza
            {
                Nombre = "Trazo",
                Malla = CatalogoMallas.Caja(largo, grosor, grosor, grosor * 0.4f),
                Material = "fieltro",
                Color = color ?? Paleta.Hex("#2A140D"),
                Posicion = new Vector3(x, y, 0f),
                Rotacion = new Vector3(0, 0, grados)
            });
        }

        // ================================================================== lectura y troceado

        private sealed class Trozo
        {
            public string Material, MaterialJuego, Clave;
            public Color Color;
        }

        private sealed class Datos
        {
            public readonly Dictionary<string, List<Trozo>> Trozos = new Dictionary<string, List<Trozo>>();
            public readonly Dictionary<string, Vector3> Pivotes = new Dictionary<string, Vector3>();
            public readonly Dictionary<string, Vector3> Tamanos = new Dictionary<string, Vector3>();
            public float AnchoCuerpo = 0.7f;
            public Color ColorPiel = Paleta.Hex("#E8B26E"), ColorOjos = Paleta.Hex("#2A140D");
            public Vector3 Pivote(string g) => Pivotes.TryGetValue(g, out var p) ? p : Vector3.zero;
            public Vector3 Tamano(string g) => Tamanos.TryGetValue(g, out var t) ? t : new Vector3(0.1f, 0.1f, 0.05f);
        }

        private struct Caja
        {
            public Vector3 Min, Max;
            public bool Vacia;
            public static Caja Nueva() => new Caja { Min = Vector3.one * float.MaxValue, Max = Vector3.one * float.MinValue, Vacia = true };
            public void Meter(Vector3 p)
            {
                Min = new Vector3(Mathf.Min(Min.x, p.x), Mathf.Min(Min.y, p.y), Mathf.Min(Min.z, p.z));
                Max = new Vector3(Mathf.Max(Max.x, p.x), Mathf.Max(Max.y, p.y), Mathf.Max(Max.z, p.z));
                Vacia = false;
            }
            public void Meter(Caja c) { if (!c.Vacia) { Meter(c.Min); Meter(c.Max); } }
            public Vector3 Centro => (Min + Max) * 0.5f;
            public Vector3 Tamano => Max - Min;
        }

        private sealed class Pieza0
        {
            public string Material;
            public readonly List<int[]> Triangulos = new List<int[]>();
            public Caja Caja = Caja.Nueva();
        }

        private static Datos Cargar()
        {
            if (_leido) return _datos;
            string texto = null;
            try { texto = Fuente?.Invoke(); }
            catch (Exception) { texto = null; }
            if (string.IsNullOrEmpty(texto)) return null;   // sin fuente todavía: se volverá a intentar
            _leido = true;
            try { _datos = Trocear(texto, LeerColores(SinExcepcion(FuenteMtl))); }
            catch (Exception) { _datos = null; }
            return _datos;
        }

        private static string SinExcepcion(Func<string> f)
        {
            try { return f?.Invoke(); }
            catch (Exception) { return null; }
        }

        /// <summary>Vuelve a leer el OBJ la próxima vez (si se ha cambiado la fuente).</summary>
        public static void Olvidar()
        {
            _leido = false;
            _datos = null;
        }

        private static Dictionary<string, Color> LeerColores(string mtl)
        {
            var colores = new Dictionary<string, Color>();
            if (string.IsNullOrEmpty(mtl)) return colores;
            string actual = null;
            foreach (var linea in mtl.Split('\n'))
            {
                var p = linea.Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (p.Length == 0) continue;
                if (p[0] == "newmtl" && p.Length > 1) actual = p[1];
                else if (p[0] == "Kd" && p.Length > 3 && actual != null)
                    colores[actual] = new Color(Num(p[1]), Num(p[2]), Num(p[3]), 1f);
            }
            return colores;
        }

        private static float Num(string s) => float.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);

        private static Datos Trocear(string obj, Dictionary<string, Color> colores)
        {
            // 1. lectura: coordenadas del juego y triángulos al revés (la Z invertida cambia la orientación)
            var v = new List<Vector3>();
            var caras = new List<(string material, int a, int b, int c)>();
            string material = "defecto";
            foreach (var linea in obj.Split('\n'))
            {
                var l = linea.Trim();
                if (l.Length < 2 || l[0] == '#') continue;
                var p = l.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (p[0] == "v" && p.Length > 3) v.Add(new Vector3(Num(p[1]), Num(p[2]), -Num(p[3])) * Escala);
                else if (p[0] == "usemtl" && p.Length > 1) material = p[1];
                else if (p[0] == "f" && p.Length > 3)
                {
                    var ind = new int[p.Length - 1];
                    for (int i = 1; i < p.Length; i++)
                    {
                        int k = int.Parse(p[i].Split('/')[0], CultureInfo.InvariantCulture);
                        ind[i - 1] = k > 0 ? k - 1 : v.Count + k;
                    }
                    for (int i = 1; i + 1 < ind.Length; i++) caras.Add((material, ind[0], ind[i + 1], ind[i]));
                }
            }
            if (v.Count == 0 || caras.Count == 0) return null;

            // 2. los pies (las botas) en y = 0
            float suelo = float.MaxValue;
            foreach (var c in caras)
                if (c.material.IndexOf("boot", StringComparison.OrdinalIgnoreCase) >= 0 || c.material.IndexOf("bota", StringComparison.OrdinalIgnoreCase) >= 0)
                    suelo = Mathf.Min(suelo, Mathf.Min(v[c.a].y, Mathf.Min(v[c.b].y, v[c.c].y)));
            if (suelo == float.MaxValue) foreach (var p in v) suelo = Mathf.Min(suelo, p.y);
            for (int i = 0; i < v.Count; i++) v[i] = new Vector3(v[i].x, v[i].y - suelo, v[i].z);

            // 3. piezas sueltas por material (unión de vértices compartidos)
            var padre = new Dictionary<(string, int), (string, int)>();
            (string, int) Raiz((string, int) x)
            {
                var r = x;
                while (padre.TryGetValue(r, out var p) && !p.Equals(r)) r = p;
                while (!x.Equals(r))
                {
                    var siguiente = padre[x];
                    padre[x] = r;
                    x = siguiente;
                }
                return r;
            }
            void Unir((string, int) a, (string, int) b)
            {
                var ra = Raiz(a);
                var rb = Raiz(b);
                if (!ra.Equals(rb)) padre[ra] = rb;
            }
            foreach (var c in caras)
            {
                Unir((c.material, c.a), (c.material, c.b));
                Unir((c.material, c.b), (c.material, c.c));
            }
            var piezas = new Dictionary<(string, int), Pieza0>();
            foreach (var c in caras)
            {
                var r = Raiz((c.material, c.a));
                if (!piezas.TryGetValue(r, out var pz)) piezas[r] = pz = new Pieza0 { Material = c.material };
                pz.Triangulos.Add(new[] { c.a, c.b, c.c });
                pz.Caja.Meter(v[c.a]);
                pz.Caja.Meter(v[c.b]);
                pz.Caja.Meter(v[c.c]);
            }

            // 4. el cuerpo: la pieza de lana más grande
            Pieza0 nucleo = null;
            int mayor = -1;
            foreach (var pz in piezas.Values)
            {
                if (!EsLana(pz.Material)) continue;
                int n = pz.Triangulos.Count;
                if (n > mayor) { mayor = n; nucleo = pz; }
            }
            if (nucleo == null) return null;
            var centro = nucleo.Caja.Centro;
            float radio = Mathf.Max(0.01f, nucleo.Caja.Tamano.x * 0.5f);

            // 5. cada pieza a su grupo
            var grupos = new Dictionary<string, List<Pieza0>>();
            foreach (var pz in piezas.Values)
            {
                var c = pz.Caja.Centro;
                float dx = (c.x - centro.x) / radio, dy = (c.y - centro.y) / radio;
                string lado = c.x < centro.x ? "_izq" : "_der";
                string m = pz.Material.ToLowerInvariant();
                string g;
                if (m.Contains("boot") || m.Contains("bota")) g = "pie" + lado;
                else if (m.Contains("glint") || m.Contains("brillo")) g = "ojo" + lado;
                else if (m.Contains("cheek") || m.Contains("moflete")) g = "mofletes";
                else if (m.Contains("eye") || m.Contains("ojo")) g = dy > 0f ? "ojo" + lado : "boca";
                else if (m.Contains("scarf") || m.Contains("bufanda")) g = dy < -0.7f ? "cola" : "bufanda";
                else if (EsLana(m))
                {
                    float ancho = pz.Caja.Tamano.x / radio;
                    if (dy < -0.75f) g = "pie" + lado;
                    else if (Mathf.Abs(dx) > 0.72f && dy > -0.55f && dy < 0.45f && ancho < 0.6f) g = "brazo" + lado;
                    else g = "cuerpo";
                }
                else g = "cuerpo";
                if (!grupos.TryGetValue(g, out var l)) grupos[g] = l = new List<Pieza0>();
                l.Add(pz);
            }

            // 6. pivotes y medidas de cada grupo
            var d = new Datos { AnchoCuerpo = radio * 2f };
            foreach (var kv in grupos)
            {
                var caja = Caja.Nueva();
                var cajaPrincipal = Caja.Nueva();
                foreach (var pz in kv.Value)
                {
                    caja.Meter(pz.Caja);
                    // el ojo se mide sin su brillo; la cola, entera; el pie, por la bota
                    string m = pz.Material.ToLowerInvariant();
                    bool principal = kv.Key.StartsWith("ojo") ? !(m.Contains("glint") || m.Contains("brillo"))
                        : kv.Key.StartsWith("pie") ? (m.Contains("boot") || m.Contains("bota")) : true;
                    if (principal) cajaPrincipal.Meter(pz.Caja);
                }
                if (cajaPrincipal.Vacia) cajaPrincipal = caja;
                Vector3 pivote;
                switch (kv.Key)
                {
                    case "cuerpo": pivote = centro; break;
                    case "brazo_izq": pivote = new Vector3(caja.Max.x, caja.Centro.y, caja.Centro.z); break;
                    case "brazo_der": pivote = new Vector3(caja.Min.x, caja.Centro.y, caja.Centro.z); break;
                    case "bufanda": pivote = new Vector3(centro.x, caja.Centro.y, centro.z); break;
                    case "cola": pivote = new Vector3(caja.Centro.x, caja.Max.y, caja.Centro.z); break;
                    default: pivote = cajaPrincipal.Centro; break;
                }
                d.Pivotes[kv.Key] = pivote;
                d.Tamanos[kv.Key] = cajaPrincipal.Tamano;
            }

            // 7. una malla por grupo y material: soldada, con normales suaves y UV
            int serie = 0;
            foreach (var kv in grupos)
            {
                var porMaterial = new Dictionary<string, List<int[]>>();
                foreach (var pz in kv.Value)
                {
                    if (!porMaterial.TryGetValue(pz.Material, out var l)) porMaterial[pz.Material] = l = new List<int[]>();
                    l.AddRange(pz.Triangulos);
                }
                var trozos = new List<Trozo>();
                foreach (var pm in porMaterial)
                {
                    var malla = Construir(v, pm.Value, d.Pivote(kv.Key), kv.Key);
                    string clave = "importado:ovillo/" + kv.Key + "/" + pm.Key + "/" + (serie++);
                    CatalogoMallas.Registrar(clave, malla);
                    var color = colores.TryGetValue(pm.Key, out var col) ? col : ColorPorNombre(pm.Key);
                    trozos.Add(new Trozo { Material = pm.Key, MaterialJuego = MaterialDeJuego(pm.Key), Clave = clave, Color = color });
                    if (kv.Key == "cuerpo" && EsLana(pm.Key) && pm.Key.IndexOf("light", StringComparison.OrdinalIgnoreCase) < 0) d.ColorPiel = color;
                    if (kv.Key.StartsWith("ojo") && MaterialDeJuego(pm.Key) == "brillante" && color.r < 0.2f) d.ColorOjos = color;
                }
                d.Trozos[kv.Key] = trozos;
            }
            return d;
        }

        private static bool EsLana(string material)
        {
            var m = material.ToLowerInvariant();
            return m.Contains("wool") || m.Contains("lana") || m.Contains("yarn");
        }

        private static string MaterialDeJuego(string material)
        {
            var m = material.ToLowerInvariant();
            if (m.Contains("scarf") || m.Contains("bufanda")) return "punto";
            if (m.Contains("eye") || m.Contains("ojo") || m.Contains("glint") || m.Contains("brillo")) return "brillante";
            if (EsLana(m)) return "lana";
            return "fieltro";
        }

        private static Color ColorPorNombre(string material)
        {
            var m = material.ToLowerInvariant();
            if (m.Contains("scarf")) return Paleta.Hex("#C8232C");
            if (m.Contains("boot")) return Paleta.Hex("#6B3920");
            if (m.Contains("glint")) return Color.white;
            if (m.Contains("eye")) return Paleta.Hex("#2A140D");
            if (m.Contains("cheek")) return Paleta.Hex("#F5929F");
            return Paleta.Hex("#E8B26E");
        }

        /// <summary>Malla de un trozo: vértices soldados por posición, normales suaves y UV según el tipo de pieza.</summary>
        private static DatosMalla Construir(List<Vector3> v, List<int[]> triangulos, Vector3 pivote, string grupo)
        {
            var m = new DatosMalla();
            var indice = new Dictionary<(long, long, long), int>();
            var pos = new List<Vector3>();
            var tris = new List<int>();
            foreach (var t in triangulos)
            {
                for (int k = 0; k < 3; k++)
                {
                    var p = v[t[k]];
                    var clave = ((long)Math.Round(p.x * 100000), (long)Math.Round(p.y * 100000), (long)Math.Round(p.z * 100000));
                    if (!indice.TryGetValue(clave, out int i))
                    {
                        i = pos.Count;
                        indice[clave] = i;
                        pos.Add(p);
                    }
                    tris.Add(i);
                }
            }
            var normales = new Vector3[pos.Count];
            for (int t = 0; t < tris.Count; t += 3)
            {
                var n = Vector3.Cross(pos[tris[t + 1]] - pos[tris[t]], pos[tris[t + 2]] - pos[tris[t]]);
                normales[tris[t]] += n;
                normales[tris[t + 1]] += n;
                normales[tris[t + 2]] += n;
            }
            // centro del trozo para las UV cilíndricas de la bufanda
            var centro = Vector3.zero;
            foreach (var p in pos) centro += p;
            centro /= Mathf.Max(1, pos.Count);
            for (int i = 0; i < pos.Count; i++)
            {
                var n = normales[i].sqrMagnitude > 1e-12f ? normales[i].normalized : Vector3.up;
                var p = pos[i];
                Vector2 uv;
                if (grupo == "bufanda")
                {
                    // a lo ancho = altura; a lo largo = vuelta alrededor del cuerpo (columnas de punto a lo largo)
                    float a = Mathf.Atan2(p.z - pivote.z, p.x - pivote.x) / (Mathf.PI * 2f) + 0.5f;
                    uv = new Vector2((p.y - pivote.y) / 0.47f * 2f, a * 7.4f);
                }
                else if (grupo == "cola")
                {
                    float a = Mathf.Atan2(p.z - centro.z, p.x - centro.x) / (Mathf.PI * 2f) + 0.5f;
                    uv = new Vector2(a * 0.6f, (p.y - pivote.y) / 0.28f);
                }
                else uv = Tres.Mallas.Octaedro(n) * 2f;
                m.Agregar(p - pivote, n, uv);
            }
            for (int t = 0; t < tris.Count; t += 3)
            {
                if (tris[t] == tris[t + 1] || tris[t + 1] == tris[t + 2] || tris[t] == tris[t + 2]) continue;
                m.Triangulos.Add(tris[t]);
                m.Triangulos.Add(tris[t + 1]);
                m.Triangulos.Add(tris[t + 2]);
            }
            return m;
        }
    }
}
