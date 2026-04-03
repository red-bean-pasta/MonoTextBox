namespace MonoTextBox.Positioning.Helpers;

public interface ILineBreaker
{
    public void Break(
        ReadOnlySpan<char> text, 
        Action<int, int> onBreak
    );
}