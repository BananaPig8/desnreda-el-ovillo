using System.Collections.Generic;
using DesenredaElOvillo.Juego.Tres;
using UnityEngine;

namespace DesenredaElOvillo.Juego
{
    /// <summary>
    /// Ovillo en el diorama (modelo según su hoja de personaje). Llega andando a su puesto al empezar cada sala
    /// (en las pares tropieza con su propia hebra), respira, parpadea, mira a la polilla más cercana y se
    /// preocupa si se acerca al carrete, guiña un ojo cuando la lana llega al carrete, aprieta los ojos con cada
    /// mordisco, da saltitos cuando cae una polilla, corre asustado si se comen el carrete y celebra la victoria
    /// con los ojos "&gt; &lt;". La punta de la bufanda ondea al correr y saltar. Animación procedural con aire
    /// de "stop motion": la pose cambia 12 veces por segundo.
    /// </summary>
    public sealed class VistaOvillo : MonoBehaviour
    {
        private const float ImagenesPorSegundo = 12f;
        private const float Escala = 1.3f;
        private const float GiroReposo = -25f;
        private const float DuracionTropiezo = 1.15f;

        /// <summary>Las piezas de un ojo: abierto (con su escala), párpado, "^" y "&gt;"/"&lt;".</summary>
        private sealed class Ojo
        {
            public Transform Abierto, Parpado, Feliz, Apretado;
            public Vector3 Escala;
        }

        private ModeloInstanciado _modelo;
        private Transform _cuerpo, _cola, _brazoIzq, _brazoDer, _pieIzq, _pieDer;
        private Vector3 _brazoIzqBase, _brazoDerBase, _pieIzqBase, _pieDerBase;
        private Quaternion _colaBase = Quaternion.identity, _brazoIzqRot = Quaternion.identity, _brazoDerRot = Quaternion.identity;
        /// <summary>Brazos del modelo OBJ: giran desde el hombro (los de la hoja son bolitas que giran sobre sí mismas).</summary>
        private bool _brazosConHombro;
        private readonly Ojo[] _ojos = new Ojo[2];
        private Transform _bocaFeliz, _bocaO, _bocaTriste;
        private float _guinoHasta;
        private Vector3 _base, _lateral;
        private float _suelo, _giro = GiroReposo;
        private float _sustoHasta, _saltoHasta, _celebrarHasta, _tristeHasta, _heridoInicio = -10f, _panicoHasta, _destelloHasta;
        private float _siguienteParpadeo, _siguienteImagen;
        private Vector3? _mirarA;

        // llegada andando
        private float _entradaInicio = -10f, _entradaAndar;
        private Vector3 _entradaDesde;
        private bool _tropieza;

        public Expresion Cara { get; private set; }

