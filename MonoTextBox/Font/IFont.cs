namespace MonoTextBox.Font;

public interface IFont
{
    float Spacing { get; }
    GlyphMetrics GetGlyph(char c);
}