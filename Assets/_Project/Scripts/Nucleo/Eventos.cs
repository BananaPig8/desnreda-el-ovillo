namespace DesenredaElOvillo.Nucleo
{
    public enum TipoEvento
    {
        InicioTurno,
        /// <summary>Una polilla entra por el agujero del rodapié.</summary>
        Aparece,
        /// <summary>Una polilla de apoyo llamada por la Reina entra por el agujero.</summary>
        Invocada,
        /// <summary>Avanza caminando o aterriza en la casilla Hasta.</summary>
        Mueve,
        /// <summary>Pasa volando por encima de la casilla Hasta sin tocarla.</summary>
        Sobrevuela,
        /// <summary>Se activa una herramienta. Valor = usos que le quedan.</summary>
        ActivaHerramienta,
        /// <summary>Un alfiler pincha a una polilla que sigue quieta encima (no gasta usos).</summary>
        PinchazoQuieta,
        HerramientaGastada,
        /// <summary>Valor = daño recibido; Extra = vida restante.</summary>
        Danio,
        Muere,
        /// <summary>El velcro la pega. Valor = turnos.</summary>
        Retenida,
        /// <summary>Pierde el turno (pegada o deslumbrada).</summary>
        SigueQuieta,
        /// <summary>La bobina la enreda. Valor = turnos.</summary>
        Enredada,
        /// <summary>Enredada y sin avanzar este turno.</summary>
        QuietaEnredada,
        SeDesenreda,
        /// <summary>El botón la lanza hacia atrás de Desde a Hasta.</summary>
        Empujada,
        /// <summary>Su placa de fieltro anula un efecto de control.</summary>
        PlacaRota,
        Deslumbrada,
        /// <summary>La Reina se come la herramienta de la casilla IndiceHerramienta.</summary>
        Devora,
        /// <summary>La Reina llama a una polilla de apoyo. Valor = umbral de vida.</summary>
        LlamaAyuda,
        LlegaAlCarrete,
        DedalProtege,
        /// <summary>Valor = daño; Extra = vida restante del carrete.</summary>
        CarreteDaniado,
        /// <summary>La Reina muerde el carrete y vuelve a empezar el recorrido.</summary>
        DaLaVuelta,
        Victoria,
        Derrota
    }

    /// <summary>
    /// Todo lo que ocurre durante una oleada se describe con eventos. La simulación es instantánea;
    /// la capa visual reproduce los eventos en orden con animaciones. Así la lógica nunca depende de los fotogramas.
    /// </summary>
    public struct Evento
    {
        public TipoEvento Tipo;
        public int Turno;
        public int PolillaId;
        public TipoPolilla TipoPolilla;
        public int Desde;
        public int Hasta;
        public int Valor;
        public int Extra;
        public TipoHerramienta Herramienta;
        /// <summary>Índice de casilla de la herramienta implicada, o -1.</summary>
        public int IndiceHerramienta;

        public override string ToString()
        {
            string quien = PolillaId >= 0 ? TipoPolilla + "#" + PolillaId : "";
            switch (Tipo)
            {
                case TipoEvento.InicioTurno: return "--- Turno " + Turno + " ---";
                case TipoEvento.Aparece: return quien + " aparece en la entrada";
                case TipoEvento.Invocada: return quien + " llega llamada por la Reina";
                case TipoEvento.Mueve: return quien + " " + Desde + "→" + Hasta;
                case TipoEvento.Sobrevuela: return quien + " sobrevuela " + Hasta;
                case TipoEvento.ActivaHerramienta: return Herramienta + "@" + IndiceHerramienta + " se activa sobre " + quien + " (quedan " + Valor + ")";
                case TipoEvento.PinchazoQuieta: return Herramienta + "@" + IndiceHerramienta + " vuelve a pinchar a " + quien + " (sigue quieta encima)";
                case TipoEvento.HerramientaGastada: return Herramienta + "@" + IndiceHerramienta + " se ha gastado";
                case TipoEvento.Danio: return quien + " recibe " + Valor + " de daño (vida " + Extra + ")";
                case TipoEvento.Muere: return quien + " queda fuera de combate";
                case TipoEvento.Retenida: return quien + " queda pegada " + Valor + " turno(s)";
                case TipoEvento.SigueQuieta: return quien + " no puede moverse";
                case TipoEvento.Enredada: return quien + " queda enredada " + Valor + " turno(s)";
                case TipoEvento.QuietaEnredada: return quien + " forcejea con el hilo";
                case TipoEvento.SeDesenreda: return quien + " se desenreda";
                case TipoEvento.Empujada: return quien + " sale despedida " + Desde + "→" + Hasta;
                case TipoEvento.PlacaRota: return quien + " pierde su placa de fieltro (el efecto no le afecta)";
                case TipoEvento.Deslumbrada: return quien + " queda deslumbrada por la lámpara";
                case TipoEvento.Devora: return quien + " se come " + Herramienta + "@" + IndiceHerramienta;
                case TipoEvento.LlamaAyuda: return quien + " llama a su corte (vida ≤ " + Valor + ")";
                case TipoEvento.LlegaAlCarrete: return quien + " llega al carrete";
                case TipoEvento.DedalProtege: return "El dedal protege el carrete";
                case TipoEvento.CarreteDaniado: return "El carrete pierde " + Valor + " (vida " + Extra + ")";
                case TipoEvento.DaLaVuelta: return quien + " da otra vuelta";
                case TipoEvento.Victoria: return "¡Sala superada!";
                case TipoEvento.Derrota: return "El carrete ha sido devorado";
                default: return Tipo.ToString();
            }
        }
    }
}
