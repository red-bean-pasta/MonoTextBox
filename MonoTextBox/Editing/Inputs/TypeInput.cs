using MonoTextBox.Editing.Inputs.Bases;
using MonoTextBox.Editing.Inputs.Interfaces;

namespace MonoTextBox.Editing.Inputs;

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