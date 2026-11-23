using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DesenredaElOvillo.Nucleo;

namespace DesenredaElOvillo.Laboratorio
{
    /// <summary>
    /// Laboratorio de balance de Desenreda el Ovillo.
    ///   dotnet run -c Release -- analizar sala2 --costurero Alfiler,Velcro,Bobina,Alfiler [--mejoras AlfileresAfilados]
    ///   dotnet run -c Release -- informe            (escribe Docs/Balance/informe_balance.md)
    ///   dotnet run -c Release -- mapa sala3
    ///   dotnet run -c Release -- ver sala2 --recorrido DDAA... --colocacion Alfiler@3,Velcro@3 --costurero ...
    /// </summary>
    public static class Programa
    {
        public static int Main(string[] args)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            Console.OutputEncoding = Encoding.UTF8;
            if (args.Length == 0)
            {
                Console.WriteLine("Uso: analizar <sala> [opciones] | informe | mapa <sala> | ver <sala> [opciones]");
                return 1;
            }
            var opciones = LeerOpciones(args.Skip(1));
            switch (args[0])
            {
                case "analizar": return Analizar(args[1], opciones);
                case "informe": return Informe(opciones);
                case "mapa": return Mapa(args[1]);
                case "ver": return Ver(args[1], opciones);
                case "fijar": return Fijar(args[1], opciones);
                default:
                    Console.WriteLine("Orden desconocida: " + args[0]);
                    return 1;
            }
        }

        private static Dictionary<string, string> LeerOpciones(IEnumerable<string> args)
        {
            var d = new Dictionary<string, string>();
            string clave = null;
            foreach (var a in args)
            {
                if (a.StartsWith("--")) { clave = a.Substring(2); d[clave] = "true"; }
                else if (clave != null) { d[clave] = a; clave = null; }
                else d["sala"] = a;
            }
            return d;
        }

