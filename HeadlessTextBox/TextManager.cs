using System.Runtime.InteropServices;
using HeadlessTextBox.Compositing.Contracts;
using HeadlessTextBox.Compositing.Serialization;
using HeadlessTextBox.Editing;
using HeadlessTextBox.Formatting;
using HeadlessTextBox.Positioning;
using HeadlessTextBox.Utils;
using Icu;
using JetBrains.Annotations;

namespace HeadlessTextBox;

public class TextManager
{
    private readonly SourceBuffer _storage;

    private Caret _caret;
    private bool _newRecordForced;
    private readonly RecordManager _undoRedoManager;

    private Document _document;
    public float Width { get; private set; }
    public Locale Locale { get; private set; }
    public float Height => _document.SubTreeHeightY;


    public TextManager(
        float width,
        int undoStackSize = 256,
        Locale? locale = null
    ) : this(string.Empty, new FormatTree(), width, undoStackSize, locale ?? new Locale())
    { }

    public TextManager(
        string text,
        FormatTree format,
        float width,
        int undoStackSize = 256,
        Locale? locale = null)
    {
        _storage = new SourceBuffer(text, format);
        _caret = new Caret(text.Length, 0);
        _undoRedoManager = new RecordManager(undoStackSize);

        Width = width;
        Locale = locale ?? new Locale();
        _document = Document.Build(width, _storage, locale);
    }

    public static TextManager Build<T>(
        string text,
        string format,
        float width,
        int undoStackSize = 256,
        Locale? locale = null
    ) where T : IFormat
    {
        var formatTree = FormatDeserializer<T>.Deserialize(format);
        return new TextManager(text, formatTree, width, undoStackSize, locale);
    }


    public (string Text, string Format) Serialize() => _storage.Serialize();

    
    [MustDisposeResource]
    public Document.DocumentGlyphEnumerator EnumerateVisualGlyphs(float startHeight, float spanHeight) 
        => _document.SliceEnumerateVisualGlyphs(startHeight, spanHeight);
    

    // Whole doc level change
    public void Resize(float newWidth)
    {
        Width = newWidth;
        _document = Document.Build(newWidth, _storage, Locale);
    }

    public void ChangeLocale(Locale newLocale)
    {
        Locale = newLocale;
        _document = Document.Build(Width, _storage, Locale);
    }


    // Caret management
    public void UpdateCaret(int start, int length, bool toLeft)
    {
        OnCaretMoved(start, toLeft ? -length : length);
    }

    public void MoveCaretLeft()
    {
        if (_caret.Length > 0)
        {
            OnCaretMoved(_caret.Left, 0);
            return;
        }

        if (_caret.Start <= 0)
            return;

        OnCaretMoved(_caret.Start - 1, 0);
    }
    
    public void MoveCaretRight()
    {
        if (_caret.Length > 0)
        {
            OnCaretMoved(_caret.Right, 0);
            return;
        }
        
        if (_caret.Start >= _storage.Length)
            return;
        
        OnCaretMoved(_caret.Start + 1, 0);
    }
    
    private void OnCaretMoved(int start, int selection)
    {
        _caret = new Caret(start, selection);
        EnforceNextUndoNew();
    }
    
    
    // Text manipulation
    public void Insert(char character)
    {
        ReadOnlySpan<char> span = stackalloc char[] { character };
        Insert(span);
    }
    
    public void Insert(ReadOnlySpan<char> text)
    {
        if (_caret.Length > 0)
        {
            // EnforceNextUndoNew(); // Selection already triggered OnCaretMoved() and EnforceNextUndoNew()
            Delete();
        }
        
        var format = _storage.Insert(_caret.Left, text);

        var formatPiece = new FormatPiece(format, text.Length);
        var formatSpan = MemoryMarshal.CreateReadOnlySpan(ref formatPiece, 1);
        RecordInsert(text, formatSpan);
        
        _caret = new Caret(_caret.Left + text.Length, 0);
    }
    
    public void Insert(ReadOnlySpan<char> text, ReadOnlySpan<FormatPiece> formats)
    {
        if (_caret.Length > 0)
        {
            // EnforceNextUndoNew(); // Selection already triggered OnCaretMoved() and EnforceNextUndoNew()
            Delete();
        }
        
        _storage.Insert(_caret.Left, text);

        RecordInsert(text, formats);
        
        _caret = new Caret(_caret.Left + text.Length, 0);
    }

