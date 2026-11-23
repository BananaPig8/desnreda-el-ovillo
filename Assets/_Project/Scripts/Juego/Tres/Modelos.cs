using System.Collections.Generic;
using System.Globalization;
using DesenredaElOvillo.Nucleo;
using UnityEngine;

namespace DesenredaElOvillo.Juego.Tres
{
    /// <summary>
    /// Una pieza de un modelo: malla + material + color + transformación local + hijos.
    /// Los modelos provisionales se describen como datos (sin GameObjects) para poder construirlos en Unity
    /// y también exportarlos a la vista previa fuera del editor. El arte final los sustituirá por FBX
    /// (ver Docs/04_Direccion_de_arte.md), manteniendo los mismos nombres de grupo para las animaciones.
    /// </summary>
    public sealed class Pieza
    {
        public string Nombre;
        /// <summary>Clave de malla: "esfera", "caja:ancho,alto,fondo,radio", "cilindro:rAbajo,rArriba,alto", "toro:R,r". Null = solo pivote.</summary>
        public string Malla;
        public string Material = "fieltro";
        public Color Color = Color.white;
        public Vector3 Posicion;
        public Vector3 Rotacion;
        public Vector3 Escala = Vector3.one;
        /// <summary>Grupo para animar (alas, ojos, placas...).</summary>
        public string Grupo;
        public bool ProyectaSombra = true;
        /// <summary>Capas de pelusa alrededor de la pieza (bordes mullidos y deshilachados). 0 = ninguna.</summary>
        public int Pelusa;
        /// <summary>Las capas de pelusa dejan libre la cara (hacia -Z local) para no tapar ojos ni boca.</summary>
        public bool CaraLibre;
        /// <summary>Empieza oculta (expresiones alternativas: ojos felices, bocas, ojos en X...).</summary>
        public bool Oculta;
        /// <summary>Sombra de contacto bajo la pieza (ancho, fondo) en su plano XZ local. Cero = sin sombra.</summary>
        public Vector2 Sombra;
        public readonly List<Pieza> Hijos = new List<Pieza>();

        public Pieza Hijo(Pieza p)
        {
            Hijos.Add(p);
            return p;
        }
    }

    /// <summary>
    /// Pelusa en capas (técnica de "shells"): copias de la malla un poco infladas con fibras recortadas por
    /// transparencia. Cada fibra atraviesa más o menos capas según su altura, así el borde se ve mullido y
    /// deshilachado sin un solo pelo modelado. Reglas compartidas por Unity y la vista previa.
    /// </summary>
    public static class CapasPelusa
    {
        /// <summary>Separación entre capas, en unidades de la malla (3,2 % del radio de la esfera base).</summary>
        public const float Espesor = 0.016f;
        public const float RepeticionUv = 1.25f;
        /// <summary>Cono de unos 45° delante de la cara que queda sin pelusa.</summary>
        public const float CosenoCara = 0.7f;

        public static string Malla(string clave, int capa, bool caraLibre) => clave + "|pelusa:" + capa + (caraLibre ? ",cara" : "");
        /// <summary>
        /// Umbral de recorte: fibras escasas (22 %, 14 % y 6 % de la superficie en las capas 1, 2 y 3). De frente
        /// apenas salpican la lana; en el contorno las capas se apilan y forman el borde mullido.
        /// </summary>
        public static float Corte(int capa) => 0.7f + 0.08f * capa;
        /// <summary>Las puntas de las fibras cogen un poco de luz.</summary>
        public static Color Tono(Color color, int capa) => Color.Lerp(color, Color.white, 0.02f * capa);
    }

    /// <summary>Catálogo de mallas por clave (con caché), compartido por todos los modelos.</summary>
    public static class CatalogoMallas
    {
        private static readonly Dictionary<string, DatosMalla> Cache = new Dictionary<string, DatosMalla>();

        /// <summary>Todas las mallas generadas hasta ahora (para verificarlas o exportarlas).</summary>
        public static IEnumerable<KeyValuePair<string, DatosMalla>> Generadas => Cache;

        public static DatosMalla Obtener(string clave)
        {
            if (Cache.TryGetValue(clave, out var m)) return m;
            int barra = clave.IndexOf('|');
            if (barra >= 0)
            {
                // "base|pelusa:capa[,cara]": una capa de pelusa de otra malla del catálogo
                string claveBase = clave.Substring(0, barra);
                var baseMalla = Obtener(claveBase);
                var mod = clave.Substring(barra + 1).Split(':');
                var args = mod.Length > 1 ? mod[1].Split(',') : new string[0];
                int capa = args.Length > 0 ? int.Parse(args[0], CultureInfo.InvariantCulture) : 1;
                bool cara = args.Length > 1 && args[1] == "cara";
                ParametrosPelusa(claveBase, out float espesor, out Vector2? escalaUv);
                m = Mallas.Capa(baseMalla, capa * espesor, CapasPelusa.RepeticionUv,
                    cara ? new Vector3(0, 0, -1) : (Vector3?)null, CapasPelusa.CosenoCara, escalaUv);
                Cache[clave] = m;
                return m;
            }
            var partes = clave.Split(':');
            var p = partes.Length > 1 ? Numeros(partes[1]) : new float[0];
            switch (partes[0])
            {
                case "esfera": m = Mallas.Esfera(0.5f, 18); break;
                case "esfera_baja": m = Mallas.Esfera(0.5f, 10); break;
                case "caja": m = Mallas.CajaRedondeada(p[0], p[1], p[2], p[3]); break;
                case "cilindro": m = Mallas.Cilindro(p[0], p[1], p[2], p.Length > 3 ? (int)p[3] : 24); break;
                case "toro": m = Mallas.Toro(p[0], p[1]); break;
                case "plano": m = Mallas.Plano(p[0], p[1]); break;
                case "semiesfera": m = Mallas.Semiesfera(0.5f, 16); break;
                case "hebras": m = Mallas.OvilloDeHebras((int)p[0], p[1], p[2], (int)p[3], p[0] >= 14 ? 40 : 34, p[0] >= 14 ? 8 : 7); break;
                case "anillo": m = Mallas.Anillo(p[0], p[1]); break;
                case "colgante": m = Mallas.Colgante(p[0], p[1]); break;
                default: m = Mallas.Esfera(0.5f, 12); break;
            }
            Cache[clave] = m;
            return m;
        }