        /// <param name="posicion">Su puesto junto al carrete (los pies sobre la mesa).</param>
        /// <param name="fuera">Dirección hacia fuera del tablero: por ahí llega andando.</param>
        public static VistaOvillo Crear(Transform padre, Vector3 posicion, Vector3 fuera)
        {
            var go = new GameObject("Ovillo (personaje)");
            go.transform.SetParent(padre, false);
            go.transform.position = posicion;
            go.transform.rotation = Quaternion.Euler(0, GiroReposo, 0);
            go.transform.localScale = Vector3.one * Escala;
            var v = go.AddComponent<VistaOvillo>();
            Pieza modelo = Ajustes.ModeloOvillo == 1 ? OvilloImportado.Modelo() : null;
            v._brazosConHombro = modelo != null;
            v._modelo = Constructor.Instanciar(modelo ?? Modelos.Ovillo(), go.transform);
            v._cuerpo = v._modelo.Primero("cuerpo");
            v._cola = v._modelo.Primero("cola_bufanda");
            v._brazoIzq = v._modelo.Primero("brazo_izq");
            v._brazoDer = v._modelo.Primero("brazo_der");
            v._pieIzq = v._modelo.Primero("pie_izq");
            v._pieDer = v._modelo.Primero("pie_der");
            if (v._brazoIzq != null) { v._brazoIzqBase = v._brazoIzq.localPosition; v._brazoIzqRot = v._brazoIzq.localRotation; }
            if (v._brazoDer != null) { v._brazoDerBase = v._brazoDer.localPosition; v._brazoDerRot = v._brazoDer.localRotation; }
            if (v._pieIzq != null) v._pieIzqBase = v._pieIzq.localPosition;
            if (v._pieDer != null) v._pieDerBase = v._pieDer.localPosition;
            if (v._cola != null) v._colaBase = v._cola.localRotation;
            for (int i = 0; i < 2; i++)
            {
                string sufijo = i == 0 ? "_izq" : "_der";
                var ojo = new Ojo
                {
                    Abierto = v._modelo.Primero("ojo" + sufijo),
                    Parpado = v._modelo.Primero("parpado" + sufijo),
                    Feliz = v._modelo.Primero("feliz" + sufijo),
                    Apretado = v._modelo.Primero("apretado" + sufijo)
                };
                if (ojo.Abierto != null) ojo.Escala = ojo.Abierto.localScale;
                v._ojos[i] = ojo;
            }
            v._bocaFeliz = v._modelo.Primero("boca_feliz");
            v._bocaO = v._modelo.Primero("boca_o");
            v._bocaTriste = v._modelo.Primero("boca_triste");
            v._base = posicion;
            v._suelo = posicion.y;
            v._lateral = Vector3.Cross(Vector3.up, fuera).normalized;
            v._siguienteParpadeo = Time.time + 2f;
            SombraDinamica.Agregar(v._modelo.Sombra, go.transform, posicion.y, posicion.y);
            return v;
        }

        // ------------------------------------------------------------------ órdenes

        /// <summary>Llega andando desde fuera del tablero hasta su puesto (y quizá tropieza a mitad de camino).</summary>
        public void Entrar(Vector3 fuera, bool tropezar)
        {
            _entradaDesde = _base + fuera.normalized * 2.3f + _lateral * 0.8f;
            _entradaInicio = Time.time;
            _entradaAndar = Vector3.Distance(_entradaDesde, _base) / 1.5f;
            _tropieza = tropezar;
            transform.position = _entradaDesde;
        }

        public bool Entrando => Time.time < _entradaInicio + _entradaAndar + (_tropieza ? DuracionTropiezo : 0f);

        public void Asustar(float s) => _sustoHasta = Time.time + s;
        /// <summary>Saltito de alegría (cae una polilla).</summary>
        public void Saltar(float s) => _saltoHasta = Time.time + s;
        /// <summary>Guiño cómplice con saltito (la lana llega al carrete).</summary>
        public void Guinar(float s)
        {
            _guinoHasta = Time.time + s;
            _saltoHasta = Time.time + s * 0.6f;
        }
        public void Entristecer(float s = 30f) => _tristeHasta = Time.time + s;
        public void MirarA(Vector3? objetivo) => _mirarA = objetivo;

        /// <summary>Un mordisco al carrete le duele a él: se encoge, aprieta los ojos y se pone colorado un instante.</summary>
        public void Herir()
        {
            _heridoInicio = Time.time;
            _destelloHasta = Time.time + 0.14f;
            _modelo.Destello(Paleta.Coral, 0.4f);
        }

        /// <summary>Corre de un lado a otro, con los brazos en alto, mientras las polillas devoran el carrete.</summary>
        public void Panico(float s) => _panicoHasta = Time.time + s;

        /// <summary>Victoria: saltos, vueltas, ojos felices y confeti.</summary>
        public void Celebrar(float s = 30f)
        {
            _celebrarHasta = Time.time + s;
            Confeti.Lanzar(_base + Vector3.up * 1.1f, 60, _suelo, 1.2f);
        }

        // ------------------------------------------------------------------ animación

