using System.Collections.Generic;
using DesenredaElOvillo.Nucleo;
using UnityEngine;

namespace DesenredaElOvillo.Juego
{
    /// <summary>
    /// Catálogo de sprites. Primero busca arte final en Resources/Arte/... (mismo nombre); si no existe,
    /// genera un provisional con el Pintor. Así el arte se puede ir sustituyendo pieza a pieza sin tocar código.
    /// </summary>
    public static class Texturas
    {
        public const int PixelesPorUnidad = 128;
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Texture2D> CacheTex = new Dictionary<string, Texture2D>();

        public static void Vaciar()
        {
            Cache.Clear();
            CacheTex.Clear();
        }

        private static Sprite Obtener(string clave, System.Func<Pintor> dibujar, float ppu = PixelesPorUnidad, Vector2? pivote = null)
        {
            if (Cache.TryGetValue(clave, out var s) && s != null) return s;
            var final = Resources.Load<Texture2D>("Arte/" + clave);
            Texture2D tex = final != null ? final : dibujar().ATextura();
            if (final == null) tex.name = clave;
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivote ?? new Vector2(0.5f, 0.5f),
                final != null ? tex.width : ppu, 0, SpriteMeshType.FullRect);
            s.name = clave;
            Cache[clave] = s;
            return s;
        }

        public static Texture2D TexturaDe(Sprite s) => s != null ? s.texture : null;

        private static Material _materialSprites;

        /// <summary>
        /// Material sin iluminación para todos los sprites: el juego se ve igual con el pipeline integrado
        /// que con URP 2D, aunque la escena no tenga luces 2D. (Cuando el arte final use luces 2D,
        /// basta con devolver null aquí para que los SpriteRenderer usen el material por defecto.)
        /// </summary>
        public static Material MaterialSprites
        {
            get
            {
                if (_materialSprites == null)
                {
                    var shader = Shader.Find("Sprites/Default");
                    if (shader != null) _materialSprites = new Material(shader) { name = "Sprites sin luz" };
                }
                return _materialSprites;
            }
        }

        public static void PrepararRenderer(SpriteRenderer sr)
        {
            var m = MaterialSprites;
            if (m != null) sr.sharedMaterial = m;
        }

        // ================================================================== Personajes

