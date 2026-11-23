# Informe de balance (generado)

> Generado por `Tools/Laboratorio` (`dotnet run -c Release -- informe`). No editar a mano: se regenera.
> **Mejor encontrado**: la mejor solución que encontró la búsqueda (recorridos al azar + optimización de colocaciones).
> **Ingenuo**: recorrido válido al azar y herramientas puestas al azar. **Aplicado**: recorrido al azar, pero el mejor de 25 intentos de colocación.
> Sirven para comparar salas entre sí, no como predicción exacta de jugadores reales: eso lo dirán las pruebas con personas.

| Sala | Escenario | Mejor encontrado | Hilo | Ingenuo: gana / perfecto / daño medio | Aplicado: gana / perfecto / daño medio |
|---|---|---|---|---|---|
| Primeros hilos | Tutorial | gana, carrete 4/4 | 11/14 | 100% / 10% / 1.8 | 100% / 88% / 0.2 |
| El primer problema | Coge la bobina y un alfiler | gana, carrete 4/4 | 17/18 | 100% / 0% / 2.4 | 100% / 5% / 1.8 |
| El primer problema | Ignora la bobina | gana, carrete 4/4 | 15/18 | 100% / 1% / 2.4 | 100% / 15% / 1.7 |
| El primer problema | Bobina y velcro | gana, carrete 2/4 | 15/18 | 17% / 0% / 3.7 | 99% / 0% / 2.0 |
| El taller se complica | Tijeras y botón | gana, carrete 4/4 | 15/18 | 74% / 0% / 2.9 | 100% / 1% / 2.0 |
| El taller se complica | Tijeras y alfiler | gana, carrete 4/4 | 17/18 | 96% / 1% / 2.3 | 100% / 24% / 1.4 |
| El taller se complica | Botón y velcro + mejora (AlfileresAfilados) | gana, carrete 4/4 | 17/18 | 94% / 0% / 2.7 | 100% / 8% / 1.8 |
| La última costura | Costurero típico | gana, carrete 4/4 | 13/16 | 32% / 0% / 3.7 | 100% / 4% / 2.4 |
| La última costura | Típico + 2 mejoras (AlfileresAfilados, VelcroReforzado) | gana, carrete 4/4 | 15/16 | 76% / 4% / 2.7 | 100% / 62% / 0.4 |
| La última costura | Mucho control, poco daño | gana, carrete 2/4 | 13/16 | 2% / 0% / 4.1 | 34% / 0% / 3.7 |
| La Reina | Costurero típico sin mejoras | gana, carrete 2/4 | 15/18 | 13% / 0% / 4.0 | 95% / 0% / 2.7 |
| La Reina | Típico + 3 mejoras (AlfileresAfilados, VelcroReforzado, AcericoLleno) | gana, carrete 4/4 | 13/18 | 86% / 3% / 2.4 | 100% / 43% / 1.1 |
| La Reina | Típico + 3 mejoras de control (OvilloGrande, BobinaPesada, MuelleTenso) | gana, carrete 4/4 | 17/21 | 16% / 0% / 3.9 | 98% / 0% / 2.5 |
| La Reina | Típico + 3 mejoras + 6 de reserva (AlfileresAfilados, VelcroReforzado, AcericoLleno) | gana, carrete 4/4 | 15/24 | 84% / 2% / 2.5 | 100% / 29% / 1.4 |

## Soluciones encontradas

### Primeros hilos — Tutorial

Costurero: Alfiler, Velcro

```
03+ 04~ 05~ 06~ 07+  .   .  
02~  .   .   .  08~  .   .  
01A  .   .   u  09+ 10~ 11+ 
 E   .   .   o   .   .   S  
 .   .   .   .   .   .   .  
 .   .   .   .   .   .   .  
 .   .   .   .   .   .   .  
Leyenda: A alfiler, V velcro, B bobina, T tijeras, O botón, + curva, ~ lana
recorrido: AAADDDDBBDDB
colocación: Alfiler@1, Velcro@1
VICTORIA | carrete 4→4 | derrotadas 2 | escapadas 0 | turnos 5
```

### El primer problema — Coge la bobina y un alfiler

Costurero: Alfiler, Velcro, Bobina, Alfiler

