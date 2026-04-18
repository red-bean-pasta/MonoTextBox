using HeadlessTextBox.Formatting.Measuring;

namespace HeadlessTextBox.Formatting;

public interface IFormat : IEquatable<IFormat>
{
    GlyphMetrics GetGlyphMetrics(char character);
}