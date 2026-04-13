namespace HeadlessTextBox;

public readonly record struct TextBox(
    float X,
    float Y,
    float Width,
    float Height,
    float OffsetY,
    IEnumerable<VisualChar> VisualChars
)
{
    
}