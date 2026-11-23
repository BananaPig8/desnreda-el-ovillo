using System.Collections;
using System.Collections.Generic;
using DesenredaElOvillo.Juego.Tres;
using DesenredaElOvillo.Nucleo;
using UnityEngine;

namespace DesenredaElOvillo.Juego
{
    /// <summary>
    /// Herramientas de costura en 3D sobre la lana. Los usos se ven como cuentas de colores junto a cada
    /// herramienta (mostaza = puede atrapar otra polilla; marrón = gastada). Cada tipo tiene su animación:
    /// el alfiler pincha, las tijeras cortan, el botón rebota, la bobina gira y el velcro se aplasta.
    /// </summary>
    public sealed class VistaHerramientas : MonoBehaviour
    {
        private sealed class Pieza3D
        {
            public TipoHerramienta Tipo;
            public int Indice;
            public ModeloInstanciado Modelo;
            public readonly List<Renderer> Usos = new List<Renderer>();
        }

        private readonly List<Pieza3D> _piezas = new List<Pieza3D>();
        private readonly MaterialPropertyBlock _bloque = new MaterialPropertyBlock();
        private static readonly Color ColorUso = Paleta.Mostaza;
        private static readonly Color ColorUsoVelcro = Paleta.AzulSuave;
        private static readonly Color ColorGastado = Paleta.Hex("#4A2E1E");

        public void Dibujar(Recorrido r, PlanColocacion plan, Reglas reglas)
        {
            Limpiar();
            if (r == null || plan == null) return;
            foreach (var c in plan.Colocaciones)
            {
                if (c.Indice <= 0 || c.Indice >= r.Cantidad) continue;
                var celda = r.Celdas[c.Indice];
                bool debajo = PlanColocacion.VaDebajo(c.Tipo);
                var modelo = Constructor.Instanciar(Modelos.Herramienta(c.Tipo), transform);
                // Encima de la lana (el velcro, debajo: la lana pasa por encima).
                float altura = debajo ? Medidas.AlturaCasilla : Medidas.AlturaLana + Medidas.RadioLana * 0.55f;
                modelo.Raiz.position = new Vector3(celda.X + 0.5f, altura, celda.Y + 0.5f);
                if (!debajo) modelo.Raiz.localScale = Vector3.one * 1.2f;
                var p = new Pieza3D { Tipo = c.Tipo, Indice = c.Indice, Modelo = modelo };
                var def = reglas.Herramienta(c.Tipo);
                for (int u = 0; u < def.Usos; u++)
                {
                    var cuenta = Constructor.Instanciar(new Pieza { Nombre = "Uso", Malla = "esfera_baja", Material = "lana", Color = debajo ? ColorUsoVelcro : ColorUso, Escala = Vector3.one * 0.085f }, transform);
                    float x = debajo ? 0.38f : -0.38f;
                    cuenta.Raiz.position = new Vector3(celda.X + 0.5f + x, Medidas.AlturaCasilla + 0.045f, celda.Y + 0.5f - 0.36f + u * 0.11f);
                    p.Usos.Add(cuenta.Renderers[0]);
                }
                _piezas.Add(p);
            }
        }

        private Pieza3D Buscar(int indice, TipoHerramienta tipo)
        {
            foreach (var p in _piezas) if (p.Indice == indice && p.Tipo == tipo) return p;
            return null;
        }

        public void Activar(int indice, TipoHerramienta tipo, int usosRestantes)
        {
            var p = Buscar(indice, tipo);
            if (p == null) return;
            for (int u = 0; u < p.Usos.Count; u++)
                Materiales.Tenir(p.Usos[u], u < usosRestantes ? (PlanColocacion.VaDebajo(tipo) ? ColorUsoVelcro : ColorUso) : ColorGastado, _bloque);
            StartCoroutine(Animar(p));
        }

        public void Gastar(int indice, TipoHerramienta tipo)
        {
            var p = Buscar(indice, tipo);
            if (p == null) return;
            if (tipo == TipoHerramienta.Alfiler)
            {
                // El alfiler se dobla: sigue clavado (pincha a sus polillas quietas) pero ya no atrapa nuevas.
                var aguja = p.Modelo.Primero("aguja");
                if (aguja != null) aguja.localRotation = Quaternion.Euler(55, 0, -40);
            }
            p.Modelo.Destello(Paleta.GrisFieltro, 0.55f);
        }

        public void Devorar(int indice, TipoHerramienta tipo)
        {
            var p = Buscar(indice, tipo);
            if (p != null) StartCoroutine(Encoger(p));
        }

        private IEnumerator Animar(Pieza3D p)
        {
            var raiz = p.Modelo.Raiz;
            float d = 0.22f;
            switch (p.Tipo)
            {
                case TipoHerramienta.Tijeras:
                {
                    var a = p.Modelo.Primero("hoja_a");
                    var b = p.Modelo.Primero("hoja_b");
                    for (float e = 0; e < d; e += Time.deltaTime)
                    {
                        float k = Mathf.Sin(e / d * Mathf.PI);
                        if (a != null) a.localRotation = Quaternion.Euler(0, 10 - k * 10, 0);
                        if (b != null) b.localRotation = Quaternion.Euler(0, -10 + k * 10, 0);
                        yield return null;
                    }
                    break;
                }
                case TipoHerramienta.Boton:
                {
                    var boton = p.Modelo.Primero("boton");
                    for (float e = 0; e < d * 1.6f; e += Time.deltaTime)
                    {
                        float k = e / (d * 1.6f);
                        if (boton != null) boton.localPosition = new Vector3(0, 0.19f - Mathf.Sin(k * Mathf.PI) * 0.1f + Mathf.Sin(k * Mathf.PI * 3f) * 0.03f * (1 - k), 0);
                        yield return null;
                    }
                    if (boton != null) boton.localPosition = new Vector3(0, 0.19f, 0);
                    break;
                }
                case TipoHerramienta.Bobina:
                {
                    var bobina = p.Modelo.Primero("bobina");
                    for (float e = 0; e < d * 2f; e += Time.deltaTime)
                    {
                        if (bobina != null) bobina.Rotate(0, 900f * Time.deltaTime, 0);
                        yield return null;
                    }
                    break;
                }
                default:
                {
                    var escala = raiz.localScale;
                    for (float e = 0; e < d; e += Time.deltaTime)
                    {
                        float k = Mathf.Sin(e / d * Mathf.PI);
                        raiz.localScale = new Vector3(escala.x * (1 + k * 0.15f), escala.y * (1 - k * 0.25f), escala.z * (1 + k * 0.15f));
                        yield return null;
                    }
                    raiz.localScale = escala;
                    break;
                }
            }
        }

        private IEnumerator Encoger(Pieza3D p)
        {
            var raiz = p.Modelo.Raiz;
            var inicial = raiz.localScale;
            float d = 0.35f;
            for (float e = 0; e < d; e += Time.deltaTime)
            {
                if (raiz == null) yield break;
                raiz.localScale = inicial * (1f - e / d);
                raiz.Rotate(0, 720 * Time.deltaTime, 0);
                yield return null;
            }
            if (raiz != null) raiz.gameObject.SetActive(false);
            foreach (var u in p.Usos) if (u != null) u.gameObject.SetActive(false);
        }

        public void Limpiar()
        {
            StopAllCoroutines();
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            _piezas.Clear();
        }
    }
}
