using System;
using System.Collections.Generic;

namespace DesenredaElOvillo.Nucleo
{
    /// <summary>
    /// Tabla de equilibrio del juego. Todos los números ajustables viven aquí.
    /// Los valores por defecto son los validados con el Laboratorio de balance (ver Docs/Balance).
    /// </summary>
    public sealed class Reglas
    {
        public int VidaCarrete = 4;
        public int VidaCarreteMaxima = 4;
        public int HiloBasePorSala = 18;
        /// <summary>Hilo extra que se añade en cada sala (mejora "Ovillo grande").</summary>
        public int HiloExtraPorSala;
        /// <summary>Máximo de hilo sobrante que se guarda para la sala siguiente (0 = no se guarda).</summary>
        public int ReservaMaximaDeHilo = 6;
        public int LongitudMinimaPorDefecto = 10;
        /// <summary>Corte de seguridad de la simulación; ninguna oleada legítima se acerca a este número.</summary>
        public int MaximoTurnos = 400;

        /// <summary>Mejora "Hilo grueso": daño extra de los alfileres colocados en una curva.</summary>
        public int BonoAlfilerEnCurva;
        /// <summary>Mejora "Dedal": el primer mordisco de cada sala hace 1 punto menos de daño al carrete.</summary>
        public bool DedalActivo;

        public readonly DefinicionHerramienta[] Herramientas = new DefinicionHerramienta[5];
        public readonly DefinicionPolilla[] Polillas = new DefinicionPolilla[4];

        public DefinicionHerramienta Herramienta(TipoHerramienta t) => Herramientas[(int)t];
        public DefinicionPolilla Polilla(TipoPolilla t) => Polillas[(int)t];

        public static Reglas PorDefecto()
        {
            var r = new Reglas();

            r.Herramientas[(int)TipoHerramienta.Alfiler] = new DefinicionHerramienta
            {
                Tipo = TipoHerramienta.Alfiler,
                Categoria = CategoriaHerramienta.Danio,
                Nombre = "Alfiler",
                Descripcion = "Pincha a la polilla que lo pisa. Si se queda quieta encima o vuelve a pasar, la pincha otra vez.",
                Danio = 2,
                Usos = 2,
                PinchaQuietas = true
            };
            r.Herramientas[(int)TipoHerramienta.Velcro] = new DefinicionHerramienta
            {
                Tipo = TipoHerramienta.Velcro,
                Categoria = CategoriaHerramienta.Inmovilizacion,
                Nombre = "Velcro",
                Descripcion = "Pega a la polilla: se queda quieta su siguiente turno. Se cose debajo y puede compartir casilla.",
                Turnos = 1,
                Usos = 2,
                VaDebajo = true
            };
            r.Herramientas[(int)TipoHerramienta.Bobina] = new DefinicionHerramienta
            {
                Tipo = TipoHerramienta.Bobina,
                Categoria = CategoriaHerramienta.Ralentizacion,
                Nombre = "Bobina",
                Descripcion = "Enreda a la polilla: va a media velocidad y no puede volar. Atrapa también a las que pasan volando.",
                Turnos = 4,
                Usos = 2,
                AtrapaEnVuelo = true
            };
            r.Herramientas[(int)TipoHerramienta.Tijeras] = new DefinicionHerramienta
            {
                Tipo = TipoHerramienta.Tijeras,
                Categoria = CategoriaHerramienta.Danio,
                Nombre = "Tijeras",
                Descripcion = "Un corte enorme a la primera polilla que las pisa (y otro si vuelve a pasar).",
                Danio = 5,
                Usos = 1
            };
            r.Herramientas[(int)TipoHerramienta.Boton] = new DefinicionHerramienta
            {
                Tipo = TipoHerramienta.Boton,
                Categoria = CategoriaHerramienta.Desplazamiento,
                Nombre = "Botón saltarín",
                Descripcion = "Lanza a la polilla hacia atrás por la lana (una vez a cada una). Al caer no activa nada; al volver a pasar, sí.",
                Distancia = 2,
                Usos = 2,
                UnaVezPorPolilla = true
            };

            r.Polillas[(int)TipoPolilla.Comun] = new DefinicionPolilla
            {
                Tipo = TipoPolilla.Comun,
                Nombre = "Polilla común",
                Descripcion = "Equilibrada. Avanza una casilla por turno.",
                Vida = 4,
                MediosPasosPorTurno = 2,
                DanioAlCarrete = 1
            };
            r.Polillas[(int)TipoPolilla.Veloz] = new DefinicionPolilla
            {
                Tipo = TipoPolilla.Veloz,
                Nombre = "Polilla veloz",
                Descripcion = "Vuela dos casillas por turno y solo toca el suelo al aterrizar. En las curvas tiene que aterrizar.",
                Vida = 3,
                MediosPasosPorTurno = 4,
                Vuela = true,
                DanioAlCarrete = 1
            };
            r.Polillas[(int)TipoPolilla.Acorazada] = new DefinicionPolilla
            {
                Tipo = TipoPolilla.Acorazada,
                Nombre = "Polilla acorazada",
                Descripcion = "Mucha vida. Su placa de fieltro anula el primer efecto de control que recibe.",
                Vida = 9,
                MediosPasosPorTurno = 2,
                Placas = 1,
                DanioAlCarrete = 2
            };
            r.Polillas[(int)TipoPolilla.Reina] = new DefinicionPolilla
            {
                Tipo = TipoPolilla.Reina,
                Nombre = "Reina de las polillas",
                Descripcion = "Se come la primera casilla con herramientas que pisa. Llama a su corte al perder vida. Si llega al carrete, muerde y da otra vuelta.",
                Vida = 34,
                MediosPasosPorTurno = 2,
                DanioAlCarrete = 2,
                ComeHerramientas = true,
                UmbralesInvocacion = new[] { 22, 11 },
                TipoInvocado = TipoPolilla.Comun,
                DaVueltas = true
            };
            return r;
        }

        public Reglas Clonar()
        {
            var c = new Reglas
            {
                VidaCarrete = VidaCarrete,
                VidaCarreteMaxima = VidaCarreteMaxima,
                HiloBasePorSala = HiloBasePorSala,
                HiloExtraPorSala = HiloExtraPorSala,
                ReservaMaximaDeHilo = ReservaMaximaDeHilo,
                LongitudMinimaPorDefecto = LongitudMinimaPorDefecto,
                MaximoTurnos = MaximoTurnos,
                BonoAlfilerEnCurva = BonoAlfilerEnCurva,
                DedalActivo = DedalActivo
            };
            for (int i = 0; i < Herramientas.Length; i++) c.Herramientas[i] = Herramientas[i].Clonar();
            for (int i = 0; i < Polillas.Length; i++) c.Polillas[i] = Polillas[i].Clonar();
            return c;
        }

        /// <summary>Copia de estas reglas con las mejoras de la partida aplicadas.</summary>
        public Reglas ConMejoras(IEnumerable<TipoMejora> mejoras)
        {
            var c = Clonar();
            if (mejoras != null)
            {
                foreach (var m in mejoras) CatalogoMejoras.AplicarAReglas(c, m);
            }
            return c;
        }

        public static int CantidadHerramientas => Enum.GetValues(typeof(TipoHerramienta)).Length;
    }
}
