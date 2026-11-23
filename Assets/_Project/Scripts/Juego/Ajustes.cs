using UnityEngine;

namespace DesenredaElOvillo.Juego
{
    /// <summary>Preferencias del jugador guardadas en PlayerPrefs.</summary>
    public static class Ajustes
    {
        private const string ClaveVolumen = "ovillo.volumen";
        private const string ClaveVelocidad = "ovillo.velocidad";
        private const string ClaveColorLana = "ovillo.colorLana";
        private const string ClaveModoFeria = "ovillo.modoFeria";
        private const string ClaveTutorial = "ovillo.tutorial";

        public static float Volumen
        {
            get => PlayerPrefs.GetFloat(ClaveVolumen, 0.8f);
            set { PlayerPrefs.SetFloat(ClaveVolumen, Mathf.Clamp01(value)); AudioListener.volume = Mathf.Clamp01(value); }
        }

        /// <summary>Multiplicador de la reproducción de oleadas (1, 2 o 4).</summary>
        public static int Velocidad
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(ClaveVelocidad, 1), 1, 4);
            set => PlayerPrefs.SetInt(ClaveVelocidad, value);
        }

        public static int ColorLana
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(ClaveColorLana, 0), 0, Paleta.ColoresLana.Length - 1);
            set => PlayerPrefs.SetInt(ClaveColorLana, value);
        }

        /// <summary>Modo feria: tras una derrota se repite la sala en vez de empezar la partida desde cero.</summary>
        public static bool ModoFeria
        {
            get => PlayerPrefs.GetInt(ClaveModoFeria, 1) == 1;
            set => PlayerPrefs.SetInt(ClaveModoFeria, value ? 1 : 0);
        }

        public static bool MostrarTutorial
        {
            get => PlayerPrefs.GetInt(ClaveTutorial, 1) == 1;
            set => PlayerPrefs.SetInt(ClaveTutorial, value ? 1 : 0);
        }

        public static void Aplicar()
        {
            AudioListener.volume = Volumen;
        }
    }
}