        /// <summary>
        /// Separación entre capas y UV de la pelusa según la malla base: las esferas usan la proyección octaédrica;
        /// los tubos (hebras, bufanda) y las cajas, sus propias UV escaladas para que cada fibra mida lo mismo.
        /// </summary>
        private static void ParametrosPelusa(string claveBase, out float espesor, out Vector2? escalaUv)
        {
            var partes = claveBase.Split(':');
            var p = partes.Length > 1 ? Numeros(partes[1]) : new float[0];
            switch (partes[0])
            {
                case "hebras": espesor = 0.22f * p[2]; escalaUv = new Vector2(0.5f, 0.5f); break;
                case "anillo":
                case "colgante": espesor = 0.22f * p[1]; escalaUv = new Vector2(0.33f, 0.19f); break;
                case "caja": espesor = CapasPelusa.Espesor * 0.6f; escalaUv = Vector2.one; break;
                default: espesor = CapasPelusa.Espesor; escalaUv = null; break;
            }
        }

        private static float[] Numeros(string s)
        {
            var trozos = s.Split(',');
            var r = new float[trozos.Length];
            for (int i = 0; i < trozos.Length; i++) r[i] = float.Parse(trozos[i], CultureInfo.InvariantCulture);
            return r;
        }

        public static string Caja(float a, float h, float f, float r) => "caja:" + F(a) + "," + F(h) + "," + F(f) + "," + F(r);
        public static string Cilindro(float r1, float r2, float h) => "cilindro:" + F(r1) + "," + F(r2) + "," + F(h);
        public static string Toro(float R, float r) => "toro:" + F(R) + "," + F(r);
        public static string Plano(float a, float f) => "plano:" + F(a) + "," + F(f);
        /// <summary>Bola de "hebras" vueltas de lana de radio "grosor" enrolladas a "radio" del centro.</summary>
        public static string Hebras(int hebras, float radio, float grosor, int semilla) => "hebras:" + hebras + "," + F(radio) + "," + F(grosor) + "," + semilla;
        public static string Anillo(float R, float r) => "anillo:" + F(R) + "," + F(r);
        public static string Colgante(float largo, float r) => "colgante:" + F(largo) + "," + F(r);
        private static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Caras de Ovillo (las de su hoja de personaje). Se montan con los grupos del modelo: por cada ojo, el ojo
    /// abierto, su párpado, el ojo feliz "^" y el ojo apretado "&gt; &lt;"; y tres bocas.
    /// </summary>
    public enum Expresion
    {
        Normal,
        Feliz,
        Guino,
        Preocupado,
        Susto,
        Triste,
        Dolor,
        Euforia
    }

    public enum EstadoOjo
    {
        Abierto,
        Feliz,
        Apretado
    }

    /// <summary>Cómo se monta una expresión (lo usan VistaOvillo en el juego y la vista previa fuera de Unity).</summary>
    public struct RasgosCara
    {
        /// <summary>0 = ojos abiertos, 1 = cerrados (solo en los ojos abiertos).</summary>
        public float Parpado;
        /// <summary>Escala de los ojos (muy abiertos > 1).</summary>
        public float Ojos;
        /// <summary>Escala de la boca abierta y sonriente.</summary>
        public float Sonrisa;
        public EstadoOjo OjoIzq, OjoDer;
        /// <summary>0 = sonrisa abierta con lengua, 1 = boca en "o", 2 = boca triste.</summary>
        public int Boca;
    }

    /// <summary>Medidas del tablero (1 casilla = 1 unidad).</summary>
    public static class Medidas
    {
        /// <summary>Altura de la cara superior de las casillas de fieltro.</summary>
        public const float AlturaCasilla = 0.16f;
        /// <summary>Altura del eje de la lana sobre el suelo.</summary>
        public const float AlturaLana = AlturaCasilla + 0.07f;
        public const float RadioLana = 0.11f;
        /// <summary>Donde apoyan las patas las polillas (sobre la lana, hundiéndose un poco en ella).</summary>
        public const float AlturaPolilla = AlturaLana + RadioLana - 0.03f;
        /// <summary>Opacidad de las sombras de contacto (manchas suaves bajo personajes y objetos).</summary>
        public const float OpacidadSombra = 0.45f;

        /// <summary>Radio del ovillito que rueda en la punta de la lana según el hilo que queda (0..1).</summary>
        public static float RadioOvillito(float hiloRestante) => Mathf.Lerp(0.13f, 0.3f, Mathf.Sqrt(Mathf.Clamp01(hiloRestante)));

        /// <summary>Centro del ovillito: delante de la punta de la lana, apoyado en el fieltro.</summary>
        public static Vector3 PosicionOvillito(Vector3 punta, Vector3 direccion, float radio)
        {
            var p = punta + direccion * (radio * 0.55f);
            p.y = AlturaCasilla + radio - 0.015f;
            return p;
        }
    }

    /// <summary>Modelos provisionales del juego, construidos con primitivas suaves y materiales textiles.</summary>
    public static class Modelos
    {
        private static Pieza P(string nombre, string malla, string material, Color color, Vector3 pos, Vector3? escala = null, Vector3? rot = null, string grupo = null)
            => new Pieza { Nombre = nombre, Malla = malla, Material = material, Color = color, Posicion = pos, Escala = escala ?? Vector3.one, Rotacion = rot ?? Vector3.zero, Grupo = grupo };

        private static Pieza Pivote(string nombre, Vector3 pos, Vector3? rot = null, string grupo = null)
            => new Pieza { Nombre = nombre, Posicion = pos, Rotacion = rot ?? Vector3.zero, Grupo = grupo };

        private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        private static readonly Color Oscuro = Paleta.Hex("#2A140D");
        private static readonly Color Blanco = Paleta.Hex("#FFF8EE");

        // ================================================================== expresiones

        public static RasgosCara Rasgos(Expresion e)
        {
            var r = new RasgosCara { Ojos = 1f, Sonrisa = 0.9f };
            switch (e)
            {
                case Expresion.Feliz: r.OjoIzq = r.OjoDer = EstadoOjo.Feliz; r.Sonrisa = 1.12f; break;
                case Expresion.Guino: r.OjoDer = EstadoOjo.Feliz; r.Sonrisa = 1f; break;
                case Expresion.Preocupado: r.Boca = 1; r.Ojos = 1.06f; break;
                case Expresion.Susto: r.Boca = 1; r.Ojos = 1.16f; break;
                case Expresion.Triste: r.Boca = 2; r.Parpado = 0.45f; break;
                case Expresion.Dolor: r.OjoIzq = r.OjoDer = EstadoOjo.Apretado; r.Boca = 2; break;
                case Expresion.Euforia: r.OjoIzq = r.OjoDer = EstadoOjo.Apretado; r.Sonrisa = 1.22f; break;
            }
            return r;
        }

