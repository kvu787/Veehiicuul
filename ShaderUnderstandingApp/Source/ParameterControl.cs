using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ShaderUnderstanding;

internal sealed class ParameterControl : StackPanel {
    private readonly Slider slider;
    private readonly TextBox entry;
    private bool updating;
    public event Action<double>? Changed;
    public double Value => this.slider.Value;
    public TextBox Entry => this.entry;
    public Slider Slider => this.slider;

    public ParameterControl(string label, double minimum, double maximum, double initial, string explanation, double step = 0.001) {
        this.Margin = new(0, 0, 0, 14);
        TextBlock heading = Appearance.Label(label, 13, strong: true);
        heading.ToolTip = explanation;
        this.Children.Add(heading);
        Grid row = new() { Margin = new(0, 5, 0, 0) };
        row.ColumnDefinitions.Add(new());
        row.ColumnDefinitions.Add(new() { Width = new(99) });
        this.slider = new() { Minimum = minimum, Maximum = maximum, Value = initial, SmallChange = step, LargeChange = step * 10, ToolTip = explanation };
        this.entry = new() { VerticalContentAlignment = VerticalAlignment.Center, ToolTip = explanation, FontFamily = new("Consolas"), FontSize = 12 };
        AutomationProperties.SetName(this.slider, label);
        AutomationProperties.SetName(this.entry, label + " exact value");
        Grid.SetColumn(this.entry, 1);
        row.Children.Add(this.slider);
        row.Children.Add(this.entry);
        this.Children.Add(row);
        this.slider.ValueChanged += (_, _) => {
            if (this.updating) return;
            this.entry.Text = this.Value.ToString("R", CultureInfo.InvariantCulture);
            this.ClearError();
            this.Changed?.Invoke(this.Value);
        };
        this.entry.KeyDown += (_, arguments) => {
            if (arguments.Key == Key.Enter) { this.CommitText(); arguments.Handled = true; }
            if (arguments.Key == Key.Escape) { this.SetValue(this.Value); arguments.Handled = true; }
        };
        this.entry.LostKeyboardFocus += (_, _) => this.CommitText();
        this.SetValue(initial);
    }

    public void SetValue(double value) {
        this.updating = true;
        this.slider.Value = value;
        this.entry.Text = value.ToString("R", CultureInfo.InvariantCulture);
        this.ClearError();
        this.updating = false;
    }

    public bool CommitText() {
        if (!double.TryParse(this.entry.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ||
            !double.IsFinite(value) || value < this.slider.Minimum || value > this.slider.Maximum) {
            this.entry.BorderBrush = Appearance.Proposed;
            this.entry.ToolTip = $"Enter a number from {this.slider.Minimum:R} to {this.slider.Maximum:R}. Escape restores the current value.";
            return false;
        }
        bool changed = value != this.Value;
        this.SetValue(value);
        if (changed) this.Changed?.Invoke(value);
        return true;
    }

    private void ClearError() {
        this.entry.BorderBrush = Appearance.Line;
        this.entry.ToolTip = this.slider.ToolTip;
    }
}
