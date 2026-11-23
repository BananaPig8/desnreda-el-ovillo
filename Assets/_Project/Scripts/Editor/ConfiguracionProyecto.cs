using System;
using System.Collections.Generic;
using System.IO;
using DesenredaElOvillo.Juego;
using DesenredaElOvillo.Nucleo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace DesenredaElOvillo.Editor
{
    /// <summary>
    /// Deja el proyecto listo la primera vez que se abre: crea la escena de juego, la añade a la build
    /// y ajusta el modo 2D y la resolución. Añade el menú "Desenreda el Ovillo" con utilidades.
    /// </summary>
    [InitializeOnLoad]
    public static class ConfiguracionProyecto
    {
        public const string RutaEscena = "Assets/_Project/Escenas/Juego.unity";
        private const string CarpetaSalas = "Assets/_Project/Resources/Salas";
        private const string CarpetaMateriales = "Assets/_Project/Resources/Materiales";

        static ConfiguracionProyecto()
        {
            EditorApplication.delayCall += PrimeraVez;
        }

        private static void PrimeraVez()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isBatchMode) return;
            if (!File.Exists(RutaEscena))
            {
                var actual = SceneManager.GetActiveScene();
                bool vacia = string.IsNullOrEmpty(actual.path) && !actual.isDirty;
                if (vacia)
                {
                    CrearEscena();
                    EditorSceneManager.OpenScene(RutaEscena);
                    Debug.Log("Desenreda el Ovillo: escena de juego creada en " + RutaEscena + ". Pulsa Play.");
                }
            }
            AsegurarEscenaEnBuild();
            AsegurarPlantillasDeMaterial();
            ComprobarURP();
            if (PlayerSettings.productName != "Desenreda el Ovillo")
            {
                PlayerSettings.productName = "Desenreda el Ovillo";
                PlayerSettings.defaultScreenWidth = 1920;
                PlayerSettings.defaultScreenHeight = 1080;
                PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
                PlayerSettings.resizableWindow = true;
                PlayerSettings.colorSpace = ColorSpace.Linear;
            }
        }

        /// <summary>
        /// Plantillas de material en Resources: garantizan que el shader iluminado del pipeline activo y sus
        /// variantes (mapa de normales, emisión, recorte de la pelusa) y el de las sombras de contacto entran
        /// en la build, aunque los materiales se creen por código.
        /// </summary>
        [MenuItem("Desenreda el Ovillo/Regenerar plantillas de material", priority = 21)]
        public static void AsegurarPlantillasDeMaterial()
        {
            bool urp = GraphicsSettings.currentRenderPipeline != null;
            var shader = urp ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
            if (shader == null) return;
            Directory.CreateDirectory(CarpetaMateriales);
            CrearPlantilla("Base", shader, m => m.EnableKeyword("_NORMALMAP"));
            CrearPlantilla("Emisivo", shader, m =>
            {
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            });
            CrearPlantilla("Pelusa", shader, m =>
            {
                m.EnableKeyword("_NORMALMAP");
                if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 1f);
                if (m.HasProperty("_Mode")) m.SetFloat("_Mode", 1f);
                if (m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", 0.5f);
                m.SetOverrideTag("RenderType", "TransparentCutout");
                m.EnableKeyword("_ALPHATEST_ON");
                m.renderQueue = (int)RenderQueue.AlphaTest;
            });
            var sprites = Shader.Find("Sprites/Default");
            if (sprites != null) CrearPlantilla("Sombra", sprites, m => { });
        }

        private static void CrearPlantilla(string nombre, Shader shader, Action<Material> configurar)
        {
            string ruta = CarpetaMateriales + "/" + nombre + ".mat";
            var existente = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (existente != null && existente.shader == shader) return;
            if (existente != null) AssetDatabase.DeleteAsset(ruta);
            var m = new Material(shader) { name = nombre };
            configurar(m);
            AssetDatabase.CreateAsset(m, ruta);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// El aspecto final necesita URP. Si el paquete está pero no hay un asset de pipeline asignado, se avisa
        /// (la forma más fiable es crear el proyecto con la plantilla "Universal 3D" de Unity 6).
        /// </summary>
        private static void ComprobarURP()
        {
            if (SessionState.GetBool("ovillo.avisoURP", false)) return;
            SessionState.SetBool("ovillo.avisoURP", true);
#if OVILLO_URP
            if (GraphicsSettings.currentRenderPipeline == null)
                Debug.LogWarning("Desenreda el Ovillo: URP está instalado pero no hay ningún URP Asset asignado. " +
                                 "Crea uno (Assets > Create > Rendering > URP Asset (with Universal Renderer)) y asígnalo en " +
                                 "Project Settings > Graphics. Mientras tanto el juego se ve con el pipeline integrado.");
#else
            Debug.Log("Desenreda el Ovillo: el proyecto no usa URP. El juego funciona, pero el acabado de maqueta " +
                      "(desenfoque de fondo, bloom, gradación) necesita URP: ver README > Abrir el proyecto.");
#endif
        }

        [MenuItem("Desenreda el Ovillo/Abrir escena de juego", priority = 0)]
        public static void AbrirEscena()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(RutaEscena)) CrearEscena();
            EditorSceneManager.OpenScene(RutaEscena);
            AsegurarEscenaEnBuild();
        }

        private static void CrearEscena()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RutaEscena));
            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camara = new GameObject("Cámara", typeof(Camera));
            camara.tag = "MainCamera";
            var juego = new GameObject("Desenreda el Ovillo");
            juego.AddComponent<ControladorJuego>();
            EditorSceneManager.SaveScene(escena, RutaEscena);
            AssetDatabase.Refresh();
        }

        private static void AsegurarEscenaEnBuild()
        {
            if (!File.Exists(RutaEscena)) return;
            var escenas = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var e in escenas) if (e.path == RutaEscena) return;
            escenas.Insert(0, new EditorBuildSettingsScene(RutaEscena, true));
            EditorBuildSettings.scenes = escenas.ToArray();
        }

        /// <summary>Comprueba que todas las salas se leen y que su solución de referencia sigue ganando.</summary>
        [MenuItem("Desenreda el Ovillo/Validar salas", priority = 20)]
        public static void ValidarSalas()
        {
            int ok = 0, total = 0;
            foreach (var ruta in Directory.GetFiles(CarpetaSalas, "*.txt"))
            {
                total++;
                try
                {
                    var sala = Sala.DesdeTexto(File.ReadAllText(ruta));
                    if (sala.Solucion == null)
                    {
                        Debug.LogWarning(sala.Id + ": no tiene solución de referencia.");
                        continue;
                    }
                    var reglas = Reglas.PorDefecto().ConMejoras(sala.Solucion.Mejoras);
                    int hilo = sala.Hilo + reglas.HiloExtraPorSala;
                    var r = Recorrido.DesdeMovimientos(sala.Tablero, sala.Solucion.Movimientos, hilo);
                    if (r == null || !ValidadorRecorrido.EsValido(r, hilo, sala.LongitudMinima))
                    {
                        Debug.LogError(sala.Id + ": la lana de la solución ya no es válida en el mapa.");
                        continue;
                    }
                    var plan = new PlanColocacion(sala.Solucion.Colocaciones);
                    var res = SimuladorOleada.Simular(reglas, r, plan, sala.Oleada, reglas.VidaCarrete);
                    if (res.Victoria && res.VidaCarreteFinal == reglas.VidaCarrete)
                    {
                        ok++;
                        Debug.Log(sala.Id + " (" + sala.Nombre + "): OK, la solución gana sin daño en " + res.Turnos + " turnos.");
                    }
                    else Debug.LogError(sala.Id + ": la solución de referencia ya no gana sin daño (carrete " + res.VidaCarreteFinal + ").");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError(Path.GetFileName(ruta) + ": " + ex.Message);
                }
            }
            Debug.Log("Validación de salas: " + ok + " de " + total + " correctas.");
        }

        [MenuItem("Desenreda el Ovillo/Abrir carpeta de documentación", priority = 40)]
        public static void AbrirDocumentacion()
        {
            EditorUtility.RevealInFinder(Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Docs")));
        }
    }
}
