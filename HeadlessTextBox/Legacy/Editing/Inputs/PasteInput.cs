using HeadlessTextBox.Legacy.Editing.Inputs.Bases;

namespace HeadlessTextBox.Legacy.Editing.Inputs;

public class PasteInput: UndoRedoInput
{
    public PasteInput(
        int anchorIndex,
        IEnumerable<char> pasted, 
        IEnumerable<char>? replaced = null)
        : base(anchorIndex, pasted, replaced)
    { }
}