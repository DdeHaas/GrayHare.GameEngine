using GrayHare.GameEngine.DemoHub.Scenes.AnimationDemo;
using GrayHare.GameEngine.DemoHub.Scenes.AnimationOneShotDemo;
using GrayHare.GameEngine.DemoHub.Scenes.AssetFallbackDemo;
using GrayHare.GameEngine.DemoHub.Scenes.AudioDemo;
using GrayHare.GameEngine.DemoHub.Scenes.AvoidanceDemo;
using GrayHare.GameEngine.DemoHub.Scenes.CameraDemo;
using GrayHare.GameEngine.DemoHub.Scenes.CameraExtrasDemo;
using GrayHare.GameEngine.DemoHub.Scenes.ClearColorDemo;
using GrayHare.GameEngine.DemoHub.Scenes.DriftingDemo;
using GrayHare.GameEngine.DemoHub.Scenes.Ecs3Demo;
using GrayHare.GameEngine.DemoHub.Scenes.EcsComponentOpsDemo;
using GrayHare.GameEngine.DemoHub.Scenes.EcsDemo;
using GrayHare.GameEngine.DemoHub.Scenes.EcsRecyclingDemo;
using GrayHare.GameEngine.DemoHub.Scenes.ExplosionAnimationDemo;
using GrayHare.GameEngine.DemoHub.Scenes.FlockingDemo;
using GrayHare.GameEngine.DemoHub.Scenes.FlockingLeaderDemo;
using GrayHare.GameEngine.DemoHub.Scenes.FlockingSpatialGridDemo;
using GrayHare.GameEngine.DemoHub.Scenes.FollowPathDemo;
using GrayHare.GameEngine.DemoHub.Scenes.GameControllerDemo;
using GrayHare.GameEngine.DemoHub.Scenes.HideDemo;
using GrayHare.GameEngine.DemoHub.Scenes.InputActionDemo;
using GrayHare.GameEngine.DemoHub.Scenes.InputDemo;
using GrayHare.GameEngine.DemoHub.Scenes.InterposeDemo;
using GrayHare.GameEngine.DemoHub.Scenes.MovementDemo;
using GrayHare.GameEngine.DemoHub.Scenes.MultipleLayersDemo;
using GrayHare.GameEngine.DemoHub.Scenes.MusicDemo;
using GrayHare.GameEngine.DemoHub.Scenes.OffsetPursuitDemo;
using GrayHare.GameEngine.DemoHub.Scenes.OverviewScene;
using GrayHare.GameEngine.DemoHub.Scenes.PathfindingAgentDemo;
using GrayHare.GameEngine.DemoHub.Scenes.PathfindingDemo;
using GrayHare.GameEngine.DemoHub.Scenes.PursueEvadeDemo;
using GrayHare.GameEngine.DemoHub.Scenes.SceneStackDemo;
using GrayHare.GameEngine.DemoHub.Scenes.SeekArriveDemo;
using GrayHare.GameEngine.DemoHub.Scenes.ShaderBlurDemo;
using GrayHare.GameEngine.DemoHub.Scenes.ShaderFlockComboDemo;
using GrayHare.GameEngine.DemoHub.Scenes.ShaderGrayscaleDemo;
using GrayHare.GameEngine.DemoHub.Scenes.ShaderHighlanderDemo;
using GrayHare.GameEngine.DemoHub.Scenes.ShaderPixelateDemo;
using GrayHare.GameEngine.DemoHub.Scenes.ShaderStormBlinkDemo;
using GrayHare.GameEngine.DemoHub.Scenes.ShaderWaveDemo;
using GrayHare.GameEngine.DemoHub.Scenes.ShapeTextureDemo;
using GrayHare.GameEngine.DemoHub.Scenes.SpriteDemo;
using GrayHare.GameEngine.DemoHub.Scenes.SteeringDemo;
using GrayHare.GameEngine.DemoHub.Scenes.StrafingDemo;
using GrayHare.GameEngine.DemoHub.Scenes.TextDemo;
using GrayHare.GameEngine.DemoHub.Scenes.TimeScaleDemo;
using GrayHare.GameEngine.Scenes;

namespace GrayHare.GameEngine.DemoHub;

/// <summary>Organizes demo scenes into groups and creates scene instances on demand.</summary>
internal sealed class DemoCatalog
{
    public DemoCatalog(DemoAssetsManifest assets)
    {
        ArgumentNullException.ThrowIfNull(assets);

        Assets = assets;
        Entries = BuildEntries();
        Groups = BuildGroups(Entries);
    }

    /// <summary>The asset manifest supplied at construction, carried forward to each scene factory.</summary>
    public DemoAssetsManifest Assets { get; }