        /// <summary>Aplica una expresión a la descripción de un modelo (para la vista previa y las hojas de personaje).</summary>
        public static void AplicarCara(Pieza raiz, RasgosCara r)
        {
            Recorrer(raiz, p =>
            {
                if (p.Grupo == null) return;
                var lado = p.Grupo.EndsWith("_izq") ? r.OjoIzq : r.OjoDer;
                switch (p.Grupo)
                {
                    case "parpado_izq":
                    case "parpado_der": p.Oculta = lado != EstadoOjo.Abierto || r.Parpado <= 0.02f; p.Escala = new Vector3(1f, Mathf.Max(0.001f, r.Parpado), 1f); break;
                    case "ojo_izq":
                    case "ojo_der": p.Oculta = lado != EstadoOjo.Abierto; p.Escala = p.Escala * r.Ojos; break;
                    case "feliz_izq":
                    case "feliz_der": p.Oculta = lado != EstadoOjo.Feliz; break;
                    case "apretado_izq":
                    case "apretado_der": p.Oculta = lado != EstadoOjo.Apretado; break;
                    case "boca_feliz": p.Oculta = r.Boca != 0; p.Escala = Vector3.one * r.Sonrisa; break;
                    case "boca_o": p.Oculta = r.Boca != 1; break;
                    case "boca_triste": p.Oculta = r.Boca != 2; break;
                }
            });
        }

        /// <summary>Párpados de una polilla (0 abiertos, 1 cerrados) y ojos en X si ha caído.</summary>
        public static void AplicarCaraPolilla(Pieza raiz, float parpado, bool derrotada)
        {
            Recorrer(raiz, p =>
            {
                switch (p.Grupo)
                {
                    case "parpados": p.Oculta = derrotada || parpado <= 0.02f; p.Escala = new Vector3(1f, Mathf.Max(0.001f, parpado), 1f); break;
                    case "ojos": p.Oculta = derrotada; break;
                    case "ojos_x": p.Oculta = !derrotada; break;
                }
            });
        }

        private static void Recorrer(Pieza p, System.Action<Pieza> accion)
        {
            accion(p);
            foreach (var h in p.Hijos) Recorrer(h, accion);
        }

        // ================================================================== Ovillo

        /// <summary>
        /// Ovillo según su hoja de personaje (Docs/Arte/referencias/ovillo_hoja_referencia.png): una bola de hebras
        /// gruesas de lana crema con pelusa, ojos grandes y brillantes, mofletes de fieltro rosa, boca abierta con
        /// lengua, bufanda roja de punto con nudo y flecos, bracitos y botas de fieltro. Raíz en los pies; la cara
        /// mira hacia -Z. Grupos: cuerpo, brazo_izq/der, pie_izq/der, ojo_*, parpado_*, feliz_*, apretado_*,
        /// boca_feliz, boca_o, boca_triste, bufanda, cola_bufanda.
        /// </summary>
        public static Pieza Ovillo()
        {
            var lana = Paleta.Hex("#F8DDA8");
            var lanaFondo = Paleta.Hex("#D9AF78");
            var fieltroCrema = Paleta.Hex("#F0D2A0");
            var raiz = Pivote("Ovillo", Vector3.zero);
            raiz.Sombra = new Vector2(0.8f, 0.68f);
            var cuerpo = raiz.Hijo(Pivote("Cuerpo", V(0, 0.4f, 0), null, "cuerpo"));
            // núcleo oscuro (las rendijas entre hebras se ven hondas) + vueltas de lana gruesa con pelusa
            cuerpo.Hijo(P("Núcleo", "esfera", "lana", lanaFondo, Vector3.zero, Vector3.one * 0.62f));
            var hebras = cuerpo.Hijo(P("Hebras", CatalogoMallas.Hebras(24, 0.3f, 0.045f, 7), "lana", lana, Vector3.zero));
            hebras.Pelusa = 2;

            // cara, apoyada sobre las hebras (radio 0,383)
            const float rc = 0.383f;
            var cara = cuerpo.Hijo(Pivote("Cara", Vector3.zero, null, "cara"));
            for (int lado = -1; lado <= 1; lado += 2)
            {
                string sufijo = lado < 0 ? "_izq" : "_der";
                var ojo = cara.Hijo(EnCara("Ojo" + sufijo, lado * 0.128f, 0.06f, rc));
                var globo = ojo.Hijo(P("Globo", "esfera", "brillante", Paleta.Hex("#2B140A"), Vector3.zero, V(0.142f, 0.162f, 0.078f), null, "ojo" + sufijo));
                globo.Hijo(P("Iris", "esfera", "brillante", Paleta.Hex("#6E3819"), V(0, -0.16f, -0.2f), V(0.74f, 0.6f, 0.72f)));
                globo.Hijo(P("Pupila", "esfera", "brillante", Paleta.Hex("#140703"), V(0, -0.04f, -0.3f), V(0.56f, 0.58f, 0.64f)));
                globo.Hijo(P("Brillo", "esfera_baja", "brillante", Blanco, V(-0.2f, 0.2f, -0.55f), V(0.34f, 0.3f, 0.2f)));
                globo.Hijo(P("Brillo pequeño", "esfera_baja", "brillante", Blanco, V(0.2f, -0.22f, -0.58f), V(0.13f, 0.12f, 0.15f)));
                // párpado: se cierra escalando su pivote en Y (0 = abierto, 1 = cerrado)
                var parpado = ojo.Hijo(Pivote("Párpado", V(0, 0.083f, -0.006f), null, "parpado" + sufijo));
                parpado.Escala = V(1f, 0.001f, 1f);
                parpado.Hijo(P("Piel", "esfera", "lana", lana, V(0, -0.083f, -0.004f), V(0.157f, 0.177f, 0.088f)));
                // ojo feliz "^"
                var feliz = ojo.Hijo(Pivote("Ojo feliz", V(0, 0.004f, -0.032f), null, "feliz" + sufijo));
                feliz.Oculta = true;
                Trazo(feliz, -0.0268f, 0f, 0.07f, 40f);
                Trazo(feliz, 0.0268f, 0f, 0.07f, -40f);
                // ojo apretado: ">" en el izquierdo y "<" en el derecho, con la punta hacia la nariz
                var apretado = ojo.Hijo(Pivote("Ojo apretado", V(0, 0f, -0.032f), null, "apretado" + sufijo));
                apretado.Oculta = true;
                Trazo(apretado, lado * 0.0103f, 0.0175f, 0.07f, lado * 30f);
                Trazo(apretado, lado * 0.0103f, -0.0175f, 0.07f, -lado * 30f);
                // moflete de fieltro rosa
                var moflete = cara.Hijo(EnCara("Moflete", lado * 0.24f, -0.066f, rc + 0.004f));
                moflete.Hijo(P("Moflete", "esfera", "fieltro", Paleta.Hex("#F5929F"), Vector3.zero, V(0.122f, 0.094f, 0.04f))).Pelusa = 1;
                // bracito de fieltro (apunta hacia fuera y abajo; al alzarlo gira hacia arriba)
                cuerpo.Hijo(P("Brazo", "esfera", "fieltro", fieltroCrema, V(lado * 0.385f, -0.14f, -0.035f), V(0.1f, 0.17f, 0.1f), V(0, 0, lado * 48f), lado < 0 ? "brazo_izq" : "brazo_der")).Pelusa = 1;
                // bota de fieltro marrón
                raiz.Hijo(P("Bota", "esfera", "fieltro", Paleta.Hex("#6B3920"), V(lado * 0.135f, 0.062f, -0.035f), V(0.175f, 0.13f, 0.235f), null, lado < 0 ? "pie_izq" : "pie_der")).Pelusa = 1;
            }
            // boca abierta con lengua (sonrisa), boca en "o" y boca triste
            var bocaFeliz = cara.Hijo(EnCara("Boca feliz", 0f, -0.082f, rc + 0.004f, "boca_feliz"));
            bocaFeliz.Hijo(P("Boca", "semiesfera", "brillante", Paleta.Hex("#4A1014"), Vector3.zero, V(0.13f, 0.1f, 0.05f)));
            bocaFeliz.Hijo(P("Lengua", "esfera", "fieltro", Paleta.Hex("#FF8A9C"), V(0, -0.034f, -0.013f), V(0.068f, 0.036f, 0.032f)));
            var bocaO = cara.Hijo(EnCara("Boca de susto", 0f, -0.1f, rc + 0.004f, "boca_o"));
            bocaO.Oculta = true;
            bocaO.Hijo(P("Boca", "esfera", "brillante", Paleta.Hex("#6A1A1A"), Vector3.zero, V(0.055f, 0.068f, 0.035f)));
            var bocaTriste = cara.Hijo(EnCara("Boca triste", 0f, -0.1f, rc + 0.004f, "boca_triste"));
            bocaTriste.Oculta = true;
            Trazo(bocaTriste, -0.02f, 0f, 0.05f, 26f);
            Trazo(bocaTriste, 0.02f, 0f, 0.05f, -26f);

            // bufanda roja de punto: una vuelta gruesa, el nudo y una punta con flecos
            var rojo = Paleta.Hex("#C8232C");
            var bufanda = cuerpo.Hijo(Pivote("Bufanda", V(0, -0.205f, 0), null, "bufanda"));
            bufanda.Hijo(P("Vuelta bufanda", CatalogoMallas.Anillo(0.305f, 0.074f), "punto", rojo, Vector3.zero, V(1f, 0.82f, 1f))).Pelusa = 1;
            bufanda.Hijo(P("Nudo", "esfera", "punto", Paleta.Oscurecer(rojo, 0.05f), V(-0.27f, -0.01f, 0.17f), V(0.15f, 0.13f, 0.13f))).Pelusa = 1;
            var cola = bufanda.Hijo(Pivote("Cola", V(-0.3f, -0.03f, 0.15f), V(0, -35, -10), "cola_bufanda"));
            cola.Hijo(P("Punta", CatalogoMallas.Colgante(0.14f, 0.058f), "punto", rojo, Vector3.zero, V(1f, 1f, 0.42f))).Pelusa = 1;
            for (int i = 0; i < 6; i++)
                cola.Hijo(P("Fleco", CatalogoMallas.Cilindro(0.011f, 0.008f, 0.075f), "lana", Paleta.Oscurecer(rojo, 0.08f), V(-0.045f + i * 0.018f, -0.19f, 0.03f), null, V(0, 0, (i - 2.5f) * 5f)));
            return raiz;
        }