        private void Update()
        {
            if (_cuerpo == null) return;
            if (_destelloHasta > 0f && Time.time > _destelloHasta)
            {
                _destelloHasta = 0f;
                _modelo.Destello(Color.white, 0f);
            }
            // Pose a saltos de 1/12 s: el encanto de la animación de maqueta.
            if (Time.time < _siguienteImagen) return;
            _siguienteImagen = Time.time + 1f / ImagenesPorSegundo;
            float t = Time.time;

            var pos = _base;
            var escala = Vector3.one;
            float giroObjetivo = GiroReposo, inclinacion = 0f;
            float alzar = 0f, balanceo = 0f, viento = 0f;
            bool suavizarGiro = true;
            var cara = Expresion.Normal;
            PonerPies(0f, 0f);

            if (Entrando)
            {
                AnimarEntrada(t, ref pos, ref escala, ref giroObjetivo, ref inclinacion, ref alzar, ref balanceo, ref cara);
                suavizarGiro = false;
                viento = -25f;
            }
            else if (t < _celebrarHasta)
            {
                float fase = Mathf.Abs(Mathf.Sin(t * 6f));
                pos += Vector3.up * fase * 0.45f;
                giroObjetivo = GiroReposo + Mathf.Repeat(t * 240f, 360f);
                suavizarGiro = false;
                alzar = 1f;
                balanceo = Mathf.Sin(t * 12f) * 0.4f;
                escala = Aplastado((1 - fase) * 0.1f);
                cara = Expresion.Euforia;
                viento = -50f * fase;
            }
            else if (t < _panicoHasta)
            {
                // ida y vuelta junto al carrete
                float x = Mathf.Sin(t * 2.4f);
                pos += _lateral * x * 0.65f;
                var dir = _lateral * Mathf.Cos(t * 2.4f);
                giroObjetivo = Mathf.Atan2(-dir.x, -dir.z) * Mathf.Rad2Deg;
                suavizarGiro = false;
                float fase = t * 22f;
                PonerPies(fase, 1.4f);
                pos += Vector3.up * Mathf.Abs(Mathf.Cos(fase)) * 0.05f;
                inclinacion = -12f;
                alzar = 1f;
                balanceo = Mathf.Sin(fase) * 0.5f;
                cara = Expresion.Susto;
                viento = -65f;
            }
            else if (t - _heridoInicio < 0.55f)
            {
                float k = (t - _heridoInicio) / 0.55f;
                float golpe = Mathf.Sin(Mathf.Min(1f, k * 2.2f) * Mathf.PI) * (1f - k);
                inclinacion = 16f * golpe;
                pos += new Vector3(Mathf.Sin(t * 60f) * 0.03f * (1f - k), 0f, 0f);
                escala = Aplastado(0.14f * (1f - k));
                alzar = 0.6f * (1f - k);
                cara = Expresion.Dolor;
            }
            else if (t < _tristeHasta)
            {
                escala = new Vector3(1.08f, 0.88f + Mathf.Sin(t * 1.3f) * 0.015f, 1.08f);
                alzar = -0.4f;
                inclinacion = -9f;
                giroObjetivo = 0f;
                cara = Expresion.Triste;
            }
            else if (t < _sustoHasta)
            {
                pos += new Vector3(Mathf.Sin(t * 50f) * 0.03f, 0, 0);
                alzar = 0.8f;
                escala = new Vector3(0.95f, 1.06f, 0.95f);
                cara = Expresion.Susto;
            }
            else if (t < _saltoHasta)
            {
                float fase = Mathf.Abs(Mathf.Sin(t * 9f));
                pos += Vector3.up * fase * 0.3f;
                alzar = 0.7f;
                escala = Aplastado((1 - fase) * 0.08f);
                cara = t < _guinoHasta ? Expresion.Guino : Expresion.Feliz;
                viento = -35f * fase;
            }
            else if (t < _guinoHasta)
            {
                alzar = 0.35f;
                cara = Expresion.Guino;
            }
            else
            {
                float respira = Mathf.Sin(t * 2.2f);
                escala = new Vector3(1f + respira * 0.015f, 1f - respira * 0.02f, 1f + respira * 0.015f);
                if (_mirarA.HasValue)
                {
                    var dir = _mirarA.Value - transform.position;
                    dir.y = 0;
                    if (dir.sqrMagnitude > 0.01f) giroObjetivo = Mathf.Clamp(Mathf.Atan2(-dir.x, -dir.z) * Mathf.Rad2Deg, -60f, 30f);
                    // una polilla cerca del carrete: ojos muy abiertos, boca en "o" y un temblorcito
                    if (dir.magnitude < 3f)
                    {
                        cara = Expresion.Preocupado;
                        pos += new Vector3(Mathf.Sin(t * 45f) * 0.008f, 0f, 0f);
                        alzar = 0.25f;
                    }
                }
            }

            _giro = suavizarGiro ? Mathf.LerpAngle(_giro, giroObjetivo, 0.35f) : giroObjetivo;
            // al inclinarse mucho (caída de morros) el cuerpo no debe hundirse en la mesa: se levanta el pivote de los pies
            pos.y += Mathf.Max(0f, Escala * (0.34f - 0.38f * Mathf.Cos(inclinacion * Mathf.Deg2Rad)));
            transform.SetPositionAndRotation(pos, Quaternion.Euler(inclinacion, _giro, 0));
            _cuerpo.localScale = escala;
            PonerBrazos(alzar, balanceo);
            // la punta de la bufanda: vaivén suave en reposo; al correr o saltar se va hacia atrás
            if (_cola != null) _cola.localRotation = _colaBase * Quaternion.Euler(viento + Mathf.Sin(t * 3f) * 6f, 0f, Mathf.Sin(t * 2.3f) * 7f);

            bool parpadea = t > _siguienteParpadeo && t < _siguienteParpadeo + 0.12f;
            if (t > _siguienteParpadeo + 0.12f) _siguienteParpadeo = t + Random.Range(2.5f, 5f);
            PonerCara(cara, parpadea);
        }

