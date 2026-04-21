namespace HeadlessTextBox.Formatting.Measuring;

public readonly record struct GlyphMetrics(
    float LeftBearing,
    float Width,
    float RightBearing
);

public readonly record struct FormatExtents(
    float Ascender,
    float Descender,
    float LineGap
);