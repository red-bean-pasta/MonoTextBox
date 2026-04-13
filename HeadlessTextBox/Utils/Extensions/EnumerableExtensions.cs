namespace HeadlessTextBox.Utils.Extensions;

public static class EnumerableExtensions
{
    public static List<T> EnumerateToList<T>(
        IEnumerable<T>? enumerable, 
        int capacity = 0)
    {
        return enumerable as List<T> 
               ?? enumerable?.ToList() 
               ?? new List<T>(capacity);
    }
    
    public static IEnumerable<T> Enumerate<T>(this T item) 
        => Enumerable.Repeat(item, 1);
}