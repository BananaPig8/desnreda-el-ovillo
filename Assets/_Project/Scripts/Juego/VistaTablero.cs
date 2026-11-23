using System.Collections.Generic;
using DesenredaElOvillo.Nucleo;
using UnityEngine;

namespace DesenredaElOvillo.Juego
{
    /// <summary>Capas de dibujo (sortingOrder). Todo es 2D con SpriteRenderer.</summary>
    public static class Capas
    {
        public const int Mesa = -100;
        public const int Tapete = -50;
        public const int Casilla = 0;
        public const int Resalte = 4;
        public const int LanaSombra = 8;
        public const int Lana = 10;
        public const int Velcro = 14;
        public const int Huella = 16;
        public const int Herramienta = 20;
        public const int Obstaculo = 30;
        public const int Polilla = 40;
        public const int Ovillo = 55;
        public const int Efectos = 70;
    }

    /// <summary>Crea y mantiene la parte fija del tablero: mesa, tapete, casillas, objetos, entrada, carrete y Ovillo.</summary>
    public sealed class VistaTablero : MonoBehaviour
    {
        private readonly List<GameObject> _objetos = new List<GameObject>();
        private readonly List<SpriteRenderer> _brillos = new List<SpriteRenderer>();
        private SpriteRenderer _carrete;
        private SpriteRenderer _resalte;
        private Transform _ovillo;
        private SpriteRenderer _ovilloSprite;
        private float _sustoHasta;
        private float _saltoHasta;
        private Vector3 _ovilloBase;
        private float _temblorCarrete;

        public Tablero Tablero { get; private set; }
        public Transform Ovillo => _ovillo;
        public Vector3 PosicionCarrete => Tablero == null ? Vector3.zero : Mundo(Tablero.Salida);
        public Vector3 PosicionEntrada => Tablero == null ? Vector3.zero : Mundo(Tablero.Entrada);

        public static Vector3 Mundo(Celda c) => new Vector3(c.X + 0.5f, c.Y + 0.5f, 0f);

        public bool CeldaEn(Vector3 mundo, out Celda celda)
        {
            celda = new Celda(Mathf.FloorToInt(mundo.x), Mathf.FloorToInt(mundo.y));
            return Tablero != null && Tablero.Dentro(celda);
        }

        /// <summary>Rectángulo en mundo que la cámara debe encuadrar (tablero + Ovillo).</summary>
        public Rect Encuadre => Tablero == null ? new Rect(0, 0, 9, 9) : new Rect(-0.6f, -0.6f, Tablero.Ancho + 2.4f, Tablero.Alto + 1.2f);

        public void Construir(Tablero tablero, int vida, int vidaMaxima)
        {
            Limpiar();
            Tablero = tablero;

            var mesa = Crear("Mesa", Texturas.Mesa(), Capas.Mesa, new Vector3(tablero.Ancho / 2f, tablero.Alto / 2f, 0));
            mesa.transform.localScale = new Vector3((tablero.Ancho + 14) / 2f, (tablero.Alto + 10) / 2f, 1);

            var tapete = Crear("Tapete", null, Capas.Tapete, new Vector3(tablero.Ancho / 2f, tablero.Alto / 2f, 0));
            var sr = tapete.GetComponent<SpriteRenderer>();
            sr.sprite = Texturas.Casilla(1);
            sr.color = Paleta.MusgoOscuro;
            tapete.transform.localScale = new Vector3(tablero.Ancho + 0.5f, tablero.Alto + 0.5f, 1);

            for (int y = 0; y < tablero.Alto; y++)
            {
                for (int x = 0; x < tablero.Ancho; x++)
                {
                    var c = new Celda(x, y);
                    var tipo = tablero.Tipo(c);
                    var casilla = Crear("Casilla " + c, Texturas.Casilla((x + y) % 2), Capas.Casilla, Mundo(c));
                    float tinte = 0.94f + 0.08f * Pintor.Ruido(x * 1.7f, y * 1.3f, 12);
                    casilla.GetComponent<SpriteRenderer>().color = new Color(tinte, tinte, tinte, 1);
                    switch (tipo)
                    {
                        case TipoCasilla.Obstaculo:
                            Crear("Obstáculo " + c, Texturas.Obstaculo(tablero.Simbolo(c)), Capas.Obstaculo - y, Mundo(c) + new Vector3(0, 0.05f, 0));
                            break;
                        case TipoCasilla.Lampara:
                            var brillo = Crear("Brillo lámpara", Texturas.Brillo(), Capas.Resalte, Mundo(c));
                            brillo.transform.localScale = Vector3.one * 3.2f;
                            var bsr = brillo.GetComponent<SpriteRenderer>();
                            bsr.color = Paleta.ConAlfa(Paleta.Hex("#FFE8A0"), 0.45f);
                            _brillos.Add(bsr);
                            Crear("Lámpara " + c, Texturas.Lampara(), Capas.Obstaculo - y, Mundo(c) + new Vector3(0, 0.08f, 0));
                            break;
                        case TipoCasilla.Entrada:
                            Crear("Entrada", Texturas.Entrada(), Capas.Obstaculo - y, Mundo(c));
                            break;
                    }
                }
            }

            var carrete = Crear("Carrete", Texturas.Carrete(vida, vidaMaxima), Capas.Obstaculo + 5, Mundo(tablero.Salida) + new Vector3(0, 0.12f, 0));
            _carrete = carrete.GetComponent<SpriteRenderer>();

            var resalte = Crear("Resalte", Texturas.Casilla(0), Capas.Resalte, Vector3.zero);
            _resalte = resalte.GetComponent<SpriteRenderer>();
            _resalte.enabled = false;

            var ovillo = Crear("Ovillo", Texturas.Ovillo(), Capas.Ovillo, Vector3.zero);
            _ovillo = ovillo.transform;
            _ovilloSprite = ovillo.GetComponent<SpriteRenderer>();
            _ovilloBase = Mundo(tablero.Salida) + DireccionFuera(tablero) * 1.05f + new Vector3(0, -0.45f, 0);
            _ovillo.position = _ovilloBase;
            _ovilloSprite.flipX = DireccionFuera(tablero).x > 0;
        }

