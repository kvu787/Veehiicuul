using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ShaderUnderstanding;

internal static class PlotText {
    public static void Draw(DrawingContext drawing, string text, Point position, Brush color, Visual visual, double size = 16, HorizontalAlignment alignment = HorizontalAlignment.Left) {
        FormattedText formatted = new(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI"), size, color, VisualTreeHelper.GetDpi(visual).PixelsPerDip);
        double offset = alignment == HorizontalAlignment.Center ? formatted.Width / 2 : alignment == HorizontalAlignment.Right ? formatted.Width : 0;
        drawing.DrawText(formatted, new(position.X - offset, position.Y));
    }
}

internal enum ColorResponseAxis { Facing, Tone }

internal sealed class CurveView : FrameworkElement {
    private static readonly Brush[] Channels = [Appearance.Brush("#F28B97"), Appearance.Brush("#A7DC86"), Appearance.Brush("#8AB4FF")];
    public CompiledPaint Paint { get; set; } = new(new());
    public Direction Probe { get; set; } = new(0, 0, 1);
    public bool ColorCurve { get; init; }
    public ColorResponseAxis ColorAxis { get; set; }
    public event Action<Direction>? ProbeChanged;
    private double? hoveredInput;

    public PaintColor SampleColor(double input) => this.ColorAxis == ColorResponseAxis.Facing ? this.Paint.ColorAtFacing(input) : this.Paint.Curve(input);
    public double SampleTone(double input) => this.ColorAxis == ColorResponseAxis.Facing ? this.Paint.ToneAtFacing(input) : input;
    public double ProbePosition(PaintModel model) {
        PaintSample sample = this.Paint.Evaluate(this.Probe, model);
        return this.ColorAxis == ColorResponseAxis.Facing ? sample.Facing : sample.Tone;
    }
    public double? BaseColorPosition {
        get {
            double tone = 1 - this.Paint.Parameters.Brightness;
            if (this.ColorAxis == ColorResponseAxis.Tone) return tone;
            double range = this.Paint.Parameters.LightPoint - this.Paint.Parameters.DarkPoint;
            if (range == 0) return null;
            double facing = (tone - this.Paint.Parameters.DarkPoint) / range;
            return facing >= 0 && facing <= 1 ? facing : null;
        }
    }

    public CurveView() {
        this.MinHeight = 145;
        this.Cursor = Cursors.Cross;
        this.MouseLeftButtonDown += (_, arguments) => {
            this.CaptureMouse();
            this.Interact(arguments.GetPosition(this), true);
        };
        this.MouseMove += (_, arguments) => this.Interact(arguments.GetPosition(this), this.IsMouseCaptured);
        this.MouseLeftButtonUp += (_, _) => this.ReleaseMouseCapture();
        this.MouseLeave += (_, _) => { this.hoveredInput = null; this.InvalidateVisual(); };
        this.ToolTip = "Click or drag the facing plot to select a normal. Hover over the color plot to read linear RGB. Its axis selector switches between the complete facing-to-color response and the underlying tone curve.";
    }

    private Rect Plot => new(48, 16, Math.Max(1, this.ActualWidth - 84), Math.Max(1, this.ActualHeight - 84));
    private Point Map(double fraction, double value) => new(this.Plot.Left + fraction * this.Plot.Width, this.Plot.Bottom - value * this.Plot.Height);

