using System.Diagnostics;
using System.Runtime.InteropServices;
using HeadlessTextBox.Formatting;

namespace HeadlessTextBox.Editing.Recording;

public class FormatRecorder
{
    private readonly FormatBuffer _formatBuffer = new();
    
    
    public (FormatUnit Removed, FormatUnit Applied) GetNewUnits()
    {
        FormatUnit removed; FormatUnit inserted;
        removed = inserted = new FormatUnit(_formatBuffer.Length, 0);
        return (removed, inserted);
    }
    
    
    public FormatUnit ExtendApply(
        int position,
        int length, 
        IFormat format, 
        FormatUnit applyUnit)
    {
        var piece = new FormatPiece(format, length);
        return ExtendApply(position, MemoryMarshal.CreateSpan(ref piece, 1), applyUnit);
    }
    
    public FormatUnit ExtendApply(
        int position,
        ReadOnlySpan<FormatPiece> formats, 
        FormatUnit applyUnit)
    {
        return ExtendUnit(position, formats, applyUnit);
    }
    
    public FormatUnit ExtendRemove(
        int position,
        ReadOnlySpan<FormatPiece> formats, 
        FormatUnit deleteUnit)
    {
        return ExtendUnit(position, formats, deleteUnit);
    }
    
    public FormatUnit ExtendBackspace(
        int position,
        ReadOnlySpan<FormatPiece> formats, 
        FormatUnit backspaceUnit)
    {
        // Backspace in format is different with backspace in text. 
        // Text records deleted chars and have order problem. 
        // While format is only about length and start.
        return ExtendUnit(position, formats, backspaceUnit);
    }
    
    
    public void Prune(FormatUnit baseUnit)
    {
        var firstIndex = baseUnit.Start;
        var pruneLength = firstIndex - 0;
        _formatBuffer.Prune(pruneLength);
    }
    
    
    private FormatUnit ExtendUnit(
        int position,
        ReadOnlySpan<FormatPiece> formats, 
        FormatUnit unit)
    {
        Debug.Assert(unit.Start + unit.Count != _formatBuffer.Length);
        
        if (unit.Count <= 0)
        {
            var (addStart, addLength) = AppendFormatsToBuffer(position, formats);
            return unit with { Start = addStart, Count = addLength };
        }
    
        var extended = MergeAppendFormatsToBuffer(position, formats);
        return unit with { Count = unit.Count + extended };
    }

    
    private (int Start, int Length) AppendFormatsToBuffer(
        int position, 
        ReadOnlySpan<FormatPiece> formats)
    {
        var start = -1; var length = 0;
        foreach (var format in formats)
        {
            var piece = new FormatBufferPiece(position, format.Length, format.Format);
            var (addStart, addLength) = _formatBuffer.AppendFormat(piece);
            
            if (start < 0) start = addStart;
            length += addLength;
        }
        return (start, length);
    }

    /// <returns>Extended length. If all is merged, the return value is 0</returns>
    private int MergeAppendFormatsToBuffer(
        int position, 
        ReadOnlySpan<FormatPiece> formats)
    {
        var length = 0;
        foreach (var format in formats)
        {
            var piece = new FormatBufferPiece(position, format.Length, format.Format);
            var merged = _formatBuffer.MergeAppendFormat(piece);
            if (!merged) length++;
        }
        return length;
    }
}