using Microsoft.Win32;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ShaderUnderstanding;

internal sealed class MainWindow : Window {
    private readonly SessionLog log;
    internal readonly Grid Root = new() { Background = Appearance.Background };
    internal readonly Dictionary<string, ParameterControl> Controls = [];
    internal readonly SphereView OriginalView = new() { Accent = Appearance.Original };
    internal readonly SphereView ProposedView = new() { Accent = Appearance.Proposed };
    internal readonly CurveView FacingPlot = new();
    internal readonly CurveView ColorPlot = new() { ColorCurve = true };
    internal readonly ComboBox StageSelector = new() { Width = 214 };
    internal readonly ComboBox ColorAxisSelector = new() { Width = 196 };
    internal readonly Slider BackRangeSelector = new() {
        Minimum = 0, Maximum = 90, Value = 90, SmallChange = 1, LargeChange = 10,
        IsSnapToTickEnabled = true, TickFrequency = 1, Width = 144, Margin = new(12, 0, 12, 0)
    };
    internal readonly CheckBox MarkerSelector = new() { Content = "Markers", IsChecked = true, Margin = new(24, 0, 0, 0) };
    internal readonly TabControl DetailTabs = new() { ClipToBounds = true };
    internal readonly Thumb DetailsDivider = new() {
        HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch,
        Cursor = Cursors.SizeNS, Focusable = true,
        ToolTip = "Drag down to fully hide details; drag the bar back up to restore them. When focused, use the Up and Down arrow keys."
    };
    private readonly RowDefinition graphsRow = new() { Height = new(1.05, GridUnitType.Star), MinHeight = 260 };
    private readonly RowDefinition detailsRow = new() { Height = new(0.82, GridUnitType.Star) };
    private readonly ComboBox experimentSelector = new();
    private readonly TextBlock experimentNote = Appearance.Label("", 16, Appearance.Quiet);
    private readonly TextBlock status = Appearance.Label("Ready", 16, Appearance.Quiet);
    private readonly TextBlock stageNote = Appearance.Label("", 16, Appearance.Quiet);
    private readonly TextBlock normalNote = Appearance.Label("", 18, Appearance.Quiet);
    private readonly TextBlock differenceNote = Appearance.Label("", 16, Appearance.Quiet);
    private readonly Border baseSwatch = new() { Height = 24, CornerRadius = new(3), Margin = new(0, 12, 0, 24) };
    private readonly TextBlock[] originalValues = new TextBlock[7];
    private readonly TextBlock[] proposedValues = new TextBlock[7];
    private readonly Border originalSwatch = new() { Height = 24, CornerRadius = new(3), Margin = new(0, 8, 10, 0) };
    private readonly Border proposedSwatch = new() { Height = 24, CornerRadius = new(3), Margin = new(0, 8, 0, 0) };
    private bool updating;
    private bool closed;
    private CancellationTokenSource? renderCancellation;
    private long renderVersion;
    internal PaintParameters Parameters { get; private set; } = new();
    internal Direction Probe { get; private set; } = new(0, 0, 1);
    internal int CompletedRenders { get; private set; }
    internal Task CurrentRender { get; private set; } = Task.CompletedTask;
    internal string StatusText => this.status.Text;

    public MainWindow(SessionLog log) {
        this.log = log;
        this.DetailsDivider.SetResourceReference(StyleProperty, "DetailsDividerStyle");
        this.DetailsDivider.DragDelta += (_, arguments) => this.ResizeDetails(arguments.VerticalChange);
        this.DetailsDivider.KeyDown += (_, arguments) => {
            if (arguments.Key is not (Key.Up or Key.Down)) return;
            this.ResizeDetails(arguments.Key == Key.Up ? -16 : 16);
            arguments.Handled = true;
        };
        AutomationProperties.SetName(this.DetailsDivider, "Resize graphs and details vertically");
        this.Title = "SimplePaint - Shader Understanding";
        this.Width = 2200;
        this.Height = 1320;
        this.MinWidth = 1560;
        this.MinHeight = 1120;
        this.Background = Appearance.Background;
        this.FontFamily = new("Segoe UI");
        this.FontSize = 18;
        this.Content = this.Root;
        this.Root.RowDefinitions.Add(new() { Height = GridLength.Auto });
        this.Root.RowDefinitions.Add(new());
        this.Root.RowDefinitions.Add(new() { Height = GridLength.Auto });
        this.Root.Children.Add(this.BuildHeader());
        Grid body = new() { Margin = new(24, 0, 24, 0) };
        body.ColumnDefinitions.Add(new() { Width = new(472) });
        body.ColumnDefinitions.Add(new() { Width = new(24) });
        body.ColumnDefinitions.Add(new());
        Grid.SetRow(body, 1);
        this.Root.Children.Add(body);
        body.Children.Add(this.BuildControls());
        Grid exploration = this.BuildExploration();
        Grid.SetColumn(exploration, 2);
        body.Children.Add(exploration);
        this.status.Margin = new(24, 12, 24, 14);
        Grid.SetRow(this.status, 2);
        this.Root.Children.Add(this.status);
        this.OriginalView.ProbeChanged += this.SetProbe;
        this.ProposedView.ProbeChanged += this.SetProbe;
        this.FacingPlot.ProbeChanged += this.SetProbe;
        this.StageSelector.SelectionChanged += (_, _) => { if (!this.updating) this.Refresh(); };
        this.MarkerSelector.Checked += (_, _) => this.RefreshMarkers();
        this.MarkerSelector.Unchecked += (_, _) => this.RefreshMarkers();
        this.Closed += (_, _) => {
            this.closed = true;
            this.renderCancellation?.Cancel();
            this.log.SaveParameters(this.Parameters);
            this.log.Write("Closed normally.");
        };
        this.ApplyExperiment(1);
    }

