using HeadlessTextBox.Legacy.Editing.Inputs.Bases;
using HeadlessTextBox.Legacy.Editing.Inputs.Interfaces;
using HeadlessTextBox.Utils;

namespace HeadlessTextBox.Legacy.Editing.Inputs;

public class DeleteInput: UndoRedoInput, IAddableInput
{
    public DeleteInput(
        int anchorIndex,
        char deleted)
    : this(anchorIndex, deleted.Enumerate())
    { }

    public DeleteInput(
        int anchorIndex,
        IEnumerable<char>? replaced = null)
    : base(anchorIndex, null, replaced)
    { }

    
    public void Add(char deleted) 
        => Replaced.Add(deleted);
    
    public void Add(IEnumerable<char> c)
        => Replaced.AddRange(c);
}