using UnityEngine;

namespace DesenredaElOvillo.Juego
{
    /// <summary>
    /// Arranque automático: al pulsar Play en cualquier escena, si no hay un ControladorJuego, se crea uno.
    /// Así el prototipo funciona nada más abrir el proyecto, sin tener que montar escenas ni prefabs.
    /// (El menú "Desenreda el Ovillo &gt; Crear escena de juego" crea además una escena dedicada para las builds.)
    /// </summary>
    public static class Arranque
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Iniciar()
        {
            if (ControladorJuego.Instancia != null) return;
            var go = new GameObject("Desenreda el Ovillo");
            go.AddComponent<ControladorJuego>();
        }
    }
}
