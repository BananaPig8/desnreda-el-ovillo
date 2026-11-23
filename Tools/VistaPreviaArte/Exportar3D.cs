using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using DesenredaElOvillo.Juego;
using DesenredaElOvillo.Juego.Tres;
using DesenredaElOvillo.Nucleo;
using UnityEngine;

/// <summary>
/// Exporta una escena 3D (la misma geometría y los mismos modelos que usa Unity) a JSON para el visor three.js.
/// Convierte de la mano izquierda de Unity a la mano derecha de three.js invirtiendo Z (y el orden de los triángulos).
/// </summary>
public sealed class Escena3D
{
    private readonly Dictionary<string, DatosMalla> _mallas = new Dictionary<string, DatosMalla>();
    private readonly StringBuilder _instancias = new StringBuilder();
    private int _cuenta;
    private readonly List<string> _luces = new List<string>();
    public string Camara = "";

    private static float[] Identidad() => new float[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 };

    /// <summary>Matriz 4x4 fila a fila de T * R * S.</summary>
    private static float[] Trs(Vector3 t, Quaternion q, Vector3 s)
    {
        float xx = q.x * q.x, yy = q.y * q.y, zz = q.z * q.z, xy = q.x * q.y, xz = q.x * q.z, yz = q.y * q.z, wx = q.w * q.x, wy = q.w * q.y, wz = q.w * q.z;
        float r00 = 1 - 2 * (yy + zz), r01 = 2 * (xy - wz), r02 = 2 * (xz + wy);
        float r10 = 2 * (xy + wz), r11 = 1 - 2 * (xx + zz), r12 = 2 * (yz - wx);
        float r20 = 2 * (xz - wy), r21 = 2 * (yz + wx), r22 = 1 - 2 * (xx + yy);
        return new[]
        {
            r00 * s.x, r01 * s.y, r02 * s.z, t.x,
            r10 * s.x, r11 * s.y, r12 * s.z, t.y,
            r20 * s.x, r21 * s.y, r22 * s.z, t.z,
            0, 0, 0, 1
        };
    }

    private static float[] Mul(float[] a, float[] b)
    {
        var r = new float[16];
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
            {
                float s = 0;
                for (int k = 0; k < 4; k++) s += a[i * 4 + k] * b[k * 4 + j];
                r[i * 4 + j] = s;
            }
        return r;
    }

    /// <param name="sueloSombra">Si tiene valor, la sombra de contacto de la raíz se pega a esa altura (polillas sobre la lana o en vuelo).</param>
    public void Agregar(Pieza p, Vector3 posicion, float giroY = 0f, float escala = 1f, float? sueloSombra = null)
    {
        var baseM = Trs(posicion, Quaternion.Euler(0, giroY, 0), Vector3.one * escala);
        bool sombraFuera = sueloSombra.HasValue && p.Sombra.x > 0f;
        if (sombraFuera)
            SombraEn(Trs(new Vector3(posicion.x, sueloSombra.Value + 0.004f, posicion.z), Quaternion.Euler(0, giroY, 0), new Vector3(p.Sombra.x * escala, 1f, p.Sombra.y * escala)));
        Recorrer(p, baseM, sombraFuera);
    }

    private void Recorrer(Pieza p, float[] padre, bool sinSombra = false)
    {
        if (p.Oculta) return;
        var m = Mul(padre, Trs(p.Posicion, Quaternion.Euler(p.Rotacion), p.Escala));
        if (!string.IsNullOrEmpty(p.Malla))
        {
            Registrar(p.Malla);
            Instancia(p.Malla, m, p.Material, p.Color, p.ProyectaSombra);
            // capas de pelusa, igual que Constructor.CrearPelusa
            for (int k = 1; k <= p.Pelusa; k++)
            {
                string clave = CapasPelusa.Malla(p.Malla, k, p.CaraLibre);
                Registrar(clave);
                Instancia(clave, m, "pelusa", CapasPelusa.Tono(p.Color, k), false, CapasPelusa.Corte(k));
            }
        }
        if (!sinSombra && p.Sombra.x > 0f) SombraEn(Mul(m, Trs(new Vector3(0, 0.004f, 0), Quaternion.identity, new Vector3(p.Sombra.x, 1f, p.Sombra.y))));
        foreach (var h in p.Hijos) Recorrer(h, m);
    }