        /// <summary>Pivote sobre la superficie de la cabeza (esfera de radio "radio"), orientado para que su -Z mire hacia fuera.</summary>
        private static Pieza EnCara(string nombre, float x, float y, float radio, string grupo = null)
        {
            float z = -Mathf.Sqrt(Mathf.Max(0f, radio * radio - x * x - y * y));
            var n = new Vector3(x, y, z) / radio;
            return Pivote(nombre, new Vector3(x, y, z), new Vector3(Mathf.Asin(n.y) * Mathf.Rad2Deg, Mathf.Atan2(-n.x, -n.z) * Mathf.Rad2Deg, 0f), grupo);
        }

        /// <summary>Trazo de fieltro oscuro (ojos "^" y "&gt;", boca triste).</summary>
        private static void Trazo(Pieza padre, float x, float y, float largo, float grados)
        {
            padre.Hijo(P("Trazo", CatalogoMallas.Caja(largo, 0.022f, 0.022f, 0.009f), "fieltro", Oscuro, V(x, y, 0f), null, V(0, 0, grados)));
        }

        // ================================================================== Polillas

        private struct EstiloPolilla
        {
            public Color Cuerpo, Ala, AlaMancha, Antena, Mejilla;
            public float Tamano, EscalaAla, Antenas;
        }

        private static EstiloPolilla Estilo(TipoPolilla t)
        {
            switch (t)
            {
                case TipoPolilla.Veloz:
                    return new EstiloPolilla { Cuerpo = Paleta.Hex("#EE8C9E"), Ala = Paleta.Hex("#F7BCC7"), AlaMancha = Paleta.Hex("#EE8AA0"), Antena = Paleta.Hex("#7A4436"), Mejilla = Paleta.Hex("#F37E7E"), Tamano = 0.46f, EscalaAla = 1.2f, Antenas = 1.5f };
                case TipoPolilla.Acorazada:
                    return new EstiloPolilla { Cuerpo = Paleta.Hex("#7D7896"), Ala = Paleta.Hex("#EED6B4"), AlaMancha = Paleta.Hex("#D9BE98"), Antena = Paleta.Hex("#4A3A3A"), Mejilla = Paleta.Hex("#E9B070"), Tamano = 0.64f, EscalaAla = 0.8f, Antenas = 1f };
                case TipoPolilla.Reina:
                    return new EstiloPolilla { Cuerpo = Paleta.Hex("#D7A2C8"), Ala = Paleta.Hex("#7B3F94"), AlaMancha = Paleta.Hex("#C25B86"), Antena = Paleta.Hex("#6C3A4E"), Mejilla = Paleta.Hex("#F08DA0"), Tamano = 0.82f, EscalaAla = 1.35f, Antenas = 0.8f };
                default:
                    return new EstiloPolilla { Cuerpo = Paleta.Hex("#9467B8"), Ala = Paleta.Hex("#F3DEC0"), AlaMancha = Paleta.Hex("#C7A6DE"), Antena = Paleta.Hex("#5A3550"), Mejilla = Paleta.Hex("#E8869E"), Tamano = 0.56f, EscalaAla = 1f, Antenas = 1f };
            }
        }

