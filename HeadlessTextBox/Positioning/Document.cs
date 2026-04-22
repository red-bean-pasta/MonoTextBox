using System.Diagnostics;
using HeadlessTextBox.Compositing.Contracts;
using HeadlessTextBox.Positioning.SpanEnumerating;
using HeadlessTextBox.Storage.WeightedTree;
using HeadlessTextBox.Utils;
using Icu;
using JetBrains.Annotations;

namespace HeadlessTextBox.Positioning;

// Changing locale and width is effectively the same as calculating a new Document,
// therefore not implemented
public class Document : Node<Paragraph>
{
    private readonly float _width;

    private readonly Locale? _locale;

    
    /// <summary>
    /// To distinguish with <see cref="Node{T}.SubTreeHeight"/>: <br/>
    /// <see cref="SubTreeHeightY"/>: positional height in Y <br/>
    /// <see cref="Node{T}.SubTreeHeight"/>: node tree "depth"
    /// </summary>
    public float SubTreeHeightY { get; private set; }
    

    private Document(
        Paragraph branch,
        float width,
        Locale? locale)
        : base(branch, null, null)
    {
        _width = width;
        _locale = locale;
        
        CalculateHeight();
    }

    public static Document Build(
        float width,
        SourceBuffer doc,
        Locale? locale = null)
    {
        Document? result = null;

        foreach (var slice in doc[..].GetTextSpan().EnumerateNewLines())
        {
            var paragraph = Paragraph.Build(width, doc.Slice(slice), locale);

            if (result is null)
                result = new Document(paragraph, width, locale);
            else
                result = (Document)result.AppendAndBalance(paragraph);
        }

        Debug.Assert(result is not null);
        return result;
    }


    [MustDisposeResource]
    public DocumentGlyphEnumerator SliceEnumerateVisualGlyphs(float startHeight, float spanHeight) 
        => new(this, startHeight, spanHeight);


    /// <summary>
    /// 
    /// </summary>
    /// <param name="start"></param>
    /// <param name="length"></param>
    /// <param name="doc">Result doc after inserting</param>
    /// <returns></returns>
    public Document Insert(int start, int length, SourceBuffer doc)
    {
        var inserted = doc.Slice(start, length);
        return !HasNewLine(inserted.GetTextSpan())
            ? InsertInLine(start, length, doc)
            : InsertMultiLines(start, length, doc);
    }

    private Document InsertInLine(int start, int length, SourceBuffer doc)
    {
        var (paragraph, index) = Find(start);
        var paraStart = start - index;
        var paraLength = length + paragraph.Length;
        var updatedParagraph = doc.Slice(paraStart, paraLength);

        paragraph.Update(_width, updatedParagraph, index, _locale);

        return this;
    }