```
 .   .   .   .   .   .   .   .   .  
 .   .   .   .   .   .   .   .   .  
 .   .   #   #   .   .   .   .   .  
 .   .   .   .   .   .   o   .   .  
 E   .   .   .   .  15+ 16~ 17~  S  
01+ 02B  o   .   .  14~  .   .   .  
04V 03A  .   .   .  13~  #   #   .  
05~  .  09+ 10~ 11~ 12+  .   .   .  
06+ 07~ 08+  .   .   .   .   .   .  
Leyenda: A alfiler, V velcro, B bobina, T tijeras, O botón, + curva, ~ lana
recorrido: BDBIBBDDADDDAAADDD
colocación: Bobina@2, Alfiler@3, Alfiler@4, Velcro@4
VICTORIA | carrete 4→4 | derrotadas 4 | escapadas 0 | turnos 11
```

### El primer problema — Ignora la bobina

Costurero: Alfiler, Velcro, Alfiler, Velcro

```
 .   .   .   .   .   .   .   .   .  
 .   .   .   .   .   .   .   .   .  
 .   .   #   #   .   .   .   .   .  
 .   .   .   .   .   .   o   .   .  
 E   .   .   .   .   .  14+ 15~  S  
01A  .   o   .  11+ 12~ 13+  .   .  
02V  .   .  09+ 10+  .   #   #   .  
03+ 04+ 07+ 08+  .   .   .   .   .  
 .  05+ 06+  .   .   .   .   .   .  
Leyenda: A alfiler, V velcro, B bobina, T tijeras, O botón, + curva, ~ lana
recorrido: BBBDBDADADADDADD
colocación: Alfiler@1, Velcro@1, Alfiler@2, Velcro@2
VICTORIA | carrete 4→4 | derrotadas 4 | escapadas 0 | turnos 9
```

### El primer problema — Bobina y velcro

Costurero: Alfiler, Velcro, Bobina, Velcro

```
 .   .   .   .   .   .   .   .   .  
 .   .   .   .   .   .   .   .   .  
 .   .   #   #   .   .   .   .   .  
 .   .   .   .   .   .   o   .   .  
 E   .   .   .   .   .  14+ 15~  S  
01~  .   o   .  11+ 12~ 13+  .   .  
02~  .   .  09+ 10A  .   #   #   .  
03B 04+ 07+ 08V  .   .   .   .   .  
 .  05+ 06+  .   .   .   .   .   .  
Leyenda: A alfiler, V velcro, B bobina, T tijeras, O botón, + curva, ~ lana
recorrido: BBBDBDADADADDADD
colocación: Bobina@3, Velcro@8, Alfiler@10, Velcro@10
VICTORIA | carrete 4→2 | derrotadas 2 | escapadas 2 | turnos 22
```

### El taller se complica — Tijeras y botón

Costurero: Alfiler, Velcro, Bobina, Alfiler, Tijeras, Boton

```
04+ 05T 06V  .   .   .   .   .   .  
03O  #  07+ 08~ 09~ 10+  .   #   .  
02A  #   .   .   c  11~  .   #   .  
01B  .   .   .   c  12+ 13~ 14~ 15+ 
 E   .   .   .   .   .   .   .   S  
 .   .   .   .   c   .   .   .   .  
 .   #   .   .   c   .   .   #   .  
 .   #   .   .   .   .   .   #   .  
 .   .   .   .   .   .   .   .   .  
Leyenda: A alfiler, V velcro, B bobina, T tijeras, O botón, + curva, ~ lana
recorrido: AAAADDBDDDBBDDDB
colocación: Bobina@1, Alfiler@2, Boton@3, Tijeras@5, Alfiler@6, Velcro@6
VICTORIA | carrete 4→4 | derrotadas 4 | escapadas 0 | turnos 12
```

### El taller se complica — Tijeras y alfiler

Costurero: Alfiler, Velcro, Bobina, Alfiler, Tijeras, Alfiler

```
 .   .   .  09+ 10~ 11+  .   .   .  
 .   #   .  08~  .  12+ 13+  #   .  
 .   #  06+ 07+  c   .  14~  #   .  
 .  04A 05+  .   c   .  15~  .   .  
 E  03T  .   .   .   .  16+ 17B  S  
01V 02A  .   .   c   .   .   .   .  
 .   #   .   .   c   .   .   #   .  
 .   #   .   .   .   .   .   #   .  
 .   .   .   .   .   .   .   .   .  
Leyenda: A alfiler, V velcro, B bobina, T tijeras, O botón, + curva, ~ lana
recorrido: BDAADADAADDBDBBBDD
colocación: Alfiler@1, Velcro@1, Alfiler@2, Tijeras@3, Alfiler@4, Bobina@17
VICTORIA | carrete 4→4 | derrotadas 4 | escapadas 0 | turnos 9
```

### El taller se complica — Botón y velcro + mejora

