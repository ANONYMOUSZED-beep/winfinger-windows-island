using System.Windows;
using System.Windows.Media.Animation;

namespace WinFinger.Controls;

/// <summary>Animates a CornerRadius (all four corners interpolated independently).</summary>
public sealed class CornerRadiusAnimation : AnimationTimeline
{
    public static readonly DependencyProperty FromProperty =
        DependencyProperty.Register(nameof(From), typeof(CornerRadius), typeof(CornerRadiusAnimation));

    public static readonly DependencyProperty ToProperty =
        DependencyProperty.Register(nameof(To), typeof(CornerRadius), typeof(CornerRadiusAnimation));

    public static readonly DependencyProperty EasingFunctionProperty =
        DependencyProperty.Register(nameof(EasingFunction), typeof(IEasingFunction), typeof(CornerRadiusAnimation));

    public CornerRadius From
    {
        get => (CornerRadius)GetValue(FromProperty);
        set => SetValue(FromProperty, value);
    }

    public CornerRadius To
    {
        get => (CornerRadius)GetValue(ToProperty);
        set => SetValue(ToProperty, value);
    }

    public IEasingFunction? EasingFunction
    {
        get => (IEasingFunction?)GetValue(EasingFunctionProperty);
        set => SetValue(EasingFunctionProperty, value);
    }

    public override Type TargetPropertyType => typeof(CornerRadius);

    protected override Freezable CreateInstanceCore() => new CornerRadiusAnimation();

    public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
    {
        double progress = animationClock.CurrentProgress ?? 0;
        if (EasingFunction is { } easing) progress = easing.Ease(progress);

        var from = From;
        var to = To;
        return new CornerRadius(
            Lerp(from.TopLeft, to.TopLeft, progress),
            Lerp(from.TopRight, to.TopRight, progress),
            Lerp(from.BottomRight, to.BottomRight, progress),
            Lerp(from.BottomLeft, to.BottomLeft, progress));
    }

    private static double Lerp(double a, double b, double t) => a + (b - a) * t;
}
