namespace HeadlessTextBox.Formatting.Font;

public interface IFontMeasurable
{
    GlyphMetrics GetGlyphMetrics(char c);
}