    /// <summary>All demo entries in display order (index 0 = overview).</summary>
    public IReadOnlyList<DemoEntry> Entries { get; }

    /// <summary>Logical groups derived from the entry list.</summary>
    public IReadOnlyList<DemoGroup> Groups { get; }

    /// <summary>Creates the scene at <paramref name="index"/>, wrapping around if out of range.</summary>
    public GameSceneBase Create(int index)
    {
        int normalized = Normalize(index);

        return Entries[normalized].Factory(this, normalized);
    }

    /// <summary>Wraps <paramref name="index"/> into the valid entry range, supporting negative wrap-around.</summary>
    public int Normalize(int index)
    {
        int count = Entries.Count;
        int normalized = index % count;

        return normalized < 0 ? normalized + count : normalized;
    }

    /// <summary>Returns the group that contains <paramref name="sceneIndex"/>.</summary>
    public DemoGroup GroupOf(int sceneIndex)
    {
        foreach (DemoGroup group in Groups)
        {
            if (sceneIndex >= group.StartIndex && sceneIndex < group.EndIndex)
            {
                return group;
            }
        }

        return Groups[0];
    }

    /// <summary>
    /// Returns the first scene index of the group that follows the group containing
    /// <paramref name="sceneIndex"/>, wrapping around to the first group.
    /// </summary>
    public int FirstIndexOfNextGroup(int sceneIndex)
    {
        DemoGroup current = GroupOf(sceneIndex);

        for (int i = 0; i < Groups.Count; i++)
        {
            if (Groups[i].Name == current.Name)
            {
                return i < Groups.Count - 1
                    ? Groups[i + 1].StartIndex
                    : Groups[0].StartIndex;
            }
        }

        return 0;
    }

    /// <summary>
    /// Returns the first scene index of the group that precedes the group containing
    /// <paramref name="sceneIndex"/>. If already at the first scene of the current group,
    /// jumps to the previous group; otherwise jumps to the start of the current group.
    /// Wraps around to the last group.
    /// </summary>
    public int FirstIndexOfPreviousGroup(int sceneIndex)
    {
        DemoGroup current = GroupOf(sceneIndex);

        if (sceneIndex > current.StartIndex)
        {
            return current.StartIndex;
        }

        for (int i = 0; i < Groups.Count; i++)
        {
            if (Groups[i].Name == current.Name)
            {
                return i > 0
                    ? Groups[i - 1].StartIndex
                    : Groups[Groups.Count - 1].StartIndex;
            }
        }

        return 0;
    }

