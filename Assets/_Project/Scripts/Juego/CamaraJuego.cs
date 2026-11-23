using UnityEngine;

namespace DesenredaElOvillo.Juego
{
    /// <summary>
    /// Cámara de maqueta: perspectiva con poco ángulo de visión (sensación de miniatura), inclinada ~55°
    /// para que todo el tablero se lea sin oclusiones. El encuadre se calcula para que el tablero ocupe
    /// exactamente la zona central de la interfaz (diseñada a 1920x1080, escalada con bandas), usando una
    /// proyección descentrada. Rueda: zum. Q/E: girar un poco la maqueta para verla en 3D.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CamaraJuego : MonoBehaviour
    {
        public const float AnchoReferencia = 1920f;
        public const float AltoReferencia = 1080f;

        private static CamaraJuego _instancia;
        private Camera _camara;
        private Bounds _mundo = new Bounds(new Vector3(4.5f, 0, 4.5f), new Vector3(9, 1, 9));
        private Rect _zona = new Rect(372, 140, 1176, 806);
        private float _sacudida;
        private float _zoom = 1f, _zoomObjetivo = 1f;
        private float _giro, _giroObjetivo;

        public float Inclinacion = 55f;
        public float CampoDeVision = 30f;
        public Camera Camara => _camara;

        private void Awake()
        {
            _instancia = this;
            _camara = GetComponent<Camera>();
            _camara.orthographic = false;
            _camara.fieldOfView = CampoDeVision;
            _camara.nearClipPlane = 0.3f;
            _camara.farClipPlane = 120f;
            _camara.clearFlags = CameraClearFlags.SolidColor;
            _camara.backgroundColor = Paleta.Hex("#3B2618");
        }

        public void Encuadrar(Bounds mundo, Rect zonaReferencia)
        {
            _mundo = mundo;
            _zona = zonaReferencia;
        }

        public void Zoom(float delta) => _zoomObjetivo = Mathf.Clamp(_zoomObjetivo - delta * 0.08f, 0.7f, 1.25f);
        public void Girar(float grados) => _giroObjetivo = Mathf.Clamp(_giroObjetivo + grados, -30f, 30f);
        public void ReiniciarVista()
        {
            _giroObjetivo = 0;
            _zoomObjetivo = 1;
        }

        public static void Sacudir(float intensidad)
        {
            if (_instancia != null) _instancia._sacudida = Mathf.Max(_instancia._sacudida, intensidad);
        }

        public static float EscalaInterfaz => Mathf.Min(Screen.width / AnchoReferencia, Screen.height / AltoReferencia);
        public static Vector2 DesplazamientoInterfaz => new Vector2((Screen.width - AnchoReferencia * EscalaInterfaz) / 2f, (Screen.height - AltoReferencia * EscalaInterfaz) / 2f);

        /// <summary>Punto de pantalla (píxeles, origen abajo) a coordenadas de referencia de la interfaz (origen arriba).</summary>
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

        public Ray RayoDesdePantalla(Vector2 pantalla) => _camara.ScreenPointToRay(new Vector3(pantalla.x, pantalla.y, 0f));

        private void LateUpdate()
        {
            _zoom = Mathf.Lerp(_zoom, _zoomObjetivo, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 10f));
            _giro = Mathf.Lerp(_giro, _giroObjetivo, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 8f));

            float s = EscalaInterfaz;
            var d = DesplazamientoInterfaz;
            float sw = Mathf.Max(1, Screen.width), sh = Mathf.Max(1, Screen.height);
            float zonaW = _zona.width * s / sw, zonaH = _zona.height * s / sh;
            float cx = (d.x + (_zona.x + _zona.width / 2f) * s) / sw;
            float cy = 1f - (d.y + (_zona.y + _zona.height / 2f) * s) / sh;

            var rot = Quaternion.Euler(Inclinacion, _giro, 0f);
            var adelante = rot * Vector3.forward;
            var derecha = rot * Vector3.right;
            var arriba = rot * Vector3.up;
            float tanY = Mathf.Tan(CampoDeVision * 0.5f * Mathf.Deg2Rad);
            float aspecto = sw / sh;

            // Distancia mínima para que las 8 esquinas quepan en la zona (búsqueda binaria).
            float lo = 1f, hi = 200f;
            for (int i = 0; i < 30; i++)
            {
                float dist = (lo + hi) / 2f;
                if (Cabe(dist, adelante, derecha, arriba, tanY, aspecto, zonaW, zonaH)) hi = dist;
                else lo = dist;
            }
            float distancia = hi * _zoom;
            var pos = _mundo.center - adelante * distancia;
            if (_sacudida > 0)
            {
                _sacudida -= Time.unscaledDeltaTime;
                pos += (derecha * Random.Range(-1f, 1f) + arriba * Random.Range(-1f, 1f)) * _sacudida * 0.12f;
            }
            transform.SetPositionAndRotation(pos, rot);

            // Proyección descentrada: el eje de la cámara cae en el centro de la zona del tablero.
            var p = Matrix4x4.Perspective(CampoDeVision, aspecto, _camara.nearClipPlane, _camara.farClipPlane);
            p[0, 2] = 1f - 2f * cx;
            p[1, 2] = 1f - 2f * cy;
            _camara.projectionMatrix = p;
        }

        private bool Cabe(float dist, Vector3 adelante, Vector3 derecha, Vector3 arriba, float tanY, float aspecto, float zonaW, float zonaH)
        {
            var centro = _mundo.center;
            var e = _mundo.extents;
            var camara = centro - adelante * dist;
            for (int i = 0; i < 8; i++)
            {
                var esquina = centro + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
                var v = esquina - camara;
                float z = Vector3.Dot(v, adelante);
                if (z <= 0.1f) return false;
                float x = Vector3.Dot(v, derecha) / (z * tanY * aspecto);
                float y = Vector3.Dot(v, arriba) / (z * tanY);
                if (Mathf.Abs(x) > zonaW || Mathf.Abs(y) > zonaH) return false;
            }
            return true;
        }
    }
}