        /// <summary>Llegada: anda (pasos alternos, balanceo, brazos) y, si toca, tropieza, cae de morros y se levanta.</summary>
        private void AnimarEntrada(float t, ref Vector3 pos, ref Vector3 escala, ref float giro, ref float inclinacion, ref float alzar, ref float balanceo, ref Expresion cara)
        {
            float transcurrido = t - _entradaInicio;
            float mitad = _entradaAndar * 0.5f;
            float andado = transcurrido;
            float tropiezo = -1f;
            if (_tropieza && transcurrido > mitad)
            {
                if (transcurrido < mitad + DuracionTropiezo)
                {
                    tropiezo = (transcurrido - mitad) / DuracionTropiezo;
                    andado = mitad;
                }
                else andado = transcurrido - DuracionTropiezo;
            }
            float k = Mathf.Clamp01(andado / _entradaAndar);
            pos = Vector3.Lerp(_entradaDesde, _base, k);
            var dir = _base - _entradaDesde;
            giro = Mathf.Atan2(-dir.x, -dir.z) * Mathf.Rad2Deg;
            if (tropiezo < 0f)
            {
                float fase = andado * 13f;
                PonerPies(fase, 1f);
                pos += Vector3.up * Mathf.Abs(Mathf.Cos(fase)) * 0.035f;
                inclinacion = -6f;
                balanceo = Mathf.Sin(fase);
                alzar = 0.1f;
                // los últimos pasos se gira hacia la cámara
                if (k > 0.85f) giro = Mathf.LerpAngle(giro, GiroReposo, (k - 0.85f) / 0.15f);
                return;
            }
            // tropiezo: traspié (0-0.13), caída de morros (0.13-0.3), en el suelo (0.3-0.65), se levanta (0.65-1)
            if (tropiezo < 0.13f)
            {
                inclinacion = -22f * (tropiezo / 0.13f);
                alzar = 1f;
                cara = Expresion.Susto;
            }
            else if (tropiezo < 0.3f)
            {
                float c = (tropiezo - 0.13f) / 0.17f;
                inclinacion = Mathf.Lerp(-22f, -82f, c * c);
                alzar = 1f;
                cara = Expresion.Susto;
            }
            else if (tropiezo < 0.65f)
            {
                float c = (tropiezo - 0.3f) / 0.35f;
                inclinacion = -82f + Mathf.Sin(c * Mathf.PI * 2f) * 4f * (1f - c);
                escala = Aplastado(0.12f * (1f - c));
                alzar = -0.3f;
                cara = Expresion.Dolor;
            }
            else
            {
                float c = (tropiezo - 0.65f) / 0.35f;
                inclinacion = Mathf.Lerp(-82f, 0f, Mathf.SmoothStep(0f, 1f, c));
                pos += new Vector3(Mathf.Sin(t * 40f) * 0.02f * c, 0f, 0f);
                alzar = 0.3f;
                cara = c > 0.7f ? Expresion.Normal : Expresion.Dolor;
            }
        }

