using System.Text;

namespace GrayHare.GameEngine.DemoHub;

internal static class DemoAssets
{
    public static DemoAssetsManifest EnsureCreated(string contentRoot)
    {
        Directory.CreateDirectory(contentRoot);

        string generatedPath = Path.Combine(contentRoot, "generated");
        Directory.CreateDirectory(generatedPath);

        string checkerTexturePath = Path.Combine(generatedPath, "checker.ppm");
        string spriteSheetPath = Path.Combine(generatedPath, "spritesheet.ppm");
        string beepSoundPath = Path.Combine(generatedPath, "beep.wav");

        File.WriteAllText(checkerTexturePath, CreateCheckerPpm(96, 96));
        File.WriteAllText(spriteSheetPath, CreateSpriteSheetPpm(32, 32, 4));
        File.WriteAllBytes(beepSoundPath, CreateBeepWav(22050, 440f, 0.35f));

        string shadersPath = Path.Combine(generatedPath, "shaders");
        Directory.CreateDirectory(shadersPath);

        string grayscaleFragPath = Path.Combine(shadersPath, "grayscale.frag");
        string waveVertPath = Path.Combine(shadersPath, "wave.vert");
        string waveFragPath = Path.Combine(shadersPath, "wave.frag");
        string highlanderFragPath = Path.Combine(shadersPath, "highlander.frag");
        string pixelateFragPath = Path.Combine(shadersPath, "pixelate.frag");
        string blurFragPath = Path.Combine(shadersPath, "blur.frag");
        string blinkFragPath = Path.Combine(shadersPath, "blink.frag");
        string stormVertPath = Path.Combine(shadersPath, "storm.frag");

        File.WriteAllText(grayscaleFragPath, GrayscaleFragSource);
        File.WriteAllText(waveVertPath, WaveVertSource);
        File.WriteAllText(waveFragPath, WaveFragSource);
        File.WriteAllText(highlanderFragPath, TheHighlanderFragSource);
        File.WriteAllText(pixelateFragPath, PixelateFragSource);
        File.WriteAllText(blurFragPath, BlurFragSource);
        File.WriteAllText(blinkFragPath, BlinkFragSource);
        File.WriteAllText(stormVertPath, StormFragSource);

        return new DemoAssetsManifest(
            Path.GetRelativePath(contentRoot, checkerTexturePath),
            Path.GetRelativePath(contentRoot, spriteSheetPath),
            Path.GetRelativePath(contentRoot, beepSoundPath),
            Path.GetRelativePath(contentRoot, grayscaleFragPath),
            Path.GetRelativePath(contentRoot, waveVertPath),
            Path.GetRelativePath(contentRoot, waveFragPath),
            Path.GetRelativePath(contentRoot, highlanderFragPath),
            Path.GetRelativePath(contentRoot, pixelateFragPath),
            Path.GetRelativePath(contentRoot, blurFragPath),
            Path.GetRelativePath(contentRoot, blinkFragPath),
            Path.GetRelativePath(contentRoot, stormVertPath));
    }

