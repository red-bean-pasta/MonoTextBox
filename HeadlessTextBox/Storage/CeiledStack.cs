namespace HeadlessTextBox.Storage;

public class CeiledStack<T>
{
    protected int Count;
    protected int Next;
    
    protected readonly T[] Items;
    
    
    protected int Capacity => Items.Length;
    protected int CurrentIndex => Wrap(Next - 1, Capacity);
    
    
    public CeiledStack(int size)
    {
        Items = new T[size];
        Count = 0;
        Next = 0;
    }


    public void Add(in T item)
    {
        Items[Next] = item;
        MoveToNextSlot();
    }

    public bool Pop(out T value)
    {
        if (!GetCurrentValue(out value))
            return false;
        
        MoveToPreviousSlot();
        return true;
    }


    public bool GetCurrentValue(out T value)
    {
        if (Count <= 0)
        {
            value = default;
            return false;
        }

        value = Items[CurrentIndex];
        return true;
    }

    public bool GetFirstValue(out T value)
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
    
    
    public void Clear()
    {
        Count = 0;
        Next = 0;
    }
    
    
    protected void MoveToPreviousSlot()
    {
        if (Count <= 0) return;
        
        Count--;
        Next--;
        Wrap(Next, Capacity);
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