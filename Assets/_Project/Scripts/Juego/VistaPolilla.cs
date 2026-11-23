using System.Collections;
using System.Collections.Generic;
using DesenredaElOvillo.Juego.Tres;
using DesenredaElOvillo.Nucleo;
using UnityEngine;

namespace DesenredaElOvillo.Juego
{
    /// <summary>
    /// Una polilla 3D: cuerpo de lana, alas de fieltro que baten, patitas que trotan y una cara expresiva
    /// (parpadea, aprieta los ojos al recibir un golpe, los entorna pegada o enredada, los cierra deslumbrada
    /// y pone ojos en X al caer). Mira a medias hacia donde avanza y a medias hacia la cámara (para que
    /// siempre se le vea la cara). No sabe nada de reglas: el ReproductorOleada le dice qué animar.
    /// </summary>
    public sealed class VistaPolilla : MonoBehaviour
    {
        private ModeloInstanciado _modelo;
        private Transform _cuerpo;
        private Transform _alaIzq, _alaDer, _patas;
        private Quaternion _alaIzqBase, _alaDerBase;
        private Vector3 _cuerpoBase;
        private GameObject _enredo, _pegote, _estrellas;
        private float _aleteo = 1f, _temblor, _giro, _giroObjetivo;
        private readonly List<Transform> _parpados = new List<Transform>();
        private List<Transform> _ojos, _ojosX;
        private float _siguienteParpadeo, _golpeHasta, _parpadoAplicado = -1f;
        private bool _pegada, _enredada, _deslumbrada, _caida;
        private SombraDinamica _sombra;

        public int Id { get; private set; }
        public TipoPolilla Tipo { get; private set; }
        public bool Volando { get; set; }
        public const float AlturaVuelo = 0.42f;

        public static VistaPolilla Crear(Transform padre, int id, TipoPolilla tipo, Vector3 posicion)
        {
            var go = new GameObject(tipo + " #" + id);
            go.transform.SetParent(padre, false);
            go.transform.position = posicion;
            var v = go.AddComponent<VistaPolilla>();
            v.Id = id;
            v.Tipo = tipo;
            v._modelo = Constructor.Instanciar(Modelos.Polilla(tipo), go.transform);
            v._cuerpo = v._modelo.Primero("cuerpo");
            v._alaIzq = v._modelo.Primero("ala_izq");
            v._alaDer = v._modelo.Primero("ala_der");
            v._patas = v._modelo.Primero("patas");
            if (v._alaIzq != null) v._alaIzqBase = v._alaIzq.localRotation;
            if (v._alaDer != null) v._alaDerBase = v._alaDer.localRotation;
            if (v._cuerpo != null) v._cuerpoBase = v._cuerpo.localPosition;
            v._parpados.AddRange(v._modelo.Grupo("parpados"));
            v._ojos = v._modelo.Grupo("ojos");
            v._ojosX = v._modelo.Grupo("ojos_x");
            v._siguienteParpadeo = Time.time + Random.Range(0.6f, 3f);
            v._sombra = SombraDinamica.Agregar(v._modelo.Sombra, go.transform, Medidas.AlturaCasilla, Medidas.AlturaPolilla);
            v._enredo = v.Adorno(new Pieza { Nombre = "Enredo", Malla = CatalogoMallas.Toro(0.24f, 0.025f), Material = "lana", Color = Paleta.Mostaza, Posicion = new Vector3(0, 0.24f, 0), Rotacion = new Vector3(70, 0, 20) });
            v._pegote = v.Adorno(new Pieza { Nombre = "Velcro pegado", Malla = CatalogoMallas.Caja(0.4f, 0.03f, 0.4f, 0.015f), Material = "velcro", Color = Paleta.Hex("#6C8FB5"), Posicion = new Vector3(0, 0.0f, 0) });
            var estrellas = new Pieza { Nombre = "Estrellas" };
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f * Mathf.Deg2Rad;
                estrellas.Hijos.Add(new Pieza { Nombre = "Estrella", Malla = "esfera_baja", Material = "emisivo", Color = Paleta.Hex("#FFE28A"), Posicion = new Vector3(Mathf.Cos(a) * 0.22f, 0.62f, Mathf.Sin(a) * 0.22f), Escala = Vector3.one * 0.07f });
            }
            v._estrellas = v.Adorno(estrellas);
            v._giro = v._giroObjetivo = 0;
            return v;
        }

        private GameObject Adorno(Pieza p)
        {
            var m = Constructor.Instanciar(p, transform);
            m.Raiz.gameObject.SetActive(false);
            return m.Raiz.gameObject;
        }

