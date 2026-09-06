Quiero que crees la base de un nuevo proyecto de videojuego llamado **Ratillery**.

Ratillery será un juego 2D de estrategia táctica por turnos, inspirado conceptualmente en juegos como Worms, pero con identidad propia y protagonizado por pequeñas ratas militares.

El foco principal del juego estará en:

* Combate táctico por turnos.
* Artillería.
* Física balística.
* Terreno destructible.
* Diferentes armas y proyectiles.
* Animaciones 2D mediante sprite sheets.
* Personajes expresivos y humor visual.
* Una arquitectura limpia que permita incorporar progresivamente gameplay, IA, físicas, animaciones y contenido.

El objetivo actual NO es construir el juego completo.

Quiero crear una base técnica sólida y sencilla para una prueba de concepto, dejando el repositorio preparado para evolucionar incrementalmente.

## Stack tecnológico

Usar:

* C#
* .NET
* MonoGame
* DesktopGL o la variante de MonoGame más conveniente para ejecutar inicialmente en Windows.
* Git como control de versiones.

Evitar por ahora motores como Unity, Godot o Unreal.

La intención es trabajar relativamente cerca del game loop y mantener control sobre rendering, físicas, animaciones y gameplay.

## Objetivo inicial

La primera gran prueba de concepto del proyecto será:

> "Una rata puede destruir el mundo."

El escenario inicial debe permitir eventualmente:

1. Mostrar una rata animada.
2. Mostrar un terreno 2D.
3. Apuntar un arma.
4. Disparar un proyectil.
5. Simular trayectoria balística.
6. Detectar impacto contra el terreno.
7. Generar una explosión.
8. Crear un cráter destruyendo parte del terreno.

No es necesario implementar todo esto en el primer commit.

La tarea actual es preparar correctamente las bases arquitectónicas para llegar a ese milestone mediante iteraciones pequeñas.

## Dirección visual

El juego será 2D con cámara lateral.

El estilo visual será cartoon estilizado, con:

* Ratas pequeñas y expresivas.
* Cabezas relativamente grandes.
* Siluetas fáciles de reconocer.
* Armas ligeramente sobredimensionadas.
* Animaciones exageradas.
* Humor visual.
* Escenarios destructibles.
* Estética militar caricaturesca.

Los personajes pueden utilizar accesorios para diferenciar equipos, por ejemplo:

* Cascos.
* Pañuelos.
* Colores secundarios.
* Mochilas.
* Equipamiento militar.

El personaje base actualmente diseñado es una rata gris/marrón con:

* Panza clara.
* Orejas, nariz, patas y cola rosadas.
* Casco militar verde.
* Vista lateral.
* Orientación inicial hacia la derecha.

## Sistema de animaciones

Los personajes utilizarán sprite sheets.

Estados de animación previstos:

* Idle
* Walk
* Aim
* Fire
* Hit
* Airborne
* Land
* Dead

No hace falta implementar todas las animaciones todavía.

La primera animación será `Idle`.

La animación idle tendrá aproximadamente entre 5 y 10 frames y deberá simular movimiento sutil, por ejemplo:

* Respiración.
* Pequeño movimiento de cabeza.
* Movimiento leve de orejas.
* Movimiento suave de cola.
* Parpadeo ocasional.

El sistema de animación debe permitir agregar fácilmente nuevas animaciones posteriormente.

Una representación conceptual podría ser:

```csharp
public enum RatState
{
    Idle,
    Walking,
    Aiming,
    Firing,
    Hit,
    Airborne,
    Landing,
    Dead
}
```

Y algún concepto equivalente a:

```csharp
public sealed class AnimationClip
{
    public Texture2D Texture { get; init; }

    public int FrameWidth { get; init; }

    public int FrameHeight { get; init; }

    public int FrameCount { get; init; }

    public float FrameDuration { get; init; }

    public bool Loop { get; init; }
}
```

