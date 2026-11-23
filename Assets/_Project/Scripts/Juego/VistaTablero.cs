using System.Collections.Generic;
using DesenredaElOvillo.Juego.Tres;
using DesenredaElOvillo.Nucleo;
using UnityEngine;

namespace DesenredaElOvillo.Juego
{
    /// <summary>
    /// El diorama fijo de la sala: mesa de madera, tapete acolchado, casillas de fieltro con volumen,
    /// objetos del taller, agujero del rodapié, lámparas, el carrete (cuyo hilo es la vida) y Ovillo.
    /// El tablero ocupa el plano XZ: casilla (x, y) → mundo (x + 0.5, altura, y + 0.5).
    /// </summary>
    public sealed class VistaTablero : MonoBehaviour
    {
        private readonly List<GameObject> _objetos = new List<GameObject>();
        private readonly MaterialPropertyBlock _bloque = new MaterialPropertyBlock();
        private Transform[] _casillas;
        private Renderer[] _rendererCasillas;
        private Color[] _colorCasillas;
        private float[] _elevacion;
        private Transform _carrete;
        private int _vidaCarrete = -1;
        private float _temblorCarrete;
        private Celda? _resaltada;
        private Color _colorResalte;
        private readonly List<Light> _lamparas = new List<Light>();

        public Tablero Tablero { get; private set; }
        public VistaOvillo Ovillo { get; private set; }

        /// <summary>Centro de la cara superior de una casilla.</summary>
        public static Vector3 Mundo(Celda c) => new Vector3(c.X + 0.5f, Medidas.AlturaCasilla, c.Y + 0.5f);

        public Vector3 PosicionCarrete => Tablero == null ? Vector3.zero : Mundo(Tablero.Salida) + Vector3.up * 0.45f;
        public Vector3 PosicionEntrada => Tablero == null ? Vector3.zero : Mundo(Tablero.Entrada);

        /// <summary>Volumen que la cámara debe encuadrar (tablero + Ovillo + la altura de los objetos).</summary>
        public Bounds Encuadre
        {
            get
            {
                if (Tablero == null) return new Bounds(new Vector3(4.5f, 0.3f, 4.5f), new Vector3(10, 1, 10));
                var fuera = DireccionFuera(Tablero, Tablero.Salida);
                var b = new Bounds(new Vector3(Tablero.Ancho / 2f, 0.35f, Tablero.Alto / 2f), new Vector3(Tablero.Ancho + 0.4f, 0.9f, Tablero.Alto + 0.4f));
                b.Encapsulate(Mundo(Tablero.Salida) + fuera * 1.6f + Vector3.up * 0.9f);
                return b;
            }
        }

        public bool CeldaBajo(Ray rayo, out Celda celda)
        {
            celda = default;
            var plano = new Plane(Vector3.up, new Vector3(0, Medidas.AlturaCasilla, 0));
            if (Tablero == null || !plano.Raycast(rayo, out float t)) return false;
            var p = rayo.GetPoint(t);
            celda = new Celda(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.z));
            return Tablero.Dentro(celda);
        }

        public static Vector3 DireccionFuera(Tablero t, Celda c)
        {
            if (c.X == t.Ancho - 1) return Vector3.right;
            if (c.X == 0) return Vector3.left;
            if (c.Y == t.Alto - 1) return Vector3.forward;
            return Vector3.back;
        }

