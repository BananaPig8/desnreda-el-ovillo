using System.Collections.Generic;

namespace DesenredaElOvillo.Nucleo
{
    /// <summary>
    /// Generador pseudoaleatorio propio (SplitMix64). Da exactamente la misma secuencia en Unity (Mono/IL2CPP),
    /// en .NET y en cualquier plataforma, cosa que System.Random no garantiza entre versiones.
    /// </summary>
    public sealed class Rng
    {
        private ulong _estado;

        public Rng(ulong semilla)
        {
            _estado = semilla;
        }

        public static Rng Derivado(int semilla, int canal)
        {
            unchecked
            {
                ulong s = (ulong)(uint)semilla * 0x9E3779B97F4A7C15UL ^ ((ulong)(uint)canal + 0xBF58476D1CE4E5B9UL) * 0x94D049BB133111EBUL;
                return new Rng(s);
            }
        }

        public ulong SiguienteBruto()
        {
            unchecked
            {
                _estado += 0x9E3779B97F4A7C15UL;
                ulong z = _estado;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        /// <summary>Entero en [0, maximoExclusivo).</summary>
        public int Siguiente(int maximoExclusivo)
        {
            if (maximoExclusivo <= 1) return 0;
            return (int)(SiguienteBruto() % (ulong)maximoExclusivo);
        }

        public T Elegir<T>(IReadOnlyList<T> lista) => lista[Siguiente(lista.Count)];

        public void Barajar<T>(IList<T> lista)
        {
            for (int i = lista.Count - 1; i > 0; i--)
            {
                int j = Siguiente(i + 1);
                var tmp = lista[i];
                lista[i] = lista[j];
                lista[j] = tmp;
            }
        }
    }
}
