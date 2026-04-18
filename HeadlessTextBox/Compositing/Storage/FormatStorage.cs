using HeadlessTextBox.Compositing.Serialization;
using HeadlessTextBox.Formatting;

namespace HeadlessTextBox.Compositing.Storage;

public class FormatStorage
{
    private FormatTree _formatTree;

    
    public int Length => _formatTree.Length;
    

    public FormatStorage()
    {
        _formatTree = FormatTree.Empty;
    }
    
    public FormatStorage(FormatTree formatTree)
    {
        _formatTree = formatTree;
    }
    
    
    public FormatEnumerator GetEnumerator() => new(_formatTree);

    public FormatEnumerator SlicedEnumerate(int start, int length) => new(_formatTree, start, length);

    
    public string Serialize() => FormatSerializer.SerializeV1(_formatTree);
    
    
    public void Extend(int position, int length)
    {
        _formatTree = _formatTree.Extend(position, length);
    }
    
    public void Insert(int position, int length, IFormat format)
    {
        var piece = new FormatPiece(format, length);
        _formatTree = _formatTree.Insert(position, piece);
    }
    
    public void Remove(int position, int length)
    {
        _formatTree = _formatTree.Remove(position, length);
    }

    public void Change(int position, int length, IFormat format)
    {
        _formatTree = _formatTree.Change(position, length, format);
    }
    
    
    public ref struct FormatEnumerator
    {
        private int _remaining;
        private FormatTree.NodeEnumerator _pieceEnumerator;
        
        
        public IFormat Current => _pieceEnumerator.Current.Format;
    
    
        public FormatEnumerator(
            FormatTree tree,
            int start = 0, 
            int length = -1)
        {
            _pieceEnumerator = tree.EnumerateSliced(start, length);
            _remaining = 0;
        }
    
    
        public FormatEnumerator GetEnumerator() => this;


        public bool MoveNext()
        {
            if (_remaining > 0)
            {
                _remaining--;
                return true;
            }

            if (!_pieceEnumerator.MoveNext())
                return false;
            
            _remaining = _pieceEnumerator.Current.Length - 1;
            return true;
        }
    
    
        public void Dispose() => _pieceEnumerator.Dispose();
    }
}