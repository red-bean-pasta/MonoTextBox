using System.Text.Json;
using HeadlessTextBox.Compositing.Serialization.FormatModels;
using HeadlessTextBox.Formatting;

namespace HeadlessTextBox.Compositing.Serialization;


public static class FormatDeserializer<T> where T : IFormat
{
    private static readonly Dictionary<int, Func<IModel, FormatTree>> TreeBuilder = new()
    {
        [1] = BuildFromModelV1
    };

    
    public static FormatTree Deserialize(string spec)
    {
        var model = JsonSerializer.Deserialize<IModel>(spec);
        if (model is null)
            throw new JsonException("Null model returned");
        
        var builder = TreeBuilder[model.Version];
        return builder.Invoke(model);
    }

    
    private static FormatTree BuildFromModelV1(IModel model)
    {
        var m = (V1<T>)model;

        var tree = new FormatTree();
        foreach (var (length, styleIndex) in m.Spans)
        {
            var style = m.Styles[styleIndex];
            var format = new FormatPiece(style, length);
            tree = tree.Append(format);
        }

        return tree;
    }
}