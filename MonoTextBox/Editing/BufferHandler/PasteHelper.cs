using System.Diagnostics;
using MonoTextBox.Editing.Inputs;

namespace MonoTextBox.Editing.BufferHandler;

public static class PasteHelper
{
    public static (PasteInput, Caret) Paste(
        List<char> source,
        Caret caret, 
        string pasted)
    {
        Debug.Assert(!string.IsNullOrEmpty(pasted));

        var replaced = caret.Slice(source);
        source.RemoveRange(caret.StartIndex, caret.Length);
        source.InsertRange(caret.StartIndex, pasted);
        
        var input = new PasteInput(
            caret.LeftIndex,
            pasted, 
            replaced);
        var updatedCaret = new Caret(caret.StartIndex + pasted.Length, 0 );
        return (input, updatedCaret);
    }
}