    private void Interact(Point point, bool selecting) {
        if (!this.Plot.Contains(point)) return;
        double fraction = Math.Clamp((point.X - this.Plot.Left) / this.Plot.Width, 0, 1);
        if (this.ColorCurve) { this.hoveredInput = fraction; this.InvalidateVisual(); }
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
        } else if (this.ColorAxis == ColorResponseAxis.Tone) {
            double first = Math.Min(this.Paint.Parameters.DarkPoint, this.Paint.Parameters.LightPoint);
            double last = Math.Max(this.Paint.Parameters.DarkPoint, this.Paint.Parameters.LightPoint);
            drawing.DrawRectangle(Appearance.Brush("#243245"), null, new(this.Map(first, 0).X, plot.Top, plot.Width * (last - first), plot.Height));
            foreach (double endpoint in new[] { first, last })
                drawing.DrawLine(new(Appearance.Quiet, 1), this.Map(endpoint, 0), this.Map(endpoint, 1));
        }
        foreach (double value in new[] { 0.0, 0.5, 1.0 }) {
            Point start = this.Map(0, value);
            drawing.DrawLine(new(Appearance.Line, 0.6), start, this.Map(1, value));
            PlotText.Draw(drawing, value.ToString("0.#", CultureInfo.InvariantCulture), new(plot.Left - 10, start.Y - 11), Appearance.Quiet, this, alignment: HorizontalAlignment.Right);
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
            PlotText.Draw(drawing, angle.ToString(CultureInfo.InvariantCulture), new(x, this.Plot.Bottom + 6), Appearance.Quiet, this, alignment: HorizontalAlignment.Center);
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
                drawing.DrawEllipse(brush, new(Appearance.Panel, 1), this.Map(fraction, this.Paint.Evaluate(this.Probe, model).Facing), 4, 4);
            }
        }
        PlotText.Draw(drawing, "Normal tilt (degrees) | shaded regions: back", new(this.Plot.Left, this.Plot.Bottom + 34), Appearance.Quiet, this);
    }

    private void DrawColorCurves(DrawingContext drawing) {
        foreach (double input in new[] { 0.0, 0.25, 0.5, 0.75, 1.0 }) {
            Point bottom = this.Map(input, 0);
            PlotText.Draw(drawing, input.ToString("0.##", CultureInfo.InvariantCulture), new(bottom.X, bottom.Y + 6), Appearance.Quiet, this, alignment: HorizontalAlignment.Center);
        }
        for (int channel = 0; channel < 3; channel++) {
            int selected = channel;
            this.DrawPath(drawing, input => Component(this.SampleColor(input), selected), Channels[channel]);
        }
        if (this.BaseColorPosition is double anchor) {
            drawing.DrawLine(new(Appearance.Quiet, 1) { DashStyle = DashStyles.Dash }, this.Map(anchor, 0), this.Map(anchor, 1));
            PaintColor anchorColor = this.SampleColor(anchor);
            for (int channel = 0; channel < 3; channel++) drawing.DrawEllipse(Channels[channel], null, this.Map(anchor, Component(anchorColor, channel)), 4, 4);
        }
        double[] endpoints = this.ColorAxis == ColorResponseAxis.Facing ? [0, 1] : [this.Paint.Parameters.DarkPoint, this.Paint.Parameters.LightPoint];
        foreach (double endpoint in endpoints) {
            PaintColor color = this.SampleColor(endpoint);
            for (int channel = 0; channel < 3; channel++) drawing.DrawEllipse(Channels[channel], new(Appearance.Panel, 1), this.Map(endpoint, Component(color, channel)), 4, 4);
        }
        drawing.DrawLine(new(Appearance.Original, 1) { DashStyle = DashStyles.Dot }, this.Map(this.ProbePosition(PaintModel.Original), 0), this.Map(this.ProbePosition(PaintModel.Original), 1));
        drawing.DrawLine(new(Appearance.Proposed, 1) { DashStyle = DashStyles.Dot }, this.Map(this.ProbePosition(PaintModel.LinearWarp), 0), this.Map(this.ProbePosition(PaintModel.LinearWarp), 1));
        if (this.hoveredInput is double hovered) {
            PaintColor sample = this.SampleColor(hovered);
            drawing.DrawLine(new(Appearance.Text, 0.7), this.Map(hovered, 0), this.Map(hovered, 1));
            string input = this.ColorAxis == ColorResponseAxis.Facing ? $"f {hovered:F3}, t {this.SampleTone(hovered):F3}" : $"t {hovered:F3}";
            PlotText.Draw(drawing, $"{input} | RGB {sample.Red:F3} {sample.Green:F3} {sample.Blue:F3}", new(this.Plot.Left, this.Plot.Bottom + 34), Appearance.Text, this);
        } else {
            string axis = this.ColorAxis == ColorResponseAxis.Facing ? "Facing f" : "Tone t";
            string anchorLabel = this.BaseColorPosition is double position ? $"base {(this.ColorAxis == ColorResponseAxis.Facing ? "f" : "t")} {position:F3}"
                : this.Paint.Parameters.DarkPoint == this.Paint.Parameters.LightPoint && this.Paint.Parameters.DarkPoint == 1 - this.Paint.Parameters.Brightness ? "base everywhere" : "base absent";
            PlotText.Draw(drawing, $"{axis} | tones {this.Paint.Parameters.DarkPoint:F3} -> {this.Paint.Parameters.LightPoint:F3} | {anchorLabel}", new(this.Plot.Left, this.Plot.Bottom + 34), Appearance.Quiet, this);
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
        drawing.DrawGeometry(null, new(color, 2.2), geometry);
    }

    private static double Component(PaintColor color, int channel) => channel == 0 ? color.Red : channel == 1 ? color.Green : color.Blue;
    private static Direction RotateForSweep(Direction normal, double rotation) {
        double angle = -rotation * Math.PI / 180;
        return new(normal.X * Math.Cos(angle) - normal.Y * Math.Sin(angle), normal.X * Math.Sin(angle) + normal.Y * Math.Cos(angle), normal.Z);
    }
}
