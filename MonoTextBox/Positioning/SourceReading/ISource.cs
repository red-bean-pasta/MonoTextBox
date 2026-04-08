using MonoTextBox.Formatting;
using MonoTextBox.Utils;

namespace MonoTextBox.Positioning.SourceReading;

public interface ISource
{
    (char Char, Format Format) this[Index index] { get; }
    Buffer this[System.Range range] { get; }
    
    Buffer Slice(int start, int length); 
    Buffer Slice(Slice slice);
}