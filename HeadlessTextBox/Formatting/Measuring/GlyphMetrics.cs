namespace HeadlessTextBox.Formatting.Measuring;

public readonly record struct GlyphMetrics(
    float LeftBearing,
    float Width,
    float RightBearing,
    float Ascender,
    float Descender,
    float LineGap
);