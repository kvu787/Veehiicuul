using Godot;

namespace InputLatencyGodot;

public partial class InputDisplay : Control
{
    public InputState State { get; set; } = null!;
    public string GamepadDescription { get; set; } = "";
    public string GamepadMapping { get; set; } = "";
    public Vector2 MousePosition { get; set; }
    public bool Focused { get; set; }
    public bool SpacePressed { get; set; }
    public bool LeftMousePressed { get; set; }
    private static readonly Color Ink = new("dce6f2");
    private static readonly Color Muted = new("8699b2");
    private static readonly Color Accent = new("50dec4");

    public override void _Draw()
    {
        Text(28, 40, "INPUT LATENCY / GODOT", 25, Ink);
        Text(28, 67, "4.7.2 .NET   /   3D   /   D3D12   /   VSync off   /   No application FPS cap", 15, Muted);
        Text(1030, 40, Focused ? "WINDOW FOCUSED" : "WINDOW UNFOCUSED", 16, Focused ? Accent : Muted);
        Text(28, 116, "RIGHT STICK", 20, Ink);
        Text(28, 145, GamepadDescription, 18, Accent, 465);
        Text(28, 169, GamepadMapping, 15, Muted, 465);

        var center = new Vector2(255, 370);
        DrawArc(center, 150, 0, Mathf.Tau, 96, new Color("35465b"), 1.5f, true);
        DrawLine(center - new Vector2(150, 0), center + new Vector2(150, 0), new Color("28364a"));
        DrawLine(center - new Vector2(0, 150), center + new Vector2(0, 150), new Color("28364a"));
        Text(247, 211, "-Y", 14, Muted);
        Text(242, 545, "+Y", 14, Muted);
        Text(77, 375, "-X", 14, Muted);
        Text(414, 375, "+X", 14, Muted);
        if (State.SelectedGamepad < 0) Text(157, 400, "Waiting for a gamepad", 17, Muted);
        Text(104, 579, $"X  {State.RightStick.X:+0.0000;-0.0000;0.0000}      Y  {State.RightStick.Y:+0.0000;-0.0000;0.0000}", 22, Ink);
        Text(83, 604, "No added deadzone or smoothing", 16, Muted);

        Indicator(28, 635, "SPACE", SpacePressed);
        Indicator(255, 635, "LEFT MOUSE", LeftMousePressed);
        Stream(92, "KEYBOARD", "Combined keyboards", State.Keyboard);
        Stream(282, "MOUSE", "Combined mice", State.Mouse);
        Stream(472, "GAMEPAD", "Selected controller + connections", State.Gamepad);
        Text(28, 703, "Click to focus. Latest 7 events per device type; timestamps are app receipt times in seconds.", 15, Muted);

        // This crosshair is rendered by the game and follows the sampled mouse position.
        if (Focused && GetViewportRect().HasPoint(MousePosition))
        {
            DrawArc(MousePosition, 9, 0, Mathf.Tau, 24, Accent, 2);
            DrawLine(MousePosition - new Vector2(13, 0), MousePosition + new Vector2(13, 0), Accent);
            DrawLine(MousePosition - new Vector2(0, 13), MousePosition + new Vector2(0, 13), Accent);
        }
    }

    private void Stream(float top, string title, string description, EventStream stream)
    {
        DrawRect(new Rect2(525, top, 727, 176), new Color("101c2d"));
        Text(541, top + 25, title, 18, Ink);
        Text(663, top + 25, $"{description}   /   {stream.Count} received", 14, Muted, 573);
        float y = top + 49;
        if (stream.Count == 0) Text(541, y, "Waiting for events...", 15, Muted);
        foreach (string line in stream.Lines)
        {
            Text(541, y, line, 15, Ink, 695);
            y += 18;
        }
    }

    private void Indicator(float x, float y, string title, bool pressed)
    {
        DrawRect(new Rect2(x, y, 212, 35), pressed ? Accent : new Color("172639"));
        Text(x + 12, y + 24, title + (pressed ? "  DOWN" : "  UP"), 16, pressed ? new Color("08151d") : Muted);
    }

    private void Text(float x, float y, string text, int size, Color color, float width = -1)
        => DrawString(ThemeDB.FallbackFont, new Vector2(x, y), text, HorizontalAlignment.Left, width, size, color);
}
