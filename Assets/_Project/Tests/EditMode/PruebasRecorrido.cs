using System.Linq;
using DesenredaElOvillo.Nucleo;
using NUnit.Framework;

namespace DesenredaElOvillo.Pruebas
{
    public class PruebasRecorrido
    {
        private static readonly string[] Mapa =
        {
            ". . . . S",
            ". o . . .",
            "E . . . ."
        };

        [Test]
        public void El_mapa_se_lee_con_el_origen_abajo_a_la_izquierda()
        {
            var t = Tablero.DesdeTexto(Mapa);
            Assert.AreEqual(5, t.Ancho);
            Assert.AreEqual(3, t.Alto);
            Assert.AreEqual(new Celda(0, 0), t.Entrada);
            Assert.AreEqual(new Celda(4, 2), t.Salida);
            Assert.AreEqual(TipoCasilla.Obstaculo, t.Tipo(new Celda(1, 1)));
            Assert.AreEqual('o', t.Simbolo(new Celda(1, 1)));
        }

        [Test]
        public void Solo_avanza_a_casillas_adyacentes()
        {
            var r = new Recorrido(Tablero.DesdeTexto(Mapa));
            Assert.AreEqual(ResultadoPaso.NoAdyacente, r.IntentarAvanzar(new Celda(2, 0), 18));
            Assert.AreEqual(ResultadoPaso.NoAdyacente, r.IntentarAvanzar(new Celda(1, 1), 18), "Las diagonales no valen");
            Assert.AreEqual(ResultadoPaso.Avanzado, r.IntentarAvanzar(new Celda(1, 0), 18));
        }

        [Test]
        public void No_atraviesa_obstaculos()
        {
            var r = new Recorrido(Tablero.DesdeTexto(Mapa));
            r.IntentarAvanzar(new Celda(1, 0), 18);
            Assert.AreEqual(ResultadoPaso.Bloqueada, r.IntentarAvanzar(new Celda(1, 1), 18));
        }

        [Test]
        public void No_pasa_dos_veces_por_la_misma_casilla()
        {
            var t = Tablero.DesdeTexto(Mapa);
            // E→(1,0)→(2,0)→(2,1)→(3,1)→(3,0) y desde ahí intentar volver a (2,0), que no es la penúltima.
            var r2 = Recorrido.DesdeMovimientos(t, "DD");
            r2.IntentarAvanzar(new Celda(2, 1), 18);
            r2.IntentarAvanzar(new Celda(3, 1), 18);
            r2.IntentarAvanzar(new Celda(3, 0), 18);
            Assert.AreEqual(ResultadoPaso.Ocupada, r2.IntentarAvanzar(new Celda(2, 0), 18));
        }

        [Test]
        public void Arrastrar_hacia_atras_deshace_el_ultimo_tramo()
        {
            var t = Tablero.DesdeTexto(Mapa);
            var r = Recorrido.DesdeMovimientos(t, "DDD");
            Assert.AreEqual(3, r.HiloUsado);
            Assert.AreEqual(ResultadoPaso.Retrocedido, r.IntentarAvanzar(new Celda(2, 0), 18));
            Assert.AreEqual(2, r.HiloUsado);
        }

        [Test]
        public void El_hilo_limita_la_longitud()
        {
            var t = Tablero.DesdeTexto(Mapa);
            var r = new Recorrido(t);
            Assert.AreEqual(ResultadoPaso.Avanzado, r.IntentarAvanzar(new Celda(1, 0), 2));
            Assert.AreEqual(ResultadoPaso.Avanzado, r.IntentarAvanzar(new Celda(2, 0), 2));
            Assert.AreEqual(ResultadoPaso.SinHilo, r.IntentarAvanzar(new Celda(3, 0), 2));
        }

        [Test]
        public void La_entrada_y_el_carrete_no_gastan_hilo()
        {
            var t = Tablero.DesdeTexto(Mapa);
            var r = Recorrido.DesdeMovimientos(t, "DDDDAA");
            Assert.IsTrue(r.EstaCompleto);
            Assert.AreEqual(7, r.Cantidad);
            Assert.AreEqual(5, r.HiloUsado);
            Assert.AreEqual(6, r.IndiceSalida);
            Assert.AreEqual(ResultadoPaso.YaCompleto, r.IntentarAvanzar(new Celda(3, 2), 18));
        }

        [Test]
        public void El_validador_explica_por_que_no_se_puede_empezar()
        {
            var t = Tablero.DesdeTexto(Mapa);
            var incompleto = Recorrido.DesdeMovimientos(t, "DD");
            Assert.That(ValidadorRecorrido.Problemas(incompleto, 18, 0).Any(p => p.Contains("carrete")));

            var corto = Recorrido.DesdeMovimientos(t, "DDDDAA");
            var problemas = ValidadorRecorrido.Problemas(corto, 18, 10);
            Assert.AreEqual(1, problemas.Count);
            StringAssert.Contains("demasiado corto", problemas[0]);

            Assert.IsTrue(ValidadorRecorrido.EsValido(corto, 18, 5));
            Assert.IsFalse(ValidadorRecorrido.EsValido(corto, 4, 0), "Más hilo del disponible");
        }

        [Test]
        public void Detecta_las_curvas()
        {
            var t = Tablero.DesdeTexto(Mapa);
            var r = Recorrido.DesdeMovimientos(t, "DDDAAD");
            Assert.IsTrue(r.EstaCompleto);
            bool[] esperado = { false, false, false, true, false, true, false };
            for (int i = 0; i < r.Cantidad; i++) Assert.AreEqual(esperado[i], r.EsCurva(i), "Casilla " + i);
            Assert.AreEqual(2, r.ContarCurvas());
        }

        [Test]
        public void Los_movimientos_se_pueden_guardar_y_recuperar()
        {
            var t = Tablero.DesdeTexto(Mapa);
            var r = Recorrido.DesdeMovimientos(t, "AADDDD");
            Assert.IsNotNull(r);
            Assert.AreEqual("AADDDD", r.AMovimientos());
            var copia = Recorrido.DesdeCeldas(t, r.Celdas.Skip(1));
            Assert.AreEqual(r.AMovimientos(), copia.AMovimientos());
        }

        [Test]
        public void Recortar_deja_la_hebra_hasta_la_casilla_indicada()
        {
            var t = Tablero.DesdeTexto(Mapa);
            var r = Recorrido.DesdeMovimientos(t, "DDDDAA");
            r.RecortarHasta(2);
            Assert.AreEqual(3, r.Cantidad);
            Assert.IsFalse(r.EstaCompleto);
            r.Reiniciar();
            Assert.AreEqual(1, r.Cantidad);
            Assert.AreEqual(t.Entrada, r.Ultima);
        }
    }
}