    public void Delete()
    {
        if (_storage.Length - _caret.Left < 1)
            return;
        
        _storage.Remove(_caret.Left, Math.Max(1, _caret.Length));
        
        RecordDelete();
        
        _caret = new Caret(_caret.Left, 0);
    }

    public void Backspace()
    {
        if (_caret.Length > 0)
        {
            Delete();
            return;
        }

        if (_caret.Left <= 0)
            return;
        
        RecordBackspace();

        _storage.Remove(_caret.Left - 1, 1);
        
        _caret = new Caret(_caret.Left - 1, 0);
    }

    public void Move(int dest)
    {
        AssertException.ThrowIf(_caret.Selection <= 0);
        
        EnforceNextUndoNew();
        var moved = _storage.Slice(_caret.Left, _caret.Length);
        Delete();
        _caret = new Caret(dest, 0);
        Insert(moved.GetTextSpan());
        
        EnforceNextUndoNew();
    }

    public void Paste(ReadOnlySpan<char> text)
    {
        EnforceNextUndoNew();
        Insert(text);
        
        EnforceNextUndoNew();
    }


    public void Undo()
    {
        _undoRedoManager.Undo(_storage);
    }

    public void Redo()
    {
        _undoRedoManager.Redo(_storage);
    }


    // Undo and redo management
    private void RecordInsert(ReadOnlySpan<char> inserted, ReadOnlySpan<FormatPiece> formats)
    {
        _undoRedoManager.Insert(_caret, inserted, formats, !CheckAppendInsertUndo(_caret));
    }
    
    private bool CheckAppendInsertUndo(Caret caretBefore)
    {
        if (!CheckAppendUndoBase())
            return false;

        _undoRedoManager.GetCurrentValue(out var record);
        if (record.CaretBefore.Left + record.InsertedText.Count != caretBefore.Left)
            return false;
        
        return true;
    }
    
    private void RecordDelete()
    {
        var slice = _storage.Slice(_caret.Left, Math.Max(_caret.Length, 1));
        var deletedText = slice.GetTextSpan();
        using var deletedFormats = FlattenFormatPieces(slice);
        _undoRedoManager.Delete(_caret, deletedText, deletedFormats.AsSpan(), !CheckAppendDeleteUndo(_caret));
    }
    
    private bool CheckAppendDeleteUndo(Caret caretBefore)
    {
        if (!CheckAppendUndoBase())
            return false;

        _undoRedoManager.GetCurrentValue(out var record);
        if (record.InsertedText.Count > 0)
            return false;
        
        if (record.CaretBefore.Left != caretBefore.Left)
            return false;
        
        return true;
    }
    
    private void RecordBackspace()
    {
        var slice = _caret.Length == 0 
            ? _storage.Slice(_caret.Left - 1, 1)
            : _storage.Slice(_caret.Left, _caret.Length);
        var backspacedText = slice.GetTextSpan();
        using var backspacedFormats = FlattenFormatPieces(slice);
        _undoRedoManager.Backspace(_caret, backspacedText, backspacedFormats.AsSpan(), !CheckAppendBackspaceUndo(_caret));
    }

    private bool CheckAppendBackspaceUndo(Caret caretBefore)
    {
        if (!CheckAppendUndoBase())
            return false;
        
        _undoRedoManager.GetCurrentValue(out var record);
        if (record.InsertedText.Count > 0)
            return false;
    
        if (caretBefore.Left + record.RemovedText.Count != record.CaretBefore.Right)
            return false;
    
        return true;
    }
    
    private bool CheckAppendUndoBase()
    {
        if (_newRecordForced)
        {
            _newRecordForced = false;
            return false;
        }
    
        if (!_undoRedoManager.GetCurrentValue(out _))
            return false;

        return true;
    }
    
    private void EnforceNextUndoNew()
    {
        _newRecordForced = true;
    }


    [MustDisposeResource]
    private static RentedList<FormatPiece> FlattenFormatPieces(in SourceRef source)
    {
        var rented = new RentedList<FormatPiece>(8);
        using var enumerator = source.EnumerateFormatPieces();
        foreach (var piece in enumerator)
            rented.Add(piece);
        return rented;
    }
}