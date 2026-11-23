using UnityEngine;

namespace DesenredaElOvillo.Juego
{
    /// <summary>
    /// Cámara ortográfica que encuadra el tablero dentro de la zona central de la interfaz
    /// (la interfaz se diseña a 1920x1080 y se escala con bandas si la pantalla tiene otra proporción).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CamaraJuego : MonoBehaviour
    {
        public const float AnchoReferencia = 1920f;
        public const float AltoReferencia = 1080f;

        private static CamaraJuego _instancia;
        private Camera _camara;
        private Rect _mundo = new Rect(0, 0, 9, 9);
        private Rect _zona = new Rect(370, 140, 1180, 810);
        private float _sacudida;

        public Camera Camara => _camara;

        private void Awake()
        {
            _instancia = this;
            _camara = GetComponent<Camera>();
            _camara.orthographic = true;
            _camara.clearFlags = CameraClearFlags.SolidColor;
            _camara.backgroundColor = Paleta.Hex("#8C5A33");
            _camara.nearClipPlane = -50f;
            _camara.farClipPlane = 50f;
            transform.position = new Vector3(0, 0, -10);
        }

        /// <param name="mundo">Rectángulo del mundo que debe verse entero.</param>
        /// <param name="zonaReferencia">Zona de la pantalla (coordenadas 1920x1080, origen arriba a la izquierda).</param>
        public void Encuadrar(Rect mundo, Rect zonaReferencia)
        {
            _mundo = mundo;
            _zona = zonaReferencia;
        }

        public static void Sacudir(float intensidad)
        {
            if (_instancia != null) _instancia._sacudida = Mathf.Max(_instancia._sacudida, intensidad);
        }

        public static float EscalaInterfaz => Mathf.Min(Screen.width / AnchoReferencia, Screen.height / AltoReferencia);
        public static Vector2 DesplazamientoInterfaz => new Vector2((Screen.width - AnchoReferencia * EscalaInterfaz) / 2f, (Screen.height - AltoReferencia * EscalaInterfaz) / 2f);

        /// <summary>Convierte un punto de pantalla (píxeles, origen abajo) a coordenadas de referencia de la interfaz.</summary>
        public static Vector2 PantallaAReferencia(Vector2 pantalla)
        {
            float s = EscalaInterfaz;
            var d = DesplazamientoInterfaz;
            return new Vector2((pantalla.x - d.x) / s, (Screen.height - pantalla.y - d.y) / s);
        }

        public Vector2 MundoAReferencia(Vector3 mundo)
        {
            var p = _camara.WorldToScreenPoint(mundo);
            return PantallaAReferencia(new Vector2(p.x, p.y));
        }

        public Vector3 PantallaAMundo(Vector2 pantalla)
        {
            var p = _camara.ScreenToWorldPoint(new Vector3(pantalla.x, pantalla.y, 10f));
            p.z = 0;
            return p;
        }

        private void LateUpdate()
        {
            float s = EscalaInterfaz;
            var d = DesplazamientoInterfaz;
            float pw = _zona.width * s, ph = _zona.height * s;
            float cx = d.x + (_zona.x + _zona.width / 2f) * s;
            float cy = Screen.height - (d.y + (_zona.y + _zona.height / 2f) * s);
            float unidadesPorPixel = Mathf.Max(_mundo.width / Mathf.Max(1, pw), _mundo.height / Mathf.Max(1, ph));
            _camara.orthographicSize = unidadesPorPixel * Screen.height / 2f;
            var centroMundo = _mundo.center;
            var desplazamiento = new Vector2(cx - Screen.width / 2f, cy - Screen.height / 2f) * unidadesPorPixel;
            var pos = new Vector3(centroMundo.x - desplazamiento.x, centroMundo.y - desplazamiento.y, -10f);
            if (_sacudida > 0)
            {
                _sacudida -= Time.deltaTime;
                pos += (Vector3)(Random.insideUnitCircle * _sacudida * 0.25f);
            }
            transform.position = pos;
        }
    }
}
