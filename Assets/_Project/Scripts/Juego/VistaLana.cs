using System.Collections.Generic;
using DesenredaElOvillo.Juego.Tres;
using DesenredaElOvillo.Nucleo;
using UnityEngine;
using UnityEngine.Rendering;

namespace DesenredaElOvillo.Juego
{
    /// <summary>
    /// La hebra de lana en 3D: un tubo grueso con torsión (textura helicoidal y mapa de normales) que sigue
    /// el recorrido con curvas suaves, apoyado sobre el fieltro y proyectando sombra. Cuando el jugador
    /// teje, el último tramo crece en lugar de aparecer de golpe. También coloca las huellas de la veloz.
    /// </summary>
    public sealed class VistaLana : MonoBehaviour
    {
        private const float DuracionCrecimiento = 0.12f;
        private MeshFilter _filtro;
        private MeshRenderer _renderer;
        private Mesh _malla;
        private readonly MaterialPropertyBlock _bloque = new MaterialPropertyBlock();
        private readonly List<Vector3> _centros = new List<Vector3>();
        private readonly List<GameObject> _huellas = new List<GameObject>();
        private int _cantidadAnterior;
        private float _crecimiento = 1f;

        public Color ColorLana => Paleta.ColoresLana[Ajustes.ColorLana];

        private void Asegurar()
        {
            if (_filtro != null) return;
            var go = new GameObject("Hebra de lana");
            go.transform.SetParent(transform, false);
            _filtro = go.AddComponent<MeshFilter>();
            _renderer = go.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = Materiales.Obtener("lana");
            _renderer.shadowCastingMode = ShadowCastingMode.On;
            _malla = new Mesh { name = "Lana" };
            _malla.MarkDynamic();
            _filtro.sharedMesh = _malla;
        }

        public void Dibujar(Recorrido r)
        {
            Asegurar();
            _centros.Clear();
            if (r != null)
                foreach (var c in r.Celdas) _centros.Add(new Vector3(c.X + 0.5f, Medidas.AlturaLana, c.Y + 0.5f));
            if (_centros.Count > _cantidadAnterior && _cantidadAnterior > 0) _crecimiento = 0f;
            else _crecimiento = 1f;
            _cantidadAnterior = _centros.Count;
            Reconstruir();
        }

        private void Reconstruir()
        {
            if (_centros.Count < 2)
            {
                _malla.Clear();
                return;
            }
            var centros = new List<Vector3>(_centros);
            if (_crecimiento < 1f)
            {
                var a = centros[centros.Count - 2];
                var b = centros[centros.Count - 1];
                centros[centros.Count - 1] = Vector3.Lerp(a, b, Mathf.Max(0.05f, _crecimiento));
            }
            var camino = Mallas.CaminoSuave(centros);
            var datos = Mallas.Tubo(camino, Medidas.RadioLana, 14, 2.5f);
            Constructor.AMesh(datos, "Lana", _malla);
            Materiales.Tenir(_renderer, ColorLana, _bloque);
        }

        private void Update()
        {
            if (_crecimiento >= 1f || _malla == null) return;
            _crecimiento = Mathf.Min(1f, _crecimiento + Time.unscaledDeltaTime / DuracionCrecimiento);
            Reconstruir();
        }

        public void MostrarHuellas(Recorrido r, IList<int> indices)
        {
            foreach (var h in _huellas) if (h != null) Destroy(h);
            _huellas.Clear();
            if (r == null || indices == null) return;
            foreach (int i in indices)
            {
                var c = r.Celdas[i];
                var modelo = Constructor.Instanciar(Modelos.Huella(), transform);
                modelo.Raiz.position = new Vector3(c.X + 0.82f, Medidas.AlturaCasilla + 0.006f, c.Y + 0.2f);
                modelo.Raiz.rotation = Quaternion.Euler(0, 20, 0);
                _huellas.Add(modelo.Raiz.gameObject);
            }
        }

        public void Limpiar()
        {
            if (_malla != null) _malla.Clear();
            _cantidadAnterior = 0;
            MostrarHuellas(null, null);
        }
    }
}
