using HeadlessTextBox.Formatting;

namespace HeadlessTextBox.Compositing.Serialization.FormatModels;

// {
//     "version": 1,
//     "styles": [
//         {},
//         {"underline": true},
//         {"highlight": "yellow"},
//         {"underline": true, "highlight": "yellow"}
//     ],
//     "runs": [
//         [3, 1],
//         [2, 3],
//         [4, 2]
//     ]
// }
public record struct V1<T>(
    List<T> Styles,
    List<SpanSpec> Spans
) : IModel where T : IFormat
{
    public int Version { get; } = 1;
}


public record struct SpanSpec(
    int Length, 
    int StyleIndex
);