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
        Require(window.OriginalView.NormalAt(center) == new Direction(0, 0, 1), "sphere center maps to forward normal");
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
        window.ArrangeOffscreen(2184, 1280);
        await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        Capture("WindowedComparison");
        // A minimum-sized window has less client area because of its title bar.
        window.ArrangeOffscreen(1544, 1080);
        await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        foreach (FrameworkElement element in new FrameworkElement[] { window.OriginalView, window.ProposedView, window.FacingPlot, window.ColorPlot, window.DetailTabs }) {
            Rect rectangle = element.TransformToAncestor(window.Root).TransformBounds(new Rect(element.RenderSize));
            Require(rectangle.Right <= window.Root.ActualWidth + 1 && rectangle.Bottom <= window.Root.ActualHeight + 1, "minimum layout contains " + element.GetType().Name);
        }
        Capture("MinimumWindow");
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
