using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ShaderUnderstanding;

internal static class Appearance {
    public static readonly SolidColorBrush Background = Brush("#101621");
    public static readonly SolidColorBrush Panel = Brush("#181F2C");
    public static readonly SolidColorBrush Text = Brush("#E6EDF5");
    public static readonly SolidColorBrush Quiet = Brush("#99ABC1");
    public static readonly SolidColorBrush Line = Brush("#344257");
    public static readonly SolidColorBrush Original = Brush("#64D5CF");
    public static readonly SolidColorBrush Proposed = Brush("#FFB574");

    public static SolidColorBrush Brush(string color) {
        SolidColorBrush brush = new((Color)ColorConverter.ConvertFromString(color));
        brush.Freeze();
        return brush;
    }

    public static TextBlock Label(string text, double size = 13, Brush? foreground = null, bool strong = false) => new() {
        Text = text, FontSize = size, Foreground = foreground ?? Text,
        FontWeight = strong ? FontWeights.SemiBold : FontWeights.Normal, TextWrapping = TextWrapping.Wrap
    };

    public static Border Surface(UIElement child, Thickness? padding = null) => new() {
        Child = child, Background = Panel, CornerRadius = new(8), Padding = padding ?? new Thickness(16)
    };

    public static Button Button(string text, Action action) {
        Button button = new() { Content = text };
        button.Click += (_, _) => action();
        return button;
    }

    public static Color DisplayColor(PaintColor color) => Color.FromRgb(Byte(color.Red), Byte(color.Green), Byte(color.Blue));
    public static byte Byte(double value) => (byte)Math.Round(Math.Clamp(value, 0, 1) * 255);
}
