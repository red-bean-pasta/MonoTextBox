using HarfBuzzSharp;

namespace HeadlessTextBox.Positioning.PositionCalculating;

public ref struct FormatPieceExtent{
    public int Height { get; }
    public ReadOnlySpan<GlyphInfo> GlyphInfos { get; }
    private readonly Span<GlyphPosition> _glyphPositions;
    public ReadOnlySpan<char> SourceChars { get; }


    public ReadOnlySpan<GlyphPosition> GlyphPositions => _glyphPositions;
    private int _width = -1;
    public int Width => CalculateWidth();
    public int GlyphLength => GlyphInfos.Length;
    public int CharLength => SourceChars.Length;
    
    
    public FormatPieceExtent(
        int height, 
        ReadOnlySpan<GlyphInfo> glyphInfos, 
        Span<GlyphPosition> glyphPositions,
        ReadOnlySpan<char> sourceChars)
    {
        Height = height;
        GlyphInfos = glyphInfos;
        _glyphPositions = glyphPositions;
        SourceChars = sourceChars;
    }

    private static FormatPieceExtent Empty(int height) => new(height, default, default, default);


    public ref GlyphPosition GetGlyphPosition(int index) => ref _glyphPositions[index];


    private int CalculateWidth()
    {
        if (_width < 0)
            _width = CalculateSliceWidth(0, GlyphInfos.Length);
        return _width;
    }

    public int CalculateSliceWidth(int glyphStart, int glyphLength)
    {
        var sum = 0;
        for (var i = glyphStart; i < glyphStart + glyphLength; i++) 
            sum += GlyphPositions[i].XAdvance;
        return sum;
    }


    public FormatPieceExtent Slice(int start, int length = -1)
    {
        if (length == 0)
            return Empty(Height);
        
        if (length < 0) 
            length = GlyphLength - start;
        
        var charStart = (int)GlyphInfos[start].Cluster;
        var charLength = start + length >= GlyphInfos.Length
            ? CharLength - charStart
            : (int)GlyphInfos[start + length].Cluster;
        
        return new FormatPieceExtent(
            Height, 
            GlyphInfos.Slice(start, length), 
            _glyphPositions.Slice(start, length),
            SourceChars.Slice(charStart, charLength)
        );
    }
}