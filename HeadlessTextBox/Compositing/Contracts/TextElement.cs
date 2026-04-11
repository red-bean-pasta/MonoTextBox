using HeadlessTextBox.Formatting;

namespace HeadlessTextBox.Compositing.Contracts;

public record struct TextElement(
    char Char, 
    IFormat Format
);