using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Extensions;
using SFML.Graphics;
using SFML.System;

namespace GrayHare.GameEngine.DemoHub.Scenes.MovementDemos;

internal sealed class NebulaLayer : ISceneLayer
{
    private uint _width;
    private uint _height;
    private Vector2f _offset;
    private bool _textureNeedsUpdate;
    private readonly List<NebulaCloud> _clouds;
    private RenderTexture? _nebulaTexture;
    private Sprite? _nebulaSprite;
    private readonly Random _random = Random.Shared;

    public int RenderOrder => -24;

    public NebulaLayer(uint cloudCount)
    {
        _clouds = new List<NebulaCloud>((int)cloudCount);
    }

    public void Load(GameHost host)
    {
        _width = host.Window.Size.X;
        _height = host.Window.Size.Y;

        _offset = Constants.Vectors.Zero;
        _textureNeedsUpdate = true;

        _nebulaTexture = new RenderTexture(new Vector2u(_width, _height));
        _nebulaSprite = new Sprite(_nebulaTexture.Texture);

        _clouds.Clear();
        GenerateNebula(_clouds.Capacity);
    }

    public void Unload(GameHost host)
    {
        _nebulaTexture?.Dispose();
        _nebulaSprite?.Dispose();
    }

    public void Move(Vector2f delta)
    {
        _offset += delta;
        _textureNeedsUpdate = true;
    }

    public void Update(GameHost host, in GameTime gameTime)
    {
        var deltaTime = gameTime.DeltaTotalSeconds;

        bool hasChanges = false;

        for (int i = 0; i < _clouds.Count; i++)
        {
            NebulaCloud cloud = _clouds[i];
            cloud.AnimationPhase += cloud.AnimationSpeed * deltaTime;
            cloud.RotationAngle += cloud.RotationSpeed * deltaTime;

            // Slowly morph the shape over time
            for (int j = 0; j < cloud.RadiusVariations.Length; j++)
            {
                float morphSpeed = 0.1f;
                float target = (float)((_random.NextDouble() * 0.6) + 0.7);
                cloud.RadiusVariations[j] += (target - cloud.RadiusVariations[j]) * morphSpeed * deltaTime;
            }

            _clouds[i] = cloud;
            hasChanges = true;
        }

        if (hasChanges)
        {
            _textureNeedsUpdate = true;
        }
    }

    public void RenderLayer(GameHost host, RenderWindow window)
    {
        if (_textureNeedsUpdate)
        {
            RenderNebulaToTexture(window.Size);
        }

        window.Draw(_nebulaSprite);
    }

    private void GenerateNebula(int cloudCount)
    {
        Color[] nebulaColors = new[]
        {
            new Color(138, 43, 226, 100),   // Blue-violet
            new Color(75, 0, 130, 100),     // Indigo
            new Color(255, 20, 147, 100),   // Deep pink
            new Color(0, 191, 255, 100),    // Deep sky blue
            new Color(218, 112, 214, 100),  // Orchid
            new Color(72, 61, 139, 100),    // Dark slate blue
            new Color(199, 21, 133, 100),   // Medium violet red
            new Color(147, 112, 219, 100)   // Medium purple
        };

        for (int i = 0; i < cloudCount; i++)
        {
            // Create random radius variations for each segment to make irregular shapes
            int segmentCount = 32;
            float[] radiusVariations = new float[segmentCount];
            for (int j = 0; j < segmentCount; j++)
            {
                radiusVariations[j] = (float)((_random.NextDouble() * 0.6) + 0.7); // 0.7-1.3 variation
            }

            NebulaCloud cloud = new NebulaCloud
            {
                Position = new Vector2f(
                    (float)(_random.NextDouble() * _width),
                    (float)(_random.NextDouble() * _height)
                ),
                Radius = (float)((_random.NextDouble() * 120) + 60), // 60-180 pixels
                Color = nebulaColors[_random.Next(nebulaColors.Length)],
                Opacity = (float)((_random.NextDouble() * 0.4) + 0.3), // 0.3-0.7
                AnimationPhase = (float)(_random.NextDouble() * MathF.PI * 2),
                AnimationSpeed = (float)((_random.NextDouble() * 0.5) + 0.3), // 0.3-0.8
                RadiusVariations = radiusVariations,
                RotationAngle = (float)(_random.NextDouble() * MathF.PI * 2),
                RotationSpeed = (float)((_random.NextDouble() * 0.2) - 0.1) // -0.1 to 0.1 radians/sec
            };

            _clouds.Add(cloud);
        }
    }

