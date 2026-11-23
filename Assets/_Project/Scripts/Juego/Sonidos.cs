using System.Collections.Generic;
using UnityEngine;

namespace DesenredaElOvillo.Juego
{
    public enum Sonido
    {
        Lana, LanaAtras, Colocar, Quitar, Alfiler, Tijeras, Velcro, Bobina, Boton, Placa,
        Dano, Muere, Mordisco, Victoria, Derrota, Clic, Aparece, Reina, Deslumbra
    }

    /// <summary>
    /// Efectos de sonido provisionales sintetizados en tiempo real (sin archivos), para que el prototipo
    /// ya comunique cada acción. Si existe un AudioClip en Resources/Audio/&lt;Nombre&gt;, se usa ese.
    /// </summary>
    public sealed class Sonidos : MonoBehaviour
    {
        private const int Frecuencia = 44100;
        private readonly Dictionary<Sonido, AudioClip> _clips = new Dictionary<Sonido, AudioClip>();
        private AudioSource _fuente;
        private float _ultimoLana;

        public static Sonidos Instancia { get; private set; }

        private void Awake()
        {
            Instancia = this;
            _fuente = gameObject.AddComponent<AudioSource>();
            _fuente.playOnAwake = false;
        }

        public static void Tocar(Sonido s, float volumen = 1f, float tono = 1f)
        {
            if (Instancia == null) return;
            Instancia.Reproducir(s, volumen, tono);
        }

        private void Reproducir(Sonido s, float volumen, float tono)
        {
            if (s == Sonido.Lana || s == Sonido.LanaAtras)
            {
                if (Time.unscaledTime - _ultimoLana < 0.035f) return;
                _ultimoLana = Time.unscaledTime;
            }
            if (!_clips.TryGetValue(s, out var clip) || clip == null)
            {
                clip = Resources.Load<AudioClip>("Audio/" + s) ?? Sintetizar(s);
                _clips[s] = clip;
            }
            _fuente.pitch = tono * Random.Range(0.96f, 1.04f);
            _fuente.PlayOneShot(clip, volumen);
        }

        private static AudioClip Sintetizar(Sonido s)
        {
            switch (s)
            {
                case Sonido.Lana: return Crear("lana", 0.06f, (t, d) => Ruido(t) * 0.25f * Env(t, d, 0.005f));
                case Sonido.LanaAtras: return Crear("lanaAtras", 0.05f, (t, d) => Ruido(t) * 0.18f * Env(t, d, 0.004f));
                case Sonido.Colocar: return Crear("colocar", 0.12f, (t, d) => Seno(t, 520 - 180 * t / d) * 0.5f * Env(t, d, 0.004f));
                case Sonido.Quitar: return Crear("quitar", 0.1f, (t, d) => Seno(t, 380 + 200 * t / d) * 0.4f * Env(t, d, 0.004f));
                case Sonido.Clic: return Crear("clic", 0.05f, (t, d) => Seno(t, 900) * 0.35f * Env(t, d, 0.002f));
                case Sonido.Alfiler: return Crear("alfiler", 0.14f, (t, d) => (Seno(t, 1760) * 0.5f + Seno(t, 2640) * 0.25f) * Env(t, d, 0.001f));
                case Sonido.Tijeras:
                    return Crear("tijeras", 0.22f, (t, d) =>
                        (Ruido(t) * 0.6f + Seno(t, 3000) * 0.2f) * (Pulso(t, 0.0f, 0.03f) + Pulso(t, 0.1f, 0.04f)));
                case Sonido.Velcro: return Crear("velcro", 0.28f, (t, d) => Ruido(t) * 0.45f * (0.6f + 0.4f * Mathf.Sin(t * 260f)) * Env(t, d, 0.01f));
                case Sonido.Bobina: return Crear("bobina", 0.3f, (t, d) => Seno(t, 220 + 120 * Mathf.Sin(t * 40f)) * 0.4f * Env(t, d, 0.01f));
                case Sonido.Boton: return Crear("boton", 0.3f, (t, d) => Seno(t, 260 + 520 * Mathf.Exp(-t * 14f)) * 0.55f * Env(t, d, 0.003f));
                case Sonido.Placa: return Crear("placa", 0.18f, (t, d) => (Seno(t, 180) * 0.5f + Ruido(t) * 0.2f) * Env(t, d, 0.002f));
                case Sonido.Dano: return Crear("dano", 0.12f, (t, d) => Seno(t, 330 - 120 * t / d) * 0.45f * Env(t, d, 0.003f));
                case Sonido.Muere: return Crear("muere", 0.35f, (t, d) => Seno(t, 520 * Mathf.Exp(-t * 4f)) * 0.45f * Env(t, d, 0.005f) + Ruido(t) * 0.1f * Env(t, d, 0.001f));
                case Sonido.Mordisco: return Crear("mordisco", 0.25f, (t, d) => (Ruido(t) * 0.5f + Seno(t, 90) * 0.5f) * (Pulso(t, 0f, 0.07f) + Pulso(t, 0.12f, 0.08f)));
                case Sonido.Aparece: return Crear("aparece", 0.15f, (t, d) => Seno(t, 300 + 300 * t / d) * 0.25f * Env(t, d, 0.01f));
                case Sonido.Deslumbra: return Crear("deslumbra", 0.35f, (t, d) => (Seno(t, 1320) + Seno(t, 1660)) * 0.18f * Env(t, d, 0.01f));
                case Sonido.Reina: return Crear("reina", 0.7f, (t, d) => (Seno(t, 110) * 0.5f + Seno(t, 165) * 0.3f) * Env(t, d, 0.05f));
                case Sonido.Victoria: return Melodia("victoria", new[] { 523f, 659f, 784f, 1047f }, 0.13f);
                default: return Melodia("derrota", new[] { 392f, 330f, 262f, 196f }, 0.2f);
            }
        }

        private delegate float Onda(float t, float duracion);

        private static AudioClip Crear(string nombre, float duracion, Onda onda)
        {
            int n = Mathf.CeilToInt(duracion * Frecuencia);
            var datos = new float[n];
            for (int i = 0; i < n; i++) datos[i] = Mathf.Clamp(onda(i / (float)Frecuencia, duracion), -1f, 1f);
            var clip = AudioClip.Create(nombre, n, 1, Frecuencia, false);
            clip.SetData(datos, 0);
            return clip;
        }

        private static AudioClip Melodia(string nombre, float[] notas, float nota)
        {
            float duracion = nota * notas.Length + 0.25f;
            return Crear(nombre, duracion, (t, d) =>
            {
                int k = Mathf.Min(notas.Length - 1, (int)(t / nota));
                float local = t - k * nota;
                float largo = k == notas.Length - 1 ? nota + 0.25f : nota;
                return (Seno(t, notas[k]) * 0.4f + Seno(t, notas[k] * 2f) * 0.1f) * Env(local, largo, 0.01f);
            });
        }

        private static float Seno(float t, float f) => Mathf.Sin(2f * Mathf.PI * f * t);

        private static float Ruido(float t)
        {
            unchecked
            {
                uint h = (uint)(t * 1000003f) * 2654435761u;
                h ^= h >> 15;
                return (h & 0xFFFF) / 32767.5f - 1f;
            }
        }

        private static float Env(float t, float d, float ataque)
        {
            if (t < ataque) return t / ataque;
            return Mathf.Clamp01(1f - (t - ataque) / Mathf.Max(0.0001f, d - ataque));
        }

        private static float Pulso(float t, float inicio, float largo)
        {
            if (t < inicio || t > inicio + largo) return 0f;
            return 1f - (t - inicio) / largo;
        }
    }
}