        /// <summary>Orienta la polilla: mitad hacia la dirección de avance, mitad hacia la cámara.</summary>
        public void MirarHacia(Vector3 direccion)
        {
            direccion.y = 0;
            if (direccion.sqrMagnitude < 1e-4f) return;
            float haciaAvance = Mathf.Atan2(-direccion.x, -direccion.z) * Mathf.Rad2Deg;
            _giroObjetivo = Mathf.LerpAngle(0f, haciaAvance, 0.5f);
        }

        private void Update()
        {
            if (_cuerpo == null) return;
            float t = Time.time + Id * 0.37f;
            float frecuencia = Volando ? 24f : 7f * _aleteo;
            float amplitud = Volando ? 38f : 14f * _aleteo;
            float batido = Mathf.Sin(t * frecuencia) * amplitud;
            if (_alaIzq != null) _alaIzq.localRotation = _alaIzqBase * Quaternion.Euler(0, batido, 0);
            if (_alaDer != null) _alaDer.localRotation = _alaDerBase * Quaternion.Euler(0, -batido, 0);
            float bote = Volando ? AlturaVuelo + Mathf.Sin(t * 6f) * 0.04f : Mathf.Abs(Mathf.Sin(t * 3f)) * 0.015f;
            var pos = _cuerpoBase + Vector3.up * bote;
            if (_temblor > 0)
            {
                _temblor -= Time.deltaTime;
                pos.x += Mathf.Sin(t * 80f) * 0.03f;
            }
            _cuerpo.localPosition = pos;
            if (_patas != null) _patas.localPosition = Vector3.up * (Volando ? bote * 0.9f : 0f);
            _giro = Mathf.LerpAngle(_giro, _giroObjetivo, 1f - Mathf.Exp(-Time.deltaTime * 10f));
            transform.rotation = Quaternion.Euler(0, _giro, 0);
            if (_enredo != null && _enredo.activeSelf) _enredo.transform.Rotate(0, 90 * Time.deltaTime, 0, Space.World);
            if (_estrellas != null && _estrellas.activeSelf) _estrellas.transform.Rotate(0, 200 * Time.deltaTime, 0, Space.World);
            if (_sombra != null) _sombra.Extra = Volando ? bote : 0f;
            ActualizarParpados();
        }

        /// <summary>Párpados: 0 abiertos, 1 cerrados. El estado (pegada, enredada, deslumbrada) manda sobre el parpadeo.</summary>
        private void ActualizarParpados()
        {
            float t = Time.time;
            float parpado = _deslumbrada ? 1f : _enredada ? 0.5f : _pegada ? 0.35f : 0f;
            if (t < _golpeHasta) parpado = Mathf.Max(parpado, 0.8f);
            if (t > _siguienteParpadeo + 0.11f) _siguienteParpadeo = t + Random.Range(2.2f, 4.8f);
            else if (t > _siguienteParpadeo) parpado = 1f;
            if (_caida) parpado = 0f;
            if (Mathf.Abs(parpado - _parpadoAplicado) < 0.01f) return;
            _parpadoAplicado = parpado;
            foreach (var p in _parpados)
            {
                bool visible = parpado > 0.02f;
                if (p.gameObject.activeSelf != visible) p.gameObject.SetActive(visible);
                if (visible) p.localScale = new Vector3(1f, parpado, 1f);
            }
        }

        private void PonerOjosEnX()
        {
            _caida = true;
            foreach (var o in _ojos) o.gameObject.SetActive(false);
            foreach (var o in _ojosX) o.gameObject.SetActive(true);
            ActualizarParpados();
        }

        // ------------------------------------------------------------------ animaciones

        public IEnumerator Aparecer(float d)
        {
            for (float e = 0; e < d; e += Time.deltaTime)
            {
                transform.localScale = Vector3.one * Elastico(Mathf.Clamp01(e / d));
                yield return null;
            }
            transform.localScale = Vector3.one;
        }