        private static string RaizRepositorio()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets", "_Project"))) dir = dir.Parent;
            if (dir == null) throw new DirectoryNotFoundException("No encuentro la raíz del repositorio.");
            return dir.FullName;
        }

        public static Sala CargarSala(string id)
        {
            var ruta = Path.Combine(RaizRepositorio(), "Assets", "_Project", "Resources", "Salas", id + ".txt");
            return Sala.DesdeTexto(File.ReadAllText(ruta));
        }

        private static int Mapa(string id)
        {
            var s = CargarSala(id);
            Console.WriteLine(s.Nombre + " (" + s.Tablero.Ancho + "x" + s.Tablero.Alto + "), hilo " + s.Hilo + ", mínimo " + s.LongitudMinima);
            Console.WriteLine("Oleada: " + s.Oleada);
            Console.WriteLine(s.Tablero.ATexto());
            return 0;
        }

        private static int Ver(string id, Dictionary<string, string> o)
        {
            var sala = CargarSala(id);
            var reglas = Reglas.PorDefecto().ConMejoras(o.TryGetValue("mejoras", out var m) ? Sala.ParsearMejoras(m) : new List<TipoMejora>());
            int hilo = sala.Hilo + reglas.HiloExtraPorSala + (o.TryGetValue("hiloextra", out var he) ? int.Parse(he) : 0);
            var r = Recorrido.DesdeMovimientos(sala.Tablero, o["recorrido"], hilo);
            if (r == null) { Console.WriteLine("Recorrido no válido."); return 1; }
            var plan = new PlanColocacion(Sala.ParsearColocaciones(o.TryGetValue("colocacion", out var c) ? c : ""));
            int vida = o.TryGetValue("vida", out var v) ? int.Parse(v) : reglas.VidaCarrete;
            var res = SimuladorOleada.Simular(reglas, r, plan, sala.Oleada, vida, true);
            Console.WriteLine(Dibujar(r, plan.Colocaciones.ToList()));
            foreach (var e in res.Eventos) Console.WriteLine("  " + e);
            Console.WriteLine(Resumen(res));
            return 0;
        }

        private static int Analizar(string id, Dictionary<string, string> o)
        {
            var sala = CargarSala(id);
            if (o.TryGetValue("oleada", out var ol)) sala.Oleada = Oleada.DesdeTexto(ol);
            if (o.TryGetValue("hilo", out var hs)) sala.Hilo = int.Parse(hs);
            var costurero = o.TryGetValue("costurero", out var k) ? Sala.ParsearHerramientas(k) : new List<TipoHerramienta>(sala.OfertaFija);
            var mejoras = o.TryGetValue("mejoras", out var m) ? Sala.ParsearMejoras(m) : new List<TipoMejora>();
            int semilla = o.TryGetValue("semilla", out var s) ? int.Parse(s) : 1;
            int recorridos = o.TryGetValue("recorridos", out var rc) ? int.Parse(rc) : 600;
            int hiloExtra = o.TryGetValue("hiloextra", out var he) ? int.Parse(he) : 0;
            int vida = o.TryGetValue("vida", out var v) ? int.Parse(v) : -1;
            var reglas = Reglas.PorDefecto();
            AplicarAjustes(reglas, o);
            reglas = reglas.ConMejoras(mejoras);

            var reloj = Stopwatch.StartNew();
            var buscador = new Buscador(sala, reglas, costurero, semilla, hiloExtra, vida);
            var mejor = buscador.Buscar(recorridos, 250, 12, 2500);
            var ingenuo = buscador.JugadorIngenuo(2000);
            var aplicado = buscador.JugadorIngenuo(600, 25);
            reloj.Stop();

            Console.WriteLine("== " + sala.Nombre + " | costurero: " + string.Join(",", costurero) + (mejoras.Count > 0 ? " | mejoras: " + string.Join(",", mejoras) : ""));
            Console.WriteLine("Oleada: " + sala.Oleada + " | hilo " + buscador.Hilo + " | mínimo " + sala.LongitudMinima);
            if (mejor == null) { Console.WriteLine("No se ha encontrado ningún recorrido válido."); return 1; }
            Console.WriteLine(Dibujar(mejor.Recorrido, mejor.Colocaciones));
            Console.WriteLine("Mejor encontrado: " + Resumen(mejor.Resultado) + " | hilo " + mejor.Recorrido.HiloUsado + " | curvas " + mejor.Recorrido.ContarCurvas());
            Console.WriteLine("solucion_recorrido: " + mejor.Recorrido.AMovimientos());
            Console.WriteLine("solucion_costurero: " + string.Join(", ", costurero));
            Console.WriteLine("solucion_colocacion: " + mejor.ColocacionesTexto());
            if (mejoras.Count > 0) Console.WriteLine("solucion_mejoras: " + string.Join(", ", mejoras));
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "Ingenuo (todo al azar): gana {0:0}% | perfecto {1:0}% | daño medio {2:0.00} | histograma daño {3}",
                ingenuo.PorcentajeVictorias, ingenuo.PorcentajePerfectas, ingenuo.DanioMedio, string.Join("/", ingenuo.Histograma.Take(5))));
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "Aplicado (25 pruebas por lana): gana {0:0}% | perfecto {1:0}% | daño medio {2:0.00}",
                aplicado.PorcentajeVictorias, aplicado.PorcentajePerfectas, aplicado.DanioMedio));
            Console.WriteLine("(" + reloj.ElapsedMilliseconds + " ms)");
            return 0;
        }

        /// <summary>
        /// Ajustes de prueba sin tocar el código: --set "Alfiler.Usos=3;Tijeras.Danio=6;Acorazada.Vida=8;VidaCarrete=5"
        /// También admite --oleada "Comun@1, Veloz@3" para probar otro guion en la misma sala.
        /// </summary>
        public static void AplicarAjustes(Reglas reglas, Dictionary<string, string> o)
        {
            if (!o.TryGetValue("set", out var ajustes)) return;
            foreach (var ajusteBruto in ajustes.Split(';'))
            {
                var ajuste = ajusteBruto.Trim();
                if (ajuste.Length == 0) continue;
                var partes = ajuste.Split('=');
                var ruta = partes[0].Trim().Split('.');
                object destino = reglas;
                string campo = ruta[0];
                if (ruta.Length == 2)
                {
                    campo = ruta[1];
                    if (Enum.TryParse<TipoHerramienta>(ruta[0], true, out var th)) destino = reglas.Herramienta(th);
                    else if (Enum.TryParse<TipoPolilla>(ruta[0], true, out var tp)) destino = reglas.Polilla(tp);
                    else throw new ArgumentException("No conozco " + ruta[0]);
                }
                var f = destino.GetType().GetField(campo);
                if (f == null) throw new ArgumentException("No existe el campo " + campo);
                if (f.FieldType == typeof(int)) f.SetValue(destino, int.Parse(partes[1]));
                else if (f.FieldType == typeof(bool)) f.SetValue(destino, bool.Parse(partes[1]));
                else if (f.FieldType == typeof(int[])) f.SetValue(destino, partes[1].Split('/').Select(int.Parse).ToArray());
                else throw new ArgumentException("Tipo no admitido para " + campo);
            }
        }

        /// <summary>
        /// Busca a fondo una solución perfecta (sin daño al carrete) y la escribe como solución de referencia
        /// en el archivo de la sala, para que las pruebas automáticas vigilen que la sala sigue siendo ganable.
        /// </summary>
        private static int Fijar(string id, Dictionary<string, string> o)
        {
            var sala = CargarSala(id);
            var costurero = Sala.ParsearHerramientas(o["costurero"]);
            var mejoras = o.TryGetValue("mejoras", out var m) ? Sala.ParsearMejoras(m) : new List<TipoMejora>();
            var reglas = Reglas.PorDefecto().ConMejoras(mejoras);
            Propuesta mejor = null;
            for (int semilla = 1; semilla <= 6; semilla++)
            {
                var b = new Buscador(sala, reglas, costurero, semilla);
                var p = b.Buscar(500, 300, 12, 3000);
                if (p != null && (mejor == null || p.Puntuacion > mejor.Puntuacion ||
                    (p.Puntuacion == mejor.Puntuacion && p.Recorrido.HiloUsado < mejor.Recorrido.HiloUsado))) mejor = p;
                if (mejor != null && mejor.Resultado.Victoria && mejor.Resultado.VidaCarreteFinal == mejor.Resultado.VidaCarreteInicial && semilla >= 2) break;
            }
            if (mejor == null || mejor.Resultado.VidaCarreteFinal != mejor.Resultado.VidaCarreteInicial)
            {
                Console.WriteLine("No se ha encontrado solución perfecta. Mejor: " + (mejor == null ? "ninguna" : Resumen(mejor.Resultado)));
                return 1;
            }
            Console.WriteLine(Dibujar(mejor.Recorrido, mejor.Colocaciones));
            Console.WriteLine(Resumen(mejor.Resultado));
            var ruta = Path.Combine(RaizRepositorio(), "Assets", "_Project", "Resources", "Salas", id + ".txt");
            var lineas = File.ReadAllLines(ruta).Where(l => !l.TrimStart().StartsWith("solucion_")).ToList();
            int iMapa = lineas.FindIndex(l => l.Trim() == "mapa:");
            var bloque = new List<string>
            {
                "solucion_recorrido: " + mejor.Recorrido.AMovimientos(),
                "solucion_costurero: " + string.Join(", ", costurero),
                "solucion_colocacion: " + mejor.ColocacionesTexto()
            };
            if (mejoras.Count > 0) bloque.Add("solucion_mejoras: " + string.Join(", ", mejoras));
            lineas.InsertRange(iMapa, bloque);
            File.WriteAllLines(ruta, lineas);
            Console.WriteLine("Solución de referencia escrita en " + ruta);
            return 0;
        }

        public static string Resumen(ResultadoOleada r) =>
            (r.Victoria ? "VICTORIA" : "DERROTA") + " | carrete " + r.VidaCarreteInicial + "→" + r.VidaCarreteFinal +
            " | derrotadas " + r.Derrotadas + " | escapadas " + r.Escapadas + " | turnos " + r.Turnos;

        /// <summary>Dibuja el tablero con la lana numerada (últimas cifras del índice) y las herramientas.</summary>
        public static string Dibujar(Recorrido r, List<Colocacion> colocaciones)
        {
            var t = r.Tablero;
            var sb = new StringBuilder();
            for (int y = t.Alto - 1; y >= 0; y--)
            {
                for (int x = 0; x < t.Ancho; x++)
                {
                    var c = new Celda(x, y);
                    int i = r.IndiceDe(c);
                    string celda;
                    if (t.Tipo(c) == TipoCasilla.Entrada) celda = " E ";
                    else if (t.Tipo(c) == TipoCasilla.Salida) celda = " S ";
                    else if (i < 0) celda = " " + t.Simbolo(c) + " ";
                    else
                    {
                        var aqui = colocaciones.Where(k => k.Indice == i).Select(k => Inicial(k.Tipo)).ToList();
                        string marca = aqui.Count == 0 ? (r.EsCurva(i) ? "+" : "~") : string.Concat(aqui);
                        celda = (i % 100).ToString("00") + marca;
                        if (celda.Length > 3) celda = celda.Substring(0, 3);
                    }
                    sb.Append(celda.PadRight(4));
                }
                sb.Append('\n');
            }
            sb.Append("Leyenda: A alfiler, V velcro, B bobina, T tijeras, O botón, + curva, ~ lana");
            return sb.ToString();
        }

        private static string Inicial(TipoHerramienta t)
        {
            switch (t)
            {
                case TipoHerramienta.Alfiler: return "A";
                case TipoHerramienta.Velcro: return "V";
                case TipoHerramienta.Bobina: return "B";
                case TipoHerramienta.Tijeras: return "T";
                default: return "O";
            }
        }

        // ---------------------------------------------------------------- Informe

        private sealed class Escenario
        {
            public string Sala;
            public string Nombre;
            public string Costurero;
            public string Mejoras = "";
        }

        private static readonly Escenario[] Escenarios =
        {
            new Escenario { Sala = "sala1", Nombre = "Tutorial", Costurero = "Alfiler, Velcro" },
            new Escenario { Sala = "sala2", Nombre = "Coge la bobina y un alfiler", Costurero = "Alfiler, Velcro, Bobina, Alfiler" },
            new Escenario { Sala = "sala2", Nombre = "Ignora la bobina", Costurero = "Alfiler, Velcro, Alfiler, Velcro" },
            new Escenario { Sala = "sala2", Nombre = "Bobina y velcro", Costurero = "Alfiler, Velcro, Bobina, Velcro" },
            new Escenario { Sala = "sala3", Nombre = "Tijeras y botón", Costurero = "Alfiler, Velcro, Bobina, Alfiler, Tijeras, Boton" },
            new Escenario { Sala = "sala3", Nombre = "Tijeras y alfiler", Costurero = "Alfiler, Velcro, Bobina, Alfiler, Tijeras, Alfiler" },
            new Escenario { Sala = "sala3", Nombre = "Botón y velcro + mejora", Costurero = "Alfiler, Velcro, Bobina, Alfiler, Boton, Velcro", Mejoras = "AlfileresAfilados" },
            new Escenario { Sala = "sala4", Nombre = "Costurero típico", Costurero = "Alfiler, Velcro, Bobina, Alfiler, Tijeras, Boton, Alfiler, Velcro" },
            new Escenario { Sala = "sala4", Nombre = "Típico + 2 mejoras", Costurero = "Alfiler, Velcro, Bobina, Alfiler, Tijeras, Boton, Alfiler, Velcro", Mejoras = "AlfileresAfilados, VelcroReforzado" },
            new Escenario { Sala = "sala4", Nombre = "Mucho control, poco daño", Costurero = "Alfiler, Velcro, Bobina, Velcro, Boton, Bobina, Alfiler, Boton" },
            new Escenario { Sala = "sala5", Nombre = "Costurero típico sin mejoras", Costurero = "Alfiler, Velcro, Bobina, Alfiler, Tijeras, Boton, Alfiler, Velcro, Tijeras, Alfiler" },
            new Escenario { Sala = "sala5", Nombre = "Típico + 3 mejoras", Costurero = "Alfiler, Velcro, Bobina, Alfiler, Tijeras, Boton, Alfiler, Velcro, Tijeras, Alfiler", Mejoras = "AlfileresAfilados, VelcroReforzado, AcericoLleno" },
            new Escenario { Sala = "sala5", Nombre = "Típico + 3 mejoras de control", Costurero = "Alfiler, Velcro, Bobina, Alfiler, Tijeras, Boton, Alfiler, Velcro, Tijeras, Alfiler", Mejoras = "OvilloGrande, BobinaPesada, MuelleTenso" },
            new Escenario { Sala = "sala5", Nombre = "Típico + 3 mejoras + 6 de reserva", Costurero = "Alfiler, Velcro, Bobina, Alfiler, Tijeras, Boton, Alfiler, Velcro, Tijeras, Alfiler", Mejoras = "AlfileresAfilados, VelcroReforzado, AcericoLleno" },
        };

        private static int Informe(Dictionary<string, string> o)
        {
            bool rapido = o.ContainsKey("rapido");
            var sb = new StringBuilder();
            sb.AppendLine("# Informe de balance (generado)");
            sb.AppendLine();
            sb.AppendLine("> Generado por `Tools/Laboratorio` (`dotnet run -c Release -- informe`). No editar a mano: se regenera.");
            sb.AppendLine("> **Mejor encontrado**: la mejor solución que encontró la búsqueda (recorridos al azar + optimización de colocaciones).");
            sb.AppendLine("> **Ingenuo**: recorrido válido al azar y herramientas puestas al azar. **Aplicado**: recorrido al azar, pero el mejor de 25 intentos de colocación.");
            sb.AppendLine("> Sirven para comparar salas entre sí, no como predicción exacta de jugadores reales: eso lo dirán las pruebas con personas.");
            sb.AppendLine();
            sb.AppendLine("| Sala | Escenario | Mejor encontrado | Hilo | Ingenuo: gana / perfecto / daño medio | Aplicado: gana / perfecto / daño medio |");
            sb.AppendLine("|---|---|---|---|---|---|");
            var detalles = new StringBuilder();
            foreach (var e in Escenarios)
            {
                var sala = CargarSala(e.Sala);
                var mejoras = Sala.ParsearMejoras(e.Mejoras);
                var reglas = Reglas.PorDefecto().ConMejoras(mejoras);
                int hiloExtra = e.Nombre.Contains("reserva") ? 6 : 0;
                var b = new Buscador(sala, reglas, Sala.ParsearHerramientas(e.Costurero), 7, hiloExtra);
                var mejor = rapido ? b.Buscar(150, 150, 6, 800) : b.Buscar(600, 250, 12, 2500);
                var ing = b.JugadorIngenuo(rapido ? 500 : 2000);
                var apl = b.JugadorIngenuo(rapido ? 150 : 600, 25);
                string mejorTxt = mejor == null ? "—" : (mejor.Resultado.Victoria ? "gana" : "pierde") + ", carrete " + mejor.Resultado.VidaCarreteFinal + "/" + mejor.Resultado.VidaCarreteInicial;
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "| {0} | {1} | {2} | {3} | {4:0}% / {5:0}% / {6:0.0} | {7:0}% / {8:0}% / {9:0.0} |",
                    sala.Nombre, e.Nombre + (e.Mejoras.Length > 0 ? " (" + e.Mejoras + ")" : ""), mejorTxt,
                    mejor == null ? "—" : mejor.Recorrido.HiloUsado + "/" + b.Hilo,
                    ing.PorcentajeVictorias, ing.PorcentajePerfectas, ing.DanioMedio,
                    apl.PorcentajeVictorias, apl.PorcentajePerfectas, apl.DanioMedio));
                if (mejor != null)
                {
                    detalles.AppendLine("### " + sala.Nombre + " — " + e.Nombre);
                    detalles.AppendLine();
                    detalles.AppendLine("Costurero: " + e.Costurero + (e.Mejoras.Length > 0 ? " · Mejoras: " + e.Mejoras : ""));
                    detalles.AppendLine();
                    detalles.AppendLine("```");
                    detalles.AppendLine(Dibujar(mejor.Recorrido, mejor.Colocaciones));
                    detalles.AppendLine("recorrido: " + mejor.Recorrido.AMovimientos());
                    detalles.AppendLine("colocación: " + mejor.ColocacionesTexto());
                    detalles.AppendLine(Resumen(mejor.Resultado));
                    detalles.AppendLine("```");
                    detalles.AppendLine();
                }
                Console.WriteLine("✓ " + e.Sala + " · " + e.Nombre);
            }
            sb.AppendLine();
            sb.AppendLine("## Soluciones encontradas");
            sb.AppendLine();
            sb.Append(detalles);
            var ruta = Path.Combine(RaizRepositorio(), "Docs", "Balance", "informe_balance.md");
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));
            File.WriteAllText(ruta, sb.ToString());
            Console.WriteLine("Informe escrito en " + ruta);
            return 0;
        }
    }
}