No copies esta estructura necesariamente de forma literal si encontrás una solución mejor.

Quiero que el diseño sea simple y extensible.

## Arquitectura

Separar claramente gameplay y lógica de dominio de los detalles de MonoGame cuando tenga sentido.

Una estructura inicial posible sería:

```text
Ratillery/
│
├── src/
│   ├── Ratillery.Game/
│   │   ├── Rendering/
│   │   ├── Input/
│   │   ├── Scenes/
│   │   ├── Animation/
│   │   └── Content/
│   │
│   └── Ratillery.Core/
│       ├── Entities/
│       ├── Combat/
│       ├── Physics/
│       ├── Turns/
│       └── World/
│
├── tests/
│   └── Ratillery.Core.Tests/
│
├── assets/
│   ├── Sprites/
│   │   └── Rats/
│   ├── Terrain/
│   ├── Weapons/
│   ├── Effects/
│   ├── UI/
│   └── Audio/
│
├── docs/
│
├── Ratillery.sln
├── README.md
├── .gitignore
└── .editorconfig
```

Esta estructura es una guía, no una restricción absoluta.

Preferir una arquitectura pragmática antes que una excesivamente abstracta.

Evitar crear interfaces o capas innecesarias solamente por aplicar patrones.

## Separación de responsabilidades

Idealmente:

### Ratillery.Core

Debe contener lógica independiente de MonoGame siempre que sea razonable.

Por ejemplo:

* Reglas de juego.
* Turnos.
* Física balística.
* Daño.
* Armas.
* Entidades.
* Estado del mundo.
* Cálculos de trayectoria.
* Viento.
* Explosiones.

Debe poder ser unit testeable sin iniciar una ventana gráfica.

### Ratillery.Game

Debe contener:

* Game loop.
* Rendering.
* Input.
* Carga de assets.
* Cámara.
* Integración con MonoGame.
* Animaciones visuales.
* Escenas.
* Adaptación entre Core y rendering.

## Física

Inicialmente no usar un motor físico complejo.

La balística puede ser implementada internamente.

Conceptualmente:

```csharp
velocity += gravity * deltaTime;
position += velocity * deltaTime;
```

Para disparos:

```csharp
velocity.X = MathF.Cos(angle) * power;
velocity.Y = -MathF.Sin(angle) * power;
```

El sistema deberá poder evolucionar posteriormente para contemplar:

* Gravedad.
* Fuerza de disparo.
* Ángulo.
* Viento.
* Diferentes masas.
* Diferentes proyectiles.
* Rebotes.
* Explosiones.

No implementar todos esos sistemas ahora si no son necesarios.

## Terreno destructible

El terreno destructible será una de las mecánicas técnicas principales.

La idea conceptual será mantener una representación del terreno mediante algo similar a:

```text
Terrain Texture
+
Collision Mask
```

Una explosión podrá remover una región circular o definida por una máscara.

Conceptualmente:

```text
Explosion
   ↓
Explosion mask / circle
   ↓
Remove terrain pixels
   ↓
Update collision information
```

No hace falta implementar todavía el sistema completo, pero quiero que la arquitectura no dificulte agregarlo.

## Entidades iniciales

Considerar conceptos como:

```text
Rat
Projectile
Weapon
Terrain
Explosion
GameWorld
TurnManager
```

No crear todos obligatoriamente ahora.

Crear solamente lo necesario para una base coherente.

## Primera escena

Crear una escena mínima ejecutable.

Al iniciar el juego debe aparecer una ventana mostrando:

* Un color de fondo sencillo.
* Una rata o placeholder de rata.
* Algún texto de debug indicando Ratillery.
* FPS o información básica de diagnóstico, si resulta sencillo.

La aplicación debe ejecutar correctamente.

Si todavía no existe el sprite definitivo, utilizar un placeholder claramente identificable.

## Assets

Los assets deberán mantenerse fuera del código.

