namespace HeadlessTextBox.Formatting.Measuring;

public readonly record struct GlyphMetrics(
    float LeftSideBearing,
    float Width,
    float RightSideBearing,
    float Ascender,
    float Descender,
    float LineGap
);