    private static string CreateCheckerPpm(int width, int height)
    {
        StringBuilder builder = new();
        builder.AppendLine("P3");
        builder.AppendLine($"{width} {height}");
        builder.AppendLine("255");

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool isPrimary = ((x / 16) + (y / 16)) % 2 == 0;
                builder.Append(isPrimary ? "255 180 64 " : "64 180 255 ");
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string CreateSpriteSheetPpm(int frameWidth, int frameHeight, int frameCount)
    {
        int width = frameWidth * frameCount;
        StringBuilder builder = new();
        builder.AppendLine("P3");
        builder.AppendLine($"{width} {frameHeight}");
        builder.AppendLine("255");

        (int r, int g, int b)[] palette =
        [
            (255, 90, 90),
            (255, 220, 90),
            (90, 220, 120),
            (90, 150, 255)
        ];

        for (int y = 0; y < frameHeight; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int frameIndex = x / frameWidth;
                (int r, int g, int b) color = palette[frameIndex % palette.Length];
                bool stripe = (x + y) % 8 < 4;
                builder.Append(stripe
                    ? $"{color.r} {color.g} {color.b} "
                    : "32 32 32 ");
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static byte[] CreateBeepWav(int sampleRate, float frequency, float durationSeconds)
    {
        int sampleCount = (int)(sampleRate * durationSeconds);
        short[] samples = new short[sampleCount];

        for (int index = 0; index < sampleCount; index++)
        {
            double sample = Math.Sin(2 * Math.PI * frequency * index / sampleRate);
            samples[index] = (short)(sample * short.MaxValue * 0.35);
        }

        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream, Encoding.ASCII, leaveOpen: true);

        const short Channels = 1;
        const short BitsPerSample = 16;
        short blockAlign = Channels * BitsPerSample / 8;
        int byteRate = sampleRate * blockAlign;
        int dataSize = samples.Length * blockAlign;

        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataSize);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(Channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(BitsPerSample);
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(dataSize);

        foreach (short sample in samples)
        {
            writer.Write(sample);
        }

        writer.Flush();
        return stream.ToArray();
    }

    // ── GLSL shader sources ───────────────────────────────────────────────────

    /// <summary>
    /// Fragment shader that desaturates the texture to grayscale and blends
    /// it with an animated <c>u_tint</c> colour uniform.
    /// </summary>
    private const string GrayscaleFragSource = """
        #version 140
        uniform sampler2D u_texture;
        uniform vec4 u_tint;

        void main()
        {
            vec4 pixel = texture2D(u_texture, gl_TexCoord[0].xy) * gl_Color;
            float gray = dot(pixel.rgb, vec3(0.299, 0.587, 0.114));
            gl_FragColor = vec4(vec3(gray) * u_tint.rgb, pixel.a * u_tint.a);
        }
        """;

    /// <summary>
    /// Vertex shader that displaces each vertex with a sine/cosine wave
    /// driven by the <c>u_time</c> uniform (seconds elapsed).
    /// </summary>
    private const string WaveVertSource = """
        #version 140
        uniform float u_time;

        void main()
        {
            vec4 pos = gl_Vertex;
            pos.x += sin(pos.y * 0.04 + u_time * 2.5) * 10.0;
            pos.y += cos(pos.x * 0.03 + u_time * 1.8) * 8.0;
            gl_Position = gl_ModelViewProjectionMatrix * pos;
            gl_TexCoord[0] = gl_TextureMatrix[0] * gl_MultiTexCoord0;
            gl_FrontColor = gl_Color;
        }
        """;

    /// <summary>Fragment shader counterpart to <see cref="WaveVertSource"/>.</summary>
    private const string WaveFragSource = """
        #version 140
        uniform sampler2D u_texture;

        void main()
        {
            gl_FragColor = gl_Color * texture2D(u_texture, gl_TexCoord[0].xy);
        }
        """;

    private const string TheHighlanderFragSource = """
        #version 460 core
        
        // Output color
        layout(location = 0) out vec4 FragColor;
        
        void main()
        {
            // 4.60 Core Feature: Subgroup Election
            // This tells the GPU: "Pick one leader in this hardware group"
            if (subgroupElect()) 
            {
                // The 'Leader' pixel turns Red
                FragColor = vec4(1.0, 0.0, 0.0, 1.0);
            }
            else 
            {
                // Everyone else turns Dark Blue
                FragColor = vec4(0.0, 0.0, 0.2, 1.0);
            }
        }
        """;

    /// <summary>
    /// Fragment shader that pixelates the texture by quantizing coordinates.
    /// Based on SFML pixelate shader example.
    /// </summary>
    private const string PixelateFragSource = """
        #version 140
        uniform sampler2D u_texture;
        uniform float u_pixel_threshold;

        void main()
        {
            float factor = 1.0 / (u_pixel_threshold + 0.001);
            vec2 pos = floor(gl_TexCoord[0].xy * factor + 0.5) / factor;
            gl_FragColor = texture2D(u_texture, pos) * gl_Color;
        }
        """;

    /// <summary>
    /// Fragment shader that applies a 9-tap box blur filter.
    /// Based on SFML blur shader example.
    /// </summary>
    private const string BlurFragSource = """
        #version 140
        uniform sampler2D u_texture;
        uniform float u_blur_radius;

        void main()
        {
            vec2 offx = vec2(u_blur_radius, 0.0);
            vec2 offy = vec2(0.0, u_blur_radius);

            vec4 pixel = texture2D(u_texture, gl_TexCoord[0].xy)               * 4.0 +
                         texture2D(u_texture, gl_TexCoord[0].xy - offx)        * 2.0 +
                         texture2D(u_texture, gl_TexCoord[0].xy + offx)        * 2.0 +
                         texture2D(u_texture, gl_TexCoord[0].xy - offy)        * 2.0 +
                         texture2D(u_texture, gl_TexCoord[0].xy + offy)        * 2.0 +
                         texture2D(u_texture, gl_TexCoord[0].xy - offx - offy) * 1.0 +
                         texture2D(u_texture, gl_TexCoord[0].xy - offx + offy) * 1.0 +
                         texture2D(u_texture, gl_TexCoord[0].xy + offx - offy) * 1.0 +
                         texture2D(u_texture, gl_TexCoord[0].xy + offx + offy) * 1.0;

            gl_FragColor = gl_Color * (pixel / 16.0);
        }
        """;

    private const string BlinkFragSource = """
        #version 140
        uniform float blink_alpha;

        void main()
        {
            vec4 pixel = gl_Color;
            pixel.a = blink_alpha;
        	gl_FragColor = pixel;
        }
        """;

    private const string StormFragSource = """
        #version 140
        uniform vec2 storm_position;
        uniform float storm_total_radius;
        uniform float storm_inner_radius;

        void main()
        {
            vec4 vertex = gl_ModelViewMatrix * gl_Vertex;
            vec2 offset = vertex.xy - storm_position;
            float len = length(offset);
            if (len < storm_total_radius)
            {
                float push_distance = storm_inner_radius + len / storm_total_radius * (storm_total_radius - storm_inner_radius);
                vertex.xy = storm_position + normalize(offset) * push_distance;
            }

        	gl_Position = gl_ProjectionMatrix * vertex;
        	gl_TexCoord[0] = gl_TextureMatrix[0] * gl_MultiTexCoord0;
        	gl_FrontColor = gl_Color;
        }
        """;
}
