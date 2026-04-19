using System.Diagnostics;
using HeadlessTextBox.Compositing.Contracts;
using HeadlessTextBox.Positioning.Manual.SpanEnumerating;
using HeadlessTextBox.Storage.WeightedTree;
using HeadlessTextBox.Utils;
using Icu;

namespace HeadlessTextBox.Positioning;

// Changing locale and width is effectively the same as calculating a new Document,
// therefore not implemented
public class Document : Node<ParagraphBranch>
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
        ParagraphBranch branch,
        float width,
        Locale? locale)
        : base(branch, null, null)
    {
        _width = width;
        _locale = locale;
    }

    public static Document Build(
        float width,
        SourceBuffer doc,
        Locale? locale = null)
    {
        Document? result = null;

        foreach (var slice in doc[..].GetTextSpan().EnumerateNewLines())
        {
            var paragraph = ParagraphBranch.Build(width, doc.Slice(slice), locale);

            if (result is null)
                result = new Document(paragraph, width, locale);
            else
                result = (Document)result.AppendAndBalance(paragraph);
        }

        Debug.Assert(result is not null);
        return result;
    }
    

    public NodeEnumerator EnumerateSliced(int start, int length) => base.GetEnumerator(start, length);

    
    public (float OffsetHeight, int StartIndex, int Length) FindInHeightIndices(float startHeight, float spanHeight)
    {
        var offsetHeight = 0f;
        var startIndex = -1;
        var endIndex = -1;

        var weightIndex = 0;
        var heightSum = 0f;
        using var nodeEnumerator = GetEnumerator();
        foreach (var paragraph in nodeEnumerator)
        {
            heightSum += paragraph.Height;

            if (startIndex == -1 && heightSum >= startHeight)
            {
                startIndex = weightIndex;
                offsetHeight = heightSum - paragraph.Height - startHeight;
            }

            if (heightSum >= startHeight + spanHeight)
            {
                endIndex = weightIndex;
                break;
            }
            
            weightIndex += paragraph.Length;
        }
        
        return (offsetHeight, startIndex, endIndex - startIndex);
    }


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
        var lastSlice = new Slice(-1, 0); // Delay append to insert last new line to right

        foreach (var slice in doc.Slice(start, length).GetTextSpan().EnumerateNewLines())
        {
            var absolute = slice + start;
            if (lastSlice.Start == -1)
            {
                lastSlice = absolute;
                continue;
            }

            result = result.InsertSplitLine(lastSlice, doc, true);
            lastSlice = slice;
        }

        result = result.InsertSplitLine(lastSlice, doc, false);

        return result;
    }

    private Document InsertSplitLine(Slice slice, SourceBuffer doc, bool toLeft)
    {
        var (result, left, right) = FindAndSplit(slice.Start);

        var target = toLeft ? left : right;
        var start = toLeft ? slice.Start - target.Length : slice.Start;
        var length = target.Length + slice.Length;
        var content = doc.Slice(start, length);
        target.Update(_width, content, slice.Start, _locale);

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

    private (Document Doc, ParagraphBranch First, ParagraphBranch Last) RemoveLinesAndPop(
        int start,
        ReadOnlySpan<char> removed,
        SourceBuffer doc)
    {
        Document? document = this;
        ParagraphBranch? first = null;
        ParagraphBranch? last = null;
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
        ParagraphBranch paragraph,
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
        SubTreeHeightY = 
            Value.Height
            + (((Document?)LeftSubNode)?.SubTreeHeightY ?? 0)
            + (((Document?)RightSubNode)?.SubTreeHeightY ?? 0);
        
        base.Recalculate();
    }


    private (Document Doc, ParagraphBranch Left, ParagraphBranch Right) FindAndSplit(int index)
    {
        var (branch, innerIndex) = Find(index);
        var branchStart = index - innerIndex;
        var branchEnd = branchStart + branch.Length;

        Document doc;
        ParagraphBranch left;
        ParagraphBranch right;
        if (innerIndex == 0)
        {
            left = ParagraphBranch.Empty();
            right = branch;
            doc = (Document)InsertAndBalance(branchStart, left);
        }
        else if (innerIndex == branch.Length)
        {
            left = branch;
            right = ParagraphBranch.Empty();
            doc = (Document)InsertAndBalance(branchEnd, right);
        }
        else
        {
            (left, right) = branch.Split(innerIndex);
            var removed = (Document?)PopAndBalance(branchStart);
            var inserted = (Document?)removed?.InsertAndBalance(branchStart, right) ?? new Document(right, _width, _locale);
            doc = (Document)inserted.InsertAndBalance(branchStart, left);
        }

        return (doc, left, right);
    }


    private ParagraphBranch MergeParagraph(
        SourceBuffer doc,
        int start,
        ParagraphBranch left, 
        ParagraphBranch right)
    {
        var length = left.Length + right.Length;
        var content = doc.Slice(start, length);
        return ParagraphBranch.Build(_width, content, _locale);
    }


    private static bool HasNewLine(ReadOnlySpan<char> text) => text.IndexNewLine() >= 0;
}


public class ParagraphBranch: Paragraph, IBranch<ParagraphBranch>
{
    public int Length => CharCount;

    
    public ParagraphBranch()
    { }
    
    public new static ParagraphBranch Empty() => new();
    
    private ParagraphBranch(List<Line> lines): base(lines)
    { }
    
    public new static ParagraphBranch Build(
        float width,
        in SourceRef paragraph,
        Locale? locale)
    {
        var p = Empty();
        p.Update(width, paragraph, 0, locale);
        return p;
    }
    
    
    public (ParagraphBranch, ParagraphBranch) Split(int index)
    {
        Debug.Assert(0 < index && index < Length);
        
        var lineIndex = FindLine(index);
        var leftPart = Lines[lineIndex].Positions.Take(index);
        var rightPart = Lines[lineIndex].Positions.Skip(index);
        
        var rightCount = Lines.Count - lineIndex;
        var rightLine = new Line(rightPart);
        var rightLines = new List<Line>(rightCount){rightLine};
        rightLines.AddRange(Lines.Skip(lineIndex));
        
        var leftLines = Lines.Take(lineIndex).ToList();
        var leftLine = new Line(leftPart);
        leftLines.Add(leftLine);

        return (
            new ParagraphBranch(leftLines), 
            new ParagraphBranch(rightLines)
        );
    }


    private int FindLine(int charIndex)
    {
        var sum = 0;
        for (var i = 0; i < Lines.Count; i++)
        {
            sum += Lines[i].CharLength;
            if (sum <= charIndex)
                continue;
            return i;
        }
        throw new IndexOutOfRangeException();
    }
}