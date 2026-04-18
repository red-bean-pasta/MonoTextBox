using System.Diagnostics;

namespace HeadlessTextBox.Editing.Recording;

public class TextRecorder
{
    private readonly TextRefBuffer _refBuffer = new();
    private readonly TextBuffer _textBuffer = new();


    public (TextUnit Removed, TextUnit Inserted) GetNewUnits()
    {
        TextUnit removed; TextUnit inserted;
        removed = inserted = new TextUnit(_refBuffer.Length, 0);
        return (removed, inserted);
    }
    
    
    public TextUnit ExtendInsert(
        Caret caretBefore,
        ReadOnlySpan<char> textInserted, 
        TextUnit insertUnit)
    {
        return ExtendUnit(caretBefore, textInserted, insertUnit);
    }
    
    public TextUnit ExtendDelete(
        Caret caretBefore,
        ReadOnlySpan<char> textDeleted, 
        TextUnit deleteUnit)
    {
        return ExtendUnit(caretBefore, textDeleted, deleteUnit);
    }
    
    // For backspace, ref can't be extended, because _textBuffer is append-only.
    // For example:
    //  Backspace 'f', 'e' and 'd' from "abcdef"
    //  Buffer will be "fed"
    //  Simply extend buffer will undo to "abcfed"
    public TextUnit ExtendBackspace(
        Caret caretBefore,
        ReadOnlySpan<char> textRemoved, 
        TextUnit backspaceUnit)
    {
        Debug.Assert(backspaceUnit.Start + backspaceUnit.Count == _refBuffer.Length);

        var (addStart, addLength) = _textBuffer.AppendText(textRemoved);
        
        var position = caretBefore.Length > 0 ? caretBefore.Left : caretBefore.Start - 1;
        Debug.Assert(position >= 0);
        var (refStart, refCount) = _refBuffer.AppendRef(new TextBufferRef(position, addStart, addLength));
        
        return backspaceUnit with { Count = backspaceUnit.Count + refCount };
    }
    
    
    public void Prune(TextUnit baseUnit)
    {
        var firstRefIndex = baseUnit.Start;
        var firstCharIndex = _refBuffer.GetSpan(firstRefIndex, 1)[0].Start;
        
        var pruneRefLength = firstRefIndex - 0;
        _refBuffer.Prune(pruneRefLength);
        var pruneCharLength = firstCharIndex - 0;
        _textBuffer.Prune(pruneCharLength);
    }
    

    private TextUnit ExtendUnit(
        Caret caretBefore,
        ReadOnlySpan<char> text, 
        TextUnit unit)
    {
        Debug.Assert(unit.Start + unit.Count == _refBuffer.Length);
        
        var (addStart, addLength) = _textBuffer.AppendText(text);
        
        if (unit.Count > 0 && CheckIfExtendable(caretBefore))
        {
            _refBuffer.ExtendLastRef(text.Length);
            return unit;
        }
    
        var position = caretBefore.Left;
        var (refStart, refCount) = _refBuffer.AppendRef(new TextBufferRef(position, addStart, addLength));

        if (unit.Count <= 0)
            return unit with { Start = refStart, Count =  refCount };
        
        return unit with { Count = unit.Count + refCount };
    }

    private bool CheckIfExtendable(Caret caret)
    {
        var last = _refBuffer.LastRef;
        if (last.Position + last.Length != caret.Left)
            return false;
        if (last.Start + last.Length != _refBuffer.Length)
            return false;
        return true;
    }


    public int CalculateUnitLength(TextUnit unit)
    {
        if (unit.Count <= 0)
            return 0;
        
        var sum = 0;
        var refs = _refBuffer.GetSpan(unit.Start, unit.Count);
        foreach (var r in refs) 
            sum += r.Length;
        return sum;
    }
}