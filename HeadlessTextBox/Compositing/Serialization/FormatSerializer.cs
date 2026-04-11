using System.Text.Json;
using HeadlessTextBox.Compositing.Serialization.FormatModels;
using HeadlessTextBox.Formatting;

namespace HeadlessTextBox.Compositing.Serialization;

public static class FormatSerializer
{
    public static string SerializeV1(FormatTree tree)
    {
        var model = BuildToModelV1(tree);
        return JsonSerializer.Serialize(model);
    }
    
    
    private static V1<IFormat> BuildToModelV1(FormatTree tree)
    {
        var styles = new Dictionary<IFormat, int>();
        var spans = new List<SpanSpec>();

        foreach (var branch in tree)
        {
            if (!styles.TryGetValue(branch.Format, out var styleIndex))
            {
                styleIndex = styles.Count;
                styles[branch.Format] = styleIndex;
            }

            var span = new SpanSpec(branch.Length, styleIndex);
            spans.Add(span);
        }
        
        return new V1<IFormat>(styles.Keys.ToList(), spans);
    }
}