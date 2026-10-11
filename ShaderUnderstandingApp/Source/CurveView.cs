using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ShaderUnderstanding;

internal static class PlotText {
    public static void Draw(DrawingContext drawing, string text, Point position, Brush color, Visual visual, double size = 11) {
        FormattedText formatted = new(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI"), size, color, VisualTreeHelper.GetDpi(visual).PixelsPerDip);
        drawing.DrawText(formatted, position);
    }
}

internal sealed class CurveView : FrameworkElement {
    private static readonly Brush[] Channels = [Appearance.Brush("#F28B97"), Appearance.Brush("#A7DC86"), Appearance.Brush("#8AB4FF")];
    public CompiledPaint Paint { get; set; } = new(new());
    public Direction Probe { get; set; } = new(0, 0, 1);
    public bool ColorCurve { get; init; }
    public event Action<Direction>? ProbeChanged;
    private double? hoveredTone;

    public CurveView() {
        this.MinHeight = 105;
        this.Cursor = Cursors.Cross;
        this.MouseLeftButtonDown += (_, arguments) => {
            this.CaptureMouse();
            this.Interact(arguments.GetPosition(this), true);
        };
        this.MouseMove += (_, arguments) => this.Interact(arguments.GetPosition(this), this.IsMouseCaptured);
        this.MouseLeftButtonUp += (_, _) => this.ReleaseMouseCapture();
        this.MouseLeave += (_, _) => { this.hoveredTone = null; this.InvalidateVisual(); };
        this.ToolTip = "Click or drag the facing plot to select a normal. Hover over the color curve to read its linear RGB output.";
    }

    private Rect Plot => new(34, 12, Math.Max(1, this.ActualWidth - 46), Math.Max(1, this.ActualHeight - 48));
    private Point Map(double fraction, double value) => new(this.Plot.Left + fraction * this.Plot.Width, this.Plot.Bottom - value * this.Plot.Height);

    private void Interact(Point point, bool selecting) {
        if (!this.Plot.Contains(point)) return;
        double fraction = Math.Clamp((point.X - this.Plot.Left) / this.Plot.Width, 0, 1);
        if (this.ColorCurve) { this.hoveredTone = fraction; this.InvalidateVisual(); }
        else if (selecting) this.ProbeChanged?.Invoke(PaintMath.Sweep(fraction * 360 - 180, this.Paint.Parameters.RotationDegrees));
    }

    protected override void OnRender(DrawingContext drawing) {
        base.OnRender(drawing);
        drawing.DrawRectangle(Brushes.Transparent, null, new(this.RenderSize));
        Rect plot = this.Plot;
        if (!this.ColorCurve) {
            Brush backFill = Appearance.Brush("#232A38");
            drawing.DrawRectangle(backFill, null, new(plot.Left, plot.Top, plot.Width * 0.25, plot.Height));
            drawing.DrawRectangle(backFill, null, new(plot.Left + plot.Width * 0.75, plot.Top, plot.Width * 0.25, plot.Height));
        }
        foreach (double value in new[] { 0.0, 0.5, 1.0 }) {
            Point start = this.Map(0, value);
            drawing.DrawLine(new(Appearance.Line, 0.6), start, this.Map(1, value));
            PlotText.Draw(drawing, value.ToString("0.#", CultureInfo.InvariantCulture), new(5, start.Y - 8), Appearance.Quiet, this);
        }
        if (this.ColorCurve) this.DrawColorCurves(drawing);
        else this.DrawFacingCurves(drawing);
        drawing.DrawRectangle(null, new(Appearance.Line, 0.8), plot);
    }

