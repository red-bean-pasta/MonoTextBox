namespace HeadlessTextBox.Storage;

public abstract class CeiledStack<T>
{
    protected int Count;
    protected int Next;
    
    protected readonly T[] Items;
    
    
    protected int Capacity => Items.Length;
    protected int CurrentIndex => Wrap(Next - 1, Capacity);
    
    
    protected CeiledStack(int size)
    {
        Items = new T[size];
        Count = 0;
        Next = 0;
    }


    protected void Add(in T item)
    {
        Items[Next] = item;
        MoveToNextSlot();
    }


    protected bool GetCurrentValue(out T value)
    {
        if (Count <= 0)
        {
            value = default;
            return false;
        }

        value = Items[CurrentIndex];
        return true;
    }

    protected bool GetFirstValue(out T value)
    {
        if (Count <= 0)
        {
            value = default;
            return false;
        }

        if (Count < Capacity)
        {
            value = Items[0];
            return true;
        }
        
        value = Items[CurrentIndex + 1];
        return true;
    }
    
    
    protected void Clear()
    {
        Count = 0;
        Next = 0;
    }

    
    protected void MoveToNextSlot()
    {
        if (Count < Capacity)
            Count++;
        
        Next++;
        Next %= Capacity;
    }
    
    protected static int Wrap(int i, int n)
    {
        return (i % n + n) % n;
    }
}