using System.Collections;
using DesenredaElOvillo.Nucleo;
using UnityEngine;

namespace DesenredaElOvillo.Juego
{
    /// <summary>
    /// Una polilla en pantalla: cuerpo de lana, alas de fieltro que baten, extras (placas, capa, corona)
    /// y pequeñas animaciones elásticas para cada cosa que le pasa. No sabe nada de reglas: el
    /// ReproductorOleada le dice qué animar según los eventos de la simulación.
    /// </summary>
    public sealed class VistaPolilla : MonoBehaviour
    {
        private Transform _cuerpoRaiz;
        private SpriteRenderer _cuerpo;
        private SpriteRenderer _alas;
        private SpriteRenderer _sombra;
        private SpriteRenderer _placas;
        private SpriteRenderer _enredo;
        private SpriteRenderer _pegote;
        private float _escalaBase = 0.82f;
        private float _aleteo = 1f;
        private float _parpadeo;
        private float _temblor;

        public int Id { get; private set; }
        public TipoPolilla Tipo { get; private set; }
        public bool Volando { get; set; }

        public static VistaPolilla Crear(Transform padre, int id, TipoPolilla tipo, Vector3 posicion)
        {
            var go = new GameObject(tipo + " #" + id);
            go.transform.SetParent(padre, false);
            go.transform.position = posicion;
            var v = go.AddComponent<VistaPolilla>();
            v.Iniciar(id, tipo);
            return v;
        }

        private void Iniciar(int id, TipoPolilla tipo)
        {
            Id = id;
            Tipo = tipo;
            _escalaBase = tipo == TipoPolilla.Reina ? 1.35f : tipo == TipoPolilla.Acorazada ? 1.02f : tipo == TipoPolilla.Veloz ? 0.82f : 0.92f;
            int orden = Capas.Polilla + id * 6 % 30;

            _sombra = Hijo("Sombra", Texturas.Circulo(), orden - 3, transform);
            _sombra.color = new Color(0, 0, 0, 0.28f);
            _sombra.transform.localScale = new Vector3(0.55f, 0.18f, 1) * _escalaBase;
            _sombra.transform.localPosition = new Vector3(0, -0.28f * _escalaBase, 0);

            _cuerpoRaiz = new GameObject("Cuerpo").transform;
            _cuerpoRaiz.SetParent(transform, false);
            _alas = Hijo("Alas", Texturas.AlasPolilla(tipo), orden - 1, _cuerpoRaiz);
            _cuerpo = Hijo("Cuerpo", Texturas.CuerpoPolilla(tipo), orden, _cuerpoRaiz);
            if (tipo == TipoPolilla.Acorazada) _placas = Hijo("Placas", Texturas.PlacasAcorazada(), orden + 1, _cuerpoRaiz);
            if (tipo == TipoPolilla.Reina)
            {
                Hijo("Capa", Texturas.CapaReina(), orden + 1, _cuerpoRaiz);
                var corona = Hijo("Corona", Texturas.Corona(), orden + 2, _cuerpoRaiz);
                corona.transform.localPosition = new Vector3(0, 0.22f, 0);
                corona.transform.localScale = Vector3.one * 0.45f;
            }
            _enredo = Hijo("Enredo", Texturas.PiezaLana(Texturas.Derecha | Texturas.Izquierda), orden + 3, _cuerpoRaiz);
            _enredo.color = Paleta.Mostaza;
            _enredo.transform.localScale = new Vector3(0.7f, 0.9f, 1);
            _enredo.transform.localRotation = Quaternion.Euler(0, 0, 25);
            _enredo.enabled = false;
            _pegote = Hijo("Velcro", Texturas.Herramienta(TipoHerramienta.Velcro), orden - 2, transform);
            _pegote.transform.localScale = new Vector3(0.55f, 0.35f, 1);
            _pegote.transform.localPosition = new Vector3(0, -0.26f, 0);
            _pegote.enabled = false;
            _cuerpoRaiz.localScale = Vector3.one * _escalaBase;
            _parpadeo = Random.value * 6f;
        }

        private SpriteRenderer Hijo(string nombre, Sprite sprite, int orden, Transform padre)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            var sr = go.AddComponent<SpriteRenderer>();
            Texturas.PrepararRenderer(sr);
            sr.sprite = sprite;
            sr.sortingOrder = orden;
            return sr;
        }

