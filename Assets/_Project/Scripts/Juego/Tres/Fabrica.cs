using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DesenredaElOvillo.Juego.Tres
{
    /// <summary>
    /// Materiales del diorama. Usa URP/Lit si el proyecto tiene URP activo y el Standard en caso contrario,
    /// con texturas y mapas de normales procedurales (TexturasTres). Cada objeto pone su color con un
    /// MaterialPropertyBlock, así una sola textura de fieltro sirve para todo el fieltro del juego.
    /// </summary>
    public static class Materiales
    {
        private static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();
        private static readonly Dictionary<string, Texture2D> Texturas = new Dictionary<string, Texture2D>();
        private static readonly int IdColorBase = Shader.PropertyToID("_BaseColor");
        private static readonly int IdColor = Shader.PropertyToID("_Color");
        private static readonly int IdEmision = Shader.PropertyToID("_EmissionColor");

        public static bool EsURP => GraphicsSettings.currentRenderPipeline != null;

        private static Shader ShaderLit(bool emisivo)
        {
            // Las plantillas de Resources/Materiales (las crea el menú del editor) garantizan que el shader y sus
            // variantes (normal map, emisión) entran en la build. En el editor basta con Shader.Find.
            var plantilla = Resources.Load<Material>(emisivo ? "Materiales/Emisivo" : "Materiales/Base");
            if (plantilla != null && plantilla.shader != null && plantilla.shader.name.Contains("Universal") == EsURP) return plantilla.shader;
            Shader s = EsURP ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
            return s != null ? s : Shader.Find("Standard");
        }

        public static Material Obtener(string id)
        {
            if (Cache.TryGetValue(id, out var m) && m != null) return m;
            bool emisivo = id == "emisivo";
            m = new Material(ShaderLit(emisivo)) { name = "Ovillo/" + id };
            switch (id)
            {
                case "fieltro": Configurar(m, Textura(TexturasTres.Fieltro()), 0f, 0.06f, 1.2f); break;
                case "casilla": Configurar(m, Textura(TexturasTres.Casilla()), 0f, 0.05f, 1f); break;
                case "lana": Configurar(m, Textura(TexturasTres.Lana()), 0f, 0.12f, 1f); break;
                case "madera": Configurar(m, Textura(TexturasTres.Madera()), 0f, 0.28f, 0.7f); break;
                case "mesa": Configurar(m, Textura(TexturasTres.Madera()), 0f, 0.32f, 2.5f); break;
                case "tela": Configurar(m, Textura(TexturasTres.Tela()), 0f, 0.12f, 1f); break;
                case "velcro": Configurar(m, Textura(TexturasTres.Velcro()), 0f, 0.1f, 1f); break;
                case "metal": Configurar(m, null, 0.85f, 0.62f, 1f); break;
                case "oro": Configurar(m, null, 0.9f, 0.55f, 1f); break;
                case "boton": Configurar(m, Textura(TexturasTres.Fieltro()), 0f, 0.42f, 0.25f); break;
                case "brillante": Configurar(m, null, 0f, 0.86f, 1f); break;
                case "emisivo":
                    Configurar(m, null, 0f, 0.3f, 1f);
                    m.EnableKeyword("_EMISSION");
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    break;
                default: Configurar(m, Textura(TexturasTres.Fieltro()), 0f, 0.1f, 1f); break;
            }
            Cache[id] = m;
            return m;
        }

        private static Texture2D[] Textura(TexturaMaterial t)
        {
            if (!Texturas.TryGetValue(t.Nombre, out var color))
            {
                color = Crear(t.Color, false, t.Nombre);
                Texturas[t.Nombre] = color;
                Texturas[t.Nombre + "_n"] = Crear(t.Normal, true, t.Nombre + "_n");
            }
            return new[] { color, Texturas[t.Nombre + "_n"] };
        }

        private static Texture2D Crear(Pintor p, bool lineal, string nombre)
        {
            var tex = new Texture2D(p.Ancho, p.Alto, TextureFormat.RGBA32, true, lineal)
            {
                name = nombre,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 4
            };
            var px = new Color[p.Ancho * p.Alto];
            for (int y = 0; y < p.Alto; y++)
                for (int x = 0; x < p.Ancho; x++)
                    px[y * p.Ancho + x] = p.Leer(x, y);
            tex.SetPixels(px);
            tex.Apply(true, true);
            return tex;
        }

        private static void Configurar(Material m, Texture2D[] texturas, float metal, float suavidad, float repeticion)
        {
            if (texturas != null)
            {
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", texturas[0]);
                if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", texturas[0]);
                m.mainTextureScale = new Vector2(repeticion, repeticion);
                if (m.HasProperty("_BumpMap"))
                {
                    m.SetTexture("_BumpMap", texturas[1]);
                    if (m.HasProperty("_BumpScale")) m.SetFloat("_BumpScale", 1f);
                    m.EnableKeyword("_NORMALMAP");
                }
            }
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", suavidad);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", suavidad);
        }

        public static void Tenir(Renderer r, Color color, MaterialPropertyBlock bloque, Color? emision = null)
        {
            r.GetPropertyBlock(bloque);
            bloque.SetColor(IdColorBase, color);
            bloque.SetColor(IdColor, color);
            if (emision.HasValue) bloque.SetColor(IdEmision, emision.Value);
            r.SetPropertyBlock(bloque);
        }
    }

    /// <summary>Un modelo ya creado en la escena, con sus grupos (para animar) y sus renderers (para teñir).</summary>
    public sealed class ModeloInstanciado
    {
        public Transform Raiz;
        public readonly Dictionary<string, List<Transform>> Grupos = new Dictionary<string, List<Transform>>();
        public readonly List<Renderer> Renderers = new List<Renderer>();
        public readonly List<Color> ColoresOriginales = new List<Color>();
        private readonly MaterialPropertyBlock _bloque = new MaterialPropertyBlock();

        public List<Transform> Grupo(string nombre) => Grupos.TryGetValue(nombre, out var l) ? l : new List<Transform>();

        public Transform Primero(string grupo)
        {
            var l = Grupo(grupo);
            return l.Count > 0 ? l[0] : null;
        }

        /// <summary>Mezcla todos los colores hacia uno (destello de daño, gris de "gastado"...). t = 0 restaura.</summary>
        public void Destello(Color color, float t)
        {
            for (int i = 0; i < Renderers.Count; i++)
            {
                if (Renderers[i] == null) continue;
                Materiales.Tenir(Renderers[i], Color.Lerp(ColoresOriginales[i], color, t), _bloque);
            }
        }
    }

    /// <summary>Convierte descripciones de piezas en GameObjects con MeshFilter + MeshRenderer.</summary>
    public static class Constructor
    {
        private static readonly Dictionary<string, Mesh> Mallas = new Dictionary<string, Mesh>();
        private static readonly MaterialPropertyBlock Bloque = new MaterialPropertyBlock();

        public static Mesh Malla(string clave)
        {
            if (Mallas.TryGetValue(clave, out var m) && m != null) return m;
            m = AMesh(CatalogoMallas.Obtener(clave), clave);
            Mallas[clave] = m;
            return m;
        }

        public static Mesh AMesh(DatosMalla d, string nombre, Mesh reutilizar = null)
        {
            var m = reutilizar != null ? reutilizar : new Mesh();
            m.Clear();
            m.name = nombre;
            if (d.Vertices.Count > 65000) m.indexFormat = IndexFormat.UInt32;
            m.SetVertices(d.Vertices);
            m.SetNormals(d.Normales);
            m.SetUVs(0, d.Uvs);
            m.SetTriangles(d.Triangulos, 0);
            m.RecalculateBounds();
            m.RecalculateTangents();
            return m;
        }

        public static ModeloInstanciado Instanciar(Pieza raiz, Transform padre, int capa = 0)
        {
            var modelo = new ModeloInstanciado();
            modelo.Raiz = Crear(raiz, padre, modelo, capa);
            return modelo;
        }

        private static Transform Crear(Pieza p, Transform padre, ModeloInstanciado modelo, int capa)
        {
            var go = new GameObject(p.Nombre ?? "Pieza");
            go.layer = capa;
            var t = go.transform;
            t.SetParent(padre, false);
            t.localPosition = p.Posicion;
            t.localRotation = Quaternion.Euler(p.Rotacion);
            t.localScale = p.Escala;
            if (!string.IsNullOrEmpty(p.Malla))
            {
                go.AddComponent<MeshFilter>().sharedMesh = Malla(p.Malla);
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = Materiales.Obtener(p.Material);
                r.shadowCastingMode = p.ProyectaSombra ? ShadowCastingMode.On : ShadowCastingMode.Off;
                r.receiveShadows = true;
                Materiales.Tenir(r, p.Color, Bloque, p.Material == "emisivo" ? (Color?)(p.Color * 2.2f) : null);
                modelo.Renderers.Add(r);
                modelo.ColoresOriginales.Add(p.Color);
            }
            if (!string.IsNullOrEmpty(p.Grupo))
            {
                if (!modelo.Grupos.TryGetValue(p.Grupo, out var lista)) modelo.Grupos[p.Grupo] = lista = new List<Transform>();
                lista.Add(t);
            }
            foreach (var h in p.Hijos) Crear(h, t, modelo, capa);
            return t;
        }
    }
}
