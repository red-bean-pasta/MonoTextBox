using HeadlessTextBox.Compositing.Contracts;

namespace HeadlessTextBox.Compositing.Serialization;

public static class StorageSerializer
{
    public static string Serialize(TextStorage storage)
    {
        var totalLength = storage.Length; 
        var result = string.Create(totalLength, storage, CopyToString);
        return result;
    }

    
    private static void CopyToString(Span<char> dest, TextStorage storage)
    {
        var pos = 0;
        foreach (var piece in storage.PieceTree)
        {
            var span = storage.GetPieceSpan(piece); 
            span.CopyTo(dest[pos..]);
            pos += piece.Length;
        }
    }
}