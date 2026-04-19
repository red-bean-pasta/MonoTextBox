using HarfBuzzSharp;
using HeadlessTextBox.Positioning.PositionCalculating;

namespace HeadlessTextBox.Positioning;


public class Line
{
    public int CharLength { get; private set; } = 0;
    public int Height { get; private set; } = 0;
    public int Width { get; private set; } = 0;
    private readonly List<GlyphInfo> _infos = new(256);
    private readonly List<GlyphPosition> _positions = new(256);
    
    
    public bool IsEmpty => CharLength == 0;
    public IReadOnlyList<GlyphInfo> Infos => _infos;
    public IReadOnlyList<GlyphPosition> Positions => _positions;


    public void Append(FormatPieceExtent extent)
    {
        CharLength += extent.CharLength;
        
        if (extent.Height > Height) 
            Height = extent.Height;
        Width += extent.Width;
        
        // Hopefully won't be a bottleneck else adopt AddBuffer
        foreach (var info in extent.GlyphInfos)
            _infos.Add(info);
        foreach (var position in extent.GlyphPositions)
            _positions.Add(position);
    }
}