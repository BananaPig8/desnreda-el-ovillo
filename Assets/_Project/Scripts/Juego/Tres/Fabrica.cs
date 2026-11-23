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

        private static Shader ShaderLit(string nombrePlantilla)
        {
            // Las plantillas de Resources/Materiales (las crea el menú del editor) garantizan que el shader y sus
            // variantes (normal map, emisión, recorte) entran en la build. En el editor basta con Shader.Find.
            var plantilla = Resources.Load<Material>("Materiales/" + nombrePlantilla);
            if (plantilla != null && plantilla.shader != null && plantilla.shader.name.Contains("Universal") == EsURP) return plantilla.shader;
            Shader s = EsURP ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
            return s != null ? s : Shader.Find("Standard");
        }

        public static Material Obtener(string id)
        {
            if (Cache.TryGetValue(id, out var m) && m != null) return m;
            if (id == "sombra")
            {
                // Sombra de contacto: shader sin luz con mezcla alfa (Sprites/Default existe en URP y en el
                // pipeline integrado). La plantilla "Sombra" garantiza que entra en la build.
                var plantillaSombra = Resources.Load<Material>("Materiales/Sombra");
                var sprites = plantillaSombra != null && plantillaSombra.shader != null ? plantillaSombra.shader : Shader.Find("Sprites/Default");
                if (sprites == null) return null;
                m = new Material(sprites) { name = "Ovillo/sombra" };
                m.mainTexture = Textura(TexturasTres.SombraContacto())[0];
                m.mainTexture.wrapMode = TextureWrapMode.Clamp;
                m.renderQueue = (int)RenderQueue.Transparent - 10;
                Cache[id] = m;
                return m;
            }
            string plantilla = id == "emisivo" ? "Emisivo" : id == "pelusa" ? "Pelusa" : "Base";
            m = new Material(ShaderLit(plantilla)) { name = "Ovillo/" + id };
            switch (id)
            {
                case "pelusa":
                    Configurar(m, Textura(TexturasTres.Pelusa()), 0f, 0.04f, 1f);
                    ConfigurarRecorte(m);
                    break;
                case "fieltro": Configurar(m, Textura(TexturasTres.Fieltro()), 0f, 0.06f, 1.2f); break;
                case "casilla": Configurar(m, Textura(TexturasTres.Casilla()), 0f, 0.05f, 1f); break;
                case "lana": Configurar(m, Textura(TexturasTres.Lana()), 0f, 0.12f, 1f); break;
                case "madera": Configurar(m, Textura(TexturasTres.Madera()), 0f, 0.28f, 0.7f); break;
                case "mesa": Configurar(m, Textura(TexturasTres.Madera()), 0f, 0.32f, 2.5f); break;
                case "tela": Configurar(m, Textura(TexturasTres.Tela()), 0f, 0.12f, 1f); break;
                case "punto": Configurar(m, Textura(TexturasTres.Punto()), 0f, 0.08f, 1f); break;
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

        /// <summary>Transparencia recortada (alpha clip) tanto en URP/Lit como en Standard.</summary>
        private static void ConfigurarRecorte(Material m)
        {
            if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 1f);
            if (m.HasProperty("_Mode")) m.SetFloat("_Mode", 1f);
            m.SetOverrideTag("RenderType", "TransparentCutout");
            m.EnableKeyword("_ALPHATEST_ON");
            if (m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", 0.5f);
            m.renderQueue = (int)RenderQueue.AlphaTest;
        }

        private static readonly int IdRecorte = Shader.PropertyToID("_Cutoff");

        public static void Recortar(Renderer r, float corte, MaterialPropertyBlock bloque)
        {
            r.GetPropertyBlock(bloque);
            bloque.SetFloat(IdRecorte, corte);
            r.SetPropertyBlock(bloque);
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
        /// <summary>Sombra de contacto de la raíz (si el modelo la tiene).</summary>
        public Transform Sombra;
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
        public const float OpacidadSombra = Medidas.OpacidadSombra;
        private static readonly Dictionary<string, Mesh> CacheMallas = new Dictionary<string, Mesh>();
        private static readonly MaterialPropertyBlock Bloque = new MaterialPropertyBlock();
        private static Mesh _mallaSombra;

        public static Mesh Malla(string clave)
        {
            if (CacheMallas.TryGetValue(clave, out var m) && m != null) return m;
            m = AMesh(CatalogoMallas.Obtener(clave), clave);
            CacheMallas[clave] = m;
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
            if (!string.IsNullOrEmpty(p.Malla) && p.Pelusa > 0) CrearPelusa(p, t, modelo, capa);
            if (p.Sombra.x > 0f && p.Sombra.y > 0f)
            {
                var sombra = CrearSombra(t, p.Sombra);
                if (modelo.Sombra == null) modelo.Sombra = sombra;
            }
            if (!string.IsNullOrEmpty(p.Grupo))
            {
                if (!modelo.Grupos.TryGetValue(p.Grupo, out var lista)) modelo.Grupos[p.Grupo] = lista = new List<Transform>();
                lista.Add(t);
            }
            foreach (var h in p.Hijos) Crear(h, t, modelo, capa);
            if (p.Oculta) go.SetActive(false);
            return t;
        }

        /// <summary>Capas de pelusa: copias un poco mayores de la malla con fibras recortadas; las exteriores, más ralas.</summary>
        private static void CrearPelusa(Pieza p, Transform padre, ModeloInstanciado modelo, int capa)
        {
            var material = Materiales.Obtener("pelusa");
            if (material == null) return;
            for (int k = 1; k <= p.Pelusa; k++)
            {
                var go = new GameObject("Pelusa " + k);
                go.layer = capa;
                go.transform.SetParent(padre, false);
                go.AddComponent<MeshFilter>().sharedMesh = Malla(CapasPelusa.Malla(p.Malla, k, p.CaraLibre));
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = material;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = true;
                var color = CapasPelusa.Tono(p.Color, k);
                Materiales.Tenir(r, color, Bloque);
                Materiales.Recortar(r, CapasPelusa.Corte(k), Bloque);
                modelo.Renderers.Add(r);
                modelo.ColoresOriginales.Add(color);
            }
        }

        /// <summary>Mancha de sombra suave bajo un personaje u objeto, para asentarlo en el fieltro o en la mesa.</summary>
        public static Transform CrearSombra(Transform padre, Vector2 tamano, float opacidad = OpacidadSombra)
        {
            var material = Materiales.Obtener("sombra");
            if (material == null) return null;
            var go = new GameObject("Sombra de contacto");
            go.layer = padre != null ? padre.gameObject.layer : 0;
            go.transform.SetParent(padre, false);
            go.transform.localPosition = new Vector3(0, 0.004f, 0);
            go.transform.localScale = new Vector3(tamano.x, 1f, tamano.y);
            go.AddComponent<MeshFilter>().sharedMesh = MallaSombra();
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            Materiales.Tenir(r, new Color(1f, 1f, 1f, opacidad), Bloque);
            return go.transform;
        }

        /// <summary>Plano de 1x1 con colores de vértice blancos (el shader de sprites los multiplica).</summary>
        private static Mesh MallaSombra()
        {
            if (_mallaSombra != null) return _mallaSombra;
            _mallaSombra = AMesh(Tres.Mallas.Plano(1f, 1f), "Sombra de contacto");
            _mallaSombra.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            return _mallaSombra;
        }
    }

    /// <summary>
    /// Mantiene la sombra de contacto de un personaje pegada al suelo aunque salte, vuele o tropiece:
    /// cuanto más se eleva, más pequeña y clara (así se lee a qué casilla corresponde una polilla en el aire).
    /// </summary>
    public sealed class SombraDinamica : MonoBehaviour
    {
        private Transform _dueno;
        private Renderer _renderer;
        private Vector3 _escalaBase;
        private float _suelo, _reposo, _alfaAplicada = -1f;
        private readonly MaterialPropertyBlock _bloque = new MaterialPropertyBlock();

        /// <summary>Altura que el dueño no refleja en su posición (el cuerpo de una polilla que vuela).</summary>
        public float Extra { get; set; }

        /// <param name="suelo">Altura del suelo en el mundo.</param>
        /// <param name="reposo">Altura del dueño cuando está apoyado.</param>
        public static SombraDinamica Agregar(Transform sombra, Transform dueno, float suelo, float reposo)
        {
            if (sombra == null || dueno == null) return null;
            var s = sombra.gameObject.AddComponent<SombraDinamica>();
            s._dueno = dueno;
            s._renderer = sombra.GetComponent<Renderer>();
            s._escalaBase = sombra.localScale;
            s._suelo = suelo;
            s._reposo = reposo;
            return s;
        }

        private void LateUpdate()
        {
            if (_dueno == null || _renderer == null) return;
            var p = _dueno.position;
            float k = Mathf.Clamp01(Mathf.Max(0f, p.y - _reposo + Extra) / 1.2f);
            transform.SetPositionAndRotation(new Vector3(p.x, _suelo + 0.004f, p.z), Quaternion.Euler(0f, _dueno.eulerAngles.y, 0f));
            transform.localScale = _escalaBase * (1f - 0.4f * k);
            float alfa = Constructor.OpacidadSombra * (1f - 0.65f * k);
            if (Mathf.Abs(alfa - _alfaAplicada) < 0.01f) return;
            _alfaAplicada = alfa;
            Materiales.Tenir(_renderer, new Color(1f, 1f, 1f, alfa), _bloque);
        }
    }
}
