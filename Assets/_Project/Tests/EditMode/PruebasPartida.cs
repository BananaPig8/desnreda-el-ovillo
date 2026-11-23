using System.Collections.Generic;
using System.Linq;
using DesenredaElOvillo.Nucleo;
using NUnit.Framework;

namespace DesenredaElOvillo.Pruebas
{
    public class PruebasPartida
    {
        private const string SalaTutorial = @"
id: t1
nombre: Prueba tutorial
hilo: 18
longitud_minima: 4
oferta_fija: Alfiler, Velcro
oleada: Comun@1, Comun@2
mapa:
E . . . . . . S";

        private const string SalaDos = @"
id: t2
nombre: Prueba dos
hilo: 18
longitud_minima: 4
oferta_garantizada: Bobina
oleada: Comun@1
mapa:
E . . . . . . S";

        private const string SalaTres = @"
id: t3
nombre: Prueba tres
hilo: 10
longitud_minima: 4
oleada: Comun@1
mapa:
E . . . . . . S";

        private static Partida NuevaPartida(int semilla = 7)
        {
            var salas = new List<Sala> { Sala.DesdeTexto(SalaTutorial), Sala.DesdeTexto(SalaDos), Sala.DesdeTexto(SalaTres) };
            return new Partida(Reglas.PorDefecto(), salas, semilla);
        }

        private static (Recorrido, PlanColocacion) SolucionTutorial(Partida p)
        {
            var r = Recorrido.DesdeMovimientos(p.SalaActual.Tablero, "DDDDDDD");
            var plan = new PlanColocacion();
            Assert.IsTrue(plan.Colocar(r, TipoHerramienta.Alfiler, 2));
            Assert.IsTrue(plan.Colocar(r, TipoHerramienta.Velcro, 2), "El velcro comparte casilla con el alfiler");
            return (r, plan);
        }

        [Test]
        public void El_tutorial_entrega_sus_herramientas_sin_elegir()
        {
            var p = NuevaPartida();
            Assert.AreEqual(FasePartida.Preparacion, p.Fase);
            CollectionAssert.AreEqual(new[] { TipoHerramienta.Alfiler, TipoHerramienta.Velcro }, p.Costurero);
            Assert.AreEqual(18, p.HiloDisponible);
            Assert.AreEqual(4, p.VidaCarrete);
        }

        [Test]
        public void No_se_pueden_colocar_dos_herramientas_de_encima_en_la_misma_casilla()
        {
            var p = NuevaPartida();
            var (r, plan) = SolucionTutorial(p);
            Assert.IsFalse(plan.PuedeColocar(r, TipoHerramienta.Tijeras, 2));
            Assert.IsFalse(plan.PuedeColocar(r, TipoHerramienta.Velcro, 2));
            Assert.IsFalse(plan.PuedeColocar(r, TipoHerramienta.Alfiler, 0), "Ni en la entrada");
            Assert.IsFalse(plan.PuedeColocar(r, TipoHerramienta.Alfiler, r.IndiceSalida), "Ni en el carrete");
            Assert.AreEqual(TipoHerramienta.Alfiler, plan.QuitarEn(2));
            Assert.AreEqual(TipoHerramienta.Velcro, plan.QuitarEn(2));
            Assert.IsNull(plan.QuitarEn(2));
        }

        [Test]
        public void No_se_pueden_usar_herramientas_que_no_estan_en_el_costurero()
        {
            var p = NuevaPartida();
            var (r, plan) = SolucionTutorial(p);
            plan.Colocar(r, TipoHerramienta.Tijeras, 4);
            Assert.That(p.ProblemasParaEmpezar(r, plan).Any(x => x.Contains("Tijeras")));
        }

        [Test]
        public void Superar_una_sala_ofrece_mejoras_y_guarda_el_hilo_sobrante()
        {
            var p = NuevaPartida();
            var (r, plan) = SolucionTutorial(p);
            var res = p.JugarOleada(r, plan);
            Assert.IsTrue(res.Victoria);
            Assert.AreEqual(FasePartida.Recompensa, p.Fase);
            Assert.AreEqual(3, p.OfertaMejoras.Count);
            Assert.AreEqual(3, p.OfertaMejoras.Distinct().Count());
            Assert.IsFalse(p.OfertaMejoras.Contains(TipoMejora.Remiendo), "Con el carrete entero no se ofrece remiendo");
            Assert.AreEqual(6, p.HiloReserva, "Sobraron 12; se guardan como máximo 6");
            Assert.AreEqual(2, p.Estadisticas.PolillasDerrotadas);
            Assert.AreEqual(6, p.Estadisticas.HiloUsado);
        }

