using HarfBuzzSharp;
using HeadlessTextBox.Formatting.Measuring;

namespace HeadlessTextBox.Formatting;

public interface IFormat : IEquatable<IFormat>
{ }


public interface IMeasurableFormat : IFormat, IEquatable<IMeasurableFormat>
{
    uint GetGlyphId(char character);
    
    GlyphMetrics GetGlyphMetrics(char character);
}


public interface IFontFormat : IFormat, IEquatable<IFontFormat>
{
    Font Font { get; }
    
    int FontSize { get; }
}