    private static DemoEntry[] BuildEntries()
    {
        return
        [
            // ── Overview ─────────────────────────────────────────────────────
            new("Overview – Demo Index", "Overview",
                "All demos organized by group. Use + / - to step, PageUp / PageDown to jump groups.",
                static (c, i) => new DemoOverviewScene(c, i)),

            // ── Core ─────────────────────────────────────────────────────────
            new("Text Rendering", "Core",
                "Renders text at multiple sizes using the built-in system font.",
                static (c, i) => new TextScene(c, i)),
            new("Clear Color", "Core",
                "Pulsing circle on a solid background. No interactions.",
                static (c, i) => new ClearColorScene(c, i)),
            new("Sprite + Input + Assets", "Core",
                "Move the checker-sprite with WASD or arrow keys.\nTextures and sounds are generated at startup.",
                static (c, i) => new SpriteScene(c, i)),

            // ── ECS ──────────────────────────────────────────────────────────
            new("ECS – Basic World & Query", "ECS",
                "Eight coloured circles bounce via Position, Velocity and Tint components.",
                static (c, i) => new EcsScene(c, i)),
            new("ECS – 3-Component Query", "ECS",
                "Entities with three components; demonstrates World.Query<A,B,C>.",
                static (c, i) => new Ecs3Scene(c, i)),
            new("ECS – Entity Recycling & ForEach", "ECS",
                "Shows entity ID reuse after removal; ForEach iteration over recycled slots.",
                static (c, i) => new EcsRecyclingScene(c, i)),
            new("ECS – Component Ops", "ECS",
                "HasComponent · TryGetComponent · RemoveComponent · World.Clear\nH toggle highlight · T try-get · R remove all · C clear + re-spawn",
                static (c, i) => new EcsComponentOpsScene(c, i)),

            // ── Input ────────────────────────────────────────────────────────
            new("Input - Visualization", "Input",
                "Keyboard and mouse visualized in real time.",
                static (c, i) => new KeyboardMouseScene(c, i)),
            new("Input - Game Controller", "Input",
                "Visualizes both analog sticks and buttons in real time.\nButton indices follow DsHidMini SXS layout. All raw axes shown at bottom.",
                static (c, i) => new GameControllerScene(c, i)),
            new("Input Actions – Named Bindings", "Input",
                "WASD / Space mapped to named actions (MoveUp/Down/Left/Right, Fire).\nShows live action states.",
                static (c, i) => new InputActionScene(c, i)),

            // ── Movement ─────────────────────────────────────────────────────
            new("Movement – No Drift", "Movement",
                "W/S throttle and brake  ·  A/D turn  ·  ` toggle debug overlay",
                static (c, i) => new MovementScene(c, i)),
            new("Movement – With Drift", "Movement",
                "W/S throttle and brake  ·  A/D turn  ·  ` toggle debug overlay",
                static (c, i) => new DriftingScene(c, i)),
            new("Movement – Strafe", "Movement",
                "W/S forward/backward  ·  A/D strafe  ·  ←/→ or mouse  rotate heading  ·  ` toggle debug overlay\nDemonstrates MovementBehavior + standalone RotationBehavior.",
                static (c, i) => new StrafingScene(c, i)),

            // ── Steering ─────────────────────────────────────────────────────
            new("Steering – Wander + Bounds", "Steering",
                "Agent wanders and steers away from window edges.  ` toggle debug",
                static (c, i) => new SteeringScene(c, i)),
            new("Steering – Seek + Arrive", "Steering",
                "Agent follows the mouse cursor.\nSpace toggle Seek / Arrive  ·  ` toggle debug",
                static (c, i) => new SeekArriveScene(c, i)),
            new("Steering – Pursue + Evade", "Steering",
                "Yellow pursues Red using predicted intercept.  ` toggle debug",
                static (c, i) => new PursueEvadeScene(c, i)),
            new("Steering – Wall + Obstacle Avoidance", "Steering",
                "Agent avoids walls and circular obstacles.  ` toggle debug",
                static (c, i) => new AvoidanceScene(c, i)),
            new("Steering – Hide", "Steering",
                "Hiders seek cover behind rocks to evade the Orange threat.  ` toggle debug",
                static (c, i) => new HideScene(c, i)),
            new("Steering – Interpose", "Steering",
                "Cyan agent interposes between Yellow and Blue targets.  ` toggle debug",
                static (c, i) => new InterposeScene(c, i)),
            new("Steering – Follow Path", "Steering",
                "Agent follows one of five looping paths.  1-5 switch presets  ·  Tab toggle smooth / on-dot  ·  ` toggle debug",
                static (c, i) => new FollowPathScene(c, i)),
            new("Steering – Offset Pursuit", "Steering",
                "Followers maintain a V-formation offset behind the leader.  ` toggle debug",
                static (c, i) => new OffsetPursuitScene(c, i)),
            new("Steering – Flocking", "Steering",
                "Boids with separation, alignment and cohesion.  ` toggle debug",
                static (c, i) => new FlockingScene(c, i)),
            new("Steering – Spatial Grid + Debug", "Steering",
                "Flocking accelerated with a spatial hash grid.  ` toggle debug / grid overlay",
                static (c, i) => new FlockingSpatialGridScene(c, i)),
            new("Steering – Flock + Leader", "Steering",
                "Flock follows/flees a leader; Space toggles to follow or flee the leader target.  ` toggle debug",
                static (c, i) => new FlockingLeaderScene(c, i)),

            // ── Rendering ────────────────────────────────────────────────────
            new("Animation – Sprite Sheet", "Rendering",
                "AnimationPlayer drives a 4-frame sprite sheet at 120 ms per frame.",
                static (c, i) => new AnimationScene(c, i)),
            new("Animation – One-Shot & Reset", "Rendering",
                "Non-looping AnimationPlayer.  Space reset + replay  ·  P pause / resume\nShows IsFinished state with colour-coded label.",
                static (c, i) => new AnimationOneShotScene(c, i)),
            new("Animation – Explosion", "Rendering",
                "Multiple looping explosion animations from a texture sequence.\nLeft-click add  ·  Right-click remove  ·  P: pause/resume",
                static (c, i) => new ExplosionAnimationScene(c, i)),
            new("Scene Layers – Background & Foreground", "Rendering",
                "Parallax starfield background, HUD and Pause layer. S scores a point.",
                static (c, i) => new LayeredScene(c, i)),
            new("Layers + Shader + Flock", "Rendering",
                "Starfield, flocking boids, post-process shader and HUD combined in one scene.",
                static (c, i) => new ShaderFlockComboScene(c, i)),

            // ── Shaders ──────────────────────────────────────────────────────
            new("Shader – Grayscale / Tint", "Shaders",
                "Fragment shader desaturates the texture and blends an animated tint colour.",
                static (c, i) => new ShaderGrayscaleScene(c, i)),
            new("Shader – Wave Distortion", "Shaders",
                "Vertex shader displaces pixels with a sine/cosine wave over time.",
                static (c, i) => new ShaderWaveScene(c, i)),
            new("Shader – The Highlander Effect", "Shaders",
                "GLSL 4.6 subgroup election: one fragment per workgroup turns red.",
                static (c, i) => new ShaderHighlanderScene(c, i)),
            new("Shader – Pixelate", "Shaders",
                "Fragment shader quantises UV coordinates. Move mouse to adjust block size.",
                static (c, i) => new ShaderPixelateScene(c, i)),
            new("Shader – Blur", "Shaders",
                "9-tap box blur fragment shader. Move mouse to adjust blur radius.",
                static (c, i) => new ShaderBlurScene(c, i)),
            new("Shader – Storm + Blink", "Shaders",
                "Vertex storm distortion combined with a blinking alpha fragment shader.",
                static (c, i) => new ShaderStormBlinkScene(c, i)),

            // ── Scene Management ─────────────────────────────────────────────
            new("Scene Stack – Push / Pop", "Scene Management",
                "P push Pause overlay  ·  G push Game-Over overlay",
                static (c, i) => new SceneStackScene(c, i)),
            new("Camera – Follow / Zoom / Shake", "Scene Management",
                "WASD move  ·  Z/X zoom in/out  ·  Space screen-shake",
                static (c, i) => new CameraScene(c, i)),
            new("Camera – Rotation & Coordinate Conversion", "Scene Management",
                "Q/E rotate  ·  R reset rotation  ·  LMB ScreenToWorld\nYellow crosshair tracks pinned world object via WorldToScreen.",
                static (c, i) => new CameraExtrasScene(c, i)),
            new("GameTime – TimeScale / Pause", "Scene Management",
                "Space pause / resume  ·  Tab toggle slow-motion (×0.25)\nLeft square uses scaled delta; right always spins.",
                static (c, i) => new TimeScaleScene(c, i)),

            // ── Audio ────────────────────────────────────────────────────────
            new("Audio – Sound Effects", "Audio",
                "Space play beep.  Vibrating rings visualise the audio amplitude.",
                static (c, i) => new AudioScene(c, i)),
            new("Audio – Volume & Mute", "Audio",
                "Streams a music track.  Up/Down master vol  ·  Left/Right music vol  ·  M mute  ·  P pause/resume\nSpace beep SFX  ·  1/2/3 SFX vol 33/66/100%",
                static (c, i) => new MusicScene(c, i)),

            // ── Assets ───────────────────────────────────────────────────────
            new("Assets – Fallback Texture", "Assets",
                "Left: valid texture. Right: missing file → magenta checkerboard fallback.",
                static (c, i) => new AssetFallbackScene(c, i)),
            new("Assets – Shape Textures & Unload", "Assets",
                "ShapeExtensions.ToTexture() · AssetStore.Unload · EngineLogger\nU unload checker  ·  R reload",
                static (c, i) => new ShapeTextureScene(c, i)),

            // ── Pathfinding ──────────────────────────────────────────────────
            new("Pathfinding – Grid Search", "Pathfinding",
                "Tab cycle algorithm (BFS/DFS/Dijkstra/A*/FlowField)  ·  Space clear grid\nLMB paint wall  ·  RMB erase  ·  S set start  ·  E set end  ·  ` toggle visited",
                static (c, i) => new PathfindingScene(c, i)),
            new("Pathfinding – Agent Maze", "Pathfinding",
                "A cycle algorithm  ·  Space new maze  ·  ` toggle debug\nAgent navigates a randomly generated obstacle course.",
                static (c, i) => new PathfindingAgentScene(c, i)),
        ];
    }

    private static DemoGroup[] BuildGroups(IReadOnlyList<DemoEntry> entries)
    {
        List<DemoGroup> groups = [];
        string? currentGroupName = null;
        int startIndex = 0;

        for (int i = 0; i < entries.Count; i++)
        {
            string groupName = entries[i].GroupName;

            if (groupName != currentGroupName)
            {
                if (currentGroupName is not null)
                {
                    groups.Add(new DemoGroup(currentGroupName, startIndex, i));
                }

                currentGroupName = groupName;
                startIndex = i;
            }
        }

        if (currentGroupName is not null)
        {
            groups.Add(new DemoGroup(currentGroupName, startIndex, entries.Count));
        }

        return groups.ToArray();
    }
}
