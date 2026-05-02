using GrayHare.GameEngine.Application;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.GameControllerDemo;

/// <summary>
/// Game controller tester — shows both analog sticks and buttons
/// arranged in DS3 layout. Button indices follow the DsHidMini SXS mapping.
/// All raw axis values are shown at the bottom for driver-specific debugging.
/// </summary>
internal sealed class GameControllerScene : DemoSceneBase
{
    // DS3 button indices (confirmed via DsHidMini SXS)
    private const uint BtnCross = 0;
    private const uint BtnCircle = 1;
    private const uint BtnSquare = 2;
    private const uint BtnTriangle = 3;
    private const uint BtnL1 = 4;
    private const uint BtnR1 = 5;
    private const uint BtnSelect = 6;
    private const uint BtnStart = 7;
    private const uint BtnL3 = 8;
    private const uint BtnR3 = 9;
    private const uint BtnPS = 10;
    // D-pad: PovX (Left=-100, Right=+100) / PovY (Up=+100, Down=-100) — no button indices
    // L2: Axis.Z = +100  |  R2: Axis.Z = -100 — no button indices

    // Threshold for treating a POV hat axis as a D-pad press (range is -100 to +100).
    private const float PovThreshold = 50f;

    // DS3 face-button colors
    private static readonly Color _colTriangle = new(100, 210, 130);
    private static readonly Color _colCircle = new(210, 80, 80);
    private static readonly Color _colCross = new(80, 130, 210);
    private static readonly Color _colSquare = new(200, 100, 180);
    private static readonly Color _colActive = new(80, 200, 255);
    private static readonly Color _colDim = new(50, 55, 70);
    private static readonly Color _colOutline = new(78, 83, 100);

    private Font _font = null!;

