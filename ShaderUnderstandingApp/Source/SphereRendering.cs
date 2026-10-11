using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ShaderUnderstanding;

internal sealed record SphereImages(byte[] Original, byte[] Proposed, int Size);

internal static class SphereRendering {
    public static SphereImages Render(CompiledPaint paint, PreviewStage stage, bool back, int size, CancellationToken cancellation) {
        byte[] original = new byte[size * size * 4];
        byte[] proposed = new byte[original.Length];
        for (int row = 0; row < size; row++) {
            cancellation.ThrowIfCancellationRequested();
            double y = 1 - (row + 0.5) * 2 / size;
            for (int column = 0; column < size; column++) {
                double x = (column + 0.5) * 2 / size - 1;
                Direction? normal = PaintMath.SphereNormal(x, y, back);
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
        PaintColor color;
        switch (stage) {
        case PreviewStage.Facing:
            color = new(sample.Facing, sample.Facing, sample.Facing);
            break;
        case PreviewStage.Tone:
            color = new(sample.Tone, sample.Tone, sample.Tone);
            break;
        case PreviewStage.Normal:
            Direction unit = sample.WarpedNormal.Unit;
            color = new(0.5 + unit.X * 0.5, 0.5 + unit.Y * 0.5, 0.5 + unit.Z * 0.5);
            break;
        default:
            color = sample.DisplayColor;
            break;
        }
        // Diagnostic scalars and normal components map directly to display bytes;
        // only the painted linear RGB uses the sRGB encoding function.
        pixels[offset] = Appearance.Byte(color.Blue);
        pixels[offset + 1] = Appearance.Byte(color.Green);
        pixels[offset + 2] = Appearance.Byte(color.Red);
        pixels[offset + 3] = 255;
    }
}

internal sealed class SphereView : FrameworkElement {
    public BitmapSource? Image { get; set; }
    public Direction Probe { get; set; } = new(0, 0, 1);
    public Direction Peak { get; set; } = new(0, 0, 1);
    public bool Back { get; set; }
    public bool ShowMarkers { get; set; } = true;
    public bool HasBackPeak { get; init; } = true;
    public Brush Accent { get; init; } = Appearance.Original;
    public event Action<Direction>? ProbeChanged;

    public SphereView() {
        this.MinHeight = 120;
        this.Cursor = Cursors.Cross;
        this.Focusable = true;
        this.ToolTip = "Click or drag on the sphere to inspect the same view-space normal in both models. Arrow keys move the probe; Home selects the center.";
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
        this.MouseLeftButtonDown += (_, arguments) => {
            this.Focus();
            this.CaptureMouse();
            this.Pick(arguments.GetPosition(this));
        };
        this.MouseMove += (_, arguments) => { if (this.IsMouseCaptured) this.Pick(arguments.GetPosition(this)); };
        this.MouseLeftButtonUp += (_, _) => this.ReleaseMouseCapture();
        this.KeyDown += (_, arguments) => {
            double x = this.Probe.X;
            double y = this.Probe.Y;
            switch (arguments.Key) {
            case Key.Left: x -= 0.025; break;
            case Key.Right: x += 0.025; break;
            case Key.Up: y += 0.025; break;
            case Key.Down: y -= 0.025; break;
            case Key.Home: x = 0; y = 0; break;
            default: return;
            }
            Direction? normal = PaintMath.SphereNormal(x, y, this.Back);
            if (normal is not null) this.ProbeChanged?.Invoke(normal.Value);
            arguments.Handled = true;
        };
    }

    public Rect SphereBounds {
        get {
            double diameter = Math.Max(1, Math.Min(this.ActualWidth - 56, this.ActualHeight - 28));
            return new((this.ActualWidth - diameter) / 2, (this.ActualHeight - diameter) / 2, diameter, diameter);
        }
    }

    public Direction? NormalAt(Point point) {
        Rect bounds = this.SphereBounds;
        return PaintMath.SphereNormal((point.X - bounds.Left) * 2 / bounds.Width - 1,
            1 - (point.Y - bounds.Top) * 2 / bounds.Height, this.Back);
    }

    private void Pick(Point point) {
        Direction? normal = this.NormalAt(point);
        if (normal is not null) this.ProbeChanged?.Invoke(normal.Value);
    }

    protected override void OnRender(DrawingContext drawing) {
        base.OnRender(drawing);
        drawing.DrawRectangle(Brushes.Transparent, null, new Rect(this.RenderSize));
        Rect bounds = this.SphereBounds;
        if (this.Image is not null) drawing.DrawImage(this.Image, bounds);
        if (!this.ShowMarkers) return;
        bool probeVisible = (this.Probe.Z < 0) == this.Back || this.Probe.Z == 0;
        if (probeVisible) {
            Point point = Project(this.Probe, bounds);
            drawing.DrawEllipse(null, new(Appearance.Background, 4), point, 6, 6);
            drawing.DrawEllipse(null, new(Brushes.White, 1.5), point, 6, 6);
            drawing.DrawLine(new(Brushes.White, 1), new(point.X - 11, point.Y), new(point.X - 7, point.Y));
            drawing.DrawLine(new(Brushes.White, 1), new(point.X + 7, point.Y), new(point.X + 11, point.Y));
        }
        if (!this.Back || this.HasBackPeak) {
            Point peak = Project(this.Back ? this.Peak * -1 : this.Peak, bounds);
            drawing.DrawEllipse(this.Accent, new(Appearance.Background, 1.5), peak, 3.5, 3.5);
        }
        PlotText.Draw(drawing, "+Y", new(bounds.Left + bounds.Width / 2 - 10, 0), Appearance.Quiet, this);
        PlotText.Draw(drawing, "+X", new(bounds.Right + 6, bounds.Top + bounds.Height / 2 - 8), Appearance.Quiet, this);
    }

    private static Point Project(Direction normal, Rect bounds) => new(bounds.Left + (normal.X + 1) * bounds.Width / 2, bounds.Top + (1 - normal.Y) * bounds.Height / 2);
}
