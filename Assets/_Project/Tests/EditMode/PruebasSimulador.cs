using System.Linq;
using DesenredaElOvillo.Nucleo;
using NUnit.Framework;

namespace DesenredaElOvillo.Pruebas
{
    /// <summary>
    /// Cada prueba reproduce una regla del documento de diseño (Docs/02_Reglas_del_juego.md)
    /// con un escenario calculado a mano. En la recta, la entrada es la casilla 0 y el carrete la 7.
    /// </summary>
    public class PruebasSimulador
    {
        [Test]
        public void El_alfiler_pincha_al_pasar_y_gasta_un_uso()
        {
            var r = Ayudas.RecorridoRecto(out _);
            var sim = Ayudas.Simulador(r, "Comun@1", Ayudas.PlanLibre("Alfiler@2"));
            var res = Ayudas.Completar(sim);
            Assert.IsTrue(res.Victoria, "Sobrevive aunque la polilla escape");
            Assert.AreEqual(3, res.VidaCarreteFinal);
            Assert.AreEqual(0, res.Derrotadas);
            Assert.AreEqual(1, res.Escapadas);
            Assert.AreEqual(1, sim.HerramientaEncima(2).Usos);
            Assert.AreEqual(8, res.Turnos, "Aparece en el turno 1 y avanza una casilla por turno desde el 2");
        }

        [Test]
        public void Velcro_debajo_del_alfiler_pincha_dos_veces_a_la_misma_polilla()
        {
            var r = Ayudas.RecorridoRecto(out _);
            var sim = Ayudas.Simulador(r, "Comun@1", Ayudas.PlanLibre("Alfiler@2, Velcro@2"));
            Ayudas.EjecutarHasta(sim, 3);
            var polilla = sim.Polillas[0];
            Assert.AreEqual(2, polilla.Indice);
            Assert.AreEqual(2, polilla.Vida);
            Assert.AreEqual(1, polilla.TurnosRetenida);
            Ayudas.EjecutarHasta(sim, 4);
            Assert.IsFalse(polilla.Viva, "Quieta encima del alfiler se vuelve a pinchar");
            var res = Ayudas.Completar(sim);
            Assert.AreEqual(4, res.VidaCarreteFinal);
            Assert.AreEqual(1, res.Derrotadas);
        }

        [Test]
        public void El_combo_del_tutorial_derrota_a_dos_polillas_comunes()
        {
            var r = Ayudas.RecorridoRecto(out _);
            var sim = Ayudas.Simulador(r, "Comun@1, Comun@2", Ayudas.PlanLibre("Alfiler@2, Velcro@2"));
            var res = Ayudas.Completar(sim);
            Assert.IsTrue(res.Victoria);
            Assert.AreEqual(2, res.Derrotadas);
            Assert.AreEqual(4, res.VidaCarreteFinal);
            Assert.AreEqual(0, sim.HerramientaEncima(2).Usos);
            Assert.AreEqual(0, sim.HerramientaDebajo(2).Usos);
        }

        [Test]
        public void Quedarse_quieta_encima_no_gasta_usos_y_el_alfiler_doblado_sigue_pinchando()
        {
            var r = Ayudas.RecorridoRecto(out _);
            var sim = Ayudas.Simulador(r, "Comun@1, Comun@3", Ayudas.PlanLibre("Alfiler@2, Velcro@2"));
            var res = Ayudas.Completar(sim);
            Assert.AreEqual(2, res.Derrotadas);
            Assert.AreEqual(0, sim.HerramientaEncima(2).Usos, "Dos entradas: los dos usos");
            Assert.AreEqual(2, Ayudas.Contar(res, TipoEvento.PinchazoQuieta), "Un pinchazo gratis por polilla pegada");
            var doblado = res.Eventos.Single(e => e.Tipo == TipoEvento.HerramientaGastada && e.Herramienta == TipoHerramienta.Alfiler);
            Assert.AreEqual(1, doblado.PolillaId, "Lo dobla la segunda polilla, y aun así la pincha quieta");
        }

