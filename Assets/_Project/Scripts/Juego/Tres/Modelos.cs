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
        public readonly List<Pieza> Hijos = new List<Pieza>();

        public Pieza Hijo(Pieza p)
        {
            Hijos.Add(p);
            return p;
        }
    }

    /// <summary>Catálogo de mallas por clave (con caché), compartido por todos los modelos.</summary>
    public static class CatalogoMallas
    {
        private static readonly Dictionary<string, DatosMalla> Cache = new Dictionary<string, DatosMalla>();

        public static DatosMalla Obtener(string clave)
        {
            if (Cache.TryGetValue(clave, out var m)) return m;
            var partes = clave.Split(':');
            var p = partes.Length > 1 ? Numeros(partes[1]) : new float[0];
            switch (partes[0])
            {
                case "esfera": m = Mallas.Esfera(0.5f, 18); break;
                case "esfera_baja": m = Mallas.Esfera(0.5f, 10); break;
                case "caja": m = Mallas.CajaRedondeada(p[0], p[1], p[2], p[3]); break;
                case "cilindro": m = Mallas.Cilindro(p[0], p[1], p[2], p.Length > 3 ? (int)p[3] : 24); break;
                case "toro": m = Mallas.Toro(p[0], p[1]); break;
                default: m = Mallas.Esfera(0.5f, 12); break;
            }
            Cache[clave] = m;
            return m;
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
        private static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
    }

    /// <summary>Medidas del tablero (1 casilla = 1 unidad).</summary>
    public static class Medidas
    {
        /// <summary>Altura de la cara superior de las casillas de fieltro.</summary>
        public const float AlturaCasilla = 0.16f;
        /// <summary>Altura del eje de la lana sobre el suelo.</summary>
        public const float AlturaLana = AlturaCasilla + 0.07f;
        public const float RadioLana = 0.11f;
        /// <summary>Donde apoyan las patas las polillas (encima de la lana).</summary>
        public const float AlturaPolilla = AlturaLana + 0.06f;
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

        // ================================================================== Ovillo

        public static Pieza Ovillo()
        {
            var lana = Paleta.Hex("#F4D3A3");
            var raiz = Pivote("Ovillo", Vector3.zero);
            var cuerpo = raiz.Hijo(Pivote("Cuerpo", V(0, 0.38f, 0), null, "cuerpo"));
            cuerpo.Hijo(P("Ovillo", "esfera", "lana", lana, Vector3.zero, Vector3.one * 0.68f));
            // vueltas de lana enrolladas: lo que lo hace leerse como un ovillo
            float[] giros = { 20, 75, 130, -35 };
            for (int i = 0; i < giros.Length; i++)
                cuerpo.Hijo(P("Vuelta " + i, CatalogoMallas.Toro(0.325f, 0.032f), "lana", Paleta.Oscurecer(lana, 0.06f), Vector3.zero, null, V(giros[i], i * 47f, 90 - giros[i] * 0.5f)));
            // cara (mira hacia -Z, hacia la cámara)
            var cara = cuerpo.Hijo(Pivote("Cara", Vector3.zero, null, "cara"));
            for (int lado = -1; lado <= 1; lado += 2)
            {
                var ojo = cara.Hijo(P("Ojo", "esfera", "brillante", Oscuro, V(lado * 0.1f, 0.07f, -0.3f), V(0.075f, 0.1f, 0.05f), null, "ojos"));
                ojo.Hijo(P("Brillo", "esfera_baja", "brillante", Blanco, V(-0.22f, 0.25f, -0.45f), Vector3.one * 0.32f));
                cara.Hijo(P("Mejilla", "esfera_baja", "fieltro", Paleta.Mejillas, V(lado * 0.2f, -0.02f, -0.275f), V(0.08f, 0.055f, 0.03f), V(0, lado * -30f, 0)));
                cuerpo.Hijo(P("Brazo", "esfera", "lana", lana, V(lado * 0.34f, -0.08f, -0.04f), Vector3.one * 0.14f, null, lado < 0 ? "brazo_izq" : "brazo_der"));
                raiz.Hijo(P("Bota", "esfera", "fieltro", Paleta.Botas, V(lado * 0.14f, 0.05f, -0.03f), V(0.16f, 0.11f, 0.22f), null, lado < 0 ? "pie_izq" : "pie_der"));
            }
            cara.Hijo(P("Boca", "esfera_baja", "brillante", Paleta.Hex("#8E2A20"), V(0, -0.04f, -0.31f), V(0.06f, 0.04f, 0.03f)));
            cara.Hijo(P("Lengua", "esfera_baja", "fieltro", Paleta.Hex("#F06A62"), V(0, -0.05f, -0.318f), V(0.035f, 0.018f, 0.015f)));
            // bufanda roja: su seña de identidad
            var bufanda = cuerpo.Hijo(Pivote("Bufanda", V(0, -0.15f, 0), null, "bufanda"));
            bufanda.Hijo(P("Vuelta bufanda", CatalogoMallas.Toro(0.3f, 0.075f), "lana", Paleta.Bufanda, Vector3.zero, V(1f, 0.8f, 1f)));
            var cola = bufanda.Hijo(Pivote("Cola", V(0.24f, 0.02f, 0.12f), V(0, 0, -20), "cola_bufanda"));
            cola.Hijo(P("Tira", CatalogoMallas.Caja(0.13f, 0.32f, 0.05f, 0.022f), "lana", Paleta.Bufanda, V(0.05f, -0.14f, 0)));
            cola.Hijo(P("Flecos", CatalogoMallas.Caja(0.14f, 0.05f, 0.06f, 0.02f), "lana", Paleta.BufandaSombra, V(0.05f, -0.31f, 0)));
            return raiz;
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
                    return new EstiloPolilla { Cuerpo = Paleta.Hex("#EE8C9E"), Ala = Paleta.Hex("#F7BCC7"), AlaMancha = Paleta.Hex("#EE8AA0"), Antena = Paleta.Hex("#7A4436"), Mejilla = Paleta.Hex("#F37E7E"), Tamano = 0.34f, EscalaAla = 1.25f, Antenas = 1.5f };
                case TipoPolilla.Acorazada:
                    return new EstiloPolilla { Cuerpo = Paleta.Hex("#706B8A"), Ala = Paleta.Hex("#EED6B4"), AlaMancha = Paleta.Hex("#D9BE98"), Antena = Paleta.Hex("#4A3A3A"), Mejilla = Paleta.Hex("#E9B070"), Tamano = 0.5f, EscalaAla = 0.85f, Antenas = 1f };
                case TipoPolilla.Reina:
                    return new EstiloPolilla { Cuerpo = Paleta.Hex("#D7A2C8"), Ala = Paleta.Hex("#7B3F94"), AlaMancha = Paleta.Hex("#C25B86"), Antena = Paleta.Hex("#6C3A4E"), Mejilla = Paleta.Hex("#F08DA0"), Tamano = 0.62f, EscalaAla = 1.55f, Antenas = 0.8f };
                default:
                    return new EstiloPolilla { Cuerpo = Paleta.Hex("#9467B8"), Ala = Paleta.Hex("#F3DEC0"), AlaMancha = Paleta.Hex("#C7A6DE"), Antena = Paleta.Hex("#5A3550"), Mejilla = Paleta.Hex("#E8869E"), Tamano = 0.42f, EscalaAla = 1f, Antenas = 1f };
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
            var cuerpo = raiz.Hijo(Pivote("Cuerpo", V(0, s * 0.58f, 0), null, "cuerpo"));
            cuerpo.Hijo(P("Bola de lana", "esfera", "lana", e.Cuerpo, Vector3.zero, V(s, s * 0.95f, s * 1.04f)));

            // alas: pivote en el lomo para poder batirlas
            for (int lado = -1; lado <= 1; lado += 2)
            {
                var pivote = cuerpo.Hijo(Pivote("Ala", V(lado * s * 0.18f, s * 0.2f, s * 0.16f), V(-18, 0, lado * -28), lado < 0 ? "ala_izq" : "ala_der"));
                float a = s * 0.62f * e.EscalaAla;
                pivote.Hijo(P("Ala alta", "esfera", "fieltro", e.Ala, V(lado * a * 0.5f, a * 0.25f, 0.02f), V(a, s * 0.06f, a * 0.72f), V(0, 0, lado * 12)));
                pivote.Hijo(P("Mancha", "esfera_baja", "fieltro", e.AlaMancha, V(lado * a * 0.62f, a * 0.27f + s * 0.032f, 0.02f), V(a * 0.36f, s * 0.02f, a * 0.28f), V(0, 0, lado * 12)));
                pivote.Hijo(P("Ala baja", "esfera", "fieltro", Paleta.Oscurecer(e.Ala, 0.06f), V(lado * a * 0.36f, -a * 0.1f, a * 0.32f), V(a * 0.62f, s * 0.05f, a * 0.46f), V(0, 30 * lado, lado * 8)));
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
                var ojo = cara.Hijo(P("Ojo", "esfera", "brillante", Oscuro, V(lado * s * 0.17f, s * 0.05f, z), V(s * 0.16f, s * 0.2f, s * 0.1f), null, "ojos"));
                ojo.Hijo(P("Brillo", "esfera_baja", "brillante", Blanco, V(-0.2f, 0.25f, -0.45f), Vector3.one * 0.3f));
                cara.Hijo(P("Ceja", CatalogoMallas.Caja(s * 0.2f, s * 0.045f, s * 0.05f, s * 0.02f), "fieltro", Oscuro, V(lado * s * 0.17f, s * 0.21f, z * 0.96f), null, V(0, 0, lado * (tipo == TipoPolilla.Acorazada ? 30 : 22))));
                cara.Hijo(P("Mejilla", "esfera_baja", "fieltro", e.Mejilla, V(lado * s * 0.31f, -s * 0.1f, z * 0.82f), V(s * 0.12f, s * 0.08f, s * 0.04f), V(0, lado * -35f, 0)));
                if (tipo == TipoPolilla.Reina)
                    cara.Hijo(P("Sombra de ojos", "esfera_baja", "fieltro", Paleta.Hex("#8E4FA3"), V(lado * s * 0.17f, s * 0.14f, z * 0.985f), V(s * 0.2f, s * 0.07f, s * 0.05f)));
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
                    capa.Hijo(P("Cuello de piel", CatalogoMallas.Toro(s * 0.34f, s * 0.11f), "lana", Paleta.Hex("#F4E2CB"), V(0, s * 0.2f, 0)));
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
            raiz.Hijo(P("Disco", CatalogoMallas.Cilindro(0.75f, 0.75f, 0.12f), "madera", Color.white, V(0, 0.06f, 0)));
            raiz.Hijo(P("Hilo", CatalogoMallas.Cilindro(0.6f, 0.6f, 1.1f), "lana", hilo, V(0, 0.67f, 0)));
            raiz.Hijo(P("Disco", CatalogoMallas.Cilindro(0.75f, 0.75f, 0.12f), "madera", Color.white, V(0, 1.28f, 0)));
            return raiz;
        }

        public static Pieza OvilloSuelto(Color color)
        {
            var raiz = Pivote("Ovillo suelto", Vector3.zero);
            raiz.Hijo(P("Bola", "esfera", "lana", color, V(0, 0.45f, 0), Vector3.one * 0.9f));
            for (int i = 0; i < 4; i++)
                raiz.Hijo(P("Vuelta", CatalogoMallas.Toro(0.43f, 0.04f), "lana", Paleta.Oscurecer(color, 0.07f), V(0, 0.45f, 0), null, V(30 + i * 40, i * 55, 80 - i * 25)));
            return raiz;
        }

        public static Pieza BotonSuelto(Color color, float radio)
        {
            var raiz = Pivote("Botón suelto", Vector3.zero);
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
            var raiz = Pivote("Rollo de tela", Vector3.zero, V(0, 0, 90));
            raiz.Hijo(P("Rollo", CatalogoMallas.Cilindro(0.45f, 0.45f, 2.6f), "tela", color, V(0.45f, 0, 0)));
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
