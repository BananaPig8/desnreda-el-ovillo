using System.Collections.Generic;
using DesenredaElOvillo.Nucleo;
using UnityEngine;

namespace DesenredaElOvillo.Juego
{
    /// <summary>
    /// Dibuja la hebra de lana casilla a casilla con piezas rectas, curvas y puntas, cada una con su sombra,
    /// y las huellas donde aterrizará la polilla veloz.
    /// </summary>
    public sealed class VistaLana : MonoBehaviour
    {
        private readonly List<SpriteRenderer> _piezas = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> _sombras = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> _huellas = new List<SpriteRenderer>();
        private int _ultimaCantidad;
        private float _latido;

        public Color ColorLana => Paleta.ColoresLana[Ajustes.ColorLana];

        public void Dibujar(Recorrido r)
        {
            int n = r == null ? 0 : r.Cantidad;
            while (_piezas.Count < n)
            {
                _sombras.Add(CrearRenderer("Sombra lana", Capas.LanaSombra));
                _piezas.Add(CrearRenderer("Lana", Capas.Lana));
            }
            if (n > _ultimaCantidad) _latido = 0.25f;
            _ultimaCantidad = n;
            for (int i = 0; i < _piezas.Count; i++)
            {
                bool visible = i < n && n > 1;
                _piezas[i].enabled = visible;
                _sombras[i].enabled = visible;
                if (!visible) continue;
                var c = r.Celdas[i];
                int mascara = 0;
                if (i > 0) mascara |= Bit(r.Celdas[i - 1].Menos(c));
                if (i < n - 1) mascara |= Bit(r.Celdas[i + 1].Menos(c));
                var sprite = Texturas.PiezaLana(mascara);
                var pos = VistaTablero.Mundo(c);
                _piezas[i].sprite = sprite;
                _piezas[i].color = ColorLana;
                _piezas[i].transform.position = pos;
                _sombras[i].sprite = sprite;
                _sombras[i].color = new Color(0.12f, 0.08f, 0.04f, 0.35f);
                _sombras[i].transform.position = pos + new Vector3(0.05f, -0.07f, 0);
            }
        }

        private static int Bit(Celda d)
        {
            if (d.X == 1) return Texturas.Derecha;
            if (d.X == -1) return Texturas.Izquierda;
            if (d.Y == 1) return Texturas.Arriba;
            return Texturas.Abajo;
        }

        public void MostrarHuellas(Recorrido r, IList<int> indices)
        {
            int n = indices == null ? 0 : indices.Count;
            while (_huellas.Count < n) _huellas.Add(CrearRenderer("Huella", Capas.Huella));
            for (int i = 0; i < _huellas.Count; i++)
            {
                bool visible = i < n;
                _huellas[i].enabled = visible;
                if (!visible) continue;
                _huellas[i].sprite = Texturas.Huella();
                _huellas[i].transform.position = VistaTablero.Mundo(r.Celdas[indices[i]]) + new Vector3(0.28f, 0.28f, 0);
                _huellas[i].transform.localScale = Vector3.one * 0.42f;
            }
        }

        private void Update()
        {
            if (_latido <= 0 || _ultimaCantidad == 0 || _ultimaCantidad > _piezas.Count) return;
            _latido -= Time.deltaTime;
            float k = 1f + Mathf.Max(0, _latido) * 0.6f;
            var ultima = _piezas[_ultimaCantidad - 1];
            if (ultima != null) ultima.transform.localScale = new Vector3(k, k, 1);
        }

        private SpriteRenderer CrearRenderer(string nombre, int orden)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            Texturas.PrepararRenderer(sr);
            sr.sortingOrder = orden;
            return sr;
        }

        public void Limpiar()
        {
            foreach (var p in _piezas) if (p != null) Destroy(p.gameObject);
            foreach (var p in _sombras) if (p != null) Destroy(p.gameObject);
            foreach (var p in _huellas) if (p != null) Destroy(p.gameObject);
            _piezas.Clear();
            _sombras.Clear();
            _huellas.Clear();
            _ultimaCantidad = 0;
        }
    }
}
