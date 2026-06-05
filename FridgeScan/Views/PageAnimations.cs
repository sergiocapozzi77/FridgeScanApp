namespace FridgeScan.Views;

/// <summary>
/// Provides consistent entrance animations for pages.
/// The animation only plays on the first appearance — once Content.Opacity reaches 1,
/// subsequent appearances skip the animation (no re-trigger on tab switch-back).
/// </summary>
public static class PageAnimations
{
    /// <summary>
    /// Fades content in from its current opacity to 1.
    /// Only performs the animation if content opacity is near 0 (first appearance).
    /// </summary>
    /// <param name="content">The root visual element of the page (<see cref="ContentPage.Content"/>).</param>
    /// <param name="duration">Animation duration in milliseconds.</param>
    public static async Task FadeIn(VisualElement? content, uint duration = 300)
    {
        if (content is null || content.Opacity > 0.01f)
            return;

        await content.FadeTo(1, duration, Easing.CubicOut);
    }
}