        [Test]
        public void Una_polilla_atrapada_vuelve_a_sufrir_la_herramienta_sin_gastar_usos()
        {
            var reglas = Reglas.PorDefecto();
            reglas.Polilla(TipoPolilla.Comun).Vida = 12;
            var r = Ayudas.RecorridoRecto(out _);
            var sim = Ayudas.Simulador(r, "Comun@1", Ayudas.PlanLibre("Tijeras@3, Boton@4"), reglas);
            Ayudas.EjecutarHasta(sim, 6);
            Assert.AreEqual(2, sim.Polillas[0].Vida, "Dos cortes: al pasar y al volver a pasar tras el botón");
            Assert.AreEqual(0, sim.HerramientaEncima(3).Usos);
            var res = Ayudas.Completar(sim);
            Assert.AreEqual(1, Ayudas.Contar(res, TipoEvento.Empujada));
        }

        [Test]
        public void Una_herramienta_sin_usos_no_atrapa_polillas_nuevas()
        {
            var r = Ayudas.RecorridoRecto(out _);
            var sim = Ayudas.Simulador(r, "Comun@1, Comun@2, Comun@3", Ayudas.PlanLibre("Alfiler@2"));
            var res = Ayudas.Completar(sim);
            Assert.AreEqual(2, Ayudas.Contar(res, TipoEvento.Danio), "Solo pincha a las dos primeras");
            Assert.AreEqual(1, res.VidaCarreteFinal);
        }

        [Test]
        public void Repartir_el_dano_no_derrota_a_nadie()
        {
            // Sin velcro el mismo alfiler solo hace 2 a cada una: ninguna cae. Concentrar el daño es la lección.
            var r = Ayudas.RecorridoRecto(out _);
            var res = Ayudas.Completar(Ayudas.Simulador(r, "Comun@1, Comun@2", Ayudas.PlanLibre("Alfiler@2, Velcro@5")));
            Assert.AreEqual(0, res.Derrotadas);
            Assert.AreEqual(2, res.VidaCarreteFinal);
        }

        [Test]
        public void Las_tijeras_cortan_una_sola_vez()
        {
            var r = Ayudas.RecorridoRecto(out _);
            var sim = Ayudas.Simulador(r, "Comun@1, Comun@3", Ayudas.PlanLibre("Tijeras@3"));
            var res = Ayudas.Completar(sim);
            Assert.AreEqual(1, res.Derrotadas);
            Assert.AreEqual(1, res.Escapadas);
            Assert.AreEqual(1, Ayudas.Contar(res, TipoEvento.HerramientaGastada));
            Assert.AreEqual(1, Ayudas.Contar(res, TipoEvento.Muere, 0), "Muere encima: el uso queda gastado");
        }

        [Test]
        public void El_boton_lanza_hacia_atras_y_al_volver_se_reactivan_las_trampas()
        {
            var r = Ayudas.RecorridoRecto(out _);
            var sim = Ayudas.Simulador(r, "Comun@1", Ayudas.PlanLibre("Alfiler@3, Boton@4"));
            Ayudas.EjecutarHasta(sim, 5);
            var p = sim.Polillas[0];
            Assert.AreEqual(2, p.Indice, "Lanzada 2 casillas hacia atrás");
            Assert.AreEqual(2, p.Vida);
            Ayudas.EjecutarHasta(sim, 6);
            Assert.IsFalse(p.Viva, "Al volver a pasar por el alfiler, se pincha otra vez");
        }

        [Test]
        public void Al_caer_tras_el_boton_no_se_activa_nada()
        {
            var r = Ayudas.RecorridoRecto(out _);
            var sim = Ayudas.Simulador(r, "Comun@1", Ayudas.PlanLibre("Alfiler@2, Boton@4"));
            var res = Ayudas.Completar(sim);
            Assert.AreEqual(1, sim.HerramientaEncima(2).Usos, "Solo se pinchó al pasar la primera vez");
            Assert.AreEqual(1, Ayudas.Contar(res, TipoEvento.Empujada), "El botón lanza una sola vez a cada polilla");
            Assert.AreEqual(1, sim.HerramientaEncima(4).Usos);
            Assert.AreEqual(3, res.VidaCarreteFinal);
        }

