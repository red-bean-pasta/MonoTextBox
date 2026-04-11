using HeadlessTextBox.Compositing.Contract;
using HeadlessTextBox.Utils;

namespace HeadlessTextBox.Deprecated.Positioning.SourceReading;

public interface ISourceable
{
    TextElement this[Index index] { get; }
    ISource this[System.Range range] { get; }
    
    ISource Slice(int start, int length); 
    ISource Slice(Slice slice);
}