    private void RenderNebulaToTexture(Vector2u windowSize)
    {
        _nebulaTexture!.Clear(new Color(0, 0, 5)); // Very dark blue background

        foreach (NebulaCloud cloud in _clouds)
        {
            Vector2f wrappedPos = (cloud.Position + _offset).WrapPosition(windowSize);

            DrawNebulaCloud(wrappedPos, cloud);

            // Handle wrapping at edges
            if (wrappedPos.X + cloud.Radius > _width)
            {
                DrawNebulaCloud(new Vector2f(wrappedPos.X - _width, wrappedPos.Y), cloud);
            }
            if (wrappedPos.X - cloud.Radius < 0)
            {
                DrawNebulaCloud(new Vector2f(wrappedPos.X + _width, wrappedPos.Y), cloud);
            }
            if (wrappedPos.Y + cloud.Radius > _height)
            {
                DrawNebulaCloud(new Vector2f(wrappedPos.X, wrappedPos.Y - _height), cloud);
            }
            if (wrappedPos.Y - cloud.Radius < 0)
            {
                DrawNebulaCloud(new Vector2f(wrappedPos.X, wrappedPos.Y + _height), cloud);
            }

            // Handle corners
            if (wrappedPos.X + cloud.Radius > _width && wrappedPos.Y + cloud.Radius > _height)
            {
                DrawNebulaCloud(new Vector2f(wrappedPos.X - _width, wrappedPos.Y - _height), cloud);
            }
            if (wrappedPos.X - cloud.Radius < 0 && wrappedPos.Y + cloud.Radius > _height)
            {
                DrawNebulaCloud(new Vector2f(wrappedPos.X + _width, wrappedPos.Y - _height), cloud);
            }
            if (wrappedPos.X + cloud.Radius > _width && wrappedPos.Y - cloud.Radius < 0)
            {
                DrawNebulaCloud(new Vector2f(wrappedPos.X - _width, wrappedPos.Y + _height), cloud);
            }
            if (wrappedPos.X - cloud.Radius < 0 && wrappedPos.Y - cloud.Radius < 0)
            {
                DrawNebulaCloud(new Vector2f(wrappedPos.X + _width, wrappedPos.Y + _height), cloud);
            }
        }

        _nebulaTexture.Display();
        _textureNeedsUpdate = false;
    }

    private void DrawNebulaCloud(Vector2f position, NebulaCloud cloud)
    {
        int segments = cloud.RadiusVariations.Length;
        float currentOpacity = cloud.Opacity * (1.0f + (MathF.Sin(cloud.AnimationPhase) * 0.2f));

        // Create multiple layers for depth effect
        for (int layer = 0; layer < 3; layer++)
        {
            float layerRadius = cloud.Radius * (1.0f - (layer * 0.25f));
            byte alpha = (byte)(cloud.Color.A * currentOpacity * (1.0f - (layer * 0.3f)));

            // Create irregular shape using ConvexShape
            using ConvexShape shape = new ConvexShape((uint)segments);

            for (int i = 0; i < segments; i++)
            {
                float angle = (i / (float)segments * MathF.PI * 2) + cloud.RotationAngle;
                float variation = cloud.RadiusVariations[i];

                // Add some smoothing between adjacent points
                float nextVariation = cloud.RadiusVariations[(i + 1) % segments];
                float smoothedVariation = (variation * 0.7f) + (nextVariation * 0.3f);

                float r = layerRadius * smoothedVariation;
                float x = MathF.Cos(angle) * r;
                float y = MathF.Sin(angle) * r;

                shape.SetPoint((uint)i, new Vector2f(x, y));
            }

            shape.Position = position;
            shape.FillColor = new Color(cloud.Color.R, cloud.Color.G, cloud.Color.B, alpha);

            _nebulaTexture!.Draw(shape, new RenderStates(BlendMode.Add));
        }
    }

    private struct NebulaCloud
    {
        public Vector2f Position;
        public float Radius;
        public Color Color;
        public float Opacity;
        public float AnimationPhase;
        public float AnimationSpeed;
        public float[] RadiusVariations;
        public float RotationAngle;
        public float RotationSpeed;
    }
}
