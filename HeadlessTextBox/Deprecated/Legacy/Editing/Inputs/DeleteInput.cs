using HeadlessTextBox.Legacy.Editing.Inputs.Bases;
using HeadlessTextBox.Legacy.Editing.Inputs.Interfaces;
using HeadlessTextBox.Utils;
using HeadlessTextBox.Utils.Extensions;

namespace HeadlessTextBox.Legacy.Editing.Inputs;

public class DeleteInput: UndoRedoInput, IAddableInput
{
    public DeleteInput(
        int start,
        char deleted)
    : this(start, deleted.Enumerate())
    { }

    public DeleteInput(
        int start,
        IEnumerable<char>? replaced = null)
    : base(start, null, replaced)
    { }

    
    public void Add(char deleted) 
        => Replaced.Add(deleted);
    
    public void Add(IEnumerable<char> c)
        => Replaced.AddRange(c);
}