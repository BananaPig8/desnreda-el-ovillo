using System.Collections.Generic;
using UnityEngine;

namespace DesenredaElOvillo.Juego.Tres
{
    /// <summary>
    /// Disposición del decorado del taller alrededor del tablero: bobinas gigantes, ovillos, botones sueltos,
    /// un rollo de tela y unas tijeras. Queda fuera del tablero (sobre todo detrás y a los lados) para dar
    /// profundidad sin tapar nunca una casilla. Se describe como datos para poder previsualizarlo fuera de Unity.
    /// </summary>
    public static class Decorado
    {
        public struct Objeto
        {
            public Pieza Pieza;
            public Vector3 Posicion;
            public float Giro;
        }

        public static List<Objeto> Disponer(int ancho, int alto, int semilla)
        {
            var azar = new System.Random(semilla);
            float R(float a, float b) => a + (float)azar.NextDouble() * (b - a);
            var lista = new List<Objeto>();
            void Poner(Pieza p, float x, float z, float giro) => lista.Add(new Objeto { Pieza = p, Posicion = new Vector3(x, 0, z), Giro = giro });

            // detrás del tablero (lejos de la cámara)
            Poner(Modelos.BobinaGrande(Paleta.AzulSuave), -1.6f, alto + 1.9f, R(0, 360));
            Poner(Modelos.BobinaGrande(Paleta.RosaPastel), ancho * 0.45f, alto + 2.6f, R(0, 360));
            Poner(Modelos.OvilloSuelto(Paleta.Bufanda), ancho + 1.4f, alto + 1.6f, R(0, 360));
            Poner(Modelos.RolloDeTela(Paleta.Morado), ancho * 0.2f, alto + 1.2f, 8);
            Poner(Modelos.BobinaGrande(Paleta.Mostaza), ancho + 3.2f, alto + 3.4f, R(0, 360));
            // a los lados
            Poner(Modelos.OvilloSuelto(Paleta.Hex("#8DB2D6")), -2.2f, alto * 0.45f, R(0, 360));
            Poner(Modelos.TijerasGrandes(), -2.1f, 0.6f, 25);
            // botones sueltos por la mesa
            var colores = new[] { Paleta.Coral, Paleta.Mostaza, Paleta.AzulSuave, Paleta.RosaPastel, Paleta.Hex("#7A9849") };
            for (int i = 0; i < 9; i++)
            {
                float x, z;
                int lado = i % 3;
                if (lado == 0) { x = R(-3.5f, -0.9f); z = R(-1.5f, alto + 1f); }
                else if (lado == 1) { x = R(ancho + 2.6f, ancho + 4.5f); z = R(-1.5f, alto + 1f); }
                else { x = R(-1f, ancho + 1f); z = R(alto + 0.9f, alto + 3.5f); }
                Poner(Modelos.BotonSuelto(colores[i % colores.Length], R(0.18f, 0.32f)), x, z, R(0, 360));
            }
            return lista;
        }
    }
}