        /// <summary>
        /// Polilla de lana con alas de fieltro. Raíz en los pies; cara hacia -Z (avance hacia -Z local, se orienta en juego).
        /// Grupos: cuerpo, ala_izq, ala_der, patas, ojos, placas (acorazada).
        /// </summary>
        public static Pieza Polilla(TipoPolilla tipo)
        {
            var e = Estilo(tipo);
            float s = e.Tamano;
            var raiz = Pivote(tipo.ToString(), Vector3.zero);
            raiz.Sombra = new Vector2(s * 1.3f, s * 1.2f);
            var cuerpo = raiz.Hijo(Pivote("Cuerpo", V(0, s * 0.58f, 0), null, "cuerpo"));
            // cuerpo: un ovillo de hebras gruesas con pelusa, como Ovillo (núcleo más oscuro para dar fondo)
            var forma = V(s, s * 0.95f, s * 1.04f);
            cuerpo.Hijo(P("Núcleo", "esfera", "lana", Paleta.Oscurecer(e.Cuerpo, 0.18f), Vector3.zero, forma * 0.8f));
            var bolaLana = cuerpo.Hijo(P("Bola de lana", CatalogoMallas.Hebras(12, 0.38f, 0.06f, 3 + (int)tipo), "lana", e.Cuerpo, Vector3.zero, forma));
            bolaLana.Pelusa = 2;
            bolaLana.CaraLibre = true;

            // alas de fieltro detrás del cuerpo, levantadas en V (como en la hoja de personajes);
            // la bisagra es vertical, en el lomo, para batirlas abriendo y cerrando
            for (int lado = -1; lado <= 1; lado += 2)
            {
                var pivote = cuerpo.Hijo(Pivote("Ala", V(lado * s * 0.12f, s * 0.12f, s * 0.3f), V(-12, lado * 18, 0), lado < 0 ? "ala_izq" : "ala_der"));
                float a = s * 0.78f * e.EscalaAla;
                pivote.Hijo(P("Ala alta", "esfera", "fieltro", e.Ala, V(lado * a * 0.46f, a * 0.42f, 0), V(a * 0.95f, a * 1.05f, s * 0.06f), V(0, 0, lado * -24)));
                pivote.Hijo(P("Mancha", "esfera_baja", "fieltro", e.AlaMancha, V(lado * a * 0.55f, a * 0.55f, -s * 0.032f), V(a * 0.36f, a * 0.4f, s * 0.02f), V(0, 0, lado * -24)));
                pivote.Hijo(P("Ala baja", "esfera", "fieltro", Paleta.Oscurecer(e.Ala, 0.06f), V(lado * a * 0.42f, -a * 0.12f, s * 0.02f), V(a * 0.62f, a * 0.56f, s * 0.05f), V(0, 0, lado * 30)));
            }

            // antenas
            for (int lado = -1; lado <= 1; lado += 2)
            {
                var antena = cuerpo.Hijo(Pivote("Antena", V(lado * s * 0.14f, s * 0.4f, -s * 0.12f), V(-25, 0, lado * -22)));
                float largo = s * 0.32f * e.Antenas;
                antena.Hijo(P("Tallo", CatalogoMallas.Cilindro(0.012f, 0.01f, largo), "fieltro", e.Antena, V(0, largo / 2f, 0)));
                antena.Hijo(P("Bolita", "esfera_baja", "lana", Paleta.Aclarar(e.Antena, 0.1f), V(0, largo, 0), Vector3.one * s * 0.12f));
            }

            // cara con cejas enfadadas (como en la hoja de personajes)
            var cara = cuerpo.Hijo(Pivote("Cara", Vector3.zero, null, "cara"));
            float z = -s * 0.47f;
            for (int lado = -1; lado <= 1; lado += 2)
            {
                var ojo = cara.Hijo(P("Ojo", "esfera", "brillante", Oscuro, V(lado * s * 0.18f, s * 0.04f, z), V(s * 0.21f, s * 0.26f, s * 0.12f), null, "ojos"));
                ojo.Hijo(P("Brillo", "esfera_baja", "brillante", Blanco, V(-0.2f, 0.25f, -0.45f), Vector3.one * 0.3f));
                var parpado = cara.Hijo(Pivote("Párpado", V(lado * s * 0.18f, s * 0.17f, z - s * 0.005f), null, "parpados"));
                parpado.Escala = V(1f, 0.001f, 1f);
                parpado.Hijo(P("Piel", "esfera", "lana", e.Cuerpo, V(0, -s * 0.13f, -s * 0.01f), V(s * 0.23f, s * 0.27f, s * 0.13f)));
                var ojoX = cara.Hijo(Pivote("Ojo en X", V(lado * s * 0.18f, s * 0.04f, z * 1.03f), null, "ojos_x"));
                ojoX.Oculta = true;
                ojoX.Hijo(P("Trazo", CatalogoMallas.Caja(s * 0.2f, s * 0.04f, s * 0.04f, s * 0.015f), "fieltro", Oscuro, Vector3.zero, null, V(0, 0, 45)));
                ojoX.Hijo(P("Trazo", CatalogoMallas.Caja(s * 0.2f, s * 0.04f, s * 0.04f, s * 0.015f), "fieltro", Oscuro, Vector3.zero, null, V(0, 0, -45)));
                cara.Hijo(P("Ceja", CatalogoMallas.Caja(s * 0.22f, s * 0.05f, s * 0.05f, s * 0.022f), "fieltro", Oscuro, V(lado * s * 0.18f, s * 0.22f, z * 0.94f), null, V(0, 0, lado * (tipo == TipoPolilla.Acorazada ? 30 : 22))));
                cara.Hijo(P("Mejilla", "esfera_baja", "fieltro", e.Mejilla, V(lado * s * 0.31f, -s * 0.1f, z * 0.82f), V(s * 0.12f, s * 0.08f, s * 0.04f), V(0, lado * -35f, 0)));
                if (tipo == TipoPolilla.Reina)
                    cara.Hijo(P("Sombra de ojos", "esfera_baja", "fieltro", Paleta.Hex("#8E4FA3"), V(lado * s * 0.18f, s * 0.15f, z * 0.985f), V(s * 0.24f, s * 0.08f, s * 0.05f)));
            }
            cara.Hijo(P("Boca", CatalogoMallas.Caja(s * 0.1f, s * 0.025f, s * 0.03f, s * 0.012f), "fieltro", Oscuro, V(0, -s * 0.12f, z * 1.01f)));

            // patitas
            var patas = raiz.Hijo(Pivote("Patas", Vector3.zero, null, "patas"));
            for (int k = 0; k < 3; k++)
            {
                for (int lado = -1; lado <= 1; lado += 2)
                    patas.Hijo(P("Pata", "esfera_baja", "fieltro", Paleta.Hex("#4A2A20"), V(lado * s * 0.26f, s * 0.07f, (k - 1) * s * 0.26f), V(s * 0.1f, s * 0.2f, s * 0.1f), V(0, 0, lado * 25)));
            }

            switch (tipo)
            {
                case TipoPolilla.Acorazada:
                {
                    var placas = cuerpo.Hijo(Pivote("Placas", Vector3.zero, null, "placas"));
                    var col = Paleta.Hex("#5E5A78");
                    for (int lado = -1; lado <= 1; lado += 2)
                        placas.Hijo(P("Faldón", "esfera", "fieltro", col, V(lado * s * 0.48f, -s * 0.05f, 0.02f), V(s * 0.16f, s * 0.7f, s * 0.86f), V(0, 0, lado * -12)));
                    placas.Hijo(P("Peto", CatalogoMallas.Caja(s * 0.5f, s * 0.22f, s * 0.12f, s * 0.05f), "fieltro", col, V(0, -s * 0.3f, -s * 0.38f)));
                    break;
                }
                case TipoPolilla.Reina:
                {
                    var capa = cuerpo.Hijo(Pivote("Capa", V(0, -s * 0.12f, 0), null, "capa"));
                    capa.Hijo(P("Capa", "esfera", "tela", Paleta.Hex("#B8395E"), V(0, -s * 0.12f, s * 0.06f), V(s * 1.12f, s * 0.75f, s * 1.08f)));
                    var cuello = capa.Hijo(P("Cuello de piel", CatalogoMallas.Toro(s * 0.4f, s * 0.13f), "lana", Paleta.Hex("#F4E2CB"), V(0, s * 0.02f, 0), V(1f, 0.9f, 1f)));
                    cuello.Pelusa = 2;
                    for (int lado = -1; lado <= 1; lado += 2)
                        capa.Hijo(P("Botón dorado", CatalogoMallas.Cilindro(s * 0.07f, s * 0.07f, s * 0.03f), "oro", Paleta.Mostaza, V(lado * s * 0.2f, 0, -s * 0.5f), null, V(90, 0, 0)));
                    var corona = cuerpo.Hijo(Pivote("Corona", V(0, s * 0.5f, 0), null, "corona"));
                    corona.Hijo(P("Aro", CatalogoMallas.Cilindro(s * 0.18f, s * 0.2f, s * 0.09f), "oro", Paleta.Dorado, Vector3.zero));
                    for (int k = 0; k < 5; k++)
                    {
                        float ang = k * 72f * Mathf.Deg2Rad;
                        corona.Hijo(P("Punta", CatalogoMallas.Cilindro(s * 0.045f, 0f, s * 0.12f), "oro", Paleta.Dorado, V(Mathf.Sin(ang) * s * 0.17f, s * 0.1f, -Mathf.Cos(ang) * s * 0.17f)));
                    }
                    corona.Hijo(P("Gema", "esfera_baja", "brillante", Paleta.Hex("#E5383B"), V(0, 0.01f, -s * 0.2f), Vector3.one * s * 0.08f));
                    break;
                }
            }
            return raiz;
        }

