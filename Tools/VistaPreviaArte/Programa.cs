using System;
using System.IO;
using System.IO.Compression;
using DesenredaElOvillo.Juego;
using DesenredaElOvillo.Nucleo;
using UnityEngine;

public static class Programa
{
    public static int Main(string[] args)
    {
        string salida = args.Length > 0 ? args[0] : "salida_arte";
        Directory.CreateDirectory(salida);
        void Guardar(string nombre, Sprite s) => Png(Path.Combine(salida, nombre + ".png"), s.texture);

        foreach (TipoPolilla t in Enum.GetValues(typeof(TipoPolilla)))
        {
            Guardar("alas_" + t, Texturas.AlasPolilla(t));
            Guardar("cuerpo_" + t, Texturas.CuerpoPolilla(t));
        }
        Guardar("placas", Texturas.PlacasAcorazada());
        Guardar("capa_reina", Texturas.CapaReina());
        Guardar("corona", Texturas.Corona());
        foreach (TipoHerramienta t in Enum.GetValues(typeof(TipoHerramienta)))
            Guardar("herramienta_" + t, Texturas.Herramienta(t));
        Guardar("herramienta_Alfiler_doblada", Texturas.Herramienta(TipoHerramienta.Alfiler, true));
        Guardar("casilla_0", Texturas.Casilla(0));
        Guardar("casilla_1", Texturas.Casilla(1));
        Guardar("mesa", Texturas.Mesa());
        foreach (char c in new[] { '#', 'o', 'u', 'c', 'a', 'm', 't' }) Guardar("obstaculo_" + (int)c, Texturas.Obstaculo(c));
        for (int v = 0; v <= 4; v++) Guardar("carrete_" + v, Texturas.Carrete(v, 4));
        Guardar("entrada", Texturas.Entrada());
        Guardar("lampara", Texturas.Lampara());
        Guardar("brillo", Texturas.Brillo());
        Guardar("huella", Texturas.Huella());
        Guardar("circulo", Texturas.Circulo());
        for (int m = 1; m < 16; m++)
        {
            int bits = 0; for (int k = 0; k < 4; k++) if ((m & (1 << k)) != 0) bits++;
            if (bits <= 2) Guardar("lana_" + m, Texturas.PiezaLana(m));
        }
        Png(Path.Combine(salida, "ui_panel.png"), Texturas.Panel(Paleta.Crema, Paleta.Hex("#C98A5A"), "ui_panel"));
        Png(Path.Combine(salida, "ui_boton.png"), Texturas.Panel(Paleta.Coral, Paleta.Crema, "ui_boton"));
        Png(Path.Combine(salida, "ui_panel_oscuro.png"), Texturas.Panel(Paleta.Hex("#5A3A28"), Paleta.Hex("#E9C79E"), "ui_panel_oscuro"));
        Console.WriteLine("Arte provisional escrito en " + Path.GetFullPath(salida));
        return 0;
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