    public GameControllerScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex) { }

    public override void Load(GameHost host)
    {
        base.Load(host);
        _font = host.Assets.LoadFont();
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        bool connected = host.Input.IsJoystickConnected(0);

        DrawHeader(window, connected);

        if (connected)
        {
            DrawController(host, window);
        }
        else
        {
            DrawNotConnected(window);
        }

        DrawFooter(host, window, connected);
    }

    private void DrawHeader(RenderWindow window, bool connected)
    {
        string statusText = connected ? "\u25cf CONNECTED" : "\u25cb NOT CONNECTED";
        Color statusColor = connected ? new Color(100, 255, 100) : new Color(255, 100, 100);
        float statusX = connected ? 1060f : 990f;

        using Text status = new(_font, statusText, 20)
        {
            Position = new Vector2f(statusX, 22f),
            FillColor = statusColor
        };
        window.Draw(status);
    }

    private void DrawNotConnected(RenderWindow window)
    {
        using Text msg = new(_font,
            "Connect a Game controller to joystick port 0.", 20)
        {
            Position = new Vector2f(180f, 290f),
            FillColor = new Color(180, 185, 205)
        };
        window.Draw(msg);

        using Text hint = new(_font,
            "Supported drivers:  DsHidMini (recommended)  \u00b7  MotioninJoy  \u00b7  BthPS3", 15)
        {
            Position = new Vector2f(280f, 328f),
            FillColor = new Color(110, 115, 135)
        };
        window.Draw(hint);
    }

    private void DrawController(GameHost host, RenderWindow window)
    {
        DrawShoulders(host, window);
        DrawButtonGrid(host, window);
        DrawDPad(host, window, cx: 330f, cy: 264f);
        DrawFaceButtons(host, window, cx: 880f, cy: 264f);
        DrawCenterButtons(host, window);

        DrawAnalogStick(host, window, cx: 455f, cy: 420f, Joystick.Axis.X, Joystick.Axis.Y, BtnL3, "L3");
        DrawAnalogStick(host, window, cx: 760f, cy: 420f, Joystick.Axis.U, Joystick.Axis.V, BtnR3, "R3");
        DrawTriggerBars(host, window);
    }

    /// <summary>
    /// Draws a row of 16 numbered button boxes centred horizontally.
    /// Each box lights up with its raw index when the corresponding button is held.
    /// Use this to map physical buttons to the driver's button indices.
    /// </summary>
    private void DrawButtonGrid(GameHost host, RenderWindow window)
    {
        const float BoxW = 58f;
        const float BoxH = 22f;
        const float GapX = 4f;
        const float StartX = (1280f - 16f * (BoxW + GapX) + GapX) / 2f; // = 144
        const float Y = 143f;

        using Text heading = new(_font, "Button IDs  (press a button to see its raw index)", 12)
        {
            Position = new Vector2f(StartX, 127f),
            FillColor = new Color(88, 93, 113)
        };
        window.Draw(heading);

        for (uint i = 0; i < 16; i++)
        {
            bool pressed = host.Input.IsJoystickButtonDown(0, i);
            bool unused = i > 10;
            float x = StartX + i * (BoxW + GapX);

            using RectangleShape box = new(new Vector2f(BoxW, BoxH))
            {
                Position = new Vector2f(x, Y),
                FillColor = pressed ? _colActive : unused ? new Color(32, 33, 42) : _colDim,
                OutlineColor = pressed ? _colActive : unused ? new Color(48, 50, 63) : _colOutline,
                OutlineThickness = 1f
            };
            window.Draw(box);

            using Text lbl = new(_font, i.ToString(), 13)
            {
                Position = new Vector2f(x + BoxW / 2f - (i >= 10 ? 7f : 4f), Y + 3f),
                FillColor = pressed ? Color.Black : unused ? new Color(62, 65, 80) : new Color(140, 148, 168)
            };
            window.Draw(lbl);
        }
    }

    private void DrawShoulders(GameHost host, RenderWindow window)
    {
        float z = host.Input.GetJoystickAxis(0, Joystick.Axis.Z);

        DrawShoulderBar(window, z > PovThreshold, "L2", 270f, 55f, 120f, 30f);
        DrawShoulderBar(window, z < -PovThreshold, "R2", 820f, 55f, 120f, 30f);
        DrawShoulderBar(window, host.Input.IsJoystickButtonDown(0, BtnL1), "L1", 270f, 89f, 120f, 30f);
        DrawShoulderBar(window, host.Input.IsJoystickButtonDown(0, BtnR1), "R1", 820f, 89f, 120f, 30f);
    }

    private void DrawShoulderBar(
        RenderWindow window,
        bool pressed, string label,
        float x, float y, float w, float h)
    {
        using RectangleShape rect = new(new Vector2f(w, h))
        {
            Position = new Vector2f(x, y),
            FillColor = pressed ? _colActive : _colDim,
            OutlineColor = pressed ? _colActive : _colOutline,
            OutlineThickness = 2f
        };
        window.Draw(rect);

        using Text lbl = new(_font, label, 18)
        {
            Position = new Vector2f(x + w / 2f - 10f, y + 6f),
            FillColor = pressed ? Color.White : new Color(160, 168, 188)
        };
        window.Draw(lbl);
    }

    private void DrawDPad(GameHost host, RenderWindow window, float cx, float cy)
    {
        float povX = host.Input.GetJoystickAxis(0, Joystick.Axis.PovX);
        float povY = host.Input.GetJoystickAxis(0, Joystick.Axis.PovY);

        // D-pad uses POV hat only. PovY: Up=+100, Down=-100. PovX: Left=-100, Right=+100.
        bool up = povY > PovThreshold;
        bool down = povY < -PovThreshold;
        bool left = povX < -PovThreshold;
        bool right = povX > PovThreshold;

        const float Size = 36f;
        const float Gap = 3f;

        DrawDPadArm(window, cx - Size / 2f, cy - Size - Gap, Size, Size, up, "\u25b2");
        DrawDPadArm(window, cx - Size / 2f, cy + Gap, Size, Size, down, "\u25bc");
        DrawDPadArm(window, cx - Size - Gap, cy - Size / 2f, Size, Size, left, "\u25c4");
        DrawDPadArm(window, cx + Gap, cy - Size / 2f, Size, Size, right, "\u25ba");
        DrawDPadCenter(window, cx - Size / 2f, cy - Size / 2f, Size);

        using Text lbl = new(_font, "D-Pad", 14)
        {
            Position = new Vector2f(cx - 20f, cy + Size + Gap + 6f),
            FillColor = new Color(110, 115, 135)
        };
        window.Draw(lbl);
    }

    private void DrawDPadArm(
        RenderWindow window,
        float x, float y, float w, float h,
        bool pressed, string arrow)
    {
        using RectangleShape seg = new(new Vector2f(w, h))
        {
            Position = new Vector2f(x, y),
            FillColor = pressed ? _colActive : _colDim,
            OutlineColor = pressed ? _colActive : _colOutline,
            OutlineThickness = 1f
        };
        window.Draw(seg);

        using Text lbl = new(_font, arrow, 16)
        {
            Position = new Vector2f(x + w / 2f - 7f, y + h / 2f - 10f),
            FillColor = pressed ? Color.White : new Color(120, 125, 145)
        };
        window.Draw(lbl);
    }

    private static void DrawDPadCenter(RenderWindow window, float x, float y, float sz)
    {
        using RectangleShape center = new(new Vector2f(sz, sz))
        {
            Position = new Vector2f(x, y),
            FillColor = new Color(40, 42, 55),
            OutlineColor = new Color(70, 73, 90),
            OutlineThickness = 1f
        };
        window.Draw(center);
    }

    private static void DrawFaceButtons(GameHost host, RenderWindow window, float cx, float cy)
    {
        const float R = 22f;
        const float Gap = 52f;

        DrawTriangleFaceButton(host, window, BtnTriangle, cx, cy - Gap, R, _colTriangle);
        DrawCircleFaceButton(host, window, BtnCircle, cx + Gap, cy, R, _colCircle);
        DrawCrossFaceButton(host, window, BtnCross, cx, cy + Gap, R, _colCross);
        DrawSquareFaceButton(host, window, BtnSquare, cx - Gap, cy, R, _colSquare);
    }

    private static bool DrawFaceButtonBase(GameHost host, RenderWindow window, uint button, float cx, float cy, float r, Color pressedColor)
    {
        bool pressed = host.Input.IsJoystickButtonDown(0, button);

        using CircleShape background = new(r)
        {
            Origin = new Vector2f(r, r),
            Position = new Vector2f(cx, cy),
            FillColor = pressed ? pressedColor : _colDim,
            OutlineColor = pressed ? pressedColor : _colOutline,
            OutlineThickness = 2f
        };
        window.Draw(background);

        return pressed;
    }

    private static void DrawTriangleFaceButton(GameHost host, RenderWindow window, uint button, float cx, float cy, float r, Color pressedColor)
    {
        bool pressed = DrawFaceButtonBase(host, window, button, cx, cy, r, pressedColor);

        // Equilateral triangle inscribed at radius 12.
        const float Ir = 12f;

        using ConvexShape tri = new(3)
        {
            FillColor = pressed ? Color.White : new Color(130, 135, 155)
        };
        tri.SetPoint(0, new Vector2f(cx, cy - Ir));
        tri.SetPoint(1, new Vector2f(cx + Ir * 0.87f, cy + Ir * 0.5f));
        tri.SetPoint(2, new Vector2f(cx - Ir * 0.87f, cy + Ir * 0.5f));
        window.Draw(tri);
    }

    private static void DrawCircleFaceButton(GameHost host, RenderWindow window, uint button, float cx, float cy, float r, Color pressedColor)
    {
        bool pressed = DrawFaceButtonBase(host, window, button, cx, cy, r, pressedColor);

        const float Ir = 9f;

        using CircleShape ring = new(Ir)
        {
            Origin = new Vector2f(Ir, Ir),
            Position = new Vector2f(cx, cy),
            FillColor = Color.Transparent,
            OutlineColor = pressed ? Color.White : new Color(130, 135, 155),
            OutlineThickness = 2.5f
        };
        window.Draw(ring);
    }

    private static void DrawCrossFaceButton(GameHost host, RenderWindow window, uint button, float cx, float cy, float r, Color pressedColor)
    {
        bool pressed = DrawFaceButtonBase(host, window, button, cx, cy, r, pressedColor);
        Color symColor = pressed ? Color.White : new Color(130, 135, 155);
        const float Len = 18f;
        const float Thi = 3f;

        using RectangleShape bar1 = new(new Vector2f(Len, Thi))
        {
            Origin = new Vector2f(Len / 2f, Thi / 2f),
            Position = new Vector2f(cx, cy),
            FillColor = symColor,
            Rotation = 45f
        };
        window.Draw(bar1);

        using RectangleShape bar2 = new(new Vector2f(Len, Thi))
        {
            Origin = new Vector2f(Len / 2f, Thi / 2f),
            Position = new Vector2f(cx, cy),
            FillColor = symColor,
            Rotation = -45f
        };
        window.Draw(bar2);
    }

    private static void DrawSquareFaceButton(GameHost host, RenderWindow window, uint button, float cx, float cy, float r, Color pressedColor)
    {
        bool pressed = DrawFaceButtonBase(host, window, button, cx, cy, r, pressedColor);

        const float Sq = 16f;

        using RectangleShape square = new(new Vector2f(Sq, Sq))
        {
            Origin = new Vector2f(Sq / 2f, Sq / 2f),
            Position = new Vector2f(cx, cy),
            FillColor = Color.Transparent,
            OutlineColor = pressed ? Color.White : new Color(130, 135, 155),
            OutlineThickness = 2.5f
        };
        window.Draw(square);
    }

    private void DrawCenterButtons(GameHost host, RenderWindow window)
    {
        DrawCenterButton(host, window, BtnSelect, "SEL", 545f, 220f);
        DrawCenterButton(host, window, BtnStart, "STA", 720f, 220f);
        DrawPsButton(window, host.Input.IsJoystickButtonDown(0, BtnPS), 640f, 232f);
    }

    private void DrawCenterButton(
        GameHost host, RenderWindow window,
        uint button, string label,
        float cx, float cy)
    {
        bool pressed = host.Input.IsJoystickButtonDown(0, button);

        const float W = 56f;
        const float H = 22f;

        using RectangleShape rect = new(new Vector2f(W, H))
        {
            Position = new Vector2f(cx - W / 2f, cy),
            FillColor = pressed ? new Color(255, 200, 60) : _colDim,
            OutlineColor = pressed ? new Color(255, 220, 100) : _colOutline,
            OutlineThickness = 1f
        };
        window.Draw(rect);

        using Text lbl = new(_font, label, 13)
        {
            Position = new Vector2f(cx - label.Length * 3.5f, cy + 4f),
            FillColor = pressed ? new Color(40, 30, 0) : new Color(140, 148, 168)
        };
        window.Draw(lbl);
    }

    private void DrawPsButton(RenderWindow window, bool pressed, float cx, float cy)
    {
        const float R = 17f;

        using CircleShape circle = new(R)
        {
            Origin = new Vector2f(R, R),
            Position = new Vector2f(cx, cy),
            FillColor = pressed ? new Color(60, 80, 180) : new Color(40, 43, 58),
            OutlineColor = pressed ? new Color(100, 140, 255) : new Color(88, 92, 112),
            OutlineThickness = 2f
        };
        window.Draw(circle);

        using Text lbl = new(_font, "PS", 13)
        {
            Position = new Vector2f(cx - 9f, cy - 9f),
            FillColor = pressed ? Color.White : new Color(100, 105, 125)
        };
        window.Draw(lbl);
    }

    private void DrawAnalogStick(
        GameHost host, RenderWindow window,
        float cx, float cy,
        Joystick.Axis axisX, Joystick.Axis axisY,
        uint clickButton, string label)
    {
        float rawX = host.Input.GetJoystickAxis(0, axisX);
        float rawY = host.Input.GetJoystickAxis(0, axisY);
        bool clicked = host.Input.IsJoystickButtonDown(0, clickButton);

        const float OuterR = 62f;
        const float DotR = 13f;
        const float Travel = OuterR - DotR - 4f;

        using CircleShape outer = new(OuterR)
        {
            Origin = new Vector2f(OuterR, OuterR),
            Position = new Vector2f(cx, cy),
            FillColor = clicked ? new Color(55, 60, 80) : new Color(35, 38, 52),
            OutlineColor = clicked ? new Color(255, 200, 60) : _colOutline,
            OutlineThickness = 2f
        };
        window.Draw(outer);

        using RectangleShape hLine = new(new Vector2f(OuterR * 2f, 1f))
        {
            Position = new Vector2f(cx - OuterR, cy),
            FillColor = new Color(58, 63, 82)
        };
        window.Draw(hLine);

        using RectangleShape vLine = new(new Vector2f(1f, OuterR * 2f))
        {
            Position = new Vector2f(cx, cy - OuterR),
            FillColor = new Color(58, 63, 82)
        };
        window.Draw(vLine);

        float dotX = cx + (rawX / 100f) * Travel;
        float dotY = cy + (rawY / 100f) * Travel;

        using CircleShape dot = new(DotR)
        {
            Origin = new Vector2f(DotR, DotR),
            Position = new Vector2f(dotX, dotY),
            FillColor = clicked ? new Color(255, 200, 60) : new Color(255, 160, 60)
        };
        window.Draw(dot);

        string clickMark = clicked ? " \u25cf" : "";

        using Text nameLbl = new(_font, label + clickMark, 16)
        {
            Position = new Vector2f(cx - 12f, cy + OuterR + 8f),
            FillColor = clicked ? new Color(255, 200, 60) : new Color(158, 165, 185)
        };
        window.Draw(nameLbl);

        using Text valLbl = new(_font,
            $"X:{rawX / 100f:+0.00;-0.00;+0.00}  Y:{rawY / 100f:+0.00;-0.00;+0.00}", 14)
        {
            Position = new Vector2f(cx - 46f, cy + OuterR + 28f),
            FillColor = new Color(120, 128, 148)
        };
        window.Draw(valLbl);
    }

    private void DrawTriggerBars(GameHost host, RenderWindow window)
    {
        float z = host.Input.GetJoystickAxis(0, Joystick.Axis.Z);
        // Centered bar: L2 fills right (+100), R2 fills left (-100).
        DrawTriggerBar(window, "L2", "R2", "Z", z, 700f, 532f);
    }

    private void DrawTriggerBar(
        RenderWindow window,
        string labelLeft, string labelRight, string axisName,
        float rawValue, float barX, float y)
    {
        const float BarW = 120f;
        const float BarH = 12f;
        float norm = Math.Clamp(rawValue / 100f, -1f, 1f);
        float half = BarW / 2f;
        float centerX = barX + half;
        bool active = MathF.Abs(norm) > 0.01f;

        using Text leftLbl = new(_font, labelLeft, 12)
        {
            Position = new Vector2f(barX - 24f, y),
            FillColor = norm > 0.01f ? _colActive : new Color(110, 115, 135)
        };
        window.Draw(leftLbl);

        using Text rightLbl = new(_font, labelRight, 12)
        {
            Position = new Vector2f(barX + BarW + 6f, y),
            FillColor = norm < -0.01f ? _colActive : new Color(110, 115, 135)
        };
        window.Draw(rightLbl);

        using Text axisLbl = new(_font, axisName, 11)
        {
            Position = new Vector2f(centerX - 4f, y - 14f),
            FillColor = new Color(88, 93, 113)
        };
        window.Draw(axisLbl);

        using RectangleShape background = new(new Vector2f(BarW, BarH))
        {
            Position = new Vector2f(barX, y + 1f),
            FillColor = _colDim,
            OutlineColor = _colOutline,
            OutlineThickness = 1f
        };
        window.Draw(background);

        using RectangleShape centerTick = new(new Vector2f(1f, BarH))
        {
            Position = new Vector2f(centerX, y + 1f),
            FillColor = _colOutline
        };
        window.Draw(centerTick);

        if (active)
        {
            float fillW = MathF.Abs(norm) * half;
            float fillX = norm >= 0f ? centerX : centerX - fillW;

            using RectangleShape filled = new(new Vector2f(fillW, BarH))
            {
                Position = new Vector2f(fillX, y + 1f),
                FillColor = _colActive
            };
            window.Draw(filled);
        }

        using Text valText = new(_font, $"{rawValue:F1}", 12)
        {
            Position = new Vector2f(centerX - 12f, y + BarH + 3f),
            FillColor = active ? _colActive : new Color(110, 115, 135)
        };
        window.Draw(valText);
    }

    private void DrawFooter(GameHost host, RenderWindow window, bool connected)
    {
        using Text legend = new(_font,
            "Btn mapping (confirmed):  " +
            "0:\u00d7  1:\u25cb  2:\u25a1  3:\u25b3  4:L1  5:R1  6:SEL  7:STA  8:L3  9:R3  10:PS  " +
            "\u2502  D-Pad: PovX (L=-100/R=+100)  PovY (U=+100/D=-100)  " +
            "\u2502  L2: Z=+100  R2: Z=-100",
            13)
        {
            Position = new Vector2f(20f, 567f),
            FillColor = new Color(88, 93, 113)
        };
        window.Draw(legend);

        if (!connected)
        {
            return;
        }

        string axes = string.Format(
            "Axes:  X:{0,7:F2}  Y:{1,7:F2}  Z:{2,7:F2}  R:{3,7:F2}  U:{4,7:F2}  V:{5,7:F2}  PovX:{6,7:F2}  PovY:{7,7:F2}",
            host.Input.GetJoystickAxis(0, Joystick.Axis.X),
            host.Input.GetJoystickAxis(0, Joystick.Axis.Y),
            host.Input.GetJoystickAxis(0, Joystick.Axis.Z),
            host.Input.GetJoystickAxis(0, Joystick.Axis.R),
            host.Input.GetJoystickAxis(0, Joystick.Axis.U),
            host.Input.GetJoystickAxis(0, Joystick.Axis.V),
            host.Input.GetJoystickAxis(0, Joystick.Axis.PovX),
            host.Input.GetJoystickAxis(0, Joystick.Axis.PovY));

        using Text axesText = new(_font, axes, 14)
        {
            Position = new Vector2f(20f, 585f),
            FillColor = new Color(128, 136, 158)
        };
        window.Draw(axesText);
    }
}