        [Test]
        public void La_bobina_hace_ir_a_media_velocidad()
        {
            var r = Ayudas.RecorridoRecto(out _);
            var sim = Ayudas.Simulador(r, "Comun@1", Ayudas.PlanLibre("Bobina@2"));
            int[] posiciones = new int[11];
            for (int t = 1; t <= 10 && !sim.Terminada; t++)
            {
                sim.EjecutarTurno();
                posiciones[t] = sim.Polillas[0].Indice;
            }
            // T3 entra en la bobina; T4 quieta; T5 avanza; T6 quieta; T7 avanza y se desenreda.
            Assert.AreEqual(2, posiciones[3]);
            Assert.AreEqual(2, posiciones[4]);
            Assert.AreEqual(3, posiciones[5]);
            Assert.AreEqual(3, posiciones[6]);
            Assert.AreEqual(4, posiciones[7]);
            Assert.AreEqual(5, posiciones[8]);
            var res = Ayudas.Completar(sim);
            Assert.AreEqual(10, res.Turnos, "Pierde dos turnos en total");
            Assert.AreEqual(1, Ayudas.Contar(res, TipoEvento.SeDesenreda));
        }

        [Test]
        public void Bobina_antes_de_un_alfiler_lo_convierte_en_doble_pinchazo()
        {
            var r = Ayudas.RecorridoRecto(out _);
            var res = Ayudas.Completar(Ayudas.Simulador(r, "Comun@1", Ayudas.PlanLibre("Bobina@2, Alfiler@3")));
            Assert.AreEqual(1, res.Derrotadas);
            Assert.AreEqual(4, res.VidaCarreteFinal);
        }

        [Test]
        public void La_veloz_sobrevuela_las_casillas_rectas_y_no_se_salta_el_carrete()
        {
            var r = Ayudas.RecorridoRecto(out _);
            var sim = Ayudas.Simulador(r, "Veloz@1", Ayudas.PlanLibre("Alfiler@3"));
            var res = Ayudas.Completar(sim);
            // Aterriza en 2, 4 y 6; la 3 la sobrevuela. Desde la 6 entra en el carrete (7) en el primer paso.
            Assert.AreEqual(2, sim.HerramientaEncima(3).Usos);
            Assert.AreEqual(3, res.VidaCarreteFinal);
            Assert.AreEqual(5, res.Turnos);
            var aterrizajes = res.Eventos.Where(e => e.Tipo == TipoEvento.Mueve && e.PolillaId == 0).Select(e => e.Hasta).ToArray();
            CollectionAssert.AreEqual(new[] { 2, 4, 6, 7 }, aterrizajes);
            CollectionAssert.AreEqual(new[] { 2, 4, 6 }, SimuladorOleada.PrevisualizarAterrizajes(r, Reglas.PorDefecto()));
        }

        [Test]
        public void La_veloz_tiene_que_aterrizar_en_las_curvas()
        {
            var t = Tablero.DesdeTexto(new[] { ". . . . S", ". . . . .", "E . . . ." });
            var r = Recorrido.DesdeMovimientos(t, "DDDAAD");
            var sim = Ayudas.Simulador(r, "Veloz@1", Ayudas.PlanLibre("Alfiler@3, Alfiler@4"));
            var res = Ayudas.Completar(sim);
            Assert.AreEqual(1, sim.HerramientaEncima(3).Usos, "La curva 3 la obliga a aterrizar encima del alfiler");
            Assert.AreEqual(2, sim.HerramientaEncima(4).Usos, "La casilla 4 la sobrevuela");
            Assert.AreEqual(3, res.VidaCarreteFinal);
            CollectionAssert.AreEqual(new[] { 2, 3, 5 }, SimuladorOleada.PrevisualizarAterrizajes(r, Reglas.PorDefecto()));
        }

        [Test]
        public void La_bobina_atrapa_a_la_veloz_en_pleno_vuelo()
        {
            var r = Ayudas.RecorridoRecto(out _);
            var sim = Ayudas.Simulador(r, "Veloz@1", Ayudas.PlanLibre("Bobina@3, Alfiler@4, Alfiler@5"));
            var res = Ayudas.Completar(sim);
            Assert.AreEqual(1, res.Derrotadas, "Enredada ya no vuela y pisa los dos alfileres");
            Assert.AreEqual(4, res.VidaCarreteFinal);
            Assert.AreEqual(1, sim.HerramientaEncima(3).Usos);
        }