    private void DrawFacingCurves(DrawingContext drawing) {
        foreach (int angle in new[] { -180, -90, 0, 90, 180 }) {
            double fraction = (angle + 180.0) / 360;
            double x = this.Map(fraction, 0).X;
            drawing.DrawLine(new(Appearance.Line, 0.6), new(x, this.Plot.Top), new(x, this.Plot.Bottom));
            PlotText.Draw(drawing, angle.ToString(CultureInfo.InvariantCulture), new(x - (angle == 180 ? 20 : 10), this.Plot.Bottom + 5), Appearance.Quiet, this);
        }
        this.DrawPath(drawing, value => this.Paint.Evaluate(PaintMath.Sweep(value * 360 - 180, this.Paint.Parameters.RotationDegrees), PaintModel.Original).Facing, Appearance.Original);
        this.DrawPath(drawing, value => this.Paint.Evaluate(PaintMath.Sweep(value * 360 - 180, this.Paint.Parameters.RotationDegrees), PaintModel.LinearWarp).Facing, Appearance.Proposed);
        Direction rotated = this.Paint.Parameters.Shift == 0
            ? RotateForSweep(this.Probe, this.Paint.Parameters.RotationDegrees)
            : this.Paint.Rotate(this.Probe);
        if (Math.Abs(rotated.Y) < 0.00001) {
            double tilt = Math.Atan2(rotated.X, rotated.Z) * 180 / Math.PI;
            double fraction = (tilt + 180) / 360;
            drawing.DrawLine(new(Appearance.Quiet, 1) { DashStyle = DashStyles.Dot }, this.Map(fraction, 0), this.Map(fraction, 1));
            foreach (PaintModel model in Enum.GetValues<PaintModel>()) {
                Brush brush = model == PaintModel.Original ? Appearance.Original : Appearance.Proposed;
                drawing.DrawEllipse(brush, new(Appearance.Panel, 1), this.Map(fraction, this.Paint.Evaluate(this.Probe, model).Facing), 3, 3);
            }
        }
        PlotText.Draw(drawing, "Normal tilt (degrees)   |   shaded regions: back", new(this.Plot.Left, this.Plot.Bottom + 22), Appearance.Quiet, this, 10.5);
    }

    private void DrawColorCurves(DrawingContext drawing) {
        foreach (double tone in new[] { 0.0, 0.25, 0.5, 0.75, 1.0 }) {
            Point bottom = this.Map(tone, 0);
            PlotText.Draw(drawing, tone.ToString("0.##", CultureInfo.InvariantCulture), new(bottom.X - (tone == 1 ? 7 : 8), bottom.Y + 5), Appearance.Quiet, this);
        }
        for (int channel = 0; channel < 3; channel++) {
            int selected = channel;
            this.DrawPath(drawing, tone => Component(this.Paint.Curve(tone), selected), Channels[channel]);
        }
        double anchor = 1 - this.Paint.Parameters.Brightness;
        drawing.DrawLine(new(Appearance.Quiet, 1) { DashStyle = DashStyles.Dash }, this.Map(anchor, 0), this.Map(anchor, 1));
        PaintColor anchorColor = this.Paint.Curve(anchor);
        for (int channel = 0; channel < 3; channel++) drawing.DrawEllipse(Channels[channel], null, this.Map(anchor, Component(anchorColor, channel)), 3, 3);
        double originalTone = this.Paint.Evaluate(this.Probe, PaintModel.Original).Tone;
        double proposedTone = this.Paint.Evaluate(this.Probe, PaintModel.LinearWarp).Tone;
        drawing.DrawLine(new(Appearance.Original, 1) { DashStyle = DashStyles.Dot }, this.Map(originalTone, 0), this.Map(originalTone, 1));
        drawing.DrawLine(new(Appearance.Proposed, 1) { DashStyle = DashStyles.Dot }, this.Map(proposedTone, 0), this.Map(proposedTone, 1));
        if (this.hoveredTone is double hovered) {
            PaintColor sample = this.Paint.Curve(hovered);
            drawing.DrawLine(new(Appearance.Text, 0.7), this.Map(hovered, 0), this.Map(hovered, 1));
            PlotText.Draw(drawing, $"t {hovered:F3}   RGB {sample.Red:F3}  {sample.Green:F3}  {sample.Blue:F3}", new(this.Plot.Left, this.Plot.Bottom + 22), Appearance.Text, this, 10.5);
        } else {
            PlotText.Draw(drawing, $"t: original {originalTone:F3} / warp {proposedTone:F3}   |   anchor {anchor:F3}", new(this.Plot.Left, this.Plot.Bottom + 22), Appearance.Quiet, this, 10.5);
        }
    }

    private void DrawPath(DrawingContext drawing, Func<double, double> function, Brush color) {
        StreamGeometry geometry = new();
        using (StreamGeometryContext context = geometry.Open()) {
            const int segments = 720;
            context.BeginFigure(this.Map(0, Math.Clamp(function(0), 0, 1)), false, false);
            for (int index = 1; index <= segments; index++) {
                double fraction = index / (double)segments;
                context.LineTo(this.Map(fraction, Math.Clamp(function(fraction), 0, 1)), true, false);
            }
        }
        geometry.Freeze();
        drawing.DrawGeometry(null, new(color, 1.8), geometry);
    }

    private static double Component(PaintColor color, int channel) => channel == 0 ? color.Red : channel == 1 ? color.Green : color.Blue;
    private static Direction RotateForSweep(Direction normal, double rotation) {
        double angle = -rotation * Math.PI / 180;
        return new(normal.X * Math.Cos(angle) - normal.Y * Math.Sin(angle), normal.X * Math.Sin(angle) + normal.Y * Math.Cos(angle), normal.Z);
    }
}
