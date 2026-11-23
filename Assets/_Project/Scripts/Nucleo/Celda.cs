using System;

namespace DesenredaElOvillo.Nucleo
{
    /// <summary>
    /// Coordenada entera de una casilla del tablero.
    /// (0,0) es la esquina inferior izquierda, igual que el espacio de mundo de Unity.
    /// </summary>
    public readonly struct Celda : IEquatable<Celda>
    {
        public readonly int X;
        public readonly int Y;

        public Celda(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>Orden fijo de vecinos: derecha, izquierda, arriba, abajo. No cambiarlo: afecta al determinismo.</summary>
        public static readonly Celda[] Direcciones =
        {
            new Celda(1, 0), new Celda(-1, 0), new Celda(0, 1), new Celda(0, -1)
        };

        public Celda Mas(Celda d) => new Celda(X + d.X, Y + d.Y);

        public Celda Menos(Celda o) => new Celda(X - o.X, Y - o.Y);

        public bool EsAdyacente(Celda o) => Math.Abs(X - o.X) + Math.Abs(Y - o.Y) == 1;

        public int DistanciaManhattan(Celda o) => Math.Abs(X - o.X) + Math.Abs(Y - o.Y);

        public bool Equals(Celda o) => X == o.X && Y == o.Y;

        public override bool Equals(object obj) => obj is Celda c && Equals(c);

        public override int GetHashCode() => unchecked((X * 397) ^ Y);

        public static bool operator ==(Celda a, Celda b) => a.Equals(b);

        public static bool operator !=(Celda a, Celda b) => !a.Equals(b);

        public override string ToString() => "(" + X + "," + Y + ")";
    }
}