        [Test]
        public void La_acorazada_anula_el_primer_control_con_su_placa()
        {
            var r = Ayudas.RecorridoRecto(out _);
            var sim = Ayudas.Simulador(r, "Acorazada@1", Ayudas.PlanLibre("Velcro@2, Boton@4"));
            Ayudas.EjecutarHasta(sim, 4);
            var p = sim.Polillas[0];
            Assert.AreEqual(3, p.Indice, "El velcro se gasta pero no la retiene");
            Assert.AreEqual(0, p.Placas);
            Assert.AreEqual(1, sim.HerramientaDebajo(2).Usos);
            Ayudas.EjecutarHasta(sim, 5);
            Assert.AreEqual(2, p.Indice, "El segundo control sí funciona");
            var res = Ayudas.Completar(sim);
            Assert.AreEqual(1, Ayudas.Contar(res, TipoEvento.PlacaRota));
            Assert.AreEqual(2, res.VidaCarreteFinal, "La acorazada quita 2 al carrete");
        }

        [Test]
        public void La_reina_se_come_la_primera_casilla_con_herramientas()
        {
            var r = Ayudas.RecorridoRecto(out _);
            var sim = Ayudas.Simulador(r, "Reina@1", Ayudas.PlanLibre("Alfiler@2, Velcro@2, Alfiler@3"));
            Ayudas.EjecutarHasta(sim, 4);
            var reina = sim.Polillas[0];
            Assert.IsTrue(reina.HaComido);
            Assert.IsTrue(sim.HerramientaEncima(2).Destruida);
            Assert.IsTrue(sim.HerramientaDebajo(2).Destruida);
            Assert.AreEqual(reina.Def.Vida - 2, reina.Vida, "Solo le hace daño el alfiler de la casilla 3");
        }

        [Test]
        public void La_reina_llama_a_su_corte_al_bajar_de_cada_umbral()
        {
            var reglas = Reglas.PorDefecto();
            var reina = reglas.Polilla(TipoPolilla.Reina);
            reina.Vida = 8;
            reina.UmbralesInvocacion = new[] { 6, 2 };
            var r = Ayudas.RecorridoRecto(out _);
            var sim = Ayudas.Simulador(r, "Reina@1", Ayudas.PlanLibre("Velcro@2, Alfiler@3, Alfiler@4, Alfiler@5"), reglas);
            Ayudas.EjecutarHasta(sim, 4);
            Assert.AreEqual(6, sim.Polillas[0].Vida);
            Assert.AreEqual(1, sim.Polillas.Count, "La llamada se resuelve al principio del turno siguiente");
            Ayudas.EjecutarHasta(sim, 5);
            Assert.AreEqual(2, sim.Polillas.Count);
            Assert.IsTrue(sim.Polillas[1].Invocada);
            Assert.AreEqual(0, sim.Polillas[1].Indice);
            var res = Ayudas.Completar(sim);
            Assert.AreEqual(2, Ayudas.Contar(res, TipoEvento.LlamaAyuda));
        }

        [Test]
        public void La_reina_muerde_el_carrete_y_da_otra_vuelta()
        {
            var reglas = Reglas.PorDefecto();
            reglas.Polilla(TipoPolilla.Reina).Vida = 6;
            reglas.Polilla(TipoPolilla.Reina).UmbralesInvocacion = new int[0];
            var r = Ayudas.RecorridoRecto(out _);
            var sim = Ayudas.Simulador(r, "Reina@1", Ayudas.PlanLibre("Velcro@2, Alfiler@3, Alfiler@4"), reglas);
            var res = Ayudas.Completar(sim);
            Assert.IsTrue(res.Victoria);
            Assert.AreEqual(1, Ayudas.Contar(res, TipoEvento.DaLaVuelta));
            Assert.AreEqual(2, res.VidaCarreteFinal);
            Assert.AreEqual(1, res.Derrotadas);
            Assert.AreEqual(0, res.Escapadas);
        }

