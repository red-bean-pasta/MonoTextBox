using Icu;

namespace HeadlessTextBox.Positioning.Manual.WordBreaking;

public static class LineBreakerManager
{
    private static readonly Locale SystemLocale = new Locale();
    
    private static readonly Dictionary<string, LineBreaker> LineBreakers = new();

    public static LineBreaker Get(Locale? locale)
    {
        locale ??= SystemLocale;
        
        if (LineBreakers.TryGetValue(locale.Id, out var breaker))
            return breaker;
        
        breaker = new LineBreaker(locale);
        LineBreakers.Add(locale.Id, breaker);
        return breaker;
    }
}