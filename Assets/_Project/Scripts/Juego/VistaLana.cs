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
    /// teje, el último tramo crece en lugar de aparecer de golpe y un ovillito rueda en la punta, cada vez
    /// más pequeño según se gasta el hilo (así se ve cuánto queda sin mirar la interfaz). Al llegar al
    /// carrete, el ovillito desaparece: la hebra queda atada. También coloca las huellas de la veloz.
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

        // ovillito de la punta
        private Transform _ovillito;
        private Color _colorOvillito;
        private bool _ovillitoVisible;
        private float _radioOvillito = 0.18f, _radioObjetivo = 0.18f;
        private Vector3 _puntaObjetivo, _direccionPunta = Vector3.forward;
        private Vector3? _puntaAnterior;

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

        /// <param name="hiloRestante">Fracción del hilo que queda por tejer (0..1): el tamaño del ovillito.</param>
        public void Dibujar(Recorrido r, float hiloRestante = 1f)
        {
            Asegurar();
            _centros.Clear();
            if (r != null)
                foreach (var c in r.Celdas) _centros.Add(new Vector3(c.X + 0.5f, Medidas.AlturaLana, c.Y + 0.5f));
            if (_centros.Count > _cantidadAnterior && _cantidadAnterior > 0) _crecimiento = 0f;
            else _crecimiento = 1f;
            _cantidadAnterior = _centros.Count;
            _ovillitoVisible = r != null && r.Cantidad > 0 && !r.EstaCompleto;
            _radioObjetivo = Medidas.RadioOvillito(hiloRestante);
            if (r != null && r.Cantidad == 1)
            {
                // solo el agujero: el ovillito espera junto a la entrada, mirando hacia dentro del tablero
                var fuera = VistaTablero.DireccionFuera(r.Tablero, r.Celdas[0]);
                _direccionPunta = -fuera;
            }
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
            var dir = centros[centros.Count - 1] - centros[centros.Count - 2];
            dir.y = 0f;
            if (dir.sqrMagnitude > 1e-6f) _direccionPunta = dir.normalized;
        }

        private void Update()
        {
            if (_crecimiento < 1f && _malla != null)
            {
                _crecimiento = Mathf.Min(1f, _crecimiento + Time.unscaledDeltaTime / DuracionCrecimiento);
                Reconstruir();
            }
            ActualizarOvillito();
        }

        /// <summary>El ovillito sigue a la punta (incluida la animación de crecimiento) y rueda lo que avanza.</summary>
        private void ActualizarOvillito()
        {
            if (!_ovillitoVisible || _centros.Count == 0)
            {
                if (_ovillito != null && _ovillito.gameObject.activeSelf) _ovillito.gameObject.SetActive(false);
                _puntaAnterior = null;
                return;
            }
            if (_ovillito == null || _colorOvillito != ColorLana)
            {
                if (_ovillito != null) Destroy(_ovillito.gameObject);
                _colorOvillito = ColorLana;
                var modelo = Constructor.Instanciar(Modelos.OvilloDeTrabajo(_colorOvillito), transform);
                _ovillito = modelo.Raiz;
                _radioOvillito = _radioObjetivo;
                _puntaAnterior = null;
            }
            if (!_ovillito.gameObject.activeSelf) _ovillito.gameObject.SetActive(true);

            var punta = _centros[_centros.Count - 1];
            if (_crecimiento < 1f && _centros.Count >= 2) punta = Vector3.Lerp(_centros[_centros.Count - 2], punta, Mathf.Max(0.05f, _crecimiento));
            _radioOvillito = Mathf.MoveTowards(_radioOvillito, _radioObjetivo, Time.unscaledDeltaTime * 0.4f);
            _puntaObjetivo = Medidas.PosicionOvillito(punta, _direccionPunta, _radioOvillito);

            if (_puntaAnterior.HasValue)
            {
                var paso = _puntaObjetivo - _puntaAnterior.Value;
                paso.y = 0f;
                float d = paso.magnitude;
                if (d > 1e-5f)
                {
                    var eje = Vector3.Cross(Vector3.up, paso / d);
                    _ovillito.rotation = Quaternion.AngleAxis(d / _radioOvillito * Mathf.Rad2Deg, eje) * _ovillito.rotation;
                }
            }
            _puntaAnterior = _puntaObjetivo;
            _ovillito.position = _puntaObjetivo;
            _ovillito.localScale = Vector3.one * (_radioOvillito * 2f);
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
            _centros.Clear();
            _ovillitoVisible = false;
            MostrarHuellas(null, null);
        }
    }
}