        // ================================================================== Herramientas

        public static Pieza Herramienta(TipoHerramienta tipo, bool doblada = false)
        {
            var raiz = Pivote(tipo.ToString(), Vector3.zero);
            switch (tipo)
            {
                case TipoHerramienta.Alfiler:
                {
                    var aguja = raiz.Hijo(Pivote("Aguja", V(0.06f, 0, 0.04f), V(doblada ? 55 : 18, 0, doblada ? -40 : -14), "aguja"));
                    aguja.Hijo(P("Metal", CatalogoMallas.Cilindro(0.008f, 0.019f, 0.46f), "metal", Paleta.Plata, V(0, 0.2f, 0)));
                    aguja.Hijo(P("Cabeza", "esfera", "boton", doblada ? Paleta.GrisFieltro : Paleta.Coral, V(0, 0.44f, 0), Vector3.one * 0.13f, null, "cabeza"));
                    break;
                }
                case TipoHerramienta.Velcro:
                    raiz.Hijo(P("Velcro", CatalogoMallas.Caja(0.8f, 0.04f, 0.8f, 0.02f), "velcro", Paleta.Hex("#6C8FB5"), V(0, 0.02f, 0)));
                    break;
                case TipoHerramienta.Bobina:
                {
                    var b = raiz.Hijo(Pivote("Bobina", Vector3.zero, null, "bobina"));
                    b.Hijo(P("Disco", CatalogoMallas.Cilindro(0.2f, 0.2f, 0.045f), "madera", Color.white, V(0, 0.025f, 0)));
                    b.Hijo(P("Hilo", CatalogoMallas.Cilindro(0.16f, 0.16f, 0.27f), "lana", Paleta.Mostaza, V(0, 0.18f, 0)));
                    b.Hijo(P("Disco", CatalogoMallas.Cilindro(0.2f, 0.2f, 0.045f), "madera", Color.white, V(0, 0.335f, 0)));
                    b.Hijo(P("Cabo suelto", CatalogoMallas.Toro(0.09f, 0.016f), "lana", Paleta.Mostaza, V(0.18f, 0.03f, -0.12f), null, V(0, 30, 0)));
                    break;
                }
                case TipoHerramienta.Tijeras:
                {
                    var t = raiz.Hijo(Pivote("Tijeras", V(0, 0.05f, 0), V(0, 35, 0)));
                    t.Hijo(P("Hoja", CatalogoMallas.Caja(0.07f, 0.025f, 0.46f, 0.012f), "metal", Paleta.Plata, V(0, 0, 0.12f), null, V(0, 10, 0), "hoja_a"));
                    t.Hijo(P("Hoja", CatalogoMallas.Caja(0.07f, 0.025f, 0.46f, 0.012f), "metal", Paleta.Hex("#B5BAC2"), V(0, 0.018f, 0.12f), null, V(0, -10, 0), "hoja_b"));
                    t.Hijo(P("Tornillo", CatalogoMallas.Cilindro(0.025f, 0.025f, 0.05f), "metal", Paleta.Oscurecer(Paleta.Plata, 0.3f), V(0, 0.02f, -0.06f)));
                    for (int lado = -1; lado <= 1; lado += 2)
                        t.Hijo(P("Mango", CatalogoMallas.Toro(0.075f, 0.026f), "boton", Paleta.Coral, V(lado * 0.08f, 0.01f, -0.22f)));
                    break;
                }
                case TipoHerramienta.Boton:
                {
                    for (int k = 0; k < 3; k++)
                        raiz.Hijo(P("Muelle", CatalogoMallas.Toro(0.12f, 0.018f), "metal", Paleta.Plata, V(0, 0.03f + k * 0.045f, 0), null, V(k % 2 == 0 ? 6 : -6, 0, 0), "muelle"));
                    var b = raiz.Hijo(Pivote("Botón", V(0, 0.19f, 0), null, "boton"));
                    b.Hijo(P("Botón", CatalogoMallas.Cilindro(0.27f, 0.27f, 0.07f), "boton", Paleta.Mostaza, Vector3.zero));
                    b.Hijo(P("Reborde", CatalogoMallas.Toro(0.24f, 0.025f), "boton", Paleta.Oscurecer(Paleta.Mostaza, 0.12f), V(0, 0.035f, 0)));
                    for (int i = 0; i < 4; i++)
                    {
                        float a = (45 + 90 * i) * Mathf.Deg2Rad;
                        b.Hijo(P("Agujero", CatalogoMallas.Cilindro(0.035f, 0.035f, 0.02f), "fieltro", Paleta.Oscurecer(Paleta.Mostaza, 0.55f), V(Mathf.Cos(a) * 0.08f, 0.032f, Mathf.Sin(a) * 0.08f)));
                    }
                    break;
                }
            }
            return raiz;
        }

