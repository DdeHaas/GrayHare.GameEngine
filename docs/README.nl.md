# GrayHare.GameEngine

GrayHare.GameEngine is een lichtgewicht 2D-game-engine gebouwd op [SFML.Net 3.0.0](https://www.sfml-dev.org/) voor .NET-applicaties.
Het biedt een kleine runtime die is gecentreerd rond `GameApplication`, `GameHost`, `GameSceneBase` en `World`, zodat je scènegestuurde games kunt bouwen met gecachede assets, audio, invoer, camerabesturing, animatie, stuurgedrag, ruimtelijke query's en padvinding in een raster, zonder een groot framework.

---

## Inhoudsopgave

- [Architectuur](#architectuur)
- [Aan de slag](#aan-de-slag)
- [Application](#application)
  - [GameApplicationOptions](#gameapplicationoptions)
  - [GameApplication](#gameapplication)
  - [GameHost](#gamehost)
  - [GameTime](#gametime)
  - [Camera2D](#camera2d)
  - [Scene Stack (Push / Pop)](#scene-stack-push--pop)
- [Abstractions](#abstractions)
  - [`IGameObject`](#igameobject)
  - [`IMovableGameObject : IGameObject`](#imovablegameobject--igameobject)
- [Scenes](#scenes)
  - [GameSceneBase](#gamescenebase)
  - [ISceneLayer](#iscenelayer)
- [ECS](#ecs)
- [Input](#input)
  - [InputSnapshot](#inputsnapshot)
  - [InputActionMap](#inputactionmap)
- [Assets](#assets)
- [Audio](#audio)
- [Animation](#animation)
- [Behaviors](#behaviors)
- [SteeringForces](#steeringforces)
- [Extensions](#extensions)
- [Shaders](#shaders)
- [Wall](#wall)
- [Spatial](#spatial)
- [Pathfinding](#pathfinding)
- [Constants](#constants)
- [Ontwerppatronen](#ontwerppatronen)

---

## Architectuur

```text
┌─────────────────────────────────────────────────────────────┐
│                      GameApplication                        │
│       venster aanmaken · hoofdlus · frametiming             │
└──────────────────────────────┬──────────────────────────────┘
                               │ bezit
                               ▼
┌─────────────────────────────────────────────────────────────┐
│                        GameHost                             │
│         service locator — doorgegeven aan elke scene        │
│  Window │ Input │ Assets │ Audio │ World │ Options          │
└──┬───────┬────────┬─────────┬───────┬────────┬──────────────┘
   │       │        │         │       │        │
   ▼       ▼        ▼         ▼       ▼        ▼
Render  Input    Asset    Audio    ECS     Game
Window  Tracker  Store    Player   World   Options
           │                         │
           ▼                         ▼
      InputSnapshot            Entity / Components
    (per-frame snapshot)       (sparse-set stores)

┌─────────────────────────────────────────────────────────────┐
│                      SceneManager                           │
│      Load → Update/Render-lus → Unload → volgende scene     │
└──────────────────────────────┬──────────────────────────────┘
                               │ beheert
                               ▼
                           GameSceneBase
                     (virtuele lifecycle-hooks)
                  Load · Update · RenderLayer · Unload
```

| Laag | Verantwoordelijkheid |
|------|----------------------|
| **GameApplication** | Maakt het SFML-venster aan, drijft de hoofdlus, meet de frametijd |
| **GameHost** | Service locator; het ene object dat aan elke scene wordt doorgegeven |
| **SceneManager** | Beheert sceneovergangen op veilige framegrenzen |
| **GameSceneBase** | Door de gebruiker gedefinieerde gameschermen; overschrijf virtuele methoden en implementeer `RenderLayer` om te tekenen |
| **World / Entity** | Lichtgewicht ECS; entiteiten zijn integer-ID's, componenten leven in getypeerde woordenboeken |
| **AssetStore** | Laadt en cachet texturen, lettertypen, geluiden en shaders |
| **InputTracker / InputSnapshot** | Bouwt een frame-scoped snapshot van toetsenbord- en muisstatus |
| **AudioPlayer** | Beheert actieve `Sound`-instanties en verwijdert voltooide per frame |
| **Behaviors** | Samenstelbare bewegings-, rotatie- en steering-force-strategieën |
| **SpatialGrid** | Gridgebaseerde spatial hash voor snelle radius-gebaseerde buurtquery's |

---

## Aan de slag

Voeg **SFML.Net 3.0.0** toe aan je project, verwijs naar `GrayHare.GameEngine` en maak vervolgens
een startpunt en een eerste scene aan:

```csharp
using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Scenes;

using SFML.Graphics;
using SFML.System;
using SFML.Window;

// Engine configureren en starten
GameApplicationOptions options = new()
{
    Title          = "Mijn Eerste Game",
    WindowSize     = new Vector2u(1280, 720),
    FrameRateLimit = 60
};

new GameApplication(options).Run(new MainMenuScene());

// -------------------------------------------------------------------

internal sealed class MainMenuScene : GameSceneBase
{
    private Font? _font;

    public override void Load(GameHost host)
    {
        _font = host.Assets.LoadFont();
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        if (host.Input.WasKeyPressed(Keyboard.Key.Escape))
        {
            host.Exit();
        }
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        window.DrawCenteredText(_font!, 48, Color.White, "Druk op ESC om te sluiten", 340f);
    }
}
```

---

## Application

### Overzicht

De `Application`-namespace vormt de bootstrap-laag van de engine. `GameApplication` maakt het
SFML-venster aan en voert de hoofdgamelus uit. `GameHost` fungeert als service locator die alle
subsystemen beschikbaar stelt aan scenes. `SceneManager` verwerkt sceneovergangen op veilige
framegrenzen.

### `GameApplicationOptions`

Alle eigenschappen gebruiken `init`-only setters en zijn thread-safe na constructie.

| Eigenschap | Type | Standaard | Beschrijving |
|-----------|------|-----------|--------------|
| `Title` | `string` | `"GrayHare.GameEngine"` | Titelbalktext van het venster |
| `WindowSize` | `Vector2u` | `(1280, 720)` | Beginresolutie van het venster in pixels |
| `ClearColor` | `Color` | `RGB(18, 24, 32)` | Achtergrondkleur die elk frame vóór het renderen wordt toegepast |
| `FrameRateLimit` | `uint` | `60` | Maximum frames per seconde (`0` = onbeperkt) |
| `VerticalSyncEnabled` | `bool` | `true` | Verticale synchronisatie in- of uitschakelen |
| `ContentRootPath` | `string` | `AppContext.BaseDirectory` | Basismap voor het omzetten van relatieve asset-paden |

### `GameApplication`

| Methode | Parameters | Returntype | Beschrijving |
|---------|-----------|------------|--------------|
| `GameApplication(options)` | `GameApplicationOptions` | — | Maakt de applicatie aan met de opgegeven opties |
| `Run(initialScene)` | `GameSceneBase` | `void` | Maakt het SFML-venster aan, koppelt alle subsystemen en start de blokkerende hoofdlus |

### `GameHost`

| Lid | Type | Beschrijving |
|-----|------|--------------|
| `Window` | `RenderWindow` | Het actieve SFML-rendervenster |
| `Input` | `InputSnapshot` | Immutable snapshot van de toetsenbord- en muisstatus dit frame |
| `Assets` | `AssetStore` | Asset-loader en -cache |
| `Audio` | `AudioPlayer` | Geluidsbeheerder |
| `World` | `World` | De ECS-wereld voor deze sessie |
| `Camera` | `Camera2D` | De 2D-camera voor de huidige applicatie |
| `InputActions` | `InputActionMap?` | Optionele actiemap; toewijzen vanuit een scene of `Program.cs` |
| `Options` | `GameApplicationOptions` | Alleen-lezen engine-configuratie |
| `ExitRequested` | `bool` | `true` nadat `Exit()` is aangeroepen |
| `TimeScale` | `float` | Huidige tijdschaalfactor (`0` = gepauzeerd, `1` = normaal, `<1` = slow-motion). Standaard `1`. |
| `IsPaused` | `bool` | `true` wanneer `TimeScale` gelijk is aan `0` |
| `ChangeScene(scene)` | `GameSceneBase` → `void` | Zet een sceneovergang in de wachtrij; wist de ECS-wereld aan het volgende framegrens |
| `Pause()` | — → `void` | Stel `TimeScale` in op `0`, waardoor `GameTime.Delta` en `GameTime.Total` bevriezen |
| `Resume()` | — → `void` | Herstel `TimeScale` naar `1` |
| `SetTimeScale(value)` | `float` → `void` | Stel `TimeScale` in op `value` (minimaal 0) |
| `Exit()` | — → `void` | Geef het signaal aan de hoofdlus om te stoppen na het huidige frame |

### Gebruiksvoorbeeld

```csharp
GameApplicationOptions options = new()
{
    Title               = "Space Shooter",
    WindowSize          = new Vector2u(1920, 1080),
    FrameRateLimit      = 120,
    VerticalSyncEnabled = false,
    ClearColor          = new Color(5, 5, 20)
};

new GameApplication(options).Run(new TitleScene());
```

### `GameTime`

`GameTime` is een immutable `record struct` die alle timinginformatie voor het huidige frame
bevat. Het wordt geconstrueerd en opgehoogd door `GameApplication` en doorgegeven aan
`GameSceneBase.Update()` elk frame.

| Lid | Type | Beschrijving |
|-----|------|--------------|
| `Total` | `TimeSpan` | Totale geschaalde verstreken tijd since de applicatie is gestart (bevroren bij pauzeren) |
| `Delta` | `TimeSpan` | Geschaalde verstreken tijd voor het huidige frame (nul bij pauzeren) |
| `RawTotal` | `TimeSpan` | Ongeschaalde totale verstreken tijd; loopt altijd door ongeacht `TimeScale` |
| `RawDelta` | `TimeSpan` | Ongeschaalde verstreken tijd voor het huidige frame; loopt altijd door ongeacht `TimeScale` |
| `DeltaTotalSeconds` | `float` | Geschaald verstreken tijd voor het huidige frame als seconden; handig voor fysicaberekeningen (equivalent aan `(float)Delta.TotalSeconds`) |
| `RawDeltaTotalSeconds` | `float` | Ongeschaald verstreken tijd voor het huidige frame als seconden (equivalent aan `(float)RawDelta.TotalSeconds`) |
| `TimeScale` | `float` | De tijdschaalfactor die dit frame is toegepast |
| `IsPaused` | `bool` | `true` wanneer `TimeScale` gelijk is aan `0` |
| `FrameNumber` | `ulong` | Nulgebaseerde frameteller |
| `Start` | `static GameTime` | Nulgeïnitialiseerde schildwachtwaarde die de status vóór het eerste frame vertegenwoordigt |
| `Advance(rawDelta, timeScale)` | `TimeSpan`, `float = 1f` → `GameTime` | Geef een nieuwe `GameTime` terug, voortgezet met `rawDelta` geschaald door `timeScale` |

### Gebruiksvoorbeeld `GameTime`

```csharp
public override void Update(GameHost host, in GameTime gameTime)
{
    // DeltaTotalSeconds komt overeen met (float)gameTime.Delta.TotalSeconds.
    float dt = gameTime.DeltaTotalSeconds;

    // Animeer positie met totale verstreken tijd als sinusgolfinvoer
    float golf = MathF.Sin((float)gameTime.Total.TotalSeconds * 2f);
    _sprite.Position = new Vector2f(400f + golf * 100f, 300f);

    // Eenmalige initialisatielogica op het allereerste frame
    if (gameTime.FrameNumber == 0)
    {
        Console.WriteLine("Eerste frame gerenderd.");
    }
}
```

### `Camera2D`

`Camera2D` verpakt een SFML-`View` en biedt vloeiend volgen, zoomen, rotatie en screen-shake-effecten.
Deze wordt gemaakt en beheerd door `GameHost` en is beschikbaar via `host.Camera`. In een standaard
`GameApplication`-lus werkt de engine `UpdateShake(gameTime.RawDeltaTotalSeconds)` al bij en past
zij `GetView()` toe voordat de huidige scene rendert. Roep deze methoden alleen handmatig aan wanneer
je een zelfstandige `Camera2D`-instantie beheert of tijdelijk zelf de actieve view overschrijft.

| Lid | Type | Beschrijving |
|-----|------|--------------|
| `Position` | `Vector2f` | Wereldruimte-middenpositie van de camera |
| `Zoom` | `float` | Zoomniveau waarbij `1` = standaard, `>1` = inzoomen, `<1` = uitzoomen; minimaal `0.01` |
| `Rotation` | `float` | Camerarotatie in graden (met de klok mee) |
| `ViewportSize` | `Vector2f` | Afmetingen van de viewport (bij constructie ingesteld vanuit de venstergrootte) |
| `Follow(target, lerpSpeed, deltaTime)` | `Vector2f`, `float`, `float` → `void` | Verplaats de camera vloeiend naar `target` met lineaire interpolatie geschaald door `deltaTime` |
| `Shake(intensity, duration)` | `float`, `float` → `void` | Start een screen-shake-effect dat lineair afneemt over `duration` seconden |
| `UpdateShake(deltaTime)` | `float` → `void` | Werk de shake-timer bij en herbereken de shake-offset; `GameApplication` doet dit al voor `host.Camera` met ruwe/ongeschaalde tijd |
| `GetView()` | — → `View` | Geef een SFML-`View` terug die de huidige camerastatus weerspiegelt; gebruik dit wanneer je handmatig een view wilt toepassen of overschrijven |
| `Reset()` | — → `void` | Zet de camera terug naar de standaardstatus: gecentreerd, geen zoom, geen rotatie, geen shake |

### Gebruiksvoorbeeld

```csharp
public class GameplayScene : GameSceneBase
{
    private Vector2f _playerPos = new(400, 300);

    public override void Update(GameHost host, in GameTime gameTime)
    {
        // Follow the player with smooth interpolation
        host.Camera.Follow(_playerPos, lerpSpeed: 5f, deltaTime: gameTime.DeltaTotalSeconds);

        // Trigger a shake on collision
        if (DetectedCollision())
        {
            host.Camera.Shake(intensity: 8f, duration: 0.2f);
        }

        // Zoom control with mouse wheel
        host.Camera.Zoom += host.Input.MouseWheelDelta * 0.05f;
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        // Draw scene content here.
        // GameApplication already updated shake and applied host.Camera for this frame.
    }
}
```

### Scene Stack (Push / Pop)

`GameHost` biedt `ChangeScene`, `PushScene` en `PopScene` voor flexibel scenebeheer.

**ChangeScene:** Vervangt de volledige scene-stack door een nieuwe scene. De ECS-wereld wordt gewist.
Gebruik dit voor overgangen tussen verschillende speltoestanden (titel → gameplay → pauze).

**PushScene / PopScene:** Beheert een stack van scenes. Handig voor overlay-UI zoals pauzemenu's,
dialogen en HUD's. De onderliggende scene blijft bestaan en wordt pas ontladen wanneer deze wordt
gepopd of gewijzigd. Bij het pushen van een overlay:
- De huidige bovenste scene ontvangt `OnDeactivated()`
- De overlay ontvangt `Load()` en `OnActivated()`
- Bij het poppen:
- De overlay ontvangt `Unload()` en wordt opgeruimd
- De scene eronder ontvangt `OnActivated()`

| Methode | Beschrijving |
|---------|--------------|
| `ChangeScene(scene)` | Zet een volledige scene-overgang in de wachtrij; wist de volledige stack en ECS-wereld |
| `PushScene(overlay)` | Zet een scene in de wachtrij om boven op de huidige stack te worden gepusht (bijv. pauzemenu, dialoog) |
| `PopScene()` | Zet verwijdering van de bovenste scene in de wachtrij; gooit `InvalidOperationException` als er nog maar één scene bestaat |
| `SceneStackDepth` | Aantal scenes dat momenteel op de stack staat |

### Gebruiksvoorbeeld

```csharp
// Pause menu overlay
internal sealed class PauseMenuScene : GameSceneBase
{
    public override void Load(GameHost host)
    {
        // Pause the game when the menu opens
        host.Pause();
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        if (host.Input.WasKeyPressed(Keyboard.Key.Escape))
        {
            // Resume and close the pause menu
            host.Resume();
            host.PopScene();
        }

        if (host.Input.WasKeyPressed(Keyboard.Key.Q))
        {
            // Close pause menu and go back to title
            host.Resume();
            host.PopScene();
            host.ChangeScene(new TitleScene());
        }
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        // Draw semi-transparent overlay
        RectangleShape overlay = new(window.Size.ToSF())
        {
            FillColor = new Color(0, 0, 0, 128)
        };

        window.Draw(overlay);
        // Draw menu items...
    }
}
```

---

## Abstractions

### Overzicht

`IGameObject` en `IMovableGameObject` definiëren de minimale contracten die alle door de engine
beheerde objecten implementeren. Deze interfaces ontkoppelen de render- en update-pipeline van
concrete game-objecttypes.

### `IGameObject`

| Lid | Type | Beschrijving |
|-----|------|--------------|
| `Rotation` | `float` | Huidige rotatie in graden |
| `Origin` | `Vector2f` | Lokaal draaipunt (pivot voor rotatie en schaal) |
| `Position` | `Vector2f` | Positie in wereldruimte |
| `Scale` | `Vector2f` | Schaalfactor die op het object wordt toegepast |
| `ZOrder` | `int` | Tekenvolgorde; lagere waarden worden eerst getekend |
| `GlobalBounds` | `FloatRect` | As-uitgelijnde begrenzingsrechthoek in wereldruimte |
| `Draw(window)` | `RenderWindow` → `void` | Render het object naar het opgegeven venster |
| `Update(deltaTime)` | `float` → `void` | Verwerk per-frame-logica voor `deltaTime` seconden |

### `IMovableGameObject : IGameObject`

Breidt `IGameObject` uit met de fysica-eigenschappen die de `Behaviors`-laag nodig heeft.

| Lid | Type | Beschrijving |
|-----|------|--------------|
| `Mass` | `float` | Massa van het object, gebruikt om krachten te schalen |
| `Heading` | `Vector2f` | Eenheidsvector die de huidige kijkrichting weergeeft |
| `Side` | `Vector2f` | Linker loodlijn op `Heading` |
| `Velocity` | `Vector2f` | Huidige snelheidsvector |
| `Speed` | `float` | Scalaire snelheid (grootte van `Velocity`) |
| `Acceleration` | `float` | Acceleratiemagnitude |
| `Deceleration` | `float` | Passieve deceleratie (bijv. wrijving) |
| `BrakingDeceleration` | `float` | Actieve remvertraging |
| `MaxSpeed` | `float` | Maximale snelheid |
| `TurnRate` | `float` | Huidige draaisnelheid |
| `MaxTurnRate` | `float` | Maximale draaisnelheid per frame |

### Gebruiksvoorbeeld

```csharp
public sealed class Ship : IMovableGameObject
{
    // IGameObject
    public float    Rotation  { get; private set; }
    public Vector2f Origin    { get; } = new(16, 16);
    public Vector2f Position  { get; set; }
    public Vector2f Scale     { get; } = new(1, 1);
    public int      ZOrder    => 0;
    public FloatRect GlobalBounds => _sprite.GetGlobalBounds();

    // IMovableGameObject
    public float    Mass                { get; } = 1.0f;
    public Vector2f Heading             { get; private set; } = new(0, -1);
    public Vector2f Side                => new(-Heading.Y, Heading.X);
    public Vector2f Velocity            { get; set; }
    public float    Speed               => MathF.Sqrt(Velocity.X * Velocity.X + Velocity.Y * Velocity.Y);
    public float    Acceleration        { get; } = 200f;
    public float    Deceleration        { get; } = 60f;
    public float    BrakingDeceleration { get; } = 400f;
    public float    MaxSpeed            { get; } = 300f;
    public float    TurnRate            { get; set; }
    public float    MaxTurnRate         { get; } = 3.5f;

    public void Draw(RenderWindow window)
    {
        window.Draw(_sprite);
    }

    public void Update(float deltaTime)
    {
        /* pas snelheid toe op positie */
    }

    private readonly Sprite _sprite;

    public Ship(Texture texture)
    {
        _sprite = new Sprite(texture);
    }
}
```

---

## Scenes

### Overzicht

`GameSceneBase` is de abstracte basisklasse voor alle gameschermen. Overschrijf de virtuele
lifecycle-methoden en de abstracte methode `RenderLayer` om assets te laden, logica bij te werken en te
tekenen. Sceneovergangen worden aangevraagd via `GameHost.ChangeScene()` en treden veilig in werking
aan het begin van het volgende frame.

Elke scene kan een of meer `ISceneLayer`-objecten bevatten die worden gecomponeerd rondom de
eigen inhoud van de scene in een gedefinieerde renderingsvolgorde. Gebruik lagen om achtergrondvisualisaties
(scrollende achtergronden, parallax-tegels) of voorgrondvisualisaties (HUD, vigneteffecten) toe te voegen
zonder die logica te koppelen aan de scene zelf.

### `GameSceneBase`

| Lid | Parameters | Returntype | Beschrijving |
|-----|-----------|------------|--------------|
| `Name` | — | `string` | Scene-identifier; standaard de naam van de runtime-klasse |
| `Load(host)` | `GameHost` | `void` | Wordt eenmalig aangeroepen wanneer de scene actief wordt; laad hier assets |
| `Unload(host)` | `GameHost` | `void` | Wordt eenmalig aangeroepen wanneer de scene wordt gedeactiveerd; geef hier per-scene-resources vrij |
| `Update(host, gameTime)` | `GameHost`, `in GameTime` | `void` | Wordt elk frame vóór `RenderLayer` aangeroepen; verwerk hier spellogica |
| `RenderLayer(host, window)` *(abstract)* | `GameHost`, `RenderWindow` | `void` | Implementeer deze methode voor de draw-calls van de scene |
| `Render(host, window)` | `GameHost`, `RenderWindow` | `void` | Rendert alle lagen en de eigen inhoud van de scene. Roept eerst lagen met negatieve RenderOrder aan, dan RenderLayer, daarna niet-negatieve lagen |
| `AddLayer(layer)` *(protected)* | `ISceneLayer` | `void` | Registreer een laag bij deze scene; roep aan vanuit de constructor of `Load` |

> **Veiligheid bij overgangen:** Wanneer `host.ChangeScene(new VolgendeScene())` wordt aangeroepen,
> wordt `Unload` van de huidige scene uitgevoerd, wordt de ECS-`World` gewist en wordt `Load` van
> de nieuwe scene aangeroepen — allemaal aan het begin van het volgende frame, zodat het huidige
> frame altijd netjes wordt afgerond.


### `ISceneLayer`

| Lid | Parameters | Returntype | Beschrijving |
|-----|-----------|------------|--------------|
| `RenderOrder` | — | `int` | Renderpositie ten opzichte van de scene; negatief = achtergrond, nul/positief = voorgrond |
| `Load(host)` | `GameHost` | `void` | Wordt eenmalig aangeroepen wanneer de eigenaarscene wordt geladen |
| `Unload(host)` | `GameHost` | `void` | Wordt eenmalig aangeroepen wanneer de eigenaarscene wordt verwijderd |
| `Update(host, gameTime)` | `GameHost`, `in GameTime` | `void` | Wordt elk frame aangeroepen (hetzelfde frame als de eigenaarscene) |
| `RenderLayer(host, window)` | `GameHost`, `RenderWindow` | `void` | Wordt elk frame aangeroepen op de positie bepaald door `RenderOrder` |
| `OnActivated(host)` | `GameHost` | `void` | Wordt aangeroepen wanneer de eigenaarscene de bovenste op de stack wordt (standaard: no-op) |
| `OnDeactivated(host)` | `GameHost` | `void` | Wordt aangeroepen wanneer een andere scene bovenop de eigenaarscene wordt gepusht (standaard: no-op) |

> **Levenscyclusdoorgave:** `OnActivated` en `OnDeactivated` hebben standaard lege implementaties.
> De engine stuurt beide gebeurtenissen automatisch door naar elke geregistreerde laag via de interne
> `ActivateInternal`- / `DeactivateInternal`-methoden van de scene — zelfs als de eigenaarscene
> de virtuele `GameSceneBase.OnActivated` / `OnDeactivated` overschrijft zonder `base` aan te roepen.
> Overschrijf deze methoden in een laag wanneer die moet reageren op het pushen of poppen van de scene
> (bijvoorbeeld het stoppen van een timer in een pauze-overlay).

> **Renderingsvolgorde:** Lagen met `RenderOrder < 0` zijn *achtergrond*lagen en worden getekend vóór de
> eigen `RenderLayer`-aanroep van de scene. Lagen met `RenderOrder >= 0` zijn *voorgrond*lagen en worden
> daarna getekend. Meerdere lagen worden oplopend gesorteerd op `RenderOrder`; lagen met dezelfde waarde
> bewaren de volgorde waarin ze zijn geregistreerd.
>
> **Parameters:** Geef data aan lagen door via hun constructors. De eigenaarscene maakt de
> laagobjecten aan en injecteert afhankelijkheden op aanmaakmogelijkheden.

### Gebruiksvoorbeeld

```csharp
// Achtergrondlaag — scrollend sterrenveld, getekend achter de scene
internal sealed class StarfieldLayer : ISceneLayer
{
    public int RenderOrder => -10; // achtergrond

    public void Load(GameHost host) { }
    public void Unload(GameHost host) { }
    public void Update(GameHost host, in GameTime gameTime) { }

    public void RenderLayer(GameHost host, RenderWindow window)
    {
        // teken scrollende sterren ...
    }
}

// Voorgrondlaag — HUD, getekend bovenop de scene
internal sealed class HudLayer : ISceneLayer
{
    private readonly Font _font;

    // Parameters worden doorgegeven door de eigenaarscene via de constructor
    public HudLayer(Font font) => _font = font;

    public int RenderOrder => 10; // voorgrond

    public void Load(GameHost host) { }
    public void Unload(GameHost host) { }

    public void Update(GameHost host, in GameTime gameTime)
    {
        // ... HUD per-frame logica ...
    }

    public void RenderLayer(GameHost host, RenderWindow window)
    {
        // teken HUD ...
    }
}

// Scene die beide lagen bezit
internal sealed class GameplayScene : GameSceneBase
{
    private Font? _font;

    public GameplayScene()
    {
        AddLayer(new StarfieldLayer()); // achtergrondlaag registreren vóór Load
    }

    public override void Load(GameHost host)
    {
        _font = host.Assets.LoadFont();
        AddLayer(new HudLayer(_font)); // voorgrondlaag na het laden van het lettertype registreren
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        // ... spellogica ...
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        // ... draw-calls voor de scene zelf ...
    }
}
```

---

## ECS

### Overzicht

De ECS-module (Entity-Component-System) biedt een lichtgewicht, woordenboek-gebaseerde wereld
waarbij entiteiten gewone integer-ID's zijn en componenten worden opgeslagen in per-type sparse
sets. De module ondersteunt enkelvoudige en duale componentquery's en wordt automatisch gereset
bij elke sceneovergang.

### `Entity`

`Entity` is een `record struct` die één `int Id` omhult.

| Lid | Beschrijving |
|-----|--------------|
| `Id` | Unieke integer-identificatie voor deze entiteit |
| `ToString()` | Geeft `"Entity(n)"` terug |

### `World`

| Methode | Parameters | Returntype | Beschrijving |
|---------|-----------|------------|--------------|
| `CreateEntity()` | — | `Entity` | Wijs een nieuwe entiteit toe met een uniek ID |
| `Exists(entity)` | `Entity` | `bool` | Geeft `true` terug als de entiteit niet is vernietigd |
| `DestroyEntity(entity)` | `Entity` | `void` | Verwijder de entiteit en al haar bijgevoegde componenten |
| `Clear()` | — | `void` | Vernietig alle entiteiten en componenten in de wereld |
| `AddComponent<T>(entity, component)` | `Entity`, `T` | `void` | Koppel een component van type `T` aan de entiteit |
| `RemoveComponent<T>(entity)` | `Entity` | `bool` | Ontkoppel het component; geeft `false` terug als de entiteit er geen heeft |
| `TryGetComponent<T>(entity, out component)` | `Entity`, `out T` | `bool` | Haal een component veilig op; geeft `false` terug bij afwezigheid |
| `GetComponent<T>(entity)` | `Entity` | `T` | Haal een component op; gooit een uitzondering als de entiteit er geen heeft |
| `Query<T>()` | — | `IEnumerable<Entity>` | Doorloop alle entiteiten met componenttype `T` |
| `Query<TA, TB>()` | — | `IEnumerable<Entity>` | Doorloop alle entiteiten met zowel `TA` als `TB` |
| `Query<TA, TB, TC>()` | — | `IEnumerable<Entity>` | Doorloop alle entiteiten met `TA`, `TB` én `TC` |

> **Implementatienoot:** Componentopslag wordt lazy aangemaakt bij de eerste `AddComponent<T>`-aanroep
> en is gebaseerd op `Dictionary<int, TComponent>` sparse sets.

### Gebruiksvoorbeeld

```csharp
// Definieer componenten als record structs
record struct Position(float X, float Y);
record struct Velocity(float X, float Y);
record struct Health(int Current, int Max);

World world = host.World;

// Maak entiteiten aan en koppel componenten
Entity player = world.CreateEntity();
world.AddComponent(player, new Position(100, 200));
world.AddComponent(player, new Velocity(0, 0));
world.AddComponent(player, new Health(100, 100));

Entity enemy = world.CreateEntity();
world.AddComponent(enemy, new Position(400, 300));
world.AddComponent(enemy, new Health(50, 50));

// Per-frame beweging: alle entiteiten met Position én Velocity bijwerken
float dt = (float)gameTime.Delta.TotalSeconds;

foreach (Entity e in world.Query<Position, Velocity>())
{
    Position pos = world.GetComponent<Position>(e);
    Velocity vel = world.GetComponent<Velocity>(e);
    world.AddComponent(e, pos with { X = pos.X + vel.X * dt, Y = pos.Y + vel.Y * dt });
}

// Veilige opvraging met null-check-patroon
if (world.TryGetComponent<Health>(player, out Health hp) && hp.Current <= 0)
{
    world.DestroyEntity(player);
}
```

---

## Input

### Overzicht

`InputSnapshot` is een frame-scoped snapshot van alle toetsenbord- en muisinvoer die tijdens één frame
is vastgelegd. De engine werkt deze elke frame in-place bij om allocaties te vermijden, maar scenes
moeten deze als alleen-lezen behandelen binnen één frame. De interne `InputTracker` wordt automatisch
door `GameApplication` gekoppeld. Scenes lezen altijd de huidige snapshot via `host.Input`.

### `InputSnapshot`

| Lid | Type | Beschrijving |
|-----|------|--------------|
| `Empty` | `static InputSnapshot` | Een snapshot zonder ingedrukte toetsen of knoppen; nuttig voor tests en beginstatus |
| `CurrentKeys` | `IReadOnlySet<Keyboard.Key>` | Toetsen die dit frame ingedrukt zijn |
| `PreviousKeys` | `IReadOnlySet<Keyboard.Key>` | Toetsen die het vorige frame ingedrukt waren |
| `CurrentButtons` | `IReadOnlySet<Mouse.Button>` | Muisknoppen die dit frame ingedrukt zijn |
| `PreviousButtons` | `IReadOnlySet<Mouse.Button>` | Muisknoppen die het vorige frame ingedrukt waren |
| `MousePosition` | `Vector2i` | Cursorpositie in venstercoördinaten |
| `MouseWheelDelta` | `float` | Scrollwieldelta verzameld dit frame |
| `IsKeyDown(key)` | `Keyboard.Key` → `bool` | `true` zolang de toets ingedrukt is |
| `WasKeyPressed(key)` | `Keyboard.Key` → `bool` | `true` op het exacte frame waarop de toets voor het eerst werd ingedrukt |
| `WasAnyKeyPressed()` | — → `bool` | `true` als een willekeurige toets dit frame werd ingedrukt |
| `WasKeyReleased(key)` | `Keyboard.Key` → `bool` | `true` op het exacte frame waarop de toets werd losgelaten |
| `IsMouseButtonDown(button)` | `Mouse.Button` → `bool` | `true` zolang de knop ingedrukt is |
| `WasMouseButtonPressed(button)` | `Mouse.Button` → `bool` | `true` op het exacte frame waarop de knop voor het eerst werd ingedrukt |
| `WasMouseButtonReleased(button)` | `Mouse.Button` → `bool` | `true` op het exacte frame waarop de knop werd losgelaten |

### Gebruiksvoorbeeld

```csharp
public override void Update(GameHost host, in GameTime gameTime)
{
    InputSnapshot input = host.Input;
    // Gebruik de DeltaTotalSeconds eigenschap als snelle koppeling voor: (float)gameTime.Delta.TotalSeconds    
    float dt = (float)gameTime.DeltaTotalSeconds;

    // Doorlopende beweging zolang toetsen ingedrukt zijn
    if (input.IsKeyDown(Keyboard.Key.W))
    {
        _position.Y -= _speed * dt;
    }

    if (input.IsKeyDown(Keyboard.Key.S))
    {
        _position.Y += _speed * dt;
    }

    if (input.IsKeyDown(Keyboard.Key.A))
    {
        _position.X -= _speed * dt;
    }

    if (input.IsKeyDown(Keyboard.Key.D))
    {
        _position.X += _speed * dt;
    }

    // Éénmalige acties
    if (input.WasKeyPressed(Keyboard.Key.Space))
    {
        SchietKogel();
    }

    if (input.WasMouseButtonPressed(Mouse.Button.Left))
    {
        SpawnDeeltje(input.MousePosition);
    }

    // Scrollwiel voor zoom
    _zoom += input.MouseWheelDelta * 0.1f;
}
```

---

## Assets

### Overzicht

`AssetStore` is de centrale cache voor alle engine-resources: afbeeldingen, texturen, lettertypen,
geluidsbuffers en GLSL-shaders. Alle assets worden gekoppeld aan hun relatieve pad en lazy geladen
bij eerste toegang. Mislukte shader-laadpogingen worden ook gecacht zodat defecte shaders niet
elk frame opnieuw worden gecompileerd.

### `AssetStore`

| Methode / Eigenschap | Parameters | Returntype | Beschrijving |
|----------------------|-----------|------------|--------------|
| `ContentRootPath` | — | `string` | Absolute basismap voor het omzetten van asset-paden |
| `ResolvePath(assetPath)` | `string` | `string` | Zet een relatief asset-pad om naar een absoluut bestandspad |
| `LoadImage(assetPath)` | `string` | `Image` | Laad en cachet een SFML-`Image`; ondersteunt PPM P3/P6 en alle SFML-native indelingen |
| `LoadTexture(assetPath, smooth)` | `string`, `bool = false` | `Texture` | Laad en cachet een SFML-`Texture`; `smooth: true` schakelt bilineaire filtering in |
| `LoadFont(assetPath)` | `string? = null` | `Font` | Laad en cachet een lettertype; valt terug op een systeemlettertype bij `null` |
| `LoadSoundBuffer(assetPath)` | `string` | `SoundBuffer` | Laad en cachet een SFML-`SoundBuffer` |
| `LoadShader(fragAssetPath)` | `string` | `Shader` | Laad een fragment-only GLSL-shader; gooit een uitzondering bij een fout |
| `LoadShader(vertAssetPath, fragAssetPath)` | `string`, `string` | `Shader` | Laad een vertex + fragment shader-paar; gooit een uitzondering bij een fout |
| `TryLoadShader(fragAssetPath, out failureReason)` | `string`, `out string?` | `Shader?` | Laad een fragment-shader; geeft `null` met een beschrijvend bericht bij een fout |
| `TryLoadShader(vertAssetPath, fragAssetPath, out failureReason)` | `string`, `string`, `out string?` | `Shader?` | Laad een shader-paar; geeft `null` met een beschrijvend bericht bij een fout |
| `Unload(assetPath)` | `string` | `void` | Geef een gecacht asset vrij en verwijder het uit de cache; geen effect als het pad nooit geladen is |
| `UnloadAll()` | — | `void` | Geef alle gecachte assets vrij en maak de cache leeg (texturen, fonts, geluiden, shaders) |
| `Dispose()` | — | `void` | Geef alle gecachte SFML-resources vrij |

> **PPM-ondersteuning:** ASCII (P3) en binaire (P6) PPM-afbeeldingen worden native gedecodeerd,
> inclusief 16-bits kanaaldiepte. Hierdoor kunnen procedurele texturen worden geladen zonder een
> externe afbeeldingsbibliotheek.

### `AssetPathResolver` (statisch)

| Methode | Parameters | Returntype | Beschrijving |
|---------|-----------|------------|--------------|
| `NormalizeContentRoot(contentRoot)` | `string` | `string` | Normaliseert het content-rootpad naar een absoluut pad |
| `ResolvePath(contentRoot, assetPath)` | `string`, `string` | `string` | Combineert een basismap en een relatief pad tot een absoluut pad |

### `SystemFont` (statisch)

| Methode | Returntype | Beschrijving |
|---------|------------|--------------|
| `FindSystemFont()` | `string` | Zoek een geschikt systeemlettertype. Zoekt naar Segoe UI / Arial op Windows, Arial / Helvetica op macOS en DejaVu Sans / Liberation Sans op Linux. Gooit een `FileNotFoundException` als er geen lettertype wordt gevonden. |

### Gebruiksvoorbeeld

```csharp
public override void Load(GameHost host)
{
    // Laad een textuur (herhaalde aanroepen met hetzelfde pad leveren de gecachte instantie op)
    _texture = host.Assets.LoadTexture("sprites/hero.png", smooth: true);

    // Laad het standaard systeemlettertype
    _font = host.Assets.LoadFont();

    // Laad een aangepast lettertype vanuit de content-root
    _titleFont = host.Assets.LoadFont("fonts/PressStart2P.ttf");

    // Laad een geluidseffect
    _jumpBuffer = host.Assets.LoadSoundBuffer("sounds/jump.wav");

    // Probeer een fragment-shader te laden met graceful fallback
    if (host.Assets.TryLoadShader("shaders/bloom.frag", out string? reason) is { } shader)
    {
        _bloomShader = shader;
    }
    else
    {
        Console.Error.WriteLine($"Shader niet beschikbaar: {reason}");
    }
}
```

---

## Audio

### Overzicht

`AudioPlayer` beheert een verzameling actieve `Sound`-instanties. Het speelt geluiden op aanvraag
af, houdt ze intern bij en verwijdert voltooide geluiden elk frame om resourcelekken te voorkomen.
Roep `Update()` één keer per frame aan om de opruiming te laten draaien.

### `AudioPlayer`

| Methode | Parameters | Returntype | Beschrijving |
|---------|-----------|------------|--------------|
| `PlaySound(assetPath, volume, loop)` | `string`, `float = 100f`, `bool = false` | `Sound` | Laad het geluid via `AssetStore` en speel het onmiddellijk af; geeft de actieve `Sound`-instantie terug |
| `StopAll()` | — | `void` | Stop en geef alle actieve geluiden onmiddellijk vrij |
| `Dispose()` | — | `void` | Geef alle beheerde geluidsresources vrij |

### Gebruiksvoorbeeld

```csharp
public override void Update(GameHost host, in GameTime gameTime)
{
    if (host.Input.WasKeyPressed(Keyboard.Key.Space))
    {
        // Speel een eenmalig geluidseffect af op 80% volume
        host.Audio.PlaySound("sounds/laser.wav", volume: 80f);
    }

    if (host.Input.WasKeyPressed(Keyboard.Key.M))
    {
        // Start achtergrondmuziek met herhaling en bewaar een referentie om later te stoppen
        _music = host.Audio.PlaySound("music/theme.ogg", volume: 50f, loop: true);
    }
}
```

---

## Animation

### Overzicht

Het animatiesysteem ondersteunt frame-voor-frame sprite-animatie op basis van verstreken tijd. Een
`AnimationClip` bevat een reeks `AnimationFrame`-records, opgebouwd vanuit een horizontaal
sprite-sheet of een lijst van losse texturen. `AnimationPlayer` stuurt de afspeling aan en rendert
elk frame het huidige frame naar het venster.

### `AnimationFrame`

`AnimationFrame` is een `record struct` met twee velden:

| Veld | Type | Beschrijving |
|------|------|--------------|
| `Duration` | `TimeSpan` | Hoe lang dit frame wordt getoond vóór het doorgaan naar het volgende |
| `Texture` | `Texture` | De SFML-textuur die voor dit frame wordt gerenderd |

### `AnimationClip`

| Lid | Parameters | Returntype | Beschrijving |
|-----|-----------|------------|--------------|
| `Name` | — | `string` | Identifier voor de clip |
| `Frames` | — | `IReadOnlyList<AnimationFrame>` | Geordende lijst van animatieframes |
| `CreateFromImage(name, image, frameWidth, frameHeight, frameCount, frameDuration)` | `string`, `Image`, `uint`, `uint`, `uint`, `TimeSpan` | `AnimationClip` | Snijd een horizontaal sprite-sheet in gelijkmatig verdeelde frames, te beginnen bij (0, 0) |
| `CreateFromImage(name, image, frameWidth, frameHeight, frameCount, frameDuration, startX, startY)` | …, `uint`, `uint` | `AnimationClip` | Hetzelfde, maar leest frames vanaf een pixeloffset — gebruik `startY = rijIndex * frameHeight` om een rij in een sprite-sheet met meerdere rijen te selecteren |
| `CreateFromTextures(name, textures, frameDuration)` | `string`, `IReadOnlyList<Texture>`, `TimeSpan` | `AnimationClip` | Bouw een clip van een expliciete lijst met vooraf geladen texturen |
| `Dispose()` | — | `void` | Geef alle frametexturen vrij |

### `AnimationPlayer`

| Lid | Parameters | Returntype | Beschrijving |
|-----|-----------|------------|--------------|
| `IsFinished` | — | `bool` | `true` wanneer een niet-herhalende clip zijn laatste frame heeft afgespeeld |
| `IsLooping` | — | `bool` | `true` wanneer de speler is ingesteld om de clip te herhalen |
| `IsPaused` | — | `bool` | `true` wanneer de afspeling halverwege is bevroren |
| `Position` | — | `Vector2f` | Wereldpositie waarop de animatie wordt gerenderd |
| `Scale` | — | `Vector2f` | Schaalfactor die op elk frame wordt toegepast |
| `Rotation` | — | `float` | Rotatie in graden die op elk frame wordt toegepast |
| `FrameIndex` | — | `int` | Nulgebaseerde index van het momenteel getoonde frame |
| `Play()` | — | `void` | Start of hervat de afspeling; wist ook `IsPaused` |
| `Pause()` | — | `void` | Bevriest op het huidige frame zonder het terug te spoelen |
| `Resume()` | — | `void` | Hervat de afspeling na een `Pause()`-aanroep |
| `Reset()` | — | `void` | Spoel terug naar frame 0 |
| `Update(delta)` | `TimeSpan` | `void` | Verwerk de afspeling met `delta`; gaat naar het volgende frame wanneer de duur van het huidige frame verstreken is |
| `Render(window)` | `RenderWindow` | `void` | Teken het huidige frame naar het venster |
| `Dispose()` | — | `void` | Geef spelerresources vrij |

### Gebruiksvoorbeeld

```csharp
private AnimationClip?   _runClip;
private AnimationPlayer? _player;

public override void Load(GameHost host)
{
    // Bouw een clip van een 4-frame horizontaal sprite-sheet (64 × 64 px per frame)
    Image sheet = host.Assets.LoadImage("sprites/run_sheet.png");
    _runClip = AnimationClip.CreateFromImage(
        name:          "rennen",
        image:         sheet,
        frameWidth:    64,
        frameHeight:   64,
        frameCount:    4,
        frameDuration: TimeSpan.FromMilliseconds(100)
    );

    _player = new AnimationPlayer(_runClip, isLooping: true, autoPlay: true)
    {
        Position  = new Vector2f(200, 300)
    };
}

public override void Update(GameHost host, in GameTime gameTime)
{
    _player!.Update(gameTime.Delta);
}

public override void Render(GameHost host, RenderWindow window)
{
    _player!.Render(window);
}
```

---

## Behaviors

### Overzicht

De `Behaviors`-namespace biedt samenstelbare, strategie-gebaseerde bewegingscomponenten voor
game-agenten. `MovementBehavior` en `RotationBehavior` verwerken door de speler aangestuurde
voortbeweging. `SteeringBehavior` implementeert een volledige reeks autonome steering-forces voor
AI-agenten. `SteeringForces` biedt statische hulpmethoden voor het veilig combineren van meerdere
steering-forces. Alle gedragingen worden geconstrueerd met een `IMovableGameObject` en werken elk
frame op de eigenschappen ervan.

### `RotationDirection` (enum)

| Waarde | Integer | Beschrijving |
|--------|---------|--------------|
| `Default` | `0` | Neemt het kortste hoekpad naar het doel |
| `Clockwise` | `1` | Rotatie met de klok mee |
| `Counterclockwise` | `-1` | Rotatie tegen de klok in |

### `RotationBehavior`

Construeer met een `IMovableGameObject`. Stel de draaivlaggen in vóór het aanroepen van
`UpdateRotation` elk frame.

| Lid | Beschrijving |
|-----|--------------|
| `IsTurningLeft` | Stel in op `true` om dit frame een draai tegen de klok in toe te passen |
| `IsTurningRight` | Stel in op `true` om dit frame een draai met de klok mee toe te passen |
| `UpdateRotation(deltaTime, ref heading)` | Pas massa-geschaalde rotatie toe begrensd tot `MaxTurnRate`; werkt `heading` ter plekke bij; geeft de nieuwe rotatiehoek in graden terug |

### `MovementBehavior`

Construeer met een `IMovableGameObject`. Stel de bewegingsvlaggen in vóór het aanroepen van
`UpdateMovement` elk frame.

| Lid | Beschrijving |
|-----|--------------|
| `IsMovingForwards` | Versnellen in de huidige kijkrichting |
| `IsMovingBackwards` | Versnellen tegenovergesteld aan de huidige kijkrichting |
| `IsBraking` | Remvertraging toepassen langs de huidige snelheidsrichting zonder om te keren |
| `IsStrafingLeft` | Versnellen langs de linker zijvector, onafhankelijk van de kijkrichting |
| `IsStrafingRight` | Versnellen langs de rechter zijvector, onafhankelijk van de kijkrichting |
| `UpdateMovement(deltaTime, currentVelocity, currentPosition)` | Pas versnelling, passieve vertraging en remmen toe; geeft de nieuwe snelheidsvector `Vector2f` terug |

### `MovementWithDriftingBehavior`

Combineert `RotationBehavior` en `MovementBehavior`. Draaien leidt momentum niet direct om —
het object drijft als een ruimtevaartuig.

| Lid | Beschrijving |
|-----|--------------|
| `IsMovingForwards` | Voorwaartse stuwkracht toepassen |
| `IsBraking` | Remvertraging toepassen zonder het huidige momentum om te leiden |
| `IsTurningLeft` | Links draaien zonder snelheid om te leiden |
| `IsTurningRight` | Rechts draaien zonder snelheid om te leiden |
| `Update(deltaTime, ref rotation, ref heading)` | Verwerk beweging en rotatie; geeft de nieuwe snelheidsvector `Vector2f` terug |

### `MovementWithRotationBehavior`

Snelheid is altijd uitgelijnd met de kijkrichting; geen drift. De agent kan niet achteruit rijden —
remmen vertraagt alleen tot stilstand.

| Lid | Beschrijving |
|-----|--------------|
| `IsMovingForwards` | Voorwaartse versnelling toepassen |
| `IsBraking` | Remvertraging toepassen |
| `IsTurningLeft` | Links draaien; snelheidsrichting wordt onmiddellijk bijgewerkt |
| `IsTurningRight` | Rechts draaien; snelheidsrichting wordt onmiddellijk bijgewerkt |
| `Update(deltaTime, ref rotation, ref heading)` | Verwerk beweging en rotatie; geeft de nieuwe snelheidsvector `Vector2f` terug |

### `SteeringBehavior`

Construeer met een `IMovableGameObject`. Elke methode geeft een `Vector2f`-stuurkracht terug.

#### Integratiepatroon

Steering-forces worden toegepast met semi-impliciete Euler-integratie. Pas dit patroon elk frame toe:

```csharp
// 1. Bereken individuele krachten
Vector2f wander    = _steering.Wander(ref _wanderAngle, 80f, 200f);
Vector2f wallForce = _steering.WallAvoidance(_walls, 120f, 45f);
Vector2f bounds    = _steering.StayWithinBounds(_boundary, 50f);

// 2. Combineer tot één kracht (zie SteeringForces hieronder)
Vector2f totalForce = SteeringForces.WeightedSum(
    _agent.MaxSpeed,
    (wander, 1f), (bounds, 2f), (wallForce, 4f));

// 3. Integreer: kracht → versnelling → snelheid → positie
_velocity += (totalForce / _agent.Mass) * deltaTime;
_velocity  = _velocity.Truncate(_agent.MaxSpeed);
_position += _velocity * deltaTime;

// 4. Houd kijkrichting uitgelijnd met snelheid
_steering.UpdateHeadingWhileMoving(deltaTime, ref _rotation);
```

#### Basiskrachten

| Methode | Parameters | Beschrijving |
|---------|-----------|--------------|
| `Seek(targetPosition)` | `Vector2f` | Stuur naar het doel op maximale snelheid |
| `Flee(targetPosition)` | `Vector2f` | Stuur weg van het doel op maximale snelheid |
| `Arrive(targetPosition, slowingRadius)` | `Vector2f`, `float` | Zoeken met een vertraagzone; de agent vertraagt bij het betreden van de vertraagstraal |

#### Voorspellingskrachten

| Methode | Parameters | Beschrijving |
|---------|-----------|--------------|
| `Pursue(target)` | `IMovableGameObject` | Leid het doel door zijn toekomstige positie te voorspellen op basis van de huidige snelheid |
| `Evade(target)` | `IMovableGameObject` | Vlucht van de voorspelde toekomstige positie van het doel |

#### Patroondynamiek

| Methode | Parameters | Beschrijving |
|---------|-----------|--------------|
| `Wander(ref wanderAngle, wanderRadius, wanderDistance)` | `ref float`, `float`, `float` | Produceer vloeiende pseudo-willekeurige dwaling door een hoek te jitteren op een geprojecteerde cirkel voor de agent |

**Parameters voor Wander:**

| Parameter | Typische waarde | Effect |
|-----------|----------------|--------|
| `wanderAngle` | `ref float` per agent | Bewaart de wandel-toestand tussen frames; initialiseer op `0f` |
| `wanderRadius` | `50–100` | Straal van de geprojecteerde wandelcirkel; groter = bredere bochten |
| `wanderDistance` | `100–250` | Afstand van de wandelcirkel voor de agent; groter = vloeiender gedrag |

#### Obstakel- en wandvermijding

| Methode | Parameters | Beschrijving |
|---------|-----------|--------------|
| `ObstacleAvoidance(obstacles, detectionLength, agentRadius)` | `IReadOnlyList<IGameObject>`, `float`, `float` | Projecteer een detectiebox voor de agent en stuur weg van het dichtstbijzijnde cirkelvormige obstakel |
| `WallAvoidance(walls, feelerLength, feelerAngle)` | `IReadOnlyList<Wall>`, `float`, `float` | Projecteer drie voelers en stuur weg van het dichtstbijzijnde wandsnijpunt |
| `StayWithinBounds(boundary, margin)` | `FloatRect`, `float` | Geef een centrerende kracht terug wanneer de agent de grensranden nadert |

**Handleiding voor wandinstelling:**

Een `Wall` is een gericht lijnsegment. De normaal is de *linker loodlijn* van de Start→Eind-richting —
wat betekent dat het **eenzijdig** is: de afstotingskracht is alleen actief wanneer de agent de wand
benadert van de voorzijde (de zijde waarnaar de normaal wijst).

Voor **randwanden** is dit eenvoudig — richt elke wand zo dat de normaal naar binnen wijst:

```csharp
float w = 1280f, h = 720f;

// Normaal wijst rechts  (naar binnen voor linkerrand)
Wall left   = new(new Vector2f(0, 0), new Vector2f(0, h));
// Normaal wijst links   (naar binnen voor rechterrand)
Wall right  = new(new Vector2f(w, h), new Vector2f(w, 0));
// Normaal wijst omlaag  (naar binnen voor bovenrand)
Wall top    = new(new Vector2f(w, 0), new Vector2f(0, 0));
// Normaal wijst omhoog  (naar binnen voor onderrand)
Wall bottom = new(new Vector2f(0, h), new Vector2f(w, h));
```

Voor **interne wanden** die agenten van beide kanten kunnen benaderen, registreer de wand **twee keer**
met Begin↔Eind omgewisseld zodat beide zijden een actieve normaal hebben:

```csharp
// Diagonale interne wand — zichtbaar van beide kanten
Wall diag1 = new(new Vector2f(400f, 200f), new Vector2f(600f, 400f));
Wall diag2 = new(new Vector2f(600f, 400f), new Vector2f(400f, 200f)); // omgekeerd

List<Wall> walls = [left, right, top, bottom, diag1, diag2];
```

#### Meervoudige-agentkrachten

| Methode | Parameters | Beschrijving |
|---------|-----------|--------------|
| `OffsetPursuit(leader, offset, slowingRadius)` | `IMovableGameObject`, `Vector2f`, `float` | Volg een leider terwijl een vaste offset in de lokale ruimte van de leider wordt gehandhaafd |
| `Interpose(object1, object2)` | `IMovableGameObject`, `IMovableGameObject` | Positioneer de agent op het middelpunt tussen twee bewegende objecten |
| `Hide(target, obstacles, distanceFromBoundary, threatDistance)` | `IMovableGameObject`, `IReadOnlyList<IGameObject>`, `float`, `float` | Beweeg achter het dichtstbijzijnde obstakel om de dreiging te ontwijken |
| `FollowPath(ref pathIndex, pathToFollow, slowingRadius)` | `ref int`, `IReadOnlyList<Vector2f>`, `float` | Kom aan bij opeenvolgende waypoints; verhoog automatisch de padindex |

#### Groepsgedrag (Flocking)

| Methode | Parameters | Beschrijving |
|---------|-----------|--------------|
| `Separation(neighbors, separationRadius)` | `IReadOnlyList<IMovableGameObject>`, `float` | Stuw de agent weg van nabijgelegen buren |
| `Alignment(neighbors)` | `IReadOnlyList<IMovableGameObject>` | Stuur om de gemiddelde koers van de buurt te evenaren |
| `Cohesion(neighbors)` | `IReadOnlyList<IMovableGameObject>` | Zoek het zwaartepunt van alle buren |

#### Hulpmethoden

| Methode | Parameters | Beschrijving |
|---------|-----------|--------------|
| `UpdateHeadingWhileMoving(deltaTime, ref rotation)` | `float`, `ref float` | Houd `Heading` uitgelijnd met de huidige snelheidsrichting wanneer de agent beweegt |

### `SteeringForces`

De statische klasse `SteeringForces` biedt twee strategieën voor het combineren van meerdere
steering-force-vectoren tot één vector die gegarandeerd `maxForce` niet overschrijdt.

| Methode | Parameters | Returntype | Beschrijving |
|---------|-----------|------------|--------------|
| `WeightedSum(maxForce, params entries)` | `float`, `ReadOnlySpan<(Vector2f Force, float Weight)>` | `Vector2f` | Schaal elke kracht met zijn gewicht, sommeer alle bijdragen en begrenzen tot `maxForce` |
| `PriorityTruncated(budget, params forces)` | `float`, `ReadOnlySpan<Vector2f>` | `Vector2f` | Vul het budget met krachten in de opgegeven volgorde; stop wanneer het budget uitgeput is |

#### Een strategie kiezen

| Scenario | Aanbevolen strategie | Reden |
|----------|---------------------|-------|
| Vermijding + dwalen (krachten kunnen tegengesteld zijn) | `WeightedSum` | Hogere gewichten op veiligheidskrachten zorgen ervoor dat ze domineren, zelfs als dwalen de verkeerde kant op trekt |
| Puur groepsgedrag (separatie + uitlijning + cohesie) | `WeightedSum` | Voorspelbare menging ongeacht individuele krachtgrootten |
| Strikt onafhankelijke, budgetvullende krachten | `PriorityTruncated` | Garandeert dat hoge-prioriteitskrachten als eerste worden vervuld wanneer ze ofwel volledig actief of nul zijn |

> **Opmerking:** `PriorityTruncated` is onveilig wanneer een lagere-prioriteitskracht met het
> resterende budget een hogere-prioriteitskracht kan tegenwerken. Als bijvoorbeeld
> `wallForce = 25` (weg van de wand) en `wanderForce = 55` (richting de wand), is het nettoresultaat
> 30 richting de wand. Gebruik in dat geval `WeightedSum` met hogere gewichten voor alle
> veiligheidskritieke krachten.

#### Voorbeeld van `WeightedSum`

```csharp
Vector2f wander    = _steering.Wander(ref _wanderAngle, 80f, 200f);
Vector2f obstacles = _steering.ObstacleAvoidance(_obstacles, 100f, 16f);
Vector2f walls     = _steering.WallAvoidance(_walls, 120f, 45f);
Vector2f bounds    = _steering.StayWithinBounds(_boundary, 50f);

// Vermijdingskrachten hebben hogere gewichten zodat ze dwalen domineren
Vector2f force = SteeringForces.WeightedSum(
    _agent.MaxSpeed,
    (wander,    1f),
    (obstacles, 2f),
    (bounds,    3f),
    (walls,     4f));
```

#### Voorbeeld van `PriorityTruncated`

```csharp
Vector2f separation = _steering.Separation(_neighbors, 60f);
Vector2f alignment  = _steering.Alignment(_neighbors);
Vector2f cohesion   = _steering.Cohesion(_neighbors);

// Flocking-krachten worden in prioriteitsvolgorde tot het budget opgeteld
Vector2f force = SteeringForces.PriorityTruncated(
    _agent.MaxSpeed,
    separation,
    alignment,
    cohesion);
```

### `SteeringDebugDrawer`

`SteeringDebugDrawer` is een per-agent-component die de interne toestand van elke
`SteeringBehavior`-methode visualiseert tijdens ontwikkeling. Het implementeert `IDisposable` —
dispose wanneer de agent wordt vernietigd om SFML-shape-resources vrij te geven.

#### Inschakelen en uitschakelen

```csharp
// Debug-overlay wisselen — meestal gekoppeld aan de accent grave-toets
SteeringDebugDrawer.Enabled = host.Input.WasKeyPressed(Keyboard.Key.Grave);
```

`static Enabled` is een globale vlag. Als deze op `false` is ingesteld, wordt elke `Draw*`-aanroep
een no-op, zodat u alle debug-aanroepen veilig in uw update/render-lus kunt laten staan zonder
runtime-kosten.

#### API-overzicht

| Methode | Signatuur | Kleur | Beschrijving |
|---------|----------|-------|--------------|
| `DrawVelocityAndHeading` | `(window)` | Wit / Groen | Snelheidspijl (wit) en richtingspijl (groen) vanaf de positie van de agent |
| `DrawWander` | `(window, wanderAngle, wanderRadius, wanderDistance)` | Cyaan / Geel | Wandercirkel (cyaan) met huidige doelpunt (geel) |
| `DrawSeek` | `(window, targetPosition)` | Groen | Lijn van agent naar zoekdoel |
| `DrawFlee` | `(window, targetPosition)` | Oranje | Lijn van agent naar vluchtpunt |
| `DrawArrive` | `(window, targetPosition, slowingRadius)` | Groen | Zoeklijn en vertragingsradiuscirkel |
| `DrawPursue` | `(window, target)` | Magenta | Lijn naar voorspelde toekomstige positie |
| `DrawEvade` | `(window, target)` | Rood | Lijn naar voorspelde toekomstige positie |
| `DrawAutoPilot` | `(window, targetPosition, arrivalRadius)` | Lichtgroen / Geel | Doelpunt, aankomstradiuscirkel en gewenste richtingspijl |
| `DrawBoundary` | `(window, boundary, margin)` | Blauw | Buitenste begrenzingsrechthoek en binnenste margerechthoek |
| `DrawObstacleAvoidance` | `(window, obstacles, detectionLength, agentRadius)` | Oranje kader / Rood | Detectiekader; dichtstbijzijnde obstakel gemarkeerd |
| `DrawWallAvoidance` | `(window, walls, feelerLength, feelerAngle)` | Gele voelers / Rood | Drie voelers; actief muurkruispunt in rood weergegeven |
| `DrawOffsetPursuit` | `(window, leader, offset, slowingRadius)` | Cyaan / Magenta | Formatiepositie, voorspeld doel en stuurlijnen |
| `DrawInterpose` | `(window, object1, object2)` | Oranje / Groen | Voorspelde posities van beide objecten en middelpuntdoel |
| `DrawHide` | `(window, target, obstacles, distanceFromBoundary, threatDistance)` | Oranje / Groen | Schuilplaatsen achter obstakels ten opzichte van de dreiging |
| `DrawFollowPath` | `(window, pathToFollow)` | Grijs / Geel | Waypointpunten en verbindende padlijnen |
| `DrawNeighborhood` | `(window, neighbors, neighborhoodRadius)` | Blauw | Buurtdetectieradiuscirkel en lijnen naar buren |
| `DrawSeparation` | `(window, neighbors, separationRadius)` | Rood | Separatieradiuscirkel en afstotingspijlen |
| `DrawAlignment` | `(window, neighbors)` | Cyaan | Pijl in de richting van de gemiddelde koers |
| `DrawCohesion` | `(window, neighbors)` | Groen | Pijl naar het zwaartepunt van de buurt |
| `static DrawStats` | `(window, font, fps, updateMs)` | Lichtgrijze tekst | FPS en laatste updatetijd linksonder in het venster |
| `static DrawSpatialGrid<T>` | `(window, grid, font)` | Blauwe cellen / Wit aantal | Bezette gridcellen als doorschijnende rechthoeken met item-aantalslabels |
| `Dispose()` | — | — | Geef alle SFML-shape-resources vrij |

#### Integratiepatroon

Maak de debug drawer naast het gedrag aan, koppel elke `SteeringBehavior`-aanroep aan de bijbehorende
`Draw*`-aanroep in `Render` en schakel in met een toetskoppeling:

```csharp
// Aanmaken
_steering = new SteeringBehavior(this);
_debug    = new SteeringDebugDrawer(this);

// In scene Update — wisselen met accent grave
SteeringDebugDrawer.Enabled = host.Input.WasKeyPressed(Keyboard.Key.Grave);

// In agent Update — bereken krachten normaal
public void Update(float dt)
{
    Vector2f wander    = _steering.Wander(ref _wanderAngle, 80f, 200f);
    Vector2f wallForce = _steering.WallAvoidance(_walls, 120f, 45f);

    Vector2f force = SteeringForces.WeightedSum(
        MaxSpeed,
        (wander, 1f), (wallForce, 4f));

    _velocity += (force / Mass) * dt;
    _velocity  = _velocity.Truncate(MaxSpeed);
    _position += _velocity * dt;
    _steering.UpdateHeadingWhileMoving(dt, ref _rotation);
}

// In agent Render — spiegel elke gedragsaanroep met zijn debug-tegenhanger
public void Render(RenderWindow window, Font font, double fps, double updateMs)
{
    // ... teken agent-sprite ...

    _debug.DrawVelocityAndHeading(window);
    _debug.DrawWander(window, _wanderAngle, 80f, 200f);
    _debug.DrawWallAvoidance(window, _walls, 120f, 45f);

    // DrawStats is statisch — roep één keer per frame aan, niet per agent
    SteeringDebugDrawer.DrawStats(window, font, fps, updateMs);
}
```

### Gebruiksvoorbeeld

```csharp
using GrayHare.GameEngine.Behaviors;
using SFML.Graphics;
using SFML.System;

internal sealed class AutonomousAgent : IMovableGameObject, IDisposable
{
    private readonly SteeringBehavior    _steering;
    private readonly SteeringDebugDrawer _debug;
    private readonly IList<Wall>         _walls;
    private readonly FloatRect           _boundary;
    private float                        _wanderAngle;
    private float                        _rotation;

    public Vector2f Position { get; set; }
    public Vector2f Velocity { get; set; }
    public Vector2f Heading  { get; private set; } = new(0f, -1f);
    public float    Mass     { get; } = 1f;
    public float    MaxSpeed { get; } = 160f;
    public float    Speed    => Velocity.Length;

    public AutonomousAgent(IList<Wall> walls, FloatRect boundary)
    {
        _walls    = walls;
        _boundary = boundary;
        _steering = new SteeringBehavior(this);
        _debug    = new SteeringDebugDrawer(this);
    }

    public void Update(float dt)
    {
        Vector2f wander      = _steering.Wander(ref _wanderAngle, 80f, 200f);
        Vector2f wallForce   = _steering.WallAvoidance(_walls, 120f, 45f);
        Vector2f boundsForce = _steering.StayWithinBounds(_boundary, 50f);

        // Veiligheidskrachten hebben hogere gewichten om dwalen te domineren
        Vector2f force = SteeringForces.WeightedSum(
            MaxSpeed,
            (wander,      1f),
            (boundsForce, 2f),
            (wallForce,   4f));

        Velocity += (force / Mass) * dt;
        Velocity  = Velocity.Truncate(MaxSpeed);
        Position += Velocity * dt;
        _steering.UpdateHeadingWhileMoving(dt, ref _rotation);
    }

    public void Render(RenderWindow window, Font font, double fps, double updateMs)
    {
        // ... teken agent-sprite ...

        _debug.DrawVelocityAndHeading(window);
        _debug.DrawWander(window, _wanderAngle, 80f, 200f);
        _debug.DrawWallAvoidance(window, _walls, 120f, 45f);
        SteeringDebugDrawer.DrawStats(window, font, fps, updateMs);
    }

    public void Dispose()
    {
        _debug.Dispose();
    }
}
```

---

## Extensions

### Overzicht

De `Extensions`-namespace biedt handige extensiemethoden voor veelgebruikte SFML- en .NET-typen,
waarmee boilerplate in scene- en gedragscode wordt gereduceerd.

### Extensiemethoden

| Extensieklasse | Methode | Parameters | Returntype | Beschrijving |
|----------------|---------|-----------|------------|--------------|
| `FloatExtensions` | `float.ToVector2f()` | — | `Vector2f` | Zet een rotatiehoek in graden om naar een eenheids-`Vector2f`; 0° wijst rechts, hoeken nemen toe met de klok mee |
| `ShapeExtensions` | `Shape.ToTexture(padding)` | `uint = 0` | `Texture` | Render de vorm buiten het scherm naar een `Texture`; `padding` voegt transparante pixels toe rondom de vorm |
| `VectorExtensions` | `Vector2f.Truncate(maximum)` | `float` | `Vector2f` | Begrens de lengte van de vector tot `maximum`, waarbij de richting behouden blijft |
| `VectorExtensions` | `Vector2f.DistanceTo(to)` | `Vector2f` | `float` | Bereken de Euclidische afstand tussen twee punten |
| `VectorExtensions` | `Vector2f.WrapPosition(size)` | `Vector2f` | `Vector2f` | Wikkel elke component naar het bereik `[0, size)`, voor toroïdale / schermomloop-beweging |
| `VectorExtensions` | `Vector2f.WrapPosition(size)` | `Vector2u` | `Vector2f` | `Vector2u`-overload van `WrapPosition`; converteert `size` naar `Vector2f` vóór het wikkelen |
| `WindowExtensions` | `RenderWindow.DrawCenteredText(font, fontSize, color, text, y)` | `Font`, `uint`, `Color`, `string`, `float` | `void` | Teken een tekstreeks horizontaal gecentreerd in het venster op de opgegeven Y-coördinaat |

### Gebruiksvoorbeeld

```csharp
// Zet een kijkrichtingshoek om naar een richtingsvector
float rotatieGraden = 45f;
Vector2f richting = rotatieGraden.ToVector2f(); // ≈ (0.707, 0.707)

// Begrens snelheid tot de maximumsnelheid van de agent
Vector2f snelheid  = new(500f, 300f);
Vector2f begrensd  = snelheid.Truncate(250f);

// Euclidische afstandscontrole
float afstand = agentPositie.DistanceTo(doelPositie);
if (afstand < 50f)
{
    Console.WriteLine("Doel is dichtbij.");
}

// Rasteriseer een cirkelform naar een herbruikbare textuur
CircleShape cirkel = new(16f) { FillColor = Color.Red };
Texture tex = cirkel.ToTexture(padding: 2);

// Wikkel de positie van een agent voor toroïdale (schermomloop) beweging
Vector2f schermGrootte = new(1280f, 720f);
positie = positie.WrapPosition(schermGrootte);

// Dezelfde omloop met een Vector2u venstergrootte
positie = positie.WrapPosition(window.Size);

// Teken gecentreerde titeltekst
window.DrawCenteredText(_font, 48, Color.White, "GAME OVER", 340f);
```

---

## Shaders

### Overzicht

GLSL-shaderondersteuning wordt geboden via `AssetStore`. Shaders worden geladen, gecompileerd en
gecacht per pad. `GlslVersionParser` is een statisch hulpprogramma dat intern wordt gebruikt door
`TryLoadShader` om het gedetecteerde GLSL-versienummer op te nemen in foutmeldingen, wat
cross-platform diagnose van versie-mismatchfouten vergemakkelijkt.

### `GlslVersionParser` (statisch)

| Methode | Parameters | Returntype | Beschrijving |
|---------|-----------|------------|--------------|
| `Parse(shaderSource)` | `string` | `int?` | Extraheer het GLSL-versienummer uit een `#version NNN`-instructie. Geeft `null` terug als er geen instructie aanwezig is. |

### Shaders laden via `AssetStore`

| Methode | Beschrijving |
|---------|--------------|
| `assets.LoadShader(fragPath)` | Laad een fragment-only shader. Gooit een uitzondering bij een compilatiefout. |
| `assets.LoadShader(vertPath, fragPath)` | Laad een vertex + fragment shader-paar. Gooit een uitzondering bij een fout. |
| `assets.TryLoadShader(fragPath, out reason)` | Laad een fragment-shader; geeft `null` met een beschrijvend bericht bij een fout. |
| `assets.TryLoadShader(vertPath, fragPath, out reason)` | Laad een shader-paar; geeft `null` met een beschrijvend bericht bij een fout. |

> **Platformnoot:** GLSL-versieondersteuning varieert per GPU-driver en besturingssysteem. Gebruik
> `TryLoadShader` in productiebuildversies om graceful te degraderen wanneer een shader niet wordt
> ondersteund op het doelplatform.

### Gebruiksvoorbeeld

```csharp
private Shader? _waveShader;
private Clock   _clock = new();

public override void Load(GameHost host)
{
    if (host.Assets.TryLoadShader("shaders/wave.frag", out string? reason) is { } shader)
    {
        _waveShader = shader;
    }
    else
    {
        Console.Error.WriteLine($"Wave-shader kon niet worden geladen: {reason}");
    }
}

public override void Render(GameHost host, RenderWindow window)
{
    if (_waveShader is not null)
    {
        _waveShader.SetUniform("time", _clock.ElapsedTime.AsSeconds());
        window.Draw(_sprite, new RenderStates(_waveShader));
    }
    else
    {
        // Fallback: renderen zonder post-process-effect
        window.Draw(_sprite);
    }
}
```

---

## Wall

### Overzicht

`Wall` is een immutable `readonly record struct` dat een gericht lijnsegment vertegenwoordigt, dat
voornamelijk wordt gebruikt door `SteeringBehavior.WallAvoidance()`. De naar binnen gerichte
normaal van de wand wordt automatisch berekend vanuit de begin-naar-eind-richting bij constructie.
Het verwisselen van `Start` en `End` keert de normaal om, waardoor de wand de andere kant op kijkt.

### `Wall`

| Lid | Type | Beschrijving |
|-----|------|--------------|
| `Start` | `Vector2f` | Het beginpunt van het segment |
| `End` | `Vector2f` | Het eindpunt van het segment |
| `Normal` | `Vector2f` | Eenheidsnormaalvector (linker loodlijn op `Start → End`) — de kant waar de wand naartoe kijkt |
| `Wall(start, end)` | Constructor | Berekend en slaat de eenheidsnormaal automatisch op |
| `TryGetIntersection(from, to, out float test)` | `Vector2f`, `Vector2f`, `out float` → `bool` | Parametrische snijpunttest; `test` ∈ [0, 1] is de genormaliseerde positie langs de `from → to`-straal |

### Gebruiksvoorbeeld

```csharp
// Definieer vier naar binnen gerichte wanden die een gesloten kamer vormen
List<Wall> wanden =
[
    new(new Vector2f(  0,   0), new Vector2f(800,   0)),  // boven
    new(new Vector2f(800,   0), new Vector2f(800, 600)),  // rechts
    new(new Vector2f(800, 600), new Vector2f(  0, 600)),  // onder
    new(new Vector2f(  0, 600), new Vector2f(  0,   0)),  // links
];

// Gebruik wandvermijding in de agentupdate
SteeringBehavior steering = new(agent);

public void Update(float dt)
{
    Vector2f vermijdKracht = steering.WallAvoidance(wanden, feelerLength: 100f, feelerAngle: 45f);
    _snelheid += (vermijdKracht / agent.Mass) * dt;
    _snelheid  = _snelheid.Truncate(agent.MaxSpeed);
    _positie  += _snelheid * dt;
}

// Handmatige snijpunttest — bijv. voor projectielbotsing
Vector2f kogel_start = new(100, 100);
Vector2f kogel_eind  = new(900, 100);

foreach (Wall wand in wanden)
{
    if (wand.TryGetIntersection(kogel_start, kogel_eind, out float t))
    {
        Console.WriteLine($"Kogel raakt wand op t = {t:F2}");
    }
}
```

---

## Spatial

### Overzicht

De `Spatial`-module biedt `SpatialGrid<T>`, een generieke gridgebaseerde spatial hash die
2D-ruimte opdeelt in cellen van vaste grootte voor snelle radius-gebaseerde buurtquery's.
Het is ontworpen voor een per-frame herbouwworkflow en integreert rechtstreeks met de
flockingmethoden van `SteeringBehavior` (Separation, Alignment, Cohesion) door resultaten
terug te geven in een `List<T>` die `IReadOnlyList<T>` implementeert.

### `SpatialGrid<T>`

| Lid | Parameters | Returntype | Beschrijving |
|-----|-----------|------------|--------------|
| `SpatialGrid(cellSize)` | `float` | — | Maak een grid met de opgegeven celbreedte/-hoogte; een goede standaard is gelijk aan de grootste queryradius |
| `CellSize` | — | `float` | De breedte en hoogte van elke gridcel |
| `Count` | — | `int` | Het aantal items dat momenteel in het grid is opgeslagen |
| `Clear()` | — | `void` | Verwijder alle items; cellijsten worden intern gepoold zodat volgende `Add`-aanroepen ze hergebruiken |
| `Add(item, position)` | `T`, `Vector2f` | `void` | Voeg een item in op de opgegeven wereldruimtepositie |
| `FindNeighbors(position, radius, results, exclude?)` | `Vector2f`, `float`, `List<T>`, `T?` | `int` | Wis `results`, vul het met items binnen `radius` en retourneer het aantal; sla optioneel `exclude` over |
| `EnumerateCells()` | — | `IEnumerable<(Vector2f, int)>` | Retourneer voor elke bezette cel de wereldruimte-oorsprong en het itemaantal (voor debugvisualisatie) |

> **Prestatienotities:**
> - Cellijsten worden gepoold zodat `Clear` → `Add`-cycli na opwarming geen allocaties produceren.
> - `FindNeighbors` gebruikt gekwadrateerde-afstandscontroles (geen `MathF.Sqrt`) in de binnenste lus.
> - Kies `cellSize` dicht bij de grootste queryradius; kleinere cellen verminderen het werk per query
>   maar verhogen het aantal bezochte cellen.

### Gebruiksvoorbeeld

```csharp
// ── Opzet ────────────────────────────────────────────────────────────────
var grid      = new SpatialGrid<IMovableGameObject>(cellSize: 120f);
var neighbors = new List<IMovableGameObject>();

// ── Elk frame in Update ──────────────────────────────────────────────────
grid.Clear();
foreach (IMovableGameObject agent in allAgents)
{
    grid.Add(agent, agent.Position);
}

foreach (IMovableGameObject agent in allAgents)
{
    grid.FindNeighbors(agent.Position, neighborhoodRadius, neighbors, exclude: agent);

    // neighbors is een List<T> die IReadOnlyList<T> implementeert —
    // geef direct door aan elke SteeringBehavior-flockingmethode:
    Vector2f sep = steering.Separation(neighbors, separationRadius);
    Vector2f ali = steering.Alignment(neighbors);
    Vector2f coh = steering.Cohesion(neighbors);
}

// ── Debugvisualisatie in Render ──────────────────────────────────────────
SteeringDebugDrawer.DrawSpatialGrid(window, grid, font);
```

---

## Pathfinding

### Overzicht

De `Pathfinding`-naamruimte biedt vijf graafzoekalgoritmen voor rastergebaseerde navigatie —
BFS, DFS, Dijkstra, A* en Flow Field — plus een `PathfindingDebugDrawer` voor het visualiseren van zoekresultaten.
Alle algoritmen gebruiken 4-richting (orthogonale) beweging en retourneren een `PathfindingResult`
dat zowel het gevonden pad als het volledig verkende gebied vastlegt, waardoor het eenvoudig is om
verkenningspatronen van verschillende algoritmen naast elkaar te vergelijken.

### `GridCell`

Een `readonly record struct` die een enkelvoudige rasterpositie identificeert.

| Lid | Type | Beschrijving |
|-----|------|--------------|
| `GridCell(Row, Column)` | constructor | Maak een cel aan op het opgegeven rij- en kolomindex (nul-gebaseerd) |
| `Row` | `int` | Nul-gebaseerde rij-index |
| `Column` | `int` | Nul-gebaseerde kolom-index |

### `PathfindingAlgorithm`

| Waarde | Beschrijving |
|--------|--------------|
| `BFS` | Breadth-first search — garandeert het kortste pad op niet-gewogen rasters |
| `DFS` | Depth-first search — vindt een geldig pad maar garandeert niet het kortste |
| `Dijkstra` | Dijkstra's algoritme — garandeert het kortste pad |
| `AStar` | A\* met Manhattan-heuristiek — garandeert het kortste pad, bezoekt minder cellen |
| `FlowField` | Stroomveld — BFS vanuit het doel; pad geëxtraheerd via per-cel richtingsvectoren |

### `PathfindingGrid`

Een rechthoekig raster van begaanbare en geblokkeerde cellen voor padzoekquery's. Alle cellen zijn
standaard begaanbaar. Buurtquery's gebruiken uitsluitend 4-richting (orthogonale) beweging.

| Lid | Parameters | Returntype | Beschrijving |
|-----|-----------|------------|--------------|
| `PathfindingGrid(rows, columns)` | `int`, `int` | — | Maak een raster aan; gooit `ArgumentOutOfRangeException` als een dimensie ≤ 0 is |
| `Rows` | — | `int` | Aantal rijen |
| `Columns` | — | `int` | Aantal kolommen |
| `IsInBounds(cell)` | `GridCell` | `bool` | Of de cel binnen de rastergrenzen valt |
| `IsWalkable(cell)` | `GridCell` | `bool` | Of de cel in-bounds en niet geblokkeerd is |
| `IsBlocked(cell)` | `GridCell` | `bool` | Of de cel in-bounds en geblokkeerd is |
| `SetBlocked(cell, blocked)` | `GridCell`, `bool` | `void` | Markeer een cel als geblokkeerd of begaanbaar; gooit `ArgumentOutOfRangeException` bij out-of-bounds |
| `Clear()` | — | `void` | Stel alle cellen terug op begaanbaar |
| `GetWalkableNeighbors(cell, results)` | `GridCell`, `List<GridCell>` | `void` | Wis de aangeleverde lijst en vul deze met begaanbare orthogonale buren; hergebruik de lijst om allocaties te vermijden |

### `PathfindingResult`

| Lid | Type | Beschrijving |
|-----|------|--------------|
| `Start` | `GridCell` | De startcel van de zoekopdracht |
| `End` | `GridCell` | De doelcel van de zoekopdracht |
| `Path` | `IReadOnlyList<GridCell>` | Geordende cellen van start tot einde (inclusief); leeg als er geen pad is gevonden |
| `Visited` | `IReadOnlySet<GridCell>` | Alle cellen die tijdens de zoekopdracht zijn onderzocht |
| `Found` | `bool` | `true` als `Path.Count > 0` |

### `PathFinder` (statisch)

| Methode | Parameters | Returntype | Beschrijving |
|---------|-----------|------------|--------------|
| `FindPath(grid, start, end, algorithm)` | `PathfindingGrid`, `GridCell`, `GridCell`, `PathfindingAlgorithm` | `PathfindingResult` | Verzendt naar het opgegeven algoritme |
| `BreadthFirstSearch(grid, start, end)` | `PathfindingGrid`, `GridCell`, `GridCell` | `PathfindingResult` | BFS — garandeert het kortste pad |
| `DepthFirstSearch(grid, start, end)` | `PathfindingGrid`, `GridCell`, `GridCell` | `PathfindingResult` | DFS — geldig pad, niet gegarandeerd kortste |
| `Dijkstra(grid, start, end)` | `PathfindingGrid`, `GridCell`, `GridCell` | `PathfindingResult` | Dijkstra — garandeert het kortste pad |
| `AStar(grid, start, end)` | `PathfindingGrid`, `GridCell`, `GridCell` | `PathfindingResult` | A\* — garandeert kortste pad, gebruikt Manhattan-heuristiek |
| `BuildFlowField(grid, goal)` | `PathfindingGrid`, `GridCell` | `FlowFieldResult` | BFS vanuit `goal` naar buiten; bouwt een per-cel richtingskaart voor het hele raster |

> **Randgevallen:**
> - Als `start` geblokkeerd is, is `Found` `false` en is `Visited` leeg.
> - Als `end` geblokkeerd is, zoekt het algoritme normaal door en geeft `Visited` het verkende gebied weer, maar `Found` is `false` omdat er geen begaanbaar pad naar een geblokkeerde cel bestaat.
> - Als `start == end`, bevat het resultaat een pad van één cel `[start]`.

### `FlowFieldResult`

Geretourneerd door `PathFinder.BuildFlowField`. Slaat een per-cel richtingskaart op, opgebouwd
via BFS vanuit het doel naar buiten. Elke bereikbare cel registreert zijn volgende stap richting
het doel, waardoor O(1)-padopvragen per agent per frame mogelijk zijn — nuttig voor het
navigeren van meerdere agents naar hetzelfde eindpunt.

| Lid | Parameters | Returntype | Beschrijving |
|-----|-----------|------------|--------------|
| `Goal` | — | `GridCell` | De doelcel waarvoor het stroomveld is gebouwd |
| `GetNextCell(cell)` | `GridCell` | `GridCell?` | De volgende cel om naartoe te bewegen vanuit `cell`; `null` als `cell` het doel is of onbereikbaar |
| `IsReachable(cell)` | `GridCell` | `bool` | `true` als de cel het doel kan bereiken (inclusief het doel zelf) |
| `ReachableCells` | — | `IEnumerable<GridCell>` | Alle cellen met een geregistreerde volgende stap (exclusief het doel) |

> **Gebruikspatroon:** Roep `BuildFlowField` eenmalig aan wanneer het doolhof verandert,
> en roep `GetNextCell` elke frame aan per agent. Het stroomveld is statisch; herbouw het
> wanneer het raster of het doel wijzigt.

### `PathfindingDebugDrawer` (statisch)

| Lid | Parameters | Beschrijving |
|-----|-----------|--------------|
| `Enabled` | — | Schakel alle debug-tekening in of uit; standaard `true` |
| `DrawGrid(window, grid, cellSize, origin)` | `RenderWindow`, `PathfindingGrid`, `float`, `Vector2f` | Teken geblokkeerde (muur)cellen en rasterlijnen |
| `DrawResult(window, result, cellSize, origin, showVisited)` | `RenderWindow`, `PathfindingResult`, `float`, `Vector2f`, `bool` | Teken bezochte cellen (optioneel), het gevonden pad en start-/eindmarkeringen |
| `DrawFlowField(window, field, cellSize, origin)` | `RenderWindow`, `FlowFieldResult`, `float`, `Vector2f` | Teken per-cel richtingspijlen voor het volledige stroomveld |

**Kleurlegenda:**

| Element | Kleur |
|---------|-------|
| Muurcellen | Donkergrijs `(60, 65, 80)` |
| Rasterlijnen | Donkerder grijs `(50, 55, 70)` |
| Bezochte cellen | Doorschijnend blauw `(40, 80, 140, 80)` |
| Padcellen | Helder groen `(80, 220, 120, 180)` |
| Startmarkering | Groen `(50, 200, 50)` |
| Eindmarkering | Rood `(220, 50, 50)` |

### `PathfindingGridExtensions` (statisch)

Koppelt `PathfindingGrid` aan de `Wall`-geometrie van de engine, zodat steering-muren
padzoekende cellen kunnen blokkeren zonder handmatige conversie.

| Methode | Parameters | Returntype | Beschrijving |
|---------|-----------|------------|--------------|
| `grid.ApplyWalls(walls, cellSize, origin)` | `IEnumerable<Wall>`, `float`, `Vector2f` | `void` | Markeer elke cel die door een `Wall`-segment wordt doorkruist als geblokkeerd. Test alle vier celaranden en beide eindpunten van de muur. Wist de grid **niet** eerst. |

> **Werking:** Per muur worden alleen cellen getest die overlappen met de as-uitgelijnde
> begrenzende box van de muur, zodat de prestaties schalen met de muurlengte in plaats
> van de gridgrootte. Een cel wordt geblokkeerd wanneer een van haar vier randen het
> muurstuk kruist, of wanneer een van de eindpunten van de muur binnen de cel valt
> (dit dekt ook zeer korte muren die volledig in één cel liggen).

### Gebruiksvoorbeeld

```csharp
// ── Setup ────────────────────────────────────────────────────────────────
Vector2f origin   = new(20f, 60f);
float    cellSize = 28f;

var grid = new PathfindingGrid(rows: 22, columns: 42);

// Optie A: Wall-segmenten van levelgeometrie op het raster stempelen
IEnumerable<Wall> levelWalls = scene.GetWalls();
grid.ApplyWalls(levelWalls, cellSize, origin);

// Optie B: afzonderlijke cellen direct blokkeren
grid.SetBlocked(new GridCell(5, 10), true);

// ── Zoeken ───────────────────────────────────────────────────────────────
GridCell          start  = new(0, 0);
GridCell          end    = new(21, 41);
PathfindingResult result = PathFinder.FindPath(grid, start, end, PathfindingAlgorithm.BFS);

if (result.Found)
{
    Console.WriteLine($"Pad: {result.Path.Count} cellen");
}

// ── Debug-visualisatie in Render ─────────────────────────────────────────
PathfindingDebugDrawer.DrawGrid(window, grid, cellSize, origin);
PathfindingDebugDrawer.DrawResult(window, result, cellSize, origin, showVisited: true);

// Stroomveld — éénmalig bouwen, meerdere agents navigeren O(1) per frame
FlowFieldResult field = PathFinder.BuildFlowField(grid, goal);
PathfindingDebugDrawer.DrawFlowField(window, field, cellSize, origin);
GridCell? next = field.GetNextCell(agentCell); // O(1) per agent per frame
```

---

## Constants

### Overzicht

De `Constants`-klasse stelt globaal toegankelijke benoemde `Vector2f`-waarden beschikbaar om
magische getallen in spel- en gedragscode te vervangen.

### `Constants.Vectors`

| Constante | Waarde | Beschrijving |
|-----------|--------|--------------|
| `Constants.Vectors.Zero` | `Vector2f(0, 0)` | Nulvector; nuttig voor het resetten van positie, snelheid of geaccumuleerde krachten |
| `Constants.Vectors.One` | `Vector2f(1, 1)` | Eenheidsschaalvector; nuttig als standaardschaal of uniforme richting |

### Gebruiksvoorbeeld

```csharp
// Reset de snelheid van een agent naar stilstand
agent.Velocity = Constants.Vectors.Zero;

// Pas een neutrale (identiteits)schaal toe op een sprite
_sprite.Scale = Constants.Vectors.One;

// Bewaker: sla steering over als de agent nog niet heeft bewogen
if (agent.Velocity == Constants.Vectors.Zero)
{
    return;
}
```

---

## Ontwerppatronen

De volgende ontwerppatronen bepalen de architectuur van GrayHare.GameEngine:

| Patroon | Waar toegepast | Doel |
|---------|---------------|------|
| **Service Locator** | `GameHost` | Biedt alle subsystemen aan scenes via één getypte accessor, waardoor diepe constructor-injectieketens worden vermeden |
| **Template Method** | `GameSceneBase` | Definieert het lifecycle-skelet van een scene (`Load → Update → RenderLayer → Unload`); subklassen overschrijven alleen de stappen die ze nodig hebben |
| **Strategy** | `MovementBehavior`, `MovementWithDriftingBehavior`, `MovementWithRotationBehavior`, `SteeringBehavior` | Samenstelbare bewegingsalgoritmen die tijdens runtime worden geselecteerd en gecombineerd zonder game-objectklassen te wijzigen |
| **Entity-Component-System** | `World` + `Entity` | Scheidt data (componenten) van identiteit (entiteiten) en logica (systemen), waardoor flexibele objectsamenstellingen mogelijk zijn zonder diepe overerving |
| **Flyweight / Asset Cache** | `AssetStore` | Zorgt ervoor dat elke asset slechts eenmaal wordt geladen en gedeeld; pad-gekoppelde woordenboeken fungeren als flyweight-fabriek |
| **Frame-Scoped Snapshot** | `InputSnapshot`, `GameTime` | Per-frame-status wordt eenmaal vastgelegd en als alleen-lezen behandeld binnen een frame, veilig te lezen van overal tijdens een frame zonder mutatierisico |
| **Null Object** | `InputSnapshot.Empty`, `GameTime.Start` | Veilige standaardinstanties die geen null-controles vereisen, waardoor codepaden worden vereenvoudigd die vóór echte status worden uitgevoerd |
| **Observer (via SFML-events)** | `InputTracker` | SFML-venstergebeurtenissen worden elk frame verzonden naar de input-tracker; scenes benaderen de status via `InputSnapshot` |
| **Spatial Hashing** | `SpatialGrid<T>` | Deelt 2D-ruimte op in cellen van vaste grootte voor O(1) cel-lookup en radius-gebaseerde buurtquery's, ter vervanging van brute-force O(n²) scans |
| **Graafzoeken (BFS / DFS / Dijkstra / A\* / Flow Field)** | `PathFinder` | Uitwisselbare zoekstrategieën achter een uniforme `FindPath`-dispatching, waardoor het algoritme tijdens runtime kan worden gewisseld zonder aanroepplaatsen te wijzigen |

---

## Nieuw in V1

### Scene Stack (Push / Pop)

De engine ondersteunt overlay-scenes via een scene-stack. Gebruik `PushScene` om een overlay
(bijv. een pauzemenu) bovenop de huidige scene te plaatsen, en `PopScene` om deze te verwijderen.

| Lid | Beschrijving |
|-----|--------------|
| `GameHost.PushScene(GameSceneBase overlay)` | Zet een overlay-scene in de wachtrij om aan het einde van het frame te worden gepusht. |
| `GameHost.PopScene()` | Zet de bovenste scene in de wachtrij om aan het einde van het frame te worden verwijderd. |
| `GameHost.SceneStackDepth` | Aantal scenes dat momenteel op de stack staat. |
| `GameSceneBase.OnActivated(GameHost)` | Wordt aangeroepen wanneer de scene de bovenste op de stack wordt. |
| `GameSceneBase.OnDeactivated(GameHost)` | Wordt aangeroepen wanneer een andere scene bovenop wordt gepusht. |

Alleen de bovenste scene ontvangt `Update`-aanroepen. Alle scenes op de stack worden van onder naar boven gerenderd.

### RemoveLayer

| Lid | Beschrijving |
|-----|--------------|
| `RemoveLayer(ISceneLayer)` | Verwijdert een eerder toegevoegde laag. Geeft true terug als deze is gevonden. |

### IDisposable-ondersteuning

`GameSceneBase` implementeert nu `IDisposable`. Overschrijf `Dispose(bool disposing)` voor aangepaste opruiming.
De engine ruimt scenes automatisch op wanneer ze van de stack worden verwijderd.

### Camera2D

Een 2D-camera die SFML's `View` omhult met vloeiend volgen, zoom en schermschudden.

| Lid | Beschrijving |
|-----|--------------|
| `Camera2D.Position` | Positie van het middelpunt in wereldcoördinaten. |
| `Camera2D.Zoom` | Zoomniveau (1 = normaal, >1 = inzoomen, <1 = uitzoomen). |
| `Camera2D.Rotation` | Rotatie in graden. |
| `Camera2D.ViewportSize` | Viewportafmetingen die bij constructie uit de venstergrootte worden overgenomen. |
| `Camera2D.Follow(target, lerpSpeed, deltaTime)` | Volgt vloeiend een doelpositie. |
| `Camera2D.Shake(intensity, duration)` | Start een afnemend schermschud-effect. |
| `Camera2D.UpdateShake(deltaTime)` | Werkt de schudtimer bij met ruwe/niet-geschaalde frameduur. |
| `Camera2D.GetView()` | Geeft de SFML View terug wanneer je handmatig een view wilt toepassen of overschrijven. |
| `Camera2D.ScreenToWorld(screenPos)` | Converteert een schermPixelcoördinaat naar een wereldruimtepositie, rekening houdend met camerapositie, zoom, rotatie en schudden. |
| `Camera2D.WorldToScreen(worldPos)` | Converteert een wereldruimtepositie naar een schermpixelcoördinaat. Handig voor het plaatsen van UI-elementen boven wereldobjecten. |
| `Camera2D.Reset()` | Herstelt standaardwaarden. |
| `GameHost.Camera` | De camera-instantie voor de huidige applicatie. |

In de normale `GameApplication`-renderlus werkt de engine `GameHost.Camera` al bij en past die toe
voordat de huidige scene rendert.

### Muziek streamen

`AudioPlayer` ondersteunt nu het streamen van muziek van schijf via SFML's `Music`-klasse.

| Lid | Beschrijving |
|-----|--------------|
| `PlayMusic(path, volume, loop)` | Streamt een muziekbestand. Stopt eerst de vorige muziek. |
| `StopMusic()` | Stopt en ruimt het huidige nummer op. |
| `PauseMusic()` / `ResumeMusic()` | Pauzeer/hervat het huidige nummer. |
| `IsMusicPlaying` | Of er momenteel muziek wordt afgespeeld. |

### Volumebeheer

| Lid | Beschrijving |
|-----|--------------|
| `MasterVolume` / `SfxVolume` / `MusicVolume` | Volumeniveaus (0–100). |
| `SetMasterVolume(float)` / `SetSfxVolume(float)` / `SetMusicVolume(float)` | Setters (begrensd tot 0–100). |
| `Mute()` / `Unmute()` | Dempen zonder opgeslagen niveaus te verliezen. |
| `IsMuted` | Of audio gedempt is. |
| `MaxActiveSounds` | Limiet voor gelijktijdige geluiden (standaard 32). |
| `ActiveSoundCount` | Aantal actieve geluiden. |

### ECS-verbeteringen

| Lid | Beschrijving |
|-----|--------------|
| `HasComponent<T>(entity)` | Controleer zonder out-parameter. |
| `EntityCount` | Aantal actieve entiteiten. |
| `ComponentTypeCount` | Aantal geregistreerde componenttypen. |
| `ComponentCount<T>()` | Aantal entiteiten met component T. |
| `ForEach<T>(action)` | Itereer over entiteiten met één component. |
| `ForEach<TA, TB>(action)` | Itereer over entiteiten met twee componenten. |
| `ForEach<TA, TB, TC>(action)` | Itereer over entiteiten met drie componenten. |

Entiteits-ID's worden nu hergebruikt. `Entity` bevat een `Generation`-veld om verouderde handles te detecteren.

### Gamepad-ondersteuning

| Lid | Beschrijving |
|-----|--------------|
| `InputSnapshot.IsJoystickConnected(id)` | Of een joystick is aangesloten. |
| `InputSnapshot.IsJoystickButtonDown(id, button)` | Joystickknop ingedrukt. |
| `InputSnapshot.WasJoystickButtonPressed(id, button)` | Eerste frame ingedrukt. |
| `InputSnapshot.GetJoystickAxis(id, axis)` | Aswaarde (−100 tot 100). |
| `InputSnapshot.ConnectedJoysticks` | Set van aangesloten joystick-ID's. |

### Input-actiemapping

`InputActionMap` laat je benoemde acties definiëren die gebonden zijn aan toetsen en joystickknoppen/-assen.

| Lid | Beschrijving |
|-----|--------------|
| `MapKey(action, key)` | Koppel een toetsenbordtoets. |
| `MapButton(action, joystickId, button)` | Koppel een joystickknop. |
| `MapMouseButton(action, mouseButton)` | Koppel een muisknop (`Mouse.Button`). |
| `MapAxis(action, joystickId, axis, deadZone)` | Koppel een joystick-as. |
| `IsActionDown(action, input)` | Een willekeurige binding is ingedrukt. |
| `WasActionPressed(action, input)` | Eerste frame dat een binding werd ingedrukt. |
| `WasActionReleased(action, input)` | Eerste frame dat een binding werd losgelaten. |
| `GetAxisValue(action, input)` | Aswaarde buiten de dode zone. |
| `GameHost.InputActions` | Optionele actiemap-instantie. |

### Asset-terugval

Wanneer `AssetStore.LoadTexture` een bestand niet kan vinden, retourneert het een 16×16 magenta/zwart
dambord-terugvaltextuur in plaats van een uitzondering te gooien. Een diagnostisch bericht wordt
gelogd via `EngineLogger`.

### Engine-logging

`EngineLogger` biedt minimale diagnostische logging (sceneovergangen, asset-ladingen, shader-fouten).

| Lid | Beschrijving |
|-----|--------------|
| `EngineLogger.SetHandler(Action<string>?)` | Vervang of schakel de loghandler uit. |
| `EngineLogger.Log(string)` | Schrijf een bericht. |
| `GameApplicationOptions.LogHandler` | Stel een aangepaste handler in bij het opstarten. |

Standaardhandler schrijft naar `System.Diagnostics.Debug.WriteLine`.

