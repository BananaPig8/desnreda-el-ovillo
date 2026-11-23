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
/// Modelos de bloqueo en glTF binario (.glb) para Blender u otro programa 3D: la misma jerarquía de piezas que el
/// juego, con sus pivotes, sus nombres de grupo (en el nombre del nodo y en "extras", que Blender importa como
/// propiedades personalizadas) y colores PBR planos. Sirven de referencia de proporciones, pivotes y nombres
/// para modelar el arte final. No llevan pelusa ni sombras de contacto; las piezas de expresiones alternativas
/// van ocultas (escala casi cero y "(oculta)" en el nombre).
/// Conversión de Unity (mano izquierda) a glTF (mano derecha): se invierte Z en posiciones y normales, se da la
/// vuelta a los triángulos, la rotación pasa a (-x, -y, z, w) y la V de las UV a 1 - v.
/// </summary>
public sealed class Glb
{
    private readonly List<string> _nodos = new List<string>();
    private readonly List<string> _mallas = new List<string>();
    private readonly List<string> _materiales = new List<string>();
    private readonly List<string> _accesores = new List<string>();
    private readonly List<string> _vistas = new List<string>();
    private readonly MemoryStream _binario = new MemoryStream();
    private readonly Dictionary<string, int[]> _geometrias = new Dictionary<string, int[]>();
    private readonly Dictionary<string, int> _indiceMaterial = new Dictionary<string, int>();
    private readonly Dictionary<string, int> _indiceMalla = new Dictionary<string, int>();

    /// <summary>Suavidad y metal de cada material del juego (los mismos valores que Fabrica.cs).</summary>
    private static readonly Dictionary<string, (float suavidad, float metal)> Pbr = new Dictionary<string, (float, float)>
    {
        ["fieltro"] = (0.06f, 0f), ["casilla"] = (0.05f, 0f), ["lana"] = (0.12f, 0f), ["madera"] = (0.28f, 0f),
        ["mesa"] = (0.32f, 0f), ["tela"] = (0.12f, 0f), ["velcro"] = (0.1f, 0f), ["metal"] = (0.62f, 0.85f),
        ["oro"] = (0.55f, 0.9f), ["boton"] = (0.42f, 0f), ["brillante"] = (0.86f, 0f), ["emisivo"] = (0.3f, 0f),
        ["punto"] = (0.08f, 0f), ["liso"] = (0.45f, 0f)
    };

    public static void Exportar(Pieza raiz, string ruta)
    {
        var g = new Glb();
        int nodoRaiz = g.Nodo(raiz, false);
        g.Guardar(ruta, nodoRaiz);
    }

    private int Nodo(Pieza p, bool oculta)
    {
        oculta |= p.Oculta;
        var hijos = new List<int>();
        foreach (var h in p.Hijos) hijos.Add(Nodo(h, oculta));

        var q = Quaternion.Euler(p.Rotacion);
        var escala = p.Oculta ? p.Escala * 0.0001f : p.Escala;
        var sb = new StringBuilder("{");
        string nombre = (p.Nombre ?? "Pieza") + (string.IsNullOrEmpty(p.Grupo) ? "" : " [" + p.Grupo + "]") + (p.Oculta ? " (oculta)" : "");
        sb.Append("\"name\":").Append(Texto(nombre));
        sb.Append(",\"translation\":[").Append(F(p.Posicion.x)).Append(',').Append(F(p.Posicion.y)).Append(',').Append(F(-p.Posicion.z)).Append(']');
        sb.Append(",\"rotation\":[").Append(F(-q.x)).Append(',').Append(F(-q.y)).Append(',').Append(F(q.z)).Append(',').Append(F(q.w)).Append(']');
        sb.Append(",\"scale\":[").Append(F(escala.x)).Append(',').Append(F(escala.y)).Append(',').Append(F(escala.z)).Append(']');
        if (!string.IsNullOrEmpty(p.Malla)) sb.Append(",\"mesh\":").Append(Malla(p.Malla, p.Material, p.Color));
        if (hijos.Count > 0) sb.Append(",\"children\":[").Append(string.Join(",", hijos)).Append(']');
        var extras = new List<string>();
        if (!string.IsNullOrEmpty(p.Grupo)) extras.Add("\"grupo\":" + Texto(p.Grupo));
        if (!string.IsNullOrEmpty(p.Material)) extras.Add("\"material\":" + Texto(p.Material));
        if (p.Pelusa > 0) extras.Add("\"capas_pelusa\":" + p.Pelusa);
        if (p.Oculta) extras.Add("\"oculta\":true");
        if (extras.Count > 0) sb.Append(",\"extras\":{").Append(string.Join(",", extras)).Append('}');
        sb.Append('}');
        _nodos.Add(sb.ToString());
        return _nodos.Count - 1;
    }

