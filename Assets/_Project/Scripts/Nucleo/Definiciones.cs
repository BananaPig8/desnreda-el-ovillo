using System;

namespace DesenredaElOvillo.Nucleo
{
    public enum TipoHerramienta
    {
        Alfiler = 0,
        Velcro = 1,
        Bobina = 2,
        Tijeras = 3,
        Boton = 4
    }

    public enum CategoriaHerramienta
    {
        Danio,
        Inmovilizacion,
        Ralentizacion,
        Desplazamiento
    }

    public enum TipoPolilla
    {
        Comun = 0,
        Veloz = 1,
        Acorazada = 2,
        Reina = 3
    }

    /// <summary>Estadísticas de una herramienta de costura (ya con mejoras aplicadas).</summary>
    public sealed class DefinicionHerramienta
    {
        public TipoHerramienta Tipo;
        public CategoriaHerramienta Categoria;
        public string Nombre;
        public string Descripcion;

        /// <summary>Daño por activación (alfiler, tijeras).</summary>
        public int Danio;
        /// <summary>
        /// Polillas distintas que puede atrapar por sala (se reponen al empezar cada sala).
        /// A una polilla ya atrapada le sigue afectando sin gastar usos.
        /// </summary>
        public int Usos;
        /// <summary>Turnos de retención (velcro) o de enredo (bobina).</summary>
        public int Turnos;
        /// <summary>Casillas de retroceso (botón saltarín).</summary>
        public int Distancia;

        /// <summary>Se cose debajo y puede compartir casilla con otra herramienta (velcro).</summary>
        public bool VaDebajo;
        /// <summary>Atrapa también a las polillas que pasan volando por encima (bobina).</summary>
        public bool AtrapaEnVuelo;
        /// <summary>Vuelve a pinchar cada turno a la polilla que se queda quieta encima (alfiler).</summary>
        public bool PinchaQuietas;
        /// <summary>Solo afecta una vez a cada polilla, aunque vuelva a pisarla (botón: evita rebotes infinitos).</summary>
        public bool UnaVezPorPolilla;

        public DefinicionHerramienta Clonar() => (DefinicionHerramienta)MemberwiseClone();
    }

    /// <summary>Estadísticas de un tipo de polilla.</summary>
    public sealed class DefinicionPolilla
    {
        public TipoPolilla Tipo;
        public string Nombre;
        public string Descripcion;

        public int Vida;
        /// <summary>Velocidad en medios pasos por turno: 2 = una casilla por turno, 4 = dos casillas.</summary>
        public int MediosPasosPorTurno;
        /// <summary>Vuela: solo toca las casillas en las que aterriza (final del movimiento o curvas).</summary>
        public bool Vuela;
        /// <summary>Placas de fieltro: cada una anula un efecto de control (retener, enredar, empujar, deslumbrar).</summary>
        public int Placas;
        /// <summary>Puntos de vida que quita al carrete si llega a él.</summary>
        public int DanioAlCarrete;
        /// <summary>Se come la primera casilla con herramientas que pisa, sin sufrir su efecto (Reina).</summary>
        public bool ComeHerramientas;
        /// <summary>Al bajar su vida hasta cada umbral llama a una polilla de apoyo (Reina).</summary>
        public int[] UmbralesInvocacion = Array.Empty<int>();
        public TipoPolilla TipoInvocado = TipoPolilla.Comun;
        /// <summary>Si llega al carrete, muerde y vuelve a empezar el recorrido (Reina).</summary>
        public bool DaVueltas;

        public int CasillasPorTurno => MediosPasosPorTurno / 2;

        public DefinicionPolilla Clonar()
        {
            var c = (DefinicionPolilla)MemberwiseClone();
            c.UmbralesInvocacion = (int[])UmbralesInvocacion.Clone();
            return c;
        }
    }
}
