# Ratillery

> Tiny rats. Massive damage.

Ratillery es un juego 2D de estrategia táctica por turnos protagonizado por
ratas militares, inspirado conceptualmente en juegos como Worms pero con
identidad propia: artillería, física balística, terreno destructible y humor
visual cartoon.

## Stack

- C# / .NET 10
- [MonoGame](https://monogame.net/) (DesktopGL)
- xUnit para unit tests de la lógica de dominio

## Comandos

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/Ratillery.Game
```

## Estructura

```text
Ratillery/
│
├── src/
│   ├── Ratillery.Game/     # Game loop, rendering, input, escenas (MonoGame)
│   └── Ratillery.Core/     # Reglas, entidades, balística, turnos (sin MonoGame)
│
├── tests/
│   └── Ratillery.Core.Tests/
│
├── assets/                 # Sprites, terreno, armas, efectos, UI, audio
│
└── docs/
```

### Separación de responsabilidades

- **Ratillery.Core**: lógica testeable sin ventana gráfica (balística, daño,
  entidades, estado del mundo).
- **Ratillery.Game**: integración con MonoGame (game loop, rendering, input,
  carga de assets, animaciones, escenas).

## Estado actual

Prueba de concepto inicial: escena mínima con una rata placeholder
(dibujada proceduralmente hasta tener los sprite sheets), suelo simple y
overlay de debug con FPS.

## First playable milestone

> A rat can destroy the world.

La milestone tendrá:

- Rata animada (sprite sheet, empezando por `Idle`).
- Terreno destructible (textura + collision mask).
- Apuntado y disparo.
- Balística con gravedad y viento.
- Explosiones que generan cráteres en el terreno.
