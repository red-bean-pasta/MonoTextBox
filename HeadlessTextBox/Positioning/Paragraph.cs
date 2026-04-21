using System.Diagnostics;
using HeadlessTextBox.Compositing.Contracts;
using HeadlessTextBox.Positioning.LineManaging;
using HeadlessTextBox.Storage.WeightedTree;
using Icu;

namespace HeadlessTextBox.Positioning;

public class Paragraph: IBranch<Paragraph>
{
    private const int OptimizeScale = 64;
    
    public int CharCount { get; private set; }
    
    private readonly LineManager _lineManager = new();
    
    
    public int Length => CharCount;
    public float Height => (float)_lineManager.TotalHeight / OptimizeScale;
    
    
    public static Paragraph Empty => new();
    
    public static Paragraph Build(
        float width,
        in SourceRef paragraph,
        Locale? locale)
    {
        var p = new Paragraph();
        p.Init(width, paragraph, locale);
        return p;
    }
    
    private void Init(
        float width,
        in SourceRef paragraph,
        Locale? locale) 
        => Update(width, paragraph, 0, locale);

    public void Update(
        float lineWidth,
        in SourceRef paragraph,
        int changeIndex,
        Locale? locale)
    {
        Debug.Assert(!paragraph.GetTextSpan().IsWhiteSpace());
        
        var (lineIndex, inLineIndex) = _lineManager.FindIndex(changeIndex);
        // Rewrap to the start of previous line
        var updateIndex = changeIndex - inLineIndex - _lineManager.Lines[lineIndex - 1].CharLength;
        _lineManager.InvalidateAndRemoveLines(lineIndex);
        
        var updateRef = paragraph[updateIndex..];
        if (updateRef.Length == 0) 
            return;
        LinePositionHelper.ShapeSourceAndAppendToLine(lineWidth, updateRef, _lineManager, locale, OptimizeScale);
        
        CharCount = paragraph.Length;
    }
    
    
    public VisualGlyphEnumerator GetEnumerator() => new(_lineManager.Lines);


    [Obsolete("Splitting without SourceRef results in incorrect position")]
    public (Paragraph, Paragraph) Split(int index) => throw new NotImplementedException();
    
    
    public ref struct VisualGlyphEnumerator
    {
        private int _y;
        
        private int _lineIndex = 0;
        private int _glyphIndex = -1;
        private readonly IReadOnlyList<Line> _lines;

        
        private Line CurrentLine => _lines[_lineIndex];
        private LineGlyph CurrentGlyph => CurrentLine.Glyphs[_glyphIndex];
        
        public VisualGlyph Current => GetCurrentValue();
        
        
        public VisualGlyphEnumerator(IReadOnlyList<Line> lines)
        {
            _lines = lines;
            _lineIndex = 0;
            _glyphIndex = -1;
            _y = _lines.Count > 0 ? _lines[_lineIndex].AboveBaseline : 0;
        }

        
        public bool MoveNext()
        {
            if (_lineIndex >= _lines.Count)
                return false;
            
            _glyphIndex++;
            if (_glyphIndex < CurrentLine.GlyphLength)
                return true;
        
            if (_lineIndex + 1 >= _lines.Count)
                return false;
            
            _y += CurrentLine.BelowBaseline;
            _lineIndex++;
            _y += CurrentLine.AboveBaseline;
            _glyphIndex = 0;
            return true;
        }

        private VisualGlyph GetCurrentValue()
        {
            const float scale = OptimizeScale;
            var (id, cluster, x, xOffset, yOffset) = CurrentGlyph;
            return new VisualGlyph(
                id,
                x / scale,
                _y / scale,
                xOffset / scale,
                yOffset / scale
            );
        }
    }
}