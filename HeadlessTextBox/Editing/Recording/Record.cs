using HeadlessTextBox.Formatting;
using HeadlessTextBox.Storage;

namespace HeadlessTextBox.Editing.Recording;

// To avoid ambiguous indexing:
//  Insert can only happen after remove
//  Format-only change cannot co-exist with text change
public record Record(
    Caret CaretBefore,
    TextUnit RemovedText,
    FormatUnit RemovedFormat,
    TextUnit InsertedText,
    FormatUnit AppliedFormat
);


// Logic grouping when add buffer fails  compactable  backspacing
public readonly record struct TextUnit(
    int Start, // Useful for Buffer compacting
    int Count
);

public readonly record struct TextBufferRef(
    int Position,
    int Start,
    int Length
);


public readonly record struct FormatUnit(
    int Start,
    int Count
);

public readonly record struct FormatBufferPiece(
    int Position,
    int Length,
    IFormat Format
);


public class TextRefBuffer: AddBuffer<TextBufferRef>
{
    public int Length => InChunkNextPosition;
    public TextBufferRef LastRef => LastItem;
    
    public TextRefBuffer(int chunkSize = 64 * 1024 / 8) : base(chunkSize)
    {}

    public void ExtendLastRef(int length)
    {
        var last = LastItem;
        LastItem = last with { Length = last.Length + length };
    }
}

public class TextBuffer: AddBuffer<char>
{
    public int Length => InChunkNextPosition;
    
    public TextBuffer(int chunkSize = 64 * 1024) : base(chunkSize)
    {}
}


public class FormatBuffer : AddBuffer<FormatBufferPiece>
{
    public int Length => InChunkNextPosition;
    
    public FormatBuffer(int size = 64 * 1024 / 8) : base(size)
    {}

    public (int Start, int Count) AppendFormat(FormatBufferPiece format) 
        => Append(format);
    
    public (int Start, int Count) AppendFormat(ReadOnlySpan<FormatBufferPiece> format) 
        => Append(format);
    
    
    public bool MergeAppendFormat(FormatBufferPiece format)
    {
        var last = LastItem;
        
        if (last.Format != format.Format)
        {
            AppendFormat(format);
            return false;
        }

        if (last.Position + last.Length == format.Position)
        {
            LastItem = last with { Length = last.Length + format.Length };
            return true;
        }
        
        if (format.Position + format.Length == last.Position)
        {
            LastItem = last with
            {
                Position = format.Position,
                Length = last.Length + format.Length
            };
            return true;
        }
        
        AppendFormat(format);
        return false;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="formats">Extended length</param>
    /// <returns></returns>
    public int MergeAppendFormat(ReadOnlySpan<FormatBufferPiece> formats)
    {
        var extended = 0;
        foreach (var format in formats)
        {
            if (MergeAppendFormat(format))
                continue;
            extended++;
        }
        return extended;
    }


    public new void Prune(int length) => base.Prune(length);
}


