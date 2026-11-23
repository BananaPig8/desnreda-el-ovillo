using System.Collections;
using System.Collections.Generic;
using DesenredaElOvillo.Nucleo;
using UnityEngine;

namespace DesenredaElOvillo.Juego
{
    /// <summary>Herramientas cosidas sobre la lana, con sus usos como puntadas de colores debajo.</summary>
    public sealed class VistaHerramientas : MonoBehaviour
    {
        private sealed class Pieza
        {
            public TipoHerramienta Tipo;
            public int Indice;
            public SpriteRenderer Sprite;
            public SpriteRenderer Sombra;
            public readonly List<SpriteRenderer> Usos = new List<SpriteRenderer>();
            public int UsosMaximos;
            public bool Doblada;
        }

        private readonly List<Pieza> _piezas = new List<Pieza>();

        public void Dibujar(Recorrido r, PlanColocacion plan, Reglas reglas)
        {
            Limpiar();
            if (r == null || plan == null) return;
            foreach (var c in plan.Colocaciones)
            {
                if (c.Indice <= 0 || c.Indice >= r.Cantidad) continue;
                var def = reglas.Herramienta(c.Tipo);
                var pos = VistaTablero.Mundo(r.Celdas[c.Indice]);
                var p = new Pieza { Tipo = c.Tipo, Indice = c.Indice, UsosMaximos = def.Usos };
                bool debajo = PlanColocacion.VaDebajo(c.Tipo);
                p.Sprite = Crear(c.Tipo.ToString(), Texturas.Herramienta(c.Tipo), debajo ? Capas.Velcro : Capas.Herramienta, pos);
                p.Sprite.transform.localScale = Vector3.one * (debajo ? 0.86f : 0.76f);
                if (!debajo)
                {
                    p.Sombra = Crear("Sombra", Texturas.Herramienta(c.Tipo), Capas.Herramienta - 1, pos + new Vector3(0.04f, -0.05f, 0));
                    p.Sombra.color = new Color(0, 0, 0, 0.3f);
                    p.Sombra.transform.localScale = Vector3.one * 0.76f;
                }
                float y = debajo ? -0.4f : -0.32f;
                float x0 = (debajo ? 0.18f : -0.18f) - (def.Usos - 1) * 0.06f;
                for (int u = 0; u < def.Usos; u++)
                {
                    var punto = Crear("Uso", Texturas.Circulo(), Capas.Herramienta + 2, pos + new Vector3(x0 + u * 0.12f, y, 0));
                    punto.transform.localScale = Vector3.one * 0.16f;
                    punto.color = debajo ? Paleta.AzulSuave : Paleta.Mostaza;
                    p.Usos.Add(punto);
                }
                _piezas.Add(p);
            }
        }

        private Pieza Buscar(int indice, TipoHerramienta tipo)
        {
            foreach (var p in _piezas) if (p.Indice == indice && p.Tipo == tipo) return p;
            return null;
        }

        /// <summary>Animación de activación y actualización de las puntadas de usos.</summary>
        public void Activar(int indice, TipoHerramienta tipo, int usosRestantes)
        {
            var p = Buscar(indice, tipo);
            if (p == null) return;
            for (int u = 0; u < p.Usos.Count; u++)
                p.Usos[u].color = u < usosRestantes ? (PlanColocacion.VaDebajo(tipo) ? Paleta.AzulSuave : Paleta.Mostaza) : new Color(0.25f, 0.18f, 0.12f, 0.8f);
            StartCoroutine(Rebote(p.Sprite.transform, PlanColocacion.VaDebajo(tipo) ? 0.86f : 0.76f));
        }

        public void Gastar(int indice, TipoHerramienta tipo)
        {
            var p = Buscar(indice, tipo);
            if (p == null) return;
            p.Doblada = true;
            if (tipo == TipoHerramienta.Alfiler)
            {
                p.Sprite.sprite = Texturas.Herramienta(tipo, true);
                if (p.Sombra != null) p.Sombra.sprite = p.Sprite.sprite;
            }
            else
            {
                p.Sprite.color = new Color(1, 1, 1, 0.45f);
                if (p.Sombra != null) p.Sombra.enabled = false;
            }
        }

        public void Devorar(int indice, TipoHerramienta tipo)
        {
            var p = Buscar(indice, tipo);
            if (p == null) return;
            StartCoroutine(Encoger(p));
        }

        private IEnumerator Rebote(Transform t, float escala)
        {
            float d = 0.18f;
            for (float e = 0; e < d; e += Time.deltaTime)
            {
                if (t == null) yield break;
                float k = 1f + Mathf.Sin(e / d * Mathf.PI) * 0.35f;
                t.localScale = new Vector3(escala * k, escala * (2f - k), 1);
                yield return null;
            }
            if (t != null) t.localScale = Vector3.one * escala;
        }

        private IEnumerator Encoger(Pieza p)
        {
            float d = 0.35f;
            var inicial = p.Sprite.transform.localScale;
            for (float e = 0; e < d; e += Time.deltaTime)
            {
                if (p.Sprite == null) yield break;
                float k = 1f - e / d;
                p.Sprite.transform.localScale = inicial * k;
                p.Sprite.transform.Rotate(0, 0, 720 * Time.deltaTime);
                yield return null;
            }
            if (p.Sprite != null) p.Sprite.enabled = false;
            if (p.Sombra != null) p.Sombra.enabled = false;
            foreach (var u in p.Usos) if (u != null) u.enabled = false;
        }

        private SpriteRenderer Crear(string nombre, Sprite sprite, int orden, Vector3 pos)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            Texturas.PrepararRenderer(sr);
            sr.sprite = sprite;
            sr.sortingOrder = orden;
            return sr;
        }

        public void Limpiar()
        {
            StopAllCoroutines();
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            _piezas.Clear();
        }
    }
}