        private static Vector3 DireccionFuera(Tablero t)
        {
            var s = t.Salida;
            if (s.X == t.Ancho - 1) return Vector3.right;
            if (s.X == 0) return Vector3.left;
            if (s.Y == t.Alto - 1) return Vector3.up;
            return Vector3.down;
        }

        public void ActualizarCarrete(int vida, int vidaMaxima, bool golpe)
        {
            if (_carrete == null) return;
            _carrete.sprite = Texturas.Carrete(Mathf.Max(0, vida), vidaMaxima);
            if (golpe)
            {
                _temblorCarrete = 0.4f;
                Asustar(0.9f);
            }
        }

        public void Resaltar(Celda? celda, Color color)
        {
            if (_resalte == null) return;
            _resalte.enabled = celda.HasValue;
            if (!celda.HasValue) return;
            _resalte.transform.position = Mundo(celda.Value);
            _resalte.color = color;
        }

        public void Asustar(float segundos) => _sustoHasta = Time.time + segundos;
        public void Saltar(float segundos) => _saltoHasta = Time.time + segundos;

        private void Update()
        {
            if (_ovillo != null)
            {
                float t = Time.time;
                var pos = _ovilloBase;
                var escala = new Vector3(1, 1, 1);
                if (t < _sustoHasta)
                {
                    pos += new Vector3(Mathf.Sin(t * 60f) * 0.03f, 0, 0);
                }
                else if (t < _saltoHasta)
                {
                    float fase = Mathf.Abs(Mathf.Sin(t * 9f));
                    pos += new Vector3(0, fase * 0.35f, 0);
                    escala = new Vector3(1f + (1 - fase) * 0.08f, 1f - (1 - fase) * 0.08f, 1);
                }
                else
                {
                    float respira = Mathf.Sin(t * 2.2f);
                    escala = new Vector3(1f + respira * 0.015f, 1f - respira * 0.02f, 1);
                }
                _ovillo.position = pos;
                _ovillo.localScale = escala;
            }
            if (_carrete != null)
            {
                var basePos = Mundo(Tablero.Salida) + new Vector3(0, 0.12f, 0);
                if (_temblorCarrete > 0)
                {
                    _temblorCarrete -= Time.deltaTime;
                    basePos += new Vector3(Mathf.Sin(Time.time * 70f) * 0.06f * _temblorCarrete / 0.4f, 0, 0);
                }
                _carrete.transform.position = basePos;
            }
            foreach (var b in _brillos)
            {
                if (b == null) continue;
                float a = 0.38f + 0.1f * Mathf.Sin(Time.time * 2.5f);
                b.color = Paleta.ConAlfa(b.color, a);
            }
        }

        private GameObject Crear(string nombre, Sprite sprite, int orden, Vector3 posicion)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(transform, false);
            go.transform.position = posicion;
            var sr = go.AddComponent<SpriteRenderer>();
            Texturas.PrepararRenderer(sr);
            sr.sprite = sprite;
            sr.sortingOrder = orden;
            _objetos.Add(go);
            return go;
        }

        public void Limpiar()
        {
            foreach (var go in _objetos) if (go != null) Destroy(go);
            _objetos.Clear();
            _brillos.Clear();
            _carrete = null;
            _resalte = null;
            _ovillo = null;
            Tablero = null;
        }
    }
}
