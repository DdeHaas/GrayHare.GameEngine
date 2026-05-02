using GrayHare.GameEngine.Application;
using GrayHare.GameEngine.Input;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GrayHare.GameEngine.DemoHub.Scenes.InputActionDemo;

/// <summary>
/// Demonstrates <see cref="InputActionMap"/> by mapping named actions to keyboard keys
/// and joystick bindings. A colored square responds to the mapped actions.
/// </summary>
internal sealed class InputActionScene : DemoSceneBase
{
    private const float MoveSpeed = 200f;
    private const float SquareSize = 40f;

    private Font _font = null!;
    private InputActionMap _actions = null!;
    private Vector2f _squarePos;
    private Color _squareColor = new(80, 200, 255);
    private float _flashTimer;

    public InputActionScene(DemoCatalog catalog, int sceneIndex) : base(catalog, sceneIndex) { }

    public override void Load(GameHost host)
    {
        base.Load(host);
        _font = host.Assets.LoadFont();

        _squarePos = new Vector2f(host.Window.Size.X / 2f, host.Window.Size.Y / 2f);

        _actions = new InputActionMap();
        _actions.MapKey("MoveUp", Keyboard.Key.W);
        _actions.MapKey("MoveDown", Keyboard.Key.S);
        _actions.MapKey("MoveLeft", Keyboard.Key.A);
        _actions.MapKey("MoveRight", Keyboard.Key.D);
        _actions.MapKey("Fire", Keyboard.Key.Space);
        _actions.MapButton("Fire", 0, 0);
        _actions.MapAxis("HorizontalMove", 0, Joystick.Axis.X);
        _actions.MapAxis("VerticalMove", 0, Joystick.Axis.Y);

        host.InputActions = _actions;
    }

    public override void Unload(GameHost host)
    {
        host.InputActions = null;
        base.Unload(host);
    }

    public override void Update(GameHost host, in GameTime gameTime)
    {
        base.Update(host, in gameTime);

        if (_actions is null)
        {
            return;
        }

        float deltaTime = gameTime.DeltaTotalSeconds;

        // Keyboard-based movement via actions.
        Vector2f move = new(0f, 0f);
        if (_actions.IsActionDown("MoveUp", host.Input))
        {
            move += new Vector2f(0f, -1f);
        }

        if (_actions.IsActionDown("MoveDown", host.Input))
        {
            move += new Vector2f(0f, 1f);
        }

        if (_actions.IsActionDown("MoveLeft", host.Input))
        {
            move += new Vector2f(-1f, 0f);
        }

        if (_actions.IsActionDown("MoveRight", host.Input))
        {
            move += new Vector2f(1f, 0f);
        }

        // Add joystick axis movement.
        float axisH = _actions.GetAxisValue("HorizontalMove", host.Input) / 100f;
        float axisV = _actions.GetAxisValue("VerticalMove", host.Input) / 100f;
        move += new Vector2f(axisH, axisV);

        float len = MathF.Sqrt(move.X * move.X + move.Y * move.Y);
        if (len > 1f)
        {
            move /= len;
        }

        _squarePos += move * MoveSpeed * deltaTime;

        // Clamp to window.
        float half = SquareSize / 2f;
        _squarePos = new Vector2f(
            Math.Clamp(_squarePos.X, half, host.Window.Size.X - half),
            Math.Clamp(_squarePos.Y, half, host.Window.Size.Y - half));

        // Fire action — flash color.
        if (_actions.WasActionPressed("Fire", host.Input))
        {
            _flashTimer = 0.2f;
        }

        if (_flashTimer > 0f)
        {
            _flashTimer -= deltaTime;
            _squareColor = new Color(255, 255, 80);
        }
        else
        {
            _squareColor = new Color(80, 200, 255);
        }
    }

    public override void RenderLayer(GameHost host, RenderWindow window)
    {
        // Draw the movable square.
        float half = SquareSize / 2f;
        using RectangleShape square = new(new Vector2f(SquareSize, SquareSize))
        {
            Origin = new Vector2f(half, half),
            Position = _squarePos,
            FillColor = _squareColor
        };
        window.Draw(square);

        if (_actions is null)
        {
            return;
        }

        // HUD showing active bindings.
        string moveUpState = _actions.IsActionDown("MoveUp", host.Input) ? "DOWN" : "---";
        string moveDownState = _actions.IsActionDown("MoveDown", host.Input) ? "DOWN" : "---";
        string moveLeftState = _actions.IsActionDown("MoveLeft", host.Input) ? "DOWN" : "---";
        string moveRightState = _actions.IsActionDown("MoveRight", host.Input) ? "DOWN" : "---";
        string fireState = _actions.IsActionDown("Fire", host.Input) ? "DOWN" : "---";
        float hAxis = _actions.GetAxisValue("HorizontalMove", host.Input);
        float vAxis = _actions.GetAxisValue("VerticalMove", host.Input);

        string info =
            $"MoveUp [W]:    {moveUpState}\n" +
            $"MoveDown [S]:  {moveDownState}\n" +
            $"MoveLeft [A]:  {moveLeftState}\n" +
            $"MoveRight [D]: {moveRightState}\n" +
            $"Fire [Space]:  {fireState}\n" +
            $"H-Axis: {hAxis:F1}  V-Axis: {vAxis:F1}";

        using Text hud = new(_font, info, 18)
        {
            Position = new Vector2f(20f, 20f),
            FillColor = new Color(200, 220, 240)
        };
        window.Draw(hud);

        using Text hints = new(_font,
            "WASD=Move  Space=Fire  Gamepad: Stick=Move  Btn0=Fire", 16)
        {
            Position = new Vector2f(20f, host.Window.Size.Y - 36f),
            FillColor = new Color(160, 160, 160)
        };
        window.Draw(hints);
    }
}
