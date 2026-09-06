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

## Arquitectura agéntica

El desarrollo se orquesta con un grafo de agentes definidos en
`.opencode/agents/` y `.opencode/skills/` (opencode):

```text
PRODUCT_REQUEST
    -> functional-analyst   (story + acceptance criteria -> docs/stories/)
    -> developer            (implementa en feat/<STORY-ID>-<slug>)
    -> qa                   (validación adversarial: PASS / FAIL / BLOCKED)

QA PASS + DEV_APPROVED + ANALYST_APPROVED
    -> merge --no-ff a main -> DONE
```

Principios del diseño:

- **Orquestación por grafo, no por chat**: un agente `orchestrator` enruta el
  trabajo entre subagentes especializados (máx. 3 ciclos dev↔QA, luego escala
  al humano).
- **Coordinación por artefactos**: los agentes no conversan entre sí; se
  comunican mediante archivos versionados (stories, QA reports, ADRs).
- **Separación de responsabilidades**: el analista no programa, QA no edita
  código, el developer no decide reglas de gameplay.
- **Escalamiento explícito**: las decisiones se clasifican en niveles (L1
  interno, L2 arquitectura con ADR, L3 producto — siempre humanas).
- **`main` protegido**: solo entra trabajo validado por los tres approvals.

Detalles en [`docs/agents/graph.md`](docs/agents/graph.md) y
[`AGENTS.md`](AGENTS.md). Estado del desarrollo: [`docs/status.md`](docs/status.md)
· Visión del producto: [`docs/product/vision.md`](docs/product/vision.md)
