using System.Runtime.InteropServices;

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
    
    
    public static int FindFirstGreater<T>(this ReadOnlySpan<T> span, T value) where T : IComparable<T>
    {
        var low = 0;
        var high = span.Length - 1;

        while (low <= high)
        {
            var mid = (high + low) / 2;
            if (span[mid].CompareTo(value) > 0)
            {
                if (mid == 0 || span[mid - 1].CompareTo(value) <= 0)
                    return mid;
                high = mid - 1;
            }
            else
            {
                low = mid + 1;
            }
        }
        return -1; 
    }
    
    public static int FindFirstGreater<T>(this List<T> list, T value) where T : IComparable<T>
    {
        return FindFirstGreater(CollectionsMarshal.AsSpan(list), value);
    }
}