    private void Registrar(string clave)
    {
        if (!_mallas.ContainsKey(clave)) _mallas[clave] = CatalogoMallas.Obtener(clave);
    }

    private void SombraEn(float[] m)
    {
        string clave = CatalogoMallas.Plano(1f, 1f);
        Registrar(clave);
        Instancia(clave, m, "sombra", new Color(0, 0, 0, 1), false, -1f, Medidas.OpacidadSombra);
    }

    public void AgregarMalla(string clave, DatosMalla d, string material, Color color)
    {
        _mallas[clave] = d;
        Instancia(clave, Identidad(), material, color, true);
    }

    private void Instancia(string malla, float[] m, string material, Color c, bool sombra, float corte = -1f, float opacidad = 1f)
    {
        // M_three = S * M * S con S = diag(1, 1, -1, 1); three.js lee las matrices por columnas.
        var s = (float[])m.Clone();
        s[2] = -s[2]; s[6] = -s[6]; s[8] = -s[8]; s[9] = -s[9]; s[11] = -s[11]; s[14] = -s[14];
        var col = new StringBuilder();
        for (int j = 0; j < 4; j++)
            for (int i = 0; i < 4; i++)
                col.Append(F(s[i * 4 + j])).Append(i == 3 && j == 3 ? "" : ",");
        if (_cuenta++ > 0) _instancias.Append(",\n");
        _instancias.Append("{\"m\":\"").Append(malla).Append("\",\"mat\":\"").Append(material).Append("\",\"c\":[")
            .Append(F(c.r)).Append(',').Append(F(c.g)).Append(',').Append(F(c.b)).Append("],\"s\":").Append(sombra ? "1" : "0");
        if (corte >= 0f) _instancias.Append(",\"a\":").Append(F(corte));
        if (opacidad < 1f) _instancias.Append(",\"o\":").Append(F(opacidad));
        _instancias.Append(",\"M\":[").Append(col).Append("]}");
    }

    public void Luz(string json) => _luces.Add(json);

    public static string F(float v) => float.IsNaN(v) ? "0" : Math.Round(v, 5).ToString(CultureInfo.InvariantCulture);
    public static string V(Vector3 v) => "[" + F(v.x) + "," + F(v.y) + "," + F(-v.z) + "]";

    public void Guardar(string ruta)
    {
        var sb = new StringBuilder();
        sb.Append("{\"mallas\":{");
        bool primera = true;
        foreach (var kv in _mallas)
        {
            if (!primera) sb.Append(',');
            primera = false;
            var d = kv.Value;
            sb.Append('"').Append(kv.Key).Append("\":{\"v\":[");
            for (int i = 0; i < d.Vertices.Count; i++) { var v = d.Vertices[i]; sb.Append(i > 0 ? "," : "").Append(F(v.x)).Append(',').Append(F(v.y)).Append(',').Append(F(-v.z)); }
            sb.Append("],\"n\":[");
            for (int i = 0; i < d.Normales.Count; i++) { var v = d.Normales[i]; sb.Append(i > 0 ? "," : "").Append(F(v.x)).Append(',').Append(F(v.y)).Append(',').Append(F(-v.z)); }
            sb.Append("],\"uv\":[");
            for (int i = 0; i < d.Uvs.Count; i++) { var v = d.Uvs[i]; sb.Append(i > 0 ? "," : "").Append(F(v.x)).Append(',').Append(F(v.y)); }
            sb.Append("],\"i\":[");
            for (int i = 0; i < d.Triangulos.Count; i += 3)
                sb.Append(i > 0 ? "," : "").Append(d.Triangulos[i]).Append(',').Append(d.Triangulos[i + 2]).Append(',').Append(d.Triangulos[i + 1]);
            sb.Append("]}");
        }
        sb.Append("},\n\"instancias\":[\n").Append(_instancias).Append("],\n\"luces\":[").Append(string.Join(",", _luces)).Append("],\n\"camara\":").Append(Camara).Append('}');
        File.WriteAllText(ruta, sb.ToString());
    }
}