    /// <summary>Una malla glTF por pareja (geometría, material); la geometría se comparte entre parejas.</summary>
    private int Malla(string clave, string material, Color color)
    {
        int mat = Material(material, color);
        string id = clave + "#" + mat;
        if (_indiceMalla.TryGetValue(id, out int i)) return i;
        var acc = Geometria(clave);
        _mallas.Add("{\"name\":" + Texto(clave) + ",\"primitives\":[{\"attributes\":{\"POSITION\":" + acc[0] + ",\"NORMAL\":" + acc[1] +
                    ",\"TEXCOORD_0\":" + acc[2] + "},\"indices\":" + acc[3] + ",\"material\":" + mat + "}]}");
        _indiceMalla[id] = _mallas.Count - 1;
        return _mallas.Count - 1;
    }

    private int Material(string id, Color c)
    {
        string clave = id + "|" + F(c.r) + "," + F(c.g) + "," + F(c.b);
        if (_indiceMaterial.TryGetValue(clave, out int i)) return i;
        var (suavidad, metal) = Pbr.TryGetValue(id ?? "", out var v) ? v : (0.1f, 0f);
        string color = "[" + F(Lineal(c.r)) + "," + F(Lineal(c.g)) + "," + F(Lineal(c.b)) + ",1]";
        var sb = new StringBuilder("{\"name\":").Append(Texto(id + " " + Hex(c)))
            .Append(",\"pbrMetallicRoughness\":{\"baseColorFactor\":").Append(color)
            .Append(",\"metallicFactor\":").Append(F(metal)).Append(",\"roughnessFactor\":").Append(F(1f - suavidad)).Append('}');
        if (id == "emisivo") sb.Append(",\"emissiveFactor\":[").Append(F(Lineal(c.r))).Append(',').Append(F(Lineal(c.g))).Append(',').Append(F(Lineal(c.b))).Append(']');
        sb.Append('}');
        _materiales.Add(sb.ToString());
        _indiceMaterial[clave] = _materiales.Count - 1;
        return _materiales.Count - 1;
    }

    private int[] Geometria(string clave)
    {
        if (_geometrias.TryGetValue(clave, out var acc)) return acc;
        var d = CatalogoMallas.Obtener(clave);
        int n = d.Vertices.Count;
        var pos = new float[n * 3];
        var nor = new float[n * 3];
        var uv = new float[n * 2];
        var min = new[] { float.MaxValue, float.MaxValue, float.MaxValue };
        var max = new[] { float.MinValue, float.MinValue, float.MinValue };
        for (int i = 0; i < n; i++)
        {
            var v = d.Vertices[i];
            var w = d.Normales[i];
            pos[i * 3] = v.x; pos[i * 3 + 1] = v.y; pos[i * 3 + 2] = -v.z;
            nor[i * 3] = w.x; nor[i * 3 + 1] = w.y; nor[i * 3 + 2] = -w.z;
            uv[i * 2] = d.Uvs[i].x; uv[i * 2 + 1] = 1f - d.Uvs[i].y;
            for (int k = 0; k < 3; k++)
            {
                min[k] = Math.Min(min[k], pos[i * 3 + k]);
                max[k] = Math.Max(max[k], pos[i * 3 + k]);
            }
        }
        var ind = new uint[d.Triangulos.Count];
        for (int t = 0; t + 2 < d.Triangulos.Count; t += 3)
        {
            ind[t] = (uint)d.Triangulos[t];
            ind[t + 1] = (uint)d.Triangulos[t + 2];
            ind[t + 2] = (uint)d.Triangulos[t + 1];
        }
        acc = new[]
        {
            Accesor(Bytes(pos), n, "VEC3", 5126, 34962, "[" + F(min[0]) + "," + F(min[1]) + "," + F(min[2]) + "]", "[" + F(max[0]) + "," + F(max[1]) + "," + F(max[2]) + "]"),
            Accesor(Bytes(nor), n, "VEC3", 5126, 34962),
            Accesor(Bytes(uv), n, "VEC2", 5126, 34962),
            Accesor(Bytes(ind), ind.Length, "SCALAR", 5125, 34963)
        };
        _geometrias[clave] = acc;
        return acc;
    }