        public IEnumerator Mover(Vector3 destino, float d, bool saltito)
        {
            var origen = transform.position;
            MirarHacia(destino - origen);
            for (float e = 0; e < d; e += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, e / d);
                var p = Vector3.Lerp(origen, destino, k);
                if (saltito) p.y += Mathf.Sin(k * Mathf.PI) * 0.14f;
                transform.position = p;
                float estira = 1f + Mathf.Sin(k * Mathf.PI) * 0.12f;
                if (_cuerpo != null) _cuerpo.localScale = new Vector3(1f / estira, estira, 1f / estira);
                yield return null;
            }
            transform.position = destino;
            if (_cuerpo != null) _cuerpo.localScale = Vector3.one;
        }

        public IEnumerator Empujada(Vector3 destino, float d)
        {
            var origen = transform.position;
            for (float e = 0; e < d; e += Time.deltaTime)
            {
                float k = e / d;
                var p = Vector3.Lerp(origen, destino, k);
                p.y += Mathf.Sin(k * Mathf.PI) * 1.1f;
                transform.position = p;
                if (_cuerpo != null) _cuerpo.localRotation = Quaternion.Euler(k * 360f, 0, 0);
                yield return null;
            }
            transform.position = destino;
            if (_cuerpo != null) _cuerpo.localRotation = Quaternion.identity;
            yield return Aplastar(0.12f);
        }

        public IEnumerator Aplastar(float d)
        {
            for (float e = 0; e < d; e += Time.deltaTime)
            {
                float k = Mathf.Sin(e / d * Mathf.PI);
                if (_cuerpo != null) _cuerpo.localScale = new Vector3(1 + k * 0.25f, 1 - k * 0.25f, 1 + k * 0.25f);
                yield return null;
            }
            if (_cuerpo != null) _cuerpo.localScale = Vector3.one;
        }

        public IEnumerator Golpe(float d)
        {
            _temblor = d;
            _golpeHasta = Time.time + Mathf.Max(d, 0.18f);
            _modelo.Destello(Paleta.Hex("#FF5A4A"), 0.6f);
            yield return Aplastar(d);
            _modelo.Destello(Color.white, 0f);
        }

        public IEnumerator Morir(float d)
        {
            Volando = false;
            PonerOjosEnX();
            var inicial = transform.localScale;
            for (float e = 0; e < d; e += Time.deltaTime)
            {
                float k = e / d;
                transform.localScale = inicial * (1f + k * 0.4f) * (1f - k);
                transform.Rotate(0, 720 * Time.deltaTime, 0);
                yield return null;
            }
            Pelusa.Soltar(transform.position + Vector3.up * 0.3f, 10);
            gameObject.SetActive(false);
        }

        public IEnumerator Escapar(Vector3 carrete, float d)
        {
            var origen = transform.position;
            for (float e = 0; e < d; e += Time.deltaTime)
            {
                float k = e / d;
                transform.position = Vector3.Lerp(origen, carrete, k);
                transform.localScale = Vector3.one * (1f - k * 0.7f);
                yield return null;
            }
            gameObject.SetActive(false);
        }

        public void Teletransportar(Vector3 posicion)
        {
            transform.position = posicion;
            transform.localScale = Vector3.one;
            gameObject.SetActive(true);
        }

        public void Pegada(bool activo)
        {
            if (_pegote != null) _pegote.SetActive(activo);
            _aleteo = activo ? 0.2f : 1f;
            _pegada = activo;
        }

        public void Enredada(bool activo)
        {
            if (_enredo != null) _enredo.SetActive(activo);
            Volando = false;
            _enredada = activo;
        }

        public void Deslumbrada(bool activo)
        {
            if (_estrellas != null) _estrellas.SetActive(activo);
            _deslumbrada = activo;
        }

        /// <summary>Mordisquitos al carrete (derrota): dos aplastones rápidos con los ojos apretados.</summary>
        public IEnumerator Masticar(float d)
        {
            _golpeHasta = Time.time + d;
            yield return Aplastar(d * 0.5f);
            yield return Aplastar(d * 0.5f);
        }

        public void Temblar(float segundos) => _temblor = segundos;

        public IEnumerator RomperPlaca(float d)
        {
            var placas = _modelo.Primero("placas");
            if (placas == null) yield break;
            var inicial = placas.localPosition;
            for (float e = 0; e < d; e += Time.deltaTime)
            {
                float k = e / d;
                placas.localPosition = inicial + new Vector3(0, -k * k * 0.6f, -k * 0.2f);
                placas.localScale = Vector3.one * (1f - k);
                yield return null;
            }
            placas.gameObject.SetActive(false);
            Pelusa.Soltar(transform.position + Vector3.up * 0.3f, 6);
        }

        private static float Elastico(float x)
        {
            if (x <= 0) return 0;
            if (x >= 1) return 1;
            return Mathf.Pow(2, -10 * x) * Mathf.Sin((x * 10 - 0.75f) * (2 * Mathf.PI / 3)) + 1;
        }
    }
}
