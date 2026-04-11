using HeadlessTextBox.Legacy.Editing.Inputs.Bases;
using HeadlessTextBox.Legacy.Editing.Inputs.Interfaces;
using HeadlessTextBox.Utils;

namespace HeadlessTextBox.Legacy.Editing.Inputs;

public class TypeInput: UndoRedoInput, IAddableInput
{
    public TypeInput(
        int anchorIndex, 
        char character, 
        IEnumerable<char>? replaced = null) 
    : base(anchorIndex, character.Enumerate(), replaced)
    { }

    
    public void Add(char c) 
        => Content.Add(c);
    
    public void Add(IEnumerable<char> c) 
        => Content.AddRange(c);
}