public static class Exportar3D
{
    private static Vector3 Mundo(Celda c) => new Vector3(c.X + 0.5f, Medidas.AlturaCasilla, c.Y + 0.5f);

    private static Vector3 Fuera(Tablero t, Celda c)
    {
        if (c.X == t.Ancho - 1) return Vector3.right;
        if (c.X == 0) return Vector3.left;
        if (c.Y == t.Alto - 1) return Vector3.forward;
        return Vector3.back;
    }

    /// <summary>Réplica de VistaTablero.Construir + lana + herramientas + algunas polillas en pleno recorrido.</summary>
    public static Escena3D Sala(Sala sala, IList<(TipoPolilla tipo, int indice, bool vuela)> polillas, float tejido = 1f)
    {
        var e = new Escena3D();
        var t = sala.Tablero;
        int w = t.Ancho, h = t.Alto;
        e.Agregar(new Pieza { Malla = CatalogoMallas.Caja(w + 16f, 0.5f, h + 14f, 0.08f), Material = "mesa", Color = Color.white }, new Vector3(w / 2f, -0.33f, h / 2f + 2f));
        e.Agregar(new Pieza { Malla = CatalogoMallas.Caja(w + 0.55f, 0.08f, h + 0.55f, 0.04f), Material = "fieltro", Color = Paleta.MusgoOscuro }, new Vector3(w / 2f, -0.04f, h / 2f));
        var casilla = CatalogoMallas.Caja(0.92f, Medidas.AlturaCasilla, 0.92f, 0.05f);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var c = new Celda(x, y);
                float variacion = Pintor.Ruido(x * 1.7f, y * 1.3f, 12);
                var color = Color.Lerp(Paleta.Musgo, Paleta.MusgoClaro, variacion * 0.45f + ((x + y) % 2) * 0.08f);
                e.Agregar(new Pieza { Malla = casilla, Material = "casilla", Color = color, Pelusa = 1 }, new Vector3(x + 0.5f, Medidas.AlturaCasilla / 2f, y + 0.5f), (variacion - 0.5f) * 3f);
                float giro = (Pintor.Ruido(x * 3.1f, y * 2.7f, 5) - 0.5f) * 40f;
                switch (t.Tipo(c))
                {
                    case TipoCasilla.Obstaculo: e.Agregar(Modelos.Obstaculo(t.Simbolo(c)), Mundo(c), t.Simbolo(c) == '#' ? giro * 0.15f : giro); break;
                    case TipoCasilla.Lampara:
                        e.Agregar(Modelos.Lampara(), Mundo(c));
                        var pl = Mundo(c) + Vector3.up * 0.5f;
                        e.Luz("{\"tipo\":\"punto\",\"p\":" + Escena3D.V(pl) + ",\"c\":\"#FFD99A\",\"i\":1.5,\"r\":2.6}");
                        break;
                    case TipoCasilla.Entrada: e.Agregar(Modelos.Entrada(Fuera(t, c)), Mundo(c)); break;
                }
            }
        e.Agregar(Modelos.Carrete(1f), Mundo(t.Salida));
        e.Agregar(Modelos.Ovillo(), Mundo(t.Salida) + Fuera(t, t.Salida) * 1.05f + Vector3.back * 0.25f - Vector3.up * (Medidas.AlturaCasilla + 0.08f), -25f, 1.3f);
        foreach (var d in Decorado.Disponer(w, h, 7)) e.Agregar(d.Pieza, d.Posicion + Vector3.up * -0.08f, d.Giro);