Usar una estructura clara.

Ejemplo:

```text
assets/
  Sprites/
    Rats/
      Base/
        idle.png
        walk.png
        fire.png
```

Los sprite sheets serán preferentemente tiras horizontales.

Por ejemplo:

```text
idle.png

[ frame 1 ][ frame 2 ][ frame 3 ][ frame 4 ][ frame 5 ]...
```

Todos los frames de una animación deberán:

* Tener las mismas dimensiones.
* Mantener el personaje alineado consistentemente.
* Mantener un pivot lógico consistente.
* Poder ser recortados fácilmente.

Idealmente el runtime utilizará PNG con transparencia.

Los assets generados originalmente con chroma key podrán procesarse posteriormente.

## Calidad de código

Aplicar buenas prácticas de C#.

Quiero:

* Nullable Reference Types habilitado.
* Implicit Usings cuando tenga sentido.
* Nombres claros.
* Clases pequeñas.
* Separación razonable de responsabilidades.
* Evitar estado global innecesario.
* Evitar patrones excesivamente complejos.
* Código fácil de leer para otros desarrolladores y agentes IA.
* Comentarios solamente donde aporten información real.

Configurar `.editorconfig`.

## Testing

Crear un proyecto de unit tests para `Ratillery.Core`.

Utilizar xUnit o un framework equivalente razonable.

No testear rendering.

Los futuros sistemas que deberían poder testearse incluyen:

* Cálculo de trayectoria.
* Daño.
* Radios de explosión.
* Turnos.
* Viento.
* Estados de entidades.

Agregar al menos uno o dos tests pequeños de ejemplo para verificar que la infraestructura de testing funciona.

## README

Crear un README inicial que explique:

### Ratillery

Juego táctico 2D por turnos protagonizado por ratas militares.

Concepto:

> Tiny rats. Massive damage.

Explicar brevemente:

* Qué es el proyecto.
* Stack tecnológico.
* Cómo compilar.
* Cómo ejecutar.
* Cómo correr tests.
* Estructura general del repositorio.
* Estado actual.
* Próximo milestone.

Agregar una sección:

```text
## First playable milestone

A rat can destroy the world.
```

Describir que esta milestone tendrá:

* Rata animada.
* Terreno destructible.
* Apuntado.
* Disparo.
* Balística.
* Explosión.
* Cráter.

## Git

Inicializar el repositorio Git.

Agregar un `.gitignore` apropiado para:

* Visual Studio.
* Rider.
* VS Code.
* .NET.
* MonoGame.
* Archivos de build.
* Archivos temporales.

Hacer un commit inicial coherente cuando el proyecto compile correctamente.

Mensaje sugerido:

```text
chore: initialize Ratillery project
```

## Reglas para esta tarea

No intentes implementar todo el juego.

No agregues sistemas complejos prematuramente.

No agregues networking.

No agregues multiplayer.

No agregues ECS salvo que exista una razón técnica muy fuerte.

No agregues dependency injection compleja.

No agregues bases de datos.

No agregues persistencia todavía.

No agregues un motor físico externo todavía.

Priorizar:

1. Proyecto ejecutable.
2. Arquitectura sencilla.
3. Game loop limpio.
4. Assets organizados.
5. Core testeable.
6. Capacidad de evolucionar iterativamente.

## Resultado esperado

Al terminar quiero un repositorio en el que pueda ejecutar algo como:

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/Ratillery.Game
```

y obtener:

* Build exitoso.
* Tests exitosos.
* Una ventana de MonoGame ejecutándose.
* Una escena mínima de Ratillery visible.
* Una arquitectura lista para comenzar a implementar gameplay.

Además, documentar brevemente cualquier decisión técnica relevante tomada durante la creación del proyecto.

No sobrearquitectures.

Cuando haya que elegir entre una solución sofisticada y una solución sencilla que permita evolucionar después, elegir inicialmente la solución sencilla.
