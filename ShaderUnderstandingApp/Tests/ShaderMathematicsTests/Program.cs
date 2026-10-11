using ShaderUnderstanding;

int checks = 0;
void Check(bool condition, string message) {
    checks++;
    if (!condition) throw new InvalidOperationException(message);
}
void Near(double actual, double expected, double tolerance, string message) => Check(Math.Abs(actual - expected) <= tolerance, $"{message}: {actual:G17} versus {expected:G17}");
PaintColor Reference(PaintParameters parameters, Direction normal) {
    double angle = -parameters.RotationDegrees * Math.PI / 180;
    double x = (normal.X * Math.Cos(angle) - normal.Y * Math.Sin(angle)) / normal.Length;
    double z = normal.Z / normal.Length;
    double radius = Math.Sqrt(x * x + z * z);
    double facing = z > 0 ? radius * z * Math.Sqrt(1 - parameters.Shift * parameters.Shift) / (radius - parameters.Shift * x) : 0;
    double tone = parameters.DarkPoint * (1 - facing) + parameters.LightPoint * facing;
    double Channel(double color) {
        double decoded = PaintMath.DecodeSrgb(color);
        double a = decoded * parameters.Brightness;
        double b = (1 - decoded) * (1 - parameters.Brightness);
        return a * tone / (a * tone + b * (1 - tone));
    }
    return new(Channel(parameters.BaseColor.Red), Channel(parameters.BaseColor.Green), Channel(parameters.BaseColor.Blue));
}

Random random = new(451);
double maximumError = 0;
for (int index = 0; index < 30000; index++) {
    double Interior() => PaintParameters.Margin + random.NextDouble() * (1 - 2 * PaintParameters.Margin);
    PaintParameters parameters = new() {
        BaseColor = new(Interior(), Interior(), Interior()), Brightness = Interior(),
        Shift = index % 5 == 0 ? 0 : random.NextDouble() * PaintParameters.MaximumInterior,
        RotationDegrees = random.NextDouble() * 360,
        DarkPoint = random.NextDouble() * PaintParameters.MaximumInterior,
        LightPoint = Interior()
    };
    CompiledPaint paint = new(parameters);
    Direction normal = new Direction(random.NextDouble() * 2 - 1, random.NextDouble() * 2 - 1, random.NextDouble() * 2 - 1).Unit;
    PaintSample original = paint.Evaluate(normal, PaintModel.Original);
    maximumError = Math.Max(maximumError, original.LinearColor.MaximumDifference(Reference(parameters, normal)));
    foreach (PaintModel model in Enum.GetValues<PaintModel>()) {
        PaintSample sample = paint.Evaluate(normal, model);
        Check(sample.Weights.Light >= 0 && sample.Weights.Dark >= 0 && sample.Weights.Light + sample.Weights.Dark > 0, "Positive weights");
        Check(sample.Facing >= 0 && sample.Facing <= 1, "Bounded facing");
        Near(sample.LinearColor.MaximumDifference(paint.Evaluate(normal * 1.7, model).LinearColor), 0, 1e-11, "Normal scale invariance");
        Near(paint.Evaluate(paint.Peak, model).Facing, 1, 1e-13, "Shared front peak");
    }
    Direction other = new(0.3, -0.7, 0.4);
    Direction firstWarp = paint.Warp(paint.Rotate(normal * 0.4 + other * 0.6));
    Direction secondWarp = paint.Warp(paint.Rotate(normal)) * 0.4 + paint.Warp(paint.Rotate(other)) * 0.6;
    Near((firstWarp + secondWarp * -1).Length, 0, 1e-14, "Warp commutes with interpolation");
}
Check(maximumError < 1e-10, "Stable original matches the independent rational reference");

foreach (double shift in new[] { 0.0, 0.6, PaintParameters.MaximumInterior }) {
    CompiledPaint paint = new(new() { Shift = shift, RotationDegrees = 0 });
    foreach (Direction normal in new[] { new Direction(1, 0, 0), new(0, 1, 0), new(0, -1, 0), new(-1, 0, 0) })
        foreach (PaintModel model in Enum.GetValues<PaintModel>()) Near(paint.Evaluate(normal, model).Facing, 0, 0, "Silhouette endpoint");
    Near(paint.Evaluate(paint.Peak * -1, PaintModel.Original).Facing, 0, 0, "Original back hemisphere");
    Near(paint.Evaluate(paint.Peak * -1, PaintModel.LinearWarp).Facing, 1, 1e-13, "Proposed antipodal peak");
    foreach (Direction normal in new[] { new Direction(0, 1, 1e-30), new(1e-30, 1, 1e-30), new(0, 0, -1) })
        foreach (PaintModel model in Enum.GetValues<PaintModel>()) Check(double.IsFinite(paint.Evaluate(normal, model).Facing), "Poles and back normals remain finite");
}

for (int index = 1; index < 1000; index++) {
    double z = index / 1000.0;
    Direction normal = new(Math.Sqrt(1 - z * z), 0, z);
    CompiledPaint zero = new(new() { Shift = 0 });
    Near(zero.Evaluate(normal, PaintModel.Original).Facing, z, 1e-15, "Original zero-shift cosine");
    Near(zero.Evaluate(normal, PaintModel.LinearWarp).Facing, z * z, 1e-15, "Squared-facing zero shift");
    Near(zero.Curve(1 - zero.Parameters.Brightness).MaximumDifference(zero.Parameters.BaseColor.Decode()), 0, 1e-15, "Base-color anchor");
}
CompiledPaint constant = new(new() { DarkPoint = 0.42, LightPoint = 0.42 });
foreach (PaintModel model in Enum.GetValues<PaintModel>())
    Near(constant.Evaluate(new(1, 0, 0), model).LinearColor.MaximumDifference(constant.Evaluate(new(0, 0, 1), model).LinearColor), 0, 1e-14, "Constant tone endpoints");
CompiledPaint inverted = new(new() { DarkPoint = 0.8, LightPoint = 0.2 });
Check(inverted.Evaluate(new(0, 1, 0), PaintModel.Original).LinearColor.Red > inverted.Evaluate(inverted.Peak, PaintModel.Original).LinearColor.Red, "Reversed tone endpoints are supported");
foreach (PaintParameters invalid in new[] { new PaintParameters { Shift = 1 }, new() { RotationDegrees = 360 }, new() { Brightness = double.NaN }, new() { BaseColor = new(0, 0.5, 0.5) } }) {
    bool rejected = false;
    try { _ = new CompiledPaint(invalid); } catch (ArgumentOutOfRangeException) { rejected = true; }
    Check(rejected, "Invalid parameters must be rejected");
}
Console.WriteLine($"{checks:N0} checks passed; 30,000 independent original-shader comparisons; maximum linear RGB error {maximumError:G5}.");
