using GrayHare.GameEngine.Application;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.InputDemo;

/// <summary>
/// Visualizes keyboard and mouse input in real time.
/// All keyboard keys light up when held; the mouse panel shows cursor position,
/// button state, and scroll-wheel activity.
/// </summary>
internal sealed class KeyboardMouseScene : DemoSceneBase
{
    // Key unit stride (rendered width + gap).
    private const float Ku = 36f;
    // Rendered key height.
    private const float Kh = 30f;
    // Gap between adjacent keys.
    private const float Kg = 2f;
    // Rendered key width (unit minus gap).
    private const float Kw = Ku - Kg;
    // Extra vertical gap between the Fn row and the main key block.
    private const float FnGap = 8f;

    // Key-info strip origin (above the keyboard).
    private const float KeyInfoY = 28f;

    // Keyboard top-left origin (shifted down to leave room for key-info strip).
    private const float KbX = 20f;
    private const float KbY = 80f;

    // Navigation cluster: right of the 15-unit-wide main block.
    private const float NavX = KbX + 15f * Ku + 16f;   // 576
    private const float NavY = KbY + Kh + Kg + FnGap;  // 120

    // Numeric keypad: right of navigation cluster.
    private const float NumpadX = NavX + 3f * Ku + 16f; // 700
    private const float NumpadY = NavY;                   // 120

    // Mouse panel: right of numeric keypad.
    private const float MouseX = NumpadX + 4f * Ku + 16f; // 860
    private const float MouseY = KbY;                      // 80

    private static readonly Color _colIdle = new(45, 48, 60);
    private static readonly Color _colPressed = new(80, 200, 255);
    private static readonly Color _colOutline = new(72, 76, 96);
    private static readonly Color _colLabel = new(140, 145, 165);
    private static readonly Color _colLabelOn = Color.White;
    private static readonly Color _colSection = new(180, 190, 220);

    // ── Key row definitions ───────────────────────────────────────────────────

    private readonly record struct KeyDef(string Label, Keyboard.Key Key, float Units = 1f);

    private static readonly KeyDef[] _numberRow =
    [
        new("`",    Keyboard.Key.Grave),
        new("1",    Keyboard.Key.Num1),
        new("2",    Keyboard.Key.Num2),
        new("3",    Keyboard.Key.Num3),
        new("4",    Keyboard.Key.Num4),
        new("5",    Keyboard.Key.Num5),
        new("6",    Keyboard.Key.Num6),
        new("7",    Keyboard.Key.Num7),
        new("8",    Keyboard.Key.Num8),
        new("9",    Keyboard.Key.Num9),
        new("0",    Keyboard.Key.Num0),
        new("-",    Keyboard.Key.Hyphen),
        new("=",    Keyboard.Key.Equal),
        new("Bksp", Keyboard.Key.Backspace, 2f),
    ];

    private static readonly KeyDef[] _qwertyRow =
    [
        new("Tab", Keyboard.Key.Tab,       1.5f),
        new("Q",   Keyboard.Key.Q),
        new("W",   Keyboard.Key.W),
        new("E",   Keyboard.Key.E),
        new("R",   Keyboard.Key.R),
        new("T",   Keyboard.Key.T),
        new("Y",   Keyboard.Key.Y),
        new("U",   Keyboard.Key.U),
        new("I",   Keyboard.Key.I),
        new("O",   Keyboard.Key.O),
        new("P",   Keyboard.Key.P),
        new("[",   Keyboard.Key.LBracket),
        new("]",   Keyboard.Key.RBracket),
        new("\\",  Keyboard.Key.Backslash, 1.5f),
    ];

    private static readonly KeyDef[] _asdfRow =
    [
        // CapsLock has no Keyboard.Key enum value in SFML; always shown unlit.
        new("Caps",  Keyboard.Key.Unknown,   1.75f),
        new("A",     Keyboard.Key.A),
        new("S",     Keyboard.Key.S),
        new("D",     Keyboard.Key.D),
        new("F",     Keyboard.Key.F),
        new("G",     Keyboard.Key.G),
        new("H",     Keyboard.Key.H),
        new("J",     Keyboard.Key.J),
        new("K",     Keyboard.Key.K),
        new("L",     Keyboard.Key.L),
        new(";",     Keyboard.Key.Semicolon),
        new("'",     Keyboard.Key.Apostrophe),
        new("Enter", Keyboard.Key.Enter,     2.25f),
    ];

