using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ShaderUnderstanding;

internal sealed record SphereImages(byte[] Original, byte[] Proposed, int Size);

internal static class SphereRendering {
    public static SphereImages Render(CompiledPaint paint, PreviewStage stage, int size, CancellationToken cancellation) {
        byte[] original = new byte[size * size * 4];
        byte[] proposed = new byte[original.Length];
        for (int row = 0; row < size; row++) {
            cancellation.ThrowIfCancellationRequested();
            double y = 1 - (row + 0.5) * 2 / size;
            for (int column = 0; column < size; column++) {
                double x = (column + 0.5) * 2 / size - 1;
                Direction? normal = PaintMath.SphereNormal(x, y);
                if (normal is null) continue;
                int offset = (row * size + column) * 4;
                Write(original, offset, paint.Evaluate(normal.Value, PaintModel.Original), stage);
                Write(proposed, offset, paint.Evaluate(normal.Value, PaintModel.LinearWarp), stage);
            }
        }
        return new(original, proposed, size);
    }

    public static BitmapSource Bitmap(byte[] pixels, int size) {
        BitmapSource image = BitmapSource.Create(size, size, 96, 96, PixelFormats.Bgra32, null, pixels, size * 4);
        image.Freeze();
        return image;
    }

    private static void Write(byte[] pixels, int offset, PaintSample sample, PreviewStage stage) {
        PaintColor color = stage switch {
            PreviewStage.Facing => new(sample.Facing, sample.Facing, sample.Facing),
            PreviewStage.Tone => new(sample.Tone, sample.Tone, sample.Tone),
            _ => sample.DisplayColor
        };
        // Diagnostic scalars map directly to display bytes;
        // only the painted linear RGB uses the sRGB encoding function.
        pixels[offset] = Appearance.Byte(color.Blue);
        pixels[offset + 1] = Appearance.Byte(color.Green);
        pixels[offset + 2] = Appearance.Byte(color.Red);
        pixels[offset + 3] = 255;
    }
}

internal sealed class SphereView : FrameworkElement {
    private Direction probe = new(0, 0, 1);
    private double rotationDegrees;
    private double lastTilt;
    public BitmapSource? Image { get; set; }
    public Direction Probe {
        get => this.probe;
        set {
            this.probe = value;
            this.lastTilt = ProbeCoordinates.FromNormal(value, this.RotationDegrees, this.lastTilt).Tilt;
        }
    }
    public Direction Peak { get; set; } = new(0, 0, 1);
    public ProbeMovement Movement { get; set; }
    public double RotationDegrees {
        get => this.rotationDegrees;
        set {
            this.rotationDegrees = value;
            this.lastTilt = ProbeCoordinates.FromNormal(this.Probe, value, this.lastTilt).Tilt;
        }
    }
    private ProbeCoordinates FrontCoordinates => ProbeCoordinates.FromNormal(new(this.Probe.X, this.Probe.Y, Math.Abs(this.Probe.Z)),
        this.RotationDegrees, Math.Asin(Math.Sin(this.lastTilt)));
    public bool ShowMarkers { get; set; } = true;
    public Brush Accent { get; init; } = Appearance.Original;
    public event Action<Direction>? ProbeChanged;

    public SphereView() {
        this.MinHeight = 120;
        this.Cursor = Cursors.Cross;
        this.Focusable = true;
        this.ToolTip = "Click or drag to move the shared probe. Probe movement can lock tilt or slice. Tilt only: Left/Right changes tilt at fixed slice. Slice only: Up/Down changes slice at fixed tilt. Home zeros the unlocked coordinate; in Free mode it selects the center. The dashed path shows permitted motion.";
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
        this.MouseLeftButtonDown += (_, arguments) => {
            this.Focus();
            this.CaptureMouse();
            this.Pick(arguments.GetPosition(this));
        };
        this.MouseMove += (_, arguments) => { if (this.IsMouseCaptured) this.Pick(arguments.GetPosition(this)); };
        this.MouseLeftButtonUp += (_, _) => this.ReleaseMouseCapture();
        this.KeyDown += (_, arguments) => {
            arguments.Handled = this.MoveProbe(arguments.Key);
        };
    }

