using UnityEngine;
using UnityEngine.Rendering;

namespace DesenredaElOvillo.Juego.Tres
{
    /// <summary>
    /// Iluminación de taller: luz principal cálida desde la ventana (arriba a la izquierda) con sombras suaves,
    /// relleno frío muy suave desde el lado contrario y luz ambiente de tres tonos (techo crema, paredes beige,
    /// suelo madera). Si el proyecto usa URP, PostprocesoURP añade el acabado de maqueta (desenfoque de fondo,
    /// bloom suave, viñeta y gradación cálida).
    /// </summary>
    public sealed class Iluminacion : MonoBehaviour
    {
        private Light _principal;
        private Light _relleno;

        public static Iluminacion Crear(Transform padre)
        {
            var go = new GameObject("Iluminación");
            go.transform.SetParent(padre, false);
            var i = go.AddComponent<Iluminacion>();
            i.Montar();
            return i;
        }

        private void Montar()
        {
            foreach (var luz in FindLuces()) if (luz.type == LightType.Directional) luz.enabled = false;

            _principal = NuevaLuz("Luz de la ventana", LightType.Directional, Paleta.Hex("#FFE6C4"), 1.25f);
            _principal.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            _principal.shadows = LightShadows.Soft;
            _principal.shadowStrength = 0.72f;
            _principal.shadowBias = 0.03f;
            _principal.shadowNormalBias = 0.25f;

            _relleno = NuevaLuz("Relleno", LightType.Directional, Paleta.Hex("#BFD3FF"), 0.22f);
            _relleno.transform.rotation = Quaternion.Euler(35f, 150f, 0f);
            _relleno.shadows = LightShadows.None;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Paleta.Hex("#E9D6BC");
            RenderSettings.ambientEquatorColor = Paleta.Hex("#B39477");
            RenderSettings.ambientGroundColor = Paleta.Hex("#5A3A24");
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Paleta.Hex("#3B2618");
            RenderSettings.fogStartDistance = 26f;
            RenderSettings.fogEndDistance = 55f;
            QualitySettings.shadowDistance = Mathf.Max(QualitySettings.shadowDistance, 40f);

#if OVILLO_URP
            PostprocesoURP.Montar(transform);
#endif
        }

        private static Light[] FindLuces()
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
#else
            return Object.FindObjectsOfType<Light>();
#endif
        }

        private Light NuevaLuz(string nombre, LightType tipo, Color color, float intensidad)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(transform, false);
            var l = go.AddComponent<Light>();
            l.type = tipo;
            l.color = color;
            l.intensity = intensidad;
            return l;
        }

        /// <summary>Luz cálida de una lámpara del tablero.</summary>
        public static Light LuzDeLampara(Transform padre, Vector3 posicion)
        {
            var go = new GameObject("Luz de lámpara");
            go.transform.SetParent(padre, false);
            go.transform.position = posicion;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = Paleta.Hex("#FFD99A");
            l.intensity = 1.6f;
            l.range = 2.6f;
            l.shadows = LightShadows.None;
            return l;
        }

        public void PrepararCamara(Camera camara)
        {
#if OVILLO_URP
            PostprocesoURP.PrepararCamara(camara);
#endif
        }
    }
}
