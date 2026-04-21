using HarfBuzzSharp;
using HeadlessTextBox.Formatting;
using HeadlessTextBox.Positioning.LineManaging;

namespace HeadlessTextBox.Positioning.PositionCalculating;


public ref struct FormatPieceExtent
{
    public FontExtents FontExtents { get; }
    public ReadOnlySpan<GlyphInfo> GlyphInfos { get; }
    private readonly Span<GlyphPosition> _glyphPositions;
    
    public Headings Metadata { get; }


    private int _height = -1;
    public int Height => CalculateHeight();
    private int _width = -1;
    public int Width => CalculateWidth();
    public int GlyphLength => GlyphInfos.Length;
    public int CharLength => Metadata.SourceChars.Length;
    public ReadOnlySpan<GlyphPosition> GlyphPositions => _glyphPositions;
    
    
    private FormatPieceExtent(
        FontExtents fontExtents,
        ReadOnlySpan<GlyphInfo> glyphInfos, 
        Span<GlyphPosition> glyphPositions,
        Headings metadata)
    {
        FontExtents = fontExtents;

        GlyphInfos = glyphInfos;
        _glyphPositions = glyphPositions;
        
        Metadata = metadata;
    }

    public static FormatPieceExtent Build(
        FontExtents fontExtents,
        ReadOnlySpan<GlyphInfo> glyphInfos, 
        Span<GlyphPosition> glyphPositions,
        IFormat format,
        int scale,
        ReadOnlySpan<char> sourceChars)
    {
        return new FormatPieceExtent(fontExtents,
            glyphInfos, glyphPositions, new Headings(sourceChars, format, scale));
    }
    

    private FormatPieceExtent Empty() => new(FontExtents, default, default, Metadata.Empty());


    public ref GlyphPosition GetGlyphPosition(int index) => ref _glyphPositions[index];


    private int CalculateWidth()
    {
        if (_width < 0)
            _width = CalculateSliceWidth(0, GlyphInfos.Length);
        return _width;
    }

    private int CalculateHeight()
    {
        if (_height < 0)
            _height = FontExtents.CalculateHeight();
        return _height;
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
            return Empty();
        
        if (length < 0) 
            length = GlyphLength - start;
        
        var charStart = (int)GlyphInfos[start].Cluster;
        var charLength = start + length >= GlyphInfos.Length
            ? CharLength - charStart
            : (int)GlyphInfos[start + length].Cluster;
        
        return new FormatPieceExtent(FontExtents,
            GlyphInfos.Slice(start, length), _glyphPositions.Slice(start, length), new Headings(Metadata.SourceChars.Slice(charStart, charLength), Metadata.Format, Metadata.Scale));
    }
    
    
    public readonly ref struct Headings
    {
        public ReadOnlySpan<char> SourceChars { get; }
        public IFormat Format { get; }
        public int Scale { get; }
        
        public Headings(
            ReadOnlySpan<char> sourceChars,
            IFormat format, 
            int scale)
        {
            Format = format;
            SourceChars = sourceChars;
            Scale = scale;
        }
        
        public Headings Empty() => new(default, Format, Scale);
    }
}