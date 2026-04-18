using System.Diagnostics;
using HeadlessTextBox.Compositing.Contracts;
using HeadlessTextBox.Compositing.Serialization;
using HeadlessTextBox.Editing;
using HeadlessTextBox.Formatting;
using HeadlessTextBox.Positioning;
using HeadlessTextBox.Positioning.Manual;
using HeadlessTextBox.Utils;
using Icu;

namespace HeadlessTextBox;

public class TextManager
{
    private readonly SourceBuffer _storage;

    private Caret _caret;
    private bool _newRecordForced;
    private readonly UndoRedoManager _undoRedoManager;

    private Document _document;
    public float Width { get; private set; }
    public Locale Locale { get; private set; }
    public float Height => _document.SubTreeHeightY;


    public TextManager(
        float width,
        int undoStackSize = 256,
        Locale? locale = null)
    {
        _storage = new SourceBuffer();
        _caret = default;
        _undoRedoManager = new UndoRedoManager(undoStackSize);

        Width = width;
        Locale = locale ?? new Locale();
        _document = Document.Build(Width, _storage, Locale);
    }

    public TextManager(
        string text,
        FormatTree format,
        float width,
        int undoStackSize = 256,
        Locale? locale = null)
    {
        _storage = new SourceBuffer(text, format);
        _caret = new Caret(text.Length, 0);
        _undoRedoManager = new UndoRedoManager(undoStackSize);

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

    
    /// <summary>
    /// <see cref="VisualChar"/> returned from enumeration is relatively measured
    /// with the document starting at (0, 0)
    /// </summary>
    /// <param name="startHeight"></param>
    /// <param name="spanHeight"></param>
    /// <returns></returns>
    public TextElementEnumerator EnumerateInScopeElements(float startHeight, float spanHeight)
    {
        return new TextElementEnumerator(_storage, _document, startHeight, spanHeight);
    }


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

        RecordInsert(text);
        
        _storage.Insert(_caret.Left, text);
        
        _caret = new Caret(_caret.Left + text.Length, 0);
    }

    public void Delete()
    {
        if (_storage.Length - _caret.Left < 1)
            return;
        
        RecordDelete();

        _storage.Remove(_caret.Left, Math.Max(1, _caret.Length));
        
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
        _undoRedoManager.Undo(_storage.Text);
    }

    public void Redo()
    {
        _undoRedoManager.Redo(_storage.Text);
    }


    // Undo and redo management
    private void RecordInsert(ReadOnlySpan<char> inserted)
    {
        if (!CheckAppendInsertUndo(_caret))
        {
            _undoRedoManager.AddUndo(_caret, inserted);
            return;
        }

        _undoRedoManager.ExtendCurrentInserted(inserted);
    }
    
    private bool CheckAppendInsertUndo(Caret caretBefore)
    {
        if (!CheckAppendUndoBase())
            return false;

        _undoRedoManager.GetLastRecord(out var record);
        if (record.CaretBefore.Left + record.Inserted.Length != caretBefore.Left)
            return false;
        
        return true;
    }
    
    private void RecordDelete()
    {
        var deleted = _storage
            .Slice(_caret.Left, Math.Max(_caret.Length, 1))
            .GetTextSpan();

        if (!CheckAppendDeleteUndo(_caret))
        {
            _undoRedoManager.AddUndo(_caret, deleted);
            return;
        }

        _undoRedoManager.ExtendCurrentRemoved(deleted);
    }
    
    private bool CheckAppendDeleteUndo(Caret caretBefore)
    {
        if (!CheckAppendUndoBase())
            return false;

        _undoRedoManager.GetLastRecord(out var record);
        if (record.Inserted.Length > 0)
            return false;
        
        if (record.CaretBefore.Left != caretBefore.Left)
            return false;
        
        return true;
    }
    
    private void RecordBackspace()
    {
        var storageSlice = _caret.Length == 0 
            ? _storage.Slice(_caret.Left - 1, 1)
            : _storage.Slice(_caret.Left, _caret.Length);
        var backspaced = storageSlice.GetTextSpan();

        if (!CheckAppendBackspaceUndo(_caret))
        {
            _undoRedoManager.AddUndo(_caret, backspaced);
            return;
        }

        _undoRedoManager.ExtendCurrentRemoved(backspaced);
    }

    private bool CheckAppendBackspaceUndo(Caret caretBefore)
    {
        if (!CheckAppendUndoBase())
            return false;
        
        _undoRedoManager.GetLastRecord(out var record);
        if (record.Inserted.Length > 0)
            return false;
    
        if (caretBefore.Left + record.Removed.Length != record.CaretBefore.Right)
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
    
        if (!_undoRedoManager.GetLastRecord(out _))
            return false;

        return true;
    }
    
    private void EnforceNextUndoNew()
    {
        _newRecordForced = true;
    }
}


public ref struct TextElementEnumerator
{
    private readonly float _topY;
    
    private ParagraphEnumerator _inParagraphEnumerator;
    private Document.NodeEnumerator _paragraphEnumerator;
    
    private TextBufferEnumerator _textEnumerator;
    
    
    public VisualChar Current => GetCurrentValue();
    
    
    public TextElementEnumerator(
        SourceBuffer storage,
        Document document,
        float startHeight,
        float spanHeight)
    {
        var (offsetHeight, startIndex, length) = document.FindInHeightIndices(startHeight, spanHeight);
        
        _topY = startIndex + offsetHeight;
        _paragraphEnumerator = document.EnumerateSliced(startIndex, length);
        _paragraphEnumerator.MoveNext();
        _inParagraphEnumerator = _paragraphEnumerator.Current.GetEnumerator();
        
        _textEnumerator = storage.SlicedEnumerate(startIndex, length);
    }


    public TextElementEnumerator GetEnumerator() => this;

    
    public bool MoveNext()
    {
        if (!_textEnumerator.MoveNext())
        {
            Debug.Assert(!_paragraphEnumerator.MoveNext() && !_inParagraphEnumerator.MoveNext());
            return false;
        }

        if (!_inParagraphEnumerator.MoveNext())
        {
            var moved = _paragraphEnumerator.MoveNext();
            Debug.Assert(moved);
            _inParagraphEnumerator = _paragraphEnumerator.Current.GetEnumerator();
            _inParagraphEnumerator.MoveNext();
        }

        return true;
    }


    private VisualChar GetCurrentValue()
    {
        var (character, format) = _textEnumerator.Current;
        var (offset, range) = _inParagraphEnumerator.Current;
        return new VisualChar(
            character,
            format,
            _topY + offset,
            range.StartPos
        );
    }


    public void Dispose()
    {
        _paragraphEnumerator.Dispose();
        _textEnumerator.Dispose();
    }
}