using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ShaderUnderstanding;

// Runs the real controls, event handlers, asynchronous previews, and WPF layout
// without creating a visible window or using any physical display.
internal static class InterfaceVerification {
    public static async Task Run(MainWindow window, SessionLog log) {
        int checks = 0;
        void Require(bool condition, string name) {
            if (!condition) throw new InvalidOperationException("Interface verification failed: " + name);
            checks++;
        }

        async Task Settle() {
            await window.WaitForPreview();
            Require(window.StatusText.StartsWith("CPU mathematical preview", StringComparison.Ordinal), "completed preview clears updating status");
            // Match a maximized 2560x1440 desktop with title bar and taskbar.
            window.ArrangeOffscreen(2560, 1392);
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        }

        void Capture(string name) {
            string path = Path.Combine(log.DirectoryPath, name + ".png");
            window.SaveSnapshot(path);
            using FileStream stream = File.OpenRead(path);
            PngBitmapDecoder decoder = new(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            Require(decoder.Frames[0].PixelWidth == (int)window.Root.ActualWidth, name + " PNG width");
            Require(decoder.Frames[0].PixelHeight == (int)window.Root.ActualHeight, name + " PNG height");
            Require(stream.Length > 10000, name + " PNG content");
        }

        await Settle();
        Require(!window.IsVisible, "window remains invisible");
        Require(window.CompletedRenders > 0, "initial preview completes");
        Require(window.OriginalView.Image?.PixelWidth == 448 && window.ProposedView.Image?.PixelWidth == 448, "both preview images exist");
        Capture("DefaultComparison");

        Task rangeRender = window.CurrentRender;
        PaintParameters rangeMaterial = window.Parameters;
        Direction rangeProbe = window.Probe;
        window.BackRangeSelector.Value = 0;
        window.ArrangeOffscreen(2560, 1392);
        Require(window.FacingPlot.FacingHalfRange == 90 && window.FacingPlot.BackRegionFraction == 0 &&
            window.FacingPlot.FacingAxisTicks.SequenceEqual(new double[] { -90, -45, 0, 45, 90 }),
            "zero Back range removes shading and gives the front-only angle axis");
        Point FacingPoint(double fraction) => new(48 + fraction * (window.FacingPlot.ActualWidth - 84),
            16 + (window.FacingPlot.ActualHeight - 84) / 3);
        Direction? rangeLeft = window.FacingPlot.NormalAt(FacingPoint(0));
        Direction? rangeRight = window.FacingPlot.NormalAt(FacingPoint(1));
        Require(rangeLeft is Direction firstNormal && rangeRight is Direction lastNormal &&
            Math.Abs(firstNormal.Z) < 1e-12 && Math.Abs(lastNormal.Z) < 1e-12 &&
            firstNormal.X < -0.7 && lastNormal.X > 0.7, "front-only plot endpoints pick the two silhouette normals");
        Require(window.FacingPlot.NormalAt(new(0, 0)) is null, "facing graph ignores clicks outside the plot");
        Capture("FrontOnlyFacingRange");
        window.BackRangeSelector.Value = 45;
        window.ArrangeOffscreen(2560, 1392);
        Require(window.FacingPlot.FacingAngleAt(0) == -135 && window.FacingPlot.FacingAngleAt(1) == 135 &&
            Math.Abs(window.FacingPlot.BackRegionFraction - 1.0 / 6) < 1e-14, "intermediate Back range scales the angle domain and both shaded regions");
        Require(window.FacingPlot.NormalAt(FacingPoint(0)) is Direction extendedNormal &&
            Math.Abs(extendedNormal.Z + Math.Sqrt(0.5)) < 1e-12, "extended graph clicks use the new angle range");
        Capture("PartialBackFacingRange");
        window.BackRangeSelector.Value = 1;
        window.ArrangeOffscreen(2560, 1392);
        Require(window.FacingPlot.FacingAxisTicks.SequenceEqual(new double[] { -91, -45, 0, 45, 91 }),
            "range endpoints do not overlap nearby tick labels");
        Track rangeTrack = (Track)window.BackRangeSelector.Template.FindName("PART_Track", window.BackRangeSelector);
        rangeTrack.Thumb.RaiseEvent(new DragDeltaEventArgs(20, 0));
        Require(window.BackRangeSelector.Value > 1 && window.FacingPlot.BackRangeDegrees == window.BackRangeSelector.Value,
            "Back range thumb routes dragging into the graph domain");
        Require(window.Parameters == rangeMaterial && window.Probe == rangeProbe && window.CurrentRender == rangeRender,
            "Back range leaves material, probe, and sphere images unchanged");
        window.BackRangeSelector.Value = 90;
        window.ArrangeOffscreen(2560, 1392);
        Require(window.FacingPlot.FacingHalfRange == 180 && window.FacingPlot.BackRegionFraction == 0.25 &&
            window.FacingPlot.FacingAxisTicks.SequenceEqual(new double[] { -180, -90, 0, 90, 180 }), "maximum Back range restores the complete angle axis");

        // Moving on the sphere used to remove the facing indicators as soon as
        // the selected normal left the fixed Rotation plane. Verify a slice
        // through each arbitrary probe, not a projected point on an unrelated
        // curve, and confirm that the colored markers actually render.
        void RequireMarkerPixels(PaintModel model) {
            CurveView view = window.FacingPlot;
            RenderTargetBitmap image = new((int)Math.Ceiling(window.Root.ActualWidth), (int)Math.Ceiling(window.Root.ActualHeight), 96, 96, PixelFormats.Pbgra32);
            image.Render(window.Root);
            Point marker = view.TransformToAncestor(window.Root).Transform(view.FacingMarkerPoint(model));
            Color expected = (model == PaintModel.Original ? Appearance.Original : Appearance.Proposed).Color;
            byte[] pixels = Pixels(image);
            bool found = false;
            for (int y = Math.Max(0, (int)marker.Y - 7); y <= Math.Min(image.PixelHeight - 1, (int)marker.Y + 7); y++) {
                for (int x = Math.Max(0, (int)marker.X - 7); x <= Math.Min(image.PixelWidth - 1, (int)marker.X + 7); x++) {
                    int offset = (y * image.PixelWidth + x) * 4;
                    if (Math.Abs(pixels[offset] - expected.B) < 3 && Math.Abs(pixels[offset + 1] - expected.G) < 3 &&
                        Math.Abs(pixels[offset + 2] - expected.R) < 3 && pixels[offset + 3] == 255) found = true;
                }
            }
            Require(found, "facing marker pixels render for " + model);
        }
        foreach (double rotation in new[] { 0.0, 45, 123 }) {
            window.SetParameters(rangeMaterial with { RotationDegrees = rotation, Shift = rotation == 123 ? 0 : 0.6 });
            await Settle();
            foreach (Direction normal in new Direction[] { new(-0.0510, -0.0332, 0.9981), new(0.3, -0.7, 0.6), new(0.2, 0.4, -0.8) }) {
                window.SetProbe(normal);
                await Settle();
                Direction sampled = window.FacingPlot.FacingNormalAt(window.FacingPlot.FacingProbeFraction);
                Require((sampled + window.Probe * -1).Length < 1e-12, "facing slice passes through an arbitrary sphere probe at rotation " + rotation);
                foreach (PaintModel model in Enum.GetValues<PaintModel>()) {
                    CompiledPaint probePaint = new(window.Parameters);
                    Require(Math.Abs(probePaint.Evaluate(sampled, model).Facing - probePaint.Evaluate(window.Probe, model).Facing) < 1e-12,
                        "facing curve agrees with the inspected value for " + model);
                    RequireMarkerPixels(model);
                }
            }
        }
        window.SetParameters(rangeMaterial);
        window.SetProbe(new(0.3, -0.7, 0.6));
        await Settle();
        Direction centerOfSlice = window.FacingPlot.NormalAt(FacingPoint(0.5))!.Value;
        double azimuth = window.Parameters.RotationDegrees * Math.PI / 180;
        double crossOffset = -window.Probe.X * Math.Sin(azimuth) + window.Probe.Y * Math.Cos(azimuth);
        Require(Math.Abs(-centerOfSlice.X * Math.Sin(azimuth) + centerOfSlice.Y * Math.Cos(azimuth) - crossOffset) < 1e-12 &&
            centerOfSlice.Z > 0, "facing graph clicks preserve the probe's offset from the Rotation plane");
        Capture("SphereProbeFacingMarkers");
        window.BackRangeSelector.Value = 0;
        window.SetProbe(new(0.2, 0.4, -0.8));
        await Settle();
        Require(window.FacingPlot.FacingProbeFraction > 1 && window.FacingPlot.FacingMarkerPoint(PaintModel.Original).X == FacingPoint(1).X,
            "a back probe outside the front-only view stays marked at the right edge");
        foreach (PaintModel model in Enum.GetValues<PaintModel>()) RequireMarkerPixels(model);
        Capture("ProbeOutsideFacingRange");
        window.SetProbe(new(-0.8, -0.2, -0.1));
        await Settle();
        Require(window.FacingPlot.FacingProbeFraction < 0 && window.FacingPlot.FacingMarkerPoint(PaintModel.Original).X == FacingPoint(0).X,
            "a probe beyond the left boundary stays marked at the left edge");
        foreach (PaintModel model in Enum.GetValues<PaintModel>()) RequireMarkerPixels(model);
        // Exact slice poles have no defined tilt; their constant slice and both
        // markers must still render without invalid normals or divisions.
        window.SetParameters(rangeMaterial with { RotationDegrees = 0 });
        window.SetProbe(new(0, 1, 0));
        await Settle();
        Require(window.FacingPlot.FacingProbeTilt == 0 && window.FacingPlot.FacingNormalAt(0) == window.Probe &&
            window.FacingPlot.FacingNormalAt(1) == window.Probe, "slice pole produces a finite constant facing curve");
        foreach (PaintModel model in Enum.GetValues<PaintModel>()) RequireMarkerPixels(model);
        window.SetParameters(rangeMaterial);
        window.SetProbe(rangeProbe);
        window.BackRangeSelector.Value = 90;
        await Settle();

        // Exercise the real drag bar handlers, including both zero-height
        // collapse and upward restoration using the same bar. Layout
        // changes must reclaim space for both graphs without changing previews.
        double initialFacingHeight = window.FacingPlot.ActualHeight;
        double initialColorHeight = window.ColorPlot.ActualHeight;
        double initialPreviewHeight = window.OriginalView.ActualHeight;
        Task layoutRender = window.CurrentRender;
        PaintParameters layoutParameters = window.Parameters;
        Direction layoutProbe = window.Probe;
        void DragDivider(double distance) {
            window.DetailsDivider.RaiseEvent(new DragStartedEventArgs(0, 0));
            window.DetailsDivider.RaiseEvent(new DragDeltaEventArgs(0, distance));
            window.DetailsDivider.RaiseEvent(new DragCompletedEventArgs(0, distance, false));
            window.ArrangeOffscreen(2560, 1392);
        }
        DragDivider(80);
        Require(Math.Abs(window.FacingPlot.ActualHeight - initialFacingHeight - 80) < 1 &&
            Math.Abs(window.ColorPlot.ActualHeight - initialColorHeight - 80) < 1, "dragging the divider adds height to both graphs");
        Require(Math.Abs(window.OriginalView.ActualHeight - initialPreviewHeight) < 1, "divider drag keeps sphere preview height");
        Capture("ResizedGraphs");
        double resizedFacingHeight = window.FacingPlot.ActualHeight;
        double resizedColorHeight = window.ColorPlot.ActualHeight;
        double reclaimedHeight = window.DetailTabs.ActualHeight;
        window.DetailTabs.SelectedIndex = 2;
        // A small pane must remain resizable below the former 180-pixel limit.
        DragDivider(reclaimedHeight - 40);
        Require(window.DetailTabs.Visibility == Visibility.Visible && Math.Abs(window.DetailTabs.ActualHeight - 40) < 1,
            "drag bar can shrink the details pane below the former minimum");
        DragDivider(1000);
        Require(window.DetailTabs.Visibility == Visibility.Collapsed && window.DetailTabs.RenderSize.Height == 0 &&
            window.DetailsDivider.Visibility == Visibility.Visible && window.DetailsDivider.ActualHeight == 18,
            "downward drag fully hides details but leaves a usable restore bar");
        Require(Math.Abs(window.FacingPlot.ActualHeight - resizedFacingHeight - reclaimedHeight) < 1 &&
            Math.Abs(window.ColorPlot.ActualHeight - resizedColorHeight - reclaimedHeight) < 1, "all collapsed space goes to both graphs");
        Require(Math.Abs(window.OriginalView.ActualHeight - initialPreviewHeight) < 1 &&
            Math.Abs(window.ProposedView.ActualHeight - initialPreviewHeight) < 1, "collapsing details keeps both sphere preview heights");
        Require(window.Parameters == layoutParameters && window.Probe == layoutProbe && window.CurrentRender == layoutRender,
            "layout changes preserve material, normal, and rendered previews");
        Capture("GraphsWithoutDetails");
        DragDivider(-reclaimedHeight);
        Require(window.DetailTabs.Visibility == Visibility.Visible && window.DetailsDivider.Visibility == Visibility.Visible &&
            window.DetailTabs.SelectedIndex == 2, "upward drag restores details with the previous selected tab");
        Require(Math.Abs(window.FacingPlot.ActualHeight - resizedFacingHeight) < 1 &&
            Math.Abs(window.ColorPlot.ActualHeight - resizedColorHeight) < 1, "upward drag restores the requested graph allocation");
        DragDivider(-1000);
        Require(window.FacingPlot.ActualHeight > 140 && window.ColorPlot.ActualHeight > 140,
            "upward drag keeps both graph panes usable at their minimum height");
        DragDivider(window.DetailTabs.ActualHeight - reclaimedHeight);
        DragDivider(-80);
        Require(Math.Abs(window.FacingPlot.ActualHeight - initialFacingHeight) < 1, "divider can return to the initial graph allocation");
        window.DetailTabs.SelectedIndex = 0;
        Require(!Descendants(window.Root).OfType<Button>().Any(button => Equals(button.Content, "Hide details") || Equals(button.Content, "Show details")),
            "details resizing uses only the drag bar without a show or hide button");
        log.Write($"Graph canvas heights at 2560x1392: facing {initialFacingHeight:F0}, color {initialColorHeight:F0}; collapsed gain {reclaimedHeight + 80:F0} pixels.");

        PaintParameters reported = new() {
            BaseColor = new(0.12955473163841805, 0.223, 0.578), Brightness = 0.37594897598870053,
            Shift = 0, RotationDegrees = 0, DarkPoint = 0.2101238082627118, LightPoint = 0.8306739936440678
        };
        window.SetParameters(reported);
        window.SetProbe(new(0, 0, 1));
        await Settle();
        Require(window.ColorPlot.ColorAxis == ColorResponseAxis.Facing, "default color plot includes the facing-to-tone remap");
        CompiledPaint reportedPaint = new(reported);
        PaintColor darkColor = window.ColorPlot.SampleColor(0);
        PaintColor lightColor = window.ColorPlot.SampleColor(1);
        Require(darkColor.MaximumDifference(reportedPaint.Evaluate(new(1, 0, 0), PaintModel.Original).LinearColor) < 1e-14, "plotted dark endpoint matches the sphere silhouette");
        Require(lightColor.MaximumDifference(reportedPaint.Evaluate(new(0, 0, 1), PaintModel.Original).LinearColor) < 1e-14 && lightColor.Blue < 0.56, "reported light endpoint matches the peak rather than white");
        Require(window.ColorPlot.BaseColorPosition is double baseFacing && window.ColorPlot.SampleColor(baseFacing).MaximumDifference(reported.BaseColor.Decode()) < 1e-14, "facing anchor reaches the decoded base color");
        Capture("ReportedFacingResponse");

        window.Controls["Dark"].Slider.Value = 0.35;
        await Settle();
        Require(window.ColorPlot.SampleColor(0).MaximumDifference(darkColor) > 0.01 && window.ColorPlot.SampleColor(1).MaximumDifference(lightColor) < 1e-14, "Dark Point changes only its facing endpoint");
        window.Controls["Light"].Slider.Value = 0.7;
        await Settle();
        Require(window.ColorPlot.SampleColor(1).MaximumDifference(lightColor) > 0.01, "Light Point changes its facing endpoint");

        window.SetParameters(reported);
        await Settle();
        Task beforeAxisChange = window.CurrentRender;
        window.ColorAxisSelector.SelectedIndex = (int)ColorResponseAxis.Tone;
        await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        Require(window.ColorPlot.ColorAxis == ColorResponseAxis.Tone && ReferenceEquals(beforeAxisChange, window.CurrentRender), "axis selector updates the plot without rerendering the spheres");
        Require(window.ColorPlot.SampleColor(0) == new PaintColor(0, 0, 0) && window.ColorPlot.SampleColor(1) == new PaintColor(1, 1, 1), "underlying tone curve retains its full black-to-white domain");
        Require(window.ColorPlot.BaseColorPosition == 1 - reported.Brightness, "tone view keeps its tone-space anchor");
        Require(window.ColorPlot.ProbePosition(PaintModel.Original) == reported.LightPoint, "tone-view probe uses remapped tone");
        Capture("ReportedToneResponse");
        PaintColor underlying = window.ColorPlot.SampleColor(0.5);
        window.Controls["Dark"].Slider.Value = 0.35;
        await Settle();
        Require(window.ColorPlot.SampleColor(0.5) == underlying, "endpoints select tones without changing the underlying curve");

        window.ColorAxisSelector.SelectedIndex = (int)ColorResponseAxis.Facing;
        window.SetParameters(reported);
        window.SetProbe(new(Math.Sqrt(0.75), 0, 0.5));
        await Settle();
        Require(Math.Abs(window.ColorPlot.ProbePosition(PaintModel.Original) - 0.5) < 1e-14 && Math.Abs(window.ColorPlot.ProbePosition(PaintModel.LinearWarp) - 0.25) < 1e-14, "facing-view markers use each model's facing");
        foreach (PaintModel model in Enum.GetValues<PaintModel>())
            Require(window.ColorPlot.SampleColor(window.ColorPlot.ProbePosition(model)).MaximumDifference(reportedPaint.Evaluate(window.Probe, model).LinearColor) < 1e-14, "plot marker agrees with inspected color for " + model);

        window.SetParameters(reported with { DarkPoint = 0.8, LightPoint = 0.2 });
        await Settle();
        Require(window.ColorPlot.SampleColor(0).Blue > window.ColorPlot.SampleColor(1).Blue, "reversed endpoints produce a decreasing facing response");
        Capture("InvertedColorResponse");
        window.SetParameters(reported with { DarkPoint = 0.42, LightPoint = 0.42 });
        await Settle();
        Require(window.ColorPlot.BaseColorPosition is null && window.ColorPlot.SampleColor(0).MaximumDifference(window.ColorPlot.SampleColor(1)) < 1e-14, "constant tone produces a flat response without a false anchor");
        Capture("ConstantColorResponse");
        double baseTone = 1 - reported.Brightness;
        window.SetParameters(reported with { DarkPoint = baseTone, LightPoint = baseTone });
        await Settle();
        Require(window.ColorPlot.BaseColorPosition is null && window.ColorPlot.SampleColor(0.5).MaximumDifference(reported.BaseColor.Decode()) < 1e-14, "constant base tone gives the base color everywhere");
        window.SetParameters(reported with { DarkPoint = 0.1, LightPoint = 0.2 });
        await Settle();
        Require(window.ColorPlot.BaseColorPosition is null, "out-of-range base tone has no facing anchor");
        window.ApplyExperiment(1);
        await Settle();

        ToolTip tip = new() { Content = window.OriginalView.ToolTip };
        tip.Measure(new(600, double.PositiveInfinity));
        tip.Arrange(new Rect(tip.DesiredSize));
        tip.UpdateLayout();
        Require(tip.ActualWidth <= tip.MaxWidth && tip.ActualHeight > 40, "sphere tooltip wraps into a compact box");
        Require(tip.Background is SolidColorBrush background && background.Color == Appearance.Panel.Color, "tooltip uses the dark background");
        TextBlock tooltipText = Descendants(tip).OfType<TextBlock>().Single();
        Require(tooltipText.TextWrapping == TextWrapping.Wrap && tooltipText.Foreground is SolidColorBrush foreground && foreground.Color == Appearance.Text.Color, "tooltip text wraps and contrasts with its background");
        RenderTargetBitmap tooltipImage = new((int)Math.Ceiling(tip.ActualWidth), (int)Math.Ceiling(tip.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        tooltipImage.Render(tip);
        PngBitmapEncoder tooltipEncoder = new();
        tooltipEncoder.Frames.Add(BitmapFrame.Create(tooltipImage));
        using (FileStream stream = File.Create(Path.Combine(log.DirectoryPath, "SphereTooltip.png"))) tooltipEncoder.Save(stream);

        Rect bounds = window.OriginalView.SphereBounds;
        Point center = new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
        Direction? centerNormal = window.OriginalView.NormalAt(center);
        Require(centerNormal is not null && (centerNormal.Value + new Direction(0, 0, -1)).Length < 1e-12, "sphere center maps to forward normal");
        Require(window.OriginalView.NormalAt(new(bounds.Left - 1, center.Y)) is null, "outside-sphere clicks rejected");
        Direction? upper = window.OriginalView.NormalAt(new(center.X, bounds.Top + bounds.Height / 4));
        Require(upper is not null && Math.Abs(upper.Value.Y - 0.5) < 1e-12, "screen up maps to positive Y");

        // Verify that the larger slider template still routes drag and track
        // commands into the same material and exact-value entry.
        Slider slider = window.Controls["Shift"].Slider;
        Track track = (Track)slider.Template.FindName("PART_Track", slider);
        double beforeDrag = slider.Value;
        track.Thumb.RaiseEvent(new DragDeltaEventArgs(20, 0));
        Require(slider.Value > beforeDrag && window.Parameters.Shift == slider.Value, "larger thumb drags update the material");
        Require(double.Parse(window.Controls["Shift"].Entry.Text, System.Globalization.CultureInfo.InvariantCulture) == slider.Value, "thumb drag updates the exact entry");
        double beforeTrack = slider.Value;
        Slider.IncreaseLarge.Execute(null, slider);
        Require(Math.Abs(slider.Value - beforeTrack - slider.LargeChange) < 1e-12, "slider track command still advances the value");

        // Exercise actual WPF control changes and their subscribed handlers.
        BitmapSource? previousImage = window.OriginalView.Image;
        window.Controls["Shift"].Slider.Value = 0.8;
        await Settle();
        Require(window.Parameters.Shift == 0.8, "slider changes shared parameters");
        Require(!ReferenceEquals(previousImage, window.OriginalView.Image), "slider regenerates preview");
        Require(window.FacingPlot.Paint.Parameters == window.Parameters && window.ColorPlot.Paint.Parameters == window.Parameters, "plots share new material");

        ParameterControl shift = window.Controls["Shift"];
        Task currentRender = window.CurrentRender;
        Require(shift.CommitText() && ReferenceEquals(currentRender, window.CurrentRender), "committing an unchanged entry does not restart rendering");
        shift.Entry.Text = "1";
        Require(!shift.CommitText() && window.Parameters.Shift == 0.8, "out-of-range shift leaves material unchanged");
        shift.Entry.Text = "NaN";
        Require(!shift.CommitText(), "nonfinite text rejected");
        shift.Entry.Text = "0.394000018715";
        Require(shift.CommitText() && window.Parameters.Shift == 0.394000018715, "exact text reaches material");
        window.SetParameters(window.Parameters with { RotationDegrees = Math.BitDecrement(360) });
        Require(window.Controls["Rotation"].CommitText(), "largest permitted rotation survives display and commit");
        Require(window.Parameters.RotationDegrees == Math.BitDecrement(360), "rotation precision preserved");

        // Rapid input cancels obsolete requests. Only the newest material may
        // reach the images, even when several worker renders finish out of order.
        for (int index = 0; index < 24; index++) window.Controls["Shift"].Slider.Value = index / 30.0;
        window.StageSelector.SelectedIndex = (int)PreviewStage.Facing;
        await Settle();
        Require(window.Parameters.Shift == 23.0 / 30, "rapid input keeps final value");
        RequirePixels(window, PreviewStage.Facing, false, Require);

        window.ApplyExperiment(2);
        window.StageSelector.SelectedIndex = (int)PreviewStage.Facing;
        await Settle();
        CompiledPaint paint = new(window.Parameters);
        Require(Math.Abs(paint.Evaluate(window.Probe, PaintModel.Original).Facing - 0.5) < 1e-12, "zero-shift exploration selects z = 0.5");
        Require(Math.Abs(paint.Evaluate(window.Probe, PaintModel.LinearWarp).Facing - 0.25) < 1e-12, "zero-shift exploration shows squared facing");
        RequirePixels(window, PreviewStage.Facing, false, Require);
        Capture("FacingComparison");

        window.BackSelector.IsChecked = true;
        await Settle();
        Require(window.Probe.Z < 0 && window.OriginalView.Back && window.ProposedView.Back, "hemisphere checkbox flips the probe and previews");
        RequirePixels(window, PreviewStage.Facing, true, Require);
        Require(!window.OriginalView.HasBackPeak && window.ProposedView.HasBackPeak, "only proposed model labels a back peak");
        Capture("BackComparison");

        window.ApplyExperiment(5);
        window.StageSelector.SelectedIndex = (int)PreviewStage.Tone;
        await Settle();
        Require(window.Parameters.DarkPoint > window.Parameters.LightPoint, "reverse-tones exploration inverts endpoints");
        RequirePixels(window, PreviewStage.Tone, false, Require);

        window.StageSelector.SelectedIndex = (int)PreviewStage.Normal;
        await Settle();
        RequirePixels(window, PreviewStage.Normal, false, Require);
        Capture("WarpedNormals");

        window.ApplyExperiment(6);
        await Settle();
        Require(Pixels(window.OriginalView.Image!).SequenceEqual(Pixels(window.ProposedView.Image!)), "constant tone produces identical painted spheres");

        window.MarkerSelector.IsChecked = false;
        Require(!window.OriginalView.ShowMarkers && !window.ProposedView.ShowMarkers, "marker checkbox updates both views");
        window.MarkerSelector.IsChecked = true;
        Button peak = Descendants(window.Root).OfType<Button>().Single(button => Equals(button.Content, "Probe peak"));
        peak.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require((window.Probe + new CompiledPaint(window.Parameters).Peak * -1).Length < 1e-12, "peak button selects shared peak");
        window.SetProbe(new CompiledPaint(window.Parameters).Peak * -1);
        await Settle();
        Require(window.BackSelector.IsChecked == true && window.ProposedView.Back, "normal selection can change hemisphere");

        window.ApplyExperiment(8);
        await Settle();
        string material = JsonSerializer.Serialize(window.Parameters, SessionLog.JsonOptions);
        PaintParameters? restored = JsonSerializer.Deserialize<PaintParameters>(material);
        Require(restored == window.Parameters, "material JSON round trip preserves every value");
        File.WriteAllText(Path.Combine(log.DirectoryPath, "VerifiedMaterial.json"), material);

        for (int index = 1; index <= 2; index++) {
            window.DetailTabs.SelectedIndex = index;
            window.ArrangeOffscreen(2560, 1392);
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            Require(Descendants(window.DetailTabs).OfType<ScrollViewer>().Any(), "explanation tab scrolls");
            Capture(index == 1 ? "Equations" : "SmoothnessAndCost");
        }

        Button reset = Descendants(window.Root).OfType<Button>().Single(button => Equals(button.Content, "Reset"));
        reset.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        window.DetailTabs.SelectedIndex = 0;
        await Settle();
        Require(window.Parameters == new PaintParameters() && window.BackSelector.IsChecked == false, "reset restores initial material and hemisphere");
        Task minimumLayoutRender = window.CurrentRender;
        window.ArrangeOffscreen(2184, 1280);
        await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        Capture("WindowedComparison");
        // A minimum-sized window has less client area because of its title bar.
        window.ArrangeOffscreen(1544, 1080);
        await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        foreach (FrameworkElement element in new FrameworkElement[] { window.OriginalView, window.ProposedView, window.FacingPlot, window.ColorPlot, window.DetailTabs, window.BackRangeSelector }) {
            Rect rectangle = element.TransformToAncestor(window.Root).TransformBounds(new Rect(element.RenderSize));
            Require(rectangle.Right <= window.Root.ActualWidth + 1 && rectangle.Bottom <= window.Root.ActualHeight + 1, "minimum layout contains " + element.GetType().Name);
        }
        Capture("MinimumWindow");
        double minimumFacingHeight = window.FacingPlot.ActualHeight;
        double minimumColorHeight = window.ColorPlot.ActualHeight;
        window.DetailsDivider.RaiseEvent(new DragDeltaEventArgs(0, 1000));
        window.ArrangeOffscreen(1544, 1080);
        Require(window.FacingPlot.ActualHeight > minimumFacingHeight + 200 && window.ColorPlot.ActualHeight > minimumColorHeight + 200,
            "collapsing details also enlarges both graphs at minimum window size");
        Rect dividerBounds = window.DetailsDivider.TransformToAncestor(window.Root).TransformBounds(new Rect(window.DetailsDivider.RenderSize));
        Require(dividerBounds.Height == 18 && dividerBounds.Width > 900 && dividerBounds.Bottom <= window.Root.ActualHeight,
            "collapsed drag bar remains fully accessible at minimum window size");
        Capture("MinimumWindowWithoutDetails");
        // Resizing a collapsed window must keep the pane hidden and retain a
        // usable drag bar, allowing it to reopen at the new window size.
        window.ArrangeOffscreen(2560, 1392);
        Require(window.DetailTabs.Visibility == Visibility.Collapsed && window.DetailsDivider.ActualHeight == 18,
            "window resizing preserves full collapse and the drag bar");
        DragDivider(-(reclaimedHeight + 80));
        Require(Math.Abs(window.FacingPlot.ActualHeight - initialFacingHeight) < 1 &&
            Math.Abs(window.ColorPlot.ActualHeight - initialColorHeight) < 1, "drag bar reopens details after resizing a collapsed window");
        Require(window.CurrentRender == minimumLayoutRender, "window resizing and pane dragging do not regenerate sphere images");
        Require(!window.IsVisible, "no physical display used throughout verification");
        log.Write($"Passed {checks} interface checks; completed {window.CompletedRenders} previews. Screenshots and verified parameters are in this session folder.");
    }

    private static void RequirePixels(MainWindow window, PreviewStage stage, bool back, Action<bool, string> require) {
        byte[][] images = [Pixels(window.OriginalView.Image!), Pixels(window.ProposedView.Image!)];
        CompiledPaint paint = new(window.Parameters);
        const int size = 448;
        // Independent pixel-to-normal mapping and display conversion verify
        // channel order, hemisphere, stage selection, and the newest parameters.
        foreach ((int x, int y) in new[] { (224, 224), (300, 140), (68, 250), (180, 405) }) {
            Direction normal = PaintMath.SphereNormal((x + 0.5) * 2 / size - 1, 1 - (y + 0.5) * 2 / size, back)!.Value;
            for (int model = 0; model < 2; model++) {
                PaintSample sample = paint.Evaluate(normal, (PaintModel)model);
                PaintColor expected = stage switch {
                    PreviewStage.Facing => new(sample.Facing, sample.Facing, sample.Facing),
                    PreviewStage.Tone => new(sample.Tone, sample.Tone, sample.Tone),
                    PreviewStage.Normal => new(0.5 + sample.WarpedNormal.Unit.X / 2, 0.5 + sample.WarpedNormal.Unit.Y / 2, 0.5 + sample.WarpedNormal.Unit.Z / 2),
                    _ => sample.DisplayColor
                };
                int offset = (y * size + x) * 4;
                require(images[model][offset] == (byte)Math.Round(expected.Blue * 255) &&
                    images[model][offset + 1] == (byte)Math.Round(expected.Green * 255) &&
                    images[model][offset + 2] == (byte)Math.Round(expected.Red * 255) && images[model][offset + 3] == 255,
                    $"{stage} pixel ({x},{y}), model {model}, back {back}");
            }
        }
        require(images[0][3] == 0 && images[1][3] == 0, "outside-sphere pixels transparent");
    }

    private static byte[] Pixels(BitmapSource image) {
        byte[] pixels = new byte[image.PixelWidth * image.PixelHeight * 4];
        image.CopyPixels(pixels, image.PixelWidth * 4, 0);
        return pixels;
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root) {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++) {
            DependencyObject child = VisualTreeHelper.GetChild(root, index);
            yield return child;
            foreach (DependencyObject descendant in Descendants(child)) yield return descendant;
        }
    }
}