        [Test]
        public void Si_el_carrete_llega_a_cero_se_pierde()
        {
            var r = Ayudas.RecorridoRecto(out _);
            var res = Ayudas.Completar(Ayudas.Simulador(r, "Comun x5 cada 1 desde 1", new PlanColocacion()));
            Assert.IsFalse(res.Victoria);
            Assert.AreEqual(0, res.VidaCarreteFinal);
            Assert.AreEqual(4, res.Escapadas, "La quinta ya no llega a morder");
            Assert.AreEqual(1, Ayudas.Contar(res, TipoEvento.Derrota));
        }

        [Test]
        public void El_dedal_amortigua_el_primer_mordisco()
        {
            var reglas = Reglas.PorDefecto();
            reglas.DedalActivo = true;
            var r = Ayudas.RecorridoRecto(out _);
            var res = Ayudas.Completar(Ayudas.Simulador(r, "Comun@1, Comun@2", new PlanColocacion(), reglas));
            Assert.AreEqual(3, res.VidaCarreteFinal);
            Assert.AreEqual(1, Ayudas.Contar(res, TipoEvento.DedalProtege));
        }

        [Test]
        public void La_lampara_deslumbra_una_vez_y_hace_aterrizar_a_la_veloz()
        {
            var t = Tablero.DesdeTexto(new[] { ". L . . . .", "E . . . . S" });
            var r = Recorrido.DesdeMovimientos(t, "DDDDD");
            var sim = Ayudas.Simulador(r, "Veloz@1, Comun@1", Ayudas.PlanLibre("Alfiler@1"));
            var res = Ayudas.Completar(sim);
            Assert.AreEqual(2, Ayudas.Contar(res, TipoEvento.Deslumbrada), "Una vez a cada polilla");
            Assert.AreEqual(0, res.Escapadas, "Deslumbradas encima del alfiler se pinchan dos veces");
            Assert.AreEqual(2, res.Derrotadas);
            Assert.AreEqual(4, res.VidaCarreteFinal);
        }

        [Test]
        public void Actua_primero_la_polilla_mas_avanzada()
        {
            var reglas = Reglas.PorDefecto();
            reglas.Herramienta(TipoHerramienta.Alfiler).Usos = 1;
            var r = Ayudas.RecorridoRecto(out _);
            // La común llega a la casilla 2 en el turno 3; la veloz aterriza también en la 2 en ese turno.
            var sim = Ayudas.Simulador(r, "Comun@1, Veloz@2", Ayudas.PlanLibre("Alfiler@2"), reglas);
            Ayudas.EjecutarHasta(sim, 3);
            Assert.AreEqual(2, sim.Polillas[0].Indice);
            Assert.AreEqual(2, sim.Polillas[1].Indice);
            Assert.AreEqual(2, sim.Polillas[0].Vida, "La común, más avanzada, actúa antes y gasta el único uso");
            Assert.AreEqual(3, sim.Polillas[1].Vida);
        }

        [Test]
        public void Varias_polillas_en_la_misma_trampa_gastan_un_uso_cada_una()
        {
            var r = Ayudas.RecorridoRecto(out _);
            var sim = Ayudas.Simulador(r, "Comun@1, Veloz@2", Ayudas.PlanLibre("Alfiler@2"));
            Ayudas.EjecutarHasta(sim, 3);
            Assert.AreEqual(0, sim.HerramientaEncima(2).Usos);
            Assert.AreEqual(2, sim.Polillas[0].Vida);
            Assert.AreEqual(1, sim.Polillas[1].Vida);
        }

        [Test]
        public void Mismos_datos_mismos_eventos()
        {
            var r = Ayudas.RecorridoRecto(out _);
            const string oleada = "Comun@1, Veloz@2, Acorazada@3, Comun@4";
            var a = Ayudas.Completar(Ayudas.Simulador(r, oleada, Ayudas.PlanLibre("Bobina@1, Alfiler@2, Velcro@2, Boton@4, Tijeras@5")));
            var b = Ayudas.Completar(Ayudas.Simulador(r, oleada, Ayudas.PlanLibre("Bobina@1, Alfiler@2, Velcro@2, Boton@4, Tijeras@5")));
            CollectionAssert.AreEqual(a.Eventos.Select(e => e.ToString()).ToList(), b.Eventos.Select(e => e.ToString()).ToList());
            Assert.Greater(a.Eventos.Count, 20);
        }
    }
}
