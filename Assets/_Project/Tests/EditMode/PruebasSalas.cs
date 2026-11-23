using System.Collections.Generic;
using System.Linq;
using DesenredaElOvillo.Nucleo;
using NUnit.Framework;

namespace DesenredaElOvillo.Pruebas
{
    /// <summary>
    /// Red de seguridad del balance: si alguien cambia un número en Reglas o un mapa y rompe una sala,
    /// estas pruebas fallan. Cada sala guarda una solución de referencia que debe ganar sin daño.
    /// </summary>
    public class PruebasSalas
    {
        public static readonly string[] Ids = { "sala1", "sala2", "sala3", "sala4", "sala5" };

        /// <summary>Herramientas que puede tener el jugador al empezar cada sala (2 nuevas por sala).</summary>
        private static readonly int[] TamanoCosturero = { 2, 4, 6, 8, 10 };

        [Test]
        public void Las_cinco_salas_de_la_demo_se_cargan()
        {
            var salas = Ayudas.CargarDemo();
            Assert.AreEqual(5, salas.Count);
            foreach (var s in salas)
            {
                Assert.IsNotEmpty(s.Nombre, s.Id);
                Assert.Greater(s.Oleada.Apariciones.Count, 0, s.Id);
                Assert.GreaterOrEqual(s.Hilo, s.LongitudMinima, s.Id);
            }
            Assert.AreEqual(1, salas[4].Oleada.Contar(TipoPolilla.Reina), "La sala 5 es la de la Reina");
        }

        [TestCaseSource(nameof(Ids))]
        public void La_solucion_de_referencia_supera_la_sala_sin_dano(string id)
        {
            var sala = Ayudas.CargarSala(id);
            Assert.IsNotNull(sala.Solucion, "La sala necesita una solución de referencia");
            var reglas = Reglas.PorDefecto().ConMejoras(sala.Solucion.Mejoras);
            int hilo = sala.Hilo + reglas.HiloExtraPorSala;

            var r = Recorrido.DesdeMovimientos(sala.Tablero, sala.Solucion.Movimientos, hilo);
            Assert.IsNotNull(r, "El recorrido de referencia no es válido en el mapa");
            CollectionAssert.IsEmpty(ValidadorRecorrido.Problemas(r, hilo, sala.LongitudMinima));

            var plan = new PlanColocacion();
            foreach (var c in sala.Solucion.Colocaciones)
                Assert.IsTrue(plan.Colocar(r, c.Tipo, c.Indice), "No se puede colocar " + c);
            CollectionAssert.IsEmpty(plan.Problemas(r, sala.Solucion.Costurero));

            int vida = sala.Solucion.VidaCarrete > 0 ? sala.Solucion.VidaCarrete : reglas.VidaCarrete;
            var res = SimuladorOleada.Simular(reglas, r, plan, sala.Oleada, vida);
            Assert.IsTrue(res.Victoria);
            Assert.AreEqual(vida, res.VidaCarreteFinal, "La solución de referencia no deja morder el carrete");
        }

        [TestCaseSource(nameof(Ids))]
        public void La_solucion_solo_usa_lo_que_un_jugador_puede_tener(string id)
        {
            var salas = Ayudas.CargarDemo();
            int indice = System.Array.IndexOf(Ids, id);
            var sala = salas[indice];
            var desbloqueadas = new HashSet<TipoHerramienta>();
            for (int i = 0; i <= indice; i++)
            {
                foreach (var t in salas[i].OfertaFija) desbloqueadas.Add(t);
                foreach (var t in salas[i].OfertaGarantizada) desbloqueadas.Add(t);
            }
            Assert.LessOrEqual(sala.Solucion.Costurero.Count, TamanoCosturero[indice]);
            foreach (var t in sala.Solucion.Costurero)
                Assert.IsTrue(desbloqueadas.Contains(t), t + " todavía no se ha presentado en la sala " + (indice + 1));
            Assert.LessOrEqual(sala.Solucion.Mejoras.Count, indice, "Solo hay una mejora por sala superada");
        }

        [Test]
        public void Todas_las_salas_ofrecen_una_eleccion_salvo_el_tutorial()
        {
            var salas = Ayudas.CargarDemo();
            Assert.That(salas[0].OfertaFija, Is.Not.Empty);
            for (int i = 1; i < salas.Count; i++)
            {
                Assert.That(salas[i].OfertaFija, Is.Empty, salas[i].Id);
                Assert.AreEqual(3, salas[i].CantidadOfrecida, salas[i].Id);
                Assert.AreEqual(2, salas[i].CantidadAElegir, salas[i].Id);
            }
        }
    }
}
