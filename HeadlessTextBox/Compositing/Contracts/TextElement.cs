using HeadlessTextBox.Formatting;

namespace HeadlessTextBox.Compositing.Contracts;

public readonly record struct TextElement(
    char Char, 
    IFormat Format
);