    internal bool MoveProbe(Key key) {
        if (key is not (Key.Left or Key.Right or Key.Up or Key.Down or Key.Home)) return false;
        if (this.Movement == ProbeMovement.Free) {
            double x = this.Probe.X + (key == Key.Left ? -0.025 : key == Key.Right ? 0.025 : 0);
            double y = this.Probe.Y + (key == Key.Down ? -0.025 : key == Key.Up ? 0.025 : 0);
            Direction? normal = PaintMath.SphereNormal(key == Key.Home ? 0 : x, key == Key.Home ? 0 : y);
            if (normal is not null) this.ProbeChanged?.Invoke(normal.Value);
        } else {
            ProbeCoordinates coordinates = this.FrontCoordinates;
            if (this.Movement == ProbeMovement.TiltOnly) {
                double change = key == Key.Left ? -Math.PI / 90 : key == Key.Right ? Math.PI / 90 : 0;
                coordinates = coordinates with { Tilt = key == Key.Home ? 0 : Math.Clamp(coordinates.Tilt + change, -Math.PI / 2, Math.PI / 2) };
            } else {
                double change = key == Key.Down ? -0.025 : key == Key.Up ? 0.025 : 0;
                coordinates = coordinates with { Slice = key == Key.Home ? 0 : Math.Clamp(coordinates.Slice + change, -1, 1) };
            }
            this.ProbeChanged?.Invoke(coordinates.ToNormal(this.RotationDegrees));
        }
        return true;
    }

    public Rect SphereBounds {
        get {
            double diameter = Math.Max(1, Math.Min(this.ActualWidth - 80, this.ActualHeight - 44));
            return new((this.ActualWidth - diameter) / 2, (this.ActualHeight - diameter) / 2, diameter, diameter);
        }
    }

    public Direction? NormalAt(Point point) {
        Rect bounds = this.SphereBounds;
        Direction? normal = PaintMath.SphereNormal((point.X - bounds.Left) * 2 / bounds.Width - 1,
            1 - (point.Y - bounds.Top) * 2 / bounds.Height);
        return normal is Direction candidate ? ProbeCoordinates.Constrain(this.Probe, candidate, this.RotationDegrees, this.Movement, this.FrontCoordinates.Tilt) : null;
    }

    internal void Pick(Point point) {
        Direction? normal = this.NormalAt(point);
        if (normal is not null) this.ProbeChanged?.Invoke(normal.Value);
    }

    protected override void OnRender(DrawingContext drawing) {
        base.OnRender(drawing);
        drawing.DrawRectangle(Brushes.Transparent, null, new Rect(this.RenderSize));
        Rect bounds = this.SphereBounds;
        if (this.Image is not null) drawing.DrawImage(this.Image, bounds);
        if (!this.ShowMarkers) return;
        if (this.Movement != ProbeMovement.Free) this.DrawMovementPath(drawing, bounds);
        if (this.Probe.Z >= 0) {
            Point point = Project(this.Probe, bounds);
            drawing.DrawEllipse(null, new(Appearance.Background, 4), point, 8, 8);
            drawing.DrawEllipse(null, new(Brushes.White, 1.5), point, 8, 8);
            drawing.DrawLine(new(Brushes.White, 1.5), new(point.X - 15, point.Y), new(point.X - 10, point.Y));
            drawing.DrawLine(new(Brushes.White, 1.5), new(point.X + 10, point.Y), new(point.X + 15, point.Y));
        }
        Point peak = Project(this.Peak, bounds);
        drawing.DrawEllipse(this.Accent, new(Appearance.Background, 1.5), peak, 4.5, 4.5);
        PlotText.Draw(drawing, "+Y", new(bounds.Left + bounds.Width / 2, 0), Appearance.Quiet, this, alignment: HorizontalAlignment.Center);
        PlotText.Draw(drawing, "+X", new(bounds.Right + 8, bounds.Top + bounds.Height / 2 - 11), Appearance.Quiet, this);
    }

    private void DrawMovementPath(DrawingContext drawing, Rect bounds) {
        ProbeCoordinates anchor = this.FrontCoordinates;
        StreamGeometry path = new();
        using (StreamGeometryContext context = path.Open()) {
            const int segments = 128;
            for (int index = 0; index <= segments; index++) {
                double angle = (index / (double)segments - 0.5) * Math.PI;
                ProbeCoordinates coordinates = this.Movement == ProbeMovement.TiltOnly ? anchor with { Tilt = angle }
                    : anchor with { Slice = Math.Sin(angle) };
                Point point = Project(coordinates.ToNormal(this.RotationDegrees), bounds);
                if (index == 0) context.BeginFigure(point, false, false);
                else context.LineTo(point, true, false);
            }
        }
        path.Freeze();
        drawing.DrawGeometry(null, new(Appearance.Background, 3) { DashStyle = new(new double[] { 2, 4.0 / 3 }, 0) }, path);
        drawing.DrawGeometry(null, new(Brushes.White, 1) { DashStyle = new(new double[] { 6, 4 }, 0) }, path);
    }

    private static Point Project(Direction normal, Rect bounds) => new(bounds.Left + (normal.X + 1) * bounds.Width / 2, bounds.Top + (1 - normal.Y) * bounds.Height / 2);
}
