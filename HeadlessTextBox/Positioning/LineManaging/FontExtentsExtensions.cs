using HarfBuzzSharp;

namespace HeadlessTextBox.Positioning.LineManaging;

public static class FontExtentsExtensions
{
    public static int CalculateHeight(this FontExtents extents)
    {
        return Math.Abs(extents.Ascender) + Math.Abs(extents.Descender) + extents.LineGap;
    }
}