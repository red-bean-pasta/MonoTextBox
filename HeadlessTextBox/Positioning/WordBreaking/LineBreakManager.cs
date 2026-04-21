using System.Collections.Concurrent;
using Icu;

namespace HeadlessTextBox.Positioning.WordBreaking;

public static class LineBreakerManager
{
    private static Locale SystemLocale => new();
    
    private static readonly ConcurrentDictionary<string, LineBreaker> LineBreakers = new();

    public static LineBreaker Get(Locale? locale)
    {
        locale ??= SystemLocale;
        
        if (LineBreakers.TryGetValue(locale.Id, out var breaker))
            return breaker;
        
        breaker = new LineBreaker(locale);
        LineBreakers.TryAdd(locale.Id, breaker);
        return breaker;
    }
}