Costurero: Alfiler, Velcro, Bobina, Alfiler, Boton, Velcro · Mejoras: AlfileresAfilados

```
 .   .   .   .   .   .   .   .   .  
 .   #   .   .   .   .   .   #   .  
 .   #   .   .   c   .   .   #   .  
 .   .   .   .   c  14+ 15~ 16~ 17+ 
 E   .   .   .   .  13~  .   .   S  
01B  .   .   .   c  12~  .   .   .  
02~  #   .   .   c  11~  .   #   .  
03~  #   .   .   .  10V  .   #   .  
04A 05A 06O 07~ 08~ 09+  .   .   .  
Leyenda: A alfiler, V velcro, B bobina, T tijeras, O botón, + curva, ~ lana
recorrido: BBBBDDDDDAAAAADDDB
colocación: Bobina@1, Alfiler@4, Alfiler@5, Velcro@5, Boton@6, Velcro@10
VICTORIA | carrete 4→4 | derrotadas 4 | escapadas 0 | turnos 13
```

### La última costura — Costurero típico

Costurero: Alfiler, Velcro, Bobina, Alfiler, Tijeras, Boton, Alfiler, Velcro

```
 .   .   #   .   .   .   #   .   .  
 .   .   #   .   .   .   #   .   .  
 .   .   .   .   L   .   .   .   .  
 #   #   .   #   #   #   .   #   #  
 E  01~ 02A  .   .   .  12+ 13~  S  
 #   #  03~  #   #   #  11~  #   #  
 .   .  04A 05O 08+ 09~ 10T  .   .  
 .   .   #  06B 07A  .   #   .   .  
 .   .   #   .   .   .   #   .   .  
Leyenda: A alfiler, V velcro, B bobina, T tijeras, O botón, + curva, ~ lana
recorrido: DDBBDBDADDAADD
colocación: Alfiler@2, Velcro@2, Alfiler@4, Boton@5, Bobina@6, Alfiler@7, Velcro@7, Tijeras@10
VICTORIA | carrete 4→4 | derrotadas 6 | escapadas 0 | turnos 17
```

### La última costura — Típico + 2 mejoras

Costurero: Alfiler, Velcro, Bobina, Alfiler, Tijeras, Boton, Alfiler, Velcro · Mejoras: AlfileresAfilados, VelcroReforzado

```
 .   .   #   .   .   .   #   .   .  
 .   .   #   .   .   .   #   .   .  
 .   .   .   .   L   .   .   .   .  
 #   #   .   #   #   #   .   #   #  
 E  01A 02T  .   .   .  14+ 15~  S  
 #   #  03A  #   #   #  13~  #   #  
 .   .  04O 05B 10+ 11~ 12+  .   .  
 .   .   #  06A 09~  .   #   .   .  
 .   .   #  07+ 08+  .   #   .   .  
Leyenda: A alfiler, V velcro, B bobina, T tijeras, O botón, + curva, ~ lana
recorrido: DDBBDBBDAADDAADD
colocación: Alfiler@1, Velcro@1, Tijeras@2, Alfiler@3, Velcro@3, Boton@4, Bobina@5, Alfiler@6
VICTORIA | carrete 4→4 | derrotadas 6 | escapadas 0 | turnos 12
```

### La última costura — Mucho control, poco daño

Costurero: Alfiler, Velcro, Bobina, Velcro, Boton, Bobina, Alfiler, Boton

```
 .   .   #   .   .   .   #   .   .  
 .   .   #  06+ 07~ 08B  #   .   .  
 .   .  04+ 05A  L  09+ 10B  .   .  
 #   #  03~  #   #   #  11V  #   #  
 E  01O 02+  .   .   .  12O 13~  S  
 #   #   .   #   #   #   .   #   #  
 .   .   .   .   .   .   .   .   .  
 .   .   #   .   .   .   #   .   .  
 .   .   #   .   .   .   #   .   .  
Leyenda: A alfiler, V velcro, B bobina, T tijeras, O botón, + curva, ~ lana
recorrido: DDAADADDBDBBDD
colocación: Boton@1, Alfiler@5, Velcro@5, Bobina@8, Bobina@10, Alfiler@11, Velcro@11, Boton@12
VICTORIA | carrete 4→2 | derrotadas 4 | escapadas 2 | turnos 23
```

### La Reina — Costurero típico sin mejoras

Costurero: Alfiler, Velcro, Bobina, Alfiler, Tijeras, Boton, Alfiler, Velcro, Tijeras, Alfiler