    private int Accesor(byte[] datos, int cuenta, string tipo, int componente, int destino, string min = null, string max = null)
    {
        while (_binario.Length % 4 != 0) _binario.WriteByte(0);
        long desplazamiento = _binario.Length;
        _binario.Write(datos, 0, datos.Length);
        _vistas.Add("{\"buffer\":0,\"byteOffset\":" + desplazamiento + ",\"byteLength\":" + datos.Length + ",\"target\":" + destino + "}");
        var sb = new StringBuilder("{\"bufferView\":").Append(_vistas.Count - 1).Append(",\"componentType\":").Append(componente)
            .Append(",\"count\":").Append(cuenta).Append(",\"type\":\"").Append(tipo).Append('"');
        if (min != null) sb.Append(",\"min\":").Append(min).Append(",\"max\":").Append(max);
        sb.Append('}');
        _accesores.Add(sb.ToString());
        return _accesores.Count - 1;
    }

    private void Guardar(string ruta, int raiz)
    {
        while (_binario.Length % 4 != 0) _binario.WriteByte(0);
        var json = new StringBuilder();
        json.Append("{\"asset\":{\"version\":\"2.0\",\"generator\":\"Desenreda el Ovillo - Tools/VistaPreviaArte\"}");
        json.Append(",\"scene\":0,\"scenes\":[{\"nodes\":[").Append(raiz).Append("]}]");
        json.Append(",\"nodes\":[").Append(string.Join(",", _nodos)).Append(']');
        json.Append(",\"meshes\":[").Append(string.Join(",", _mallas)).Append(']');
        json.Append(",\"materials\":[").Append(string.Join(",", _materiales)).Append(']');
        json.Append(",\"accessors\":[").Append(string.Join(",", _accesores)).Append(']');
        json.Append(",\"bufferViews\":[").Append(string.Join(",", _vistas)).Append(']');
        json.Append(",\"buffers\":[{\"byteLength\":").Append(_binario.Length).Append("}]}");
        var bytesJson = Encoding.UTF8.GetBytes(json.ToString());
        int rellenoJson = (4 - bytesJson.Length % 4) % 4;
        var bin = _binario.ToArray();
        int total = 12 + 8 + bytesJson.Length + rellenoJson + 8 + bin.Length;
        using var fs = File.Create(ruta);
        using var w = new BinaryWriter(fs);
        w.Write(0x46546C67u);
        w.Write(2u);
        w.Write((uint)total);
        w.Write((uint)(bytesJson.Length + rellenoJson));
        w.Write(0x4E4F534Au);
        w.Write(bytesJson);
        for (int i = 0; i < rellenoJson; i++) w.Write((byte)' ');
        w.Write((uint)bin.Length);
        w.Write(0x004E4942u);
        w.Write(bin);
    }

    private static byte[] Bytes(float[] v)
    {
        var b = new byte[v.Length * 4];
        Buffer.BlockCopy(v, 0, b, 0, b.Length);
        return b;
    }