        if (sala.Solucion != null)
        {
            var r = Recorrido.DesdeMovimientos(t, sala.Solucion.Movimientos);
            var centros = new List<Vector3>();
            foreach (var c in r.Celdas) centros.Add(new Vector3(c.X + 0.5f, Medidas.AlturaLana, c.Y + 0.5f));
            if (tejido < 1f)
            {
                // a medio tejer: sin herramientas ni polillas, con el ovillito rodando en la punta
                int n = Math.Max(2, (int)Math.Round(centros.Count * tejido));
                centros.RemoveRange(n, centros.Count - n);
                e.AgregarMalla("lana_recorrido", Mallas.Tubo(Mallas.CaminoSuave(centros), Medidas.RadioLana, 14, 2.5f), "lana", Paleta.ColoresLana[0]);
                float restante = 1f - (float)(n - 1) / sala.Hilo;
                float radio = Medidas.RadioOvillito(restante);
                var dir = (centros[n - 1] - centros[n - 2]).normalized;
                e.Agregar(Modelos.OvilloDeTrabajo(Paleta.ColoresLana[0]), Medidas.PosicionOvillito(centros[n - 1], dir, radio), 35f, radio * 2f);
                LucesYCamara(e, new Vector3(w / 2f + 0.6f, 0.35f, h / 2f - 0.1f), Math.Max(w, h) + 0.4f, 52f, 28f);
                return e;
            }
            e.AgregarMalla("lana_recorrido", Mallas.Tubo(Mallas.CaminoSuave(centros), Medidas.RadioLana, 14, 2.5f), "lana", Paleta.ColoresLana[0]);
            var reglas = Reglas.PorDefecto();
            foreach (var col in sala.Solucion.Colocaciones)
            {
                var c = r.Celdas[col.Indice];
                bool debajo = col.Tipo == TipoHerramienta.Velcro;
                float altura = debajo ? Medidas.AlturaCasilla : Medidas.AlturaLana + Medidas.RadioLana * 0.55f;
                e.Agregar(Modelos.Herramienta(col.Tipo), new Vector3(c.X + 0.5f, altura, c.Y + 0.5f), 0f, debajo ? 1f : 1.2f);
                for (int u = 0; u < reglas.Herramienta(col.Tipo).Usos; u++)
                    e.Agregar(new Pieza { Malla = "esfera_baja", Material = "lana", Color = debajo ? Paleta.AzulSuave : Paleta.Mostaza, Escala = Vector3.one * 0.085f },
                        new Vector3(c.X + 0.5f + (debajo ? 0.38f : -0.38f), Medidas.AlturaCasilla + 0.045f, c.Y + 0.5f - 0.36f + u * 0.11f));
            }
            if (sala.Oleada.Contar(TipoPolilla.Veloz) > 0)
                foreach (int i in SimuladorOleada.PrevisualizarAterrizajes(r, reglas))
                    e.Agregar(Modelos.Huella(), new Vector3(r.Celdas[i].X + 0.82f, Medidas.AlturaCasilla + 0.006f, r.Celdas[i].Y + 0.2f), 20f);
            foreach (var p in polillas)
            {
                var c = r.Celdas[p.indice];
                var sig = r.Celdas[Math.Min(p.indice + 1, r.Cantidad - 1)];
                var dir = new Vector3(sig.X - c.X, 0, sig.Y - c.Y);
                float giro = dir.sqrMagnitude > 0 ? Mathf.Atan2(-dir.x, -dir.z) * Mathf.Rad2Deg * 0.5f : 0f;
                var pos = new Vector3(c.X + 0.5f, Medidas.AlturaPolilla + (p.vuela ? 0.42f : 0f), c.Y + 0.5f);
                e.Agregar(Modelos.Polilla(p.tipo), pos, giro, 1f, Medidas.AlturaCasilla);
            }
        }
        LucesYCamara(e, new Vector3(w / 2f + 0.6f, 0.35f, h / 2f - 0.1f), Math.Max(w, h) + 0.4f, 52f, 28f);
        return e;
    }

    /// <summary>Ovillo y las cuatro polillas en fila sobre fieltro, como la hoja de personajes, pero en 3D.</summary>
    public static Escena3D Personajes()
    {
        var e = new Escena3D();
        e.Agregar(new Pieza { Malla = CatalogoMallas.Caja(30f, 0.5f, 20f, 0.08f), Material = "mesa", Color = Color.white }, new Vector3(0, -0.33f, 3f));
        e.Agregar(new Pieza { Malla = CatalogoMallas.Caja(7.2f, 0.08f, 2.4f, 0.04f), Material = "fieltro", Color = Paleta.Hex("#E9C9A0") }, new Vector3(0, -0.04f, 0));
        e.Agregar(Modelos.Ovillo(), new Vector3(-2.9f, 0f, 0), -12f, 1.3f);
        e.Agregar(Modelos.Polilla(TipoPolilla.Comun), new Vector3(-1.45f, 0f, 0), 8f, 1.3f);
        e.Agregar(Modelos.Polilla(TipoPolilla.Veloz), new Vector3(-0.2f, 0f, 0), -6f, 1.3f);
        e.Agregar(Modelos.Polilla(TipoPolilla.Acorazada), new Vector3(1.15f, 0f, 0), 10f, 1.3f);
        e.Agregar(Modelos.Polilla(TipoPolilla.Reina), new Vector3(2.85f, 0f, 0.1f), -8f, 1.3f);
        e.Agregar(Modelos.BobinaGrande(Paleta.AzulSuave), new Vector3(-3.2f, -0.08f, 2.6f), 30f);
        e.Agregar(Modelos.OvilloSuelto(Paleta.Bufanda), new Vector3(3.4f, -0.08f, 2.3f), 0f);
        e.Agregar(Modelos.BotonSuelto(Paleta.Mostaza, 0.3f), new Vector3(0.9f, -0.08f, -1.6f), 20f);
        e.Agregar(Modelos.BotonSuelto(Paleta.RosaPastel, 0.22f), new Vector3(-1.2f, -0.08f, -1.7f), 50f);
        LucesYCamara(e, new Vector3(0, 0.62f, 0.1f), 4.4f, 16f, 22f);
        return e;
    }

    /// <summary>Hoja de expresiones: las seis caras de Ovillo y la polilla común normal, pegada y derrotada.</summary>
    public static Escena3D Expresiones()
    {
        var e = new Escena3D();
        e.Agregar(new Pieza { Malla = CatalogoMallas.Caja(30f, 0.5f, 20f, 0.08f), Material = "mesa", Color = Color.white }, new Vector3(0, -0.33f, 3f));
        e.Agregar(new Pieza { Malla = CatalogoMallas.Caja(12.6f, 0.08f, 3.6f, 0.04f), Material = "fieltro", Color = Paleta.Hex("#7E9A4A") }, new Vector3(0, -0.04f, 0.4f));
        var caras = new[] { Expresion.Normal, Expresion.Feliz, Expresion.Guino, Expresion.Preocupado, Expresion.Susto, Expresion.Triste, Expresion.Dolor, Expresion.Euforia };
        for (int i = 0; i < caras.Length; i++)
        {
            var o = Modelos.Ovillo();
            Modelos.AplicarCara(o, Modelos.Rasgos(caras[i]));
            e.Agregar(o, new Vector3(-5.25f + i * 1.5f, 0f, -0.2f), 0f, 1.3f);
        }
        for (int i = 0; i < 3; i++)
        {
            var p = Modelos.Polilla(TipoPolilla.Comun);
            Modelos.AplicarCaraPolilla(p, i == 1 ? 0.4f : 0f, i == 2);
            e.Agregar(p, new Vector3(-1.5f + i * 1.5f, 0f, 1.5f), 0f, 1.3f);
        }
        LucesYCamara(e, new Vector3(0, 0.5f, 0.6f), 5.6f, 30f, 22f);
        return e;
    }

    /// <summary>Retrato de Ovillo como en su hoja: de tres cuartos, saludando con un bracito, sobre fieltro verde.</summary>
    public static Escena3D Retrato(Expresion cara = Expresion.Normal, float alzarIzq = 1f, float alzarDer = 0f, float giro = 22f)
    {
        var e = new Escena3D();
        e.Agregar(new Pieza { Malla = CatalogoMallas.Caja(30f, 0.5f, 20f, 0.08f), Material = "mesa", Color = Color.white }, new Vector3(0, -0.33f, 3f));
        var cesped = new Pieza { Malla = CatalogoMallas.Caja(4.5f, 0.08f, 3.2f, 0.04f), Material = "casilla", Color = Paleta.Hex("#7E9A4A"), Pelusa = 1 };
        e.Agregar(cesped, new Vector3(0, -0.04f, 0.6f));
        var o = Modelos.Ovillo();
        Modelos.AplicarCara(o, Modelos.Rasgos(cara));
        Posar(o, "brazo_izq", alzarIzq);
        Posar(o, "brazo_der", alzarDer);
        e.Agregar(o, new Vector3(0f, 0f, 0f), giro, 1.3f);
        e.Agregar(Modelos.BobinaGrande(Paleta.AzulSuave), new Vector3(-1.6f, -0.08f, 2.6f), 30f);
        e.Agregar(Modelos.OvilloSuelto(Paleta.Morado), new Vector3(1.9f, -0.08f, 2.3f), 0f);
        LucesYCamara(e, new Vector3(0, 0.55f, 0f), 1.55f, 12f, 24f);
        return e;
    }

    /// <summary>Alza un bracito en la descripción del modelo (igual que VistaOvillo.PonerBrazo con alzar = 1).</summary>
    private static void Posar(Pieza p, string grupo, float alzar)
    {
        if (p.Grupo == grupo)
        {
            float lado = grupo.EndsWith("_izq") ? -1f : 1f;
            p.Rotacion = new Vector3(0, 0, lado * 48f * (alzar >= 0f ? 1f - 2f * alzar : 1f + alzar));
            p.Posicion += new Vector3(lado * 0.03f * Mathf.Max(0f, alzar), 0.13f * Mathf.Max(0f, alzar), 0f);
        }
        foreach (var h in p.Hijos) Posar(h, grupo, alzar);
    }

    /// <summary>Tejiendo: la lana a medio camino, con el ovillito en la punta (más pequeño cuanto menos hilo queda).</summary>
    public static Escena3D Tejiendo(Sala sala, float fraccion)
    {
        var e = Sala(sala, new (TipoPolilla, int, bool)[0], fraccion);
        return e;
    }

    /// <summary>Hoja de giro: el mismo personaje de frente, de lado y de espaldas, sobre casillas (escala de juego).</summary>
    public static Escena3D Giro(Pieza modelo, float escala)
    {
        var e = new Escena3D();
        e.Agregar(new Pieza { Malla = CatalogoMallas.Caja(20f, 0.5f, 14f, 0.08f), Material = "mesa", Color = Color.white }, new Vector3(0, -0.33f, 3f));
        float[] giros = { 0f, -90f, 180f };
        for (int i = 0; i < 3; i++)
        {
            float x = (i - 1) * 1.55f;
            e.Agregar(new Pieza { Malla = CatalogoMallas.Caja(0.92f, 0.16f, 0.92f, 0.05f), Material = "casilla", Color = Paleta.Musgo }, new Vector3(x, -0.08f + 0.08f, 0));
            e.Agregar(modelo, new Vector3(x, 0.16f, 0), giros[i], escala);
        }
        LucesYCamara(e, new Vector3(0, 0.5f, 0), 3.5f, 10f, 20f);
        return e;
    }

    private static void LucesYCamara(Escena3D e, Vector3 objetivo, float tamano, float inclinacion = 55f, float fov = 30f)
    {
        var principal = Quaternion.Euler(52f, -35f, 0f) * Vector3.forward;
        var relleno = Quaternion.Euler(35f, 150f, 0f) * Vector3.forward;
        e.Luz("{\"tipo\":\"direccional\",\"d\":" + Escena3D.V(principal) + ",\"c\":\"#FFE6C4\",\"i\":2.2,\"sombra\":1}");
        e.Luz("{\"tipo\":\"direccional\",\"d\":" + Escena3D.V(relleno) + ",\"c\":\"#BFD3FF\",\"i\":0.35,\"sombra\":0}");
        e.Luz("{\"tipo\":\"hemisferio\",\"cielo\":\"#E9D6BC\",\"suelo\":\"#5A3A24\",\"i\":0.85}");
        var rot = Quaternion.Euler(inclinacion, 0f, 0f);
        var adelante = rot * Vector3.forward;
        float distancia = tamano / (2f * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad)) * 1.08f;
        var pos = objetivo - adelante * distancia;
        e.Camara = "{\"p\":" + Escena3D.V(pos) + ",\"t\":" + Escena3D.V(objetivo) + ",\"fov\":" + Escena3D.F(fov) + "}";
    }
}
