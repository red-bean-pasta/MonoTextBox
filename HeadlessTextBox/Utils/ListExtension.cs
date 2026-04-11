namespace HeadlessTextBox.Utils;

public static class ListExtension
{
    public static int FindFirstGreater<T>(this List<T> list, T value) where T : IComparable<T>
    {
        var low = 0;
        var high = list.Count - 1;

        while (low <= high)
        {
            var mid = (high + low) / 2;
            if (list[mid].CompareTo(value) > 0)
            {
                if (mid == 0 || list[mid - 1].CompareTo(value) <= 0)
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
}