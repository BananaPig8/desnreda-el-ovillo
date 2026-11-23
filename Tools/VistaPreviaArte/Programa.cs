using System;
using System.IO;
using System.IO.Compression;
using DesenredaElOvillo.Juego;
using DesenredaElOvillo.Juego.Tres;
using DesenredaElOvillo.Nucleo;
using UnityEngine;

public static class Programa
{
    public static int Main(string[] args)
    {
        string salida = args.Length > 0 ? args[0] : "salida_arte";
        Directory.CreateDirectory(salida);
        void Guardar(string nombre, Sprite s) => Png(Path.Combine(salida, nombre + ".png"), s.texture);

        // Iconos de la interfaz (2D)
        foreach (TipoHerramienta t in Enum.GetValues(typeof(TipoHerramienta)))
            Guardar("icono_" + t, Iconos.Herramienta(t));
        Guardar("icono_carrete", Iconos.Carrete(1, 1));
        Guardar("icono_lana", Iconos.PiezaLana(Iconos.Derecha | Iconos.Izquierda));
        Png(Path.Combine(salida, "ui_panel.png"), Iconos.Panel(Paleta.Crema, Paleta.Hex("#C98A5A"), "ui_panel"));
        Png(Path.Combine(salida, "ui_boton.png"), Iconos.Panel(Paleta.Coral, Paleta.Crema, "ui_boton"));

        // Texturas de los materiales 3D (color + normales)
        foreach (var t in new[] { TexturasTres.Fieltro(), TexturasTres.Casilla(), TexturasTres.Lana(), TexturasTres.Madera(), TexturasTres.Tela(), TexturasTres.Velcro() })
        {
            Png(Path.Combine(salida, t.Nombre + ".png"), t.Color);
            Png(Path.Combine(salida, t.Nombre + "_n.png"), t.Normal);
        }

        // Escenas 3D para el visor three.js
        string raiz = RaizRepositorio();
        Sala Cargar(string id) => Sala.DesdeTexto(File.ReadAllText(Path.Combine(raiz, "Assets", "_Project", "Resources", "Salas", id + ".txt")));
        Exportar3D.Sala(Cargar("sala2"), new (TipoPolilla, int, bool)[] { (TipoPolilla.Comun, 7, false), (TipoPolilla.Veloz, 11, true), (TipoPolilla.Comun, 14, false) })
            .Guardar(Path.Combine(salida, "escena_sala2.json"));
        Exportar3D.Sala(Cargar("sala5"), new (TipoPolilla, int, bool)[] { (TipoPolilla.Reina, 9, false), (TipoPolilla.Comun, 12, false), (TipoPolilla.Acorazada, 5, false) })
            .Guardar(Path.Combine(salida, "escena_sala5.json"));
        Exportar3D.Personajes().Guardar(Path.Combine(salida, "escena_personajes.json"));
        Exportar3D.Giro(Modelos.Ovillo(), 1.3f).Guardar(Path.Combine(salida, "giro_ovillo.json"));
        foreach (TipoPolilla t in Enum.GetValues(typeof(TipoPolilla)))
            Exportar3D.Giro(Modelos.Polilla(t), 1f).Guardar(Path.Combine(salida, "giro_" + t.ToString().ToLowerInvariant() + ".json"));
        Console.WriteLine("Arte y escenas escritos en " + Path.GetFullPath(salida));
        return 0;
    }

    private static string RaizRepositorio()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets", "_Project"))) dir = dir.Parent;
        return dir?.FullName ?? ".";
    }

    private static void Png(string ruta, Pintor p)
    {
        var t = new Texture2D(p.Ancho, p.Alto, TextureFormat.RGBA32, false);
        for (int y = 0; y < p.Alto; y++)
            for (int x = 0; x < p.Ancho; x++)
                t.Pixeles[y * p.Ancho + x] = p.Leer(x, y);
        Png(ruta, t);
    }

    /// <summary>Codificador PNG mínimo (RGBA 8 bits). Unity guarda las texturas de abajo arriba; PNG de arriba abajo.</summary>
    private static void Png(string ruta, Texture2D t)
    {
        int w = t.width, h = t.height;
        var crudo = new byte[(w * 4 + 1) * h];
        int k = 0;
        for (int y = h - 1; y >= 0; y--)
        {
            crudo[k++] = 0;
            for (int x = 0; x < w; x++)
            {
                var c = t.Pixeles[y * w + x];
                crudo[k++] = B(c.r); crudo[k++] = B(c.g); crudo[k++] = B(c.b); crudo[k++] = B(c.a);
            }
        }
        using var fs = File.Create(ruta);
        fs.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var ihdr = new byte[13];
        Be(ihdr, 0, w); Be(ihdr, 4, h); ihdr[8] = 8; ihdr[9] = 6;
        Trozo(fs, "IHDR", ihdr);
        using (var ms = new MemoryStream())
        {
            using (var z = new ZLibStream(ms, CompressionLevel.Optimal, true)) z.Write(crudo);
            Trozo(fs, "IDAT", ms.ToArray());
        }
        Trozo(fs, "IEND", Array.Empty<byte>());
    }

    private static byte B(float v) => (byte)Math.Clamp((int)MathF.Round(v * 255f), 0, 255);
    private static void Be(byte[] b, int o, int v) { b[o] = (byte)(v >> 24); b[o + 1] = (byte)(v >> 16); b[o + 2] = (byte)(v >> 8); b[o + 3] = (byte)v; }

    private static void Trozo(Stream s, string tipo, byte[] datos)
    {
        var len = new byte[4]; Be(len, 0, datos.Length); s.Write(len);
        var t = System.Text.Encoding.ASCII.GetBytes(tipo);
        s.Write(t); s.Write(datos);
        uint crc = Crc(t, 0xFFFFFFFFu); crc = Crc(datos, crc) ^ 0xFFFFFFFFu;
        var c = new byte[4]; Be(c, 0, (int)crc); s.Write(c);
    }

    private static uint[] _tabla;
    private static uint Crc(byte[] d, uint c)
    {
        if (_tabla == null)
        {
            _tabla = new uint[256];
            for (uint n = 0; n < 256; n++) { uint v = n; for (int i = 0; i < 8; i++) v = (v & 1) != 0 ? 0xEDB88320u ^ (v >> 1) : v >> 1; _tabla[n] = v; }
        }
        foreach (var b in d) c = _tabla[(c ^ b) & 0xFF] ^ (c >> 8);
        return c;
    }
}