        public void Construir(Tablero tablero, int vida, int vidaMaxima)
        {
            Limpiar();
            Tablero = tablero;
            int w = tablero.Ancho, h = tablero.Alto;

            Instanciar(new Pieza { Nombre = "Mesa", Malla = CatalogoMallas.Caja(w + 16f, 0.5f, h + 14f, 0.08f), Material = "mesa", Color = Color.white }, new Vector3(w / 2f, -0.33f, h / 2f + 2f));
            Instanciar(new Pieza { Nombre = "Tapete", Malla = CatalogoMallas.Caja(w + 0.55f, 0.08f, h + 0.55f, 0.04f), Material = "fieltro", Color = Paleta.MusgoOscuro }, new Vector3(w / 2f, -0.04f, h / 2f));

            int n = w * h;
            _casillas = new Transform[n];
            _rendererCasillas = new Renderer[n];
            _colorCasillas = new Color[n];
            _elevacion = new float[n];
            var mallaCasilla = CatalogoMallas.Caja(0.92f, Medidas.AlturaCasilla, 0.92f, 0.05f);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var c = new Celda(x, y);
                    int i = y * w + x;
                    float variacion = Pintor.Ruido(x * 1.7f, y * 1.3f, 12);
                    var color = Color.Lerp(Paleta.Musgo, Paleta.MusgoClaro, variacion * 0.45f + ((x + y) % 2) * 0.08f);
                    var casilla = Instanciar(new Pieza { Nombre = "Casilla " + c, Malla = mallaCasilla, Material = "casilla", Color = color },
                        new Vector3(x + 0.5f, Medidas.AlturaCasilla / 2f, y + 0.5f));
                    casilla.transform.localRotation = Quaternion.Euler(0, (variacion - 0.5f) * 3f, 0);
                    _casillas[i] = casilla.transform;
                    _rendererCasillas[i] = casilla.GetComponent<Renderer>();
                    _colorCasillas[i] = color;

                    var tipo = tablero.Tipo(c);
                    float giro = (Pintor.Ruido(x * 3.1f, y * 2.7f, 5) - 0.5f) * 40f;
                    if (tipo == TipoCasilla.Obstaculo)
                    {
                        var o = Instanciar(Modelos.Obstaculo(tablero.Simbolo(c)), Mundo(c));
                        o.transform.localRotation = Quaternion.Euler(0, tablero.Simbolo(c) == '#' ? giro * 0.15f : giro, 0);
                    }
                    else if (tipo == TipoCasilla.Lampara)
                    {
                        Instanciar(Modelos.Lampara(), Mundo(c));
                        _lamparas.Add(Iluminacion.LuzDeLampara(transform, Mundo(c) + Vector3.up * 0.5f));
                    }
                    else if (tipo == TipoCasilla.Entrada)
                    {
                        Instanciar(Modelos.Entrada(DireccionFuera(tablero, c)), Mundo(c));
                    }
                }
            }

            ActualizarCarrete(vida, vidaMaxima, false);

            var fueraSalida = DireccionFuera(tablero, tablero.Salida);
            Ovillo = VistaOvillo.Crear(transform, Mundo(tablero.Salida) + fueraSalida * 1.05f + Vector3.back * 0.25f - Vector3.up * (Medidas.AlturaCasilla + 0.08f));
            _objetos.Add(Ovillo.gameObject);

            foreach (var d in Decorado.Disponer(w, h, 7))
            {
                var go = Instanciar(d.Pieza, d.Posicion + Vector3.up * -0.08f);
                go.transform.localRotation = Quaternion.Euler(0, d.Giro, 0) * go.transform.localRotation;
            }
        }

        private GameObject Instanciar(Pieza pieza, Vector3 posicion)
        {
            var modelo = Constructor.Instanciar(pieza, transform);
            modelo.Raiz.position = posicion;
            _objetos.Add(modelo.Raiz.gameObject);
            return modelo.Raiz.gameObject;
        }

        public void ActualizarCarrete(int vida, int vidaMaxima, bool golpe)
        {
            if (Tablero == null) return;
            vida = Mathf.Max(0, vida);
            if (vida != _vidaCarrete || _carrete == null)
            {
                if (_carrete != null)
                {
                    _objetos.Remove(_carrete.gameObject);
                    Destroy(_carrete.gameObject);
                }
                var modelo = Constructor.Instanciar(Modelos.Carrete(vidaMaxima <= 0 ? 0 : (float)vida / vidaMaxima), transform);
                _carrete = modelo.Raiz;
                _carrete.position = Mundo(Tablero.Salida);
                _objetos.Add(_carrete.gameObject);
                _vidaCarrete = vida;
            }
            if (golpe)
            {
                _temblorCarrete = 0.45f;
                if (Ovillo != null) Ovillo.Asustar(0.9f);
            }
        }

        public void Resaltar(Celda? celda, Color color)
        {
            _resaltada = celda;
            _colorResalte = color;
        }

        public void Asustar(float s) { if (Ovillo != null) Ovillo.Asustar(s); }
        public void Saltar(float s) { if (Ovillo != null) Ovillo.Saltar(s); }

        private void Update()
        {
            if (Tablero == null || _casillas == null) return;
            float dt = Time.deltaTime;
            for (int i = 0; i < _casillas.Length; i++)
            {
                var c = new Celda(i % Tablero.Ancho, i / Tablero.Ancho);
                bool activa = _resaltada.HasValue && _resaltada.Value == c;
                float objetivo = activa ? 0.05f : 0f;
                float antes = _elevacion[i];
                _elevacion[i] = Mathf.MoveTowards(_elevacion[i], objetivo, dt * 0.6f);
                if (Mathf.Abs(antes - _elevacion[i]) > 1e-5f || activa)
                {
                    var t = _casillas[i];
                    t.position = new Vector3(t.position.x, Medidas.AlturaCasilla / 2f + _elevacion[i], t.position.z);
                    var col = activa ? Color.Lerp(_colorCasillas[i], new Color(_colorResalte.r, _colorResalte.g, _colorResalte.b), _colorResalte.a) : _colorCasillas[i];
                    Materiales.Tenir(_rendererCasillas[i], col, _bloque);
                }
                else if (_elevacion[i] <= 0f && antes > 0f)
                {
                    Materiales.Tenir(_rendererCasillas[i], _colorCasillas[i], _bloque);
                }
            }
            if (_carrete != null)
            {
                var basePos = Mundo(Tablero.Salida);
                if (_temblorCarrete > 0)
                {
                    _temblorCarrete -= dt;
                    basePos += new Vector3(Mathf.Sin(Time.time * 70f) * 0.05f * _temblorCarrete / 0.45f, 0, 0);
                }
                _carrete.position = basePos;
            }
            foreach (var l in _lamparas) if (l != null) l.intensity = 1.5f + Mathf.Sin(Time.time * 2.3f) * 0.08f + Mathf.Sin(Time.time * 7.1f) * 0.04f;
        }

        public void Limpiar()
        {
            foreach (var go in _objetos) if (go != null) Destroy(go);
            foreach (var l in _lamparas) if (l != null) Destroy(l.gameObject);
            _objetos.Clear();
            _lamparas.Clear();
            _carrete = null;
            _vidaCarrete = -1;
            _casillas = null;
            Ovillo = null;
            Tablero = null;
        }
    }
}