        // ================================================================== Tablero

        public static Pieza Obstaculo(char simbolo)
        {
            var raiz = Pivote("Obstáculo " + simbolo, Vector3.zero);
            raiz.Sombra = Vector2.one * (simbolo == 'u' ? 0.72f : simbolo == 'c' ? 0.84f : 0.95f);
            switch (simbolo)
            {
                case 'o':
                    raiz.Hijo(P("Botón gigante", CatalogoMallas.Cilindro(0.4f, 0.4f, 0.13f), "boton", Paleta.AzulSuave, V(0, 0.065f, 0)));
                    raiz.Hijo(P("Reborde", CatalogoMallas.Toro(0.35f, 0.04f), "boton", Paleta.Oscurecer(Paleta.AzulSuave, 0.12f), V(0, 0.13f, 0)));
                    for (int i = 0; i < 4; i++)
                    {
                        float a = (45 + 90 * i) * Mathf.Deg2Rad;
                        raiz.Hijo(P("Agujero", CatalogoMallas.Cilindro(0.055f, 0.055f, 0.02f), "fieltro", Paleta.Oscurecer(Paleta.AzulSuave, 0.6f), V(Mathf.Cos(a) * 0.12f, 0.131f, Mathf.Sin(a) * 0.12f)));
                    }
                    break;
                case 'u':
                    raiz.Hijo(P("Dedal", CatalogoMallas.Cilindro(0.27f, 0.22f, 0.5f), "metal", Paleta.Plata, V(0, 0.25f, 0)));
                    raiz.Hijo(P("Cúpula", "esfera", "metal", Paleta.Plata, V(0, 0.5f, 0), V(0.44f, 0.2f, 0.44f)));
                    raiz.Hijo(P("Ribete", CatalogoMallas.Toro(0.27f, 0.03f), "metal", Paleta.Oscurecer(Paleta.Plata, 0.15f), V(0, 0.02f, 0)));
                    break;
                case 'c':
                    raiz.Hijo(P("Disco", CatalogoMallas.Cilindro(0.33f, 0.33f, 0.06f), "madera", Color.white, V(0, 0.03f, 0)));
                    raiz.Hijo(P("Hilo", CatalogoMallas.Cilindro(0.26f, 0.26f, 0.42f), "lana", Paleta.AzulSuave, V(0, 0.27f, 0)));
                    raiz.Hijo(P("Disco", CatalogoMallas.Cilindro(0.33f, 0.33f, 0.06f), "madera", Color.white, V(0, 0.51f, 0)));
                    break;
                case 'a':
                    raiz.Hijo(P("Acerico", "esfera", "fieltro", Paleta.Coral, V(0, 0.2f, 0), V(0.74f, 0.44f, 0.74f)));
                    for (int i = 0; i < 3; i++)
                    {
                        var al = Herramienta(TipoHerramienta.Alfiler);
                        al.Posicion = V(-0.15f + i * 0.15f, 0.3f, 0.05f);
                        raiz.Hijo(al);
                    }
                    break;
                case 'm':
                    raiz.Hijo(P("Metro", CatalogoMallas.Cilindro(0.36f, 0.36f, 0.16f), "boton", Paleta.Mostaza, V(0, 0.08f, 0)));
                    raiz.Hijo(P("Cinta", CatalogoMallas.Caja(0.3f, 0.02f, 0.1f, 0.005f), "tela", Paleta.Crema, V(0.35f, 0.02f, -0.1f)));
                    break;
                default:
                    raiz.Hijo(P("Caja de costura", CatalogoMallas.Caja(0.82f, 0.42f, 0.74f, 0.07f), "madera", Color.white, V(0, 0.21f, 0)));
                    raiz.Hijo(P("Tapa", CatalogoMallas.Caja(0.86f, 0.07f, 0.78f, 0.03f), "madera", Paleta.Hex("#D9B58C"), V(0, 0.4f, 0)));
                    raiz.Hijo(P("Cierre", CatalogoMallas.Caja(0.12f, 0.1f, 0.04f, 0.015f), "oro", Paleta.Dorado, V(0, 0.33f, -0.38f)));
                    break;
            }
            return raiz;
        }

        /// <summary>Agujero del rodapié. "fuera" es la dirección hacia el exterior del tablero.</summary>
        public static Pieza Entrada(Vector3 fuera)
        {
            float giro = Mathf.Atan2(fuera.x, fuera.z) * Mathf.Rad2Deg;
            var raiz = Pivote("Entrada", Vector3.zero, V(0, giro, 0));
            raiz.Hijo(P("Rodapié", CatalogoMallas.Caja(1.0f, 0.62f, 0.2f, 0.05f), "madera", Paleta.Hex("#B98A5E"), V(0, 0.31f - Medidas.AlturaCasilla, 0.42f)));
            raiz.Hijo(P("Agujero", CatalogoMallas.Cilindro(0.27f, 0.27f, 0.24f), "fieltro", Paleta.Hex("#1B0F09"), V(0, 0f, 0.41f), null, V(90, 0, 0)));
            raiz.Hijo(P("Marco", CatalogoMallas.Toro(0.28f, 0.035f), "madera", Paleta.Hex("#8E5A31"), V(0, 0f, 0.31f), null, V(90, 0, 0)));
            return raiz;
        }

