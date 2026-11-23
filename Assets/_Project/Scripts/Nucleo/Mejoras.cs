using System.Collections.Generic;

namespace DesenredaElOvillo.Nucleo
{
    public enum TipoMejora
    {
        AlfileresAfilados,
        AcericoLleno,
        HiloGrueso,
        VelcroReforzado,
        BobinaPesada,
        TijerasDeSastre,
        MuelleTenso,
        OvilloGrande,
        Remiendo,
        Dedal
    }

    public sealed class DefinicionMejora
    {
        public TipoMejora Tipo;
        public string Nombre;
        public string Descripcion;
        /// <summary>Herramienta a la que afecta (para el icono). Null si es general.</summary>
        public TipoHerramienta? Herramienta;
    }

    /// <summary>
    /// Mejoras de partida: se elige una de tres al superar las salas 1 a 4.
    /// Cada una tiene una consecuencia visible e inmediata; ninguna es permanente entre partidas.
    /// </summary>
    public static class CatalogoMejoras
    {
        public static readonly IReadOnlyList<DefinicionMejora> Todas = new List<DefinicionMejora>
        {
            new DefinicionMejora { Tipo = TipoMejora.AlfileresAfilados, Nombre = "Alfileres afilados", Descripcion = "Los alfileres hacen +1 de daño.", Herramienta = TipoHerramienta.Alfiler },
            new DefinicionMejora { Tipo = TipoMejora.AcericoLleno, Nombre = "Acerico lleno", Descripcion = "Cada alfiler puede atrapar a una polilla más.", Herramienta = TipoHerramienta.Alfiler },
            new DefinicionMejora { Tipo = TipoMejora.HiloGrueso, Nombre = "Hilo grueso", Descripcion = "Un alfiler clavado en una curva de la lana hace +1 de daño.", Herramienta = TipoHerramienta.Alfiler },
            new DefinicionMejora { Tipo = TipoMejora.VelcroReforzado, Nombre = "Velcro reforzado", Descripcion = "El velcro retiene un turno más.", Herramienta = TipoHerramienta.Velcro },
            new DefinicionMejora { Tipo = TipoMejora.BobinaPesada, Nombre = "Bobina pesada", Descripcion = "El enredo de la bobina dura 2 turnos más y tiene +1 uso.", Herramienta = TipoHerramienta.Bobina },
            new DefinicionMejora { Tipo = TipoMejora.TijerasDeSastre, Nombre = "Tijeras de sastre", Descripcion = "Las tijeras cortan una vez más.", Herramienta = TipoHerramienta.Tijeras },
            new DefinicionMejora { Tipo = TipoMejora.MuelleTenso, Nombre = "Muelle tenso", Descripcion = "El botón saltarín lanza una casilla más lejos.", Herramienta = TipoHerramienta.Boton },
            new DefinicionMejora { Tipo = TipoMejora.OvilloGrande, Nombre = "Ovillo grande", Descripcion = "+3 de hilo en cada sala." },
            new DefinicionMejora { Tipo = TipoMejora.Remiendo, Nombre = "Remiendo", Descripcion = "El carrete recupera 2 puntos de vida (máximo 4)." },
            new DefinicionMejora { Tipo = TipoMejora.Dedal, Nombre = "Dedal", Descripcion = "El primer mordisco al carrete en cada sala hace 1 punto menos de daño." }
        };

        public static DefinicionMejora Obtener(TipoMejora tipo)
        {
            foreach (var m in Todas) if (m.Tipo == tipo) return m;
            return null;
        }

        /// <summary>Aplica el efecto de la mejora sobre la tabla de reglas (las de efecto inmediato se aplican en Partida).</summary>
        public static void AplicarAReglas(Reglas r, TipoMejora m)
        {
            switch (m)
            {
                case TipoMejora.AlfileresAfilados: r.Herramienta(TipoHerramienta.Alfiler).Danio += 1; break;
                case TipoMejora.AcericoLleno: r.Herramienta(TipoHerramienta.Alfiler).Usos += 1; break;
                case TipoMejora.HiloGrueso: r.BonoAlfilerEnCurva += 1; break;
                case TipoMejora.VelcroReforzado: r.Herramienta(TipoHerramienta.Velcro).Turnos += 1; break;
                case TipoMejora.BobinaPesada:
                    r.Herramienta(TipoHerramienta.Bobina).Turnos += 2;
                    r.Herramienta(TipoHerramienta.Bobina).Usos += 1;
                    break;
                case TipoMejora.TijerasDeSastre: r.Herramienta(TipoHerramienta.Tijeras).Usos += 1; break;
                case TipoMejora.MuelleTenso: r.Herramienta(TipoHerramienta.Boton).Distancia += 1; break;
                case TipoMejora.OvilloGrande: r.HiloExtraPorSala += 3; break;
                case TipoMejora.Dedal: r.DedalActivo = true; break;
                case TipoMejora.Remiendo: break; // efecto inmediato sobre la vida del carrete (Partida)
            }
        }
    }
}
