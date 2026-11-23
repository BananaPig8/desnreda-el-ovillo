using DesenredaElOvillo.Juego.Tres;
using UnityEngine;

namespace DesenredaElOvillo.Juego
{
    /// <summary>Bolitas de lana que saltan al derrotar a una polilla, romper una placa o morder el carrete.</summary>
    public sealed class Pelusa : MonoBehaviour
    {
        private static readonly Color[] ColoresPorDefecto = { Paleta.Crema, Paleta.RosaPastel, Paleta.Morado, Paleta.Mostaza };
        private Vector3 _velocidad;
        private float _vida;

        public static void Soltar(Vector3 origen, int cantidad, Color[] colores = null)
        {
            colores = colores ?? ColoresPorDefecto;
            for (int i = 0; i < cantidad; i++)
            {
                var m = Constructor.Instanciar(new Pieza { Nombre = "Pelusa", Malla = "esfera_baja", Material = "lana", Color = colores[i % colores.Length], Escala = Vector3.one * Random.Range(0.05f, 0.09f), ProyectaSombra = false }, null);
                m.Raiz.position = origen;
                var p = m.Raiz.gameObject.AddComponent<Pelusa>();
                p._velocidad = new Vector3(Random.Range(-1.6f, 1.6f), Random.Range(1.5f, 3f), Random.Range(-1.6f, 1.6f));
                p._vida = Random.Range(0.6f, 0.9f);
            }
        }

        private void Update()
        {
            _vida -= Time.deltaTime;
            _velocidad += Vector3.down * 7f * Time.deltaTime;
            transform.position += _velocidad * Time.deltaTime;
            transform.localScale *= 1f - Time.deltaTime * 0.8f;
            if (_vida <= 0) Destroy(gameObject);
        }
    }

    /// <summary>
    /// Confeti de fieltro: recortes de colores que saltan, giran como hojas al caer, se quedan un rato en la mesa
    /// y desaparecen. Para superar una sala y, a lo grande, para la victoria.
    /// </summary>
    public sealed class Confeti : MonoBehaviour
    {
        private static readonly Color[] Colores = { Paleta.Coral, Paleta.Mostaza, Paleta.AzulSuave, Paleta.RosaPastel, Paleta.Crema, Paleta.Hex("#8DBF6A"), Paleta.Morado };
        private Vector3 _velocidad, _giro;
        private float _suelo, _vida, _enSuelo = -1f;

        public static void Lanzar(Vector3 origen, int cantidad, float suelo, float fuerza = 1f)
        {
            for (int i = 0; i < cantidad; i++)
            {
                bool redondo = i % 3 == 0;
                var malla = redondo ? CatalogoMallas.Cilindro(0.04f, 0.04f, 0.012f) : CatalogoMallas.Caja(0.085f, 0.012f, 0.055f, 0.004f);
                var m = Constructor.Instanciar(new Pieza { Nombre = "Confeti", Malla = malla, Material = "fieltro", Color = Colores[i % Colores.Length], ProyectaSombra = false }, null);
                m.Raiz.position = origen + Random.insideUnitSphere * 0.15f;
                m.Raiz.rotation = Random.rotation;
                var c = m.Raiz.gameObject.AddComponent<Confeti>();
                float angulo = Random.Range(0f, Mathf.PI * 2f);
                float abierto = Random.Range(0.6f, 2.2f) * fuerza;
                c._velocidad = new Vector3(Mathf.Cos(angulo) * abierto, Random.Range(2.6f, 4.6f) * fuerza, Mathf.Sin(angulo) * abierto);
                c._giro = Random.insideUnitSphere * 720f;
                c._suelo = suelo;
                c._vida = Random.Range(2.5f, 4f);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _vida -= dt;
            if (_enSuelo < 0f)
            {
                // caída de papel: gravedad suave, mucho rozamiento y un balanceo lateral
                _velocidad += Vector3.down * 6f * dt;
                _velocidad *= 1f - Mathf.Min(1f, 1.6f * dt);
                var p = transform.position + (_velocidad + new Vector3(Mathf.Sin(Time.time * 7f + _giro.x), 0f, Mathf.Cos(Time.time * 6f + _giro.z)) * 0.25f) * dt;
                transform.Rotate(_giro * dt, Space.Self);
                if (p.y <= _suelo + 0.008f && _velocidad.y < 0f)
                {
                    p.y = _suelo + 0.008f;
                    _enSuelo = 0f;
                    transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                }
                transform.position = p;
            }
            else _enSuelo += dt;
            if (_vida < 0.5f) transform.localScale = Vector3.one * Mathf.Max(0f, _vida / 0.5f);
            if (_vida <= 0f) Destroy(gameObject);
        }
    }
}
