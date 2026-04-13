using HeadlessTextBox.Formatting;

namespace HeadlessTextBox;

public readonly record struct VisualChar(
    char Char,
    IFormat Format,
    float X,
    float Y
);