        /// <summary>Ovillo (imagen de referencia del proyecto). Pivote en los pies.</summary>
        public static Sprite Ovillo()
        {
            const string clave = "Personajes/ovillo";
            if (Cache.TryGetValue(clave, out var s) && s != null) return s;
            var tex = Resources.Load<Texture2D>("Arte/" + clave);
            if (tex == null)
            {
                var p = new Pintor(128, 128);
                p.Forma(q => Pintor.Circulo(q, new Vector2(64, 66), 50), Paleta.LanaOvillo, 0.35f, 7);
                p.Forma(q => Pintor.Caja(q, new Vector2(64, 50), 52, 9, 8), Paleta.Bufanda, 0.3f, 8);
                p.Forma(q => Pintor.Elipse(q, new Vector2(80, 78), 6, 9), Paleta.Ojos);
                p.Forma(q => Pintor.Elipse(q, new Vector2(100, 78), 6, 9), Paleta.Ojos);
                tex = p.ATextura();
            }
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.04f), tex.height / 1.35f, 0, SpriteMeshType.FullRect);
            s.name = clave;
            Cache[clave] = s;
            return s;
        }

        /// <summary>Colores de cada polilla, tomados de la hoja de personajes (Docs/Arte/referencias/hoja_personajes.png).</summary>
        private struct EstiloPolilla
        {
            public Color Cuerpo, Sombra, Ala, AlaPuntada, AlaMancha, Antena, Mejilla;
            public float Radio, AlaRx, AlaRy, LargoAntena;
        }

        private static EstiloPolilla Estilo(TipoPolilla t)
        {
            switch (t)
            {
                case TipoPolilla.Veloz:
                    return new EstiloPolilla
                    {
                        Cuerpo = Paleta.Hex("#EE8C9E"), Sombra = Paleta.Hex("#C25F78"), Ala = Paleta.Hex("#F7BCC7"),
                        AlaPuntada = Paleta.Hex("#D9708A"), AlaMancha = Paleta.Hex("#F29AAE"), Antena = Paleta.Hex("#7A4436"),
                        Mejilla = Paleta.Hex("#F37E7E"), Radio = 29, AlaRx = 25, AlaRy = 33, LargoAntena = 30
                    };
                case TipoPolilla.Acorazada:
                    return new EstiloPolilla
                    {
                        Cuerpo = Paleta.Hex("#706B8A"), Sombra = Paleta.Hex("#4A4661"), Ala = Paleta.Hex("#EED6B4"),
                        AlaPuntada = Paleta.Hex("#8A7F9E"), AlaMancha = Paleta.Hex("#D9BE98"), Antena = Paleta.Hex("#4A3A3A"),
                        Mejilla = Paleta.Hex("#E9B070"), Radio = 40, AlaRx = 24, AlaRy = 28, LargoAntena = 22
                    };
                case TipoPolilla.Reina:
                    return new EstiloPolilla
                    {
                        Cuerpo = Paleta.Hex("#D7A2C8"), Sombra = Paleta.Hex("#A9709C"), Ala = Paleta.Hex("#7B3F94"),
                        AlaPuntada = Paleta.Hex("#E7A6C2"), AlaMancha = Paleta.Hex("#B4537A"), Antena = Paleta.Hex("#6C3A4E"),
                        Mejilla = Paleta.Hex("#F08DA0"), Radio = 34, AlaRx = 30, AlaRy = 36, LargoAntena = 18
                    };
                default:
                    return new EstiloPolilla
                    {
                        Cuerpo = Paleta.Hex("#9467B8"), Sombra = Paleta.Hex("#62408A"), Ala = Paleta.Hex("#F3DEC0"),
                        AlaPuntada = Paleta.Hex("#9B6CC0"), AlaMancha = Paleta.Hex("#C7A6DE"), Antena = Paleta.Hex("#5A3550"),
                        Mejilla = Paleta.Hex("#E8869E"), Radio = 34, AlaRx = 24, AlaRy = 30, LargoAntena = 24
                    };
            }
        }

        /// <summary>Alas de fieltro cosidas (sprite aparte para poder batirlas).</summary>
        public static Sprite AlasPolilla(TipoPolilla tipo) => Obtener("Polillas/alas_" + tipo, () =>
        {
            var p = new Pintor(128, 128);
            var e = Estilo(tipo);
            for (int lado = -1; lado <= 1; lado += 2)
            {
                int l = lado;
                var alta = new Vector2(64 + l * 34, 76);
                var baja = new Vector2(64 + l * 30, 44);
                float angAlta = l * -28, angBaja = l * 30;
                p.Sombra(q => Pintor.Elipse(q, alta + new Vector2(2, -3), e.AlaRx, e.AlaRy, angAlta), 4, 0.25f);
                p.Forma(q => Pintor.Elipse(q, baja, e.AlaRx * 0.72f, e.AlaRy * 0.6f, angBaja), Paleta.Oscurecer(e.Ala, 0.06f), 0.35f, 21 + l);
                p.Forma(q => Pintor.Elipse(q, alta, e.AlaRx, e.AlaRy, angAlta), e.Ala, 0.4f, 11 + l,
                    q => 0.88f + 0.18f * Mathf.Clamp01((q.y - 40f) / 70f));
                p.Forma(q => Pintor.Elipse(q, alta + Pintor.Rotar(new Vector2(0, e.AlaRy * 0.35f), angAlta), e.AlaRx * 0.35f, e.AlaRy * 0.28f, angAlta), Paleta.ConAlfa(e.AlaMancha, 0.85f), 0.3f, 31 + l);
                p.Puntadas(q => Mathf.Abs(Pintor.Elipse(q, alta, e.AlaRx - 6, e.AlaRy - 6, angAlta)) - 1.3f,
                    q => Mathf.Atan2(q.y - alta.y, q.x - alta.x) * 30f, 5, 4, e.AlaPuntada);
                p.Puntadas(q => Mathf.Abs(Pintor.Elipse(q, baja, e.AlaRx * 0.72f - 5, e.AlaRy * 0.6f - 5, angBaja)) - 1.2f,
                    q => Mathf.Atan2(q.y - baja.y, q.x - baja.x) * 22f, 4, 4, e.AlaPuntada);
                if (tipo == TipoPolilla.Reina)
                    p.Forma(q => Mathf.Abs(Pintor.Elipse(q, alta, e.AlaRx, e.AlaRy, angAlta)) - 1.8f, Paleta.Hex("#E7A6C2"));
            }
            return p;
        });

        public static Sprite CuerpoPolilla(TipoPolilla tipo) => Obtener("Polillas/cuerpo_" + tipo, () =>
        {
            var p = new Pintor(128, 128);
            var e = Estilo(tipo);
            float r = e.Radio;
            var centro = new Vector2(64, 58);
            var patas = Paleta.Hex("#4A2A20");
            // patitas
            for (int k = 0; k < 3; k++)
            {
                for (int lado = -1; lado <= 1; lado += 2)
                {
                    var a = centro + new Vector2(lado * (r * 0.25f + k * r * 0.22f), -r * 0.55f);
                    var b = a + new Vector2(lado * (4 + k * 4), -r * 0.42f);
                    p.Forma(q => Pintor.Segmento(q, a, b, 7f), patas, 0.2f, 5);
                }
            }
            // antenas curvas con bolita
            for (int lado = -1; lado <= 1; lado += 2)
            {
                var a = centro + new Vector2(lado * r * 0.3f, r * 0.82f);
                var m = a + new Vector2(lado * e.LargoAntena * 0.15f, e.LargoAntena * 0.6f);
                var b = m + new Vector2(lado * e.LargoAntena * 0.45f, e.LargoAntena * 0.35f);
                p.Forma(q => Mathf.Min(Pintor.Segmento(q, a, m, 5f), Pintor.Segmento(q, m, b, 4.5f)), e.Antena, 0.3f, 3);
                p.Forma(q => Pintor.Circulo(q, b, 5f), Paleta.Aclarar(e.Antena, 0.15f), 0.2f, 4);
            }
            // cuerpo de lana
            p.Sombra(q => Pintor.Circulo(q, centro + new Vector2(3, -4), r), 5, 0.3f);
            p.Forma(q => Pintor.Circulo(q, centro, r), e.Cuerpo, 0.5f, 31 + (int)tipo,
                q => 0.78f + 0.36f * Mathf.Clamp01(1f - ((q - (centro + new Vector2(-r * 0.3f, r * 0.35f))).magnitude / (r * 1.7f))));
            for (int k = -2; k <= 2; k++)
            {
                float off = k * r * 0.3f;
                p.Forma(q => Mathf.Max(Pintor.Anillo(q, centro + new Vector2(off * 0.4f, -r * 1.35f), r * 1.65f + off, 1.4f), Pintor.Circulo(q, centro, r - 3)),
                    Paleta.ConAlfa(e.Sombra, 0.45f));
            }
            // cara: ojos grandes, cejas enfadadas, mejillas y boquita en "w"
            float ojoY = centro.y + r * 0.08f, ojoX = r * 0.34f, ojoR = r * 0.19f;
            for (int lado = -1; lado <= 1; lado += 2)
            {
                var o = new Vector2(centro.x + lado * ojoX, ojoY);
                p.Forma(q => Pintor.Elipse(q, o, ojoR * 0.9f, ojoR * 1.08f), Paleta.Ojos);
                p.Forma(q => Pintor.Circulo(q, o + new Vector2(-ojoR * 0.28f, ojoR * 0.38f), ojoR * 0.32f), Color.white);
                p.Forma(q => Pintor.Circulo(q, o + new Vector2(ojoR * 0.3f, -ojoR * 0.35f), ojoR * 0.13f), Paleta.ConAlfa(Color.white, 0.8f));
                var c1 = o + new Vector2(-lado * ojoR * 0.2f, ojoR * 1.05f);
                var c2 = o + new Vector2(lado * ojoR * 1.25f, ojoR * 1.6f);
                p.Forma(q => Pintor.Segmento(q, c1, c2, tipo == TipoPolilla.Acorazada ? 5f : 4f), Paleta.Ojos);
                if (tipo == TipoPolilla.Reina)
                {
                    p.Forma(q => Pintor.Elipse(q, o + new Vector2(0, ojoR * 0.75f), ojoR * 1.15f, ojoR * 0.5f), Paleta.ConAlfa(Paleta.Hex("#8E4FA3"), 0.55f));
                    var pestana = o + new Vector2(lado * ojoR * 0.95f, ojoR * 0.55f);
                    p.Forma(q => Pintor.Segmento(q, pestana, pestana + new Vector2(lado * 5, 3), 2.2f), Paleta.Ojos);
                }
                p.Forma(q => Pintor.Circulo(q, new Vector2(centro.x + lado * r * 0.6f, centro.y - r * 0.22f), r * 0.14f), Paleta.ConAlfa(e.Mejilla, 0.9f));
            }
            var boca = new Vector2(centro.x, centro.y - r * 0.22f);
            p.Forma(q => Mathf.Min(Pintor.Segmento(q, boca + new Vector2(-6, 2), boca + new Vector2(-3, -1), 2.2f),
                Mathf.Min(Pintor.Segmento(q, boca + new Vector2(-3, -1), boca + new Vector2(0, 2), 2.2f),
                    Mathf.Min(Pintor.Segmento(q, boca + new Vector2(0, 2), boca + new Vector2(3, -1), 2.2f),
                        Pintor.Segmento(q, boca + new Vector2(3, -1), boca + new Vector2(6, 2), 2.2f)))), Paleta.Ojos);
            return p;
        });

        /// <summary>Faldones y peto de fieltro de la acorazada (sprite aparte: se caen al romperse la placa).</summary>
        public static Sprite PlacasAcorazada() => Obtener("Polillas/placas", () =>
        {
            var p = new Pintor(128, 128);
            var col = Paleta.Hex("#5E5A78");
            var centro = new Vector2(64, 58);
            for (int lado = -1; lado <= 1; lado += 2)
            {
                var c = centro + new Vector2(lado * 40, -6);
                float ang = lado * 18;
                p.Sombra(q => Pintor.Elipse(q, c + new Vector2(2, -3), 20, 34, ang), 4, 0.3f);
                p.Forma(q => Pintor.Elipse(q, c, 20, 34, ang), col, 0.45f, 41 + lado, q => 0.85f + 0.2f * Mathf.Clamp01((q.y - 20) / 80f));
                p.Puntadas(q => Mathf.Abs(Pintor.Elipse(q, c, 14, 28, ang)) - 1.2f, q => q.y * 1.3f, 5, 4, Paleta.Hex("#E9C7A0"));
            }
            p.Forma(q => Mathf.Max(Pintor.Caja(q, centro + new Vector2(0, -20), 26, 10, 8), -Pintor.Circulo(q, centro + new Vector2(0, 2), 12)), col, 0.4f, 43);
            p.Puntadas(q => Pintor.Segmento(q, centro + new Vector2(-20, -22), centro + new Vector2(20, -22), 2.2f), q => q.x, 5, 4, Paleta.Hex("#E9C7A0"));
            return p;
        });

        /// <summary>Capa magenta y cuello de piel de la Reina.</summary>
        public static Sprite CapaReina() => Obtener("Polillas/capa_reina", () =>
        {
            var p = new Pintor(128, 128);
            var capa = Paleta.Hex("#B8395E");
            var centro = new Vector2(64, 50);
            p.Sombra(q => Pintor.Caja(q, centro + new Vector2(3, -10), 40, 30, 16), 5, 0.3f);
            p.Forma(q => Mathf.Max(Pintor.Caja(q, centro + new Vector2(0, -12), 42, 30, 18), q.y - (centro.y + 6)), capa, 0.4f, 52,
                q => 0.82f + 0.25f * Mathf.Clamp01(1f - Mathf.Abs(q.x - 64f) / 60f));
            p.Puntadas(q => Mathf.Max(Mathf.Abs(Pintor.Caja(q, centro + new Vector2(0, -12), 35, 23, 14)) - 1.3f, q.y - (centro.y + 2)),
                q => q.x + q.y, 5, 4, Paleta.Hex("#F2B8C8"));
            for (int k = -3; k <= 3; k++)
            {
                var c = centro + new Vector2(k * 11, 10 - Mathf.Abs(k) * 2.5f);
                p.Forma(q => Pintor.Circulo(q, c, 10), Paleta.Hex("#F4E2CB"), 0.5f, 53 + k);
            }
            for (int lado = -1; lado <= 1; lado += 2)
            {
                var b = centro + new Vector2(lado * 18, -8);
                p.Forma(q => Pintor.Circulo(q, b, 7), Paleta.Mostaza, 0.2f, 57);
                p.Forma(q => Pintor.Circulo(q, b + new Vector2(-2, 2), 1.6f), Paleta.Oscurecer(Paleta.Mostaza, 0.45f));
                p.Forma(q => Pintor.Circulo(q, b + new Vector2(2, -2), 1.6f), Paleta.Oscurecer(Paleta.Mostaza, 0.45f));
            }
            return p;
        });

        public static Sprite Corona() => Obtener("Polillas/corona", () =>
        {
            var p = new Pintor(128, 64);
            p.Forma(q => Mathf.Min(Pintor.Caja(q, new Vector2(64, 14), 32, 10, 4),
                Mathf.Min(Mathf.Min(Pintor.Segmento(q, new Vector2(36, 18), new Vector2(40, 50), 11), Pintor.Segmento(q, new Vector2(64, 18), new Vector2(64, 56), 11)),
                    Pintor.Segmento(q, new Vector2(92, 18), new Vector2(88, 50), 11))), Paleta.Dorado, 0.25f, 51,
                q => 0.85f + 0.25f * Mathf.Clamp01((q.y - 4f) / 50f));
            p.Forma(q => Pintor.Circulo(q, new Vector2(40, 52), 5), Paleta.Dorado);
            p.Forma(q => Pintor.Circulo(q, new Vector2(64, 58), 5), Paleta.Dorado);
            p.Forma(q => Pintor.Circulo(q, new Vector2(88, 52), 5), Paleta.Dorado);
            p.Forma(q => Pintor.Elipse(q, new Vector2(64, 16), 6, 8), Paleta.Hex("#E5383B"));
            p.Forma(q => Pintor.Circulo(q, new Vector2(62, 19), 2), Paleta.ConAlfa(Color.white, 0.8f));
            return p;
        }, PixelesPorUnidad, new Vector2(0.5f, 0f));

        /// <summary>Retrato recortado de la hoja de personajes (Resources/Arte/Retratos), para las fichas de la interfaz.</summary>
        public static Texture2D Retrato(string nombre)
        {
            string clave = "Retratos/" + nombre;
            if (CacheTex.TryGetValue(clave, out var t)) return t;
            t = Resources.Load<Texture2D>("Arte/" + clave);
            CacheTex[clave] = t;
            return t;
        }

        public static Texture2D Retrato(TipoPolilla tipo)
        {
            switch (tipo)
            {
                case TipoPolilla.Veloz: return Retrato("veloz");
                case TipoPolilla.Acorazada: return Retrato("acorazada");
                case TipoPolilla.Reina: return Retrato("reina");
                default: return Retrato("comun");
            }
        }

        // ================================================================== Tablero

        public static Sprite Casilla(int variante) => Obtener("Tablero/casilla_" + variante, () =>
        {
            var p = new Pintor(128, 128);
            var c = variante % 2 == 0 ? Paleta.Musgo : Paleta.Hex("#64813A");
            p.Forma(q => Pintor.Caja(q, new Vector2(64, 64), 61, 61, 14), c, 0.38f, 100 + variante,
                q => 0.92f + 0.12f * Mathf.Clamp01((q.y - 10f) / 118f));
            p.Puntadas(q => Mathf.Abs(Pintor.Caja(q, new Vector2(64, 64), 53, 53, 10)) - 1.1f,
                q => q.x + q.y, 7, 6, Paleta.ConAlfa(Paleta.MusgoClaro, 0.6f));
            return p;
        });

        public static Sprite Mesa() => Obtener("Tablero/mesa", () =>
        {
            var p = new Pintor(256, 256);
            p.Forma(q => -1f, Paleta.Madera, 0f, 0,
                q => 0.86f + 0.22f * Pintor.Ruido(q.x * 0.015f, q.y * 0.18f, 9) + 0.06f * Pintor.Ruido(q.x * 0.2f, q.y * 0.9f, 10));
            return p;
        });

        public static Sprite Obstaculo(char simbolo) => Obtener("Tablero/obstaculo_" + (int)simbolo, () =>
        {
            var p = new Pintor(128, 128);
            var c = new Vector2(64, 66);
            switch (simbolo)
            {
                case 'o':
                    p.Sombra(q => Pintor.Circulo(q, c + new Vector2(5, -6), 48), 6, 0.35f);
                    p.Forma(q => Pintor.Circulo(q, c, 48), Paleta.AzulSuave, 0.3f, 61, q => 0.85f + 0.25f * Mathf.Clamp01((q.y - 20) / 100f));
                    p.Forma(q => Pintor.Anillo(q, c, 38, 5), Paleta.Oscurecer(Paleta.AzulSuave, 0.2f));
                    for (int i = 0; i < 4; i++)
                    {
                        var h = c + Pintor.Rotar(new Vector2(12, 0), 45 + 90 * i);
                        p.Forma(q => Pintor.Circulo(q, h, 7), Paleta.Oscurecer(Paleta.AzulSuave, 0.45f));
                    }
                    break;
                case 'u':
                    p.Sombra(q => Pintor.Caja(q, c + new Vector2(5, -8), 34, 44, 18), 6, 0.35f);
                    p.Forma(q => Pintor.Caja(q, c + new Vector2(0, -2), 34, 44, 18), Paleta.Plata, 0.1f, 62,
                        q => 0.75f + 0.4f * Mathf.Clamp01(1f - Mathf.Abs(q.x - 52f) / 40f));
                    for (int y = 0; y < 5; y++)
                        for (int x = 0; x < 4; x++)
                        {
                            var h = c + new Vector2(-18 + x * 12 + (y % 2) * 6, -26 + y * 12);
                            p.Forma(q => Pintor.Circulo(q, h, 2.6f), Paleta.ConAlfa(Paleta.Tinta, 0.35f));
                        }
                    p.Forma(q => Pintor.Caja(q, c + new Vector2(0, -40), 38, 6, 4), Paleta.Oscurecer(Paleta.Plata, 0.15f));
                    break;
                case 'c':
                    DibujarCarrete(p, c, Paleta.AzulSuave, 1f, 0.8f);
                    break;
                case 'a':
                    p.Sombra(q => Pintor.Elipse(q, c + new Vector2(5, -8), 50, 40), 6, 0.35f);
                    p.Forma(q => Pintor.Elipse(q, c, 50, 40), Paleta.Coral, 0.35f, 63, q => 0.85f + 0.25f * Mathf.Clamp01((q.y - 30) / 80f));
                    for (int i = -2; i <= 2; i++)
                        p.Forma(q => Mathf.Max(Pintor.Anillo(q, c + new Vector2(i * 20, 0), 44, 2), Pintor.Elipse(q, c, 48, 38)), Paleta.Oscurecer(Paleta.Coral, 0.25f));
                    for (int i = 0; i < 3; i++)
                    {
                        var b = c + new Vector2(-20 + i * 20, 30);
                        p.Forma(q => Pintor.Segmento(q, b, b + new Vector2(6, 26), 3), Paleta.Plata);
                        p.Forma(q => Pintor.Circulo(q, b + new Vector2(6, 28), 7), i == 1 ? Paleta.Mostaza : Paleta.AzulSuave);
                    }
                    break;
                case 'm':
                    p.Sombra(q => Pintor.Circulo(q, c + new Vector2(5, -6), 46), 6, 0.35f);
                    p.Forma(q => Pintor.Circulo(q, c, 46), Paleta.Mostaza, 0.25f, 64);
                    p.Forma(q => Pintor.Circulo(q, c, 16), Paleta.Oscurecer(Paleta.Mostaza, 0.3f));
                    for (int i = 0; i < 24; i++)
                    {
                        var a = c + Pintor.Rotar(new Vector2(36, 0), i * 15);
                        var b = c + Pintor.Rotar(new Vector2(i % 3 == 0 ? 26 : 31, 0), i * 15);
                        p.Forma(q => Pintor.Segmento(q, a, b, 2), Paleta.Tinta);
                    }
                    break;
                case 't':
                    p.Sombra(q => Pintor.Caja(q, c + new Vector2(5, -6), 14, 48, 6, 30), 6, 0.35f);
                    p.Forma(q => Pintor.Caja(q, c + new Vector2(10, 14), 9, 40, 6, 30), Paleta.Plata, 0.1f, 65);
                    p.Forma(q => Pintor.Anillo(q, c + new Vector2(-18, -30), 14, 7), Paleta.Coral);
                    p.Forma(q => Pintor.Anillo(q, c + new Vector2(2, -38), 14, 7), Paleta.Coral);
                    break;
                default:
                    p.Sombra(q => Pintor.Caja(q, c + new Vector2(5, -7), 52, 46, 12), 6, 0.35f);
                    p.Forma(q => Pintor.Caja(q, c, 52, 46, 12), Paleta.Madera, 0.25f, 66,
                        q => 0.85f + 0.2f * Pintor.Ruido(q.x * 0.03f, q.y * 0.3f, 66));
                    p.Forma(q => Pintor.Caja(q, c + new Vector2(0, 18), 52, 4, 2), Paleta.MaderaOscura);
                    p.Forma(q => Pintor.Caja(q, c + new Vector2(0, 8), 9, 9, 3), Paleta.Dorado);
                    p.Puntadas(q => Mathf.Abs(Pintor.Caja(q, c, 44, 38, 8)) - 1.2f, q => q.x + q.y, 6, 5, Paleta.ConAlfa(Paleta.Crema, 0.7f));
                    break;
            }
            return p;
        });

        private static void DibujarCarrete(Pintor p, Vector2 c, Color hilo, float escala, float cantidad)
        {
            float w = 34 * escala, h = 42 * escala;
            p.Sombra(q => Pintor.Caja(q, c + new Vector2(5, -7), w + 8, h + 4, 8), 6, 0.35f);
            p.Forma(q => Pintor.Caja(q, c, w * 0.55f, h, 4), Paleta.Beige, 0.2f, 70);
            float anchoHilo = w * (0.55f + 0.45f * Mathf.Clamp01(cantidad));
            if (cantidad > 0.01f)
            {
                p.Forma(q => Pintor.Caja(q, c, anchoHilo, h - 8 * escala, 8), hilo, 0.25f, 71,
                    q => 0.8f + 0.3f * Mathf.Clamp01(1f - Mathf.Abs(q.x - (c.x - anchoHilo * 0.3f)) / (anchoHilo * 1.4f)));
                for (float y = -h + 12 * escala; y < h - 10 * escala; y += 7 * escala)
                {
                    float yy = y;
                    p.Forma(q => Mathf.Max(Pintor.Segmento(q, c + new Vector2(-anchoHilo, yy), c + new Vector2(anchoHilo, yy + 3 * escala), 1.6f),
                        Pintor.Caja(q, c, anchoHilo, h - 8 * escala, 8)), Paleta.ConAlfa(Paleta.Oscurecer(hilo, 0.25f), 0.7f));
                }
            }
            p.Forma(q => Pintor.Caja(q, c + new Vector2(0, h), w + 8, 7 * escala, 5), Paleta.Madera, 0.2f, 72);
            p.Forma(q => Pintor.Caja(q, c + new Vector2(0, -h), w + 8, 7 * escala, 5), Paleta.Madera, 0.2f, 73);
        }

        /// <summary>El carrete de la salida: el hilo que le queda refleja la vida (diegético).</summary>
        public static Sprite Carrete(int vida, int vidaMaxima) => Obtener("Tablero/carrete_" + vida + "_" + vidaMaxima, () =>
        {
            var p = new Pintor(160, 160);
            DibujarCarrete(p, new Vector2(80, 78), Paleta.Bufanda, 1.5f, vidaMaxima <= 0 ? 0 : (float)vida / vidaMaxima);
            return p;
        }, 128);

        public static Sprite Entrada() => Obtener("Tablero/entrada", () =>
        {
            var p = new Pintor(128, 128);
            var c = new Vector2(64, 50);
            p.Forma(q => Pintor.Caja(q, new Vector2(64, 30), 62, 30, 6), Paleta.MaderaOscura, 0.2f, 80);
            p.Forma(q => Mathf.Min(Pintor.Caja(q, c + new Vector2(0, -10), 34, 26, 2), Pintor.Circulo(q, c + new Vector2(0, 14), 34)),
                Paleta.Hex("#1E120C"), 0.15f, 81);
            p.Forma(q => Mathf.Max(Pintor.Anillo(q, c + new Vector2(0, 14), 36, 5), -(q.y - c.y - 14)), Paleta.Madera);
            return p;
        });

        public static Sprite Lampara() => Obtener("Tablero/lampara", () =>
        {
            var p = new Pintor(128, 128);
            var c = new Vector2(64, 62);
            p.Sombra(q => Pintor.Elipse(q, c + new Vector2(4, -40), 36, 10), 5, 0.4f);
            p.Forma(q => Pintor.Elipse(q, c + new Vector2(0, -38), 30, 9), Paleta.MaderaOscura, 0.2f, 90);
            p.Forma(q => Pintor.Segmento(q, c + new Vector2(0, -36), c + new Vector2(0, 8), 7), Paleta.Dorado);
            p.Forma(q => Pintor.Circulo(q, c + new Vector2(0, 14), 16), Paleta.Hex("#FFF4C2"));
            p.Forma(q => Mathf.Max(Pintor.Caja(q, c + new Vector2(0, 26), 40, 20, 6), -(q.y - c.y - 4)), Paleta.Mostaza, 0.3f, 91,
                q => 0.8f + 0.3f * Mathf.Clamp01((q.y - c.y) / 50f));
            p.Puntadas(q => Pintor.Segmento(q, c + new Vector2(-38, 10), c + new Vector2(38, 10), 2.2f), q => q.x, 5, 4, Paleta.Crema);
            return p;
        });

        public static Sprite Brillo() => Obtener("Efectos/brillo", () =>
        {
            var p = new Pintor(128, 128);
            p.Forma(q => -1f, Color.white, 0, 0, q => 1f, null);
            for (int y = 0; y < 128; y++)
                for (int x = 0; x < 128; x++)
                {
                    float d = (new Vector2(x + 0.5f, y + 0.5f) - new Vector2(64, 64)).magnitude / 64f;
                    float a = Mathf.Clamp01(1f - d);
                    p.Escribir(x, y, new Color(1, 1, 1, a * a));
                }
            return p;
        });

        public static Sprite Circulo() => Obtener("Efectos/circulo", () =>
        {
            var p = new Pintor(64, 64);
            p.Forma(q => Pintor.Circulo(q, new Vector2(32, 32), 30), Color.white);
            return p;
        }, 64);

        public static Sprite Huella() => Obtener("Efectos/huella", () =>
        {
            var p = new Pintor(64, 64);
            p.Forma(q => Pintor.Elipse(q, new Vector2(24, 34), 7, 11, 12), Paleta.ConAlfa(Paleta.RosaPastel, 0.95f));
            p.Forma(q => Pintor.Elipse(q, new Vector2(40, 28), 7, 11, -12), Paleta.ConAlfa(Paleta.RosaPastel, 0.95f));
            p.Forma(q => Pintor.Anillo(q, new Vector2(32, 31), 26, 2.4f), Paleta.ConAlfa(Paleta.RosaPastel, 0.9f));
            return p;
        }, 64);

        // ================================================================== Lana

        public const int Derecha = 1, Arriba = 2, Izquierda = 4, Abajo = 8;

        /// <summary>
        /// Pieza de lana gruesa para una casilla según sus conexiones (bits Derecha/Arriba/Izquierda/Abajo).
        /// Se pinta en gris claro con volumen (sombreado cilíndrico), torsión de las hebras y pelusa;
        /// el color final lo pone el SpriteRenderer, así el jugador puede elegir el color de la lana.
        /// </summary>
        public static Sprite PiezaLana(int mascara) => Obtener("Lana/pieza_" + mascara, () =>
        {
            const int n = 128;
            var p = new Pintor(n, n);
            float medio = n / 2f, semi = n * 0.17f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    var q = new Vector2(x + 0.5f, y + 0.5f);
                    if (!Parametros(mascara, q, n, medio, out float t, out float s)) continue;
                    float borde = semi + (Pintor.Ruido(x * 0.35f, y * 0.35f, 5) - 0.5f) * 3.5f;
                    float a = Mathf.Clamp01((borde - Mathf.Abs(t)) / 1.6f);
                    if (a <= 0) continue;
                    float k = Mathf.Clamp01(Mathf.Abs(t) / semi);
                    float cilindro = 0.55f + 0.45f * Mathf.Sqrt(1f - k * k);
                    float luz = Mathf.Clamp01(1f - Mathf.Abs(t / semi + 0.35f)) * 0.12f;
                    float hebra = 0.5f + 0.5f * Mathf.Sin((s / 21f + t / 26f) * Mathf.PI * 2f);
                    float surco = 0.8f + 0.2f * Mathf.SmoothStep(0f, 1f, hebra);
                    float fibra = 0.93f + 0.14f * Pintor.Ruido(s * 0.9f, t * 0.9f, 6);
                    float b = Mathf.Clamp01(cilindro * surco * fibra + luz);
                    p.Escribir(x, y, new Color(b, b, b, a));
                }
            }
            return p;
        });

        private static bool Parametros(int m, Vector2 q, int n, float medio, out float t, out float s)
        {
            t = 0;
            s = 0;
            bool d = (m & Derecha) != 0, a = (m & Arriba) != 0, i = (m & Izquierda) != 0, b = (m & Abajo) != 0;
            int cuenta = (d ? 1 : 0) + (a ? 1 : 0) + (i ? 1 : 0) + (b ? 1 : 0);
            if (cuenta == 2 && d && i) { t = q.y - medio; s = q.x; return true; }
            if (cuenta == 2 && a && b) { t = q.x - medio; s = q.y; return true; }
            if (cuenta == 2)
            {
                var centro = new Vector2(d ? n : 0, a ? n : 0);
                var v = q - centro;
                t = v.magnitude - medio;
                s = Mathf.Atan2(Mathf.Abs(v.y), Mathf.Abs(v.x)) * medio;
                return true;
            }
            if (cuenta == 1)
            {
                Vector2 borde = d ? new Vector2(n, medio) : a ? new Vector2(medio, n) : i ? new Vector2(0, medio) : new Vector2(medio, 0);
                var centro = new Vector2(medio, medio);
                var eje = (borde - centro).normalized;
                var v = q - centro;
                float along = Vector2.Dot(v, eje);
                float perp = Vector2.Dot(v, new Vector2(-eje.y, eje.x));
                if (along >= 0) { t = perp; s = along; }
                else { t = v.magnitude * (perp >= 0 ? 1 : -1); s = along; }
                return true;
            }
            return false;
        }

        // ================================================================== Herramientas

        public static Sprite Herramienta(TipoHerramienta tipo, bool doblada = false) =>
            Obtener("Herramientas/" + tipo + (doblada ? "_doblada" : ""), () => DibujarHerramienta(tipo, doblada));

        private static Pintor DibujarHerramienta(TipoHerramienta tipo, bool doblada)
        {
            var p = new Pintor(128, 128);
            var c = new Vector2(64, 64);
            switch (tipo)
            {
                case TipoHerramienta.Alfiler:
                {
                    var a = new Vector2(38, 26);
                    var b = doblada ? new Vector2(70, 70) : new Vector2(88, 96);
                    var b2 = doblada ? new Vector2(98, 78) : b;
                    p.Sombra(q => Pintor.Segmento(q, a + new Vector2(5, -5), b + new Vector2(5, -5), 7), 4, 0.35f);
                    p.Forma(q => Pintor.Segmento(q, a, b, 6), Paleta.Plata, 0, 0, q => 0.8f + 0.3f * Mathf.Clamp01((q.x - q.y + 60) / 60f));
                    if (doblada) p.Forma(q => Pintor.Segmento(q, b, b2, 6), Paleta.Plata);
                    var cabeza = doblada ? b2 : b;
                    p.Sombra(q => Pintor.Circulo(q, cabeza + new Vector2(4, -5), 15), 5, 0.35f);
                    p.Forma(q => Pintor.Circulo(q, cabeza, 15), doblada ? Paleta.GrisFieltro : Paleta.Coral, 0.15f, 101,
                        q => 0.75f + 0.45f * Mathf.Clamp01(1f - (q - (cabeza + new Vector2(-5, 6))).magnitude / 22f));
                    p.Forma(q => Pintor.Circulo(q, cabeza + new Vector2(-5, 6), 4), Paleta.ConAlfa(Color.white, 0.8f));
                    break;
                }
                case TipoHerramienta.Velcro:
                {
                    var col = Paleta.Hex("#6C8FB5");
                    p.Forma(q => Pintor.Caja(q, c, 54, 38, 10), col, 0.3f, 102);
                    for (int y = 0; y < 6; y++)
                        for (int x = 0; x < 9; x++)
                        {
                            var h = c + new Vector2(-44 + x * 11 + (y % 2) * 5, -27 + y * 11);
                            p.Forma(q => Pintor.Anillo(q, h, 3.2f, 1.6f), Paleta.Oscurecer(col, 0.3f));
                        }
                    p.Puntadas(q => Mathf.Abs(Pintor.Caja(q, c, 47, 31, 7)) - 1.3f, q => q.x + q.y, 6, 5, Paleta.Crema);
                    break;
                }
                case TipoHerramienta.Bobina:
                {
                    p.Sombra(q => Pintor.Caja(q, c + new Vector2(5, -7), 40, 46, 10), 6, 0.35f);
                    p.Forma(q => Pintor.Caja(q, c, 22, 44, 4), Paleta.Beige, 0.2f, 103);
                    p.Forma(q => Pintor.Caja(q, c, 34, 34, 10), Paleta.Mostaza, 0.3f, 104,
                        q => 0.8f + 0.3f * Mathf.Clamp01(1f - Mathf.Abs(q.x - 54f) / 50f));
                    for (int k = -4; k <= 4; k++)
                    {
                        float yy = k * 7f;
                        p.Forma(q => Mathf.Max(Pintor.Segmento(q, c + new Vector2(-34, yy), c + new Vector2(34, yy + 3), 1.8f), Pintor.Caja(q, c, 34, 34, 10)),
                            Paleta.ConAlfa(Paleta.Oscurecer(Paleta.Mostaza, 0.3f), 0.7f));
                    }
                    p.Forma(q => Pintor.Caja(q, c + new Vector2(0, 40), 42, 7, 5), Paleta.Madera, 0.2f, 105);
                    p.Forma(q => Pintor.Caja(q, c + new Vector2(0, -40), 42, 7, 5), Paleta.Madera, 0.2f, 106);
                    p.Forma(q => Pintor.Segmento(q, c + new Vector2(30, 10), c + new Vector2(56, -30), 3), Paleta.Mostaza);
                    break;
                }
                case TipoHerramienta.Tijeras:
                {
                    p.Sombra(q => Pintor.Caja(q, c + new Vector2(5, 0), 14, 50, 6, 20), 6, 0.3f);
                    p.Forma(q => Pintor.Caja(q, c + new Vector2(-8, 22), 8, 40, 6, -16), Paleta.Plata, 0.1f, 107, q => 0.85f + 0.25f * Mathf.Clamp01((q.x - 40) / 40f));
                    p.Forma(q => Pintor.Caja(q, c + new Vector2(8, 22), 8, 40, 6, 16), Paleta.Hex("#B5BAC2"), 0.1f, 108);
                    p.Forma(q => Pintor.Anillo(q, c + new Vector2(-20, -30), 15, 8), Paleta.Coral, 0.2f, 109);
                    p.Forma(q => Pintor.Anillo(q, c + new Vector2(20, -30), 15, 8), Paleta.Coral, 0.2f, 110);
                    p.Forma(q => Pintor.Circulo(q, c + new Vector2(0, -4), 5), Paleta.Tinta);
                    break;
                }
                case TipoHerramienta.Boton:
                {
                    for (int k = 0; k < 4; k++)
                    {
                        float y0 = 20 + k * 7;
                        p.Forma(q => Pintor.Segmento(q, new Vector2(46, y0), new Vector2(82, y0 + 4), 3), Paleta.Plata);
                    }
                    var cc = c + new Vector2(0, 14);
                    p.Sombra(q => Pintor.Circulo(q, cc + new Vector2(5, -6), 42), 6, 0.35f);
                    p.Forma(q => Pintor.Circulo(q, cc, 42), Paleta.Mostaza, 0.25f, 111, q => 0.82f + 0.3f * Mathf.Clamp01((q.y - 40) / 80f));
                    p.Forma(q => Pintor.Anillo(q, cc, 32, 5), Paleta.Oscurecer(Paleta.Mostaza, 0.2f));
                    for (int i = 0; i < 4; i++)
                    {
                        var h = cc + Pintor.Rotar(new Vector2(11, 0), 45 + 90 * i);
                        p.Forma(q => Pintor.Circulo(q, h, 6), Paleta.Oscurecer(Paleta.Mostaza, 0.5f));
                    }
                    break;
                }
            }
            return p;
        }

        // ================================================================== Interfaz

        public static Texture2D Panel(Color fondo, Color puntada, string clave, int tam = 96, float radio = 22f)
        {
            if (CacheTex.TryGetValue(clave, out var t) && t != null) return t;
            var p = new Pintor(tam, tam);
            float m = tam / 2f;
            p.Sombra(q => Pintor.Caja(q, new Vector2(m + 2, m - 3), m - 4, m - 4, radio), 4, 0.35f);
            p.Forma(q => Pintor.Caja(q, new Vector2(m, m), m - 4, m - 4, radio), fondo, 0.3f, clave.GetHashCode() & 0xFF,
                q => 0.9f + 0.12f * Mathf.Clamp01(q.y / tam));
            p.Puntadas(q => Mathf.Abs(Pintor.Caja(q, new Vector2(m, m), m - 12, m - 12, radio - 8)) - 1.2f, q => q.x + q.y, 6, 5, puntada);
            t = p.ATextura();
            t.name = clave;
            CacheTex[clave] = t;
            return t;
        }

        public static Texture2D Plano(Color color, string clave)
        {
            if (CacheTex.TryGetValue(clave, out var t) && t != null) return t;
            t = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            t.SetPixel(0, 0, color);
            t.Apply();
            CacheTex[clave] = t;
            return t;
        }
    }
}
