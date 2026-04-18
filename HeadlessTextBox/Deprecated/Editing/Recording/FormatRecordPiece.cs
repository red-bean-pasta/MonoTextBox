using HeadlessTextBox.Formatting;

namespace HeadlessTextBox.Editing.Recording;

public readonly record struct FormatRecordPiece(
    int Start,
    int Length,
    IFormat Format
);