        [Test]
        public void La_oferta_de_herramientas_depende_solo_de_la_semilla()
        {
            List<TipoHerramienta> Oferta(int semilla)
            {
                var p = NuevaPartida(semilla);
                var (r, plan) = SolucionTutorial(p);
                p.JugarOleada(r, plan);
                p.ElegirMejora(p.OfertaMejoras[0]);
                Assert.AreEqual(FasePartida.EleccionHerramientas, p.Fase);
                return p.OfertaHerramientas;
            }

            var a = Oferta(42);
            var b = Oferta(42);
            CollectionAssert.AreEqual(a, b);
            Assert.AreEqual(3, a.Count);
            Assert.Contains(TipoHerramienta.Bobina, a, "La herramienta nueva de la sala siempre se ofrece");
            Assert.IsTrue(a.All(t => t == TipoHerramienta.Alfiler || t == TipoHerramienta.Velcro || t == TipoHerramienta.Bobina),
                "Solo se ofrecen herramientas ya presentadas");
        }

        [Test]
        public void El_hilo_de_reserva_se_suma_a_la_sala_siguiente()
        {
            var p = NuevaPartida();
            var (r, plan) = SolucionTutorial(p);
            p.JugarOleada(r, plan);
            p.ElegirMejora(p.OfertaMejoras.First(m => m != TipoMejora.OvilloGrande));
            p.ElegirHerramientas(p.OfertaHerramientas.Take(2).ToList());
            Assert.AreEqual(FasePartida.Preparacion, p.Fase);
            Assert.AreEqual(18 + 6, p.HiloDisponible);
            Assert.AreEqual(4, p.Costurero.Count);
        }

        [Test]
        public void Las_mejoras_cambian_las_reglas_de_la_partida()
        {
            var reglas = Reglas.PorDefecto().ConMejoras(new[] { TipoMejora.AlfileresAfilados, TipoMejora.AcericoLleno, TipoMejora.MuelleTenso, TipoMejora.OvilloGrande });
            Assert.AreEqual(3, reglas.Herramienta(TipoHerramienta.Alfiler).Danio);
            Assert.AreEqual(3, reglas.Herramienta(TipoHerramienta.Alfiler).Usos);
            Assert.AreEqual(3, reglas.Herramienta(TipoHerramienta.Boton).Distancia);
            Assert.AreEqual(3, reglas.HiloExtraPorSala);
            Assert.AreEqual(2, Reglas.PorDefecto().Herramienta(TipoHerramienta.Alfiler).Danio, "Las reglas base no se tocan");
        }

        [Test]
        public void Tras_una_derrota_se_puede_reintentar_la_sala_tal_como_empezo()
        {
            var p = NuevaPartida();
            var r = Recorrido.DesdeMovimientos(p.SalaActual.Tablero, "DDDDDDD");
            var res = p.JugarOleada(r, new PlanColocacion());
            Assert.IsTrue(res.Victoria, "Dos polillas se escapan pero el carrete aguanta");
            Assert.AreEqual(2, p.VidaCarrete);

            // Forzamos una derrota en una partida nueva con un carrete débil.
            var reglas = Reglas.PorDefecto();
            reglas.VidaCarrete = 1;
            var salas = new List<Sala> { Sala.DesdeTexto(SalaTutorial) };
            var debil = new Partida(reglas, salas, 1);
            var r2 = Recorrido.DesdeMovimientos(debil.SalaActual.Tablero, "DDDDDDD");
            debil.JugarOleada(r2, new PlanColocacion());
            Assert.AreEqual(FasePartida.Derrota, debil.Fase);
            Assert.IsTrue(debil.ReintentarSala());
            Assert.AreEqual(FasePartida.Preparacion, debil.Fase);
            Assert.AreEqual(1, debil.VidaCarrete);
            Assert.AreEqual(1, debil.Estadisticas.Reintentos);
            CollectionAssert.AreEqual(new[] { TipoHerramienta.Alfiler, TipoHerramienta.Velcro }, debil.Costurero);
        }

        [Test]
        public void Superar_la_ultima_sala_es_la_victoria_de_la_partida()
        {
            var salas = new List<Sala> { Sala.DesdeTexto(SalaTutorial) };
            var p = new Partida(Reglas.PorDefecto(), salas, 3);
            var (r, plan) = SolucionTutorial(p);
            p.JugarOleada(r, plan);
            Assert.AreEqual(FasePartida.Victoria, p.Fase);
        }
    }
}