```
04V 05T 06O 07A 08~ 09A 10~ 11+  .  
03T  .   .   L   .   .   .  12~  .  
02A  .   .   .   .   .   o  13~  .  
01B  .   u   .   .   .   .  14~  .  
 E   .   .   .   .   .   .  15+  S  
 .   .   .   .   .   .   u   .   .  
 .   .   o   .   .   .   .   .   .  
 .   .   .   .   .   L   .   .   .  
 .   .   .   .   .   .   .   .   .  
Leyenda: A alfiler, V velcro, B bobina, T tijeras, O botón, + curva, ~ lana
recorrido: AAAADDDDDDDBBBBD
colocación: Bobina@1, Alfiler@2, Tijeras@3, Alfiler@4, Velcro@4, Tijeras@5, Boton@6, Alfiler@7, Alfiler@9, Velcro@9
VICTORIA | carrete 4→2 | derrotadas 5 | escapadas 0 | turnos 27
```

### La Reina — Típico + 3 mejoras

Costurero: Alfiler, Velcro, Bobina, Alfiler, Tijeras, Boton, Alfiler, Velcro, Tijeras, Alfiler · Mejoras: AlfileresAfilados, VelcroReforzado, AcericoLleno

```
 .   .   .   .   .   .   .   .   .  
 .   .   .   L   .   .   .   .   .  
02A 03T 04A 05A 08+ 09+  o   .   .  
01A  .   u  06O 07+ 10T  .   .   .  
 E   .   .   .   .  11+ 12~ 13B  S  
 .   .   .   .   .   .   u   .   .  
 .   .   o   .   .   .   .   .   .  
 .   .   .   .   .   L   .   .   .  
 .   .   .   .   .   .   .   .   .  
Leyenda: A alfiler, V velcro, B bobina, T tijeras, O botón, + curva, ~ lana
recorrido: AADDDBDADBBDDD
colocación: Alfiler@1, Alfiler@2, Velcro@2, Tijeras@3, Alfiler@4, Alfiler@5, Velcro@5, Boton@6, Tijeras@10, Bobina@13
VICTORIA | carrete 4→4 | derrotadas 5 | escapadas 0 | turnos 17
```

### La Reina — Típico + 3 mejoras de control

Costurero: Alfiler, Velcro, Bobina, Alfiler, Tijeras, Boton, Alfiler, Velcro, Tijeras, Alfiler · Mejoras: OvilloGrande, BobinaPesada, MuelleTenso

```
04A 05A 08+ 09~ 10~ 11T  .   .   .  
03B 06+ 07A  L   .  12T 13O 14~ 15+ 
02~  .   .   .   .   .   o   .  16~ 
01A  .   u   .   .   .   .   .  17~ 
 E   .   .   .   .   .   .   .   S  
 .   .   .   .   .   .   u   .   .  
 .   .   o   .   .   .   .   .   .  
 .   .   .   .   .   L   .   .   .  
 .   .   .   .   .   .   .   .   .  
Leyenda: A alfiler, V velcro, B bobina, T tijeras, O botón, + curva, ~ lana
recorrido: AAAADBDADDDBDDDBBB
colocación: Alfiler@1, Velcro@1, Bobina@3, Alfiler@4, Velcro@4, Alfiler@5, Alfiler@7, Tijeras@11, Tijeras@12, Boton@13
VICTORIA | carrete 4→4 | derrotadas 5 | escapadas 0 | turnos 29
```

### La Reina — Típico + 3 mejoras + 6 de reserva

Costurero: Alfiler, Velcro, Bobina, Alfiler, Tijeras, Boton, Alfiler, Velcro, Tijeras, Alfiler · Mejoras: AlfileresAfilados, VelcroReforzado, AcericoLleno

```
 .   .  06O 07T 08~ 09+  .   .   .  
03A 04A 05T  L   .  10+ 11~ 12+  .  
02V  .   .   .   .   .   o  13B 14+ 
01A  .   u   .   .   .   .   .  15~ 
 E   .   .   .   .   .   .   .   S  
 .   .   .   .   .   .   u   .   .  
 .   .   o   .   .   .   .   .   .  
 .   .   .   .   .   L   .   .   .  
 .   .   .   .   .   .   .   .   .  
Leyenda: A alfiler, V velcro, B bobina, T tijeras, O botón, + curva, ~ lana
recorrido: AAADDADDDBDDBDBB
colocación: Alfiler@1, Alfiler@2, Velcro@2, Alfiler@3, Alfiler@4, Velcro@4, Tijeras@5, Boton@6, Tijeras@7, Bobina@13
VICTORIA | carrete 4→4 | derrotadas 5 | escapadas 0 | turnos 17
```

