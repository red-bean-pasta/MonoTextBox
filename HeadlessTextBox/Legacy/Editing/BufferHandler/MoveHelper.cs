using System.Diagnostics;
using HeadlessTextBox.Legacy.Editing.Inputs;

namespace HeadlessTextBox.Legacy.Editing.BufferHandler;

public static class MoveHelper
{
    public static (MoveInput, Caret) Move(
        List<char> source,
        Caret startCaret,
        int destIndex)
    {
        var selected = startCaret.Slice(source);
        
        Debug.Assert(selected != null);
        var input = new MoveInput(startCaret, destIndex);
        var caret = GetMovedCaret(startCaret, destIndex);
        return (input, caret);
    }

    private static Caret GetMovedCaret(
        Caret startCaret,
        int destIndex)
    {
        var offset = startCaret.LeftIndex - destIndex;
        var start = startCaret.StartIndex - offset;
        var selection = startCaret.Selection;
        return new Caret(start, selection);
    }
}