using Godot;

namespace InputLatencyGodot;

public partial class InputDisplay : Control {
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

    public override void _Draw() {
        this.Text(28, 40, "INPUT LATENCY / GODOT", 25, Ink);
        this.Text(28, 67, "4.7.2 .NET   /   3D   /   D3D12   /   VSync off   /   No application FPS cap", 15, Muted);
        this.Text(1530, 40, this.Focused ? "WINDOW FOCUSED" : "WINDOW UNFOCUSED", 16, this.Focused ? Accent : Muted);
        this.Stick(0, "LEFT STICK", this.State.LeftStick);
        this.Stick(500, "RIGHT STICK", this.State.RightStick);

        this.Indicator(28, 635, "SPACE", this.SpacePressed);
        this.Indicator(255, 635, "LEFT MOUSE", this.LeftMousePressed);
        this.Stream(92, "KEYBOARD", "Combined keyboards", this.State.Keyboard);
        this.Stream(282, "MOUSE", "Combined mice", this.State.Mouse);
        this.Stream(472, "GAMEPAD", "Selected controller + connections", this.State.Gamepad);
        this.Text(28, 703, "Click to focus. Latest 7 events per device type; timestamps are app receipt times in seconds.", 15, Muted);

        // This crosshair is rendered by the game and follows the sampled mouse position.
        if (this.Focused && this.GetViewportRect().HasPoint(this.MousePosition)) {
            this.DrawArc(this.MousePosition, 9, 0, Mathf.Tau, 24, Accent, 2);
            this.DrawLine(this.MousePosition - new Vector2(13, 0), this.MousePosition + new Vector2(13, 0), Accent);
            this.DrawLine(this.MousePosition - new Vector2(0, 13), this.MousePosition + new Vector2(0, 13), Accent);
        }
    }

    private void Stick(float left, string title, Vector2 position) {
        this.DrawSetTransform(new Vector2(left, 0));
        this.Text(28, 116, title, 20, Ink);
        this.Text(28, 145, this.GamepadDescription, 18, Accent, 465);
        this.Text(28, 169, this.GamepadMapping, 15, Muted, 465);

        Vector2 center = new(255, 370);
        this.DrawArc(center, 150, 0, Mathf.Tau, 96, new Color("35465b"), 1.5f, true);
        this.DrawLine(center - new Vector2(150, 0), center + new Vector2(150, 0), new Color("28364a"));
        this.DrawLine(center - new Vector2(0, 150), center + new Vector2(0, 150), new Color("28364a"));
        this.Text(247, 211, "-Y", 14, Muted);
        this.Text(242, 545, "+Y", 14, Muted);
        this.Text(77, 375, "-X", 14, Muted);
        this.Text(414, 375, "+X", 14, Muted);
        if (this.State.SelectedGamepad < 0) {
            this.Text(157, 400, "Waiting for a gamepad", 17, Muted);
        }

        this.Text(104, 579, $"X  {position.X:+0.0000;-0.0000;0.0000}      Y  {position.Y:+0.0000;-0.0000;0.0000}", 22, Ink);
        this.Text(83, 604, "No added deadzone or smoothing", 16, Muted);
        this.DrawSetTransform(Vector2.Zero);
    }

    private void Stream(float top, string title, string description, InputEventHistory stream) {
        this.DrawRect(new Rect2(1025, top, 727, 176), new Color("101c2d"));
        this.Text(1041, top + 25, title, 18, Ink);
        this.Text(1163, top + 25, $"{description}   /   {stream.Count} received", 14, Muted, 573);
        float y = top + 49;
        if (stream.Count == 0) {
            this.Text(1041, y, "Waiting for events...", 15, Muted);
        }

        foreach (string line in stream.Lines) {
            this.Text(1041, y, line, 15, Ink, 695);
            y += 18;
        }
    }

    private void Indicator(float x, float y, string title, bool pressed) {
        this.DrawRect(new Rect2(x, y, 212, 35), pressed ? Accent : new Color("172639"));
        this.Text(x + 12, y + 24, title + (pressed ? "  DOWN" : "  UP"), 16, pressed ? new Color("08151d") : Muted);
    }

    private void Text(float x, float y, string text, int size, Color color, float width = -1) {
        this.DrawString(ThemeDB.FallbackFont, new Vector2(x, y), text, HorizontalAlignment.Left, width, size, color);
    }
}
