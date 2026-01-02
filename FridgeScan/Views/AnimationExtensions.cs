namespace FridgeScan.Views;

public static class AnimationExtensions
{
    public static Task WidthRequestTo(this VisualElement element, double newWidth, uint length = 250)
    {
        double startWidth = element.WidthRequest;

        var animation = new Animation(v =>
        {
            element.WidthRequest = v;
        }, startWidth, newWidth);

        var tcs = new TaskCompletionSource<bool>();
        animation.Commit(element, "WidthAnimation", 16, length, Easing.Linear,
            (v, c) => tcs.SetResult(true));

        return tcs.Task;
    }
}
