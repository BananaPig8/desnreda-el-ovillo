using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DesenredaElOvillo.Nucleo;
using NUnit.Framework;

namespace DesenredaElOvillo.Pruebas
{
    /// <summary>Utilidades compartidas por las pruebas. Funcionan igual en el Test Runner de Unity y con "dotnet test".</summary>
    public static class Ayudas
    {
        /// <summary>Tablero en línea recta de 8 casillas: E(0) . . . . . . S(7).</summary>
        public static readonly string[] Recta = { "E . . . . . . S" };

        public static Recorrido RecorridoRecto(out Tablero tablero)
        {
            tablero = Tablero.DesdeTexto(Recta);
            var r = Recorrido.DesdeMovimientos(tablero, "DDDDDDD");
            Assert.IsNotNull(r);
            Assert.IsTrue(r.EstaCompleto);
            return r;
        }

        /// <summary>Construye el plan sin validar contra un recorrido (las pruebas controlan los índices).</summary>
        public static PlanColocacion PlanLibre(params string[] colocaciones)
        {
            var lista = new List<Colocacion>();
            foreach (var c in colocaciones) lista.AddRange(Sala.ParsearColocaciones(c));
            return new PlanColocacion(lista);
        }

        public static SimuladorOleada Simulador(Recorrido r, string oleada, PlanColocacion plan, Reglas reglas = null, int vida = 4)
        {
            return new SimuladorOleada(reglas ?? Reglas.PorDefecto(), r, plan, Oleada.DesdeTexto(oleada), vida, true);
        }

        public static void EjecutarHasta(SimuladorOleada sim, int turno)
        {
            while (!sim.Terminada && sim.Turno < turno) sim.EjecutarTurno();
        }

        public static ResultadoOleada Completar(SimuladorOleada sim)
        {
            while (!sim.Terminada) sim.EjecutarTurno();
            return sim.Resultado();
        }

        public static int Contar(ResultadoOleada res, TipoEvento tipo, int polillaId = -1)
            => res.Eventos.Count(e => e.Tipo == tipo && (polillaId < 0 || e.PolillaId == polillaId));

        public static string CarpetaSalas()
        {
#if UNITY_5_3_OR_NEWER
            return Path.Combine(UnityEngine.Application.dataPath, "_Project", "Resources", "Salas");
#else
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidata = Path.Combine(dir.FullName, "Assets", "_Project", "Resources", "Salas");
                if (Directory.Exists(candidata)) return candidata;
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException("No se encuentra Assets/_Project/Resources/Salas desde " + AppContext.BaseDirectory);
#endif
        }

        public static Sala CargarSala(string id)
        {
            var ruta = Path.Combine(CarpetaSalas(), id + ".txt");
            return Sala.DesdeTexto(File.ReadAllText(ruta));
        }

        public static IReadOnlyList<Sala> CargarDemo()
        {
            return new[] { "sala1", "sala2", "sala3", "sala4", "sala5" }.Select(CargarSala).ToList();
        }
    }
}