    private Grid BuildHeader() {
        Grid header = new() { Margin = new(24, 20, 24, 20) };
        header.ColumnDefinitions.Add(new());
        header.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        StackPanel title = new();
        title.Children.Add(Appearance.Label("SimplePaint", 32, strong: true));
        title.Children.Add(Appearance.Label("A normal becomes a tone. A tone becomes a color.", 18, Appearance.Quiet));
        header.Children.Add(title);
        StackPanel actions = new() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        foreach ((string label, Action action) in new (string, Action)[] {
            ("Reset", () => this.ApplyExperiment(1)), ("Load parameters", this.LoadParameters),
            ("Save parameters", this.SaveParameters), ("Export PNG", this.ExportSnapshot)
        }) {
            Button button = Appearance.Button(label, action);
            button.Margin = new(12, 0, 0, 0);
            actions.Children.Add(button);
        }
        Grid.SetColumn(actions, 1);
        header.Children.Add(actions);
        return header;
    }

    private Border BuildControls() {
        StackPanel content = new();
        content.Children.Add(Appearance.Label("ONE MATERIAL, TWO MODELS", 18, Appearance.Quiet, true));
        content.Children.Add(Appearance.Label("All parameters are shared.", 16, Appearance.Quiet));
        content.Children.Add(this.baseSwatch);
        this.AddControl(content, "Red", "Base color / Red", PaintParameters.Margin, PaintParameters.MaximumInterior, 0.107, "Red channel in sRGB. The application decodes it once before evaluating the color curve.");
        this.AddControl(content, "Green", "Base color / Green", PaintParameters.Margin, PaintParameters.MaximumInterior, 0.223, "Green channel in sRGB. The displayed input is an sRGB value, not linear intensity.");
        this.AddControl(content, "Blue", "Base color / Blue", PaintParameters.Margin, PaintParameters.MaximumInterior, 0.578, "Blue channel in sRGB. Each channel uses its own anchored color curve.");
        this.AddControl(content, "Brightness", "Brightness", PaintParameters.Margin, PaintParameters.MaximumInterior, 0.5, "The base color appears at tone 1 - Brightness. Increasing this value moves the anchor left; it does not multiply RGB.");
        this.AddControl(content, "Shift", "Shift", 0, PaintParameters.MaximumInterior, 0.6, "Moves the highlight from the center toward the silhouette. The maximum's sideways component is Shift; its tilt is asin(Shift).");
        this.AddControl(content, "Rotation", "Rotation / degrees", 0, Math.BitDecrement(360), 45, "Counterclockwise on screen: 0 right, 90 up, 180 left, 270 down. At zero Shift, Rotation has no visible effect.", 1);
        this.AddControl(content, "Dark", "Dark Point", 0, PaintParameters.MaximumInterior, 0, "Input tone at facing 0. The Facing f to RGB plot shows its output color at the left endpoint. It may exceed Light Point.");
        this.AddControl(content, "Light", "Light Point", PaintParameters.Margin, 1, 1, "Input tone at facing 1. The Facing f to RGB plot shows its output color at the right endpoint. Equal endpoints give a constant color; reversed endpoints invert the gradient.");
        TextBlock heading = Appearance.Label("GUIDED EXPLORATIONS", 18, Appearance.Quiet, true);
        heading.Margin = new(0, 8, 0, 12);
        content.Children.Add(heading);
        foreach (string name in new[] { "Custom parameters", "Default comparison", "No shift: z versus z squared", "Near the silhouette", "Reverse the tones", "Make a constant color", "Original scene / sphere", "Original scene / body" }) this.experimentSelector.Items.Add(name);
        this.experimentSelector.SelectionChanged += (_, _) => {
            if (!this.updating && this.experimentSelector.SelectedIndex > 0) this.ApplyExperiment(this.experimentSelector.SelectedIndex);
        };
        content.Children.Add(this.experimentSelector);
        this.experimentNote.Margin = new(0, 14, 0, 0);
        content.Children.Add(this.experimentNote);
        ScrollViewer scroll = new() { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Padding = new(0, 0, 5, 0) };
        return Appearance.Surface(scroll);
    }

