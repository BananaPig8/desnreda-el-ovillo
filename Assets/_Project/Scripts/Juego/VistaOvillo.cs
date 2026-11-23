using DesenredaElOvillo.Juego.Tres;
using UnityEngine;

namespace DesenredaElOvillo.Juego
{
    /// <summary>
    /// Ovillo en el diorama: respira, parpadea, mira a la polilla más cercana, se asusta cuando muerden el carrete,
    /// salta cuando cae una polilla y celebra la victoria. Animación procedural con un toque "stop motion"
    /// (la pose se actualiza a 12 imágenes por segundo, como una maqueta animada a mano).
    /// </summary>
    public sealed class VistaOvillo : MonoBehaviour
    {
        private const float ImagenesPorSegundo = 12f;
        private ModeloInstanciado _modelo;
        private Transform _cuerpo;
        private Transform _cola;
        private Transform _brazoIzq, _brazoDer;
        private Vector3 _base;
        private float _sustoHasta, _saltoHasta, _celebrarHasta, _tristeHasta;
        private float _siguienteParpadeo;
        private float _siguienteImagen;
        private Vector3? _mirarA;

        public static VistaOvillo Crear(Transform padre, Vector3 posicion)
        {
            var go = new GameObject("Ovillo (personaje)");
            go.transform.SetParent(padre, false);
            go.transform.position = posicion;
            go.transform.rotation = Quaternion.Euler(0, -25f, 0);
            go.transform.localScale = Vector3.one * 1.3f;
            var v = go.AddComponent<VistaOvillo>();
            v._modelo = Constructor.Instanciar(Modelos.Ovillo(), go.transform);
            v._cuerpo = v._modelo.Primero("cuerpo");
            v._cola = v._modelo.Primero("cola_bufanda");
            v._brazoIzq = v._modelo.Primero("brazo_izq");
            v._brazoDer = v._modelo.Primero("brazo_der");
            v._base = posicion;
            v._siguienteParpadeo = Time.time + 2f;
            return v;
        }

        public void Asustar(float s) => _sustoHasta = Time.time + s;
        public void Saltar(float s) => _saltoHasta = Time.time + s;
        public void Celebrar(float s = 30f) => _celebrarHasta = Time.time + s;
        public void Entristecer(float s = 30f) => _tristeHasta = Time.time + s;
        public void MirarA(Vector3? objetivo) => _mirarA = objetivo;

        private void Update()
        {
            if (_cuerpo == null) return;
            // Pose a saltos de 1/12 s: el encanto de la animación de maqueta.
            if (Time.time < _siguienteImagen) return;
            _siguienteImagen = Time.time + 1f / ImagenesPorSegundo;
            float t = Time.time;

            var pos = _base;
            var escala = Vector3.one;
            float giro = -25f;
            float brazos = 0f;

            if (t < _celebrarHasta)
            {
                float fase = Mathf.Abs(Mathf.Sin(t * 6f));
                pos += Vector3.up * fase * 0.45f;
                giro += Mathf.Repeat(t * 240f, 360f);
                brazos = 60f;
                escala = new Vector3(1f + (1 - fase) * 0.1f, 1f - (1 - fase) * 0.1f, 1f + (1 - fase) * 0.1f);
            }
            else if (t < _tristeHasta)
            {
                escala = new Vector3(1.08f, 0.88f, 1.08f);
                brazos = -30f;
                giro = 0f;
            }
            else if (t < _sustoHasta)
            {
                pos += new Vector3(Mathf.Sin(t * 50f) * 0.03f, 0, 0);
                brazos = 70f;
                escala = new Vector3(0.95f, 1.06f, 0.95f);
            }
            else if (t < _saltoHasta)
            {
                float fase = Mathf.Abs(Mathf.Sin(t * 9f));
                pos += Vector3.up * fase * 0.3f;
                brazos = 45f;
                escala = new Vector3(1f + (1 - fase) * 0.08f, 1f - (1 - fase) * 0.08f, 1f + (1 - fase) * 0.08f);
            }
            else
            {
                float respira = Mathf.Sin(t * 2.2f);
                escala = new Vector3(1f + respira * 0.015f, 1f - respira * 0.02f, 1f + respira * 0.015f);
                if (_mirarA.HasValue)
                {
                    var dir = _mirarA.Value - transform.position;
                    dir.y = 0;
                    if (dir.sqrMagnitude > 0.01f) giro = Mathf.Clamp(Mathf.Atan2(-dir.x, -dir.z) * Mathf.Rad2Deg, -60f, 30f);
                }
            }

            transform.position = pos;
            transform.rotation = Quaternion.Euler(0, giro, 0);
            _cuerpo.localScale = escala;
            if (_brazoIzq != null) _brazoIzq.localRotation = Quaternion.Euler(0, 0, -brazos);
            if (_brazoDer != null) _brazoDer.localRotation = Quaternion.Euler(0, 0, brazos);
            if (_cola != null) _cola.localRotation = Quaternion.Euler(Mathf.Sin(t * 3f) * 8f, 0, -20f + Mathf.Sin(t * 2.3f) * 10f);

            bool parpadea = t > _siguienteParpadeo && t < _siguienteParpadeo + 0.12f;
            if (t > _siguienteParpadeo + 0.12f) _siguienteParpadeo = t + Random.Range(2.5f, 5f);
            foreach (var ojo in _modelo.Grupo("ojos"))
                ojo.localScale = new Vector3(ojo.localScale.x, parpadea ? 0.012f : 0.1f, ojo.localScale.z);
        }
    }
}