    private Document InsertMultiLines(int start, int length, SourceBuffer doc)
    {
        var result = this;
        
        var (paragraph, inParaIndex) = Find(start);
        var originalLeftLength = inParaIndex - 0;
        var originalRightLength = paragraph.Length - inParaIndex;

        var isFirst = true;
        var lastSlice = new Slice(-1, 0); 
        foreach (var slice in doc.Slice(start, length).GetTextSpan().EnumerateNewLines())
        {
            if (lastSlice.Start == -1)
            {
                lastSlice = slice; // Delay append to insert last new line to right
                continue;
            }
            
            if (isFirst)
            {
                var leftSlice = slice.Offset(start).Offset(-inParaIndex).Extend(originalLeftLength);
                var leftContent = doc.Slice(leftSlice);
                var leftParagraph = paragraph; 
                leftParagraph.Update(_width, leftContent, inParaIndex, _locale);
                isFirst = false;
                continue;
            }

            var middleSlice = slice.Offset(start);
            var middleContent = doc.Slice(middleSlice);
            var middleParagraph = Paragraph.Build(_width, middleContent, _locale);
            Insert(middleParagraph, middleSlice.Start);
        }
        var rightSlice = lastSlice.Offset(start).Extend(originalRightLength);
        var rightContent = doc.Slice(rightSlice);
        var rightParagraph = Paragraph.Build(_width, rightContent, _locale);
        Insert(rightParagraph, rightSlice.Start);

        return result;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="start"></param>
    /// <param name="removed"></param>
    /// <param name="doc">Result doc after removing</param>
    /// <returns></returns>
    public Document Remove(
        int start,
        ReadOnlySpan<char> removed,
        SourceBuffer doc)
    {
        return !HasNewLine(removed)
            ? RemoveInLine(start, removed, doc)
            : RemoveMultiLines(start, removed, doc);
    }

    private Document RemoveInLine(int start, ReadOnlySpan<char> removed, SourceBuffer doc)
    {
        var absoluteIndex = start;
        var (paragraph, relativeIndex) = Find(absoluteIndex);
        RemoveParagraphLine(doc, absoluteIndex, paragraph, relativeIndex, removed.Length);
        return this;
    }

    private Document RemoveMultiLines(int start, ReadOnlySpan<char> removed, SourceBuffer doc)
    {
        var result = this;

        (result, var first, var last) = result.RemoveLinesAndPop(start, removed, doc);

        var firstStart = start - first.Length;
        var merged = MergeParagraph(doc, firstStart, first, last);
        result = (Document)result.InsertAndBalance(firstStart, merged);
        
        return result;
    }

    private (Document Doc, Paragraph First, Paragraph Last) RemoveLinesAndPop(
        int start,
        ReadOnlySpan<char> removed,
        SourceBuffer doc)
    {
        Document? document = this;
        Paragraph? first = null;
        Paragraph? last = null;
        foreach (var slice in removed.EnumerateNewLines())
        {
            var absoluteStart = start;
            var length = slice.Length;

            var (paragraph, relativeStart) = Find(absoluteStart);

            if (length > 0)
                RemoveParagraphLine(doc, absoluteStart, paragraph, relativeStart, length);
            if (first is null)
                first = paragraph;
            else
                last = paragraph;
            document = (Document?)PopAndBalance(absoluteStart);
        }

        Debug.Assert(first is not null && last is not null && document is not null);
        return (document, first, last);
    }

    private void RemoveParagraphLine(
        SourceBuffer doc,
        int absoluteIndex,
        Paragraph paragraph,
        int relativeIndex,
        int removedLength)
    {
        Debug.Assert(removedLength > 0);

        var start = absoluteIndex - relativeIndex;
        var length = paragraph.Length - removedLength;
        var updated = doc.Slice(start, length);
        paragraph.Update(_width, updated, relativeIndex, _locale);
    }


    protected override void Recalculate()
    {
        base.Recalculate();
        CalculateHeight();
    }

    private void CalculateHeight()
    {
        SubTreeHeightY = 
            Value.Height
            + (((Document?)LeftSubNode)?.SubTreeHeightY ?? 0)
            + (((Document?)RightSubNode)?.SubTreeHeightY ?? 0);
    }
    

    private Paragraph MergeParagraph(
        SourceBuffer doc,
        int start,
        Paragraph left, 
        Paragraph right)
    {
        var length = left.Length + right.Length;
        var content = doc.Slice(start, length);
        return Paragraph.Build(_width, content, _locale);
    }


    private static bool HasNewLine(ReadOnlySpan<char> text) => text.IndexNewLine() >= 0;


    private (int Start, float InParagraphHeight, Paragraph Paragraph) FindParagraphWithHeight(float startY)
    {
        Debug.Assert(startY >= 0);
        
        var left = (Document?)LeftSubNode;
        var leftHeight = left?.SubTreeHeightY ?? 0;
        if (startY < leftHeight)
        {
            Debug.Assert(left is not null);
            return left.FindParagraphWithHeight(startY);
        }

        var right = (Document?)RightSubNode;
        var beforeRightHeight = Value.Height + leftHeight;
        if (right is not null && startY >= beforeRightHeight)
        {
            Debug.Assert(right is not null);
            return right.FindParagraphWithHeight(startY - beforeRightHeight);
        }

        return (LeftLength, Value.Length, Value);
    }


    [MustDisposeResource]
    public ref struct DocumentGlyphEnumerator
    {
        private NodeEnumerator _paragraphEnumerator;
        private Paragraph.VisualGlyphEnumerator _paraGlyphEnumerator;
        
        public VisualGlyph Current => _paraGlyphEnumerator.Current;
        
        public DocumentGlyphEnumerator(Document document, float startHeight, float spanHeight)
        {
            var (startIndex, inStartHeight, startParagraph) = document.FindParagraphWithHeight(startHeight);
            var (endIndex, inEndHeight, endParagraph) = document.FindParagraphWithHeight(spanHeight);
            var start = startIndex;
            var end = endIndex + endParagraph.Length;

            _paragraphEnumerator = document.GetEnumerator(start, end);
            _paragraphEnumerator.MoveNext();
            _paraGlyphEnumerator = _paragraphEnumerator.Current.GetEnumerator();
        }

        public bool MoveNext()
        {
            if (_paraGlyphEnumerator.MoveNext())
                return true;

            if (!_paragraphEnumerator.MoveNext())
                return false;

            _paraGlyphEnumerator = _paragraphEnumerator.Current.GetEnumerator();
            _paraGlyphEnumerator.MoveNext();
            return true;
        }
        
        public void Dispose() => _paragraphEnumerator.Dispose();
    }
}