    private void AddControl(Panel content, string key, string label, double minimum, double maximum, double initial, string explanation, double step = 0.001) {
        ParameterControl control = new(label, minimum, maximum, initial, explanation, step);
        this.Controls.Add(key, control);
        content.Children.Add(control);
        control.Changed += _ => {
            if (this.updating) return;
            this.Parameters = new() {
                BaseColor = new(this.Controls["Red"].Value, this.Controls["Green"].Value, this.Controls["Blue"].Value),
                Brightness = this.Controls["Brightness"].Value, Shift = this.Controls["Shift"].Value,
                RotationDegrees = this.Controls["Rotation"].Value, DarkPoint = this.Controls["Dark"].Value, LightPoint = this.Controls["Light"].Value
            };
            this.experimentSelector.SelectedIndex = 0;
            this.experimentNote.Text = "Click either sphere to inspect a normal. Hover a parameter label for its meaning. Enter exact values and press Enter.";
            this.Refresh();
        };
    }

    private Grid BuildExploration() {
        Grid grid = new();
        grid.RowDefinitions.Add(new() { Height = new(60) });
        grid.RowDefinitions.Add(new() { Height = new(1.18, GridUnitType.Star), MinHeight = 260 });
        grid.RowDefinitions.Add(new() { Height = new(18) });
        // Keep the preview allocation fixed while the lower area is resized or
        // collapsed, so all reclaimed space goes to the graphs.
        grid.RowDefinitions.Add(new() { Height = new(1.87, GridUnitType.Star), MinHeight = 578 });
        StackPanel toolbar = new() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Top };
        TextBlock displayLabel = Appearance.Label("Display", 18, Appearance.Quiet);
        displayLabel.VerticalAlignment = VerticalAlignment.Center;
        displayLabel.Margin = new(0, 0, 14, 0);
        toolbar.Children.Add(displayLabel);
        foreach (string stage in new[] { "Painted color", "Facing f", "Remapped tone t", "Normal vectors" }) this.StageSelector.Items.Add(stage);
        this.StageSelector.SelectedIndex = 0;
        toolbar.Children.Add(this.StageSelector);
        toolbar.Children.Add(this.MarkerSelector);
        Button peak = Appearance.Button("Probe peak", () => this.SetProbe(new CompiledPaint(this.Parameters).Peak));
        peak.Margin = new(24, 0, 0, 0);
        toolbar.Children.Add(peak);
        grid.Children.Add(toolbar);
        Grid previews = PairGrid();
        previews.Children.Add(BuildPreview(this.OriginalView, "Original SimplePaint", "Shifted circular-slice facing", Appearance.Original));
        UIElement proposed = BuildPreview(this.ProposedView, "Linear warp + squared facing", "Linear normal transform, then squared facing", Appearance.Proposed);
        Grid.SetColumn(proposed, 2);
        previews.Children.Add(proposed);
        Grid.SetRow(previews, 1);
        grid.Children.Add(previews);
        Grid charts = PairGrid();
        StackPanel rangeControls = new() { Orientation = Orientation.Horizontal };
        Label rangeLabel = new() {
            Content = Appearance.Label("Back range", 18, Appearance.Quiet), Target = this.BackRangeSelector,
            VerticalContentAlignment = VerticalAlignment.Center, Padding = new(0)
        };
        TextBlock rangeValue = Appearance.Label("90°", 18, Appearance.Quiet);
        rangeValue.MinWidth = 36;
        rangeValue.VerticalAlignment = VerticalAlignment.Center;
        rangeControls.Children.Add(rangeLabel);
        rangeControls.Children.Add(this.BackRangeSelector);
        rangeControls.Children.Add(rangeValue);
        this.BackRangeSelector.ToolTip = "Visible back-hemisphere angle on each side of the facing graph. 0 degrees shows -90 to 90; 90 degrees shows -180 to 180. This changes the graph's range, not the shader.";
        AutomationProperties.SetName(this.BackRangeSelector, "Facing graph back range in degrees per side");
        this.BackRangeSelector.ValueChanged += (_, _) => {
            this.FacingPlot.BackRangeDegrees = this.BackRangeSelector.Value;
            rangeValue.Text = this.BackRangeSelector.Value.ToString("0", CultureInfo.InvariantCulture) + "°";
            this.FacingPlot.InvalidateVisual();
        };
        charts.Children.Add(BuildChart(this.FacingPlot, "01   Shape of the shading", "Facing f   |   original / teal     proposed / amber", headerControls: rangeControls));
        this.ColorAxisSelector.Items.Add("Facing f to RGB");
        this.ColorAxisSelector.Items.Add("Tone t to RGB");
        this.ColorAxisSelector.SelectedIndex = 0;
        this.ColorAxisSelector.ToolTip = "Facing f includes Dark Point and Light Point. Tone t shows the underlying curve over all tones; the shaded interval marks the tones selected by those endpoints. The base-color anchor may be outside that interval.";
        AutomationProperties.SetName(this.ColorAxisSelector, "Color response horizontal axis");
        UIElement colorChart = BuildChart(this.ColorPlot, "02   Shared color response", "Linear RGB | Dark and Light Points included", this.ColorAxisSelector);
        Grid.SetColumn(colorChart, 2);
        charts.Children.Add(colorChart);
        Grid lowerArea = new();
        lowerArea.RowDefinitions.Add(this.graphsRow);
        lowerArea.RowDefinitions.Add(new() { Height = new(18) });
        lowerArea.RowDefinitions.Add(this.detailsRow);
        lowerArea.Children.Add(charts);
        Grid.SetRow(this.DetailsDivider, 1);
        lowerArea.Children.Add(this.DetailsDivider);
        this.DetailTabs.Items.Add(new TabItem { Header = "Inspect a normal", Content = this.BuildInspector() });
        this.DetailTabs.Items.Add(new TabItem { Header = "Equations and meaning", Content = BuildGuide() });
        this.DetailTabs.Items.Add(new TabItem { Header = "Smoothness and cost", Content = BuildTradeoffs() });
        Grid.SetRow(this.DetailTabs, 2);
        lowerArea.Children.Add(this.DetailTabs);
        Grid.SetRow(lowerArea, 3);
        grid.Children.Add(lowerArea);
        return grid;
    }

    private void ResizeDetails(double verticalChange) {
        double available = this.graphsRow.ActualHeight + this.detailsRow.ActualHeight;
        double height = Math.Clamp(this.detailsRow.ActualHeight - verticalChange, 0, Math.Max(0, available - this.graphsRow.MinHeight));
        this.graphsRow.Height = new(available - height, GridUnitType.Star);
        this.detailsRow.Height = new(height, GridUnitType.Star);
        this.DetailTabs.Visibility = height > 0 ? Visibility.Visible : Visibility.Collapsed;
        // The bar always remains available, including when the details row is
        // zero. Flush layout so the next drag delta uses the current heights.
        this.Root.UpdateLayout();
    }

    private static Border BuildPreview(SphereView view, string title, string subtitle, Brush accent) {
        Grid grid = new();
        grid.RowDefinitions.Add(new() { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new() { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new());
        grid.RowDefinitions.Add(new() { Height = GridLength.Auto });
        grid.Children.Add(Appearance.Label(title, 22, accent, true));
        TextBlock description = Appearance.Label(subtitle, 16, Appearance.Quiet);
        Grid.SetRow(description, 1);
        grid.Children.Add(description);
        Grid.SetRow(view, 2);
        grid.Children.Add(view);
        TextBlock footnote = Appearance.Label("White ring: front probe   |   colored dot: peak direction", 16, Appearance.Quiet);
        Grid.SetRow(footnote, 3);
        grid.Children.Add(footnote);
        AutomationProperties.SetName(view, title + ". Click or use arrow keys to inspect a normal.");
        return Appearance.Surface(grid, new(20, 16, 20, 14));
    }

    private static Border BuildChart(CurveView view, string title, string subtitle, ComboBox? axisSelector = null, FrameworkElement? headerControls = null) {
        Grid grid = new();
        grid.RowDefinitions.Add(new() { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new() { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new());
        TextBlock heading = Appearance.Label(title, 18, strong: true);
        heading.VerticalAlignment = VerticalAlignment.Center;
        if (axisSelector is not null) {
            StackPanel axisControls = new() { Orientation = Orientation.Horizontal };
            Label inputLabel = new() {
                Content = Appearance.Label("Input axis", 18, Appearance.Quiet), Target = axisSelector,
                VerticalContentAlignment = VerticalAlignment.Center, Padding = new(0, 0, 12, 0)
            };
            axisControls.Children.Add(inputLabel);
            axisControls.Children.Add(axisSelector);
            headerControls = axisControls;
        }
        if (headerControls is null) grid.Children.Add(heading);
        else {
            DockPanel header = new();
            headerControls.Margin = new(16, 0, 0, 0);
            DockPanel.SetDock(headerControls, Dock.Right);
            header.Children.Add(headerControls);
            header.Children.Add(heading);
            grid.Children.Add(header);
        }
        TextBlock description = Appearance.Label(subtitle, 16, Appearance.Quiet);
        if (axisSelector is not null) axisSelector.SelectionChanged += (_, _) => {
            view.ColorAxis = (ColorResponseAxis)axisSelector.SelectedIndex;
            description.Text = view.ColorAxis == ColorResponseAxis.Facing ? "Linear RGB | Dark and Light Points included" : "Linear RGB | shaded: selected tone range";
            view.InvalidateVisual();
        };
        Grid.SetRow(description, 1);
        grid.Children.Add(description);
        Grid.SetRow(view, 2);
        grid.Children.Add(view);
        return Appearance.Surface(grid, new(18, 14, 18, 12));
    }

    private ScrollViewer BuildInspector() {
        StackPanel content = new();
        content.Children.Add(this.normalNote);
        Grid values = new() { Margin = new(0, 12, 0, 0) };
        values.ColumnDefinitions.Add(new() { Width = new(200) });
        values.ColumnDefinitions.Add(new());
        values.ColumnDefinitions.Add(new());
        string[] labels = ["Rotated normal", "Warped normal", "Light P / Dark Q", "Facing f", "Remapped tone t", "Linear RGB", "Display sRGB"];
        for (int row = 0; row < labels.Length; row++) {
            values.RowDefinitions.Add(new() { Height = GridLength.Auto });
            TextBlock label = Appearance.Label(labels[row], 18, Appearance.Quiet);
            Grid.SetRow(label, row);
            values.Children.Add(label);
            for (int column = 1; column <= 2; column++) {
                TextBlock value = Appearance.Label("", 18, column == 1 ? Appearance.Original : Appearance.Proposed);
                value.FontFamily = new("Consolas");
                value.Margin = new(0, 0, 12, 4);
                Grid.SetColumn(value, column);
                Grid.SetRow(value, row);
                values.Children.Add(value);
                (column == 1 ? this.originalValues : this.proposedValues)[row] = value;
            }
        }
        content.Children.Add(values);
        Grid swatches = PairGrid();
        swatches.Children.Add(this.originalSwatch);
        Grid.SetColumn(this.proposedSwatch, 2);
        swatches.Children.Add(this.proposedSwatch);
        content.Children.Add(swatches);
        this.differenceNote.Margin = new(0, 12, 0, 0);
        content.Children.Add(this.differenceNote);
        this.stageNote.Margin = new(0, 8, 0, 0);
        content.Children.Add(this.stageNote);
        return new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
    }

    private static ScrollViewer BuildGuide() {
        StackPanel text = new();
        AddExplanation(text, "1. Start with a camera-space normal", "+X is screen-right, +Y is up, +Z points toward the camera. Rotation turns XY by the negative input angle. A sphere lets you inspect every front-facing normal without changing the camera.");
        AddExplanation(text, "2. Choose a facing function", "Original, using a rotated unit normal (x,y,z):\nr = sqrt(x*x + z*z);  h = sqrt(1 - s*s)\nf = r*z*h / (r - s*x) for z > 0; otherwise f = 0.\n\nProposed, using an unnormalized rotated normal:\nw = (h*x - s*z, h*y, z)\nP = w.z*w.z;  Q = w.x*w.x + w.y*w.y;  f = P/(P+Q).", true);
        AddExplanation(text, "3. Remap facing to a tone", "t = DarkPoint*(1 - f) + LightPoint*f\nEqual endpoints give a constant tone. Reversed endpoints invert the progression.", true);
        AddExplanation(text, "4. Use the same anchored color curve", "For each decoded linear base-color channel c, with Brightness b:\nA = c*b;  B = (1-c)*(1-b)\nF(t) = A*t / (A*t + B*(1-t))\nF(0)=0, F(1)=1, F(1-b)=c. Finally, encode linear RGB to sRGB once.", true);
        AddExplanation(text, "Read the shared color response plot", "Facing f to RGB shows F(DarkPoint*(1-f) + LightPoint*f), including both tone endpoints. Changing Dark Point or Light Point changes these curves. Tone t to RGB shows the underlying F(t), which depends only on Base Color and Brightness; its shaded interval marks the tones the shader uses. In the facing view, the dashed base-color anchor appears only when that color is reachable. Reversed endpoints give decreasing facing-to-color curves; equal endpoints give flat curves.");
        AddExplanation(text, "Why P and Q are kept separately", "The original implementation also computes stable, nonnegative P and Q. Reconstructing Q from 1-f can erase tiny highlight complements. Their absolute sizes can differ between models; compare f and output color. The pixel shader combines their weights directly with precomputed color coefficients.");
        return new ScrollViewer { Content = text, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    }

    private static ScrollViewer BuildTradeoffs() {
        StackPanel text = new();
        AddExplanation(text, "What stays the same", "Both models share every material control, their front highlight direction, their silhouette tone, and their color response. The front maximum is (s, 0, sqrt(1-s*s)) before undoing Rotation.");
        AddExplanation(text, "What changes", "At zero Shift the original facing is z on the front hemisphere, while the proposed facing is z squared. The proposal has a second, antipodal maximum on the back. The original back hemisphere stays at Dark Point. The facing graph's Back range reveals this difference.");
        AddExplanation(text, "Infinite differentiability", "The original is smooth inside the front hemisphere, but generally has a derivative discontinuity where it meets its constant back hemisphere. The proposed facing and linear RGB are ratios of polynomials with positive denominators for every nonzero normal and Shift < 1, so they are infinitely differentiable there. The final sRGB encoding is piecewise defined. This claim excludes that display encoding, finite pixels, triangle seams, and quantization.");
        AddExplanation(text, "Where the shader work happens", "CPU: material validation, sRGB decoding, angle constants, and color coefficients.\nVertex stage: original rotation; proposed rotation plus a linear warp. Both commute with interpolation.\nPixel facing: original uses one square root at zero Shift and two otherwise. Proposed uses just P = z*z and Q = dot(xy,xy).\nPixel color: both retain the channel-wise rational curve and its divisions.");
        AddExplanation(text, "What this app measures", "This app evaluates the shader mathematics in double precision on the CPU and renders analytic spheres. It is an educational reference, not a GPU benchmark, a bit-for-bit emulation of binary32, or proof of a production speedup. Pixel coverage can miss very narrow peaks near maximum Shift.");
        AddExplanation(text, "Sources", "Original: 3dTestScene_CppDx12/Source/SimplePaint/Specification.md, SimplePaintCore.hlsli, and Material.cpp. Original facing-lobe and anchored color-curve construction: Kevin Vu. The linear-warp model implements the proposal from this conversation.");
        return new ScrollViewer { Content = text, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    }

    private static void AddExplanation(Panel panel, string title, string explanation, bool formula = false) {
        TextBlock heading = Appearance.Label(title, 20, Appearance.Text, true);
        heading.Margin = new(0, 0, 0, 8);
        panel.Children.Add(heading);
        TextBlock body = Appearance.Label(explanation, 18, Appearance.Quiet);
        body.Margin = new(0, 0, 14, 22);
        if (formula) body.FontFamily = new("Consolas");
        panel.Children.Add(body);
    }

    private static Grid PairGrid() {
        Grid grid = new();
        grid.ColumnDefinitions.Add(new());
        grid.ColumnDefinitions.Add(new() { Width = new(18) });
        grid.ColumnDefinitions.Add(new());
        return grid;
    }

    internal void SetParameters(PaintParameters parameters) {
        parameters.Validate();
        this.Parameters = parameters;
        this.updating = true;
        this.Controls["Red"].SetValue(parameters.BaseColor.Red);
        this.Controls["Green"].SetValue(parameters.BaseColor.Green);
        this.Controls["Blue"].SetValue(parameters.BaseColor.Blue);
        this.Controls["Brightness"].SetValue(parameters.Brightness);
        this.Controls["Shift"].SetValue(parameters.Shift);
        this.Controls["Rotation"].SetValue(parameters.RotationDegrees);
        this.Controls["Dark"].SetValue(parameters.DarkPoint);
        this.Controls["Light"].SetValue(parameters.LightPoint);
        this.updating = false;
        this.Refresh();
    }

    internal void SetProbe(Direction normal) {
        this.Probe = normal.Unit;
        this.UpdateInspection(new(this.Parameters));
    }

    private void RefreshMarkers() {
        this.OriginalView.ShowMarkers = this.MarkerSelector.IsChecked == true;
        this.ProposedView.ShowMarkers = this.MarkerSelector.IsChecked == true;
        this.OriginalView.InvalidateVisual();
        this.ProposedView.InvalidateVisual();
    }

    private void Refresh() {
        if (this.closed) return;
        CompiledPaint paint = new(this.Parameters);
        this.baseSwatch.Background = new SolidColorBrush(Appearance.DisplayColor(this.Parameters.BaseColor));
        this.FacingPlot.Paint = paint;
        this.ColorPlot.Paint = paint;
        this.FacingPlot.InvalidateVisual();
        this.ColorPlot.InvalidateVisual();
        this.UpdateInspection(paint);
        this.renderCancellation?.Cancel();
        CancellationTokenSource cancellation = new();
        this.renderCancellation = cancellation;
        long version = ++this.renderVersion;
        this.CurrentRender = this.RenderAsync(paint, (PreviewStage)this.StageSelector.SelectedIndex, cancellation, version);
    }

    private async Task RenderAsync(CompiledPaint paint, PreviewStage stage, CancellationTokenSource cancellation, long version) {
        Stopwatch duration = Stopwatch.StartNew();
        try {
            this.status.Text = "Updating the mathematical previews...";
            SphereImages images = await Task.Run(() => SphereRendering.Render(paint, stage, 448, cancellation.Token), cancellation.Token).ConfigureAwait(false);
            // Construction starts the first render before Application.Run has
            // installed a UI synchronization context. Never rely on await to
            // return to the owning thread, including for errors and cleanup.
            await this.UpdateOnDispatcher(() => {
                if (this.closed || cancellation.IsCancellationRequested || version != this.renderVersion) return;
                this.OriginalView.Image = SphereRendering.Bitmap(images.Original, images.Size);
                this.ProposedView.Image = SphereRendering.Bitmap(images.Proposed, images.Size);
                this.OriginalView.InvalidateVisual();
                this.ProposedView.InvalidateVisual();
                this.CompletedRenders++;
                this.status.Text = "CPU mathematical preview  |  Orthographic normals  |  Click or drag either sphere; click the facing plot to explore the back.";
                this.log.Write($"Preview {version} completed in {duration.Elapsed.TotalMilliseconds:F1} ms; {stage}.");
            });
        } catch (OperationCanceledException) {
            // Newer input replaces an obsolete render; the UI never blocks on it.
        } catch (Exception exception) {
            await this.UpdateOnDispatcher(() => {
                this.log.Write(exception.ToString());
                if (!this.closed && version == this.renderVersion) this.status.Text = "Preview failed. Details are in this session's Application.log.";
            });
            throw;
        } finally {
            try {
                await this.UpdateOnDispatcher(() => {
                    if (ReferenceEquals(this.renderCancellation, cancellation)) this.renderCancellation = null;
                });
            } finally { cancellation.Dispose(); }
        }
    }

    private async Task UpdateOnDispatcher(Action update) {
        if (this.Dispatcher.HasShutdownStarted) return;
        try { await this.Dispatcher.InvokeAsync(update); }
        catch (OperationCanceledException) when (this.Dispatcher.HasShutdownStarted) { }
    }

    private void UpdateInspection(CompiledPaint paint) {
        this.OriginalView.Probe = this.Probe;
        this.ProposedView.Probe = this.Probe;
        this.OriginalView.Peak = paint.Peak;
        this.ProposedView.Peak = paint.Peak;
        this.FacingPlot.Probe = this.Probe;
        this.ColorPlot.Probe = this.Probe;
        this.OriginalView.InvalidateVisual();
        this.ProposedView.InvalidateVisual();
        this.FacingPlot.InvalidateVisual();
        this.ColorPlot.InvalidateVisual();
        PaintSample original = paint.Evaluate(this.Probe, PaintModel.Original);
        PaintSample proposed = paint.Evaluate(this.Probe, PaintModel.LinearWarp);
        this.normalNote.Text = $"Selected view normal: {Format(this.Probe)}   |   {(this.Probe.Z < 0 ? "back" : this.Probe.Z == 0 ? "silhouette" : "front")}";
        FillValues(this.originalValues, original);
        FillValues(this.proposedValues, proposed);
        this.originalSwatch.Background = new SolidColorBrush(Appearance.DisplayColor(original.DisplayColor));
        this.proposedSwatch.Background = new SolidColorBrush(Appearance.DisplayColor(proposed.DisplayColor));
        this.differenceNote.Text = $"Maximum channel difference: {original.LinearColor.MaximumDifference(proposed.LinearColor):F6} linear RGB; {original.DisplayColor.MaximumDifference(proposed.DisplayColor):F6} sRGB.";
        this.stageNote.Text = this.StageSelector.SelectedIndex switch {
            1 => "Facing diagnostic: 0 = black, 1 = white. P and Q have arbitrary common scale; f = P/(P+Q).",
            2 => "Tone diagnostic: 0 = black, 1 = white, before the RGB curve. Tone is not displayed luminance.",
            3 => "Normal diagnostic: XYZ mapped to RGB as (normalized vector + 1)/2. The proposal shows the warped vector.",
            _ => "Painted color uses the shared curve in linear RGB, then one sRGB encoding. Inspector colors always show final paint."
        };
    }

    private static void FillValues(TextBlock[] fields, PaintSample sample) {
        fields[0].Text = Format(sample.RotatedNormal);
        fields[1].Text = Format(sample.WarpedNormal);
        fields[2].Text = $"{sample.Weights.Light:G6} / {sample.Weights.Dark:G6}";
        fields[3].Text = sample.Facing.ToString("F8", CultureInfo.InvariantCulture);
        fields[4].Text = sample.Tone.ToString("F8", CultureInfo.InvariantCulture);
        fields[5].Text = Format(sample.LinearColor);
        fields[6].Text = Format(sample.DisplayColor);
    }

    private static string Format(Direction value) => $"({value.X:F4}, {value.Y:F4}, {value.Z:F4})";
    private static string Format(PaintColor value) => $"({value.Red:F5}, {value.Green:F5}, {value.Blue:F5})";

    internal void ApplyExperiment(int index) {
        PaintParameters parameters = new();
        Direction probe = new(0, 0, 1);
        string note;
        switch (index) {
        case 2:
            parameters = parameters with { Shift = 0, RotationDegrees = 0 };
            note = "Switch Display to Facing f. On the front, compare z with z squared. Rotation has no effect at zero Shift.";
            probe = PaintMath.Sweep(60, 0);
            break;
        case 3:
            parameters = parameters with { Shift = 0.6, RotationDegrees = 0 };
            probe = PaintMath.Sweep(88, 0);
            note = "The probe is 2 degrees from the silhouette. Click and drag the facing plot across +90 to inspect the front/back transition.";
            break;
        case 4:
            parameters = parameters with { DarkPoint = 0.85, LightPoint = 0.15 };
            note = "The facing functions are unchanged. The tone remap runs backward, so the geometric maximum now receives the darker tone.";
            break;
        case 5:
            parameters = parameters with { DarkPoint = 0.5, LightPoint = 0.5 };
            note = "Both models give the base color everywhere at Brightness 0.5. Switch to Facing f to see the underlying shapes still differ.";
            break;
        case 6:
            parameters = parameters with { Brightness = 0.126, Shift = 0, RotationDegrees = 0 };
            note = "The sphere material from the current C++ scene. Increasing Brightness shifts the base-color anchor left on the color plot.";
            break;
        case 7:
            parameters = parameters with { BaseColor = new(PaintParameters.Margin, 0.45466664, 0.88), Brightness = 0.3070000145825, Shift = 0.394000018715, RotationDegrees = 60.00000285 };
            note = "The body material from the current C++ scene. The very small red channel makes the final white highlight narrow.";
            break;
        default:
            note = "Both highlights occupy the same position. Compare the gradient toward the rim, then switch Display to Facing f to remove color from the comparison.";
            break;
        }
        this.updating = true;
        this.StageSelector.SelectedIndex = 0;
        this.experimentSelector.SelectedIndex = index;
        this.updating = false;
        this.Probe = probe;
        this.experimentNote.Text = note;
        this.SetParameters(parameters);
        this.log.Write($"Selected exploration {index}.");
    }

    private void SaveParameters() {
        SaveFileDialog dialog = new() { Title = "Save material parameters", Filter = "JSON parameters (*.json)|*.json", FileName = "PaintParameters.json", InitialDirectory = this.log.DirectoryPath };
        if (dialog.ShowDialog(this) != true) return;
        this.GuardFileAction(() => File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(this.Parameters, SessionLog.JsonOptions)), "Parameters saved.");
    }

    private void LoadParameters() {
        OpenFileDialog dialog = new() { Title = "Load material parameters", Filter = "JSON parameters (*.json)|*.json", InitialDirectory = this.log.DirectoryPath };
        if (dialog.ShowDialog(this) != true) return;
        this.GuardFileAction(() => {
            PaintParameters parameters = JsonSerializer.Deserialize<PaintParameters>(File.ReadAllText(dialog.FileName)) ?? throw new InvalidDataException("The file contains no material.");
            parameters.Validate();
            this.SetParameters(parameters);
            this.experimentSelector.SelectedIndex = 0;
            this.experimentNote.Text = "Loaded material parameters. All previews and plots use the same values.";
        }, "Parameters loaded.");
    }

    private async void ExportSnapshot() {
        SaveFileDialog dialog = new() { Title = "Export the comparison", Filter = "PNG image (*.png)|*.png", FileName = "ShaderComparison.png", InitialDirectory = this.log.DirectoryPath };
        if (dialog.ShowDialog(this) != true) return;
        await this.WaitForPreview();
        this.GuardFileAction(() => this.SaveSnapshot(dialog.FileName), "Comparison image saved.");
    }

    internal async Task WaitForPreview() {
        Task pending;
        do {
            pending = this.CurrentRender;
            await pending;
        } while (!ReferenceEquals(pending, this.CurrentRender));
    }

    private void GuardFileAction(Action action, string success) {
        try { action(); this.status.Text = success; this.log.Write(success); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or ArgumentException) {
            this.log.Write(exception.ToString());
            this.status.Text = "Could not use that file: " + exception.Message;
        }
    }

    internal void SaveSnapshot(string path) {
        this.Root.UpdateLayout();
        RenderTargetBitmap target = new((int)Math.Ceiling(this.Root.ActualWidth), (int)Math.Ceiling(this.Root.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        target.Render(this.Root);
        PngBitmapEncoder encoder = new();
        encoder.Frames.Add(BitmapFrame.Create(target));
        using FileStream stream = File.Create(path);
        encoder.Save(stream);
    }

    internal void ArrangeOffscreen(double width, double height) {
        this.Root.Measure(new(width, height));
        this.Root.Arrange(new Rect(0, 0, width, height));
        this.Root.UpdateLayout();
    }
}
