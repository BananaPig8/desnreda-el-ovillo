using System.Collections.Generic;
using System.IO;
using DesenredaElOvillo.Juego;
using DesenredaElOvillo.Nucleo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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
            if (PlayerSettings.productName != "Desenreda el Ovillo")
            {
                PlayerSettings.productName = "Desenreda el Ovillo";
                PlayerSettings.defaultScreenWidth = 1920;
                PlayerSettings.defaultScreenHeight = 1080;
                PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
                PlayerSettings.resizableWindow = true;
                EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            }
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