    private static readonly KeyDef[] _zxcvRow =
    [
        new("Shift", Keyboard.Key.LShift,  2.25f),
        new("Z",     Keyboard.Key.Z),
        new("X",     Keyboard.Key.X),
        new("C",     Keyboard.Key.C),
        new("V",     Keyboard.Key.V),
        new("B",     Keyboard.Key.B),
        new("N",     Keyboard.Key.N),
        new("M",     Keyboard.Key.M),
        new(",",     Keyboard.Key.Comma),
        new(".",     Keyboard.Key.Period),
        new("/",     Keyboard.Key.Slash),
        new("Shift", Keyboard.Key.RShift,  2.75f),
    ];

    private static readonly KeyDef[] _bottomRow =
    [
        new("Ctrl",  Keyboard.Key.LControl, 1.25f),
        new("Win",   Keyboard.Key.LSystem,  1.25f),
        new("Alt",   Keyboard.Key.LAlt,     1.25f),
        new("Space", Keyboard.Key.Space,    6.25f),
        new("Alt",   Keyboard.Key.RAlt,     1.25f),
        new("Win",   Keyboard.Key.RSystem,  1.25f),
        new("Menu",  Keyboard.Key.Menu,     1.25f),
        new("Ctrl",  Keyboard.Key.RControl, 1.25f),
    ];

    private Font _font = null!;
    // Accumulated scroll delta with decay for visual feedback.
    private float _scrollLevel;