    private static byte[] Bytes(uint[] v)
    {
        var b = new byte[v.Length * 4];
        Buffer.BlockCopy(v, 0, b, 0, b.Length);
        return b;
    }

    private static float Lineal(float c) => c <= 0.04045f ? c / 12.92f : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);
    private static string Hex(Color c) => "#" + ((int)Math.Round(c.r * 255)).ToString("X2") + ((int)Math.Round(c.g * 255)).ToString("X2") + ((int)Math.Round(c.b * 255)).ToString("X2");
    private static string F(float v) => float.IsNaN(v) || float.IsInfinity(v) ? "0" : Math.Round(v, 6).ToString(CultureInfo.InvariantCulture);
    private static string Texto(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}

/// <summary>Los modelos del juego que se exportan como bloqueo y se verifican.</summary>
public static class CatalogoModelos
{
    public static IEnumerable<(string nombre, Pieza modelo)> Todos()
    {
        yield return ("ovillo", Modelos.Ovillo());
        // el modelo OBJ entregado, ya troceado por grupos de animación y con pivotes (punto de partida para el rig)
        if (OvilloImportado.Disponible) yield return ("ovillo_modelo_obj_por_piezas", OvilloImportado.Modelo(false));
        foreach (TipoPolilla t in Enum.GetValues(typeof(TipoPolilla)))
            yield return ("polilla_" + t.ToString().ToLowerInvariant(), Modelos.Polilla(t));
        foreach (TipoHerramienta t in Enum.GetValues(typeof(TipoHerramienta)))
            yield return ("herramienta_" + t.ToString().ToLowerInvariant(), Modelos.Herramienta(t));
        yield return ("herramienta_alfiler_gastado", Modelos.Herramienta(TipoHerramienta.Alfiler, true));
        yield return ("carrete", Modelos.Carrete(1f));
        yield return ("lampara", Modelos.Lampara());
        yield return ("entrada", Modelos.Entrada(Vector3.back));
        yield return ("huella", Modelos.Huella());
        yield return ("ovillo_de_trabajo", Modelos.OvilloDeTrabajo(Paleta.ColoresLana[0]));
        foreach (var (simbolo, nombre) in new[] { ('#', "caja_de_costura"), ('o', "boton_gigante"), ('u', "dedal"), ('c', "carrete_pequeno"), ('a', "acerico"), ('m', "metro") })
            yield return ("obstaculo_" + nombre, Modelos.Obstaculo(simbolo));
        yield return ("decorado_bobina_grande", Modelos.BobinaGrande(Paleta.AzulSuave));
        yield return ("decorado_ovillo_suelto", Modelos.OvilloSuelto(Paleta.Bufanda));
        yield return ("decorado_boton_suelto", Modelos.BotonSuelto(Paleta.Mostaza, 0.3f));
        yield return ("decorado_rollo_de_tela", Modelos.RolloDeTela(Paleta.Morado));
        yield return ("decorado_tijeras_grandes", Modelos.TijerasGrandes());
    }
}