        private static Vector3 Aplastado(float k) => new Vector3(1f + k, 1f - k, 1f + k);

        /// <summary>Pasos: cada bota sube y avanza por turnos. intensidad 0 = quietas.</summary>
        private void PonerPies(float fase, float intensidad)
        {
            float paso = Mathf.Sin(fase);
            if (_pieIzq != null) _pieIzq.localPosition = _pieIzqBase + new Vector3(0, Mathf.Max(0f, paso) * 0.06f, -paso * 0.07f) * intensidad;
            if (_pieDer != null) _pieDer.localPosition = _pieDerBase + new Vector3(0, Mathf.Max(0f, -paso) * 0.06f, paso * 0.07f) * intensidad;
        }

        /// <summary>
        /// Bracitos de fieltro: en reposo apuntan hacia fuera y abajo; al alzarlos (alzar 1) giran hacia arriba
        /// como en la hoja ("¡bien!"), cuelgan más rectos si está triste (alzar &lt; 0) y se balancean al andar.
        /// </summary>
        private void PonerBrazos(float alzar, float balanceo)
        {
            PonerBrazo(_brazoIzq, _brazoIzqBase, _brazoIzqRot, -1f, alzar, -balanceo);
            PonerBrazo(_brazoDer, _brazoDerBase, _brazoDerRot, 1f, alzar, balanceo);
        }

        private void PonerBrazo(Transform brazo, Vector3 basePos, Quaternion baseRot, float lado, float alzar, float balanceo)
        {
            if (brazo == null) return;
            if (_brazosConHombro)
            {
                // el brazo sale del hombro hacia fuera: alzarlo es girarlo hacia arriba (antihorario el derecho, horario el izquierdo)
                float arriba = lado * (alzar >= 0f ? 80f * alzar : 25f * alzar);
                brazo.localRotation = baseRot * Quaternion.Euler(balanceo * 20f, 0f, arriba);
                return;
            }
            float giro = lado * 48f * (alzar >= 0f ? 1f - 2f * alzar : 1f + alzar);
            brazo.localRotation = Quaternion.Euler(balanceo * 25f, 0f, giro);
            brazo.localPosition = basePos + new Vector3(lado * 0.03f * Mathf.Max(0f, alzar), 0.13f * Mathf.Max(0f, alzar), balanceo * 0.06f);
        }

        private void PonerCara(Expresion e, bool parpadeo)
        {
            Cara = e;
            var rasgos = Modelos.Rasgos(e);
            float parpado = rasgos.Parpado;
            if (parpadeo && rasgos.Ojos <= 1f) parpado = 1f;
            PonerOjo(_ojos[0], rasgos.OjoIzq, parpado, rasgos.Ojos);
            PonerOjo(_ojos[1], rasgos.OjoDer, parpado, rasgos.Ojos);
            Mostrar(_bocaFeliz, rasgos.Boca == 0);
            if (_bocaFeliz != null) _bocaFeliz.localScale = Vector3.one * rasgos.Sonrisa;
            Mostrar(_bocaO, rasgos.Boca == 1);
            Mostrar(_bocaTriste, rasgos.Boca == 2);
        }

        private static void PonerOjo(Ojo ojo, EstadoOjo estado, float parpado, float tamano)
        {
            if (ojo == null) return;
            bool abierto = estado == EstadoOjo.Abierto;
            Mostrar(ojo.Abierto, abierto);
            if (ojo.Abierto != null && abierto) ojo.Abierto.localScale = ojo.Escala * tamano;
            bool conParpado = abierto && parpado > 0.02f;
            Mostrar(ojo.Parpado, conParpado);
            if (ojo.Parpado != null && conParpado) ojo.Parpado.localScale = new Vector3(1f, parpado, 1f);
            Mostrar(ojo.Feliz, estado == EstadoOjo.Feliz);
            Mostrar(ojo.Apretado, estado == EstadoOjo.Apretado);
        }

        private static void Mostrar(Transform t, bool visible)
        {
            if (t != null && t.gameObject.activeSelf != visible) t.gameObject.SetActive(visible);
        }
    }
}