    public KeyboardMouseScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex) { }

    public override void Load(GameHost host)
    {
        base.Load(host);
        _font = host.Assets.LoadFont();
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        base.Update(host, in gameTime);

        float wheel = host.Input.MouseWheelDelta;
        if (wheel != 0f)
        {
            _scrollLevel = Math.Clamp(_scrollLevel + wheel * 25f, -50f, 50f);
        }
        else
        {
            float decay = 80f * gameTime.DeltaTotalSeconds;
            if (MathF.Abs(_scrollLevel) <= decay)
            {
                _scrollLevel = 0f;
            }
            else
            {
                _scrollLevel -= MathF.Sign(_scrollLevel) * decay;
            }
        }
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        DrawKeyInfoSection(host, window);
        DrawKeyboard(host, window);
        DrawMousePanel(host, window);
    }

    // ── Keyboard ─────────────────────────────────────────────────────────────

    private void DrawKeyboard(GameHost host, RenderWindow window)
    {
        DrawSectionLabel(window, KbX, KbY - 22f, "Keyboard");
        DrawFnRow(host, window);

        float y = NavY;
        DrawKeyRow(host, window, KbX, y, _numberRow);
        y += Kh + Kg;
        DrawKeyRow(host, window, KbX, y, _qwertyRow);
        y += Kh + Kg;
        DrawKeyRow(host, window, KbX, y, _asdfRow);
        y += Kh + Kg;
        DrawKeyRow(host, window, KbX, y, _zxcvRow);
        y += Kh + Kg;
        DrawKeyRow(host, window, KbX, y, _bottomRow);

        DrawNavCluster(host, window);
        DrawNumpad(host, window);
    }

    private void DrawFnRow(GameHost host, RenderWindow window)
    {
        // Fn keys are slightly narrower than main keys.
        const float fnKu = 33f;
        const float fnKw = fnKu - Kg;
        const float fnGrpGap = 7f;

        float x = KbX;
        float y = KbY;

        DrawKey(window, x, y, Kw, "Esc", host.Input.IsKeyDown(Keyboard.Key.Escape));
        x += Ku + fnGrpGap;

        Keyboard.Key[] f1f4 = [Keyboard.Key.F1, Keyboard.Key.F2, Keyboard.Key.F3, Keyboard.Key.F4];
        Keyboard.Key[] f5f8 = [Keyboard.Key.F5, Keyboard.Key.F6, Keyboard.Key.F7, Keyboard.Key.F8];
        Keyboard.Key[] f9f12 = [Keyboard.Key.F9, Keyboard.Key.F10, Keyboard.Key.F11, Keyboard.Key.F12];

        foreach (Keyboard.Key functionKey in f1f4)
        {
            DrawKey(window, x, y, fnKw, functionKey.ToString(), host.Input.IsKeyDown(functionKey));
            x += fnKu;
        }

        x += fnGrpGap;

        foreach (Keyboard.Key functionKey in f5f8)
        {
            DrawKey(window, x, y, fnKw, functionKey.ToString(), host.Input.IsKeyDown(functionKey));
            x += fnKu;
        }

        x += fnGrpGap;

        foreach (Keyboard.Key functionKey in f9f12)
        {
            DrawKey(window, x, y, fnKw, functionKey.ToString(), host.Input.IsKeyDown(functionKey));
            x += fnKu;
        }

        x += fnGrpGap;

        // Pause is the only Fn-row extra key in the SFML enum; PrtSc and ScrLk are not.
        DrawKey(window, x, y, fnKw, "PrtSc", false);
        x += fnKu;
        DrawKey(window, x, y, fnKw, "ScrLk", false);
        x += fnKu;
        DrawKey(window, x, y, fnKw, "Pause", host.Input.IsKeyDown(Keyboard.Key.Pause));
    }

    private void DrawNavCluster(GameHost host, RenderWindow window)
    {
        float y = NavY;
        DrawKey(window, NavX, y, Kw, "Ins", host.Input.IsKeyDown(Keyboard.Key.Insert));
        DrawKey(window, NavX + Ku, y, Kw, "Home", host.Input.IsKeyDown(Keyboard.Key.Home));
        DrawKey(window, NavX + 2f * Ku, y, Kw, "PgUp", host.Input.IsKeyDown(Keyboard.Key.PageUp));

        y += Kh + Kg;
        DrawKey(window, NavX, y, Kw, "Del", host.Input.IsKeyDown(Keyboard.Key.Delete));
        DrawKey(window, NavX + Ku, y, Kw, "End", host.Input.IsKeyDown(Keyboard.Key.End));
        DrawKey(window, NavX + 2f * Ku, y, Kw, "PgDn", host.Input.IsKeyDown(Keyboard.Key.PageDown));

        // Arrow keys: Up above a blank row, then inverted-T of L/Dn/R below.
        y = NavY + 3f * (Kh + Kg);
        DrawKey(window, NavX + Ku, y, Kw, "Up", host.Input.IsKeyDown(Keyboard.Key.Up));

        y += Kh + Kg;
        DrawKey(window, NavX, y, Kw, "Lt", host.Input.IsKeyDown(Keyboard.Key.Left));
        DrawKey(window, NavX + Ku, y, Kw, "Dn", host.Input.IsKeyDown(Keyboard.Key.Down));
        DrawKey(window, NavX + 2f * Ku, y, Kw, "Rt", host.Input.IsKeyDown(Keyboard.Key.Right));
    }

    private void DrawKeyRow(GameHost host, RenderWindow window, float startX, float y, KeyDef[] keys)
    {
        float x = startX;
        foreach (KeyDef key in keys)
        {
            float w = key.Units * Ku - Kg;
            bool pressed = key.Key != Keyboard.Key.Unknown && host.Input.IsKeyDown(key.Key);
            DrawKey(window, x, y, w, key.Label, pressed);
            x += key.Units * Ku;
        }
    }

    private void DrawKey(RenderWindow window, float x, float y, float w, string label, bool pressed, float h = Kh)
    {
        using RectangleShape rect = new(new Vector2f(w, h))
        {
            Position = new Vector2f(x, y),
            FillColor = pressed ? _colPressed : _colIdle,
            OutlineColor = _colOutline,
            OutlineThickness = 1f
        };
        window.Draw(rect);

        uint fontSize = label.Length <= 2 ? 14u : label.Length <= 4 ? 11u : 9u;
        using Text lbl = new(_font, label, fontSize)
        {
            FillColor = pressed ? _colLabelOn : _colLabel
        };

        FloatRect bounds = lbl.GetLocalBounds();
        lbl.Origin = new Vector2f(bounds.Left + bounds.Width / 2f, bounds.Top + bounds.Height / 2f);
        lbl.Position = new Vector2f(x + w / 2f, y + h / 2f);
        window.Draw(lbl);
    }

    // ── Key-info strip ────────────────────────────────────────────────────────

    private void DrawKeyInfoSection(GameHost host, RenderWindow window)
    {
        const float stripH = 24f;
        float stripW = MouseX - KbX - 10f;

        using RectangleShape strip = new(new Vector2f(stripW, stripH))
        {
            Position = new Vector2f(KbX, KeyInfoY),
            FillColor = new Color(28, 30, 42),
            OutlineColor = new Color(65, 70, 90),
            OutlineThickness = 1f
        };
        window.Draw(strip);

        using Text caption = new(_font, "Keys:", 13)
        {
            Position = new Vector2f(KbX + 6f, KeyInfoY + 4f),
            FillColor = _colSection
        };
        window.Draw(caption);

        float x = KbX + 52f;
        float maxX = KbX + stripW - 10f;
        bool anyPressed = false;

        foreach (Keyboard.Key key in host.Input.CurrentKeys)
        {
            if (x >= maxX)
            {
                break;
            }

            anyPressed = true;
            string chip = $"{key} ({(int)key})";

            using Text t = new(_font, chip, 13)
            {
                Position = new Vector2f(x, KeyInfoY + 4f),
                FillColor = _colPressed
            };

            FloatRect bounds = t.GetLocalBounds();
            x += bounds.Left + bounds.Width + 14f;
            window.Draw(t);
        }

        if (!anyPressed)
        {
            using Text none = new(_font, "\u2014", 13)
            {
                Position = new Vector2f(x, KeyInfoY + 4f),
                FillColor = new Color(70, 75, 95)
            };
            window.Draw(none);
        }
    }

    // ── Numeric keypad ────────────────────────────────────────────────────────

    private void DrawNumpad(GameHost host, RenderWindow window)
    {
        DrawSectionLabel(window, NumpadX, NumpadY - 22f, "Numpad");

        float ky = NumpadY;
        float tallH = 2f * Kh + Kg;

        // Row 0: NumLk / * -
        DrawKey(window, NumpadX, ky, Kw, "NmLk", false);
        DrawKey(window, NumpadX + Ku, ky, Kw, "/", host.Input.IsKeyDown(Keyboard.Key.Divide));
        DrawKey(window, NumpadX + 2f * Ku, ky, Kw, "*", host.Input.IsKeyDown(Keyboard.Key.Multiply));
        DrawKey(window, NumpadX + 3f * Ku, ky, Kw, "-", host.Input.IsKeyDown(Keyboard.Key.Subtract));
        ky += Kh + Kg;

        // Row 1: 7 8 9, tall + starts here spanning rows 1-2.
        DrawKey(window, NumpadX, ky, Kw, "7", host.Input.IsKeyDown(Keyboard.Key.Numpad7));
        DrawKey(window, NumpadX + Ku, ky, Kw, "8", host.Input.IsKeyDown(Keyboard.Key.Numpad8));
        DrawKey(window, NumpadX + 2f * Ku, ky, Kw, "9", host.Input.IsKeyDown(Keyboard.Key.Numpad9));
        DrawKey(window, NumpadX + 3f * Ku, ky, Kw, "+", host.Input.IsKeyDown(Keyboard.Key.Add), tallH);
        ky += Kh + Kg;

        // Row 2: 4 5 6 (col 3 occupied by tall +).
        DrawKey(window, NumpadX, ky, Kw, "4", host.Input.IsKeyDown(Keyboard.Key.Numpad4));
        DrawKey(window, NumpadX + Ku, ky, Kw, "5", host.Input.IsKeyDown(Keyboard.Key.Numpad5));
        DrawKey(window, NumpadX + 2f * Ku, ky, Kw, "6", host.Input.IsKeyDown(Keyboard.Key.Numpad6));
        ky += Kh + Kg;

        // Row 3: 1 2 3, tall Enter starts here spanning rows 3-4.
        DrawKey(window, NumpadX, ky, Kw, "1", host.Input.IsKeyDown(Keyboard.Key.Numpad1));
        DrawKey(window, NumpadX + Ku, ky, Kw, "2", host.Input.IsKeyDown(Keyboard.Key.Numpad2));
        DrawKey(window, NumpadX + 2f * Ku, ky, Kw, "3", host.Input.IsKeyDown(Keyboard.Key.Numpad3));
        DrawKey(window, NumpadX + 3f * Ku, ky, Kw, "En", host.Input.IsKeyDown(Keyboard.Key.Enter), tallH);
        ky += Kh + Kg;

        // Row 4: wide 0 and . (col 3 occupied by tall Enter, Numpad. has no SFML key code).
        DrawKey(window, NumpadX, ky, 2f * Ku - Kg, "0", host.Input.IsKeyDown(Keyboard.Key.Numpad0));
        DrawKey(window, NumpadX + 2f * Ku, ky, Kw, ".", false);
    }

    // ── Mouse panel ───────────────────────────────────────────────────────────

    private void DrawMousePanel(GameHost host, RenderWindow window)
    {
        DrawSectionLabel(window, MouseX, MouseY - 22f, "Mouse");
        DrawMouseSilhouette(host, window);
        DrawMouseInfo(host, window);
    }

    private void DrawMouseSilhouette(GameHost host, RenderWindow window)
    {
        const float bodyW = 140f;
        const float btnH = 72f;
        const float bodyH = 148f;
        const float lbW = 62f;
        const float mbW = 16f;

        float x = MouseX;
        float y = MouseY;

        bool lmb = host.Input.IsMouseButtonDown(Mouse.Button.Left);
        bool mmb = host.Input.IsMouseButtonDown(Mouse.Button.Middle);
        bool rmb = host.Input.IsMouseButtonDown(Mouse.Button.Right);

        // Main body fill.
        using RectangleShape body = new(new Vector2f(bodyW, btnH + bodyH))
        {
            Position = new Vector2f(x, y),
            FillColor = new Color(32, 35, 47),
            OutlineColor = Color.Transparent,
            OutlineThickness = 0f
        };
        window.Draw(body);

        // Left mouse button.
        using RectangleShape lb = new(new Vector2f(lbW, btnH))
        {
            Position = new Vector2f(x, y),
            FillColor = lmb ? new Color(80, 160, 255) : new Color(42, 46, 62)
        };
        window.Draw(lb);

        // Right mouse button.
        using RectangleShape rb = new(new Vector2f(lbW, btnH))
        {
            Position = new Vector2f(x + bodyW - lbW, y),
            FillColor = rmb ? new Color(255, 100, 100) : new Color(42, 46, 62)
        };
        window.Draw(rb);

        // Middle button strip.
        float mX = x + lbW;
        using RectangleShape mb = new(new Vector2f(mbW, btnH - 10f))
        {
            Position = new Vector2f(mX, y),
            FillColor = mmb ? new Color(80, 210, 80) : new Color(38, 41, 55)
        };
        window.Draw(mb);

        DrawScrollWheelGlyph(window, mX + mbW / 2f, y + (btnH - 10f) / 2f);

        // Dividers between button regions.
        DrawVLine(window, x + lbW, y, y + btnH, new Color(55, 60, 78));
        DrawVLine(window, x + lbW + mbW, y, y + btnH, new Color(55, 60, 78));

        // Body outline drawn last so it sits on top of button fills.
        using RectangleShape outline = new(new Vector2f(bodyW, btnH + bodyH))
        {
            Position = new Vector2f(x, y),
            FillColor = Color.Transparent,
            OutlineColor = new Color(70, 75, 95),
            OutlineThickness = 2f
        };
        window.Draw(outline);

        // Button labels beneath the button row.
        float labelY = y + btnH + 6f;
        DrawCenteredLabel(window, x + lbW / 2f, labelY, "L", lmb, new Color(80, 160, 255));
        DrawCenteredLabel(window, x + lbW + mbW / 2f, labelY, "M", mmb, new Color(80, 210, 80));
        DrawCenteredLabel(window, x + bodyW - lbW / 2f, labelY, "R", rmb, new Color(255, 100, 100));
    }

    private void DrawScrollWheelGlyph(RenderWindow window, float cx, float cy)
    {
        const float wW = 6f;
        const float wH = 18f;

        using RectangleShape wheel = new(new Vector2f(wW, wH))
        {
            Origin = new Vector2f(wW / 2f, wH / 2f),
            Position = new Vector2f(cx, cy),
            FillColor = new Color(110, 115, 140),
            OutlineColor = new Color(155, 160, 185),
            OutlineThickness = 1f
        };
        window.Draw(wheel);

        if (MathF.Abs(_scrollLevel) > 2f)
        {
            bool up = _scrollLevel > 0f;
            DrawScrollArrow(window, cx, up ? cy - 22f : cy + 8f, up);
        }
    }

    private static void DrawScrollArrow(RenderWindow window, float cx, float tipY, bool up)
    {
        using ConvexShape arrow = new(3)
        {
            FillColor = new Color(200, 210, 230)
        };

        if (up)
        {
            arrow.SetPoint(0, new Vector2f(cx, tipY));
            arrow.SetPoint(1, new Vector2f(cx - 5f, tipY + 8f));
            arrow.SetPoint(2, new Vector2f(cx + 5f, tipY + 8f));
        }
        else
        {
            arrow.SetPoint(0, new Vector2f(cx, tipY + 8f));
            arrow.SetPoint(1, new Vector2f(cx - 5f, tipY));
            arrow.SetPoint(2, new Vector2f(cx + 5f, tipY));
        }

        window.Draw(arrow);
    }

    private void DrawMouseInfo(GameHost host, RenderWindow window)
    {
        float x = MouseX + 160f;
        float y = MouseY;

        bool lmb = host.Input.IsMouseButtonDown(Mouse.Button.Left);
        bool mmb = host.Input.IsMouseButtonDown(Mouse.Button.Middle);
        bool rmb = host.Input.IsMouseButtonDown(Mouse.Button.Right);

        Vector2i pos = host.Input.MousePosition;
        using Text posText = new(_font, $"X: {pos.X,4}   Y: {pos.Y,4}", 16)
        {
            Position = new Vector2f(x, y),
            FillColor = new Color(180, 190, 215)
        };
        window.Draw(posText);

        DrawInfoRow(window, x, y + 32f, "Left   (LMB)", lmb, new Color(80, 160, 255));
        DrawInfoRow(window, x, y + 58f, "Middle (MMB)", mmb, new Color(80, 210, 80));
        DrawInfoRow(window, x, y + 84f, "Right  (RMB)", rmb, new Color(255, 100, 100));

        DrawScrollBar(window, x, y + 116f);
    }

    private void DrawInfoRow(RenderWindow window, float x, float y, string label, bool pressed, Color activeColor)
    {
        const float dotR = 6f;
        using CircleShape dot = new(dotR)
        {
            Origin = new Vector2f(dotR, dotR),
            Position = new Vector2f(x + dotR, y + dotR),
            FillColor = pressed ? activeColor : new Color(40, 43, 58),
            OutlineColor = pressed ? activeColor : new Color(80, 85, 105),
            OutlineThickness = 1.5f
        };
        window.Draw(dot);

        using Text lbl = new(_font, label, 15)
        {
            Position = new Vector2f(x + 18f, y),
            FillColor = pressed ? activeColor : _colLabel
        };
        window.Draw(lbl);
    }

    private void DrawScrollBar(RenderWindow window, float x, float y)
    {
        using Text title = new(_font, "Scroll", 14)
        {
            Position = new Vector2f(x, y),
            FillColor = _colLabel
        };
        window.Draw(title);

        const float barW = 160f;
        const float barH = 14f;
        float barY = y + 20f;

        using RectangleShape bg = new(new Vector2f(barW, barH))
        {
            Position = new Vector2f(x, barY),
            FillColor = new Color(35, 38, 50),
            OutlineColor = new Color(70, 75, 95),
            OutlineThickness = 1f
        };
        window.Draw(bg);

        // Center tick.
        DrawVLine(window, x + barW / 2f, barY, barY + barH, new Color(80, 85, 105));

        if (MathF.Abs(_scrollLevel) > 0.5f)
        {
            float norm = MathF.Abs(_scrollLevel) / 50f;
            float fillW = norm * (barW / 2f);
            float fillX = _scrollLevel > 0f ? x + barW / 2f : x + barW / 2f - fillW;
            Color fillColor = _scrollLevel > 0f
                ? new Color(80, 200, 255)
                : new Color(255, 160, 80);

            using RectangleShape fill = new(new Vector2f(fillW, barH - 2f))
            {
                Position = new Vector2f(fillX, barY + 1f),
                FillColor = fillColor
            };
            window.Draw(fill);
        }
    }

    // ── Shared helpers ────────────────────────────────────────────────────────

    private void DrawSectionLabel(RenderWindow window, float x, float y, string text)
    {
        using Text lbl = new(_font, text, 16)
        {
            Position = new Vector2f(x, y),
            FillColor = _colSection
        };
        window.Draw(lbl);
    }

    private void DrawCenteredLabel(RenderWindow window, float cx, float y, string text, bool active, Color activeColor)
    {
        using Text lbl = new(_font, text, 14)
        {
            FillColor = active ? activeColor : _colLabel
        };
        FloatRect b = lbl.GetLocalBounds();
        lbl.Origin = new Vector2f(b.Left + b.Width / 2f, b.Top);
        lbl.Position = new Vector2f(cx, y);
        window.Draw(lbl);
    }

    private static void DrawVLine(RenderWindow window, float x, float y1, float y2, Color color)
    {
        using RectangleShape line = new(new Vector2f(1f, y2 - y1))
        {
            Position = new Vector2f(x, y1),
            FillColor = color
        };
        window.Draw(line);
    }
}