        /// <summary>El carrete que protege Ovillo. "cantidad" (0..1) es el hilo que le queda: la vida, a la vista.</summary>
        public static Pieza Carrete(float cantidad)
        {
            var raiz = Pivote("Carrete", Vector3.zero);
            raiz.Sombra = Vector2.one * 1.02f;
            raiz.Hijo(P("Disco", CatalogoMallas.Cilindro(0.43f, 0.43f, 0.08f), "madera", Color.white, V(0, 0.04f, 0)));
            raiz.Hijo(P("Eje", CatalogoMallas.Cilindro(0.18f, 0.18f, 0.66f), "madera", Paleta.Hex("#E2C29A"), V(0, 0.39f, 0)));
            float r = Mathf.Lerp(0.19f, 0.38f, Mathf.Clamp01(cantidad));
            if (cantidad > 0.01f) raiz.Hijo(P("Hilo", CatalogoMallas.Cilindro(r, r, 0.58f), "lana", Paleta.Bufanda, V(0, 0.39f, 0), null, null, "hilo"));
            raiz.Hijo(P("Disco", CatalogoMallas.Cilindro(0.43f, 0.43f, 0.08f), "madera", Color.white, V(0, 0.74f, 0)));
            if (cantidad > 0.01f) raiz.Hijo(P("Cabo", CatalogoMallas.Toro(0.1f, 0.02f), "lana", Paleta.Bufanda, V(r * 0.9f, 0.08f, -0.25f), null, V(70, 20, 0)));
            return raiz;
        }

        public static Pieza Lampara()
        {
            var raiz = Pivote("Lámpara", Vector3.zero);
            raiz.Sombra = Vector2.one * 0.76f;
            raiz.Hijo(P("Base", CatalogoMallas.Cilindro(0.3f, 0.26f, 0.08f), "madera", Color.white, V(0, 0.04f, 0)));
            raiz.Hijo(P("Pie", CatalogoMallas.Cilindro(0.03f, 0.03f, 0.56f), "oro", Paleta.Dorado, V(0, 0.36f, 0)));
            raiz.Hijo(P("Bombilla", "esfera", "emisivo", Paleta.Hex("#FFE7A6"), V(0, 0.55f, 0), Vector3.one * 0.18f, null, "bombilla"));
            raiz.Hijo(P("Pantalla", CatalogoMallas.Cilindro(0.36f, 0.19f, 0.28f), "fieltro", Paleta.Mostaza, V(0, 0.68f, 0)));
            return raiz;
        }

        /// <summary>Huella rosa: dónde aterrizará la polilla veloz.</summary>
        public static Pieza Huella()
        {
            var raiz = Pivote("Huella", Vector3.zero);
            for (int lado = -1; lado <= 1; lado += 2)
                raiz.Hijo(P("Pisada", CatalogoMallas.Cilindro(0.045f, 0.045f, 0.012f), "fieltro", Paleta.RosaPastel, V(lado * 0.045f, 0, lado * 0.02f), V(1f, 1f, 1.5f)));
            return raiz;
        }

        // ================================================================== Decorado del taller

        public static Pieza BobinaGrande(Color hilo)
        {
            var raiz = Pivote("Bobina grande", Vector3.zero);
            raiz.Sombra = Vector2.one * 1.8f;
            raiz.Hijo(P("Disco", CatalogoMallas.Cilindro(0.75f, 0.75f, 0.12f), "madera", Color.white, V(0, 0.06f, 0)));
            raiz.Hijo(P("Hilo", CatalogoMallas.Cilindro(0.6f, 0.6f, 1.1f), "lana", hilo, V(0, 0.67f, 0)));
            raiz.Hijo(P("Disco", CatalogoMallas.Cilindro(0.75f, 0.75f, 0.12f), "madera", Color.white, V(0, 1.28f, 0)));
            return raiz;
        }

        /// <summary>El ovillito que rueda en la punta de la lana mientras se teje (diámetro 1; la vista lo escala).</summary>
        public static Pieza OvilloDeTrabajo(Color color)
        {
            var raiz = Pivote("Ovillo de trabajo", Vector3.zero);
            var bola = raiz.Hijo(P("Bola", "esfera", "lana", color, Vector3.zero, Vector3.one, null, "bola"));
            bola.Pelusa = 2;
            for (int i = 0; i < 3; i++)
                raiz.Hijo(P("Vuelta", CatalogoMallas.Toro(0.48f, 0.045f), "lana", Paleta.Oscurecer(color, 0.08f), Vector3.zero, null, V(25 + i * 50, i * 60, 70 - i * 30)));
            return raiz;
        }

        public static Pieza OvilloSuelto(Color color)
        {
            var raiz = Pivote("Ovillo suelto", Vector3.zero);
            raiz.Sombra = Vector2.one * 1.15f;
            var bolaSuelta = raiz.Hijo(P("Bola", "esfera", "lana", color, V(0, 0.45f, 0), Vector3.one * 0.9f));
            bolaSuelta.Pelusa = 2;
            for (int i = 0; i < 4; i++)
                raiz.Hijo(P("Vuelta", CatalogoMallas.Toro(0.43f, 0.04f), "lana", Paleta.Oscurecer(color, 0.07f), V(0, 0.45f, 0), null, V(30 + i * 40, i * 55, 80 - i * 25)));
            return raiz;
        }

        public static Pieza BotonSuelto(Color color, float radio)
        {
            var raiz = Pivote("Botón suelto", Vector3.zero);
            raiz.Sombra = Vector2.one * radio * 2.4f;
            raiz.Hijo(P("Botón", CatalogoMallas.Cilindro(radio, radio, radio * 0.3f), "boton", color, V(0, radio * 0.15f, 0)));
            raiz.Hijo(P("Reborde", CatalogoMallas.Toro(radio * 0.86f, radio * 0.1f), "boton", Paleta.Oscurecer(color, 0.12f), V(0, radio * 0.3f, 0)));
            for (int i = 0; i < 4; i++)
            {
                float a = (45 + 90 * i) * Mathf.Deg2Rad;
                raiz.Hijo(P("Agujero", CatalogoMallas.Cilindro(radio * 0.13f, radio * 0.13f, radio * 0.04f), "fieltro", Paleta.Oscurecer(color, 0.6f), V(Mathf.Cos(a) * radio * 0.3f, radio * 0.31f, Mathf.Sin(a) * radio * 0.3f)));
            }
            return raiz;
        }

        public static Pieza RolloDeTela(Color color)
        {
            var raiz = Pivote("Rollo de tela", Vector3.zero);
            raiz.Sombra = new Vector2(2.9f, 1.1f);
            raiz.Hijo(P("Rollo", CatalogoMallas.Cilindro(0.45f, 0.45f, 2.6f), "tela", color, V(0, 0.45f, 0), null, V(0, 0, 90)));
            return raiz;
        }

        public static Pieza TijerasGrandes()
        {
            var t = Herramienta(TipoHerramienta.Tijeras);
            t.Escala = Vector3.one * 3.2f;
            return t;
        }
    }
}