        private void Update()
        {
            if (_cuerpoRaiz == null) return;
            float t = Time.time + Id * 0.37f;
            float vel = Volando ? 26f : 9f * _aleteo;
            if (_alas != null) _alas.transform.localScale = new Vector3(0.82f + 0.18f * Mathf.Abs(Mathf.Sin(t * vel)), 1f, 1f);
            float bote = Volando ? 0.18f + Mathf.Sin(t * 8f) * 0.03f : Mathf.Abs(Mathf.Sin(t * 3f)) * 0.025f;
            var pos = new Vector3(0, bote, 0);
            if (_temblor > 0)
            {
                _temblor -= Time.deltaTime;
                pos.x += Mathf.Sin(t * 80f) * 0.05f;
            }
            _cuerpoRaiz.localPosition = pos;
            if (_sombra != null) _sombra.transform.localScale = new Vector3(0.55f, 0.18f, 1) * _escalaBase * (Volando ? 0.75f : 1f);
            _parpadeo -= Time.deltaTime;
        }

        // ------------------------------------------------------------------ animaciones

        public IEnumerator Aparecer(float d)
        {
            for (float e = 0; e < d; e += Time.deltaTime)
            {
                float k = Elastico(Mathf.Clamp01(e / d));
                transform.localScale = Vector3.one * k;
                yield return null;
            }
            transform.localScale = Vector3.one;
        }

        public IEnumerator Mover(Vector3 destino, float d, bool saltito)
        {
            var origen = transform.position;
            for (float e = 0; e < d; e += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, e / d);
                var p = Vector3.Lerp(origen, destino, k);
                if (saltito) p.y += Mathf.Sin(k * Mathf.PI) * 0.12f;
                transform.position = p;
                float estira = 1f + Mathf.Sin(k * Mathf.PI) * 0.12f;
                if (_cuerpoRaiz != null) _cuerpoRaiz.localScale = new Vector3(_escalaBase / estira, _escalaBase * estira, 1);
                yield return null;
            }
            transform.position = destino;
            if (_cuerpoRaiz != null) _cuerpoRaiz.localScale = Vector3.one * _escalaBase;
        }

        public IEnumerator Empujada(Vector3 destino, float d)
        {
            var origen = transform.position;
            for (float e = 0; e < d; e += Time.deltaTime)
            {
                float k = e / d;
                var p = Vector3.Lerp(origen, destino, k);
                p.y += Mathf.Sin(k * Mathf.PI) * 0.9f;
                transform.position = p;
                transform.rotation = Quaternion.Euler(0, 0, k * 360f);
                yield return null;
            }
            transform.position = destino;
            transform.rotation = Quaternion.identity;
            yield return Aplastar(0.12f);
        }

        public IEnumerator Aplastar(float d)
        {
            for (float e = 0; e < d; e += Time.deltaTime)
            {
                float k = Mathf.Sin(e / d * Mathf.PI);
                if (_cuerpoRaiz != null) _cuerpoRaiz.localScale = new Vector3(_escalaBase * (1 + k * 0.25f), _escalaBase * (1 - k * 0.25f), 1);
                yield return null;
            }
            if (_cuerpoRaiz != null) _cuerpoRaiz.localScale = Vector3.one * _escalaBase;
        }

        public IEnumerator Golpe(float d)
        {
            _temblor = d;
            if (_cuerpo != null) _cuerpo.color = new Color(1f, 0.55f, 0.55f, 1f);
            yield return Aplastar(d);
            if (_cuerpo != null) _cuerpo.color = Color.white;
        }

        public IEnumerator Morir(float d)
        {
            Volando = false;
            var inicial = transform.localScale;
            for (float e = 0; e < d; e += Time.deltaTime)
            {
                float k = e / d;
                transform.localScale = inicial * (1f + k * 0.4f) * (1f - k);
                transform.Rotate(0, 0, 540 * Time.deltaTime);
                yield return null;
            }
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
            if (_pegote != null) _pegote.enabled = activo;
            _aleteo = activo ? 0.2f : 1f;
        }

        public void Enredada(bool activo)
        {
            if (_enredo != null) _enredo.enabled = activo;
            Volando = false;
        }

        public void Temblar(float segundos) => _temblor = segundos;

        public IEnumerator RomperPlaca(float d)
        {
            if (_placas == null) yield break;
            var t = _placas.transform;
            var inicial = t.localPosition;
            for (float e = 0; e < d; e += Time.deltaTime)
            {
                float k = e / d;
                t.localPosition = inicial + new Vector3(k * 0.3f, -k * k * 0.8f, 0);
                t.localRotation = Quaternion.Euler(0, 0, -k * 70);
                _placas.color = new Color(1, 1, 1, 1 - k);
                yield return null;
            }
            _placas.enabled = false;
        }

        private static float Elastico(float x)
        {
            if (x <= 0) return 0;
            if (x >= 1) return 1;
            return Mathf.Pow(2, -10 * x) * Mathf.Sin((x * 10 - 0.75f) * (2 * Mathf.PI / 3)) + 1;
        }
    }
}