/// <summary>
/// Control de calidad de las mallas procedurales (lo ejecuta CI): todas las claves que usan los modelos, sus
/// capas de pelusa, la casilla, la sombra y la lana del tablero. Errores: valores no finitos, listas de distinto
/// tamaño, índices fuera de rango, normales que no miden 1 y triángulos al revés respecto a sus normales.
/// Los triángulos degenerados (los polos de las esferas) solo se cuentan.
/// </summary>
public static class VerificadorMallas
{
    public static int Ejecutar()
    {
        var claves = new SortedSet<string>(StringComparer.Ordinal);
        void Recorrer(Pieza p)
        {
            if (!string.IsNullOrEmpty(p.Malla))
            {
                claves.Add(p.Malla);
                for (int k = 1; k <= p.Pelusa; k++) claves.Add(CapasPelusa.Malla(p.Malla, k, p.CaraLibre));
            }
            foreach (var h in p.Hijos) Recorrer(h);
        }
        foreach (var (_, modelo) in CatalogoModelos.Todos()) Recorrer(modelo);
        if (OvilloImportado.Disponible) Recorrer(OvilloImportado.Modelo(true));
        var casilla = CatalogoMallas.Caja(0.92f, Medidas.AlturaCasilla, 0.92f, 0.05f);
        claves.Add(casilla);
        claves.Add(CapasPelusa.Malla(casilla, 1, false));
        claves.Add(CatalogoMallas.Plano(1f, 1f));

        var mallas = new List<(string, DatosMalla)>();
        foreach (var c in claves) mallas.Add((c, CatalogoMallas.Obtener(c)));
        var lana = new List<Vector3>();
        for (int i = 0; i < 12; i++) lana.Add(new Vector3(0.5f + (i % 2 == 0 ? i : i - 1) * 0.5f, Medidas.AlturaLana, 0.5f + (i % 2 == 1 ? 1f : 0f)));
        mallas.Add(("lana del tablero", Mallas.Tubo(Mallas.CaminoSuave(lana), Medidas.RadioLana, 14, 2.5f)));

        int errores = 0, triangulos = 0, degenerados = 0;
        foreach (var (clave, d) in mallas)
        {
            var fallos = Revisar(d, out int tris, out int deg);
            triangulos += tris;
            degenerados += deg;
            if (fallos.Count == 0) continue;
            errores += fallos.Count;
            Console.WriteLine("✗ " + clave + ": " + string.Join("; ", fallos));
        }
        Console.WriteLine(mallas.Count + " mallas, " + triangulos + " triángulos (" + degenerados + " degenerados, sin importancia), " + errores + " errores.");
        return errores == 0 ? 0 : 1;
    }

    private static List<string> Revisar(DatosMalla d, out int triangulos, out int degenerados)
    {
        var fallos = new List<string>();
        int n = d.Vertices.Count;
        triangulos = d.Triangulos.Count / 3;
        degenerados = 0;
        if (d.Normales.Count != n || d.Uvs.Count != n) fallos.Add("listas de distinto tamaño");
        if (d.Triangulos.Count % 3 != 0) fallos.Add("índices que no son múltiplo de 3");
        if (n == 0 || triangulos == 0) fallos.Add("malla vacía");
        int noFinitos = 0, normalesMalas = 0, fuera = 0, alReves = 0;
        for (int i = 0; i < n; i++)
        {
            var v = d.Vertices[i];
            var w = i < d.Normales.Count ? d.Normales[i] : Vector3.zero;
            var uv = i < d.Uvs.Count ? d.Uvs[i] : Vector2.zero;
            if (!Finito(v.x) || !Finito(v.y) || !Finito(v.z) || !Finito(w.x) || !Finito(w.y) || !Finito(w.z) || !Finito(uv.x) || !Finito(uv.y)) noFinitos++;
            float largo = w.magnitude;
            if (largo < 0.98f || largo > 1.02f) normalesMalas++;
        }
        for (int t = 0; t + 2 < d.Triangulos.Count; t += 3)
        {
            int a = d.Triangulos[t], b = d.Triangulos[t + 1], c = d.Triangulos[t + 2];
            if (a < 0 || b < 0 || c < 0 || a >= n || b >= n || c >= n) { fuera++; continue; }
            var cruz = Vector3.Cross(d.Vertices[b] - d.Vertices[a], d.Vertices[c] - d.Vertices[a]);
            if (cruz.sqrMagnitude < 1e-14f) { degenerados++; continue; }
            if (Vector3.Dot(cruz, d.Normales[a] + d.Normales[b] + d.Normales[c]) <= 0f) alReves++;
        }
        if (noFinitos > 0) fallos.Add(noFinitos + " vértices con valores no finitos");
        if (normalesMalas > 0) fallos.Add(normalesMalas + " normales que no miden 1");
        if (fuera > 0) fallos.Add(fuera + " triángulos con índices fuera de rango");
        if (alReves > 0) fallos.Add(alReves + " triángulos al revés respecto a sus normales");
        return fallos;
    }

    private static bool Finito(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
}
